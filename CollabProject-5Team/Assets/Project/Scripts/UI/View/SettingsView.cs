using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// 설정 패널 공용 View. 타이틀/게임 씬 모두 사용.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _settingsPanel;

        [Header("Audio — 추후 활성화")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _gameQuitButton;

        public Observable<Unit> OnCloseClicked    => _closeButton.OnClickAsObservable();
        public Observable<Unit> OnGameQuitClicked => _gameQuitButton.OnClickAsObservable();

        // [추후 활성화]
        // public Observable<float> OnBGMChanged => _bgmSlider.OnValueChangedAsObservable();
        // public Observable<float> OnSFXChanged => _sfxSlider.OnValueChangedAsObservable();

        private void Awake()
        {
            _settingsPanel.SetActive(false);

            if (_bgmSlider != null) _bgmSlider.gameObject.SetActive(false);
            if (_sfxSlider != null) _sfxSlider.gameObject.SetActive(false);

            _closeButton.OnClickAsObservable()
                .Subscribe(_ => Hide())
                .AddTo(this);
        }

        public void Show() => _settingsPanel.SetActive(true);
        public void Hide() => _settingsPanel.SetActive(false);
        public bool IsVisible => _settingsPanel.activeSelf;

        public void SetBGMSlider(float value) { if (_bgmSlider != null) _bgmSlider.value = value; }
        public void SetSFXSlider(float value) { if (_sfxSlider != null) _sfxSlider.value = value; }
    }
}