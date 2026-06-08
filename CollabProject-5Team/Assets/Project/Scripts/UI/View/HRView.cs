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

        [Header("Tab_EmployeeManage")]
        [SerializeField] private GameObject      _tabEmployeeManage;
        [SerializeField] private TextMeshProUGUI _employeeManageCountLabel;
        [SerializeField] private TMP_Dropdown    _employeeManageSortDropdown;
        [SerializeField] private Transform       _employeeGridContent;
        [SerializeField] private GameObject      _employeeManagePanelList;
        [SerializeField] private GameObject      _employeeManagePanelDetail;
        [SerializeField] private Transform       _employeeManageDetailContent;
        [SerializeField] private Button          _employeeManageEducationButton;
        [SerializeField] private Button          _employeeManageFireButton;
        [SerializeField] private Button          _employeeManageBackButton;

        [Header("Tab_Hire — Panel_HireMain")]
        [SerializeField] private GameObject _tabHire;
        [SerializeField] private GameObject _panelHireMain;
        [SerializeField] private Button     _recruitButton;
        [SerializeField] private TextMeshProUGUI _recruitButtonLabel;
        [SerializeField] private Button     _applicantButton;
        [SerializeField] private TextMeshProUGUI _applicantButtonLabel;

        [Header("Tab_Hire — Panel_Recruit")]
        [SerializeField] private GameObject      _panelRecruit;
        [SerializeField] private TextMeshProUGUI _jobCategoryTag;
        [SerializeField] private TextMeshProUGUI _recruitCountTag;
        [SerializeField] private Button          _recruitBackButton;
        [SerializeField] private TextMeshProUGUI _totalCountLabel;
        [SerializeField] private TextMeshProUGUI _totalCostLabel;
        [SerializeField] private Button          _recruitConfirmButton;

        [Header("Tab_Hire — Panel_Recruit — JobSliders")]
        [SerializeField] private JobSliderGroupView _sliderPlanning;
        [SerializeField] private JobSliderGroupView _sliderArt;
        [SerializeField] private JobSliderGroupView _sliderDev;
        [SerializeField] private JobSliderGroupView _sliderQA;
        [SerializeField] private JobSliderGroupView _sliderMarketing;

        [Header("Tab_Hire — Panel_ApplicantList")]
        [SerializeField] private GameObject      _panelApplicantList;
        [SerializeField] private TMP_Dropdown    _applicantSortDropdown;
        [SerializeField] private Transform       _applicantScrollContent;
        [SerializeField] private Button          _applicantListBackButton;
        [SerializeField] private Button          _finalHireButton;

        [Header("Tab_Hire — Panel_ApplicantDetail")]
        [SerializeField] private GameObject _panelApplicantDetail;
        [SerializeField] private Transform  _applicantDetailContent;
        [SerializeField] private Button     _hireButton;
        [SerializeField] private Button     _cancelHireButton;
        [SerializeField] private Button     _applicantDetailBackButton;

        [Header("Tab_Fire")]
        [SerializeField] private GameObject      _tabFire;
        [SerializeField] private TextMeshProUGUI _fireCountLabel;
        [SerializeField] private TMP_Dropdown    _fireSortDropdown;
        [SerializeField] private Transform       _fireListContent;
        [SerializeField] private GameObject      _firePanelList;
        [SerializeField] private GameObject      _firePanelDetail;
        [SerializeField] private Transform       _fireDetailContent;
        [SerializeField] private Button          _fireConfirmButton;
        [SerializeField] private Button          _fireBackButton;

        [Header("Tab_Education")]
        [SerializeField] private GameObject      _tabEducation;
        [SerializeField] private TextMeshProUGUI _educationCountLabel;
        [SerializeField] private TMP_Dropdown    _educationSortDropdown;
        [SerializeField] private Transform       _educationListContent;
        [SerializeField] private GameObject      _educationPanelList;
        [SerializeField] private GameObject      _educationPanelDetail;
        [SerializeField] private Transform       _educationDetailContent;
        [SerializeField] private Button          _educationButton;
        [SerializeField] private Button          _educationDetailBackButton;
        [SerializeField] private GameObject      _panelEducationCourse;
        [SerializeField] private Button          _educationCourseConfirmButton;
        [SerializeField] private Button          _educationCourseBackButton;

        // Tab 이벤트
        public Observable<Unit> OnEmployeeManageTabClicked => _employeeManageTabButton.OnClickAsObservable();
        public Observable<Unit> OnHireTabClicked           => _hireTabButton.OnClickAsObservable();
        public Observable<Unit> OnFireTabClicked           => _fireTabButton.OnClickAsObservable();
        public Observable<Unit> OnEducationTabClicked      => _educationTabButton.OnClickAsObservable();

        // Tab_EmployeeManage 이벤트
        public Observable<int>  OnEmployeeManageSortChanged => _employeeManageSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnEmployeeManageEducationClicked => _employeeManageEducationButton.OnClickAsObservable();
        public Observable<Unit> OnEmployeeManageFireClicked      => _employeeManageFireButton.OnClickAsObservable();
        public Observable<Unit> OnEmployeeManageBackClicked      => _employeeManageBackButton.OnClickAsObservable();

        // Tab_Hire 이벤트
        public Observable<Unit> OnRecruitClicked          => _recruitButton.OnClickAsObservable();
        public Observable<Unit> OnApplicantClicked        => _applicantButton.OnClickAsObservable();
        public Observable<Unit> OnRecruitBackClicked      => _recruitBackButton.OnClickAsObservable();
        public Observable<Unit> OnRecruitConfirmClicked   => _recruitConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnApplicantListBackClicked   => _applicantListBackButton.OnClickAsObservable();
        public Observable<int>  OnApplicantSortChanged    => _applicantSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnFinalHireClicked        => _finalHireButton.OnClickAsObservable();
        public Observable<Unit> OnHireClicked             => _hireButton.OnClickAsObservable();
        public Observable<Unit> OnCancelHireClicked       => _cancelHireButton.OnClickAsObservable();
        public Observable<Unit> OnApplicantDetailBackClicked => _applicantDetailBackButton.OnClickAsObservable();

        // Tab_Fire 이벤트
        public Observable<int>  OnFireSortChanged     => _fireSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnFireConfirmClicked  => _fireConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnFireBackClicked     => _fireBackButton.OnClickAsObservable();

        // Tab_Education 이벤트
        public Observable<int>  OnEducationSortChanged        => _educationSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnEducationClicked            => _educationButton.OnClickAsObservable();
        public Observable<Unit> OnEducationDetailBackClicked  => _educationDetailBackButton.OnClickAsObservable();
        public Observable<Unit> OnEducationCourseConfirmClicked => _educationCourseConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnEducationCourseBackClicked  => _educationCourseBackButton.OnClickAsObservable();

        // Content Transform (Presenter에서 프리팹 Instantiate 위치로 사용)
        public Transform EmployeeGridContent       => _employeeGridContent;
        public Transform EmployeeManageDetailContent => _employeeManageDetailContent;
        public Transform ApplicantScrollContent    => _applicantScrollContent;
        public Transform ApplicantDetailContent    => _applicantDetailContent;
        public Transform FireListContent           => _fireListContent;
        public Transform FireDetailContent         => _fireDetailContent;
        public Transform EducationListContent      => _educationListContent;
        public Transform EducationDetailContent    => _educationDetailContent;

        private void Awake()
        {
            _hrPopup.SetActive(false);

            // Tab_EmployeeManage를 기본 탭으로
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

            _finalHireButton.interactable     = false;
            _hireButton.interactable          = false;
            _cancelHireButton.interactable    = false;
            _educationButton.interactable     = false;
            _educationCourseConfirmButton.interactable = false;
            _recruitConfirmButton.interactable = false;
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
            { _sliderPlanning, _sliderArt, _sliderDev, _sliderQA, _sliderMarketing };

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

        public void ShowEducationCourse()
        {
            _panelEducationCourse.SetActive(true);
        }

        // 수치 표시
        public void SetEmployeeManageCountLabel(int count) => _employeeManageCountLabel.text = $"직원 {count}명";
        public void SetFireCountLabel(int count)           => _fireCountLabel.text = $"직원 {count}명";
        public void SetEducationCountLabel(int count)      => _educationCountLabel.text = $"직원 {count}명";
        public void SetJobCategoryTag(string category)     => _jobCategoryTag.text = category;
        public void SetRecruitCountTag(int count)          => _recruitCountTag.text = count.ToString();

        public void SetRecruitButtonLabel(bool isRecruiting)
            => _recruitButtonLabel.text = isRecruiting ? "직원모집중" : "직원모집";

        public void SetApplicantButtonLabel(bool isHired)
            => _applicantButtonLabel.text = isHired ? "채용완료" : "지원자리스트";

        // interactable 제어
        public void SetRecruitButtonInteractable(bool interactable)
            => _recruitButton.interactable = interactable;

        public void SetRecruitConfirmInteractable(bool interactable)
            => _recruitConfirmButton.interactable = interactable;

        public void SetFinalHireInteractable(bool interactable)
            => _finalHireButton.interactable = interactable;

        public void SetHireButtonInteractable(bool interactable)
            => _hireButton.interactable = interactable;

        public void SetCancelHireButtonInteractable(bool interactable)
            => _cancelHireButton.interactable = interactable;

        public void SetEducationButtonInteractable(bool interactable)
            => _educationButton.interactable = interactable;

        public void SetEducationCourseConfirmInteractable(bool interactable)
            => _educationCourseConfirmButton.interactable = interactable;
    }

    public enum HRTab { EmployeeManage, Hire, Fire, Education }
}