using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.ProjectPopup 담당 View.
    /// 탭 전환, 패널 전환, 버튼 이벤트 발행.
    /// 프리팹 동적 생성 및 데이터 바인딩은 Presenter에서 담당.
    /// </summary>
    public sealed class ProjectView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject _projectPopup;

        [Header("TabButtons")]
        [SerializeField] private Button _newProjectTabButton;
        [SerializeField] private Button _inProgressTabButton;

        [Header("Tab_NewProject — Panel_SlotSelect")]
        [SerializeField] private GameObject _tabNewProject;
        [SerializeField] private GameObject _panelSlotSelect;
        [SerializeField] private Transform  _slotScrollContent;

        [Header("Tab_NewProject — Panel_ProjectSetup")]
        [SerializeField] private GameObject      _panelProjectSetup;
        [SerializeField] private TMP_InputField  _projectNameInput;
        [SerializeField] private Button          _scaleCardSmall;
        [SerializeField] private Button          _scaleCardMedium;
        [SerializeField] private Button          _scaleCardLarge;
        [SerializeField] private GameObject      _scaleCardMediumLock;
        [SerializeField] private GameObject      _scaleCardLargeLock;
        [SerializeField] private Button          _projectSetupBackButton;
        [SerializeField] private Button          _projectSetupNextButton;

        [Header("Tab_NewProject — Panel_StaffAssign")]
        [SerializeField] private GameObject      _panelStaffAssign;
        [SerializeField] private TextMeshProUGUI _minStaffLabel;
        [SerializeField] private Button          _resetButton;
        [SerializeField] private TMP_Dropdown    _staffSortDropdown;
        [SerializeField] private Transform       _staffGridContent;
        [SerializeField] private Button          _synergyButton;
        [SerializeField] private Button          _staffAssignBackButton;
        [SerializeField] private Button          _staffAssignConfirmButton;

        [Header("Tab_InProgress")]
        [SerializeField] private GameObject      _tabInProgress;
        [SerializeField] private TMP_Dropdown    _inProgressSortDropdown;
        [SerializeField] private Transform       _inProgressListContent;
        [SerializeField] private TextMeshProUGUI _inProgressEmptyLabel;

        [Header("Tab_InProgress — Panel_ProjectDetail")]
        [SerializeField] private GameObject      _panelProjectDetail;
        [SerializeField] private Button          _projectDetailBackButton;
        [SerializeField] private TextMeshProUGUI _gameNameValue;
        [SerializeField] private TextMeshProUGUI _scaleValue;
        [SerializeField] private TextMeshProUGUI _genreValue;
        [SerializeField] private TextMeshProUGUI _artValue;
        [SerializeField] private TextMeshProUGUI _engineValue;
        [SerializeField] private Slider          _progressBar;
        [SerializeField] private TextMeshProUGUI _progressValueLabel;
        [SerializeField] private GameObject      _operationGroup;
        [SerializeField] private GameObject      _operationEmptyLabel;
        [SerializeField] private GameObject      _statusGroup;
        [SerializeField] private GameObject      _userCountGroup;
        [SerializeField] private GameObject      _salesGroup;
        [SerializeField] private GameObject      _maintenanceGroup;
        [SerializeField] private GameObject      _profitGroup;
        [SerializeField] private GameObject      _revenueGraph;
        [SerializeField] private TextMeshProUGUI _statusValue;
        [SerializeField] private TextMeshProUGUI _userCountValue;
        [SerializeField] private TextMeshProUGUI _salesValue;
        [SerializeField] private TextMeshProUGUI _maintenanceValue;
        [SerializeField] private TextMeshProUGUI _profitValue;
        [SerializeField] private Button          _serviceStopButton;

        [Header("Panel_StaffDetailPopup")]
        [SerializeField] private GameObject _panelStaffDetailPopup;
        [SerializeField] private Transform  _staffDetailContent;
        [SerializeField] private Button     _staffDetailCloseButton;

        // Tab 이벤트
        public Observable<Unit> OnNewProjectTabClicked  => _newProjectTabButton.OnClickAsObservable();
        public Observable<Unit> OnInProgressTabClicked  => _inProgressTabButton.OnClickAsObservable();

        // Tab_NewProject 이벤트
        public Observable<Unit>   OnProjectSetupBackClicked    => _projectSetupBackButton.OnClickAsObservable();
        public Observable<Unit>   OnProjectSetupNextClicked    => _projectSetupNextButton.OnClickAsObservable();
        public Observable<Unit>   OnScaleCardSmallClicked      => _scaleCardSmall.OnClickAsObservable();
        public Observable<Unit>   OnScaleCardMediumClicked     => _scaleCardMedium.OnClickAsObservable();
        public Observable<Unit>   OnScaleCardLargeClicked      => _scaleCardLarge.OnClickAsObservable();
        public Observable<string> OnProjectNameChanged         => _projectNameInput.onValueChanged.AsObservable();
        public Observable<Unit>   OnResetClicked               => _resetButton.OnClickAsObservable();
        public Observable<int>    OnStaffSortChanged           => _staffSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit>   OnSynergyClicked             => _synergyButton.OnClickAsObservable();
        public Observable<Unit>   OnStaffAssignBackClicked     => _staffAssignBackButton.OnClickAsObservable();
        public Observable<Unit>   OnStaffAssignConfirmClicked  => _staffAssignConfirmButton.OnClickAsObservable();

        // Tab_InProgress 이벤트
        public Observable<int>  OnInProgressSortChanged    => _inProgressSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnProjectDetailBackClicked => _projectDetailBackButton.OnClickAsObservable();
        public Observable<Unit> OnServiceStopClicked       => _serviceStopButton.OnClickAsObservable();

        // Panel_StaffDetailPopup 이벤트
        public Observable<Unit> OnStaffDetailCloseClicked => _staffDetailCloseButton.OnClickAsObservable();

        // Content Transform (Presenter에서 프리팹 Instantiate 위치로 사용)
        public Transform SlotScrollContent      => _slotScrollContent;
        public Transform StaffGridContent       => _staffGridContent;
        public Transform InProgressListContent  => _inProgressListContent;
        public Transform StaffDetailContent     => _staffDetailContent;

        private void Awake()
        {
            _projectPopup.SetActive(false);

            ShowTab(ProjectTab.NewProject);

            _panelProjectSetup.SetActive(false);
            _panelStaffAssign.SetActive(false);
            _panelProjectDetail.SetActive(false);
            _panelStaffDetailPopup.SetActive(false);

            _scaleCardMediumLock.SetActive(true);
            _scaleCardLargeLock.SetActive(true);

            _projectSetupNextButton.interactable   = false;
            _staffAssignConfirmButton.interactable = false;
            _serviceStopButton.interactable        = false;
        }

        public void Show() => _projectPopup.SetActive(true);
        public void Hide() => _projectPopup.SetActive(false);

        public void ShowTab(ProjectTab tab)
        {
            _tabNewProject.SetActive(tab == ProjectTab.NewProject);
            _tabInProgress.SetActive(tab == ProjectTab.InProgress);
        }

        // Tab_NewProject 패널 전환
        public void ShowSlotSelect()
        {
            _panelSlotSelect.SetActive(true);
            _panelProjectSetup.SetActive(false);
            _panelStaffAssign.SetActive(false);
        }

        public void ShowProjectSetup()
        {
            _panelSlotSelect.SetActive(false);
            _panelProjectSetup.SetActive(true);
            _panelStaffAssign.SetActive(false);
        }

        public void ShowStaffAssign()
        {
            _panelProjectSetup.SetActive(false);
            _panelStaffAssign.SetActive(true);
        }

        // Tab_InProgress 패널 전환
        public void ShowInProgressList()
        {
            _panelProjectDetail.SetActive(false);
        }

        public void ShowProjectDetail()
        {
            _panelProjectDetail.SetActive(true);
        }

        public void ShowStaffDetailPopup()  => _panelStaffDetailPopup.SetActive(true);
        public void HideStaffDetailPopup()  => _panelStaffDetailPopup.SetActive(false);

        // 규모 카드 잠금 해제
        public void SetScaleMediumLocked(bool locked) => _scaleCardMediumLock.SetActive(locked);
        public void SetScaleLargeLocked(bool locked)  => _scaleCardLargeLock.SetActive(locked);

        // 수치 표시
        public void SetMinStaffLabel(int planning, int planningMax,
            int art, int artMax, int dev, int devMax)
        {
            _minStaffLabel.text = $"최소 인원: 기획 {planning}/{planningMax}, 아트 {art}/{artMax}, 개발 {dev}/{devMax}";
        }

        public void SetProgressBar(float value)
        {
            _progressBar.value      = value;
            _progressValueLabel.text = $"{value * 100f:F1}%";
        }

        public void SetProjectDetailInfo(string gameName, string scale,
            string genre, string art, string engine)
        {
            _gameNameValue.text = gameName;
            _scaleValue.text    = scale;
            _genreValue.text    = genre;
            _artValue.text      = art;
            _engineValue.text   = engine;
        }

        /// <summary>
        /// 서비스 상태 여부에 따라 OperationGroup 표시 전환.
        /// </summary>
        public void SetStatusGroupVisible(bool isInService)
        {
            _operationGroup.SetActive(true);
            _operationEmptyLabel.SetActive(!isInService);
            _statusGroup.SetActive(isInService);
            _userCountGroup.SetActive(isInService);
            _salesGroup.SetActive(isInService);
            _maintenanceGroup.SetActive(isInService);
            _profitGroup.SetActive(isInService);
            _revenueGraph.SetActive(isInService);
        }

        public void SetStatusValue(string status)         => _statusValue.text = status;
        public void SetUserCountValue(string value, bool isUp)   => SetColoredValue(_userCountValue, value, isUp);
        public void SetSalesValue(string value, bool isUp)       => SetColoredValue(_salesValue, value, isUp);
        public void SetMaintenanceValue(string value)     => _maintenanceValue.text = value;
        public void SetProfitValue(string value, bool isUp)      => SetColoredValue(_profitValue, value, isUp);

        // 상승 빨강 / 하락 파랑
        private void SetColoredValue(TextMeshProUGUI label, string value, bool isUp)
        {
            label.text  = value;
            label.color = isUp ? Color.red : Color.blue;
        }

        public void SetInProgressEmptyVisible(bool visible) => _inProgressEmptyLabel.gameObject.SetActive(visible);

        // interactable 제어
        public void SetProjectSetupNextInteractable(bool interactable)
            => _projectSetupNextButton.interactable = interactable;

        public void SetStaffAssignConfirmInteractable(bool interactable)
            => _staffAssignConfirmButton.interactable = interactable;

        public void SetServiceStopInteractable(bool interactable)
            => _serviceStopButton.interactable = interactable;
    }

    public enum ProjectTab { NewProject, InProgress }
}