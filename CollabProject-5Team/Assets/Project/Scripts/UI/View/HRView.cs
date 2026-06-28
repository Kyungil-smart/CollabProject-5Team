using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.HRPopup 담당 View.
    /// 탭 전환, 패널 전환, 버튼 이벤트 발행.
    /// 프리팹 동적 생성 및 데이터 바인딩은 Presenter에서 담당.
    /// </summary>
    public sealed class HRView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject _hrPopup;

        [Header("TabButtons")]
        [SerializeField] private Button _employeeManageTabButton;
        [SerializeField] private Button _hireTabButton;
        [SerializeField] private Button _fireTabButton;
        [SerializeField] private Button _educationTabButton;

        [Header("TabButton Sprites")]
        [SerializeField] private Sprite _tabActiveSprite;
        [SerializeField] private Sprite _tabInactiveSprite;
        [SerializeField] private Color _tabActiveLabelColor;
        [SerializeField] private Color _tabInactiveLabelColor;

        [Header("Tab_EmployeeManage")]
        [SerializeField] private GameObject _tabEmployeeManage;
        [SerializeField] private TextMeshProUGUI _employeeManageCountLabel;
        [SerializeField] private TMP_Dropdown _employeeManageSortDropdown;
        [SerializeField] private Transform _employeeGridContent;
        [SerializeField] private GameObject _employeeManagePanelList;
        [SerializeField] private GameObject _employeeManagePanelDetail;
        [SerializeField] private Transform _employeeManageDetailContent;
        [SerializeField] private Button _employeeManageEducationButton;
        [SerializeField] private Button _employeeManageFireButton;
        [SerializeField] private Button _employeeManageBackButton;

        [Header("Tab_Hire — Panel_HireMain")]
        [SerializeField] private GameObject _tabHire;
        [SerializeField] private GameObject _panelHireMain;
        [SerializeField] private Button _recruitButton;
        [SerializeField] private TextMeshProUGUI _recruitButtonLabel;
        [SerializeField] private Button _applicantButton;
        [SerializeField] private TextMeshProUGUI _applicantButtonLabel;

        [Header("Tab_Hire — Panel_Recruit")]
        [SerializeField] private GameObject _panelRecruit;
        [SerializeField] private TextMeshProUGUI _jobCategoryTag;
        [SerializeField] private TextMeshProUGUI _recruitCountTag;
        [SerializeField] private Button _recruitBackButton;
        [SerializeField] private TextMeshProUGUI _totalCountLabel;
        [SerializeField] private TextMeshProUGUI _totalCostLabel;
        [SerializeField] private Button _recruitConfirmButton;

        [Header("Tab_Hire — Panel_Recruit — JobSliders")]
        [SerializeField] private JobSliderGroupView _sliderPlanning;
        [SerializeField] private JobSliderGroupView _sliderArt;
        [SerializeField] private JobSliderGroupView _sliderDev;

        [Header("Tab_Hire — Panel_ApplicantList")]
        [SerializeField] private GameObject _panelApplicantList;
        [SerializeField] private TMP_Dropdown _applicantSortDropdown;
        [SerializeField] private Transform _applicantScrollContent;
        [SerializeField] private Button _applicantListBackButton;

        [Header("Tab_Hire — Panel_ApplicantDetail")]
        [SerializeField] private GameObject _panelApplicantDetail;
        [SerializeField] private Transform _applicantDetailContent;
        [SerializeField] public Button HireButton;
        [SerializeField] private Button _applicantDetailBackButton;

        [Header("Tab_Fire")]
        [SerializeField] private GameObject _tabFire;
        [SerializeField] private TextMeshProUGUI _fireCountLabel;
        [SerializeField] private TMP_Dropdown _fireSortDropdown;
        [SerializeField] private Transform _fireListContent;
        [SerializeField] private GameObject _firePanelList;
        [SerializeField] private GameObject _firePanelDetail;
        [SerializeField] private Transform _fireDetailContent;
        [SerializeField] private Button _fireConfirmButton;
        [SerializeField] private Button _fireBackButton;

        [Header("Tab_Education")]
        [SerializeField] private GameObject _tabEducation;
        [SerializeField] private TextMeshProUGUI _educationCountLabel;
        [SerializeField] private TMP_Dropdown _educationSortDropdown;
        [SerializeField] private Transform _educationListContent;
        [SerializeField] private GameObject _educationPanelList;
        [SerializeField] private GameObject _educationPanelDetail;
        [SerializeField] private Transform _educationDetailContent;
        [SerializeField] private Button _educationButton;
        [SerializeField] private Button _educationDetailBackButton;
        [SerializeField] private GameObject _panelEducationCourse;
        [SerializeField] private Button _educationCourseConfirmButton;
        [SerializeField] private Button _educationCourseBackButton;

        [Header("Tab_Education — ConfirmButton Sprites")]
        [SerializeField] private Sprite _confirmActiveSprite;
        [SerializeField] private Sprite _confirmInactiveSprite;

        [Header("Tab_Education — CourseCards")]
        [SerializeField] private Button _courseButton0;
        [SerializeField] private Button _courseButton1;
        [SerializeField] private Button _courseButton2;

        [Header("Tab_Education — CourseCards — SelectIMG")]
        [SerializeField] private GameObject _courseSelectImg0;
        [SerializeField] private GameObject _courseSelectImg1;
        [SerializeField] private GameObject _courseSelectImg2;

        [Header("Tab_Education — CourseCards — EducationOverlay")]
        [SerializeField] private GameObject _courseEducationOverlay0;
        [SerializeField] private GameObject _courseEducationOverlay1;
        [SerializeField] private GameObject _courseEducationOverlay2;

        [Header("Tab_Education — CourseCards — CostValue")]
        [SerializeField] private TextMeshProUGUI _courseCostValue0;
        [SerializeField] private TextMeshProUGUI _courseCostValue1;
        [SerializeField] private TextMeshProUGUI _courseCostValue2;

        // Tab 이벤트
        public Observable<Unit> OnEmployeeManageTabClicked => _employeeManageTabButton.OnClickAsObservable();
        public Observable<Unit> OnHireTabClicked => _hireTabButton.OnClickAsObservable();
        public Observable<Unit> OnFireTabClicked => _fireTabButton.OnClickAsObservable();
        public Observable<Unit> OnEducationTabClicked => _educationTabButton.OnClickAsObservable();

        // Tab_EmployeeManage 이벤트
        public Observable<int> OnEmployeeManageSortChanged => _employeeManageSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnEmployeeManageEducationClicked => _employeeManageEducationButton.OnClickAsObservable();
        public Observable<Unit> OnEmployeeManageFireClicked => _employeeManageFireButton.OnClickAsObservable();
        public Observable<Unit> OnEmployeeManageBackClicked => _employeeManageBackButton.OnClickAsObservable();

        // Tab_Hire 이벤트
        public Observable<Unit> OnRecruitClicked => _recruitButton.OnClickAsObservable();
        public Observable<Unit> OnApplicantClicked => _applicantButton.OnClickAsObservable();
        public Observable<Unit> OnRecruitBackClicked => _recruitBackButton.OnClickAsObservable();
        public Observable<Unit> OnRecruitConfirmClicked => _recruitConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnApplicantListBackClicked => _applicantListBackButton.OnClickAsObservable();
        public Observable<int> OnApplicantSortChanged => _applicantSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnHireClicked => HireButton.OnClickAsObservable();
        public Observable<Unit> OnApplicantDetailBackClicked => _applicantDetailBackButton.OnClickAsObservable();

        // Tab_Fire 이벤트
        public Observable<int> OnFireSortChanged => _fireSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnFireConfirmClicked => _fireConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnFireBackClicked => _fireBackButton.OnClickAsObservable();

        // Tab_Education 이벤트
        public Observable<int> OnEducationSortChanged => _educationSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnEducationClicked => _educationButton.OnClickAsObservable();
        public Observable<Unit> OnEducationDetailBackClicked => _educationDetailBackButton.OnClickAsObservable();
        public Observable<Unit> OnEducationCourseConfirmClicked => _educationCourseConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnEducationCourseBackClicked => _educationCourseBackButton.OnClickAsObservable();

        // 카드 버튼 클릭 시 인덱스(0~2) 발행
        public Observable<int> OnCourseSelected => Observable.Merge(
            _courseButton0.OnClickAsObservable().Select(_ => 0),
            _courseButton1.OnClickAsObservable().Select(_ => 1),
            _courseButton2.OnClickAsObservable().Select(_ => 2)
        );

        // Content Transform (Presenter에서 프리팹 Instantiate 위치로 사용)
        public Transform EmployeeGridContent => _employeeGridContent;
        public Transform EmployeeManageDetailContent => _employeeManageDetailContent;
        public Transform ApplicantScrollContent => _applicantScrollContent;
        public Transform ApplicantDetailContent => _applicantDetailContent;
        public Transform FireListContent => _fireListContent;
        public Transform FireDetailContent => _fireDetailContent;
        public Transform EducationListContent => _educationListContent;
        public Transform EducationDetailContent => _educationDetailContent;
        public int EmployeeManageSortIndex => _employeeManageSortDropdown.value;
        public int ApplicantSortIndex => _applicantSortDropdown.value;
        public int FireSortIndex => _fireSortDropdown.value;
        public int EducationSortIndex => _educationSortDropdown.value;

        private void Awake()
        {
            ShowTab(HRTab.EmployeeManage);

            _employeeManagePanelDetail.SetActive(false);
            _panelRecruit.SetActive(false);
            _panelApplicantList.SetActive(false);
            _panelApplicantDetail.SetActive(false);
            _firePanelList.SetActive(true);
            _firePanelDetail.SetActive(false);
            _educationPanelList.SetActive(true);
            _educationPanelDetail.SetActive(false);
            _panelEducationCourse.SetActive(false);

            HireButton.interactable = false;
            _educationButton.interactable = false;
            _educationCourseConfirmButton.interactable = false;
            _recruitConfirmButton.interactable = false;

            // SelectIMG / EducationOverlay 초기 비활성화
            _courseSelectImg0.SetActive(false);
            _courseSelectImg1.SetActive(false);
            _courseSelectImg2.SetActive(false);
            _courseEducationOverlay0.SetActive(false);
            _courseEducationOverlay1.SetActive(false);
            _courseEducationOverlay2.SetActive(false);
        }

        public void Show() => _hrPopup.SetActive(true);
        public void Hide() => _hrPopup.SetActive(false);
        public bool IsVisible => _hrPopup.activeSelf;

        public void ShowTab(HRTab tab)
        {
            _tabEmployeeManage.SetActive(tab == HRTab.EmployeeManage);
            _tabHire.SetActive(tab == HRTab.Hire);
            _tabFire.SetActive(tab == HRTab.Fire);
            _tabEducation.SetActive(tab == HRTab.Education);

            SetTabButtonState(_employeeManageTabButton, tab == HRTab.EmployeeManage);
            SetTabButtonState(_hireTabButton, tab == HRTab.Hire);
            SetTabButtonState(_fireTabButton, tab == HRTab.Fire);
            SetTabButtonState(_educationTabButton, tab == HRTab.Education);
        }

        private void SetTabButtonState(Button button, bool isActive)
        {
            button.image.sprite = isActive ? _tabActiveSprite : _tabInactiveSprite;
            button.GetComponentInChildren<TextMeshProUGUI>().color = isActive ? _tabActiveLabelColor : _tabInactiveLabelColor;
        }

        // Tab_EmployeeManage 패널 전환
        public void ShowEmployeeManageList()
        {
            _employeeManagePanelList.SetActive(true);
            _employeeManagePanelDetail.SetActive(false);
        }

        public void ShowEmployeeManageDetail()
        {
            _employeeManagePanelList.SetActive(false);
            _employeeManagePanelDetail.SetActive(true);
        }

        // Tab_Hire 패널 전환
        public void ShowHireMain()
        {
            _panelHireMain.SetActive(true);
            _panelRecruit.SetActive(false);
            _panelApplicantList.SetActive(false);
            _panelApplicantDetail.SetActive(false);
        }

        public void ShowRecruit()
        {
            _panelHireMain.SetActive(false);
            _panelRecruit.SetActive(true);
        }

        public JobSliderGroupView[] AllSliders => new[]
            { _sliderPlanning, _sliderArt, _sliderDev };

        public void SetTotalRecruitInfo(int count, int cost)
        {
            _totalCountLabel.text = $"총 {count} 명";
            _totalCostLabel.text = $"{cost:N0} G";
        }

        public void ShowApplicantList()
        {
            _panelHireMain.SetActive(false);
            _panelApplicantList.SetActive(true);
            _panelApplicantDetail.SetActive(false);
        }

        public void ShowApplicantDetail()
        {
            _panelApplicantDetail.SetActive(true);
        }

        // Tab_Fire 패널 전환
        public void ShowFireList()
        {
            _firePanelList.SetActive(true);
            _firePanelDetail.SetActive(false);
        }

        public void ShowFireDetail()
        {
            _firePanelList.SetActive(false);
            _firePanelDetail.SetActive(true);
        }

        // Tab_Education 패널 전환
        public void ShowEducationList()
        {
            _educationPanelList.SetActive(true);
            _educationPanelDetail.SetActive(false);
            _panelEducationCourse.SetActive(false);
        }

        public void ShowEducationDetail()
        {
            _educationPanelList.SetActive(false);
            _educationPanelDetail.SetActive(true);
            _panelEducationCourse.SetActive(false);
        }

        /// <summary>
        /// 교육 과정 패널 진입.
        /// inTrainingIndex: 현재 교육 중인 과정 인덱스(-1이면 미교육 상태).
        /// </summary>
        public void ShowEducationCourse(int inTrainingIndex = -1)
        {
            ResetCourseSelection();
            _panelEducationCourse.SetActive(true);

            // 교육 중인 직원의 경우 해당 카드에 EducationOverlay 표시 및 버튼 잠금
            bool isInTraining = inTrainingIndex >= 0;
            SetCourseButtonsInteractable(!isInTraining);

            if (isInTraining)
                SetCourseEducationOverlay(inTrainingIndex, true);
        }

        /// <summary>
        /// 선택 상태 초기화. 패널 진입 및 결정 완료 후 호출.
        /// </summary>
        public void ResetCourseSelection()
        {
            _courseSelectImg0.SetActive(false);
            _courseSelectImg1.SetActive(false);
            _courseSelectImg2.SetActive(false);
            _courseEducationOverlay0.SetActive(false);
            _courseEducationOverlay1.SetActive(false);
            _courseEducationOverlay2.SetActive(false);
        }

        public void SetCourseSelectImg(int index, bool active)
        {
            switch (index)
            {
                case 0: _courseSelectImg0.SetActive(active); break;
                case 1: _courseSelectImg1.SetActive(active); break;
                case 2: _courseSelectImg2.SetActive(active); break;
            }
        }

        public void SetCourseEducationOverlay(int index, bool active)
        {
            switch (index)
            {
                case 0: _courseEducationOverlay0.SetActive(active); break;
                case 1: _courseEducationOverlay1.SetActive(active); break;
                case 2: _courseEducationOverlay2.SetActive(active); break;
            }
        }

        public void SetCourseButtonsInteractable(bool interactable)
        {
            _courseButton0.interactable = interactable;
            _courseButton1.interactable = interactable;
            _courseButton2.interactable = interactable;
        }

        public void SetCourseCostValues(string cost0, string cost1, string cost2)
        {
            _courseCostValue0.text = cost0;
            _courseCostValue1.text = cost1;
            _courseCostValue2.text = cost2;
        }

        // 수치 표시
        public void SetEmployeeManageCountLabel(int count) => _employeeManageCountLabel.text = $"직원 {count}명";
        public void SetFireCountLabel(int count) => _fireCountLabel.text = $"직원 {count}명";
        public void SetEducationCountLabel(int count) => _educationCountLabel.text = $"직원 {count}명";
        public void SetJobCategoryTag(string category) => _jobCategoryTag.text = category;
        public void SetRecruitCountTag(int count) => _recruitCountTag.text = count.ToString();

        public void SetRecruitButtonLabel(bool isRecruiting)
            => _recruitButtonLabel.text = isRecruiting ? "직원모집중" : "직원모집";

        public void SetApplicantButtonLabel(bool isHired)
            => _applicantButtonLabel.text = isHired ? "채용완료" : "지원자리스트";

        // interactable 제어
        public void SetRecruitButtonInteractable(bool interactable)
            => _recruitButton.interactable = interactable;

        public void SetRecruitConfirmInteractable(bool interactable)
            => _recruitConfirmButton.interactable = interactable;

        public void SetHireButtonInteractable(bool interactable)
            => HireButton.interactable = interactable;

        public void SetEducationButtonInteractable(bool interactable)
            => _educationButton.interactable = interactable;

        public void SetEducationCourseConfirmInteractable(bool interactable)
        {
            _educationCourseConfirmButton.interactable = interactable;
            _educationCourseConfirmButton.image.sprite = interactable ? _confirmActiveSprite : _confirmInactiveSprite;
        }
    }

    public enum HRTab { EmployeeManage, Hire, Fire, Education }
}