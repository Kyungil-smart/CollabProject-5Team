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

        [Header("태그 앵커")]
        [SerializeField] private Transform _departmentTagAnchor;

        [Header("태그 프리팹")]
        [SerializeField] private DepartmentTagView _departmentTagPrefab;

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
            var so = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = GetProfileSprite(so, mutable);
            _nameLabel.text = so.Name;

            ClearAnchor(_departmentTagAnchor);
            var tag = Instantiate(_departmentTagPrefab, _departmentTagAnchor);
            tag.Bind(so.role);

            // [TODO: 주차별 충성도 변화량 데이터 구조 확정 후 실제 값 연결]
            SetLoyaltyChange(0);
            SetComment(employee);
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

        private void SetComment(Employee employee)
        {
            if (!employee.hasTalkedThisWeek)
            {
                _commentLabel.text = "코멘트 없음";
                _commentLabel.color = Color.red;
                return;
            }

            // [TODO: 대화 시스템 연결 후 실제 코멘트 데이터 바인딩]
            var mutable = employee.MutableData;
            _commentLabel.text = GetFallbackComment(mutable);
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

        private static void ClearAnchor(Transform anchor)
        {
            foreach (Transform child in anchor)
                Destroy(child.gameObject);
        }
    }
}