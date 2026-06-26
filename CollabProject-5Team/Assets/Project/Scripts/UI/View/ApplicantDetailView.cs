using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 지원자 이력서 상세 페이지 프리팹 바인딩.
    /// Panel_ApplicantDetail.DetailContent에 동적 생성.
    /// 태그들은 앵커 위치에 프리팹으로 동적 생성.
    /// </summary>
    public sealed class ApplicantDetailView : MonoBehaviour, IBindable<EmployeeImmutableData>
    {
        [Header("기본 정보")]
        [SerializeField] private Image _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameValue;
        [SerializeField] private TextMeshProUGUI _honorificValue;
        [SerializeField] private TextMeshProUGUI _salaryValue;
        [SerializeField] private TextMeshProUGUI _hiringCostValue;

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

        [Header("능력치")]
        [SerializeField] private Slider _abilityBar;
        [SerializeField] private TextMeshProUGUI _abilityValue;

        [Header("자기소개서")]
        [SerializeField] private TextMeshProUGUI _selfIntroValue;

        [Header("채용 확정 도장")]
        [SerializeField] private GameObject _hireStamp;

        public void Bind(EmployeeImmutableData so)
        {
            _profileIcon.sprite = so.iconNormal;
            _nameValue.text = so.Name;
            _honorificValue.text = so.style;
            _salaryValue.text = $"{so.weekSalary:N0}G";
            _hiringCostValue.text = $"{so.hiringCost:N0}G";
            _selfIntroValue.text = so.hireText;

            SpawnTag(_departmentTagAnchor, _departmentTagPrefab, so.role);
            SpawnTag(_mbtiTagAnchor, _mbtiTagPrefab, so.mbtiParsed);
            SpawnTraitTags(so.mainTrait, so.subTrait, so.riskTrait);

            _abilityBar.value = so.ability;
            _abilityValue.text = so.ability.ToString();

            _hireStamp.SetActive(false);
        }

        private void SpawnTraitTags(Trait main, Trait sub, Trait risk)
        {
            ClearAnchor(_mainTraitTagAnchor);
            ClearAnchor(_subTraitTagAnchor);
            ClearAnchor(_riskTraitTagAnchor);

            if (sub != Trait.None)
            {
                SpawnTraitTag(_mainTraitTagAnchor, main);
                SpawnTraitTag(_subTraitTagAnchor, sub);
                SpawnTraitTag(_riskTraitTagAnchor, risk);
                _riskTraitTagAnchor.gameObject.SetActive(true);
            }
            else
            {
                SpawnTraitTag(_mainTraitTagAnchor, main);
                SpawnTraitTag(_subTraitTagAnchor, risk);
                _riskTraitTagAnchor.gameObject.SetActive(false);
            }
        }

        public void SetHireStamp(bool active)
        {
            _hireStamp.SetActive(active);
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
            var tag = Instantiate(_traitTagPrefab, anchor);
            tag.Bind(trait);
        }

        private static void ClearAnchor(Transform anchor)
        {
            foreach (Transform child in anchor)
                Destroy(child.gameObject);
        }
    }
}