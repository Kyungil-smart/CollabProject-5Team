using System.Collections.Generic;
using UnityEngine;
using R3;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public List<QuestSO> dailyQuests;

    [SerializeField] private Transform questObjectsRoot;   // 퀘스트용 오브젝트들이 들어있는 루트
    [SerializeField] private GameObject speechBubblePrefab; // 퀘스트 성공 시 직원 머리 위에 띄울 말풍선 프리팹
    [SerializeField] private Vector3 bubbleWorldOffset = new Vector3(0f, 2f, 0f); // 말풍선이 뜰 위치 (직원 기준 오프셋)
    [SerializeField] private RectTransform questCanvas;    // 전구/별/말풍선이 생성될 Canvas_Quest

    public RectTransform QuestCanvas => questCanvas;

    // 직전에 나온 퀘스트 기억 (다음 뽑기에서 제외 + 결과물 정리용)
    private QuestSO _lastPicked;

    public ReactiveProperty<QuestState> dailyQuestState = new(QuestState.Ready);
    public DailyQuest curDailyQuest;

    // 현재 진행도 (UI 표시용)
    public ReactiveProperty<int> dailyQuestProgress = new(0);

    // 직군별 일일 퀘스트 클리어 누적 포인트 (금요일 밤 보고서 점수에 합산)
    private readonly Dictionary<Role, int> _weeklyBonusPoints = new();

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }
    #endregion

    private void Start()
    {
        // dailyQuestState 변화감지
        dailyQuestState.Skip(1).Subscribe(state =>
        {
            curDailyQuest.state = state;
            OnDailyQuestChanged(state);
        }).AddTo(this);
    }

    // R3 구독을 통해 실행될 상태별 로직 처리부
    void OnDailyQuestChanged(QuestState newState)
    {
        Debug.Log($"[QM] OnDailyQuestChanged 호출됨 - newState: {newState}, result: {curDailyQuest.result}");

        if (newState != QuestState.End) return;

        // 진행 중이던 활성 오브젝트는 정리
        SetObjectsActive(curDailyQuest.so.activeObjects, false);

        if (curDailyQuest.result == QuestResult.Success)
        {
            Debug.Log("[QM] 퀘스트 성공 처리 시작");

            SetObjectsActive(curDailyQuest.so.resultObjects, true);
            ShowSpeechBubble(curDailyQuest.so.npcDialogue);

            AddBonusPoint(curDailyQuest.so.role, curDailyQuest.so.successEffect);
            DateTimeManager.Instance.CompleteDayWork();

            Debug.Log("[QM] CompleteDayWork 호출 완료");
        }
    }

    // 일일 퀘스트 시작! (출근 시 호출)
    public void StartDailyQuest()
    {
        if (dailyQuestState.Value != QuestState.Ready)
        {
            Debug.LogWarning("[QM] 퀘스트 준비상태가 아닌데 뭐죠?");
            return;
        }

        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Debug.LogWarning("[QM] dailyQuests가 비어있습니다.");
            return;
        }

        // 전날 결과물 정리 (resultObjects)
        if (_lastPicked != null)
        {
            SetObjectsActive(_lastPicked.resultObjects, false);
        }

        // 직전 퀘스트를 제외한 후보 목록 구성 (풀이 1개면 제외 없이 그대로 사용)
        List<QuestSO> candidates = dailyQuests.Count > 1
            ? dailyQuests.FindAll(q => q != _lastPicked)
            : dailyQuests;

        QuestSO picked = candidates[Random.Range(0, candidates.Count)];
        _lastPicked = picked;

        curDailyQuest = new DailyQuest();
        curDailyQuest.Init(picked);
        dailyQuestProgress.Value = 0;

        // 오늘의 활성 오브젝트 켜기
        SetObjectsActive(picked.activeObjects, true);

        // curDailyQuest.state를 직접 동기화 (Subscribe 등록 전에 호출돼도 안전하도록)
        curDailyQuest.state = QuestState.Playing;

        // 상태를 변경하면 Subscribe된 로직이 실행
        dailyQuestState.Value = QuestState.Playing;
    }

    // 미니게임 입력에 따른 진행도 갱신 (Tap/Hold/Swipe 컨트롤러에서 호출)
    public void UpdateProgress(int count)
    {
        if (dailyQuestState.Value != QuestState.Playing)
        {
            Debug.Log($"[QM] UpdateProgress 무시됨 - 현재 상태: {dailyQuestState.Value}");
            return;
        }

        curDailyQuest.UpdateProgress(count);
        dailyQuestProgress.Value = curDailyQuest.curCount;

        Debug.Log($"[QM] 진행도 갱신 - curCount: {curDailyQuest.curCount} / {curDailyQuest.TargetCount}, state: {curDailyQuest.state}");

        if (curDailyQuest.state == QuestState.End)
            dailyQuestState.Value = QuestState.End;
    }

    // 콤마로 구분된 오브젝트 이름들을 questObjectsRoot 하위에서 찾아 활성/비활성 처리
    private void SetObjectsActive(string names, bool active)
    {
        if (string.IsNullOrEmpty(names) || questObjectsRoot == null) return;

        foreach (string objName in names.Split(','))
        {
            string trimmed = objName.Trim();
            if (trimmed == "" || trimmed == "None") continue;

            Transform target = FindDeepChild(questObjectsRoot, trimmed);
            if (target == null) continue;

            // 항상 있는 오브젝트(커피머신 등)는 GameObject를 끄지 않고 QuestObject 컴포넌트만 토글
            QuestObject questObject = target.GetComponent<QuestObject>();
            if (questObject != null && questObject.IsPermanent)
                questObject.enabled = active;
            else
                target.gameObject.SetActive(active);
        }
    }

    // 활성 NPC 중 한 명의 머리 위에 말풍선을 띄움 (몇 초 후 자동 소멸)
    private void ShowSpeechBubble(string message)
    {
        if (speechBubblePrefab == null || questCanvas == null) return;

        Transform npcTransform = GameManager.Instance.GetRandomActiveNpcTransform();
        if (npcTransform == null) return;

        SpeechBubble bubble = Instantiate(speechBubblePrefab, questCanvas).GetComponent<SpeechBubble>();
        if (bubble == null)
        {
            Debug.LogWarning("[QM] speechBubblePrefab에 SpeechBubble 컴포넌트가 없습니다.");
            return;
        }

        bubble.Show(npcTransform, bubbleWorldOffset, message);
    }

    private Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;

            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    void AddBonusPoint(Role role, int amount)
    {
        _weeklyBonusPoints.TryGetValue(role, out int cur);
        _weeklyBonusPoints[role] = cur + amount;
    }

    // 금요일 밤 보고서 정산 시 직군별 누적 포인트 조회
    public int GetWeeklyBonus(Role role)
    {
        return _weeklyBonusPoints.TryGetValue(role, out int value) ? value : 0;
    }

    // 주간 정산 후 초기화
    public void ResetWeeklyBonus()
    {
        _weeklyBonusPoints.Clear();
    }
}