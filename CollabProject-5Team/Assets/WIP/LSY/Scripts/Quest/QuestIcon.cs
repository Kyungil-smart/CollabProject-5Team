using UnityEngine;
using UnityEngine.UI;

// 전구 아이콘 프리팹. Canvas_Quest 하위에 런타임 생성되며, 대상 오브젝트 위치를 화면 좌표로 추적한다.
public class QuestIcon : MonoBehaviour
{
    [SerializeField] private Button button;

    private RectTransform _rect;
    private Transform _target;
    private Vector3 _worldOffset;

    public Button Button => button;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
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

        _rect.position = Camera.main.WorldToScreenPoint(_target.position + _worldOffset);
    }
}