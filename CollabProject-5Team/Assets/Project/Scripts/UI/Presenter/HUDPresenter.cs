using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_HUD Presenter.
    /// DateTimeManager, Company의 ReactiveProperty를 구독해 HUDView에 반영.
    /// 버튼 이벤트를 받아 게임 로직으로 전달.
    /// </summary>
    public sealed class HUDPresenter : MonoBehaviour
    {
        [SerializeField] private HUDView _view;
        [SerializeField] private AlertView _alertView;
        [SerializeField] private SettingsPresenter _settingsPresenter;
        [SerializeField] private SavePresenter _savePresenter;
        [SerializeField] private SaveView _saveView;
        [SerializeField] private QuestPresenter _questPresenter;

        [Header("업무 시작 시 이동할 데스크탑 프리팹")]
        [SerializeField] private DeskInteract _desk;

        [Header("업무 시작 아이콘")]
        [SerializeField] private GameObject _workStartIconPrefab;
        [SerializeField] private Vector3 _workStartIconOffset = new Vector3(0f, 1.5f, 0f);

        [Header("BGM")]
        [SerializeField] private AudioClip _dayBGM;
        [SerializeField] private AudioClip _nightBGM;

        private WorkStartIcon _workStartIcon;

        private HRPresenter _hrPresenter;
        private ProjectPresenter _projectPresenter;
        private CompanyPresenter _companyPresenter;

        // NightUI 버튼 인덱스 — HUDView 배열 순서와 일치해야 함
        private const int IndexHR = 0;
        private const int IndexProject = 1;
        private const int IndexCompany = 2;
        private const int IndexSave = 3;

        private void Awake()
        {
            _hrPresenter = FindObjectOfType<HRPresenter>(true);
            _projectPresenter = FindObjectOfType<ProjectPresenter>(true);
            _companyPresenter = FindObjectOfType<CompanyPresenter>(true);
        }

        // 모든 Presenter의 Start() 완료 후 바인딩을 보장하기 위해 한 프레임 대기
        private async void Start()
        {
            DateTimeManager.OnReportEnd += SwitchToNight;
            DateTimeManager.OnDay += OnNewDay;

            await UniTask.Yield();
            BindButtons();
            BindQuestBanner();

            // 튜토리얼 중 Night로 시작하면 OnDay 이벤트가 오지 않으므로 미리 생성·등록
            if (TutorialManager.Instance != null && _workStartIconPrefab != null && _workStartIcon == null)
            {
                _desk = FindObjectOfType<DeskInteract>(true);
                if (_desk != null)
                {
                    _workStartIcon = Instantiate(_workStartIconPrefab, (RectTransform)_view.transform)
                        .GetComponent<WorkStartIcon>();
                    _workStartIcon.Button.onClick.AddListener(OnWorkStartClicked);
                    _workStartIcon.gameObject.SetActive(false);
                    TutorialManager.Instance.RegisterObject("WorkStartBubble", _workStartIcon.Button.gameObject);
                    _workStartIcon.SetTarget(_desk.transform, _workStartIconOffset);
                    _workStartIcon.gameObject.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            DateTimeManager.OnReportEnd -= SwitchToNight;
            DateTimeManager.OnDay -= OnNewDay;
        }

        public void SwitchToNight()
        {
            CloseAllBottomPopups();  // HR, Project, Company 닫기
            _settingsPresenter.Hide();  // 세팅도 같이 닫기
            _view.SwitchToNight();
            AudioManager.Instance?.PlayBGM(_nightBGM);
            RefreshHUD();
        }

        private void BindButtons()
        {
            _view.OnQuestIconClicked
                .Subscribe(_ => OnQuestIconClicked())
                .AddTo(this);

            _view.OnHRClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); OnNightButtonClicked(IndexHR, _hrPresenter); })
                .AddTo(this);

            _view.OnProjectClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); OnNightButtonClicked(IndexProject, _projectPresenter); })
                .AddTo(this);

            _view.OnCompanyClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); OnNightButtonClicked(IndexCompany, _companyPresenter); })
                .AddTo(this);

            _view.OnSaveClicked
                .Subscribe(_ => OnSaveClicked())
                .AddTo(this);

            _view.OnNightQuitClicked
                .Subscribe(_ => OnNightQuitClicked())
                .AddTo(this);

            _view.OnSettingsClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _settingsPresenter.Show(); })
                .AddTo(this);

            // Save 팝업 닫힐 때 버튼 선택 해제
            _saveView.OnCloseClicked
                .Subscribe(_ => _view.DeselectAllNightButtons())
                .AddTo(this);

        }

        private void BindQuestBanner()
        {
            QuestManager.Instance.dailyQuestState
                .Subscribe(OnDailyQuestStateChanged)
                .AddTo(this);

            QuestManager.Instance.dailyQuestProgress
                .Subscribe(progress =>
                {
                    DailyQuest quest = QuestManager.Instance.curDailyQuest;
                    if (quest == null) return;

                    _view.SetQuestBannerProgress(progress, quest.TargetCount);
                })
                .AddTo(this);

            if (StoryQuestManager.Instance != null)
            {
                StoryQuestManager.Instance.storyQuestState
                    .Subscribe(OnStoryQuestStateChanged)
                    .AddTo(this);

                StoryQuestManager.Instance.storyQuestProgress
                    .Subscribe(progress =>
                    {
                        StoryQuest quest = StoryQuestManager.Instance.curStoryQuest;
                        if (quest == null) return;

                        _view.SetQuestBannerProgress(progress, quest.TargetCount);
                    })
                    .AddTo(this);
            }

            if (EventQuestManager.Instance == null) return;

            EventQuestManager.Instance.eventQuestState
                .Subscribe(OnEventQuestStateChanged)
                .AddTo(this);

            EventQuestManager.Instance.eventQuestProgress
                .Subscribe(progress =>
                {
                    EventQuest quest = EventQuestManager.Instance.curEventQuest;
                    if (quest == null) return;

                    _view.SetQuestBannerProgress(progress, quest.TargetCount);
                })
                .AddTo(this);
        }

        private void OnDailyQuestStateChanged(QuestState state)
        {
            DailyQuest quest = QuestManager.Instance.curDailyQuest;
            if (quest == null) return;

            switch (state)
            {
                case QuestState.Playing:
                    _view.ShowQuestBanner(quest.so.Name, quest.curCount, quest.TargetCount);
                    break;

                case QuestState.End:
                    _view.SetQuestBannerCompleted();
                    break;

                case QuestState.Ready:
                    _view.HideQuestBanner();
                    break;
            }
        }

        private void OnStoryQuestStateChanged(QuestState state)
        {
            StoryQuest quest = StoryQuestManager.Instance.curStoryQuest;
            if (quest == null) return;

            switch (state)
            {
                case QuestState.Playing:
                    _view.ShowQuestBanner(
                        "스토리 퀘스트",
                        quest.so.questName,
                        quest.curCount,
                        quest.TargetCount);
                    break;

                case QuestState.End:
                    _view.SetQuestBannerCompleted();
                    break;

                case QuestState.Ready:
                    _view.HideQuestBanner();
                    break;
            }
        }

        private void OnEventQuestStateChanged(QuestState state)
        {
            EventQuest quest = EventQuestManager.Instance.curEventQuest;
            if (quest == null) return;

            switch (state)
            {
                case QuestState.Playing:
                    _view.ShowQuestBanner(
                        EventQuest.QuestTypeName,
                        EventQuest.QuestName,
                        quest.curCount,
                        quest.TargetCount);
                    break;

                case QuestState.End:
                    _view.SetQuestBannerCompleted();
                    break;

                case QuestState.Ready:
                    _view.HideQuestBanner();
                    break;
            }
        }

        /// <summary>
        /// DateTimeManager year/month 확정 후 시간 표시 형식 연결.
        /// </summary>
        public void RefreshHUD()
        {
            // [TODO: DateTimeManager year/month 데이터 확정 후 시간 표시 형식 연결]
            var dtm = DateTimeManager.Instance;
            _view.SetTimeLabel($"{dtm.currentWeek.Value}주 {dtm.GetDayName()}");
            _view.SetTimeIcon(dtm.currentTime == TimeOfDay.Day);
        }

        private void OnNewDay()
        {
            _view.SwitchToDay();
            AudioManager.Instance?.PlayBGM(_dayBGM);

            _desk = FindObjectOfType<DeskInteract>(true);

            if (_workStartIconPrefab != null && _desk != null)
            {
                if (_workStartIcon == null)
                {
                    _workStartIcon = Instantiate(_workStartIconPrefab, (RectTransform)_view.transform)
                        .GetComponent<WorkStartIcon>();
                    _workStartIcon.Button.onClick.AddListener(OnWorkStartClicked);
                    _workStartIcon.gameObject.SetActive(false);
                    TutorialManager.Instance?.RegisterObject("WorkStartBubble", _workStartIcon.Button.gameObject);
                }

                _workStartIcon.SetTarget(_desk.transform, _workStartIconOffset);
                _workStartIcon.gameObject.SetActive(true);
            }

            RefreshHUD();
        }

        private void OnQuestIconClicked()
        {
            AudioManager.Instance?.PlaySFXClick();
            TutorialManager.Instance?.CompletePunchHoleStep();
            if (_questPresenter.IsDetailVisible)
                _questPresenter.HideDetail();
            else
                _questPresenter.ShowDetailAsync().Forget();
        }

        private void OnWorkStartClicked()
        {
            AudioManager.Instance?.PlaySFXPositive();
            if (_workStartIcon != null)
                _workStartIcon.gameObject.SetActive(false);
            QuestManager.Instance.StartQuestForToday();
        }

        private void OnNightButtonClicked(int index, IBottomNightUI targetPresenter)
        {
            bool wasVisible = targetPresenter.IsVisible;

            CloseAllBottomPopups();

            if (!wasVisible)
            {
                targetPresenter.Show();
                _view.SelectNightButton(index);
            }
            else
            {
                _view.DeselectAllNightButtons();
            }
        }

        private void OnSaveClicked()
        {
            AudioManager.Instance?.PlaySFXClick();
            _view.SelectNightButton(IndexSave);
            _savePresenter.Show();
        }

        private void OnNightQuitClicked()
        {
            AudioManager.Instance?.PlaySFXClick();
            CloseAllBottomPopups();
            _view.SwitchToDay();
            DateTimeManager.Instance.OnClickEndDayButton().Forget();
        }

        private void CloseAllBottomPopups()
        {
            _view.DeselectAllNightButtons();

            foreach (var presenter in GetBottomPopupPresenters())
                presenter.Hide();
        }

        private IEnumerable<IBottomNightUI> GetBottomPopupPresenters()
        {
            var yielded = new HashSet<IBottomNightUI>();

            if (yielded.Add(_hrPresenter)) yield return _hrPresenter;
            if (yielded.Add(_projectPresenter)) yield return _projectPresenter;
            if (yielded.Add(_companyPresenter)) yield return _companyPresenter;

            //추후 IBottomNightUI가 추가로 존재하면 여기에 추가
        }
    }
}
