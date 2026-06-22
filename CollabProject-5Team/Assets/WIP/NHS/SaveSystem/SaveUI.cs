using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveUI : MonoBehaviour
{
    [System.Serializable]
    public struct SaveSlotUI
    {
        public Image           companyImage;
        public TextMeshProUGUI saveDateText;
        public TextMeshProUGUI saveDataText;
        public TextMeshProUGUI saveDataDetailText;
        public Button          saveButton;
    }

    [SerializeField] private SaveSlotUI[] slotUIs = new SaveSlotUI[3];

    [SerializeField] private Sprite[] companySprites;

    private void Start()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            int slotNumber = i;
            slotUIs[slotNumber].saveButton.onClick.AddListener(() =>
            {
                SaveLoadSystem.Instance.SaveGame(slotNumber);
                RefreshSaveSlots();
            });
        }
    }

    private void OnEnable()
    {
        Debug.Log("정보를 초기화합니다.");
        RefreshSaveSlots();
    }

    public void RefreshSaveSlots()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            int slotNumber = i;

            if (SaveLoadSystem.Instance.HasSaveData(slotNumber))
            {
                SaveData data = SaveLoadSystem.Instance.GetSaveDataWithoutApply(slotNumber);

                if (data != null)
                {
                    slotUIs[slotNumber].saveDateText.text = data.realSaveTime;
                    slotUIs[slotNumber].saveDataText.text = $"{data.activeProjectsData.project_userNamed}";
                    slotUIs[slotNumber].saveDataDetailText.text = $"{data.currentWeek}주차 {data.day}일 {data.currentDay}({data.currentTime})\n " +
                                                                  $"플레이 시간 {data.playTime} \n " +
                                                                  $"자금: {data.company_Gold}G \n " +
                                                                  $"직원 수 : {data.savedEmployees.Count}";
                }
            }
            else
            {
                slotUIs[i].saveDateText.text = "----/--/--";
                slotUIs[i].saveDataText.text = "빈 슬롯";
                slotUIs[i].saveDataDetailText.text = "데이터 없음";
            }
        }
    }
}
