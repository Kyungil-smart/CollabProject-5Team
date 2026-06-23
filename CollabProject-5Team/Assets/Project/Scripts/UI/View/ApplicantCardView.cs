using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 지원자 카드 프리팹 바인딩.
    /// Panel_ApplicantList.ApplicantScroll에 동적 생성.
    /// </summary>
    public sealed class ApplicantCardView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Image           _profileIcon;
        [SerializeField] private TextMeshProUGUI _nameValue;
        [SerializeField] private TextMeshProUGUI _deptValue;
        [SerializeField] private GameObject      _hireStamp;
        [SerializeField] private GameObject      _nextWeekOverlay;
        [SerializeField] private TextMeshProUGUI _overlayLabel;

        public void Bind(Employee e)
        {
            _profileIcon.sprite = e.so.iconNormal;
            _nameValue.text     = e.so.Name;
            _deptValue.text     = RoleToString(e.so.role);

            _hireStamp.SetActive(false);
            _nextWeekOverlay.SetActive(false);
        }

        public void SetHireStamp(bool active)
        {
            _hireStamp.SetActive(active);
        }

        /// <summary>
        /// 채용 확정 후 다음 주 출근 오버레이 표시.
        /// </summary>
        public void SetNextWeekOverlay(bool active, int weeksLeft = 1)
        {
            _nextWeekOverlay.SetActive(active);
            if (active)
                _overlayLabel.text = $"다음 주 부터 출근";
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