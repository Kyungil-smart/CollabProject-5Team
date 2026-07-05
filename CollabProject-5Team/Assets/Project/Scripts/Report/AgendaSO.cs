using UnityEngine;

[CreateAssetMenu(fileName = "AgendaSO", menuName = "Scriptable Objects/AgendaSO")]
public class AgendaSO : SheetDataSOBase
{
    public int grade;
    public float sucessRate;
    public int cost;
    public string desc;

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        grade = ParseInt(data[3]);
        sucessRate = ParseFloat(data[4]);
        cost = ParseInt(data[5]);
        desc = data[6];
    }
}
