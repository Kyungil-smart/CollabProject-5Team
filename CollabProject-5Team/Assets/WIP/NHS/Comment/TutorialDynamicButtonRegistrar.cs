using UnityEngine;

public class TutorialDynamicButtonRegistrar : MonoBehaviour
{
    [Header("튜토리얼 매니저와 매칭할 고유 ID 명칭")]
    [SerializeField] private string _tutorialButtonId;

    private void Start()
    {
        if (TutorialManager.Instance != null && !string.IsNullOrEmpty(_tutorialButtonId))
        {
            TutorialManager.Instance.RegisterDynamicButton(_tutorialButtonId, this.gameObject);
        }
    }
}