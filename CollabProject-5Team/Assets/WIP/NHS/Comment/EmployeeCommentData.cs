using System.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "EmployeeCommentData", menuName = "Scriptable Objects/EmployeeCommentData")]
public class EmployeeCommentData : SheetDataSOBase
{
    public int trigger_desire;
    public int trigger_fatigue;
    public int trigger_loyalty;
    public Role target_role;

    public string comment_text;

    public override void SetData(string[] data)
    {
        id              = ParseInt(data[0]);
        trigger_desire  = ParseInt(data[1]);
        trigger_fatigue = ParseInt(data[2]);
        trigger_loyalty = ParseInt(data[3]);
        target_role     = ParseEnum<Role>(data[4]);

        comment_text    = data[5];
    }
}