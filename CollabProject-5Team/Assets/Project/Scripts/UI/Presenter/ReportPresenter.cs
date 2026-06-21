using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private ReportCardView _reportCardViewPrefab;

        // 직군 진행 순서
        static readonly Role[] RoleOrder = { Role.PLANNER, Role.ARTIST, Role.PROGRAMMER };

        private int _roleIndex;
        private List<Report> _currentReports;
        private List<ReportCardView> _currentCards = new();
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
                DateTimeManager.OnReportEnd?.Invoke();
                return;
            }
            _view.Show();

            RefreshCoverInfo();
            RefreshEmployeeStatusSlide();
        }


        private void RefreshCoverInfo()
        {
            var dtm = DateTimeManager.Instance;
            int week = dtm.currentWeek.Value;

            // 이번 주 날짜 범위 표시 — 금요일 밤 기준 해당 주차 월~금
            string dateRange = $"{DateTimeManager.GetMonthWeekString((week - 1) * 5)}";


            _view.SetCoverInfo(dateRange, Company.Instance.Name);
        }

        private void RefreshEmployeeStatusSlide()
        {
            foreach (Transform child in _view.SlidePreviewContent)
                Destroy(child.gameObject);

            // 직군순(기획→아트→개발) + 이름순 정렬
            var employees = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                .OrderBy(e => GetRoleOrder(e.so.role))
                .ThenBy(e => e.so.Name)
                .ToList();

            foreach (var employee in employees)
            {
                var item = Instantiate(_employeeStatusMiniItemPrefab, _view.SlidePreviewContent);
                item.GetComponent<IBindable<Employee>>().Bind(employee);
            }

            // 슬라이드 코멘트 — 직원 상태 요약
            int highFatigueCount = employees.Count(e => e.MutableData.fatigue >= 70);
            _view.SetSlideComment(highFatigueCount > 0
                ? $"피로도 주의 직원이 {highFatigueCount}명 있습니다."
                : "이번 주 전체 컨디션 양호합니다.");
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
                // 모든 직군 완료
                // 보고서 승인 처리
                Company.Instance.curProject.ApproveSelectedReports();
                // PersonalOpinion해야하지만 미완이라 그냥 바로 완료
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

            // 해당 패널만 활성화, Detail 패널 닫기
            _view.ShowReportReviewPanel(_roleIndex);
            _view.PanelReportDetail.SetActive(false);

            // 이전 카드 제거 후 재생성
            var content = _view.ReportReviewContents[_roleIndex];
            foreach (Transform child in content)
                Destroy(child.gameObject);

            _currentCards.Clear();
            foreach (var report in _currentReports)
            {
                var card = Instantiate(_reportCardViewPrefab, content);
                card.Bind(report);
                card.OnCardClicked
                    .Subscribe(r => ShowDetail(r))
                    .AddTo(this);
                _currentCards.Add(card);
            }
        }

        private void ShowDetail(Report report)
        {
            _viewingReport = report;
            _view.SetDetailInfo(report);
            _view.PanelReportDetail.SetActive(true);
        }

        private void OnAdoptReport()
        {
            if (_viewingReport == null) return;

            Company.Instance.curProject.SelectReport(_viewingReport);
            _viewingReport = null;

            foreach (var card in _currentCards)
                card.SetDisabled(true);

            _view.PanelReportDetail.SetActive(false);

            _nextButtons[_roleIndex].interactable = true;
        }

        private void OnCancelDetail()
        {
            _view.PanelReportDetail.SetActive(false);
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
            DateTimeManager.OnReportEnd?.Invoke();
        }

        // ── 헬퍼 ──
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
