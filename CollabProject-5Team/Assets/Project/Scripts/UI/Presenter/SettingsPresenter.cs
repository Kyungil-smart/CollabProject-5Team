using R3;
using UnityEngine;
using UnityEngine.Audio;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// 설정 패널 공용 Presenter. 타이틀/게임 씬 모두 사용.
    /// BGM/SFX 볼륨 제어 및 PlayerPrefs 저장/복원.
    /// </summary>
    public sealed class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView  _view;
        [SerializeField] private AudioMixer    _audioMixer;

        private const string KEY_BGM = "BGMVolume";
        private const string KEY_SFX = "SFXVolume";

        // AudioMixer exposed parameter 이름 — Mixer에서 Expose한 파라미터명과 일치해야 함
        private const string MIXER_BGM = "BGM";
        private const string MIXER_SFX = "SFX";

        private void Start()
        {
            LoadSettings();
            BindSliders();
        }

        public void Show() => _view.Show();
        public void Hide() => _view.Hide();

        private void LoadSettings()
        {
            float bgm = PlayerPrefs.GetFloat(KEY_BGM, 1f);
            float sfx = PlayerPrefs.GetFloat(KEY_SFX, 1f);

            _view.SetBGMSlider(bgm);
            _view.SetSFXSlider(sfx);

            ApplyBGM(bgm);
            ApplySFX(sfx);
        }

        private void BindSliders()
        {
            _view.OnBGMChanged
                .Subscribe(value =>
                {
                    ApplyBGM(value);
                    PlayerPrefs.SetFloat(KEY_BGM, value);
                })
                .AddTo(this);

            _view.OnSFXChanged
                .Subscribe(value =>
                {
                    ApplySFX(value);
                    PlayerPrefs.SetFloat(KEY_SFX, value);
                })
                .AddTo(this);
        }

        /// <summary>
        /// AudioMixer는 로그 스케일로 볼륨을 받음.
        /// 슬라이더 0~1 값을 -80~0 dB로 변환.
        /// </summary>
        private void ApplyBGM(float value)
        {
            _audioMixer.SetFloat(MIXER_BGM, LinearToDecibel(value));
        }

        private void ApplySFX(float value)
        {
            _audioMixer.SetFloat(MIXER_SFX, LinearToDecibel(value));
        }

        private static float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        }
    }
}