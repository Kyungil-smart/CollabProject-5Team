using R3;
using UnityEngine;
using UnityEngine.UI;

public class QuestObject : MonoBehaviour, IInteractable
{
    [SerializeField] private SpeechBubble speechBubble;   // 클릭 유도용 아이콘 말풍선
    [SerializeField] private Button interactButton;       // 말풍선 클릭 시 상호작용 시작
    [SerializeField] private Collider targetCollider;     // 상호작용 거리 체크용 콜라이더

    private void Start()
    {
        if (interactButton != null)
            interactButton.onClick.AddListener(OnBubbleClicked);

        QuestManager.Instance.dailyQuestState.Subscribe(OnQuestStateChanged).AddTo(this);
    }

    private void OnQuestStateChanged(QuestState state)
    {
        if (speechBubble == null) return;

        if (state == QuestState.Playing && IsCurrentTarget())
            speechBubble.Show();
        else
            speechBubble.Hide();
    }

    private bool IsCurrentTarget()
    {
        DailyQuest quest = QuestManager.Instance.curDailyQuest;
        if (quest == null) return false;

        foreach (string objName in quest.so.activeObjects.Split(','))
        {
            if (objName.Trim() == gameObject.name) return true;
        }
        return false;
    }

    // 말풍선(버튼) 클릭 시 플레이어를 이 오브젝트로 이동시켜 상호작용 시작
    private void OnBubbleClicked()
    {
        if (!IsCurrentTarget()) return;

        Collider col = targetCollider != null ? targetCollider : GetComponent<Collider>();
        GameManager.Instance.player.SetInteractTarget(this, col);
    }

    public void OnInteract()
    {
        if (!IsCurrentTarget()) return;

        // 도착하면 전구 말풍선은 끄고 퀘스트 탭(별 아이콘)으로 전환
        speechBubble?.Hide();

        // TAP/HOLD/SWIPE 모두 퀘스트 탭(미니게임 UI)을 통해 진행 - 완료 시 UI에서 CompleteInteraction 호출
        // TODO: 퀘스트 탭 UI 오픈 연동 (UI 담당)
    }

    // 퀘스트 탭(미니게임 UI) 완료 시 호출 - 진행도 갱신 + 오브젝트 비활성화
    public void CompleteInteraction()
    {
        speechBubble?.Hide();
        gameObject.SetActive(false);

        DailyQuest quest = QuestManager.Instance.curDailyQuest;

        // TAP: 아이콘 1개당 1진행도 / HOLD,SWIPE: 아이콘 1개로 퀘스트 전체 완료
        int amount = quest.so.controlType == ControlType.TAP ? 1 : quest.TargetCount;
        QuestManager.Instance.UpdateProgress(amount);
    }

    public Transform GetTransform() => transform;
}
