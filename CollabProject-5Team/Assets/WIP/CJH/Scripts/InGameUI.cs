// using DG.Tweening; // TODO: DoTween 패키지 설치 후 주석 해제

using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// InGame Scene UI 담당.
    /// 버튼 이벤트 발행, 날짜 표시, 낮/밤 전환, 보고서 패널 Show/Hide.
    /// 날짜 계산 및 게임 로직은 외부에 위임.
    /// </summary>
    public sealed class InGameUI : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI _timeLabel;

        [Header("Day UI")]
        [SerializeField] private GameObject _dayUI;
        [SerializeField] private Button     _workStartButton;
        [SerializeField] private Button     _dayQuitButton;

        [Header("Night UI")]
        [SerializeField] private GameObject _nightUI;
        [SerializeField] private Button     _reportButton;
        [SerializeField] private Button     _nightQuitButton;

        [Header("Report Panel")]
        [SerializeField] private GameObject      _reportPanel;
        [SerializeField] private Button          _reportCloseButton;
        [SerializeField] private Button          _reportNextButton;
        [SerializeField] private TextMeshProUGUI _reportText;

        public event Action OnWorkStartClicked;
        public event Action OnDayQuitClicked;
        public event Action OnNightQuitClicked;
        public event Action OnReportNextClicked;

        private void Awake()
        {
            _dayUI.SetActive(true);
            _nightUI.SetActive(false);
            _reportPanel.SetActive(false);

            _dayQuitButton.interactable   = false;
            _nightQuitButton.interactable = false;

            BindButtons();
        }

        private void BindButtons()
        {
            _workStartButton.OnClickAsObservable()
                .Subscribe(_ => OnWorkStartClicked?.Invoke())
                .AddTo(this);

            _dayQuitButton.OnClickAsObservable()
                .Subscribe(_ => OnDayQuitClicked?.Invoke())
                .AddTo(this);

            _reportButton.OnClickAsObservable()
                .Subscribe(_ => ShowReportPanel())
                .AddTo(this);

            _reportCloseButton.OnClickAsObservable()
                .Subscribe(_ => HideReportPanel())
                .AddTo(this);

            _reportNextButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    HideReportPanel();
                    OnReportNextClicked?.Invoke();
                })
                .AddTo(this);

            _nightQuitButton.OnClickAsObservable()
                .Subscribe(_ => OnNightQuitClicked?.Invoke())
                .AddTo(this);
        }

        public void SetTime(int week, string dayName, bool isNight)
        {
            if (_timeLabel == null) return;
            _timeLabel.text = $"{week}주차 {dayName} {(isNight ? "밤" : "낮")}";
        }

        public void SetDayQuitButtonActive(bool active)
            => _dayQuitButton.interactable = active;

        public void SetNightQuitButtonActive(bool active)
            => _nightQuitButton.interactable = active;

        public void SwitchToNight()
        {
            // [DoTween 설치 후 페이드 연출 추가]
            _dayUI.SetActive(false);
            _nightUI.SetActive(true);
        }

        public void SwitchToDay()
        {
            // [DoTween 설치 후 페이드 연출 추가]
            _nightUI.SetActive(false);
            _dayUI.SetActive(true);
        }

        public void SetReportText(string text)
        {
            if (_reportText != null)
                _reportText.text = text;
        }

        public void ShowReportPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            _reportPanel.SetActive(true);
        }

        public void HideReportPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            _reportPanel.SetActive(false);
        }
    }
}