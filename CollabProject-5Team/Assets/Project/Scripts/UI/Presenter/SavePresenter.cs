using System;
using Cysharp.Threading.Tasks;
using GameDevTycoon.Core;
using R3;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Save Presenter.
    /// 슬롯 선택/해제 토글, 저장/불러오기 버튼 처리.
    /// 빈 슬롯 선택 시 SaveButton만 활성, 저장된 슬롯은 둘 다 활성, AutoSaveSlot은 LoadButton만 활성.
    /// </summary>
    public sealed class SavePresenter : MonoBehaviour
    {
        [SerializeField] private SaveView _view;
        [SerializeField] private AlertView _alertView;

        private const int AutoSaveViewSlot = 1;
        private const int Slot1ViewSlot = 2;
        private const int Slot2ViewSlot = 3;

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
            _view.SetSlotSelected(0);
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
            _autoSlotData = CreateSlotData(0, true);
            _slot1Data = CreateSlotData(1, false);
            _slot2Data = CreateSlotData(2, false);

            _view.BindAutoSlot(_autoSlotData);
            _view.BindSlot1(_slot1Data);
            _view.BindSlot2(_slot2Data);
        }

        private static SaveSlotData CreateSlotData(int saveSlotIndex, bool isAutoSlot)
        {
            SaveLoadSystem saveLoadSystem = SaveLoadSystem.Instance;
            if (saveLoadSystem == null || !saveLoadSystem.HasSaveData(saveSlotIndex))
                return null;

            SaveData data = saveLoadSystem.GetSaveDataWithoutApply(saveSlotIndex);
            return SaveSlotData.FromSaveData(saveSlotIndex, data, isAutoSlot);
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
            _view.SetSlotSelected(_selectedSlot);
        }

        private void ApplyButtonInteractable()
        {
            var data = GetSelectedSlotData();

            // AutoSaveSlot : LoadButton만 활성
            // 저장된 슬롯 : 둘 다 활성
            // 빈 슬롯 : SaveButton만 활성
            bool isAuto = _selectedSlot == AutoSaveViewSlot;
            bool hasSave = data != null;

            _view.SetSaveButtonInteractable(!isAuto);
            _view.SetLoadButtonInteractable(hasSave);
        }

        private SaveSlotData GetSelectedSlotData() => _selectedSlot switch
        {
            AutoSaveViewSlot => _autoSlotData,
            Slot1ViewSlot => _slot1Data,
            Slot2ViewSlot => _slot2Data,
            _ => null,
        };

        private void OnSaveClicked()
        {
            if (_selectedSlot == 0) return;
            if (_selectedSlot == AutoSaveViewSlot) return;

            Confirm("저장하시겠습니까?", () =>
            {
                SaveLoadSystem.Instance.SaveGame(_selectedSlot - 1);

                RefreshSlots();
                ApplySlotSelection();
                ApplyButtonInteractable();
                ShowAlert("저장되었습니다.");
            });
        }

        private void OnLoadClicked()
        {
            if (_selectedSlot == 0) return;
            if (GetSelectedSlotData() == null) return;

            Confirm("불러오시겠습니까?", () =>
            {
                if (!SaveLoadSystem.Instance.SetPendingLoad(_selectedSlot - 1))
                {
                    ShowAlert("불러오기에 실패했습니다.");
                    return;
                }

                ReloadGameSceneAsync().Forget();
            });
        }
        private async UniTaskVoid ReloadGameSceneAsync()
        {
            Hide();
            await SceneLoader.Instance.LoadAsync(SceneName.Game);
        }

        private void Confirm(string message, Action onConfirm)
        {
            if (_alertView != null)
            {
                _alertView.ShowConfirmPopup(message, onConfirm);
                return;
            }

            onConfirm?.Invoke();
        }

        private void ShowAlert(string message)
        {
            if (_alertView != null)
            {
                _alertView.ShowAlertPopup(message);
                return;
            }

            Debug.Log(message);
        }
    }
}
