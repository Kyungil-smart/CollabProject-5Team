using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteract : MonoBehaviour, IInteractable
{
    Employee emp;
    private string npcName;

    [Header("NPC대화 UI창")]
    [SerializeField] private GameObject interactionUI;

    [Header("UI창 내부의 텍스트, 퀘스트 완료 버튼")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    //[SerializeField] private Button questCompleteButton;

    private void Awake()
    {
        emp = GetComponent<Employee>();
        emp.Init();
    }

    private void Start()
    {
        // 하이어라키 창의 이름이 NPC의 고유 식별자로 등록
        npcName = gameObject.name;

        // NPC 자신의 Employee를 haveEmployees에 등록
        _EmployeeManager.Instance.haveEmployees.AddEmployee(emp);

        //if (questCompleteButton != null)
        //{
        //    questCompleteButton.onClick.AddListener(OnClickQuestComplete);
        //}
    }

    public void OnInteract()
    {
        int state = DateTimeManager.Instance.GetDialogueState(emp.so.id.ToString());
        
        // 일반 대화
        if (state == 0)
        {
            // 임무 없으면 대화 없음 - 플레이어 이동 잠금 해제
            GameManager.Instance.player?.CloseInteractionUI();
            return;
        }
        // 업무 완료 후 첫 대화
        else if (state == 1)
        {
            Debug.Log("진입");
            Dialogue.DialogueManager.Instance.StartDialogueById(emp);
            DateTimeManager.Instance.MarkTalkedThisWeek(emp);
        }
        // 퀘스트 완료 후 대화
        else if (state == 2)
        {
            dialogueText.text = $"{npcName}: See Ya (Done)";
        }

        interactionUI.SetActive(true);
    }

    //private void OnClickQuestComplete()
    //{
    //    DateTimeManager.Instance.CompleteSpecialDialogue(npcName);
    //    dialogueText.text = $"{npcName}: Quest Complete, Thanks";
    //    if (questCompleteButton != null)
    //    {
    //        questCompleteButton.gameObject.SetActive(false);
    //    }
    //}

    public Transform GetTransform()
    {
        // 플레이어가 다가올 내 위치 제공
        return transform;
    }
}
