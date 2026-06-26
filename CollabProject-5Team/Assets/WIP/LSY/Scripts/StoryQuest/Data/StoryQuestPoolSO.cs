using UnityEngine;

[CreateAssetMenu(fileName = "StoryQuestPool_", menuName = "Scriptable Objects/StoryQuest/StoryQuestPoolSO")]
public class StoryQuestPoolSO : SheetDataSOBase
{
    public string questName;
    public int parentId;       // 선행 퀘스트 ID - 0이면 선행 퀘스트 없음(루트)
    public int conditionCompanyLv;
    public int conditionGold;
    public int conditionReputation;
    public bool isSpyQuest;
    public int startDialogueId;
    public string successEffect;  // 기획 미확정 - 빈 값일 수 있음, placeholder로 취급

    public override void SetData(string[] data)
    {
        id                  = ParseInt(data[0]);
        questName           = data[1].Trim();
        parentId            = ParseInt(data[2]);
        conditionCompanyLv  = ParseInt(data[3]);
        conditionGold       = ParseInt(data[4]);
        conditionReputation = ParseInt(data[5]);
        isSpyQuest          = ParseBool(data[6]);
        startDialogueId     = ParseInt(data[7]);
        successEffect       = data.Length > 8 ? data[8].Trim() : "";
    }
}