using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using System.Linq;


public class PlayerMove : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Camera _mainCamera;
    private Animator _anim;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask _interactableLayer;   // 상호작용 레이어
    [SerializeField] private LayerMask _groundLayer;         // 바닥 레이어

    [Header("상호작용 거리")]
    [SerializeField] private float _interactionDistance = 1f;  // 상호작용 거리

    private Collider _targetCollider = null;           // 타겟의 콜라이더

    private IInteractable _targetInteractable = null;   // 현재 목표로 타겟팅한 대상
    private bool _hasInteracted = false;               // 현재 상호작용 중인지

    private Vector2 _touchStartPos;                     // 터치 시작점
    private bool _isDraggingCamera = false;             // 터치 드래그 했는지
    private const float DragThreshold = 30f;            // 드래그했다고 간주하는 거리

    private bool _isMovingToPosition = false;

    private void Start()
    {
        GameManager.Instance.InjectPlayer(this);
        _mainCamera = Camera.main;

        _anim = GetComponent<Animator>();

        _agent = GetComponent<NavMeshAgent>();

        // 상호작용 오브젝트쪽으로 이동 후 멈출 때 여유거리
        _agent.stoppingDistance = _interactionDistance -0.2f;
    }

    private void Update()
    {
        UpdateAnimation();

        // UI창이 열려있다면 터치 이동로직을 무시
        if (IsPointerOverUI()) return;

        if(_isMovingToPosition)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _isMovingToPosition = false;
            }

            return;
        }

        bool IsUIOpen = CameraManager.Instance != null && CameraManager.Instance.IsUIOpen.Value;

        // UI창이 열려있으면 터치 관통 방지
        if (IsPointerOverUI()) return;

        // 유니티 에디터에서는 클릭으로 움직임 테스트
        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            // 터치가 시작되면 위치를 기억하고 아직 드래그가 아님
            _touchStartPos = Input.mousePosition;
            _isDraggingCamera = false;
        }

        if (Input.GetMouseButton(0))
        {
            // 터치 후 드래그를 DragThreshold보다 길게하면 드래그 한 것으로 간주하고 화면 이동
            if (Vector2.Distance(_touchStartPos, Input.mousePosition) > DragThreshold)
            {
                _isDraggingCamera = true;
            }
        }

        // 터치를 땠을 때 isDraggingCamera가 true면 이동x, false면 플레이어 이동
        if (Input.GetMouseButtonUp(0))
        {
            if (!_isDraggingCamera)
            {
                MoveToTarget(Input.mousePosition);
            }
        }

        // 안드로이드 빌드파일에선 손가락 터치고 움직임
        #else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // 두 손가락 줌 중일 때는 이동 로직 차단
            if (Input.touchCount >= 2)
            {
                _isDraggingCamera = true;
                return;
            }

            switch (touch.phase)
            {
                // 터치가 시작되면 위치를 기억하고 아직 드래그가 아님
                case TouchPhase.Began:
                    _touchStartPos = touch.position;
                    _isDraggingCamera = false;
                    break;

                // 터치 후 드래그를 DragThreshold보다 길게하면 드래그 한 것으로 간주하고 화면 이동
                case TouchPhase.Moved:
                    if (Vector2.Distance(_touchStartPos, touch.position) > DragThreshold)
                    {
                        _isDraggingCamera = true;
                    }
                    break;

                // 터치를 땠을 때 isDraggingCamera가 true면 이동x, false면 플레이어 이동
                case TouchPhase.Ended:
                    if (!_isDraggingCamera)
                    {
                        MoveToTarget(touch.position);
                    }

                    _isDraggingCamera = false;
                    break;
            }
        }
        
        #endif

        // 상호작용 대상을 터치했고, 상호작용 전이라면 거리체크
        if (_targetInteractable != null && !_hasInteracted && _targetCollider != null)
        {
            // 상호작용할 타겟의 콜라이더 표면중 Player와 가장 가까운 표면
            Vector3 closestPoint = _targetCollider.ClosestPoint(transform.position);

            // Player에서부터 가장 가까운 표면까지 직선거리
            float distance = Vector3.Distance(transform.position, closestPoint);

            if (distance <= _interactionDistance)
            {
                TriggerInteraction();
            }
        }
    }

    // 화면을 터치해서 레이캐스트를 쏴서 이동
    private void MoveToTarget(Vector2 screenPosition)
    {
        if (_hasInteracted)
        {
            ExitInteraction();
        }
        if (_agent != null && !_agent.enabled) _agent.enabled = true;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        // interactableLayer가 붙은 사물에 Ray쏨
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _interactableLayer))
        {
            // 터치한 오브젝트의 인터페이스를 가져옴
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                var point = interactable as IInteractablePoint;
                if(point != null && point.GetPointType() == PointType.Desk)
                {
                    Debug.Log("이곳은 책상이라 앉을 수 없습니다.");
                }

                else
                {
                    var actionPoint = PointManager.Instance.GetAllPoints()
                                        .FirstOrDefault(p => p.name == hit.collider.name 
                                        && Vector3.Distance(p.transform.position, hit.collider.transform.position) < 0.1f);

                    if (actionPoint != null && actionPoint.IsOccupied)
                    {
                        Debug.Log("이미 누군가 앉아있습니다.");
                        return; // 점유 중이면 이동하지 않고 종료
                    }

                    _targetInteractable = interactable;
                    _targetCollider = hit.collider;
                    _hasInteracted = false;

                    _agent.SetDestination(_targetInteractable.GetTransform().position);

                    return;
                }
            }
        }

        // 상호작용가능한 오브젝트를 터치한 게 아니면 바닥 레이어만 조준해서 쏨
        if (Physics.Raycast(ray, out hit, 100f, _groundLayer))
        {
            _targetInteractable = null;
            _targetCollider = null;
            _hasInteracted = false;
            
            _agent.ResetPath();

            // 일반 바닥 이동
            _agent.SetDestination(hit.point);
        }
    }

    // 외부(퀘스트 말풍선 버튼 등)에서 상호작용 대상을 지정 - 해당 위치로 이동 후 도착하면 자동으로 상호작용 실행
    public void SetInteractTarget(IInteractable target, Collider collider)
    {
        // 앉아있는 상태라면 먼저 일어나서 에이전트를 활성화함
        if (_hasInteracted)
        {
            ExitInteraction();
        }

        // 에이전트가 비활성화 상태라면 활성화
        if (_agent != null && !_agent.enabled)
        {
            _agent.enabled = true;
        }

        // 경로를 설정하기 전에 에이전트가 NavMesh 위에 있는지 확인
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            _targetInteractable = target;
            _targetCollider = collider;
            _hasInteracted = false;

            _agent.SetDestination(target.GetTransform().position);
        }
    }

    // 해당 좌표로 이동(업무 시작 시 자기 자리로 이동)
    public void MoveToPosition(Vector3 position)
    {
        _targetInteractable = null;
        _targetCollider = null;
        _hasInteracted = false;

        _isMovingToPosition = true;

        _agent.SetDestination(position);
    }

    private void TriggerInteraction()
    {
        _hasInteracted = true;
        _agent.ResetPath();
        _agent.enabled = false;

        var point = _targetInteractable as IInteractablePoint;
        if (point != null)
        {
            // 진짜 컴포넌트인지, 원본과 같은지 ID로 확인
            Debug.Log($"[플레이어] 점유할 포인트 이름: {((MonoBehaviour)point).name}, ID: {((MonoBehaviour)point).GetInstanceID()}");
        
            // 형변환
            var actionPoint = PointManager.Instance.GetAllPoints()
                                .FirstOrDefault(p => p.name == ((MonoBehaviour)point).name 
                                && Vector3.Distance(p.transform.position, ((MonoBehaviour)point).transform.position) < 0.1f);
            if(actionPoint != null) 
            {
                actionPoint.IsOccupied = true;
                Debug.Log($"[플레이어] {actionPoint.name}의 IsOccupied를 {actionPoint.IsOccupied}로 변경함");
            }
        
            Transform targetTransform = point.GetTransform();

            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;

            switch (point.GetPointType())
            {
                case PointType.Work:        _anim.SetBool("IsWorking", true); break;
                case PointType.Sofa:        _anim.SetBool("IsResting", true); 
                                            _anim.SetFloat("RestIndex", Random.Range(0,4)); break;
                case PointType.Drink:       _anim.SetTrigger("Drink"); Invoke(nameof(ExitInteraction), 6f); break;
                case PointType.CopyMachine: _anim.SetTrigger("Fax"); Invoke(nameof(ExitInteraction), 18f); break;
                case PointType.ServerRoom:  _anim.SetTrigger("PushButton"); Invoke(nameof(ExitInteraction), 4f); break;

            }
        }

        // 상호작용 실행
        _targetInteractable.OnInteract();
    }

    public void ExitInteraction()
    {
        var point = _targetInteractable as IInteractablePoint;
        if (point != null)
        {
            // PointManager 리스트를 통해 실제 객체 찾아 점유 해제
            var realPoint = PointManager.Instance.GetAllPoints().FirstOrDefault(p => p.name == ((MonoBehaviour)point).name && Vector3.Distance(p.transform.position, ((MonoBehaviour)point).transform.position) < 0.1f);
            if(realPoint != null) realPoint.IsOccupied = false;
        }

        _hasInteracted = false;

        _anim.SetBool("IsWorking", false);
        _anim.SetBool("IsResting", false);
        _anim.SetTrigger("Idle");

        NavMeshHit closestHit;
        if (NavMesh.SamplePosition(transform.position, out closestHit, 10.0f, NavMesh.AllAreas))
        {
            _agent.Warp(closestHit.position);
        }

        _agent.enabled = true;
        _agent.ResetPath();
    }

    public void CloseInteractionUI()
    {
        // UI창을 끌 때 상호작용상태 초기화
        _targetInteractable = null;
        _hasInteracted = false;
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

    // NavMeshAgent의 속도를 애니메이터에 전달
    private void UpdateAnimation()
    {
        if (_agent != null && _anim != null)
        {
            // 정지 상태면 0에 가깝고, 최고 속도로 달리면 agent.speed 값
            float currentSpeed = _agent.velocity.magnitude;

            // 애니메이터 파라미터의 "Speed"에 속도를 전달
            _anim.SetFloat("Speed", currentSpeed);
        }
    }

    public void WorkCompleteAnim()
    {
        if(_anim != null)
        {
            _anim.SetTrigger("IsWorkDone");
        }
    }

    public void ResetMovementState()
    {
        _isMovingToPosition = false;
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }
    }
}
