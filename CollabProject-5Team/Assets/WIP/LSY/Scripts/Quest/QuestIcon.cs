using UnityEngine;
using UnityEngine.UI;

// 전구 아이콘 프리팹. Canvas_Quest 하위에 런타임 생성되며, 대상 오브젝트 위치를 화면 좌표로 추적한다.
// 화면 밖에 있을 때는 가장자리에 클램프되어 표시된다.
public class QuestIcon : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private float edgePadding = 60f;

    private RectTransform _rect;
    private Canvas _canvas;
    private Transform _target;
    private Vector3 _worldOffset;
    private bool _isOffscreen;

    public Button Button => button;

    private void Awake()
    {
        _rect   = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (_isOffscreen && _target != null)
            CameraManager.Instance?.FocusOnTarget(_target.position + _worldOffset, Camera.main.orthographicSize);
    }

    public void SetTarget(Transform target, Vector3 worldOffset)
    {
        _target = target;
        _worldOffset = worldOffset;
        UpdatePosition();
    }

    private void LateUpdate() => UpdatePosition();

    private void UpdatePosition()
    {
        if (_target == null || Camera.main == null) return;
        if (_rect == null) _rect = GetComponent<RectTransform>();

        Vector3 worldPos  = _target.position + _worldOffset;
        Vector3 screenPos = Camera.main.WorldToViewportPoint(worldPos);

        _isOffscreen = screenPos.z < 0f
            || screenPos.x < 0f || screenPos.x > 1f
            || screenPos.y < 0f || screenPos.y > 1f;

        if (!_isOffscreen)
        {
            _rect.position = Camera.main.WorldToScreenPoint(worldPos);
            return;
        }

        if (screenPos.z < 0f)
            screenPos = -screenPos;

        // 캔버스 크기 기준으로 가장자리 클램프
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        float halfW = Screen.width  * 0.5f - edgePadding * scaleFactor;
        float halfH = Screen.height * 0.5f - edgePadding * scaleFactor;

        Vector2 dir = new Vector2(screenPos.x - 0.5f, screenPos.y - 0.5f).normalized;
        float   scaleX = Mathf.Abs(dir.x) > 0.001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue;
        float   scaleY = Mathf.Abs(dir.y) > 0.001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue;
        float   scale  = Mathf.Min(scaleX, scaleY);

        Vector2 clampedScreen = new Vector2(Screen.width * 0.5f + dir.x * scale,
                                            Screen.height * 0.5f + dir.y * scale);
        _rect.position = clampedScreen;
    }
}