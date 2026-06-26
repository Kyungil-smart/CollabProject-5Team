using Dialogue;
using R3;
using UnityEngine;

public class EventQuestManager : MonoBehaviour
{
    public static EventQuestManager Instance;

    [SerializeField, Range(0f, 1f)] float eventQuestChanceWithProject = 0.23f;

    public ReactiveProperty<QuestState> eventQuestState = new(QuestState.Ready);
    public ReactiveProperty<int> eventQuestProgress = new(0);
    public EventQuest curEventQuest;

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    #endregion
        DialogueEvents.OnDialogueEnded
            .Subscribe(_ => CompleteDialogueQuest())
            .AddTo(this);
    }
    public bool TryStartEventQuestForToday()
    {
        if (!ShouldStartEventQuest()) return false;

        StartDialogueQuest();
        return true;
    }

    public void StartDialogueQuest()
    {
        if (eventQuestState.Value == QuestState.Playing)
        {
            Debug.LogWarning("[EventQuestManager] 이미 이벤트 퀘스트가 진행 중입니다.");
            return;
        }

        curEventQuest = new EventQuest();
        curEventQuest.Init();
        curEventQuest.StartQuest();

        eventQuestProgress.Value = curEventQuest.curCount;
        DateTimeManager.Instance.isEventQuest = true;
        eventQuestState.Value = curEventQuest.state;
    }

    public void ResetForNewDay()
    {
        DateTimeManager.Instance.isEventQuest = false;

        if (eventQuestState.Value != QuestState.Playing) return;

        curEventQuest = null;
        eventQuestProgress.Value = 0;
        eventQuestState.Value = QuestState.Ready;
    }

    private bool ShouldStartEventQuest()
    {
        if (Company.Instance.activeProjectCount.Value <= 0)
            return true;

        return Random.value < eventQuestChanceWithProject;
    }

    private void CompleteDialogueQuest()
    {
        if (curEventQuest == null || curEventQuest.state != QuestState.Playing) return;

        curEventQuest.CompleteDialogue();
        eventQuestProgress.Value = curEventQuest.curCount;
        DateTimeManager.Instance.isEventQuest = false;
        eventQuestState.Value = curEventQuest.state;
        DateTimeManager.Instance.CompleteDayWork();
    }
}
