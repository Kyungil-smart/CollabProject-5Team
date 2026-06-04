using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// DayBottom 진척도 프레임 내 프로젝트 아이템 프리팹 바인딩.
    /// DayBottomView.ProjectListContent에 동적 생성.
    /// </summary>
    public sealed class ProjectProgressItemView : MonoBehaviour, IBindable<Project>
    {
        [SerializeField] private TextMeshProUGUI _projectNameLabel;
        [SerializeField] private Slider          _progressBar;
        [SerializeField] private TextMeshProUGUI _progressValueLabel;
        [SerializeField] private TextMeshProUGUI _remainDaysLabel;

        public void Bind(Project project)
        {
            _projectNameLabel.text = project.userNamed.Value;

            float progress = project.ProgressDayBar / 100f;
            _progressBar.value       = progress;
            _progressValueLabel.text = $"{project.ProgressDayBar:F1}%";

            int remainDays = project.DurationDays - project.day;
            _remainDaysLabel.text = remainDays > 0 ? $"D-{remainDays}" : "마감";
        }
    }
}