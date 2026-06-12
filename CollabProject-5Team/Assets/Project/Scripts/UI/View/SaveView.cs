using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Save 담당 View.
    /// 슬롯 3개 고정(AutoSaveSlot, SaveSlot_1, SaveSlot_2) 구조.
    /// 슬롯 선택 상태, SavedGroup/EmptyGroup 전환, 버튼 이벤트 발행.
    /// 실제 저장/불러오기 로직은 Presenter에서 담당.
    /// </summary>
    public sealed class SaveView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject _savePopup;
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
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;

        public Observable<Unit> OnCloseClicked => _closeButton.OnClickAsObservable();
        public Observable<Unit> OnAutoSlotClicked => _autoSlotButton.OnClickAsObservable();
        public Observable<Unit> OnSlot1Clicked => _slot1Button.OnClickAsObservable();
        public Observable<Unit> OnSlot2Clicked => _slot2Button.OnClickAsObservable();
        public Observable<Unit> OnSaveClicked => _saveButton.OnClickAsObservable();
        public Observable<Unit> OnLoadClicked => _loadButton.OnClickAsObservable();

        public bool IsVisible => _savePopup.activeSelf;

        private void Awake()
        {
            _savePopup.SetActive(false);
            _saveButton.interactable = false;
            _loadButton.interactable = false;
        }

        public void Show() => _savePopup.SetActive(true);
        public void Hide() => _savePopup.SetActive(false);

        // 슬롯 데이터 바인딩 — data가 null이면 EmptyGroup 표시
        public void BindAutoSlot(SaveSlotData data)
        {
            bool hasSave = data != null;
            _autoSavedGroup.SetActive(hasSave);
            _autoEmptyGroup.SetActive(!hasSave);

            if (!hasSave) return;
            _autoCompanyNameLabel.text = data.companyName;
            _autoDateTimeLabel.text = data.dateTime;
            _autoGoldLabel.text = data.gold;
            _autoEmployeeCountLabel.text = $"{data.employeeCount}명";
        }

        public void BindSlot1(SaveSlotData data)
        {
            bool hasSave = data != null;
            _slot1SavedGroup.SetActive(hasSave);
            _slot1EmptyGroup.SetActive(!hasSave);

            if (!hasSave) return;
            _slot1CompanyNameLabel.text = data.companyName;
            _slot1DateTimeLabel.text = data.dateTime;
            _slot1GoldLabel.text = data.gold;
            _slot1EmployeeCountLabel.text = $"{data.employeeCount}명";
        }

        public void BindSlot2(SaveSlotData data)
        {
            bool hasSave = data != null;
            _slot2SavedGroup.SetActive(hasSave);
            _slot2EmptyGroup.SetActive(!hasSave);

            if (!hasSave) return;
            _slot2CompanyNameLabel.text = data.companyName;
            _slot2DateTimeLabel.text = data.dateTime;
            _slot2GoldLabel.text = data.gold;
            _slot2EmployeeCountLabel.text = $"{data.employeeCount}명";
        }

        // 선택 상태 표시 — 스프라이트 스왑, 리소스 없으면 스킵
        public void SetAutoSlotSelected(bool selected)
            => ApplySlotSprite(_autoSlotButton, selected ? _autoSlotSelectedSprite : _autoSlotDefaultSprite);

        public void SetSlot1Selected(bool selected)
            => ApplySlotSprite(_slot1Button, selected ? _slot1SelectedSprite : _slot1DefaultSprite);

        public void SetSlot2Selected(bool selected)
            => ApplySlotSprite(_slot2Button, selected ? _slot2SelectedSprite : _slot2DefaultSprite);

        public void SetSaveButtonInteractable(bool interactable)
            => _saveButton.interactable = interactable;

        public void SetLoadButtonInteractable(bool interactable)
            => _loadButton.interactable = interactable;

        private static void ApplySlotSprite(Button button, Sprite sprite)
        {
            if (sprite == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.sprite = sprite;
        }
    }

    public sealed class SaveSlotData
    {
        public string companyName;
        public string dateTime;
        public string gold;
        public int employeeCount;
    }
}