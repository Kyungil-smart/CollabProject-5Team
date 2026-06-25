using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("직군별 보고서 Next Buttons")]
        [SerializeField] Button[] _nextButtons;

        [Header("프리팹")]
        [SerializeField] private GameObject _employeeStatusMiniItemPrefab;

        // 직군 진행 순서
        static readonly Role[] RoleOrder = { Role.PLANNER, Role.ARTIST, Role.PROGRAMMER };

        private int _roleIndex;
        private List<Report> _currentReports;
        private Report _viewingReport;

        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            _view.OnCoverNextPageClicked
                .Subscribe(_ =>
                {
                    _view.ShowPanel(ReportPanel.EmployeeComment);
                    // [TODO: 담당자 EmployeeComment 패널 초기화 호출]
                })
                .AddTo(this);

            _view.OnAdoptClicked
                .Subscribe(_ => OnAdoptReport())
                .AddTo(this);

            _view.OnCancelClicked
                .Subscribe(_ => OnCancelDetail())
                .AddTo(this);

            _view.OnReportEndConfirmClicked
                .Subscribe(_ => OnReportEndConfirmed())
                .AddTo(this);

            for (int i = 0; i < _nextButtons.Length; i++)
            {
                int idx = i;
                _nextButtons[idx].OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        _roleIndex = idx + 1;
                        ShowReviewForCurrentRole();
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
            RefreshCoverInfo();
        }

        private void RefreshCoverInfo()
        {
            var dtm = DateTimeManager.Instance;
            int week = dtm.currentWeek.Value;

            string dateRange = $"{DateTimeManager.GetMonthWeekString((week - 1) * 5)}";
            _view.SetCoverInfo(dateRange, Company.Instance.CompanyName);
        }

        private void RefreshEmployeeStatusSlide(Employee employee)
        {
            for (int i = _view.SlidePreviewContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_view.SlidePreviewContent.GetChild(i).gameObject);

            var item = Instantiate(_employeeStatusMiniItemPrefab, _view.SlidePreviewContent);
            var bindable = item.GetComponent<IBindable<Employee>>();
            bindable?.Bind(employee);

            Debug.Log($"[ReportPresenter] RefreshEmployeeStatusSlide: {employee.so.Name}, IBindable: {bindable != null}");
        }

        /// <summary>
        /// EmployeeComment 완료 후 외부에서 호출하여 직군별 보고서 리뷰를 시작.
        /// </summary>
        public void StartReportReviewFlow()
        {
            _roleIndex = 0;
            ShowReviewForCurrentRole();

            for (int i = 0; i < _nextButtons.Length; i++)
                _nextButtons[i].interactable = false;
        }

        private void ShowReviewForCurrentRole()
        {
            if (_roleIndex >= RoleOrder.Length)
            {
                Company.Instance.curProject.ApproveSelectedReports();
                OnPersonalOpinionCompleted();
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
            _nextButtons[_roleIndex].interactable = true;
        }

        private void OnCancelDetail()
        {
            _view.PanelReportDetail.SetActive(false);
            _view.SetSlideInteractable(false);
        }

        /// <summary>
        /// 담당자 패널(PersonalOpinion)에서 모든 의견 처리 완료 시 외부 호출.
        /// </summary>
        public void OnPersonalOpinionCompleted()
        {
            _view.ShowPanel(ReportPanel.ReportEnd);
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
    }
}