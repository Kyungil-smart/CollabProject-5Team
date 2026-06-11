using TMPro;
using UnityEngine;

// 퀘스트 성공 시 직원 머리 위에 런타임 생성되는 말풍선. Canvas_Quest 하위에 생성되며,
// 대상 직원 위치를 화면 좌표로 추적하다가 일정 시간 후 자동으로 사라진다.
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private const float DisplayDuration = 3f; // 표시 후 사라지기까지 시간(초)

    private RectTransform _rect;
    private Transform _target;
    private Vector3 _worldOffset;
    private float _timer;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void Show(Transform target, Vector3 worldOffset, string message)
    {
        _target = target;
        _worldOffset = worldOffset;
        _timer = DisplayDuration;

        if (text != null) text.text = message;

        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();

        _timer -= Time.deltaTime;
        if (_timer <= 0f) Destroy(gameObject);
    }

    private void UpdatePosition()
    {
        if (_target == null || Camera.main == null) return;

        if (_rect == null) _rect = GetComponent<RectTransform>();

        _rect.position = Camera.main.WorldToScreenPoint(_target.position + _worldOffset);
    }
}