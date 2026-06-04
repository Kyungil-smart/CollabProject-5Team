using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 지원자 이력서 상세 페이지 프리팹 바인딩.
    /// Panel_ApplicantDetail.DetailContent에 동적 생성.
    /// </summary>
    public sealed class ApplicantDetailView : MonoBehaviour, IBindable<EmployeeImmutableData>
    {
        [Header("기본 정보")]
        [SerializeField] private Image           _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameValue;
        [SerializeField] private DepartmentTagView _departmentTag;
        [SerializeField] private TextMeshProUGUI _honorificValue;
        [SerializeField] private MBTITagView     _mbtiTag;
        [SerializeField] private TextMeshProUGUI _salaryValue;

        [Header("능력치")]
        [SerializeField] private Slider          _abilityBar;
        [SerializeField] private TextMeshProUGUI _abilityValue;

        [Header("자기소개서")]
        [SerializeField] private TextMeshProUGUI _selfIntroValue;

        [Header("특성 — 3개 고정")]
        [SerializeField] private TraitTagView _mainTraitTag;
        [SerializeField] private TraitTagView _subTraitTag;
        [SerializeField] private TraitTagView _riskTraitTag;

        [Header("채용 확정 도장")]
        [SerializeField] private GameObject _hireStamp;

        public void Bind(EmployeeImmutableData so)
        {
            _profileIcon.sprite  = so.iconNormal;
            _nameValue.text      = so.Name;
            _honorificValue.text = so.style;
            _salaryValue.text    = $"{so.weekSalary:N0}G";
            _selfIntroValue.text = so.hireText;

            _departmentTag.Bind(so.role);
            _mbtiTag.Bind(so.mbtiParsed);

            _mainTraitTag.Bind(so.mainTrait);
            _subTraitTag.Bind(so.subTrait);
            _riskTraitTag.Bind(so.riskTrait);

            _abilityBar.value   = so.ability;
            _abilityValue.text  = so.ability.ToString();

            _hireStamp.SetActive(false);
        }

        public void SetHireStamp(bool active)
        {
            _hireStamp.SetActive(active);
        }
    }
}