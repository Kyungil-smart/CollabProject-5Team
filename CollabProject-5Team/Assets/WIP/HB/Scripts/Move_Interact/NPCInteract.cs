using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteract : MonoBehaviour, IInteractable
{
    [Header("NPC 설정")]
    [SerializeField] private string npcName = "Cat1";

    [Header("띄울 UI창")]
    [SerializeField] private GameObject interactionUI;

    [Header("UI창 내부의 텍스트")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button questCompleteButton;


    public void OnInteract()
    {
        int state = DateTimeManager.Instance.GetDialogueState(npcName);

            // 일반 대화
            if (state == 0)
            {
                // 일반 대화
                dialogueText.text = $"{npcName}: Hi (General Dialogue)";
            }

            // 특별 대화
            else if (state == 1)
            {
                dialogueText.text = $"{npcName}: Quest (Special Dialogue)";

                if (questCompleteButton != null)
                {
                    questCompleteButton.gameObject.SetActive(true);

                    questCompleteButton.onClick.RemoveAllListeners();
                    questCompleteButton.onClick.AddListener(() => OnClickQuestComplete());
                }
            }

            // 퀘스트 완료 후 대화
            else if (state == 2)
            {
                dialogueText.text = $"{npcName}: See Ya (Done)";
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
