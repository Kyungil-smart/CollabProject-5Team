using UnityEngine;

public class NPCInteract : MonoBehaviour, IIteractable
{
    [Header("띄울 UI창")]
    [SerializeField] private GameObject interactionUI;


    public void OnInteract()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(true);
        }
    }
    public Transform GetTransform()
    {
        // 플레이어가 다가올 내 위치 제공
        return transform;
    }
}
