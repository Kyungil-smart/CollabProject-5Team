using UnityEngine;

[CreateAssetMenu(fileName = "WebRequestTestSO", menuName = "Scriptable Objects/WebRequestTestSO")]
public class WebRequestTestSO : SheetDataSOBase
{
    [Header("정보")]
    public string nickName;
    [field: TextArea] public string desc;
    public int hp;
    public int attack;
    public int defense;
    public bool canChase;

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        nickName = data[1];
        desc = data[2];
        hp = ParseInt(data[3]);
        attack = ParseInt(data[4]);
        defense = ParseInt(data[5]);
        canChase = ParseBool(data[6]);
    }
}
