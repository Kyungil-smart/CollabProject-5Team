using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveUI : MonoBehaviour
{
    // 각 슬롯의 UI를 묶어서 관리하면 코드가 깔끔해집니다.
    [System.Serializable]
    public struct SaveSlotUI
    {
        public Image companyImage;
        public TextMeshProUGUI saveDateText;
        public TextMeshProUGUI saveDataText;
        public TextMeshProUGUI saveDataDetailText;
    }

    [SerializeField] private SaveSlotUI[] slotUIs = new SaveSlotUI[3];

    [SerializeField] private Sprite[] companySprites;

    private void OnEnable()
    {
        // UI 창이 켜질 때마다 새로고침합니다.
        RefreshSaveSlots();
    }

    public void RefreshSaveSlots()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            int slotNumber = i + 1; // 슬롯 번호는 1, 2, 3

            if (SaveLoadSystem.Instance.HasSaveData(slotNumber))
            {
                tempData data = SaveLoadSystem.Instance.LoadGame(slotNumber);

                if (data != null)
                {
                }
            }
            else
            {
            }
        }
    }
}