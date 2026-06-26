public enum QuestState
{
    Ready,
    Playing,   // 진행 중
    End,       // 완료
    Locked,    // 잠금
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
}

public class DailyQuest : QuestBase
{
    public QuestSO so;
    public QuestResult result;

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
    public QuestResult result;

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
