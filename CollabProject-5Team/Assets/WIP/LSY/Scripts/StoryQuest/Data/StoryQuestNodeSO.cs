using UnityEngine;

[CreateAssetMenu(fileName = "StoryQuestNode_", menuName = "Scriptable Objects/StoryQuest/StoryQuestNodeSO")]
public class StoryQuestNodeSO : SheetDataSOBase
{
    public int nextId;     // 다음 노드 ID (0이면 종료)
    public string speaker; // 화자: USER, NPC1, NPC2, SPY, UCSPY, COMP, BLANK
    public bool isUser => speaker == "USER";
    public bool isBlank => speaker == "BLANK";

    [TextArea(2, 5)]
    public string text;

    public override void SetData(string[] data)
    {
        id      = ParseInt(data[0]);
        nextId  = ParseInt(data[1]);
        speaker = data[2].Trim();
        text    = data[3].Trim();
    }
}