using System.Xml.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; 
#endif

[CreateAssetMenu(fileName = "CutSceneData", menuName = "Scriptable Objects/CutSceneData")]
public class CutSceneData : SheetDataSOBase
{
    public string employeeName;
    [TextArea] public string dialogue;
    public Sprite cutSceenImage;

    public override void SetData(string[] data)
    {
        id           = ParseInt(data[0]);
        employeeName = data[1].Trim();
        dialogue     = data[2].Trim();

        string imageName = data[3].Trim();
        SetImageByName(imageName);
    }

    private void SetImageByName(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
        {
            cutSceenImage = null;
            return;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets($"{imageName} t:Sprite");

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            cutSceenImage = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        else
        {
            Debug.LogWarning($"[{employeeName}] 이미지를 찾을 수 없습니다. 파일명: {imageName}");
            cutSceenImage = null;
        }
#endif
    }
}