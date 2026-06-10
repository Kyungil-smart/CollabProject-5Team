using System.Collections.Generic;
using UnityEngine;
using R3;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public List<QuestSO> dailyQuests;

    public ReactiveProperty<QuestState> dailyQuestState = new(QuestState.Ready);
    public DailyQuest curDailyQuest;

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
    }

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
        // 로직 추가 필요
    }

    // 일일 퀘스트 시작!
    public void StartDailyQuest()
    {
        if (dailyQuestState.Value != QuestState.Ready)
        {
            Debug.LogWarning("[QM] 퀘스트 준비상태가 아닌데 뭐죠?");
            return;
        }

        // 전체목록중 무식하게 랜덤으로 하나 선택
        int i = Random.Range(0, dailyQuests.Count);

        curDailyQuest = new DailyQuest();
        curDailyQuest.Init(dailyQuests[i]);

        // 상태를 변경하면 Subscribe된 로직이 실행
        dailyQuestState.Value = QuestState.Playing;
    }
}
