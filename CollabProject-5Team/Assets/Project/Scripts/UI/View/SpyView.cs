using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 스파이 지목용 직원 카드 뷰.
    /// Canvas_Spy > PopupFrame > EmployeeCardGroup 하위 9개 고정 배치.
    /// </summary>
    public sealed class SpyView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameLabel;

        [Header("직군 태그 고정 UI 컴포넌트")]
        [SerializeField] private Image _departmentTagBG;
        [SerializeField] private TextMeshProUGUI _departmentTagText;

        [SerializeField] private Image _abilityImage;
        [SerializeField] private TextMeshProUGUI _abilityValue;
        [SerializeField] private GameObject _selectIMG;
        [SerializeField] private GameObject _stamp;

        [Header("직군별 능력치 및 태그 색상")]
        [SerializeField] private Color _plannerAbilityColor;
        [SerializeField] private Color _programmerAbilityColor;
        [SerializeField] private Color _artistAbilityColor;

        public Employee BoundEmployee { get; private set; }

        public Observable<Employee> OnClickAsObservable => _button.OnClickAsObservable().Select(_ => BoundEmployee);

        public void Bind(Employee employee)
        {
            BoundEmployee = employee;

            var so = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = so.iconNormal;
            _nameLabel.text = so.Name;
            _abilityValue.text = mutable.ability.ToString();

            // 직군 데이터에 대응하는 기획 색상을 가져와 하단 능력치 배경과 좌측 상단 태그 배경 색상을 동시에 동기화합니다.
            Color targetColor = RoleToAbilityColor(so.role);
            _abilityImage.color = targetColor;

            if (_departmentTagBG != null)
                _departmentTagBG.color = targetColor;

            // 고정 형태의 직군 태그 TMP 컴포넌트에 직군 enum에 알맞은 로컬라이징 텍스트를 직접 대입합니다.
            if (_departmentTagText != null)
                _departmentTagText.text = RoleToText(so.role);

            // UI 풀링 또는 재사용 시 이전 카드의 선택/도장 상태가 전이되는 현상을 막기 위해 초기화합니다.
            _selectIMG.SetActive(false);
            _stamp.SetActive(false);
        }

        public void SetSelected(bool isSelected)
        {
            _selectIMG.SetActive(isSelected);
        }

        public void SetStamped(bool isStamped)
        {
            _stamp.SetActive(isStamped);
        }

        private Color RoleToAbilityColor(Role role) => role switch
        {
            Role.PLANNER => _plannerAbilityColor,
            Role.PROGRAMMER => _programmerAbilityColor,
            Role.ARTIST => _artistAbilityColor,
            _ => Color.white,
        };

        /// <summary>
        /// 직군 enum 값을 인게임에 표시할 최종 문자열로 치환합니다.
        /// </summary>
        private string RoleToText(Role role) => role switch
        {
            Role.PLANNER => "기획",
            Role.PROGRAMMER => "개발",
            Role.ARTIST => "아트",
            _ => "미정"
        };
    }
}