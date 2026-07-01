using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class NPCInteract : MonoBehaviour, IInteractable
{
    Employee emp;
    private string npcName;

    private Animator anim;

    [Header("NPC대화 UI창")]
    [SerializeField] private GameObject interactionUI;

    [Header("UI창 내부의 텍스트, 퀘스트 완료 버튼")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    private void Awake()
    {
        emp = GetComponent<Employee>();

        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        // 하이어라키 창의 이름이 NPC의 고유 식별자로 등록
        npcName = gameObject.name;
    }

    public void OnInteract()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartWaitingForDialogue();
        }

        var controller = GetComponent<NPCController>();
        var player = GameManager.Instance.player;

        if (controller != null && controller.IsInteracting) return;
        
        
        if (controller != null)
        {
            controller.StartConversation();
        }


        if (player != null)
        {
            Vector3 lookAtPlayer = player.transform.position;
            lookAtPlayer.y = transform.position.y;
            transform.LookAt(lookAtPlayer);

            Vector3 lookAtNpc = transform.position;
            lookAtNpc.y = player.transform.position.y;
            player.transform.LookAt(lookAtNpc);
        }

        if (anim != null)
        {
            anim.SetTrigger("Greet");

            Invoke(nameof(RollBackAnimation), 1.0f);
        }

        if (player != null)
        {
            player.GetComponent<Animator>()?.SetTrigger("Greet");
            player.ResetMovementState();
        }


        int state = DateTimeManager.Instance.GetDialogueState(emp.so.id.ToString(),DateTimeManager.Instance.isEventQuest);
        
        // 일반 대화
        if (state == 0)
        {
            player?.CloseInteractionUI();
            controller?.EndConversation();
            // 임무 없으면 대화 없음 - 플레이어 이동 잠금 해제
            GameManager.Instance.player.CloseInteractionUI();
            return;
        }
        // 업무 완료 후 첫 대화
        else if (state == 1)
        {
            Dialogue.DialogueManager.Instance.StartDialogueById(emp);
            DateTimeManager.Instance.MarkTalkedThisWeek(emp);
            return;
        }
        // 퀘스트 완료 후 대화
        else if (state == 2)
        {
            Dialogue.DialogueManager.Instance.ShowBusyMessage(emp);

            controller?.EndConversation();

            return;
        }

        interactionUI.SetActive(true);
    }

    private void RollBackAnimation()
    {
        var controller = GetComponent<NPCController>();
        if(controller != null)
        {
            controller?.RestoreActionAnimation();
        }
    }

    public Transform GetTransform()
    {
        // 플레이어가 다가올 내 위치 제공
        return transform;
    }
}
