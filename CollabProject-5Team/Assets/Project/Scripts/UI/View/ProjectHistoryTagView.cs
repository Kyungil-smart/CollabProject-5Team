using TMPro;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 과거 참여 프로젝트 태그 프리팹 바인딩.
    /// EmployeeDetailView.ProjectGroup.ProjectHistory에 동적 생성.
    /// </summary>
    public sealed class ProjectHistoryTagView : MonoBehaviour, IBindable<string>
    {
        [SerializeField] private TextMeshProUGUI _tagLabel;

        public void Bind(string projectName)
        {
            _tagLabel.text = projectName;
        }
    }
}