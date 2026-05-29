using UnityEngine;

public class DeskInteract : MonoBehaviour
{
    [Header("업무 UI창")]
    [SerializeField] private GameObject interactionUI;

    [Header("플레이어 레이어 설정")]
    [SerializeField] private LayerMask playerLayer;

    private void Start()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 레이어인지 비트 연산으로 체크
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            if (DateTimeTestUI.Instance != null)
            {
                // 업무 시작 버튼 활성화
                DateTimeTestUI.Instance.SetWorkStartButtonInteractable(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            if (DateTimeTestUI.Instance != null)
            {
                // 책상을 벗어나면 업무 시작 버튼 잠금
                DateTimeTestUI.Instance.SetWorkStartButtonInteractable(false);
            }

            // 업무 창을 켠 상태로 이동하면 창을 닫음
            if (interactionUI != null && interactionUI.activeSelf)
            {
                interactionUI.SetActive(false);
            }
        }
    }
}
