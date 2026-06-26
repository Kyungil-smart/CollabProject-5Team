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

        [Header("회사 레벨별 아이콘 (1/2/3)")]
        [SerializeField] private Sprite[] _companyLevelIcons;

        [Header("AutoSaveSlot")]
        [SerializeField] private Button _autoSlotButton;
        [SerializeField] private GameObject _autoSlotSelectEffect;
        [SerializeField] private GameObject _autoSavedGroup;
        [SerializeField] private GameObject _autoEmptyGroup;
        [SerializeField] private Image _autoLevelIconImage;
        [SerializeField] private TextMeshProUGUI _autoCompanyNameLabel;
        [SerializeField] private TextMeshProUGUI _autoDateTimeLabel;
        [SerializeField] private TextMeshProUGUI _autoGoldLabel;
        [SerializeField] private TextMeshProUGUI _autoEmployeeCountLabel;

        [Header("SaveSlot_1")]
        [SerializeField] private Button _slot1Button;
        [SerializeField] private GameObject _slot1SelectEffect;
        [SerializeField] private GameObject _slot1SavedGroup;
        [SerializeField] private GameObject _slot1EmptyGroup;
        [SerializeField] private Image _slot1LevelIconImage;
        [SerializeField] private TextMeshProUGUI _slot1CompanyNameLabel;
        [SerializeField] private TextMeshProUGUI _slot1DateTimeLabel;
        [SerializeField] private TextMeshProUGUI _slot1GoldLabel;
        [SerializeField] private TextMeshProUGUI _slot1EmployeeCountLabel;

        [Header("SaveSlot_2")]
        [SerializeField] private Button _slot2Button;
        [SerializeField] private GameObject _slot2SelectEffect;
        [SerializeField] private GameObject _slot2SavedGroup;
        [SerializeField] private GameObject _slot2EmptyGroup;
        [SerializeField] private Image _slot2LevelIconImage;
        [SerializeField] private TextMeshProUGUI _slot2CompanyNameLabel;
        [SerializeField] private TextMeshProUGUI _slot2DateTimeLabel;
        [SerializeField] private TextMeshProUGUI _slot2GoldLabel;
        [SerializeField] private TextMeshProUGUI _slot2EmployeeCountLabel;

        [Header("Buttons")]
        [SerializeField] private Button _loadButton;
        [SerializeField] private Sprite _loadActiveSprite;
        [SerializeField] private Sprite _buttonInactiveSprite;

        public Observable<Unit> OnCloseClicked => _closeButton.OnClickAsObservable();
        public Observable<Unit> OnAutoSlotClicked => _autoSlotButton.OnClickAsObservable();
        public Observable<Unit> OnSlot1Clicked => _slot1Button.OnClickAsObservable();
        public Observable<Unit> OnSlot2Clicked => _slot2Button.OnClickAsObservable();
        public Observable<Unit> OnLoadClicked => _loadButton.OnClickAsObservable();

        public bool IsVisible => _loadPopup.activeSelf;

        private void Awake()
        {
            _loadPopup.SetActive(false);
            SetLoadButtonInteractable(false);
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
            BindSlotData(data, _autoSavedGroup, _autoEmptyGroup, _autoLevelIconImage,
                _autoCompanyNameLabel, _autoDateTimeLabel, _autoGoldLabel, _autoEmployeeCountLabel);
        }

        public void BindSlot1(SaveSlotData data)
        {
            BindSlotData(data, _slot1SavedGroup, _slot1EmptyGroup, _slot1LevelIconImage,
                _slot1CompanyNameLabel, _slot1DateTimeLabel, _slot1GoldLabel, _slot1EmployeeCountLabel);
        }

        public void BindSlot2(SaveSlotData data)
        {
            BindSlotData(data, _slot2SavedGroup, _slot2EmptyGroup, _slot2LevelIconImage,
                _slot2CompanyNameLabel, _slot2DateTimeLabel, _slot2GoldLabel, _slot2EmployeeCountLabel);
        }

        // 선택 상태 표시 — 슬롯 배경 위에 선택 이펙트 오브젝트 on/off
        public void SetSlotSelected(int slotIndex)
        {
            SetAutoSlotSelected(slotIndex == 1);
            SetSlot1Selected(slotIndex == 2);
            SetSlot2Selected(slotIndex == 3);
        }

        public void SetAutoSlotSelected(bool selected)
            => SetActiveIfExists(_autoSlotSelectEffect, selected);

        public void SetSlot1Selected(bool selected)
            => SetActiveIfExists(_slot1SelectEffect, selected);

        public void SetSlot2Selected(bool selected)
            => SetActiveIfExists(_slot2SelectEffect, selected);

        public void SetLoadButtonInteractable(bool interactable)
        {
            _loadButton.interactable = interactable;
            ApplyButtonSprite(_loadButton, interactable ? _loadActiveSprite : _buttonInactiveSprite);
        }

        private static void SetActiveIfExists(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
        }

        private static void ApplyButtonSprite(Button button, Sprite sprite)
        {
            if (sprite == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.sprite = sprite;
        }

        private void BindSlotData(
            SaveSlotData data,
            GameObject savedGroup,
            GameObject emptyGroup,
            Image levelIconImage,
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

            ApplyLevelIcon(levelIconImage, data.companyLevel);
        }

        private void ApplyLevelIcon(Image levelIconImage, int companyLevel)
        {
            if (levelIconImage == null || _companyLevelIcons == null) return;

            int index = companyLevel - 1;
            if (index < 0 || index >= _companyLevelIcons.Length) return;

            Sprite sprite = _companyLevelIcons[index];
            if (sprite != null)
                levelIconImage.sprite = sprite;
        }
    }
}
