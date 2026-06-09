using R3;
using UnityEngine;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// 설정 패널 공용 Presenter. 타이틀/게임 씬 모두 사용.
    /// BGM/SFX 연결은 추후 활성화.
    /// </summary>
    public sealed class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;
        [SerializeField] private AlertView    _alertView;

        // [추후 활성화]
        // [SerializeField] private AudioMixer _audioMixer;
        // private const string KEY_BGM   = "BGMVolume";
        // private const string KEY_SFX   = "SFXVolume";
        // private const string MIXER_BGM = "BGM";
        // private const string MIXER_SFX = "SFX";

        private void Start()
        {
            _view.OnGameQuitClicked
                .Subscribe(_ => OnGameQuitClicked())
                .AddTo(this);

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

        private void OnGameQuitClicked()
        {
            _alertView.ShowConfirmPopup("게임을 종료하시겠습니까?", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
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