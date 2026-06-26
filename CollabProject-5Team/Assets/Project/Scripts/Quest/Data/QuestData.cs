public enum QuestState
{
    Locked,    // 스토리 퀘스트 조건 미충족
    Ready,
    Playing,   // 진행 중
    End,       // 완료
}
public enum QuestResult
{
    None,
    Success,   // 성공
    Fail,      // 실패
}

public enum QuestType
{
    Daily,  // 일일 퀘스트
    Story,  // 스토리 퀘스트
    Event,  // 이벤트 퀘스트 (주로 대화)
}

public enum ControlType
{
    NONE, TAP, HOLD,
}

public abstract class QuestBase
{
    public QuestType type;
    public QuestState state;
    public QuestResult result;
}

public class DailyQuest : QuestBase
{
    public QuestSO so;

    private int _targetCount;
    public int TargetCount => _targetCount;
    public int curCount;

    // 퀘스트 초기화
    public void Init(QuestSO questSO)
    {
        type = QuestType.Daily;
        state = QuestState.Ready;
        so = questSO;
        result = QuestResult.None;
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
            result = QuestResult.Success;
        }
    }

    // 복합 조작 phase 2 전환 시 호출
    public void ResetForPhase2(int newTargetCount)
    {
        state = QuestState.Playing;
        result = QuestResult.None;
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
        result = QuestResult.None;
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
        result = QuestResult.Success;
    }
}

public class EventQuest : QuestBase
{
    public const string QuestTypeName = "대화 퀘스트"; // 일단 이거 하나로 퀘스트 고정
    public const string QuestName = "아무 직원이랑 대화하기";

    private const int DefaultTargetCount = 1;

    public int TargetCount => DefaultTargetCount;
    public int curCount;

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
