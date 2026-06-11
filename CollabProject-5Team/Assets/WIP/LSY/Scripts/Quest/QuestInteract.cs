using UnityEngine;
using UnityEngine.EventSystems;

// 퀘스트 탭에서 노출되는 상호작용 아이콘(별). 컨트롤 타입(TAP/HOLD/SWIPE)에 맞게 입력을 처리해 QuestObject를 완료시킨다.
public class QuestInteract : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler
{
    [SerializeField] private QuestObject questObject;
    [SerializeField] private RectTransform progressGauge; // HOLD 진행 게이지(옵션)
    [SerializeField] private RectTransform icon;          // HOLD 중 살짝 작아질 아이콘(옵션)

    private const float SwipeDistance = 100f; // 스와이프로 인정할 최소 드래그 거리(px)
    private const float HoldIconScale = 0.9f; // HOLD 중 아이콘 축소 비율

    private float _holdTime;
    private bool _isHolding;
    private Vector2 _dragStartPos;
    private Vector3 _iconOriginalScale;

    private void Awake()
    {
        if (icon != null) _iconOriginalScale = icon.localScale;
    }

    private void Update()
    {
        if (!_isHolding) return;

        DailyQuest quest = QuestManager.Instance.curDailyQuest;
        if (quest.so.controlType != ControlType.HOLD) return;

        _holdTime += Time.deltaTime;

        if (progressGauge != null)
            progressGauge.localScale = new Vector3(Mathf.Clamp01(_holdTime / quest.so.targetCount), 1f, 1f);

        if (_holdTime >= quest.so.targetCount)
        {
            _isHolding = false;
            questObject.CompleteInteraction();
        }
    }

    // TAP: 클릭 한 번으로 완료
    public void OnPointerClick(PointerEventData eventData)
    {
        if (QuestManager.Instance.curDailyQuest.so.controlType == ControlType.TAP)
            questObject.CompleteInteraction();
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
            questObject.CompleteInteraction();
    }
}