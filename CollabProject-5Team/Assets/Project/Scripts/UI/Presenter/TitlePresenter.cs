using Cysharp.Threading.Tasks;
using GameDevTycoon.Core;
using GameDevTycoon.UI.Title;
using R3;
using UnityEngine;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 타이틀 씬 Presenter.
    /// 시작/설정 버튼 처리, 세이브 슬롯 바인딩, 씬 전환 담당.
    /// 세이브 시스템 미구현으로 슬롯 1개 고정, 클릭 시 바로 게임씬 전환.
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
            SpawnSlot();
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

        private void SpawnSlot()
        {
            var slot = Instantiate(_slotPrefab, _view.SlotContent);
            slot.PlayEntrance(0f);

            // [TODO: SaveSystem 연결 후 실제 데이터 바인딩 및 다중 슬롯으로 교체]
            slot.Bind(new SaveSlotData { SlotName = "새 게임" });
            slot.OnSelected += _ => LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            _view.HideLoadPanel();
            await SceneLoader.Instance.LoadAsync(
                SceneName.Game,
                this.GetCancellationTokenOnDestroy()
            );
        }
    }
}