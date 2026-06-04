using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// 설정 패널 공용 View. 타이틀/게임 씬 모두 사용.
    /// BGM/SFX 슬라이더 값 변경 이벤트 발행 및 패널 Show/Hide.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _settingsPanel;

        [Header("Audio")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;

        public Observable<float> OnBGMChanged => _bgmSlider.OnValueChangedAsObservable();
        public Observable<float> OnSFXChanged => _sfxSlider.OnValueChangedAsObservable();
        public Observable<Unit>  OnCloseClicked => _closeButton.OnClickAsObservable();

        private void Awake()
        {
            _settingsPanel.SetActive(false);

            _closeButton.OnClickAsObservable()
                .Subscribe(_ => Hide())
                .AddTo(this);
        }

        public void Show()
        {
            _settingsPanel.SetActive(true);
            // [DoTween 페이드 연출 추가 예정]
        }

        public void Hide()
        {
            // [DoTween 페이드 연출 추가 예정]
            _settingsPanel.SetActive(false);
        }

        public void SetBGMSlider(float value) => _bgmSlider.value = value;
        public void SetSFXSlider(float value) => _sfxSlider.value = value;
    }
}