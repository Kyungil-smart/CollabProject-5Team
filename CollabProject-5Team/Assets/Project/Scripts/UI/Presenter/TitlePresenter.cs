using Cysharp.Threading.Tasks;
using GameDevTycoon.Core;
using R3;
using UnityEngine;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 타이틀 씬 Presenter.
    /// 시작/설정 버튼 처리, 세이브 슬롯 바인딩, 씬 전환 담당.
    /// </summary>
    public sealed class TitlePresenter : MonoBehaviour
    {
        [SerializeField] private TitleView         _view;
        [SerializeField] private SettingsPresenter _settingsPresenter;

        [Header("LoadSlotView 프리팹")]
        [SerializeField] private LoadSlotView _slotPrefab;

        private void Start()
        {
            BindButtons();
            SpawnSlots();
        }

        private void BindButtons()
        {
            _view.OnStartClicked
                .Subscribe(_ => _view.ShowLoadPanel())
                .AddTo(this);

            _view.OnSettingsClicked
                .Subscribe(_ => _settingsPresenter.Show())
                .AddTo(this);
        }

        private void SpawnSlots()
        {
            // 세이브 슬롯 3개 고정 생성
            for (int i = 0; i < 3; i++)
            {
                var slot = Instantiate(_slotPrefab, _view.SlotContent);
                slot.PlayEntrance(i * 0.08f);

                // [TODO: SaveSystem 연결 후 실제 데이터 바인딩]
                // SaveSlotData data = SaveSystem.LoadSlot(i);
                // slot.Bind(data);
                slot.Bind(null);

                int captured = i;
                slot.OnSelected += slotIndex => OnSlotSelected(slotIndex);
            }
        }

        private void OnSlotSelected(int slotIndex)
        {
            _view.HideLoadPanel();
            // [TODO: SaveSystem에서 슬롯 데이터 로드 후 씬 전환]
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await SceneLoader.Instance.LoadAsync(
                SceneName.Game,
                this.GetCancellationTokenOnDestroy()
            );
        }
    }
}