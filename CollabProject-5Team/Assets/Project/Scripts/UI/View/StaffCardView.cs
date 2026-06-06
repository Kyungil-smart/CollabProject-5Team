using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 인원 배치 직원 카드 프리팹 바인딩.
    /// Panel_StaffAssign.StaffGrid Content에 동적 생성.
    /// 직군별 테두리 색상은 배리언트(StaffCardView_Planning/Art/Dev)로 처리.
    /// </summary>
    public sealed class StaffCardView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Image           _profileIcon;
        [SerializeField] private Image           _departmentTagBG;
        [SerializeField] private TextMeshProUGUI _departmentTagLabel;
        [SerializeField] private TextMeshProUGUI _honorificLabel;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _abilityValue;
        [SerializeField] private GameObject      _deployOverlay;
        [SerializeField] private GameObject      _educationOverlay;
        [SerializeField] private GameObject      _stateOverlay;
        [SerializeField] private GameObject      _plusIcon;
        [SerializeField] private GameObject      _minusIcon;

        public void Bind(Employee employee)
        {
            var so      = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite      = so.iconNormal;
            _departmentTagLabel.text = RoleToString(so.role);
            _honorificLabel.text     = so.style;
            _nameLabel.text          = so.Name;
            _abilityValue.text       = mutable.ability.ToString();

            // [TODO: 교육 시스템 연결 후 교육 중 상태 처리]
            _educationOverlay.SetActive(false);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            _deployOverlay.SetActive(selected);
            _stateOverlay.SetActive(selected);
            _plusIcon.SetActive(!selected);
            _minusIcon.SetActive(selected);
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
    }
}