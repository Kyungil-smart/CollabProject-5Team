using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// 설정 패널 공용 Presenter. 타이틀/게임 씬 모두 사용.
    /// BGM/SFX AudioMixer 연결은 추후 활성화.
    /// </summary>
    public sealed class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;
        [SerializeField] private AlertView _alertView;

        // [추후 활성화]
        // [SerializeField] private AudioMixer _audioMixer;
        // private const string KEY_BGM   = "BGMVolume";
        // private const string KEY_SFX   = "SFXVolume";
        // private const string MIXER_BGM = "BGM";
        // private const string MIXER_SFX = "SFX";

        private bool _isBGMOn = true;
        private bool _isSFXOn = true;

        private void Start()
        {
            _view.SetBGMToggle(_isBGMOn);
            _view.SetSFXToggle(_isSFXOn);

            BindButtons();

            // [추후 활성화]
            // LoadSettings();
            // BindSliders();
        }

        public void Show()
        {
            if (_view.IsVisible) return;
            _view.Show();
        }

        public void Hide() => _view.Hide();

        // 타이틀씬에서는 SetTitleButtonVisible(false) 호출
        public void SetTitleButtonVisible(bool visible)
            => _view.SetTitleButtonVisible(visible);

        private void BindButtons()
        {
            _view.OnConfirmClicked
                .Subscribe(_ => Hide())
                .AddTo(this);

            _view.OnBGMToggleClicked
                .Subscribe(_ => OnBGMToggleClicked())
                .AddTo(this);

            _view.OnSFXToggleClicked
                .Subscribe(_ => OnSFXToggleClicked())
                .AddTo(this);

            _view.OnTitleClicked
                .Subscribe(_ => OnTitleClicked())
                .AddTo(this);

            // [추후 활성화]
            // BindSliders();
        }

        private void OnBGMToggleClicked()
        {
            _isBGMOn = !_isBGMOn;
            _view.SetBGMToggle(_isBGMOn);

            // 슬라이더 0단계 시 자동 OFF — 슬라이더 연결 후 처리
            // [TODO: AudioMixer 연결 후 ApplyBGM 호출]
        }

        private void OnSFXToggleClicked()
        {
            _isSFXOn = !_isSFXOn;
            _view.SetSFXToggle(_isSFXOn);

            // [TODO: AudioMixer 연결 후 ApplySFX 호출]
        }

        private void OnTitleClicked()
        {
            _alertView.ShowConfirmPopup("타이틀로 이동하시겠습니까?", () =>
            {
                // [TODO: SceneLoader 확정 후 교체]
                SceneManager.LoadScene("TitleScene");
            });
        }

        // [추후 활성화]
        // private void LoadSettings() { ... }
        // private void BindSliders() { ... }
        // private void ApplyBGM(float value) { ... }
        // private void ApplySFX(float value) { ... }
        // private static float LinearToDecibel(float linear) => linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }
}