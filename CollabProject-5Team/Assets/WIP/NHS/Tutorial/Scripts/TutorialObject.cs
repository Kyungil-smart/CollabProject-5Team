using UnityEngine;

public class TutorialObject : MonoBehaviour
{
    [SerializeField] private string _tutorialObjectId;

    private void Start()
    {
        RegisterTutorialObject();
    }

    private void OnEnable()
    {
        if (TutorialManager.Instance != null && !string.IsNullOrEmpty(_tutorialObjectId))
        {
            RegisterTutorialObject();
        }
    }

    private void RegisterTutorialObject()
    {
        if (string.IsNullOrEmpty(_tutorialObjectId) || TutorialManager.Instance == null)
            return;

        TutorialManager.Instance.RegisterObject(_tutorialObjectId, this.gameObject);

        Debug.Log($"[TutorialObject] : {_tutorialObjectId} 등록되었습니다. (Object: {gameObject.name})");
    }
}