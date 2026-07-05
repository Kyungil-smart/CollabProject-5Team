using UnityEngine;

public class TutorialObject : MonoBehaviour
{
    [SerializeField] private string _tutorialObjectId;

    private void Start()
    {
        RegisterTutorialObject();
        Debug.Log($"[TutorialObject: Start] : {_tutorialObjectId} 등록되었습니다. (Object: {gameObject.name})");
    }

    private void OnEnable()
    {
        if (TutorialManager.Instance != null && !string.IsNullOrEmpty(_tutorialObjectId))
        {
            RegisterTutorialObject();
            Debug.Log($"[TutorialObject : OnEnable] : {_tutorialObjectId} 등록되었습니다. (Object: {gameObject.name})");
        }
    }

    private void RegisterTutorialObject()
    {
        if (string.IsNullOrEmpty(_tutorialObjectId) || TutorialManager.Instance == null)
            return;

        TutorialManager.Instance.RegisterObject(_tutorialObjectId, this.gameObject);
    }

    public Vector3 GetWorldPosition()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[0] + corners[2]) / 2f;
        }

        return transform.position;
    }
}