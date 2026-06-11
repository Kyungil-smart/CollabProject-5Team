using System.Collections.Generic;
using UnityEngine;
using R3;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public List<QuestSO> dailyQuests;

    // 직전에 나온 퀘스트 기억 (다음 뽑기에서 제외)
    private QuestSO _lastPicked;

    public ReactiveProperty<QuestState> dailyQuestState = new(QuestState.Ready);
    public DailyQuest curDailyQuest;

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
        if (newState != QuestState.End) return;

        if (curDailyQuest.result == QuestResult.Success)
        {
            AddBonusPoint(curDailyQuest.so.role, curDailyQuest.so.successEffect);
            DateTimeManager.Instance.CompleteDayWork();
        }
    }

    // 일일 퀘스트 시작!
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

        // 직전 퀘스트를 제외한 후보 목록 구성 (풀이 1개면 제외 없이 그대로 사용)
        List<QuestSO> candidates = dailyQuests.Count > 1
            ? dailyQuests.FindAll(q => q != _lastPicked)
            : dailyQuests;

        QuestSO picked = candidates[Random.Range(0, candidates.Count)];
        _lastPicked = picked;

        curDailyQuest = new DailyQuest();
        curDailyQuest.Init(picked);

        // 상태를 변경하면 Subscribe된 로직이 실행
        dailyQuestState.Value = QuestState.Playing;
    }

    // 미니게임 입력에 따른 진행도 갱신 (Tap/Hold/Swipe 컨트롤러에서 호출)
    public void UpdateProgress(int count)
    {
        if (dailyQuestState.Value != QuestState.Playing) return;

        curDailyQuest.UpdateProgress(count);

        if (curDailyQuest.state == QuestState.End)
            dailyQuestState.Value = QuestState.End;
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
