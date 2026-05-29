using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteract : MonoBehaviour, IInteractable
{
    private string npcName;

    [Header("NPC대화 UI창")]
    [SerializeField] private GameObject interactionUI;

    [Header("UI창 내부의 텍스트, 퀘스트 완료 버튼")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button questCompleteButton;

    private void Start()
    {
        // 하이어라키 창의 이름이 NPC의 고유 식별자로 등록
        npcName = gameObject.name;

        if(questCompleteButton != null)
        {
            questCompleteButton.onClick.AddListener(OnClickQuestComplete);
        }
    }

    public void OnInteract()
    {
        int state = DateTimeManager.Instance.GetDialogueState(npcName);

            // 일반 대화
            if (state == 0)
            {
                // 일반 대화
                dialogueText.text = $"{npcName}: Hi (General Dialogue)";

                if (questCompleteButton != null)
                {
                    questCompleteButton.gameObject.SetActive(false);
                }
            }

            // 특별 대화
            else if (state == 1)
            {
                dialogueText.text = $"{npcName}: Quest (Special Dialogue)";

                if (questCompleteButton != null)
                {
                    questCompleteButton.gameObject.SetActive(true);
                }
            }

            // 퀘스트 완료 후 대화
            else if (state == 2)
            {
                dialogueText.text = $"{npcName}: See Ya (Done)";

                if (questCompleteButton != null)
                {
                    questCompleteButton.gameObject.SetActive(false);
                }
            }

            interactionUI.SetActive(true);   
    }

    private void OnClickQuestComplete()
    {
        // 퀘스트 완료 버튼을 누른 뒤 해당 상황을 기록
        DateTimeManager.Instance.CompleteSpecialDialogue(npcName);

        // 버튼 누른 뒤 대사
        dialogueText.text = $"{npcName}: Quest Complete, Thanks";

        // 퀘스트 완료버튼 숨기기
        if (questCompleteButton != null)
        {
            questCompleteButton.gameObject.SetActive(false);
        }
    }

    public Transform GetTransform()
    {
        // 플레이어가 다가올 내 위치 제공
        return transform;
    }
}
