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
    /// SettingsPanel 제어는 HUDPresenter에서 SettingsPresenter를 통해 처리.
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        [Header("TopBar")]
        [SerializeField] private TextMeshProUGUI _timeLabel;
        [SerializeField] private TextMeshProUGUI _moneyLabel;

        [Header("Buttons")]
        [SerializeField] private Button _settingsButton;

        [Header("DayUI")]
        public GameObject _dayUI;
        [SerializeField] private Button _questIconButton;
        [SerializeField] private Button _workStartButton;

        [Header("DayUI — QuestBanner")]
        [SerializeField] private GameObject _questBanner;
        [SerializeField] private TextMeshProUGUI _questNameLabel;
        [SerializeField] private TextMeshProUGUI _questProgressLabel;

        [Header("NightUI")]
        [SerializeField] private GameObject _nightUI;
        [SerializeField] private Button _hrButton;
        [SerializeField] private Button _projectButton;
        [SerializeField] private Button _companyButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _nightQuitButton;

        public Observable<Unit> OnSettingsClicked => _settingsButton.OnClickAsObservable();
        public Observable<Unit> OnQuestIconClicked => _questIconButton.OnClickAsObservable();
        public Observable<Unit> OnWorkStartClicked => _workStartButton.OnClickAsObservable();
        public Observable<Unit> OnHRClicked => _hrButton.OnClickAsObservable();
        public Observable<Unit> OnProjectClicked => _projectButton.OnClickAsObservable();
        public Observable<Unit> OnCompanyClicked => _companyButton.OnClickAsObservable();
        public Observable<Unit> OnSaveClicked => _saveButton.OnClickAsObservable();
        public Observable<Unit> OnNightQuitClicked => _nightQuitButton.OnClickAsObservable();

        private void Awake()
        {
            _dayUI.SetActive(true);
            _nightUI.SetActive(false);
            _nightQuitButton.interactable = false;
            _questBanner.SetActive(false);
        }

        // [TODO: DateTimeManager에 year/month 데이터 추가 후 파라미터 확정]
        public void SetTimeLabel(string timeText)
        {
            _timeLabel.text = timeText;
        }

        public void SetMoneyLabel(int money)
        {
            _moneyLabel.text = $"{money:N0}G";
        }

        public void SetNightQuitInteractable(int activeProjectCount)
        {
            _nightQuitButton.interactable = activeProjectCount > 0;
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

        public void ShowQuestBanner(string questName, int current, int total)
        {
            _questNameLabel.text = questName;
            _questProgressLabel.text = $"{current}/{total}";
            _questBanner.SetActive(true);
        }

        public void HideQuestBanner() => _questBanner.SetActive(false);

        public void SetQuestBannerProgress(int current, int total)
        {
            _questProgressLabel.text = $"{current}/{total}";
        }

        // 퀘스트 완료 시 반투명 처리
        public void SetQuestBannerCompleted()
        {
            var group = _questBanner.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = 0.5f;
        }
    }
}