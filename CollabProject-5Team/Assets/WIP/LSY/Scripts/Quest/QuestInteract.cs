using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 별 아이콘 프리팹. Canvas_Quest 하위에 런타임 생성되며, 대상 오브젝트 위치를 화면 좌표로 추적한다.
// 컨트롤 타입(TAP/HOLD/SWIPE)에 맞게 입력을 처리해 QuestObject를 완료시킨다.
public class QuestInteract : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Slider progressGauge; // HOLD 진행 게이지 (실린더 Slider, 옵션)
    [SerializeField] private RectTransform icon;          // HOLD 중 살짝 작아질 아이콘(옵션)
    [SerializeField] private Image iconImage;             // icon의 스프라이트 교체용 (옵션)

    [Header("퀘스트가 필요한 탭 횟수별 스프라이트 (옵션, 순서대로 1탭/2탭/...)")]
    [SerializeField] private Sprite[] tapSprites;

    [Header("퀘스트가 HOLD일 때 스프라이트 (옵션)")]
    [SerializeField] private Sprite holdSprite;

    private const float HoldIconScale = 0.9f; // HOLD 중 아이콘 축소 비율

    private RectTransform _rect;
    private QuestObject _questObject;
    private Transform _target;
    private Vector3 _worldOffset;

    private int _tapCount;
    private float _holdTime;
    private bool _isHolding;

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

        if (QuestManager.Instance?.curDailyQuest == null) return;

        ControlType type = QuestManager.Instance.EffectiveControlType;

        if (progressGauge != null)
            progressGauge.gameObject.SetActive(type == ControlType.HOLD);

        if (iconImage == null) return;

        if (type == ControlType.HOLD && holdSprite != null)
        {
            iconImage.sprite = holdSprite;
        }
        else if (type == ControlType.TAP && tapSprites != null && tapSprites.Length > 0)
        {
            iconImage.sprite = tapSprites[0]; // 탭 0회 상태
        }
    }

    private void LateUpdate()
    {
        UpdatePosition();

        if (!_isHolding) return;

        if (QuestManager.Instance?.curDailyQuest == null) return;
        if (QuestManager.Instance.EffectiveControlType != ControlType.HOLD) return;

        _holdTime += Time.deltaTime;

        int effectiveTarget = QuestManager.Instance.EffectiveTargetCount;
        if (progressGauge != null)
            progressGauge.value = Mathf.Clamp01(_holdTime / effectiveTarget);

        QuestManager.Instance.SetDisplayProgress(Mathf.FloorToInt(_holdTime));

        if (_holdTime >= effectiveTarget)
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
        if (TutorialManager.Instance != null)
        {
            TutorialManager.OnSomewhereTutorialCompleted?.Invoke();
            TutorialManager.Instance.CompletePunchHoleStep();
        }

        DailyQuest quest = QuestManager.Instance.curDailyQuest;
        if (quest == null || QuestManager.Instance.EffectiveControlType != ControlType.TAP) return;

        int tapsNeeded = quest.so.ActiveObjectCount > 1 ? 1 : QuestManager.Instance.EffectiveTargetCount;

        _tapCount++;

        // 퀘스트 배너 진행도 실시간 갱신
        QuestManager.Instance.SetDisplayProgress(_tapCount);

        if (iconImage != null && tapSprites != null && tapSprites.Length > 0)
            iconImage.sprite = tapSprites[Mathf.Min(_tapCount, tapSprites.Length - 1)];

        if (_tapCount >= tapsNeeded)
            _questObject.CompleteInteraction();
    }

    // HOLD: 누르고 있는 동안 게이지 증가 (뗐던 지점부터 이어서 재개)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (QuestManager.Instance?.curDailyQuest == null) return;
        if (QuestManager.Instance.EffectiveControlType != ControlType.HOLD) return;

        _isHolding = true;

        if (icon != null)
            icon.localScale = _iconOriginalScale * HoldIconScale;
    }

    // HOLD: 떼면 그 시점에서 멈춤 (진행도 유지)
    public void OnPointerUp(PointerEventData eventData)
    {
        if (QuestManager.Instance?.curDailyQuest == null) return;
        if (QuestManager.Instance.EffectiveControlType != ControlType.HOLD) return;

        _isHolding = false;

        if (icon != null)
            icon.localScale = _iconOriginalScale;
    }

}