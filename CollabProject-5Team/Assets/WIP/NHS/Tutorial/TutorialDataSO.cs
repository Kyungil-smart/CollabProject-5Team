using UnityEngine;
using static TutorialManager;

[CreateAssetMenu(fileName = "TutorialData", menuName = "Scriptable Objects/TutorialData")]

public class TutorialDataSO : ScriptableObject
{
    public string tutorialObjectId;

    public ShowMode         showMode;
    public GameObject activateObject;
    public string       tutorialText;
}
