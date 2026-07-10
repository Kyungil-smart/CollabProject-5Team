using UnityEngine;

// 퀘스트 활성 오브젝트에 부착. 활성화되면 Canvas_Quest 하위에 전구/별 아이콘을 생성해 자기 위치 위에 띄운다.
public class QuestObject : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject bulbIconPrefab;   // 전구 아이콘 프리팹 (이동 유도)
    [SerializeField] private GameObject starIconPrefab;   // 별 아이콘 프리팹 (TAP/HOLD/SWIPE 입력 처리)
    [SerializeField] private Collider targetCollider;     // 상호작용 거리 체크용 콜라이더
    [SerializeField] private Vector3 iconWorldOffset = new Vector3(0f, 1f, 0f); // 아이콘이 뜰 위치 (오브젝트 기준 오프셋)
    [SerializeField] private float interactionRange = 1f;

    // 커피머신처럼 씬에 항상 존재하는 오브젝트인 경우 체크.
    // true면 QuestManager가 이 오브젝트를 켜고 끌 때 GameObject 전체가 아닌 이 컴포넌트(enabled)만 토글한다.
    [SerializeField] private bool isPermanent;
    public bool IsPermanent => isPermanent;

    private QuestIcon _bulbInstance;
    private QuestInteract _starInstance;

    private void Awake()
    {
        if (targetCollider == null) targetCollider = GetComponent<Collider>();
    }

    private void OnEnable() { ShowBulb(); }
    private void OnDisable() { ClearIcons(); }

    private void Update()
    {
        if (_bulbInstance == null && _starInstance == null) return;

        Collider col = targetCollider;
        if (col == null || GameManager.Instance.player == null) return;

        Vector3 closestPoint = col.ClosestPoint(GameManager.Instance.player.transform.position);
        float distance = Vector3.Distance(GameManager.Instance.player.transform.position, closestPoint);

        if (_bulbInstance != null && distance <= interactionRange)
            OnInteract();
        else if (_starInstance != null && distance > interactionRange + 0.5f)
            ShowBulb();
    }

    public void Activate()
    {
        enabled = true;
        ShowBulb();
    }

    public void Deactivate()
    {
        ClearIcons();
        enabled = false;
    }

    private void ShowBulb()
    {
        ClearIcons();

        _bulbInstance = Instantiate(bulbIconPrefab, QuestManager.Instance.QuestCanvas).GetComponent<QuestIcon>();
        _bulbInstance.gameObject.SetActive(true);
        _bulbInstance.transform.SetAsFirstSibling();
        _bulbInstance.SetTarget(transform, iconWorldOffset);
        _bulbInstance.Button.onClick.AddListener(OnIconClicked);
    }

    private void ShowStar()
    {
        ClearIcons();

        _starInstance = Instantiate(starIconPrefab, QuestManager.Instance.QuestCanvas).GetComponent<QuestInteract>();
        _starInstance.gameObject.SetActive(true);
        _starInstance.transform.SetAsFirstSibling();
        _starInstance.SetTarget(transform, iconWorldOffset);
        _starInstance.SetQuestObject(this);
    }

    private void ClearIcons()
    {
        if (_bulbInstance != null) Destroy(_bulbInstance.gameObject);
        if (_starInstance != null) Destroy(_starInstance.gameObject);

        _bulbInstance = null;
        _starInstance = null;
    }

    // 전구 아이콘 클릭 시 플레이어를 이 오브젝트로 이동시켜 상호작용 시작
    private void OnIconClicked()
    {
        AudioManager.Instance?.PlaySFXClick();
        GameManager.Instance.player.SetInteractTarget(this, targetCollider);
    }

    public void OnInteract()
    {
        // 도착하면 전구 아이콘을 끄고 별 아이콘으로 전환 (TAP/HOLD/SWIPE 입력은 QuestInteract가 처리)
        ShowStar();
    }

    // 미니게임(TAP/HOLD/SWIPE) 완료 시 호출 - 진행도 갱신 + 오브젝트 비활성화
    public void CompleteInteraction()
    {
        ClearIcons();

        // 항상 있는 오브젝트(커피머신 등)는 비활성화하지 않고 컴포넌트만 끔
        if (isPermanent) enabled = false;
        else gameObject.SetActive(false);

        // 상호작용 상태 해제 - 안 하면 플레이어가 다시 움직이지 못함
        GameManager.Instance.player.CloseInteractionUI();

        // 다중 오브젝트 퀘스트면 이 오브젝트와 같은 순서의 결과물을 즉시 활성화 (전체 완료 대기 없이 바로 전환)
        QuestManager.Instance.ActivatePairedResult(gameObject.name);

        DailyQuest quest = QuestManager.Instance.curDailyQuest;
        ControlType effectiveType = QuestManager.Instance.EffectiveControlType;
        int effectiveTarget = QuestManager.Instance.EffectiveTargetCount;

        // 활성 오브젝트가 여러 개인 TAP: 오브젝트 1개당 1진행도 / 그 외: 한 번에 퀘스트 전체 완료
        int amount = (effectiveType == ControlType.TAP && quest.so.ActiveObjectCount > 1) ? 1 : effectiveTarget;
        QuestManager.Instance.UpdateProgress(amount);
    }

    public Transform GetTransform() => transform;
}