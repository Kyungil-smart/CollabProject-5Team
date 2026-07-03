using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDevTycoon.UI
{
    public sealed class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;
        [SerializeField] private AlertView _alertView;

        private const string KEY_BGM_VOL = "BGMVolume";
        private const string KEY_SFX_VOL = "SFXVolume";
        private const string KEY_BGM_ON  = "BGMOn";
        private const string KEY_SFX_ON  = "SFXOn";

        private bool _isBGMOn;
        private bool _isSFXOn;
        private float _bgmVolume;
        private float _sfxVolume;

        private void Start()
        {
            _bgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOL, 100f);
            _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 100f);
            _isBGMOn   = PlayerPrefs.GetInt(KEY_BGM_ON, 1) == 1;
            _isSFXOn   = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;

            _view.SetBGMToggle(_isBGMOn);
            _view.SetSFXToggle(_isSFXOn);
            _view.SetBGMSlider(_bgmVolume);
            _view.SetSFXSlider(_sfxVolume);

            float bgmVol = _isBGMOn ? _bgmVolume : 0f;
            float sfxVol = _isSFXOn ? _sfxVolume : 0f;
            AudioManager.Instance?.SetAudioVolume(EAudioMixerType.BGM, Mathf.Max(bgmVol / 50f, 0.0001f));
            AudioManager.Instance?.SetAudioVolume(EAudioMixerType.SFX, Mathf.Max(sfxVol / 50f, 0.0001f));

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

        public void SetTitleButtonVisible(bool visible)
            => _view.SetTitleButtonVisible(visible);

        private void BindButtons()
        {
            _view.OnConfirmClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); Hide(); })
                .AddTo(this);

            _view.OnBGMToggleClicked
                .Subscribe(_ => OnBGMToggleClicked())
                .AddTo(this);

            _view.OnSFXToggleClicked
                .Subscribe(_ => OnSFXToggleClicked())
                .AddTo(this);

            _view.OnTitleClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); OnTitleClicked(); })
                .AddTo(this);
        }

        private void OnBGMToggleClicked()
        {
            AudioManager.Instance?.PlaySFXClick();
            _isBGMOn = !_isBGMOn;
            _view.SetBGMToggle(_isBGMOn);
            PlayerPrefs.SetInt(KEY_BGM_ON, _isBGMOn ? 1 : 0);
            float vol = _isBGMOn ? _bgmVolume : 0f;
            AudioManager.Instance?.SetAudioVolume(EAudioMixerType.BGM, Mathf.Max(vol / 50f, 0.0001f));
        }

        private void OnSFXToggleClicked()
        {
            AudioManager.Instance?.PlaySFXClick();
            _isSFXOn = !_isSFXOn;
            _view.SetSFXToggle(_isSFXOn);
            PlayerPrefs.SetInt(KEY_SFX_ON, _isSFXOn ? 1 : 0);
            float vol = _isSFXOn ? _sfxVolume : 0f;
            AudioManager.Instance?.SetAudioVolume(EAudioMixerType.SFX, Mathf.Max(vol / 50f, 0.0001f));
        }

        private void BindSliders()
        {
            _view.OnBGMChanged
                .Subscribe(v =>
                {
                    _bgmVolume = v;
                    PlayerPrefs.SetFloat(KEY_BGM_VOL, v);
                    if (_isBGMOn)
                        AudioManager.Instance?.SetAudioVolume(EAudioMixerType.BGM, Mathf.Max(v / 50f, 0.0001f));
                })
                .AddTo(this);

            _view.OnSFXChanged
                .Subscribe(v =>
                {
                    _sfxVolume = v;
                    PlayerPrefs.SetFloat(KEY_SFX_VOL, v);
                    if (_isSFXOn)
                        AudioManager.Instance?.SetAudioVolume(EAudioMixerType.SFX, Mathf.Max(v / 50f, 0.0001f));
                })
                .AddTo(this);
        }

        private void OnTitleClicked()
        {
            AudioManager.Instance?.PlaySFXAlert();
            _alertView.ShowConfirmPopup("타이틀로 이동하시겠습니까?", () =>
            {
                // [TODO: SceneLoader 확정 후 교체]
                SceneManager.LoadScene("TitleScene");
            });
        }
    }
}