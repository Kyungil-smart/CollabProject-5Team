using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 보고서 하단 미니 패널 직원 상태 카드 프리팹 바인딩.
    /// EmployeeStatusSlide.PreviewContent에 직군순 + 이름순으로 동적 생성.
    /// </summary>
    public sealed class EmployeeStatusMiniItemView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Image           _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private DepartmentTagView _departmentTag;
        [SerializeField] private MBTITagView     _mbtiTag;

        [Header("의욕도")]
        [SerializeField] private Slider          _motivationBar;
        [SerializeField] private TextMeshProUGUI _motivationValue;
        [SerializeField] private TextMeshProUGUI _motivationComment;

        [Header("피로도")]
        [SerializeField] private Slider          _fatigueBar;
        [SerializeField] private TextMeshProUGUI _fatigueValue;
        [SerializeField] private TextMeshProUGUI _fatigueComment;

        public void Bind(Employee employee)
        {
            var so      = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = GetProfileSprite(so, mutable);
            _nameLabel.text     = so.Name;

            _departmentTag.Bind(so.role);
            _mbtiTag.Bind(so.mbtiParsed);

            _motivationBar.value    = mutable.desire;
            _motivationValue.text   = mutable.desire.ToString();
            _motivationComment.text = GetMotivationComment(mutable.desire);

            _fatigueBar.value    = mutable.fatigue;
            _fatigueValue.text   = mutable.fatigue.ToString();
            _fatigueComment.text = GetFatigueComment(mutable.fatigue);
        }

        private static string GetMotivationComment(int desire) => desire switch
        {
            >= 80 => "의욕 넘침",
            >= 40 => "평시",
            _     => "의욕 저하"
        };

        private static string GetFatigueComment(int fatigue) => fatigue switch
        {
            >= 70 => "번아웃 위험",
            >= 50 => "피로 누적",
            _     => "컨디션 양호"
        };

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