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
        activeObjects = data[5];
        resultObjects = data[6];
        targetMap = data[7];
        successEffect = ParseInt(data[8]);
        npcDialogue = data[9];
    }
}