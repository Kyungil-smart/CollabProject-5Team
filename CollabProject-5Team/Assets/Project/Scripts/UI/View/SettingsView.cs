using R3;
using TMPro;
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

        [Header("Audio")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Buttons")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _bgmToggle;
        [SerializeField] private TextMeshProUGUI _bgmToggleLabel;
        [SerializeField] private Sprite _bgmToggleOnSprite;
        [SerializeField] private Sprite _bgmToggleOffSprite;
        [SerializeField] private Button _sfxToggle;
        [SerializeField] private TextMeshProUGUI _sfxToggleLabel;
        [SerializeField] private Sprite _sfxToggleOnSprite;
        [SerializeField] private Sprite _sfxToggleOffSprite;
        [SerializeField] private Button _titleButton;

        public Observable<Unit> OnConfirmClicked => _confirmButton.OnClickAsObservable();
        public Observable<Unit> OnBGMToggleClicked => _bgmToggle.OnClickAsObservable();
        public Observable<Unit> OnSFXToggleClicked => _sfxToggle.OnClickAsObservable();
        public Observable<Unit> OnTitleClicked => _titleButton.OnClickAsObservable();
        public Observable<float> OnBGMChanged => _bgmSlider.OnValueChangedAsObservable();
        public Observable<float> OnSFXChanged => _sfxSlider.OnValueChangedAsObservable();

        private void Awake()
        {
            _settingsPanel.SetActive(false);
            _titleButton.gameObject.SetActive(false);
        }

        public void Show() => _settingsPanel.SetActive(true);
        public void Hide() => _settingsPanel.SetActive(false);
        public bool IsVisible => _settingsPanel.activeSelf;

        public void SetBGMSlider(float value) { if (_bgmSlider != null) _bgmSlider.value = value; }
        public void SetSFXSlider(float value) { if (_sfxSlider != null) _sfxSlider.value = value; }

        public void SetBGMToggle(bool isOn)
        {
            _bgmToggleLabel.text = isOn ? "ON" : "OFF";
            var sprite = isOn ? _bgmToggleOnSprite : _bgmToggleOffSprite;
            if (sprite != null)
                _bgmToggle.GetComponent<Image>().sprite = sprite;
        }

        public void SetSFXToggle(bool isOn)
        {
            _sfxToggleLabel.text = isOn ? "ON" : "OFF";
            var sprite = isOn ? _sfxToggleOnSprite : _sfxToggleOffSprite;
            if (sprite != null)
                _sfxToggle.GetComponent<Image>().sprite = sprite;
        }

        // 타이틀씬에서는 숨김
        public void SetTitleButtonVisible(bool visible)
            => _titleButton.gameObject.SetActive(visible);
    }
}