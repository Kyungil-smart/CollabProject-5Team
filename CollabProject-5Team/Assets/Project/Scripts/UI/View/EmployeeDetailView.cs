using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직원 상세 페이지 프리팹 바인딩.
    /// Panel_Detail, Panel_FireDetail, Panel_EducationDetail.DetailContent에 공통 재사용.
    /// TraitTagView 3개 고정 배치, ProjectHistoryTagView 동적 생성.
    /// </summary>
    public sealed class EmployeeDetailView : MonoBehaviour, IBindable<Employee>
    {
        [Header("기본 정보")]
        [SerializeField] private Image           _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameValue;
        [SerializeField] private DepartmentTagView _departmentTag;
        [SerializeField] private TextMeshProUGUI _honorificValue;
        [SerializeField] private MBTITagView     _mbtiTag;
        [SerializeField] private TextMeshProUGUI _salaryValue;
        [SerializeField] private TextMeshProUGUI _severanceValue;

        [Header("특성 — 3개 고정")]
        [SerializeField] private TraitTagView _mainTraitTag;
        [SerializeField] private TraitTagView _subTraitTag;
        [SerializeField] private TraitTagView _riskTraitTag;

        [Header("현재 상태")]
        [SerializeField] private TextMeshProUGUI _statusValue;

        [Header("스탯 바")]
        [SerializeField] private Slider          _abilityBar;
        [SerializeField] private TextMeshProUGUI _abilityValue;
        [SerializeField] private Slider          _motivationBar;
        [SerializeField] private TextMeshProUGUI _motivationValue;
        [SerializeField] private Slider          _fatigueBar;
        [SerializeField] private TextMeshProUGUI _fatigueValue;
        [SerializeField] private Slider          _loyaltyBar;
        [SerializeField] private TextMeshProUGUI _loyaltyValue;

        [Header("프로젝트 이력")]
        [SerializeField] private Transform       _projectHistoryContent;
        [SerializeField] private ProjectHistoryTagView _projectHistoryTagPrefab;

        public void Bind(Employee employee)
        {
            var so      = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = GetProfileSprite(so, mutable);
            _nameValue.text     = so.Name;
            _honorificValue.text = so.style;
            _salaryValue.text   = $"{so.weekSalary:N0}G";
            _severanceValue.text = $"{so.hiringCost:N0}G";

            _departmentTag.Bind(so.role);
            _mbtiTag.Bind(so.mbtiParsed);

            _mainTraitTag.Bind(so.mainTrait);
            _subTraitTag.Bind(so.subTrait);
            _riskTraitTag.Bind(so.riskTrait);

            _statusValue.text = GetStatusString(employee);

            _abilityBar.value    = mutable.ability;
            _abilityValue.text   = mutable.ability.ToString();
            _motivationBar.value = mutable.desire;
            _motivationValue.text = mutable.desire.ToString();
            _fatigueBar.value    = mutable.fatigue;
            _fatigueValue.text   = mutable.fatigue.ToString();
            _loyaltyBar.value    = mutable.loyalty;
            _loyaltyValue.text   = mutable.loyalty.ToString();

            RefreshProjectHistory(so);
        }

        private void RefreshProjectHistory(EmployeeImmutableData so)
        {
            foreach (Transform child in _projectHistoryContent)
                Destroy(child.gameObject);

            foreach (var completed in Company.Instance.completedProjects)
            {
                // [TODO: 직원별 참여 프로젝트 기록 데이터 구조 확정 후 필터링]
                var tag = Instantiate(_projectHistoryTagPrefab, _projectHistoryContent);
                tag.Bind(completed.projectName);
            }
        }

        private string GetStatusString(Employee employee)
        {
            foreach (var project in Company.Instance.projects)
            {
                if (project.GetAllEmployees().Contains(employee))
                    return $"{project.userNamed.Value} 진행중";
            }
            // [TODO: 교육 시스템 연결 후 교육활동 참여중 상태 추가]
            return "대기중";
        }

        private static Sprite GetProfileSprite(EmployeeImmutableData so, EmployeeMutableData mutable)
        {
            bool highFatigue = mutable.fatigue > 50;
            bool lowDesire   = mutable.desire  < 50;

            if (highFatigue && lowDesire) return so.iconCritical;
            if (highFatigue || lowDesire) return so.iconCaution;
            return so.iconNormal;
        }
    }
}