using UnityEngine;

public class TutorialObject : MonoBehaviour
{
    [SerializeField] private string _tutorialObjectId;

    void Start()
    {
        TutorialManager.Instance.RegisterObject(_tutorialObjectId, this.gameObject);
    }
}
