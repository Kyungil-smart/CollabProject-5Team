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
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _progressValueLabel;

        public void Bind(Project project)
        {
            _projectNameLabel.text = project.userNamed.Value;
            _progressBar.value = project.ProgressDayBar / 100f;
            _progressValueLabel.text = $"{project.ProgressDayBar:F1}%";
        }
    }
}