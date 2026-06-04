using UnityEngine;

public enum QuestStatus
{
    None,      // 없음
    Playing,   // 진행 중
    Completed, // 완료
    Failed,    // 실패
}

public enum QuestType
{
    daily,  // 일일 퀘스트
    Story,  // 스토리 퀘스트
    Event,  // 이벤트 퀘스트
}

public enum QuestMode
{
    None,
    Tap,   // 연타
    Hold,  // 누르고 있기
    Swipe, // 슬라이드
    Talk,
}

public class Quest
{
    public QuestStatus status;
    public QuestType type;
    public QuestMode mode;
}
