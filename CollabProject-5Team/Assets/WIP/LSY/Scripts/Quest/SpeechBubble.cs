using TMPro;
using UnityEngine;

// 퀘스트 성공 시 직원 머리 위에 런타임 생성되는 말풍선. Canvas_Quest 하위에 생성되며,
// 대상 직원 위치를 화면 좌표로 추적하다가 일정 시간 후 자동으로 사라진다.
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private const float DisplayDuration = 3f; // 표시 후 사라지기까지 시간(초)

    private RectTransform _rect;
    private Camera _cam;
    private Transform _target;
    private Vector3 _worldOffset;
    private float _timer;
    private bool _isPersistent;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cam  = Camera.main;
    }

    public void Show(Transform target, Vector3 worldOffset, string message, float displayDuration = DisplayDuration)
    {
        _target = target;
        _worldOffset = worldOffset;
        _timer = displayDuration;
        _isPersistent = displayDuration <= 0f;

        if (text != null) text.text = message;

        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();

        if (_isPersistent) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f) Destroy(gameObject);
    }

    private void UpdatePosition()
    {
        if (_target == null || _cam == null) return;

        _rect.position = _cam.WorldToScreenPoint(_target.position + _worldOffset);
    }
}
