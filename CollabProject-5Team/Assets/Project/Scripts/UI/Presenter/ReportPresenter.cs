using System.Collections.Generic;
using R3;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Report Presenter.
    /// 금요일 밤 OnNight 이벤트 수신 시 보고서 시퀀스 시작.
    /// Cover → EmployeeComment → ReportReview(직군별 순차) → PersonalOpinion → ReportEnd 흐름 제어.
    /// </summary>
    public sealed class ReportPresenter : MonoBehaviour
    {
        [SerializeField] private ReportView _view;
        [SerializeField] private HUDPresenter _hudPresenter;

        [Header("프리팹")]
        [SerializeField] private EmployeeStatusMiniItemView _employeeStatusMiniItemPrefab;

        [Header("Agenda")]
        [SerializeField] private List<AgendaSO> _allAgendas = new();

        // 직군 진행 순서
        static readonly Role[] RoleOrder = { Role.PLANNER, Role.ARTIST, Role.PROGRAMMER };

        private int _roleIndex;
        private List<Report> _currentReports;
        private Report _viewingReport;
        private readonly Dictionary<int, List<AgendaSO>> _agendasByGrade = new();
        private readonly List<PersonalAgendaRequest> _personalAgendaQueue = new();
        private int _personalAgendaIndex;
        private PersonalAgendaRequest _currentPersonalAgenda;

        private void Start()
        {
            InitAgendaList();
            BindButtons();
        }

        private void InitAgendaList()
        {
            _agendasByGrade.Clear();

            foreach (AgendaSO agenda in _allAgendas)
            {
                if (agenda == null) continue;

                if (!_agendasByGrade.TryGetValue(agenda.grade, out List<AgendaSO> agendas))
                {
                    agendas = new List<AgendaSO>();
                    _agendasByGrade[agenda.grade] = agendas;
                }

                agendas.Add(agenda);
            }
        }

        private void BindButtons()
        {
            _view.OnCoverNextPageClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowPanel(ReportPanel.EmployeeComment);
                    _view.SetSlideInteractable(false);
                })
                .AddTo(this);

            _view.OnAdoptClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXPositive(); OnAdoptReport(); })
                .AddTo(this);

            _view.OnCancelClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXNegative(); OnCancelDetail(); })
                .AddTo(this);

            _view.OnReportEndConfirmClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXPositive(); OnReportEndConfirmed(); })
                .AddTo(this);

            _view.OnPersonalOpinionAdoptClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXPositive(); OnAdoptPersonalOpinion(); })
                .AddTo(this);

            _view.OnPersonalOpinionBackClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXNegative(); ShowNextPersonalOpinion(); })
                .AddTo(this);

            for (int i = 0; i < _view.NextButtonCount; i++)
            {
                int idx = i;
                _view.OnNextButtonClicked(idx)
                    .Subscribe(_ =>
                    {
                        AudioManager.Instance?.PlaySFXClick();
                        _roleIndex = idx + 1;
                        ShowReviewForCurrentRole();
                    })
                    .AddTo(this);
            }

            for (int i = 0; i < _view.ReAdoptButtonCount; i++)
            {
                int idx = i;
                _view.OnReAdoptButtonClicked(idx)
                    .Subscribe(_ =>
                    {
                        AudioManager.Instance?.PlaySFXClick();
                        OnReAdoptReport(idx);
                    })
                    .AddTo(this);
            }
        }

        /// <summary>
        /// 금요일 밤 보고서 시퀀스 시작. ProjectCompletedPopupPresenter에서 호출.
        /// </summary>
        public void StartNightSequence()
        {
            if (Company.Instance.activeProjectCount.Value < 1)
            {
                SaveLoadSystem.Instance.SaveGame(0); // 밤 자동 저장
                DateTimeManager.OnReportEnd?.Invoke();
                return;
            }

            _view.Show();
            _view.SetSlideVisible(true);
            _view.SetSlideInteractable(false);
            RefreshCoverInfo();
        }

        private void RefreshCoverInfo()
        {
            var dtm = DateTimeManager.Instance;
            int week = dtm.currentWeek.Value;

            string dateRange = $"{DateTimeManager.GetMonthWeekString((week - 1) * 5)}";
            _view.SetCoverInfo(dateRange, Company.Instance.CompanyName);
        }

        private void ClearEmployeeStatusSlide()
        {
            for (int i = _view.SlidePreviewContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_view.SlidePreviewContent.GetChild(i).gameObject);
        }

        private void RefreshEmployeeStatusSlide(Employee employee)
        {
            ClearEmployeeStatusSlide();

            var item = Instantiate(_employeeStatusMiniItemPrefab, _view.SlidePreviewContent);
            item.Bind(employee);
        }

        /// <summary>
        /// EmployeeComment 완료 후 외부에서 호출하여 직군별 보고서 리뷰를 시작.
        /// </summary>
        public void StartReportReviewFlow()
        {
            if (Company.Instance.activeProjectCount.Value > 0)
            {
                foreach (var employee in Company.Instance.curProject.GetAllEmployees())
                {
                    employee.SaveCurrentData();
                }
            }

            _roleIndex = 0;
            ShowReviewForCurrentRole();

            for (int i = 0; i < _view.NextButtonCount; i++)
                _view.SetNextButtonInteractable(i, false);
        }

        private void ShowReviewForCurrentRole()
        {
            if (_roleIndex >= RoleOrder.Length)
            {
                Company.Instance.curProject.ApproveSelectedReports();
                StartPersonalOpinionFlow();
                return;
            }

            Role currentRole = RoleOrder[_roleIndex];
            _currentReports = GetReportsByRole(currentRole);

            if (_currentReports == null || _currentReports.Count == 0)
            {
                Debug.Log($"[ReportPresenter] {currentRole} 보고서 없음, 다음 직군으로 넘어감");
                _roleIndex++;
                ShowReviewForCurrentRole();
                return;
            }

            ReportPanel panel = _roleIndex switch
            {
                0 => ReportPanel.ReportReviewPlanner,
                1 => ReportPanel.ReportReviewArtist,
                2 => ReportPanel.ReportReviewProgrammer,
                _ => ReportPanel.ReportReviewPlanner,
            };
            _view.ShowPanel(panel);
            _view.PanelReportDetail.SetActive(false);
            _view.SetSlideInteractable(false);

            _view.BindReviewCards(_roleIndex, _currentReports, ShowDetail);
        }

        private void ShowDetail(Report report)
        {
            AudioManager.Instance?.PlaySFXClick();
            _viewingReport = report;
            _view.SetDetailInfo(report);
            _view.PanelReportDetail.SetActive(true);
            _view.SetSlideInteractable(true);
            RefreshEmployeeStatusSlide(report.owner);

            Debug.Log($"[ReportPresenter] ShowDetail: {report.owner.so.Name}, SlideContent 자식 수: {_view.SlidePreviewContent.childCount}");
        }

        private void OnAdoptReport()
        {
            if (_viewingReport == null) return;

            Company.Instance.curProject.SelectReport(_viewingReport);
            _viewingReport = null;

            var cards = _view.GetCards(_roleIndex);
            foreach (var card in cards)
                card.SetDisabled(true);

            _view.PanelReportDetail.SetActive(false);
            _view.SetSlideInteractable(false);
            _view.SetNextButtonInteractable(_roleIndex, true);
            ClearEmployeeStatusSlide();
        }

        private void OnCancelDetail()
        {
            _view.PanelReportDetail.SetActive(false);
            _view.SetSlideInteractable(false);
            ClearEmployeeStatusSlide();
        }

        // 채택된 보고서를 취소하고 해당 직군 카드를 다시 선택 가능한 상태로 되돌림
        private void OnReAdoptReport(int roleIndex)
        {
            Role role = RoleOrder[roleIndex];

            if (!Company.Instance.curProject.selectedReports.ContainsKey(role))
                return;

            Company.Instance.curProject.DeselectReport(role);

            var cards = _view.GetCards(roleIndex);
            foreach (var card in cards)
                card.SetDisabled(false);

            _view.SetNextButtonInteractable(roleIndex, false);
        }

        /// <summary>
        /// 담당자 패널(PersonalOpinion)에서 모든 의견 처리 완료 시 외부 호출.
        /// </summary>
        private void StartPersonalOpinionFlow()
        {
            BuildPersonalAgendaQueue();
            _personalAgendaIndex = 0;

            if (_personalAgendaQueue.Count == 0)
            {
                OnPersonalOpinionCompleted();
                return;
            }

            ShowCurrentPersonalOpinion();
        }

        private void BuildPersonalAgendaQueue()
        {
            _personalAgendaQueue.Clear();

            var candidates = new List<Employee>(_EmployeeManager.Instance.haveEmployees.haveEmployeeList);
            int count = Mathf.Min(Random.Range(1, 3), candidates.Count);

            for (int i = 0; i < count; i++)
            {
                int employeeIndex = Random.Range(0, candidates.Count);
                Employee employee = candidates[employeeIndex];
                candidates.RemoveAt(employeeIndex);

                AgendaSO agenda = PickAgenda(employee);
                if (agenda != null)
                    _personalAgendaQueue.Add(new PersonalAgendaRequest(employee, agenda));
            }
        }

        private AgendaSO PickAgenda(Employee employee)
        {
            int grade = ReportPolicy.PickAgendaGradeByLoyalty(employee.MutableData.loyalty);
            if (_agendasByGrade.TryGetValue(grade, out List<AgendaSO> agendas) && agendas.Count > 0)
                return agendas[Random.Range(0, agendas.Count)];

            return _allAgendas.Count > 0 ? _allAgendas[Random.Range(0, _allAgendas.Count)] : null;
        }

        private void ShowCurrentPersonalOpinion()
        {
            if (_personalAgendaIndex >= _personalAgendaQueue.Count)
            {
                OnPersonalOpinionCompleted();
                return;
            }

            _currentPersonalAgenda = _personalAgendaQueue[_personalAgendaIndex];
            _view.SetPersonalOpinionInfo(_currentPersonalAgenda.employee, _currentPersonalAgenda.agenda);
            _view.ShowPanel(ReportPanel.PersonalOpinion);
            _view.SetSlideInteractable(true);
            RefreshEmployeeStatusSlide(_currentPersonalAgenda.employee);
        }

        private void ShowNextPersonalOpinion()
        {
            _personalAgendaIndex++;
            ShowCurrentPersonalOpinion();
        }

        private void OnAdoptPersonalOpinion()
        {
            Employee employee = _currentPersonalAgenda.employee;
            AgendaSO agenda = _currentPersonalAgenda.agenda;

            Company.Instance.gold.Value -= agenda.cost;
            Company.Instance.curManagementStatus.otherExpense += agenda.cost;
            Company.Instance.cumulativeManagementStatus.otherExpense += agenda.cost;
            Company.Instance.curManagementStatus.Recalculate();
            Company.Instance.cumulativeManagementStatus.Recalculate();

            if (Random.value <= agenda.sucessRate)
            {
                employee.AddAbilityDelta(10);
                employee.MutableData.desire += 5;
            }
            else
            {
                employee.MutableData.desire -= 5;
            }

            _hudPresenter?.RefreshHUD();
            ShowNextPersonalOpinion();
        }

        public void OnPersonalOpinionCompleted()
        {
            _view.SetSlideInteractable(false);
            _view.ShowPanel(ReportPanel.ReportEnd);
            ClearEmployeeStatusSlide();
        }

        private void OnReportEndConfirmed()
        {
            _view.Hide();
            SaveLoadSystem.Instance.SaveGame(0); // 밤 자동 저장
            DateTimeManager.OnReportEnd?.Invoke();
        }

        private List<Report> GetReportsByRole(Role role)
        {
            var result = new List<Report>();
            foreach (var r in Company.Instance.curProject.pendingReports)
            {
                if (r.role == role) result.Add(r);
            }
            return result;
        }

        private static int GetRoleOrder(Role role) => role switch
        {
            Role.PLANNER => 0,
            Role.ARTIST => 1,
            Role.PROGRAMMER => 2,
            _ => 3,
        };

        private readonly struct PersonalAgendaRequest
        {
            public readonly Employee employee;
            public readonly AgendaSO agenda;

            public PersonalAgendaRequest(Employee employee, AgendaSO agenda)
            {
                this.employee = employee;
                this.agenda = agenda;
            }
        }
    }
}