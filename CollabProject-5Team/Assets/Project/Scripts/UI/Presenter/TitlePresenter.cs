using Cysharp.Threading.Tasks;
using GameDevTycoon.Core;
using GameDevTycoon.UI.Title;
using R3;
using UnityEngine;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 타이틀 씬 Presenter.
    /// 시작/불러오기/설정/종료 버튼 처리, 씬 전환 담당.
    /// </summary>
    public sealed class TitlePresenter : MonoBehaviour
    {
        [SerializeField] private TitleView         _view;
        [SerializeField] private SettingsPresenter _settingsPresenter;
        [SerializeField] private LoadPresenter _loadPresenter;

        [Header("BGM")]
        [SerializeField] private AudioClip _bgm;

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(_bgm);
            BindButtons();
        }

        private void BindButtons()
        {
            _view.OnStartClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXPositive(); LoadNewGameSceneAsync().Forget(); })
                .AddTo(this);

            _view.OnLoadClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _loadPresenter.Show(); })
                .AddTo(this);

            _view.OnSettingsClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _settingsPresenter.Show(); })
                .AddTo(this);

            _view.OnQuitClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXNegative(); Application.Quit(); })
                .AddTo(this);
        }

        private async UniTaskVoid LoadNewGameSceneAsync()
        {
            // [TODO: 회사 이름 설정 팝업 → 페이드아웃 → 씬 전환 순서로 교체]

            // 새 게임은 보내진 로드 슬롯 없음
            SaveLoadSystem.Instance.pendingLoadSlot = null;

            await SceneLoader.Instance.LoadAsync(SceneName.Game);
        }
    }
}
