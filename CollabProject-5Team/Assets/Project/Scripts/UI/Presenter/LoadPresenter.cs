using Cysharp.Threading.Tasks;
using GameDevTycoon.Core;
using R3;
using UnityEngine;
using GameDevTycoon.UI;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// Canvas_Load Presenter.
    /// 타이틀 씬 전용 로드 전용 팝업. 저장 기능 없음.
    /// 슬롯 선택 후 불러오기 시 게임씬 전환.
    /// 실제 SaveSystem 연결은 [TODO]로 표기.
    /// </summary>
    public sealed class LoadPresenter : MonoBehaviour
    {
        [SerializeField] private LoadView _view;
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
            if (_selectedSlot == slotIndex)
            {
                _selectedSlot = 0;
                ApplySlotSelection();
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
            bool hasSave = data != null || _selectedSlot == 1;
            _view.SetLoadButtonInteractable(hasSave);
        }

        private SaveSlotData GetSelectedSlotData() => _selectedSlot switch
        {
            1 => _autoSlotData,
            2 => _slot1Data,
            3 => _slot2Data,
            _ => null,
        };

        private void OnLoadClicked()
        {
            if (_selectedSlot == 0) return;

            _alertView.ShowConfirmPopup("불러오시겠습니까?", () =>
            {
                // [TODO: SaveSystem 연결 후 실제 로드 처리 및 씬 전환]
                LoadGameSceneAsync().Forget();
            });
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            Hide();
            await SceneLoader.Instance.LoadAsync(
                SceneName.Game,
                this.GetCancellationTokenOnDestroy()
            );
        }
    }
}