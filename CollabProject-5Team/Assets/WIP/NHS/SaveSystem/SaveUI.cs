using System.Linq;
using System.Threading.Tasks;
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
            slotUIs[i].saveButton.onClick.AddListener(() =>
            {
                SaveLoadSystem.Instance.SaveGame(i);
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
                SaveData data = SaveLoadSystem.Instance.LoadGame(slotNumber);

                if (data != null)
                {
                }
            }
            else
            {
                slotUIs[i].saveDateText.text = "----/--/--";
                slotUIs[i].saveDataDetailText.text = "데이터 없음";
            }
        }
    }
}