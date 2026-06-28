public enum QuestState
{
    Locked,    // 스토리 퀘스트 조건 미충족
    Ready,
    Playing,   // 진행 중
    End,       // 완료
}

public enum QuestType
{
    Daily,  // 일일 퀘스트
    Story,  // 스토리 퀘스트
    Event,  // 이벤트 퀘스트 (주로 대화)
}

public enum QuestRewardType
{
    None,
    ProjectScore,
    Gold,
}

public struct QuestReward
{
    public QuestRewardType type;
    public Role role;
    public int amount;

    QuestReward(QuestRewardType type, Role role, int amount)
    {
        this.type = type;
        this.role = role;
        this.amount = amount;
    }

    public static QuestReward None => new(QuestRewardType.None, default, 0);
    public static QuestReward ProjectScore(Role role, int amount) => new(QuestRewardType.ProjectScore, role, amount);
    public static QuestReward Gold(int amount) => new(QuestRewardType.Gold, default, amount);
}

public enum ControlType
{
    NONE, TAP, HOLD,
}

public abstract class QuestBase
{
    public QuestType type;
    public QuestState state;
    public virtual QuestReward Reward => QuestReward.None;
}

public class DailyQuest : QuestBase
{
    public QuestSO so;

    private int _targetCount;
    public int TargetCount => _targetCount;
    public int curCount;
    public override QuestReward Reward => QuestReward.ProjectScore(so.role, so.successEffect);

    // 퀘스트 초기화
    public void Init(QuestSO questSO)
    {
        type = QuestType.Daily;
        state = QuestState.Ready;
        so = questSO;
        curCount = 0;
        _targetCount = questSO.targetCount;
    }

    // 퀘스트 진행 업데이트
    public void UpdateProgress(int count)
    {
        if (state != QuestState.Playing) return;
        curCount += count;
        if (curCount >= TargetCount)
        {
            state = QuestState.End;
        }
    }

    // 복합 조작 phase 2 전환 시 호출
    public void ResetForPhase2(int newTargetCount)
    {
        state = QuestState.Playing;
        curCount = 0;
        _targetCount = newTargetCount;
    }
}

public class StoryQuest : QuestBase
{
    public StoryQuestPoolSO so;

    // 스토리 퀘스트 초기화
    public void Init(StoryQuestPoolSO questSO)
    {
        type = QuestType.Story;
        state = QuestState.Locked;
        so = questSO;
    }

    public void SetReady()
    {
        if (state != QuestState.Locked) return;
        state = QuestState.Ready;
    }

    public void StartQuest()
    {
        if (state != QuestState.Ready) return;
        state = QuestState.Playing;
    }

    public void Complete()
    {
        state = QuestState.End;
    }
}

public class EventQuest : QuestBase
{ // 일단 이거 하나로 퀘스트 고정
    public const string QuestTypeName = "대화 퀘스트";
    public const string QuestName = "아무 직원이랑 대화하기";
    public const int GoldRewardAmount = 200;

    private const int DefaultTargetCount = 1;

    public int TargetCount => DefaultTargetCount;
    public int curCount;
    public override QuestReward Reward => QuestReward.Gold(GoldRewardAmount);

    public void Init()
    {
        type = QuestType.Event;
        state = QuestState.Ready;
        curCount = 0;
    }

    public void StartQuest()
    {
        if (state != QuestState.Ready) return;
        state = QuestState.Playing;
    }

    public void CompleteDialogue()
    {
        if (state != QuestState.Playing) return;
        curCount = TargetCount;
        state = QuestState.End;
    }
}
