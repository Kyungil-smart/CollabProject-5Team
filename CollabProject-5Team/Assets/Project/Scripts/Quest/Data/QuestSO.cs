using UnityEngine;

[CreateAssetMenu(fileName = "DailyQuestSO", menuName = "Scriptable Objects/DailyQuestSO")]
public class QuestSO : SheetDataSOBase
{
    public string Name;
    public Role role;
    public ControlType controlType;
    public int targetCount;
    public ControlType controlType2;
    public int targetCount2;
    public string activeObjects;
    public string resultObjects;
    public string targetMap;
    public int successEffect;
    public string npcDialogue;

    public int ActiveObjectCount
    {
        get
        {
            if (string.IsNullOrEmpty(activeObjects)) return 0;

            int count = 0;
            foreach (string objName in activeObjects.Split(','))
            {
                string trimmed = objName.Trim();
                if (trimmed != "" && trimmed != "None") count++;
            }
            return count;
        }
    }

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        Name = data[1];
        role = ParseEnum<Role>(data[2]);
        controlType = ParseEnum<ControlType>(data[3]);
        targetCount = ParseInt(data[4]);
        controlType2 = string.IsNullOrWhiteSpace(data[5]) ? ControlType.NONE : ParseEnum<ControlType>(data[5]);
        targetCount2 = string.IsNullOrWhiteSpace(data[6]) ? 0 : ParseInt(data[6]);
        activeObjects = data[7];
        resultObjects = data[8];
        targetMap = data[9];
        successEffect = ParseInt(data[10]);
        npcDialogue = data[11];
    }
}