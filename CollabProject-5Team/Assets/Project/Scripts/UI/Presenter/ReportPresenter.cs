using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using GameDevTycoon.UI;
using GameDevTycoon.UI.Ingame;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Report Presenter.
    /// 금요일 밤 OnNight 이벤트 수신 시 보고서 시퀀스 시작.
    /// Cover → EmployeeComment → ReportReview(직군별 순차) → PersonalOpinion → ReportEnd 흐름 제어.
    /// Panel_EmployeeComment, Panel_ReportReview, Panel_ReportDetail, Panel_PersonalOpinion
    /// 내부 바인딩은 담당자 스크립트 연결 후 활성화.
    /// </summary>
    public sealed class ReportPresenter : MonoBehaviour
    {
        [SerializeField] private ReportView   _view;
        [SerializeField] private HUDPresenter _hudPresenter;

        [Header("프리팹")]
        [SerializeField] private GameObject _employeeStatusMiniItemPrefab;

        // 직군별 보고서 채택 완료 여부 추적
        private readonly Dictionary<Role, bool> _reportAdopted = new()
        {
            { Role.PLANNER,    false },
            { Role.PROGRAMMER, false },
            { Role.ARTIST,     false },
        };

        private void OnEnable()
        {
            DateTimeManager.OnNight += OnNightStarted;
        }

        private void OnDisable()
        {
            DateTimeManager.OnNight -= OnNightStarted;
        }

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

            _view.OnReportEndConfirmClicked
                .Subscribe(_ => OnReportEndConfirmed())
                .AddTo(this);
        }

        private void OnNightStarted()
        {
            ResetAdoptedState();
            RefreshCoverInfo();
            RefreshEmployeeStatusSlide();

            _view.Show();
        }

        private void RefreshCoverInfo()
        {
            var dtm = DateTimeManager.Instance;
            int week = dtm.currentWeek.Value;

            // 이번 주 날짜 범위 표시 — 금요일 밤 기준 해당 주차 월~금
            string dateRange = $"{DateTimeManager.GetDateString((week - 1) * 5)} ~ " +
                               $"{DateTimeManager.GetDateString((week - 1) * 5 + 4)}";

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
                // [TODO: IBindable<Employee> 연결 후 활성화]
                // item.GetComponent<IBindable<Employee>>().Bind(employee);
            }

            // 슬라이드 코멘트 — 직원 상태 요약
            int highFatigueCount = employees.Count(e => e.MutableData.fatigue >= 70);
            _view.SetSlideComment(highFatigueCount > 0
                ? $"피로도 주의 직원이 {highFatigueCount}명 있습니다."
                : "이번 주 전체 컨디션 양호합니다.");
        }

        /// <summary>
        /// 담당자 패널(ReportReview)에서 직군별 채택 완료 시 외부 호출.
        /// 3개 직군 모두 채택 완료 시 PersonalOpinion 패널로 자동 진행.
        /// </summary>
        public void OnReportAdopted(Role role)
        {
            _reportAdopted[role] = true;

            if (AllReportsAdopted())
                _view.ShowPanel(ReportPanel.PersonalOpinion);
        }

        /// <summary>
        /// 담당자 패널(PersonalOpinion)에서 모든 의견 처리 완료 시 외부 호출.
        /// </summary>
        public void OnPersonalOpinionCompleted()
        {
            _view.ShowPanel(ReportPanel.ReportEnd);
            _hudPresenter.SetNightQuitInteractable(true);
        }

        private void OnReportEndConfirmed()
        {
            // 보고서 승인 처리
            foreach (var project in Company.Instance.projects)
                project.ApproveSelectedReports();

            _hudPresenter.SetNightQuitInteractable(true);
            _view.Hide();
        }

        private void ResetAdoptedState()
        {
            _reportAdopted[Role.PLANNER]    = false;
            _reportAdopted[Role.PROGRAMMER] = false;
            _reportAdopted[Role.ARTIST]     = false;

            _hudPresenter.SetNightQuitInteractable(false);
        }

        private bool AllReportsAdopted()
            => _reportAdopted.Values.All(v => v);

        private static int GetRoleOrder(Role role) => role switch
        {
            Role.PLANNER    => 0,
            Role.ARTIST     => 1,
            Role.PROGRAMMER => 2,
            _               => 3,
        };
    }
}