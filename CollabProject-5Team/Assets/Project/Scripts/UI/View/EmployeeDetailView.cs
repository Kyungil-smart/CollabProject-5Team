using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직원 상세 페이지 프리팹 바인딩.
    /// Panel_Detail, Panel_FireDetail, Panel_EducationDetail.DetailContent에 공통 재사용.
    /// 태그들은 앵커 위치에 프리팹으로 동적 생성.
    /// </summary>
    public sealed class EmployeeDetailView : MonoBehaviour, IBindable<Employee>
    {
        [Header("기본 정보")]
        [SerializeField] private Image _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameValue;
        [SerializeField] private TextMeshProUGUI _honorificValue;
        [SerializeField] private TextMeshProUGUI _salaryValue;
        [SerializeField] private TextMeshProUGUI _severanceValue;

        [Header("태그 앵커")]
        [SerializeField] private Transform _departmentTagAnchor;
        [SerializeField] private Transform _mbtiTagAnchor;
        [SerializeField] private Transform _mainTraitTagAnchor;
        [SerializeField] private Transform _subTraitTagAnchor;
        [SerializeField] private Transform _riskTraitTagAnchor;

        [Header("태그 프리팹")]
        [SerializeField] private DepartmentTagView _departmentTagPrefab;
        [SerializeField] private MBTITagView _mbtiTagPrefab;
        [SerializeField] private TraitTagView _traitTagPrefab;

        [Header("현재 상태")]
        [SerializeField] private TextMeshProUGUI _statusValue;

        [Header("스탯 바")]
        [SerializeField] private Slider _abilityBar;
        [SerializeField] private TextMeshProUGUI _abilityValue;
        [SerializeField] private Slider _motivationBar;
        [SerializeField] private TextMeshProUGUI _motivationValue;
        [SerializeField] private Slider _fatigueBar;
        [SerializeField] private TextMeshProUGUI _fatigueValue;
        [SerializeField] private Slider _loyaltyBar;
        [SerializeField] private TextMeshProUGUI _loyaltyValue;

        [Header("프로젝트 이력")]
        [SerializeField] private Transform _projectHistoryAnchor;
        [SerializeField] private ProjectHistoryTagView _projectHistoryTagPrefab;

        public void Bind(Employee employee)
        {
            var so = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = GetProfileSprite(so, mutable);
            _nameValue.text = so.Name;
            _honorificValue.text = so.style;
            _salaryValue.text = $"{so.weekSalary:N0}G";
            _severanceValue.text = $"{so.hiringCost:N0}G";

            SpawnTag(_departmentTagAnchor, _departmentTagPrefab, so.role);
            SpawnTag(_mbtiTagAnchor, _mbtiTagPrefab, so.mbtiParsed);
            SpawnTraitTag(_mainTraitTagAnchor, so.mainTrait);
            SpawnTraitTag(_subTraitTagAnchor, so.subTrait);
            SpawnTraitTag(_riskTraitTagAnchor, so.riskTrait);

            _statusValue.text = GetStatusString(employee);

            _abilityBar.value = mutable.ability;
            _abilityValue.text = mutable.ability.ToString();
            _motivationBar.value = mutable.desire;
            _motivationValue.text = mutable.desire.ToString();
            _fatigueBar.value = mutable.fatigue;
            _fatigueValue.text = mutable.fatigue.ToString();
            _loyaltyBar.value = mutable.loyalty;
            _loyaltyValue.text = mutable.loyalty.ToString();

            RefreshProjectHistory(employee);
        }

        private void RefreshProjectHistory(Employee employee)
        {
            ClearAnchor(_projectHistoryAnchor);

            if (employee.completedProjectNames == null) return;

            foreach (var projectName in employee.completedProjectNames)
            {
                var tag = Instantiate(_projectHistoryTagPrefab, _projectHistoryAnchor);
                tag.Bind(projectName);
            }
        }

        private string GetStatusString(Employee employee)
        {
            return employee.WorkStatus switch
            {
                EmployeeWorkStatus.InProject => "프로젝트중",
                EmployeeWorkStatus.InTraining => "교육중",
                _ => "대기중"
            };
        }

        private void SpawnTag(Transform anchor, DepartmentTagView prefab, Role role)
        {
            ClearAnchor(anchor);
            var tag = Instantiate(prefab, anchor);
            tag.Bind(role);
        }

        private void SpawnTag(Transform anchor, MBTITagView prefab, MbtiFlags mbti)
        {
            ClearAnchor(anchor);
            var tag = Instantiate(prefab, anchor);
            tag.Bind(mbti);
        }

        private void SpawnTraitTag(Transform anchor, Trait trait)
        {
            ClearAnchor(anchor);
            var tag = Instantiate(_traitTagPrefab, anchor);
            tag.Bind(trait);
        }

        private static void ClearAnchor(Transform anchor)
        {
            foreach (Transform child in anchor)
                Destroy(child.gameObject);
        }

        private static Sprite GetProfileSprite(EmployeeImmutableData so, EmployeeMutableData mutable)
        {
            bool highFatigue = mutable.fatigue > 50;
            bool lowDesire = mutable.desire < 50;

            if (highFatigue && lowDesire) return so.iconCritical;
            if (highFatigue || lowDesire) return so.iconCaution;
            return so.iconNormal;
        }
    }
}
