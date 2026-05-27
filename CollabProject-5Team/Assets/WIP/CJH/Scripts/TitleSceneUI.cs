using DG.Tweening;
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 타이틀 씬 UI 담당.
    /// 버튼 이벤트 발행 및 패널 Show/Hide.
    /// 씬 전환 및 데이터 처리는 외부에 위임.
    /// </summary>
    public sealed class TitleSceneUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _creditsButton;

        [Header("Load Panel")]
        [SerializeField] private GameObject _loadPanel;
        [SerializeField] private Button     _loadPanelCloseButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button     _settingsPanelCloseButton;

        [Header("Credits Panel")]
        [SerializeField] private GameObject _creditsPanel;
        [SerializeField] private Button     _creditsPanelCloseButton;

        public event Action      OnStartClicked;
        public event Action      OnExitClicked;
        public event Action<int> OnSlotSelected;

        private void Awake()
        {
            _loadPanel.SetActive(false);

            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_creditsPanel  != null) _creditsPanel.SetActive(false);
        }

        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            _startButton.OnClickAsObservable()
                .Subscribe(_ => ShowLoadPanel())
                .AddTo(this);

            _exitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    OnExitClicked?.Invoke();
                    QuitGame();
                })
                .AddTo(this);

            _loadPanelCloseButton.OnClickAsObservable()
                .Subscribe(_ => HideLoadPanel())
                .AddTo(this);

            if (_settingsButton != null)
                _settingsButton.OnClickAsObservable()
                    .Subscribe(_ => ShowSettingsPanel())
                    .AddTo(this);

            if (_settingsPanelCloseButton != null)
                _settingsPanelCloseButton.OnClickAsObservable()
                    .Subscribe(_ => HideSettingsPanel())
                    .AddTo(this);

            // 크레딧은 인스펙터에서 버튼 연결 후 활성화
            if (_creditsButton != null)
                _creditsButton.OnClickAsObservable()
                    .Subscribe(_ => ShowCreditsPanel())
                    .AddTo(this);

            if (_creditsPanelCloseButton != null)
                _creditsPanelCloseButton.OnClickAsObservable()
                    .Subscribe(_ => HideCreditsPanel())
                    .AddTo(this);
        }

        public void NotifySlotSelected(int slotIndex)
        {
            HideLoadPanel();
            OnSlotSelected?.Invoke(slotIndex);
        }

        public void ShowLoadPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            _loadPanel.SetActive(true);
            OnStartClicked?.Invoke();
        }

        public void HideLoadPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            _loadPanel.SetActive(false);
        }

        public void ShowSettingsPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
        }

        public void HideSettingsPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
        }

        public void ShowCreditsPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            if (_creditsPanel != null) _creditsPanel.SetActive(true);
        }

        public void HideCreditsPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            if (_creditsPanel != null) _creditsPanel.SetActive(false);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}