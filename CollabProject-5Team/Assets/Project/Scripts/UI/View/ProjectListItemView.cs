using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 진행 프로젝트 목록 아이템 프리팹 바인딩.
    /// Tab_InProgress.InProgressList Content에 동적 생성.
    /// 상태별 배경색은 Inspector에서 지정.
    /// </summary>
    public sealed class ProjectListItemView : MonoBehaviour, IBindable<Project>
    {
        [SerializeField] private Image           _itemFrame;
        [SerializeField] private TextMeshProUGUI _projectNumLabel;
        [SerializeField] private TextMeshProUGUI _projectNameLabel;
        [SerializeField] private TextMeshProUGUI _statusValue;

        [Header("상태별 색상")]
        [SerializeField] private Color _activeColor;
        [SerializeField] private Color _inactiveColor;

        public void Bind(Project project)
        {
            _projectNameLabel.text = project.userNamed.Value;

            bool isFinished = project.isFinished.Value;
            _statusValue.text  = isFinished ? "서비스" : "제작 중";
            _itemFrame.color   = isFinished ? _activeColor : _activeColor;
        }

        public void SetNumber(int number)
        {
            _projectNumLabel.text = number.ToString();
        }

        public void SetInactive()
        {
            _itemFrame.color  = _inactiveColor;
            _statusValue.text = "서비스종료";
        }
    }
}