using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;


public class PlayerMove : MonoBehaviour
{
    private NavMeshAgent agent;
    private Camera mainCamera;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask interactableLayer;   // 상호작용 레이어
    [SerializeField] private LayerMask groundLayer;         // 바닥 레이어

    [Header("상호작용 거리")]
    [SerializeField] private float interactionDistance = 1f;  // 상호작용 거리

    private Collider targetCollider = null;           // 타겟의 콜라이더

    private IIteractable targetInteractable = null;   // 현재 목표로 타겟팅한 대상
    private bool hasInteracted = false;               // 현재 상호작용 중인지

    private void Start()
    {
        mainCamera = Camera.main;

        agent = GetComponent<NavMeshAgent>();

        // 상호작용 오브젝트쪽으로 이동 후 멈출 때 여유거리
        agent.stoppingDistance = interactionDistance -0.2f;
    }

    private void Update()
    {
        // UI창이 열려있다면 터치 이동로직을 무시
        if (hasInteracted) return;

        // UI창이 열려있으면 터치 관통 방지
        if (IsPointerOverUI()) return;

        // 유니티 에디터에서는 클릭으로 움직임 테스트
        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            MoveToTarget(Input.mousePosition);
        }

        // 안드로이드 빌드파일에선 손가락 터치고 움직임
        #else
        if(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began){
            MoveToTarget(Input.GetTouch(0).position);
        }
        #endif

        // 상호작용 대상을 터치했고, 상호작용 전이라면 거리체크
        if (targetInteractable != null && !hasInteracted && targetCollider != null)
        {
            // 상호작용할 타겟의 콜라이더 표면중 Player와 가장 가까운 표면
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);

            // Player에서부터 가장 가까운 표면까지 직선거리
            float distance = Vector3.Distance(transform.position, closestPoint);

            if (distance <= interactionDistance)
            {
                TriggerInteraction();
            }
        }
    }

    private void MoveToTarget(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // interactableLayer가 붙은 사물에 Ray쏨
        if (Physics.Raycast(ray, out hit, 100f, interactableLayer))
        {
            // 터치한 오브젝트의 인터페이스를 가져옴
            IIteractable interactable = hit.collider.GetComponent<IIteractable>();

            if (interactable != null)
            {
                targetInteractable = interactable;
                targetCollider = hit.collider;
                hasInteracted = false;

                agent.SetDestination(targetInteractable.GetTransform().position);

                return;
            }
        }

        // 상호작용가능한 오브젝트를 터치한 게 아니면 바닥 레이어만 조준해서 쏨
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            targetInteractable = null;
            targetCollider = null;
            hasInteracted = false;

            // 일반 바닥 이동
            agent.SetDestination(hit.point);
        }
    }

    private void TriggerInteraction()
    {
        hasInteracted = true;
        agent.ResetPath();

        // 대상 바라보기
        Vector3 targetPos = targetInteractable.GetTransform().position;
        Vector3 lookDirection = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        transform.LookAt(lookDirection);

        // 상호작용 실행
        targetInteractable.OnInteract();
    }

    public void CloseInteractionUI()
    {
        // UI창을 끌 때 상호작용상태 초기화
        targetInteractable = null;
        hasInteracted = false;
    }

    private bool IsPointerOverUI()
    {
        // 에디터 환경일 때 체크
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // 모바일 환경일 때 체크
        if (Input.touchCount > 0)
        {
            if(EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return true;
        }

        return false;
    }
}
