using UnityEngine;

[CreateAssetMenu(fileName = "ProjectUpdateSO", menuName = "Scriptable Objects/ProjectUpdateSO")]
public class ProjectUpdateSO : SheetDataSOBase
{
    public string Name;
    public Role role;
    public int cost;
    public ProjectSize size;
    public string desc;

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        Name = data[1];
        role = ParseEnum<Role>(data[2]);
        cost = ParseInt(data[3]);
        size = ParseEnum<ProjectSize>(data[4]);
        desc = data[5];
    }
}
