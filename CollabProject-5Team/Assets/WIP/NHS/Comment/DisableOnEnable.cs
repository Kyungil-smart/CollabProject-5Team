using UnityEngine;

public class DisableOnEnable : MonoBehaviour
{
    [SerializeField] private GameObject _targetObject;

    private void OnEnable()
    {
        if (_targetObject != null)
        {
            _targetObject.SetActive(false);
        }
    }
}