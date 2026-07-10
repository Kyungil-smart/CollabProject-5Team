using UnityEngine;
using UnityEngine.UI;

// 업무 시작 아이콘 프리팹. Canvas_HUD 하위에 런타임 생성되며, 모니터 월드 위치를 화면 좌표로 추적한다.
public class WorkStartIcon : MonoBehaviour
{
    [SerializeField] private Button _button;

    private RectTransform _rect;
    private Camera _cam;
    private Transform _target;
    private Vector3 _worldOffset;

    public Button Button => _button;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cam  = Camera.main;
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
        if (_target == null || _cam == null) return;
        _rect.position = _cam.WorldToScreenPoint(_target.position + _worldOffset);
    }
}