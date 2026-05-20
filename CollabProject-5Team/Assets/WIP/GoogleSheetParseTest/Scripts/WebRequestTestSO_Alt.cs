using UnityEngine;

public enum MaterialType
{
    Wood, Liquid, Metal
}

[CreateAssetMenu(fileName = "WebRequestTestSO_Alt", menuName = "Scriptable Objects/WebRequestTestSO_Alt")]
public class WebRequestTestSO_Alt : SheetDataSOBase
{
    [Header("정보")]
    public string nickName;
    public MaterialType materialType;
    public float softness;
    public bool canProcess;

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        nickName = data[1];
        materialType = ParseEnum<MaterialType>(data[2]);
        softness = ParseFloat(data[3]);
        canProcess = ParseBool(data[4]);
    }
}
