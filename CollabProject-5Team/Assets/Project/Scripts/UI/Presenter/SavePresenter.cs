using R3;
using UnityEngine;
using GameDevTycoon.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Save Presenter.
    /// 슬롯 선택/해제 토글, 저장/불러오기 버튼 처리.
    /// 빈 슬롯 선택 시 SaveButton만 활성, 저장된 슬롯은 둘 다 활성, AutoSaveSlot은 LoadButton만 활성.
    /// 실제 SaveSystem 연결은 [TODO]로 표기.
    /// </summary>
    public sealed class SavePresenter : MonoBehaviour
    {
        [SerializeField] private SaveView _view;
        [SerializeField] private AlertView _alertView;

        // 0 = 없음, 1 = AutoSave, 2 = Slot1, 3 = Slot2
        private int _selectedSlot;

        private SaveSlotData _autoSlotData;
        private SaveSlotData _slot1Data;
        private SaveSlotData _slot2Data;

        private void Start()
        {
            BindButtons();
        }

        public void Show()
        {
            if (_view.IsVisible) return;
            RefreshSlots();
            _view.Show();
        }

        public void Hide()
        {
            _selectedSlot = 0;
            _view.SetAutoSlotSelected(false);
            _view.SetSlot1Selected(false);
            _view.SetSlot2Selected(false);
            _view.SetSaveButtonInteractable(false);
            _view.SetLoadButtonInteractable(false);
            _view.Hide();
        }

        private void BindButtons()
        {
            _view.OnCloseClicked
                .Subscribe(_ => Hide())
                .AddTo(this);

            _view.OnAutoSlotClicked
                .Subscribe(_ => OnSlotClicked(1))
                .AddTo(this);

            _view.OnSlot1Clicked
                .Subscribe(_ => OnSlotClicked(2))
                .AddTo(this);

            _view.OnSlot2Clicked
                .Subscribe(_ => OnSlotClicked(3))
                .AddTo(this);

            _view.OnSaveClicked
                .Subscribe(_ => OnSaveClicked())
                .AddTo(this);

            _view.OnLoadClicked
                .Subscribe(_ => OnLoadClicked())
                .AddTo(this);
        }

        private void RefreshSlots()
        {
            // [TODO: SaveSystem 연결 후 실제 슬롯 데이터 바인딩]
            _autoSlotData = null;
            _slot1Data = null;
            _slot2Data = null;

            _view.BindAutoSlot(_autoSlotData);
            _view.BindSlot1(_slot1Data);
            _view.BindSlot2(_slot2Data);
        }

        private void OnSlotClicked(int slotIndex)
        {
            // 같은 슬롯 재클릭 시 선택 해제
            if (_selectedSlot == slotIndex)
            {
                _selectedSlot = 0;
                ApplySlotSelection();
                _view.SetSaveButtonInteractable(false);
                _view.SetLoadButtonInteractable(false);
                return;
            }

            _selectedSlot = slotIndex;
            ApplySlotSelection();
            ApplyButtonInteractable();
        }

        private void ApplySlotSelection()
        {
            _view.SetAutoSlotSelected(_selectedSlot == 1);
            _view.SetSlot1Selected(_selectedSlot == 2);
            _view.SetSlot2Selected(_selectedSlot == 3);
        }

        private void ApplyButtonInteractable()
        {
            var data = GetSelectedSlotData();

            // AutoSaveSlot : LoadButton만 활성
            // 저장된 슬롯 : 둘 다 활성
            // 빈 슬롯 : SaveButton만 활성
            bool isAuto = _selectedSlot == 1;
            bool hasSave = data != null;

            _view.SetSaveButtonInteractable(!isAuto);
            _view.SetLoadButtonInteractable(isAuto || hasSave);
        }

        private SaveSlotData GetSelectedSlotData() => _selectedSlot switch
        {
            1 => _autoSlotData,
            2 => _slot1Data,
            3 => _slot2Data,
            _ => null,
        };

        private void OnSaveClicked()
        {
            if (_selectedSlot == 0) return;

            _alertView.ShowConfirmPopup("저장하시겠습니까?", () =>
            {
                // [TODO: SaveSystem 연결]
            });
        }

        private void OnLoadClicked()
        {
            if (_selectedSlot == 0) return;

            _alertView.ShowConfirmPopup("불러오시겠습니까?", () =>
            {
                // [TODO: SaveSystem 연결]
            });
        }
    }
}