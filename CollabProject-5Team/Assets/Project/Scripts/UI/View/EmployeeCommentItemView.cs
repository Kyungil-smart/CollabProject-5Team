using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직원 코멘트 아이템 프리팹 바인딩.
    /// Panel_EmployeeComment.CommentList Content에 직군순 + 이름순으로 동적 생성.
    /// </summary>
    public sealed class EmployeeCommentItemView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Image _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameLabel;

        [Header("태그")]
        [SerializeField] private Image _tagBG;
        [SerializeField] private TextMeshProUGUI _tagLabel;

        [Header("직군별 색상")]
        [SerializeField] private Color _plannerColor;
        [SerializeField] private Color _programmerColor;
        [SerializeField] private Color _artistColor;

        [Header("충성도 변화")]
        [SerializeField] private TextMeshProUGUI _loyaltyChangeTitleLabel;
        [SerializeField] private TextMeshProUGUI _loyaltyChangeValue;
        [SerializeField] private TextMeshProUGUI _loyaltyChangeWeightLabel;

        [Header("코멘트")]
        [SerializeField] private TextMeshProUGUI _commentLabel;

        private static readonly Color ColorUp = Color.red;
        private static readonly Color ColorDown = Color.blue;
        private static readonly Color ColorNone = Color.gray;

        public void Bind(Employee employee)
        {
            Bind(employee, null);
        }

        /// <summary>
        /// commentText가 null이면 fallback 코멘트 사용.
        /// </summary>
        public void Bind(Employee employee, string commentText)
        {
            var so = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = GetProfileSprite(so, mutable);
            _nameLabel.text = so.Name;

            _tagLabel.text = RoleToString(so.role);
            _tagBG.color = RoleToColor(so.role);

            SetLoyaltyChange(mutable.loyalty - mutable.preLoyalty);
            SetComment(employee, commentText);
        }

        private void SetLoyaltyChange(int delta)
        {
            _loyaltyChangeTitleLabel.text = "충성도";

            if (delta > 0)
            {
                _loyaltyChangeValue.text = $"+{delta}";
                _loyaltyChangeValue.color = ColorUp;
                _loyaltyChangeWeightLabel.color = ColorUp;
            }
            else if (delta < 0)
            {
                _loyaltyChangeValue.text = delta.ToString();
                _loyaltyChangeValue.color = ColorDown;
                _loyaltyChangeWeightLabel.color = ColorDown;
            }
            else
            {
                _loyaltyChangeValue.text = "-";
                _loyaltyChangeValue.color = ColorNone;
                _loyaltyChangeWeightLabel.color = ColorNone;
            }
        }

        private void SetComment(Employee employee, string commentText)
        {
            _commentLabel.text = commentText ?? GetFallbackComment(employee.MutableData);
            _commentLabel.color = Color.white;
        }

        private static string GetFallbackComment(EmployeeMutableData mutable)
        {
            if (mutable.fatigue >= 70) return "요즘 좀 힘드네요...";
            if (mutable.desire >= 80) return "이번 주도 열심히 할게요!";
            return "잘 부탁드립니다.";
        }

        private static Sprite GetProfileSprite(EmployeeImmutableData so, EmployeeMutableData mutable)
        {
            bool highFatigue = mutable.fatigue > 50;
            bool lowDesire = mutable.desire < 50;

            if (highFatigue && lowDesire) return so.iconCritical;
            if (highFatigue || lowDesire) return so.iconCaution;
            return so.iconNormal;
        }

        private static string RoleToString(Role role) => role switch
        {
            Role.PLANNER => "기획",
            Role.PROGRAMMER => "개발",
            Role.ARTIST => "아트",
            _ => string.Empty
        };

        private Color RoleToColor(Role role) => role switch
        {
            Role.PLANNER => _plannerColor,
            Role.PROGRAMMER => _programmerColor,
            Role.ARTIST => _artistColor,
            _ => Color.white
        };
    }
}
