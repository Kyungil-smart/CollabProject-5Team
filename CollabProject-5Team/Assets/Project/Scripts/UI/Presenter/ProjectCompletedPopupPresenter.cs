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

        // 직전 OnNight 시점의 완료 프로젝트 수 — 새로 완료된 항목 감지용
        private int _prevCompletedCount;
        private ProjectCompleted _pendingRecord;

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
            _prevCompletedCount = Company.Instance.completedProjects.Count;

            _view.OnConfirmClicked
                .Subscribe(_ => OnConfirmClicked())
                .AddTo(this);
        }

        private void OnNightStarted()
        {
            var completed = Company.Instance.completedProjects;

            // 이번 OnNight에서 새로 추가된 프로젝트 확인
            if (completed.Count > _prevCompletedCount)
            {
                // 가장 마지막에 추가된 항목이 이번에 완료된 프로젝트
                _pendingRecord = completed[completed.Count - 1];
                _prevCompletedCount = completed.Count;

                ShowPopup(_pendingRecord);
            }
            else
            {
                _prevCompletedCount = completed.Count;
                ProceedToReport();
            }
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
            _pendingRecord = null;
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
            'B' => "양겜",
            _ => "망겜",
        };
    }
}