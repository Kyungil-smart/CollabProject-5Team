using UnityEngine;

[CreateAssetMenu(fileName = "DailyQuestSO", menuName = "Scriptable Objects/DailyQuestSO")]
public class QuestSO : SheetDataSOBase
{
    public string Name;
    public Role role;
    public ControlType controlType;
    public int targetCount;
    public string activeObjects;
    public string resultObjects;
    public string successEffect;
    public string npcDialogue;

    public override void SetData(string[] data)
    {
        // 시트 리뷰가 필요해 보여서 확정 후 시트데이에 맞게 다시 수정~
        Name = data[1];
        role = ParseEnum<Role>(data[2]);
        controlType = ParseEnum<ControlType>(data[3]);
        targetCount = ParseInt(data[4]);
        activeObjects = data[5];
        resultObjects = data[6];
        successEffect = data[7];
        npcDialogue = data[8];
    }
}
