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

        private float _bgmVolume = 100f;
        private float _sfxVolume = 100f;

        private void Start()
        {
            _view.SetBGMToggle(_isBGMOn);
            _view.SetSFXToggle(_isSFXOn);
            _view.SetBGMSlider(100f);
            _view.SetSFXSlider(100f);

            bool isGameScene = gameObject.scene.name == "GameScene";
            _view.SetTitleButtonVisible(isGameScene);

            BindButtons();
            BindSliders();
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
            float vol = _isBGMOn ? _bgmVolume : 0f;
            AudioManager.Instance?.SetAudioVolume(EAudioMixerType.BGM, Mathf.Max(vol / 50f, 0.0001f));
        }

        private void OnSFXToggleClicked()
        {
            _isSFXOn = !_isSFXOn;
            _view.SetSFXToggle(_isSFXOn);
            float vol = _isSFXOn ? _sfxVolume : 0f;
            AudioManager.Instance?.SetAudioVolume(EAudioMixerType.SFX, Mathf.Max(vol / 50f, 0.0001f));
        }

        private void BindSliders()
        {
            _view.OnBGMChanged
                .Subscribe(v =>
                {
                    _bgmVolume = v;
                    if (_isBGMOn)
                        AudioManager.Instance?.SetAudioVolume(EAudioMixerType.BGM, Mathf.Max(v / 50f, 0.0001f));
                })
                .AddTo(this);

            _view.OnSFXChanged
                .Subscribe(v =>
                {
                    _sfxVolume = v;
                    if (_isSFXOn)
                        AudioManager.Instance?.SetAudioVolume(EAudioMixerType.SFX, Mathf.Max(v / 50f, 0.0001f));
                })
                .AddTo(this);
        }

        private void OnTitleClicked()
        {
            _alertView.ShowConfirmPopup("타이틀로 이동하시겠습니까?", () =>
            {
                // [TODO: SceneLoader 확정 후 교체]
                SceneManager.LoadScene("TitleScene");
            });
        }

    }
}