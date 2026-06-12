using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 인원 배치 직원 카드 프리팹 바인딩.
    /// Panel_StaffAssign.StaffGrid Content에 동적 생성.
    /// 직군별 테두리 색상은 배리언트(StaffCardView_Planning/Art/Dev)로 처리.
    /// 카드 클릭 시 ButtonOverlay 활성화 — 정보/배치(해제) 버튼 표시.
    /// </summary>
    public sealed class StaffCardView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Image _profileIcon;
        [SerializeField] private Image _departmentTagBG;
        [SerializeField] private TextMeshProUGUI _departmentTagLabel;
        [SerializeField] private TextMeshProUGUI _honorificLabel;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _abilityValue;
        [SerializeField] private GameObject _deployOverlay;
        [SerializeField] private GameObject _educationOverlay;

        [Header("ButtonOverlay")]
        [SerializeField] private GameObject _buttonOverlay;
        [SerializeField] private Button _infoButton;
        [SerializeField] private Button _assignButton;
        [SerializeField] private TextMeshProUGUI _assignButtonLabel;
        [SerializeField] private Sprite _assignSprite;    // "배치" 버튼 스프라이트
        [SerializeField] private Sprite _releaseSprite;   // "해제" 버튼 스프라이트

        public Observable<Unit> OnInfoClicked => _infoButton.OnClickAsObservable();
        public Observable<Unit> OnAssignClicked => _assignButton.OnClickAsObservable();
        public bool IsOverlayVisible => _buttonOverlay.activeSelf;

        private void Awake()
        {
            _buttonOverlay.SetActive(false);
        }

        public void Bind(Employee employee)
        {
            var so = employee.so;
            var mutable = employee.MutableData;

            _profileIcon.sprite = so.iconNormal;
            _departmentTagLabel.text = RoleToString(so.role);
            _honorificLabel.text = so.style;
            _nameLabel.text = so.Name;
            _abilityValue.text = mutable.ability.ToString();

            // [TODO: 교육 시스템 연결 후 교육 중 상태 처리]
            _educationOverlay.SetActive(false);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            _buttonOverlay.SetActive(selected);
        }

        /// <summary>
        /// 직원 상태에 따라 배치/해제/배치(비활성) 버튼 전환.
        /// </summary>
        public void SetAssignState(StaffAssignState state)
        {
            _deployOverlay.SetActive(state == StaffAssignState.Assigned);
            _educationOverlay.SetActive(state == StaffAssignState.InEducation);

            switch (state)
            {
                case StaffAssignState.Assigned:
                    _assignButtonLabel.text = "해제";
                    _assignButton.interactable = true;
                    if (_releaseSprite != null)
                        _assignButton.GetComponent<Image>().sprite = _releaseSprite;
                    break;
                case StaffAssignState.InEducation:
                    _assignButtonLabel.text = "배치";
                    _assignButton.interactable = false;
                    if (_assignSprite != null)
                        _assignButton.GetComponent<Image>().sprite = _assignSprite;
                    break;
                default:
                    _assignButtonLabel.text = "배치";
                    _assignButton.interactable = true;
                    if (_assignSprite != null)
                        _assignButton.GetComponent<Image>().sprite = _assignSprite;
                    break;
            }
        }

        private static string RoleToString(Role role) => role switch
        {
            Role.PLANNER => "기획",
            Role.PROGRAMMER => "개발",
            Role.ARTIST => "아트",
            Role.MARKETING => "마케팅",
            Role.QA => "QA",
            _ => string.Empty
        };
    }

    public enum StaffAssignState { Default, Assigned, InEducation }
}