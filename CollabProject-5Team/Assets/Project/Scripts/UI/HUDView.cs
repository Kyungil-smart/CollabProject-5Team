using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_HUD 담당 View.
    /// TopBar 수치 표시, DayUI/NightUI 전환, 버튼 이벤트 발행.
    /// 낮/밤 전환 및 수치 갱신은 외부(Presenter)에서 호출.
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        [Header("TopBar")]
        [SerializeField] private TextMeshProUGUI _timeLabel;
        [SerializeField] private TextMeshProUGUI _moneyLabel;
        [SerializeField] private TextMeshProUGUI _reputationLabel;
        [SerializeField] private Button          _settingsButton;

        [Header("DayUI")]
        [SerializeField] private GameObject _dayUI;
        [SerializeField] private Button     _workStartButton;

        [Header("NightUI")]
        [SerializeField] private GameObject _nightUI;
        [SerializeField] private Button     _hrButton;
        [SerializeField] private Button     _projectButton;
        [SerializeField] private Button     _companyButton;
        [SerializeField] private Button     _saveButton;
        [SerializeField] private Button     _nightQuitButton;

        [Header("SettingsPanel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button     _gameQuitButton;

        public Observable<Unit> OnWorkStartClicked  => _workStartButton.OnClickAsObservable();
        public Observable<Unit> OnHRClicked         => _hrButton.OnClickAsObservable();
        public Observable<Unit> OnProjectClicked    => _projectButton.OnClickAsObservable();
        public Observable<Unit> OnCompanyClicked    => _companyButton.OnClickAsObservable();
        public Observable<Unit> OnSaveClicked       => _saveButton.OnClickAsObservable();
        public Observable<Unit> OnNightQuitClicked  => _nightQuitButton.OnClickAsObservable();
        public Observable<Unit> OnGameQuitClicked   => _gameQuitButton.OnClickAsObservable();

        private void Awake()
        {
            _dayUI.SetActive(true);
            _nightUI.SetActive(false);
            _settingsPanel.SetActive(false);

            _nightQuitButton.interactable = false;

            _settingsButton.OnClickAsObservable()
                .Subscribe(_ => ToggleSettingsPanel())
                .AddTo(this);
        }

        public void SetTimeLabel(int week, string dayName, bool isNight)
        {
            _timeLabel.text = $"{week}주차 {dayName} {(isNight ? "밤" : "낮")}";
        }

        public void SetMoneyLabel(long money)
        {
            _moneyLabel.text = $"{money:N0}G";
        }

        public void SetReputationLabel(int reputation)
        {
            _reputationLabel.text = reputation.ToString();
        }

        public void SetNightQuitInteractable(bool interactable)
        {
            _nightQuitButton.interactable = interactable;
        }

        public void SwitchToDay()
        {
            // [DoTween 페이드 연출 추가 예정]
            _nightUI.SetActive(false);
            _dayUI.SetActive(true);
        }

        public void SwitchToNight()
        {
            // [DoTween 페이드 연출 추가 예정]
            _dayUI.SetActive(false);
            _nightUI.SetActive(true);
        }

        private void ToggleSettingsPanel()
        {
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        }
    }
}