using UnityEngine;

public class DeskInteract : MonoBehaviour
{
    [Header("업무 UI창")]
    [SerializeField] private GameObject _interactionUI;

    /*
    [Header("플레이어 레이어 설정")]
    [SerializeField] private LayerMask _playerLayer;
    */

    [SerializeField] private Transform _workPosition;

    private void Start()
    {
        if (_interactionUI != null)
        {
            _interactionUI.SetActive(false);
        }
    }

    public void OnClickWorkButton()
    {
        if (GameManager.Instance.player != null)
        {
            GameManager.Instance.player.MoveToPosition(_workPosition.position);
        }
    }

    /*
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 레이어인지 비트 연산으로 체크
        if (((1 << other.gameObject.layer) & _playerLayer) != 0)
        {
            // UI 열기
            if (_interactionUI != null) _interactionUI.SetActive(true);

            // 카메라 조작 멈춤
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.IsUIOpen.Value = true;
            }

            if (DateTimeTestUI.Instance != null)
            {
                // 업무 시작 버튼 활성화
                DateTimeTestUI.Instance.SetWorkStartButtonInteractable(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & _playerLayer) != 0)
        {
            // UI 닫기
            if (_interactionUI != null) _interactionUI.SetActive(false);
            
            // 카메라 조작 재개
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.IsUIOpen.Value = false;
            }

            if (DateTimeTestUI.Instance != null)
            {
                // 책상을 벗어나면 업무 시작 버튼 잠금
                DateTimeTestUI.Instance.SetWorkStartButtonInteractable(false);
            }
        }
    }
    */
}
