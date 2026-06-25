using System.Collections.Generic;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public List<QuestSO> dailyQuests;

    [SerializeField] private Transform questObjectsRoot;   // 퀘스트용 오브젝트들이 들어있는 루트
    [SerializeField] private GameObject speechBubblePrefab; // 퀘스트 성공 시 직원 머리 위에 띄울 말풍선 프리팹
    [SerializeField] private Vector3 bubbleWorldOffset = new Vector3(0f, 2f, 0f); // 말풍선이 뜰 위치 (직원 기준 오프셋)
    [SerializeField] private RectTransform questCanvas;    // 전구/별/말풍선이 생성될 Canvas_Quest

    public RectTransform QuestCanvas => questCanvas;

    public void SetQuestObjectsRoot(Transform root)
    {
        questObjectsRoot = root;
        LogDuplicateQuestObjectNames();
    }

    // 같은 이름의 QuestObject가 2개 이상 있으면 경고. FindQuestObject는 이름으로 첫 번째 매치만 찾기 때문에
    // 중복된 이름이 있으면 의도치 않은 오브젝트가 토글되거나, 다른 하나가 영원히 방치될 수 있음
    private void LogDuplicateQuestObjectNames()
    {
        if (questObjectsRoot == null) return;

        var nameCount = new Dictionary<string, int>();
        foreach (QuestObject qo in questObjectsRoot.GetComponentsInChildren<QuestObject>(true))
        {
            nameCount.TryGetValue(qo.gameObject.name, out int count);
            nameCount[qo.gameObject.name] = count + 1;
        }

        foreach (var pair in nameCount)
        {
            if (pair.Value > 1)
                Debug.LogWarning($"[QM] 중복된 QuestObject 이름: '{pair.Key}' ({pair.Value}개) - questObjectsRoot 하위에서 이름 충돌");
        }
    }

    // 직전에 나온 퀘스트 기억 (다음 뽑기에서 제외 + 결과물 정리용)
    private QuestSO _lastPicked;

    private int _questPhase = 1;
    public ControlType EffectiveControlType => _questPhase == 2 ? curDailyQuest.so.controlType2 : curDailyQuest.so.controlType;
    public int EffectiveTargetCount => _questPhase == 2 ? curDailyQuest.so.targetCount2 : curDailyQuest.TargetCount;

    public ReactiveProperty<QuestState> dailyQuestState = new(QuestState.Ready);
    public DailyQuest curDailyQuest;

    // 현재 진행도 (UI 표시용)
    public ReactiveProperty<int> dailyQuestProgress = new(0);

    // 직군별 일일 퀘스트 클리어 누적 포인트 (금요일 밤 보고서 점수에 합산)
    public Dictionary<Role, int> _weeklyBonusPoints = new();

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        if (newState != QuestState.End) return;

        if (_questPhase == 2)
            SetObjectsActive(curDailyQuest.so.resultObjects, false);
        else
            SetObjectsActive(curDailyQuest.so.activeObjects, false);

        if (curDailyQuest.result == QuestResult.Success)
        {
            if (_questPhase != 2)
                SetObjectsActive(curDailyQuest.so.resultObjects, true);

            ShowSpeechBubble(curDailyQuest.so.npcDialogue);
            AddBonusPoint(curDailyQuest.so.role, curDailyQuest.so.successEffect);
            DateTimeManager.Instance.CompleteDayWork();
        }
    }

    // 새 하루 시작 전 상태 초기화. 디버그 스킵 등으로 퀘스트가 미완료 상태로 남아있으면
    // OnDailyQuestChanged(End)를 안 거치고 넘어가므로 여기서 직접 활성 오브젝트를 정리해야 함
    public void ResetForNewDay()
    {
        if (curDailyQuest == null) return;

        if (_questPhase == 2)
            SetObjectsActive(curDailyQuest.so.resultObjects, false);
        else
            SetObjectsActive(curDailyQuest.so.activeObjects, false);

        dailyQuestState.Value = QuestState.Ready;
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

        StartDailyQuestAsync().Forget();
    }

    // GameManager가 맵 생성 후 SetQuestObjectsRoot를 호출하기 전에 StartDailyQuest가 불릴 수 있어서
    // (게임 시작 직후 바로 업무 시작을 누르는 경우) questObjectsRoot가 준비될 때까지 대기 후 진행
    private async UniTask StartDailyQuestAsync()
    {
        while (questObjectsRoot == null)
            await UniTask.Yield();

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

        _questPhase = 1;
        curDailyQuest = new DailyQuest();
        curDailyQuest.Init(picked);
        dailyQuestProgress.Value = 0;

        // activeObjects를 켜기 전에 먼저 꺼서 겹쳐 보이는 것을 방지
        SetObjectsActive(picked.resultObjects, false);

        // 오늘의 활성 오브젝트 켜기
        SetObjectsActive(picked.activeObjects, true);

        // curDailyQuest.state를 직접 동기화 (Subscribe 등록 전에 호출돼도 안전하도록)
        curDailyQuest.state = QuestState.Playing;

        // 상태를 변경하면 Subscribe된 로직이 실행
        dailyQuestState.Value = QuestState.Playing;
    }

    // (전체 퀘스트가 끝나야 한꺼번에 바뀌는 게 아니라, 하나씩 완료할 때마다 바로바로 바뀌도록)
    public void ActivatePairedResult(string completedObjectName)
    {
        if (curDailyQuest == null || _questPhase == 2) return;

        string[] actives = curDailyQuest.so.activeObjects.Split(',');
        string[] results = curDailyQuest.so.resultObjects.Split(',');

        for (int i = 0; i < actives.Length; i++)
        {
            if (actives[i].Trim() != completedObjectName) continue;

            if (i < results.Length)
            {
                string resultName = results[i].Trim();
                if (resultName != "" && resultName != "None")
                    SetObjectsActive(resultName, true);
            }
            return;
        }
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

        if (curDailyQuest.state == QuestState.End)
        {
            if (_questPhase == 1 && curDailyQuest.so.controlType2 != ControlType.NONE)
            {
                _questPhase = 2;
                SetObjectsActive(curDailyQuest.so.activeObjects, false);
                SetObjectsActive(curDailyQuest.so.resultObjects, true);
                curDailyQuest.ResetForPhase2(curDailyQuest.so.targetCount2);
                dailyQuestProgress.Value = 0;
            }
            else
            {
                dailyQuestState.Value = QuestState.End;
            }
        }
    }

    // HOLD 진행 중 실시간 표시용 - curCount(실제 완료 판정)는 건드리지 않고 배너 진행도만 갱신
    public void SetDisplayProgress(int current)
    {
        if (dailyQuestState.Value != QuestState.Playing) return;

        dailyQuestProgress.Value = Mathf.Min(current, EffectiveTargetCount);
    }

    // 콤마로 구분된 오브젝트 이름들을 questObjectsRoot 하위에서 찾아 활성/비활성 처리
    private void SetObjectsActive(string names, bool active)
    {
        if (string.IsNullOrEmpty(names) || questObjectsRoot == null) return;

        foreach (string objName in names.Split(','))
        {
            string trimmed = objName.Trim();
            if (trimmed == "" || trimmed == "None") continue;

            // QuestObject 컴포넌트가 있는 오브젝트 우선 탐색 (같은 이름의 메쉬 오브젝트와 혼동 방지)
            QuestObject questObject = FindQuestObject(trimmed);
            if (questObject != null)
            {
                if (questObject.IsPermanent)
                {
                    if (active) questObject.Activate();
                    else questObject.Deactivate();
                }
                else
                    questObject.gameObject.SetActive(active);
                continue;
            }

            // QuestObject 없는 일반 오브젝트 (resultObjects 등) — 이름으로 탐색
            Transform target = FindDeepChild(questObjectsRoot, trimmed);
            if (target == null)
            {
                Debug.LogWarning($"[QM] SetObjectsActive - '{trimmed}'를 questObjectsRoot 하위에서 못 찾음");
                continue;
            }
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

        bubble.transform.SetAsFirstSibling();

        bubble.Show(npcTransform, bubbleWorldOffset, message);
    }

    private QuestObject FindQuestObject(string name)
    {
        foreach (QuestObject qo in questObjectsRoot.GetComponentsInChildren<QuestObject>(true))
        {
            if (qo.gameObject.name == name) return qo;
        }
        return null;
    }

    private Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
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

    #region 세이브/로드
    public void ExportQuestData(SaveData data)
    {
        data.weeklyBonusPoints = new Dictionary<Role, int>(_weeklyBonusPoints);
    }

    public void ImportQuestData(SaveData data)
    {
        _weeklyBonusPoints.Clear();

        foreach (var pair in data.weeklyBonusPoints)
        {
            _weeklyBonusPoints[pair.Key] = pair.Value;
        }
    }
    #endregion
}
