using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직군 태그 프리팹 바인딩.
    /// 직군별 색상은 Inspector에서 지정.
    /// </summary>
    public sealed class DepartmentTagView : MonoBehaviour, IBindable<Role>
    {
        [SerializeField] private Image           _tagBG;
        [SerializeField] private TextMeshProUGUI _tagLabel;

        [Header("직군별 색상")]
        [SerializeField] private Color _plannerColor;
        [SerializeField] private Color _programmerColor;
        [SerializeField] private Color _artistColor;
        [SerializeField] private Color _marketingColor;
        [SerializeField] private Color _qaColor;

        public void Bind(Role role)
        {
            _tagLabel.text = RoleToString(role);
            _tagBG.color   = RoleToColor(role);
        }

        private static string RoleToString(Role role) => role switch
        {
            Role.PLANNER    => "기획",
            Role.PROGRAMMER => "개발",
            Role.ARTIST     => "아트",
            Role.MARKETING  => "마케팅",
            Role.QA         => "QA",
            _               => string.Empty
        };

        private Color RoleToColor(Role role) => role switch
        {
            Role.PLANNER    => _plannerColor,
            Role.PROGRAMMER => _programmerColor,
            Role.ARTIST     => _artistColor,
            Role.MARKETING  => _marketingColor,
            Role.QA         => _qaColor,
            _               => Color.white
        };
    }
}