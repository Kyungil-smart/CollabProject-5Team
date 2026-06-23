using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직원 카드 프리팹 바인딩.
    /// Tab_EmployeeManage.EmployeeGrid, Tab_Fire.FireList, Tab_Education.EducationList에 동적 생성.
    /// </summary>
    public sealed class EmployeeCardView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Image _profileIcon;
        [SerializeField] private GameObject _deployBadge;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _abilityValue;
        [SerializeField] private GameObject _educationOverlay;

        [Header("태그 앵커")]
        [SerializeField] private Transform _departmentTagAnchor;

        [Header("태그 프리팹")]
        [SerializeField] private DepartmentTagView _departmentTagPrefab;

        private DepartmentTagView _departmentTag;

        public void Bind(Employee employee)
        {
            var so = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = GetProfileSprite(so, mutable);
            _nameLabel.text = so.Name;
            _abilityValue.text = mutable.ability.ToString();

            if (_departmentTag == null)
                _departmentTag = Instantiate(_departmentTagPrefab, _departmentTagAnchor);
            _departmentTag.Bind(so.role);

            _deployBadge.SetActive(IsDeployed(employee));

            // [TODO: 교육 시스템 연결 후 교육 잔여 주수 표시]
            _educationOverlay.SetActive(false);
        }

        private static Sprite GetProfileSprite(EmployeeImmutableData so, EmployeeMutableData mutable)
        {
            bool highFatigue = mutable.fatigue > 50;
            bool lowDesire = mutable.desire < 50;

            if (highFatigue && lowDesire) return so.iconCritical;
            if (highFatigue || lowDesire) return so.iconCaution;
            return so.iconNormal;
        }

        private static bool IsDeployed(Employee employee)
        {
            if (Company.Instance.activeProjectCount.Value <= 0) return false;
            return Company.Instance.curProject.GetAllEmployees().Contains(employee);
        }

    }
}
