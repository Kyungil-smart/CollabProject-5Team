using UnityEngine;
using UnityEngine.EventSystems;
using R3;
using R3.Triggers;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class CameraManager : MonoBehaviour
{
    [Header("조작 속도")]
    [SerializeField] private float _screenMoveSpeed = 1f;                    // 드래그 시 카메라 이동 속도
    [SerializeField] private float _zoomSpeed = 0.05f;                       // 줌 속도
    [SerializeField] private float _inertiaDuration = 0.5f;                  // 관성 지속 시간

    [Header("줌 범위")]
    [SerializeField] private float _defaultSize = 13f;                        // 기본 줌
    [SerializeField] private float _minSize = 5f;                            // 최대 줌
    [SerializeField] private float _maxSize = 13f;                           // 최소 줌

    private Camera _cam;
    private Tweener _inertiaTweener;                                         // 관성 이동 제어
    private CompositeDisposable _disposable = new CompositeDisposable();     // R3 구독 해제용

    [Header("한 손 터치용 변수")]
    private Vector3 _lastTouchPosition;                                       // 최근 터치 지점
    private Vector3 _drag;                                                    // 드래그
    private bool _isCameraDragValid;                                          // 카메라를 움직이기에 유효한 터치인지

    [Header("두 손 터치용 변수")]
    private float _lastTouchDistance;                                         // 최근 터치 거리
    private bool _isZooming = false;                                          // 줌 상태 여부

    [Header("맵의 카메라 움직임 제한 영역")]
    [SerializeField] private float _diamondWidth = 15f;                       // 맵 중심에서 오른쪽 꼭지점까지의 길이
    [SerializeField] private float _diamondLength = 15f;                      // 맵 중심에서 위쪽 꼭지점까지의 길이

    public SerializableReactiveProperty<bool> IsUIOpen { get; private set; } = new SerializableReactiveProperty<bool>(false);

    public static CameraManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        
        _cam = GetComponent<Camera>();
        if (_cam.orthographic)
        {
            _cam.orthographicSize = _defaultSize;
        }
    }

    private void Start()
    {
        // 매 프레임 검사
        this.UpdateAsObservable()

        // Where 안에 조건을 걸어줌, UI창이 열려있지 않다면
        // 조건을 만족 시켰다면, Subscribe안에 있는 함수를 실행
        .Where(_=> !IsUIOpen.Value)
        .Subscribe(_ =>
        {
            HandleTouchInput();
        })

        // 오브젝트 파괴 시 구독 해제
        .AddTo(_disposable);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지
        _disposable.Dispose();        
    }

    /// <summary>
    ///  R3의 입력을 받아 터치 개수별로 분기 처리
    /// </summary>
    private void HandleTouchInput()
    {
        // 유니티 에디터용 마우스 휠 줌 처리
        float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(mouseScroll) > 0.01f)
        {
            _cam.orthographicSize -= mouseScroll * (_zoomSpeed * 20f);
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, _minSize, _maxSize);
            
            transform.position = GetClampedCameraPosition(transform.position);
            return;
        }

        // 모바일에서 터치 로직
        int touchCount = Input.touchCount;

        if (touchCount == 0) return;

        // 3개 이상의 터치는 2개로 간주
        if (touchCount >= 3) touchCount = 2;

        if (touchCount == 1)
        {
            HandleSingleTouch();
        }

        else if (touchCount == 2)
        {
            HandleMultiTouch();
        }
    }

    
    /// <summary>
    /// 한 손 드래그, 화면 이동
    /// </summary>
    private void HandleSingleTouch()
    {
        Touch touch = Input.GetTouch(0);

        // 새로운 드래그가 시작되면 관성 트윈 멈춤
        if (touch.phase == TouchPhase.Began)
        {
            if (_inertiaTweener != null && _inertiaTweener.IsActive())
            {
                _inertiaTweener.Kill();
            }
        }

        switch (touch.phase)
        {
            // 손가락이 화면에 처음 닿았을 때
            case TouchPhase.Began:
                // UI창 위 터치는 무효
                if (IsPointerOverUIObject())
                {
                    _isCameraDragValid = false;
                    return;
                }

                // UI창을 안 눌렀을 때만 카메라 이동
                _isCameraDragValid = true;        
                _lastTouchPosition = GetTouchWorldPosition(touch.position);
                // 속도 초기화
                _drag = Vector3.zero;
                break;
            
            // 손가락을 화면에 댄 채로 움직이는 상태
            case TouchPhase.Moved:
                // 무효된 터치라면 드래그 연산하지 않음
                if (!_isCameraDragValid) return;

                Vector3 currentTouchWorldPos = GetTouchWorldPosition(touch.position);
                // 현재 손 위치에서 처음 손을 댄 위치를 빼 변화량을 계산
                Vector3 direction = currentTouchWorldPos - _lastTouchPosition;

                // x,z축 움직임을 계산
                Vector3 moveTarget = new Vector3(direction.x, 0, direction.z) * _screenMoveSpeed;

                // 손을 움직인 반대방향으로 카메라가 이동
                Vector3 rawTargetPos = transform.position - moveTarget;
                transform.position = GetClampedCameraPosition(rawTargetPos);

                // 카메라가 얼마나 움직였는지
                _drag = moveTarget;

                // 초기화
                _lastTouchPosition = GetTouchWorldPosition(touch.position);
                break;

            // 손가락을 때는 순간, 하단 canceled로직 실행
            case TouchPhase.Ended:
            // 시스템에 의해 터치가 강제로 취소됐을 때
            case TouchPhase.Canceled:
                // 손을 떼면  속도에 비례해 감속 후 정지
                if (_drag.magnitude > 0.01f)
                {
                    // 손을 뗄 때 속도 벡터 기반 목표 지점 계산
                    Vector3 targetPosition = transform.position - (_drag * 10f);

                    // 카메라가 도착할 목적지가 맵바깥으로 나가지 않게
                    Vector3 clampedTargetPos = GetClampedCameraPosition(targetPosition);

                    // DOTween으로 부드럽게 감속하면서 정지
                    _inertiaTweener = transform.DOMove(clampedTargetPos, _inertiaDuration)
                        // 미끄러지듯 멈추기
                        .SetEase(Ease.OutCubic);
                }

                // 터치 종료 시 초기화
                _isCameraDragValid = false;
                break;
        }
    }

    /// <summary>
    /// 두 손가락 핀치, 줌
    /// </summary>
    private void HandleMultiTouch()
    {
        _isCameraDragValid = false;

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // 두 손가락의 중심점 계산
        Vector2 midPoint = (touch0.position + touch1.position) * 0.5f;

        // 한 점 터치 중 추가로 한 점이 터치되면 두 점으로 간주
        if (touch1.phase == TouchPhase.Began || !_isZooming)
        {
            _lastTouchDistance = Vector2.Distance(touch0.position, touch1.position);
            _isZooming = true;
            return;
        }

        if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            // 두 손가락의 거리를 계산해서 처음보다 멀어졌다면 +, 가까워 졌다면 -
            float currentTouchDistance = Vector2.Distance(touch0.position, touch1.position);
            float distance = currentTouchDistance - _lastTouchDistance;

            float zoomAmount = distance * _zoomSpeed;
            ZoomToLocation(midPoint, zoomAmount);

            _lastTouchDistance = currentTouchDistance;
        }

        // 하나라도 터치가 떨어지면 줌기능 off
        if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
        {
            _isZooming = false;
        }
    }

    // 터치한 지점을 중심으로 줌인/아웃
    private void ZoomToLocation(Vector2 screenPos, float zoomAmount)
    {
        // 줌 하기 전 월드 좌표 기억
        Vector3 beforeZoomWorldPos = GetTouchWorldPosition(screenPos);

        // zoomAmount 값이 -면 줌아웃, +면 줌인
        _cam.orthographicSize -= zoomAmount;
        // 화면 최대, 최소 배율 넘지 않음
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, _minSize, _maxSize);

        // 줌을 한 뒤 똑같은 화면 좌표의 새로운 월드 좌표
        Vector3 afterZoomWorldPos = GetTouchWorldPosition(screenPos);

        // 줌 하기 전 후 차이값 만큼 카메라 위치 조정
        Vector3 diff = beforeZoomWorldPos - afterZoomWorldPos;

        Vector3 rawTargetPos = transform.position + new Vector3(diff.x, 0, diff.z);

        transform.position = GetClampedCameraPosition(rawTargetPos);
    }

    /// <summary>
    /// 다이아몬드꼴맵 경계에 맞게 카메라 위치를 제한
    /// </summary>
    private Vector3 GetClampedCameraPosition(Vector3 targetPos)
    {
        // 카메라가 이동할 가상 목적지에서 화면 중앙으로 레이저를 쏴봄
        Ray ray = _cam.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Vector3 rayOffset = targetPos - transform.position;
        ray.origin += rayOffset;

        // 가상의 바닥과 레이저가 만나는 좌표를 쏨
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Vector3 currentLookAtPos = Vector3.zero;

        // 바닥에 레이저를 쏴서 좌표를 받아오고 그 곳을 바라봄
        if (groundPlane.Raycast(ray, out float enter))
        {
            currentLookAtPos = ray.GetPoint(enter);
        }

        // 비스듬한 카메라 각돋 때문에 발생하는 카메라와 바닥 중심점 사이의 간격을 구함
        // 나중에 보정된 바닥 좌표에 이 간격을 더해 카메라 위치를 잡아줌
        Vector3 cameraToGroundOffset = targetPos - currentLookAtPos;

        // 현재 카메라가 비추는 화면의 가로/세로 월드 크기 계산
        float camHeight = _cam.orthographicSize;
        float camWidth = camHeight * _cam.aspect;

        // 사각형인 카메라 화면을 45도 회전된 마름모꼴 맵 경계에 맞추기 위한 작업
        // 맵이 정방형 마름모꼴이기 떄문에 45도 직각삼각형의 대각선 비율인 sin(45도) = 약 0.7을 곱함
        // 마름모 결계선과 맞닿는 카메라의 실제 대각선방향을 계산
        float scaleX = camWidth * 0.7f;
        float scaleZ = camHeight * 0.7f;

        // Mathf.Max로 두 수를 비교해서 큰 값을 반환(음수 값을 차단하고 최하 한계선을 0으로 설정)
        // (맵의 꼭지점 - 카메라의 크기)를 빼서 카메라의 확대/축소 배율에 따라 이동할 수 있는 범위가 달라짐
        float clampWidth = Mathf.Max(0, _diamondWidth - scaleX);
        float clampHeight = Mathf.Max(0, _diamondLength - scaleZ);
        
        // X, Z를 더한 뒤 빼면 마름모꼴 바깥쪽 임의의 사각형의 가로, 세로를 구할 수 있음
        float sumXZ = currentLookAtPos.x + currentLookAtPos.z;
        float diffXZ = currentLookAtPos.x - currentLookAtPos.z;

        // 계산한 사각형의 꼭지점 거리안으로 카메라의 움직임을 제어할 벽을 만듦
        sumXZ = Mathf.Clamp(sumXZ, -clampWidth, clampWidth);
        diffXZ = Mathf.Clamp(diffXZ, -clampHeight, clampHeight);

        // 다시 3D월드 좌표로 역계산(더하고 뺀 값을 2로 나누면 원래 X,Z 좌표로 복구)
        currentLookAtPos.x = (sumXZ + diffXZ) * 0.5f;
        currentLookAtPos.z = (sumXZ - diffXZ) * 0.5f;

        // 목표지점에 위에 계산해둔 거리를 더해 카메라를 움직여 줌
        return currentLookAtPos + cameraToGroundOffset;
    }

    /// <summary>
    /// 화면 좌표를 월드 좌표로 변환
    /// </summary>
    private Vector3 GetTouchWorldPosition(Vector2 screenPos)
    {
        // 터치한 부분으로 레이저를 쏨
        Ray ray = _cam.ScreenPointToRay(screenPos);

        // 가상의 바닥을 만들어 Ray와 접촉하게 함
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Ray와 가상의 바닥이 만나는 지점의 3D좌표를 반환
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        // 만나는 지점이 없다면 0,0,0 반환
        return Vector3.zero;
    }

    /// <summary>
    /// UI 레이캐스트 차단 검사
    /// </summary>
    private bool IsPointerOverUIObject()
    {
        // 이벤트 시스템이 안 켜져 있다면 UI안 누른 걸로 안전장치
        if (EventSystem.current == null) return false;

        // 여러개가 동시에 터치됐을 경우 첫 번째 누른 것을 터치한 것으로 인정함
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }

        // 유니티 에디터 환경에선 마우스 커서 위치를 기준으로 체크
        return EventSystem.current.IsPointerOverGameObject();
    }
}
