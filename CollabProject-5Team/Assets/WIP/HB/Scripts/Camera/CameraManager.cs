using UnityEngine;
using UnityEngine.EventSystems;
using R3;
using R3.Triggers;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class CameraManager : MonoBehaviour
{
    [System.Serializable]
    public struct CameraBounds
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
    }

    [Header("조작 속도")]
    [SerializeField] private float _screenMoveSpeed = 1f;                  // 드래그 시 카메라 이동 속도
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

    [Header("두 손 터치용 변수")]
    private float _lastTouchDistance;                                         // 최근 터치 거리
    private bool _isZooming = false;                                          // 줌 상태 여부

    [Header("맵의 카메라 움직임 제한 영역")]
    [SerializeField] private float _diamondWidth = 15f;                       // 맵 중심에서 오른쪽 꼭지점까지의 길이
    [SerializeField] private float _diamondLength = 15f;                      // 맵 중심에서 위쪽 꼭지점까지의 길이

    public SerializableReactiveProperty<bool> IsUIOpen { get; private set; } = new SerializableReactiveProperty<bool>(false);

    private void Awake()
    {
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

        // Where 안에 조건을 걸어줌, UI창이 열리지 않고 UI를 터치 하지 않았을 때
        // 조건을 만족 시켰다면, Subscribe안에 있는 함수를 실행
        .Where(_=> !IsUIOpen.Value && !IsPointerOverUIObject())
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
        Vector3 currentTouchWorldPos = GetTouchWorldPosition(touch.position);

        switch (touch.phase)
        {
            // 손가락이 화면에 처음 닿았을 때
            case TouchPhase.Began:
                // 새로운 드래그가 시작되면 관성 트윈 멈춤
                if (_inertiaTweener != null && _inertiaTweener.IsActive())
                    {
                        _inertiaTweener.Kill();
                    }

                    _lastTouchPosition = currentTouchWorldPos;

                    // 속도 초기화
                    _drag = Vector3.zero;
                    break;
            
            // 손가락을 화면에 댄 채로 움직이는 상태
            case TouchPhase.Moved:
                // 현재 손 위치에서 처음 손을 댄 위치를 빼 변화량을 계산
                Vector3 direction = currentTouchWorldPos - _lastTouchPosition;
                Vector3 moveTarget = new Vector3(direction.x, 0, direction.z) * _screenMoveSpeed;

                // 손을 움직인 반대방향으로 카메라가 이동
                transform.position -= moveTarget;

                // 카메라가 얼마나 움직였는지
                _drag = moveTarget;
                break;

            // 손가락을 때는 순간, 하단 canceled로직 실행
            case TouchPhase.Ended:

            // 시스템에 의해 터치가 강제로 취소됨
            case TouchPhase.Canceled:
                // 손을 떼면  속도에 비례해 감속 후 정지
                if (_drag.magnitude > 0.01f)
                {
                    // 손을 뗄 때 속도 벡터 기반 목표 지점 계산
                    Vector3 targetPosition = transform.position - (_drag * 10f);

                    // DOTween으로 부드럽게 감속하면서 정지
                    _inertiaTweener = transform.DOMove(targetPosition, _inertiaDuration)
                        // 미끄러지듯 멈추기
                        .SetEase(Ease.OutCubic)
                        // 이동중에도 경계체크
                        .OnUpdate(ClampCameraPosition);
                }
                break;
        }

        ClampCameraPosition();
    }

    /// <summary>
    /// 두 손가락 핀치, 줌
    /// </summary>
    private void HandleMultiTouch()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

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

            // 거리를 계산한 값이 +면 줌인, -면 줌 아웃
            _cam.orthographicSize -= distance * _zoomSpeed;
            // 화면 최대,최소 배율은 넘지 않음
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, _minSize, _maxSize);

            _lastTouchDistance = currentTouchDistance;
        }

        // 하나라도 터치가 떨어지면 줌기능 off
        if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
        {
            _isZooming = false;
        }
    }

    /// <summary>
    /// 다이아몬드꼴맵 경계에 맞게 카메라 위치를 제한
    /// </summary>
    private void ClampCameraPosition()
    {
        Vector3 pos = transform.position;
        
        // X, Z를 더한 뒤 빼면 마름모꼴 바깥쪽 임의의 사각형의 가로, 세로를 구할 수 있음
        float sumXZ = pos.x + pos.z;
        float diffXZ = pos.x - pos.z;

        // 계산한 사각형의 꼭지점 거리안으로 카메라의 움직임을 제어할 벽을 만듦
        sumXZ = Mathf.Clamp(sumXZ, -_diamondWidth, _diamondWidth);
        diffXZ = Mathf.Clamp(diffXZ, -_diamondLength, _diamondLength);

        // 다시 3D월드 좌표로 역계산(더하고 뺀 값을 2로 나누면 원래 X,Z 좌표로 복구)
        pos.x = (sumXZ + diffXZ) * 0.5f;
        pos.z = (sumXZ - diffXZ) * 0.5f;

        // 카메라에 적용
        transform.position = pos;
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
