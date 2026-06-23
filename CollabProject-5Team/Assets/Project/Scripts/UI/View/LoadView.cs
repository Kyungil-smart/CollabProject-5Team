using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameDevTycoon.UI;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// Canvas_Load 담당 View.
    /// SaveView와 동일한 슬롯 구조, 저장 버튼 제거.
    /// 슬롯 선택 상태, SavedGroup/EmptyGroup 전환, 버튼 이벤트 발행.
    /// </summary>
    public sealed class LoadView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject _loadPopup;
        [SerializeField] private Button _closeButton;

        [Header("AutoSaveSlot")]
        [SerializeField] private Button _autoSlotButton;
        [SerializeField] private GameObject _autoSavedGroup;
        [SerializeField] private GameObject _autoEmptyGroup;
        [SerializeField] private TextMeshProUGUI _autoCompanyNameLabel;
        [SerializeField] private TextMeshProUGUI _autoDateTimeLabel;
        [SerializeField] private TextMeshProUGUI _autoGoldLabel;
        [SerializeField] private TextMeshProUGUI _autoEmployeeCountLabel;
        [SerializeField] private Sprite _autoSlotSelectedSprite;
        [SerializeField] private Sprite _autoSlotDefaultSprite;

        [Header("SaveSlot_1")]
        [SerializeField] private Button _slot1Button;
        [SerializeField] private GameObject _slot1SavedGroup;
        [SerializeField] private GameObject _slot1EmptyGroup;
        [SerializeField] private TextMeshProUGUI _slot1CompanyNameLabel;
        [SerializeField] private TextMeshProUGUI _slot1DateTimeLabel;
        [SerializeField] private TextMeshProUGUI _slot1GoldLabel;
        [SerializeField] private TextMeshProUGUI _slot1EmployeeCountLabel;
        [SerializeField] private Sprite _slot1SelectedSprite;
        [SerializeField] private Sprite _slot1DefaultSprite;

        [Header("SaveSlot_2")]
        [SerializeField] private Button _slot2Button;
        [SerializeField] private GameObject _slot2SavedGroup;
        [SerializeField] private GameObject _slot2EmptyGroup;
        [SerializeField] private TextMeshProUGUI _slot2CompanyNameLabel;
        [SerializeField] private TextMeshProUGUI _slot2DateTimeLabel;
        [SerializeField] private TextMeshProUGUI _slot2GoldLabel;
        [SerializeField] private TextMeshProUGUI _slot2EmployeeCountLabel;
        [SerializeField] private Sprite _slot2SelectedSprite;
        [SerializeField] private Sprite _slot2DefaultSprite;

        [Header("Buttons")]
        [SerializeField] private Button _loadButton;

        public Observable<Unit> OnCloseClicked => _closeButton.OnClickAsObservable();
        public Observable<Unit> OnAutoSlotClicked => _autoSlotButton.OnClickAsObservable();
        public Observable<Unit> OnSlot1Clicked => _slot1Button.OnClickAsObservable();
        public Observable<Unit> OnSlot2Clicked => _slot2Button.OnClickAsObservable();
        public Observable<Unit> OnLoadClicked => _loadButton.OnClickAsObservable();

        public bool IsVisible => _loadPopup.activeSelf;

        private void Awake()
        {
            _loadPopup.SetActive(false);
            _loadButton.interactable = false;
        }

        public void Show() => _loadPopup.SetActive(true);
        public void Hide() => _loadPopup.SetActive(false);

        public void BindSlot(int slotIndex, SaveSlotData data)
        {
            switch (slotIndex)
            {
                case 1:
                    BindAutoSlot(data);
                    break;
                case 2:
                    BindSlot1(data);
                    break;
                case 3:
                    BindSlot2(data);
                    break;
            }
        }

        public void BindAutoSlot(SaveSlotData data)
        {
            BindSlotData(data, _autoSavedGroup, _autoEmptyGroup,
                _autoCompanyNameLabel, _autoDateTimeLabel, _autoGoldLabel, _autoEmployeeCountLabel);
        }

        public void BindSlot1(SaveSlotData data)
        {
            BindSlotData(data, _slot1SavedGroup, _slot1EmptyGroup,
                _slot1CompanyNameLabel, _slot1DateTimeLabel, _slot1GoldLabel, _slot1EmployeeCountLabel);
        }

        public void BindSlot2(SaveSlotData data)
        {
            BindSlotData(data, _slot2SavedGroup, _slot2EmptyGroup,
                _slot2CompanyNameLabel, _slot2DateTimeLabel, _slot2GoldLabel, _slot2EmployeeCountLabel);
        }

        public void SetSlotSelected(int slotIndex)
        {
            SetAutoSlotSelected(slotIndex == 1);
            SetSlot1Selected(slotIndex == 2);
            SetSlot2Selected(slotIndex == 3);
        }

        public void SetAutoSlotSelected(bool selected)
            => ApplySlotSprite(_autoSlotButton, selected ? _autoSlotSelectedSprite : _autoSlotDefaultSprite);

        public void SetSlot1Selected(bool selected)
            => ApplySlotSprite(_slot1Button, selected ? _slot1SelectedSprite : _slot1DefaultSprite);

        public void SetSlot2Selected(bool selected)
            => ApplySlotSprite(_slot2Button, selected ? _slot2SelectedSprite : _slot2DefaultSprite);

        public void SetLoadButtonInteractable(bool interactable)
            => _loadButton.interactable = interactable;

        private static void ApplySlotSprite(Button button, Sprite sprite)
        {
            if (sprite == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.sprite = sprite;
        }

        private static void BindSlotData(
            SaveSlotData data,
            GameObject savedGroup,
            GameObject emptyGroup,
            TextMeshProUGUI companyNameLabel,
            TextMeshProUGUI dateTimeLabel,
            TextMeshProUGUI goldLabel,
            TextMeshProUGUI employeeCountLabel)
        {
            bool hasSave = data != null;
            savedGroup.SetActive(hasSave);
            emptyGroup.SetActive(!hasSave);

            if (!hasSave) return;

            companyNameLabel.text = string.IsNullOrWhiteSpace(data.title) ? data.companyName : data.title;
            dateTimeLabel.text = data.dateTime;
            goldLabel.text = data.gold;
            employeeCountLabel.text = $"{data.employeeCount}명";
        }
    }
}
