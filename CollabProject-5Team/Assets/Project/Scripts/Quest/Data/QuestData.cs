

public enum QuestState
{
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
    Event,  // 이벤트 퀘스트
}

public enum ControlType
{
    TAP, HOLD, SWIPE,
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

    public int TargetCount => so.targetCount;
    public int curCount;

    // 퀘스트 초기화
    public void Init(QuestSO questSO)
    {
        type = QuestType.Daily;
        state = QuestState.Ready;
        so = questSO;
        result = QuestResult.None;
        curCount = 0;
    }

    // 퀘스트 진행 업데이트
    public void UpdateProgress(int count)
    {
        if (state != QuestState.Playing) return;
        curCount += count;
        if (curCount >= TargetCount)
        {
            state = QuestState.End;
            result = QuestResult.Success; // 실패 조건이 있다면 추가 로직 필요
        }
    }
}