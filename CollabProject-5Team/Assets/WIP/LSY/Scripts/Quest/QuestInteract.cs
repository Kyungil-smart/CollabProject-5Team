using UnityEngine;
using UnityEngine.EventSystems;

// 별 아이콘 프리팹. Canvas_Quest 하위에 런타임 생성되며, 대상 오브젝트 위치를 화면 좌표로 추적한다.
// 컨트롤 타입(TAP/HOLD/SWIPE)에 맞게 입력을 처리해 QuestObject를 완료시킨다.
public class QuestInteract : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler
{
    [SerializeField] private RectTransform progressGauge; // HOLD 진행 게이지(옵션)
    [SerializeField] private RectTransform icon;          // HOLD 중 살짝 작아질 아이콘(옵션)

    private const float SwipeDistance = 100f; // 스와이프로 인정할 최소 드래그 거리(px)
    private const float HoldIconScale = 0.9f; // HOLD 중 아이콘 축소 비율

    private RectTransform _rect;
    private QuestObject _questObject;
    private Transform _target;
    private Vector3 _worldOffset;

    private int _tapCount;
    private float _holdTime;
    private bool _isHolding;
    private Vector2 _dragStartPos;
    private Vector3 _iconOriginalScale;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (icon != null) _iconOriginalScale = icon.localScale;
    }

    public void SetTarget(Transform target, Vector3 worldOffset)
    {
        _target = target;
        _worldOffset = worldOffset;
        UpdatePosition();
    }

    // QuestObject가 인스턴스 생성 직후 호출해 자기 자신을 등록
    public void SetQuestObject(QuestObject questObject)
    {
        _questObject = questObject;

        // HOLD가 아니면 게이지는 숨김
        if (progressGauge != null)
            progressGauge.gameObject.SetActive(QuestManager.Instance.curDailyQuest.so.controlType == ControlType.HOLD);
    }

    private void LateUpdate()
    {
        UpdatePosition();

        if (!_isHolding) return;

        DailyQuest quest = QuestManager.Instance.curDailyQuest;
        if (quest.so.controlType != ControlType.HOLD) return;

        _holdTime += Time.deltaTime;

        if (progressGauge != null)
            progressGauge.localScale = new Vector3(Mathf.Clamp01(_holdTime / quest.so.targetCount), 1f, 1f);

        if (_holdTime >= quest.so.targetCount)
        {
            _isHolding = false;
            _questObject.CompleteInteraction();
        }
    }

    private void UpdatePosition()
    {
        if (_target == null || Camera.main == null) return;

        if (_rect == null) _rect = GetComponent<RectTransform>();

        _rect.position = Camera.main.WorldToScreenPoint(_target.position + _worldOffset);
    }

    // TAP: 활성 오브젝트가 여러 개면 1탭으로 완료, 1개뿐이면 targetCount번 연타해야 완료
    public void OnPointerClick(PointerEventData eventData)
    {
        DailyQuest quest = QuestManager.Instance.curDailyQuest;
        if (quest.so.controlType != ControlType.TAP) return;

        int tapsNeeded = quest.so.ActiveObjectCount > 1 ? 1 : quest.so.targetCount;

        _tapCount++;
        if (_tapCount >= tapsNeeded)
            _questObject.CompleteInteraction();
    }

    // HOLD: 누르고 있는 동안 게이지 증가 (뗐던 지점부터 이어서 재개)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (QuestManager.Instance.curDailyQuest.so.controlType != ControlType.HOLD) return;

        _isHolding = true;

        if (icon != null)
            icon.localScale = _iconOriginalScale * HoldIconScale;
    }

    // HOLD: 떼면 그 시점에서 멈춤 (진행도 유지)
    public void OnPointerUp(PointerEventData eventData)
    {
        if (QuestManager.Instance.curDailyQuest.so.controlType != ControlType.HOLD) return;

        _isHolding = false;

        if (icon != null)
            icon.localScale = _iconOriginalScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragStartPos = eventData.position;
    }

    // SWIPE: 일정 거리 이상 드래그하면 완료
    public void OnDrag(PointerEventData eventData)
    {
        if (QuestManager.Instance.curDailyQuest.so.controlType != ControlType.SWIPE) return;

        if (Vector2.Distance(_dragStartPos, eventData.position) >= SwipeDistance)
            _questObject.CompleteInteraction();
    }
}