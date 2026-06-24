using R3;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.ProjectCompletedPopup Presenter.
    /// OnNight 발화 시 이번 주 새로 완료된 프로젝트를 감지해 팝업 표시.
    /// 확인 버튼 클릭 후 ReportPresenter로 흐름 이관.
    /// </summary>
    public sealed class ProjectCompletedPopupPresenter : MonoBehaviour
    {
        [SerializeField] private ProjectCompletedPopupView _view;
        [SerializeField] private ReportPresenter _reportPresenter;

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
            _view.OnConfirmClicked
                .Subscribe(_ => OnConfirmClicked())
                .AddTo(this);
        }

        private void OnNightStarted()
        {
            if (!Company.Instance.hasPendingCompletedProject)
            {
                ProceedToReport();
                return;
            }

            var completedProject = Company.Instance.pendingCompletedProject;
            Company.Instance.hasPendingCompletedProject = false;
            Company.Instance.pendingCompletedProject = null;
            ShowPopup(completedProject);
        }

        private void ShowPopup(ProjectCompleted record)
        {
            _view.Bind(
                projectName: record.projectName,
                grade: GradeToString(record.grade),
                completion: record.qualityScore,
                stability: record.stabilityScore,
                appeal: record.charmScore
            );
            _view.Show();
        }

        private void OnConfirmClicked()
        {
            _view.Hide();
            ProceedToReport();
        }

        private void ProceedToReport()
        {
            _reportPresenter.StartNightSequence();
        }

        private static string GradeToString(char grade) => grade switch
        {
            'S' => "갓겜",
            'A' => "명작",
            'B' => "평작",
            _ => "망겜",
        };
    }
}
