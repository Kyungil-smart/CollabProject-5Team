using System.Collections.Generic;
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
        private const string RevenueGraphLinePrefix = "RevenueLine_";
        private const int RevenueGraphSlotCount = 4;

        [Header("Popup")]
        [SerializeField] private GameObject _projectPopup;

        [Header("TabButtons")]
        [SerializeField] private Button _newProjectTabButton;
        [SerializeField] private Button _inProgressTabButton;
        [SerializeField] private Button _completedTabButton;

        [Header("TabButton Sprites")]
        [SerializeField] private Sprite _tabActiveSprite;
        [SerializeField] private Sprite _tabInactiveSprite;
        [SerializeField] private Color _tabActiveLabelColor;
        [SerializeField] private Color _tabInactiveLabelColor;

        [Header("Tab_NewProject")]
        [SerializeField] private GameObject _tabNewProject;
        [SerializeField] private CanvasGroup _tabNewProjectCanvasGroup;
        [SerializeField] private GameObject _contentFrame;
        [SerializeField] private TextMeshProUGUI _activeProjectLabel;

        [Header("Tab_NewProject — Panel_ProjectSetup")]
        [SerializeField] private GameObject _panelProjectSetup;
        [SerializeField] private TMP_InputField _projectNameInput;
        [SerializeField] private Button _scaleCardSmall;
        [SerializeField] private Button _scaleCardMedium;
        [SerializeField] private Button _scaleCardLarge;
        [SerializeField] private GameObject _scaleCardSmallSelectImg;
        [SerializeField] private GameObject _scaleCardMediumSelectImg;
        [SerializeField] private GameObject _scaleCardLargeSelectImg;
        [SerializeField] private GameObject _scaleCardMediumLock;
        [SerializeField] private GameObject _scaleCardLargeLock;
        [SerializeField] private Button _projectSetupNextButton;

        [Header("Tab_NewProject — Panel_StaffAssign")]
        [SerializeField] private GameObject _panelStaffAssign;
        [SerializeField] private TextMeshProUGUI _minStaffLabel;
        [SerializeField] private Button _resetButton;
        [SerializeField] private TMP_Dropdown _staffSortDropdown;
        [SerializeField] private Transform _staffGridContent;
        [SerializeField] private Button _staffAssignBackButton;
        [SerializeField] private Button _staffAssignConfirmButton;

        [Header("Tab_NewProject — Panel_StaffAssign — ConfirmButton Sprites")]
        [SerializeField] private Sprite _confirmActiveSprite;
        [SerializeField] private Sprite _confirmInactiveSprite;

        [Header("Tab_InProgress")]
        [SerializeField] private GameObject _tabInProgress;
        [SerializeField] private TMP_Dropdown _inProgressSortDropdown;
        [SerializeField] private Transform _inProgressListContent;
        [SerializeField] private TextMeshProUGUI _inProgressEmptyLabel;

        [Header("Tab_InProgress — Panel_ProjectDetail")]
        [SerializeField] private GameObject _panelProjectDetail;
        [SerializeField] private Button _projectDetailBackButton;
        [SerializeField] private TextMeshProUGUI _gameNameValue;
        [SerializeField] private TextMeshProUGUI _scaleValue;
        [SerializeField] private TextMeshProUGUI _genreValue;
        [SerializeField] private TextMeshProUGUI _artValue;
        [SerializeField] private TextMeshProUGUI _engineValue;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _progressValueLabel;
        [SerializeField] private GameObject _operationGroup;
        [SerializeField] private GameObject _operationEmptyLabel;
        [SerializeField] private GameObject _userCountGroup;
        [SerializeField] private TextMeshProUGUI _userCountValue;
        [SerializeField] private GameObject _salesGroup;
        [SerializeField] private TextMeshProUGUI _salesValue;
        [SerializeField] private GameObject _maintenanceGroup;
        [SerializeField] private TextMeshProUGUI _maintenanceValue;
        [SerializeField] private GameObject _profitGroup;
        [SerializeField] private TextMeshProUGUI _profitValue;
        [SerializeField] private GameObject _revenueGraph;
        [SerializeField] private Button _serviceStopButton;
        [SerializeField] private Button _updateButton;

        [Header("Tab_InProgress — Panel_UpdateManagement")]
        [SerializeField] private GameObject _panelUpdateManagement;
        [SerializeField] private TextMeshProUGUI _updateProjectNameLabel;
        [SerializeField] private Button _updateItem_Plan;
        [SerializeField] private Button _updateItem_Art;
        [SerializeField] private Button _updateItem_Dev;
        [SerializeField] private GameObject _selectImg_Plan;
        [SerializeField] private GameObject _selectImg_Art;
        [SerializeField] private GameObject _selectImg_Dev;
        [SerializeField] private GameObject _completedOverlay_Plan;
        [SerializeField] private GameObject _completedOverlay_Art;
        [SerializeField] private GameObject _completedOverlay_Dev;
        [SerializeField] private Button _updateBackButton;
        [SerializeField] private Button _updateConfirmButton;

        [Header("Tab_InProgress — Panel_UpdateManagement — ConfirmButton Sprites")]
        [SerializeField] private Sprite _updateConfirmActiveSprite;
        [SerializeField] private Sprite _updateConfirmInactiveSprite;

        [Header("Tab_Completed")]
        [SerializeField] private GameObject _tabCompleted;
        [SerializeField] private TMP_Dropdown _completedSortDropdown;
        [SerializeField] private Transform _completedListContent;
        [SerializeField] private TextMeshProUGUI _completedEmptyLabel;

        [Header("Tab_Completed — Panel_ProjectDetail")]
        [SerializeField] private GameObject _completedPanelProjectDetail;
        [SerializeField] private Button _completedDetailBackButton;
        [SerializeField] private TextMeshProUGUI _completedGameNameValue;
        [SerializeField] private TextMeshProUGUI _completedScaleValue;
        [SerializeField] private TextMeshProUGUI _completedGenreValue;
        [SerializeField] private TextMeshProUGUI _completedArtValue;
        [SerializeField] private TextMeshProUGUI _completedEngineValue;
        [SerializeField] private Slider _completedProgressBar;
        [SerializeField] private TextMeshProUGUI _completedProgressValueLabel;
        [SerializeField] private GameObject _completedOperationGroup;
        [SerializeField] private GameObject _completedOperationEmptyLabel;
        [SerializeField] private GameObject _completedUserCountGroup;
        [SerializeField] private TextMeshProUGUI _completedUserCountValue;
        [SerializeField] private GameObject _completedSalesGroup;
        [SerializeField] private TextMeshProUGUI _completedSalesValue;
        [SerializeField] private GameObject _completedMaintenanceGroup;
        [SerializeField] private TextMeshProUGUI _completedMaintenanceValue;
        [SerializeField] private GameObject _completedProfitGroup;
        [SerializeField] private TextMeshProUGUI _completedProfitValue;
        [SerializeField] private GameObject _completedRevenueGraph;

        [Header("Panel_StaffDetailPopup")]
        [SerializeField] private GameObject _panelStaffDetailPopup;
        [SerializeField] private Transform _staffDetailContent;
        [SerializeField] private Button _staffDetailCloseButton;

        // Tab 이벤트
        public Observable<Unit> OnNewProjectTabClicked => _newProjectTabButton.OnClickAsObservable();
        public Observable<Unit> OnInProgressTabClicked => _inProgressTabButton.OnClickAsObservable();
        public Observable<Unit> OnCompletedTabClicked => _completedTabButton.OnClickAsObservable();

        // Tab_NewProject 이벤트
        public Observable<Unit> OnProjectSetupNextClicked => _projectSetupNextButton.OnClickAsObservable();
        public Observable<Unit> OnScaleCardSmallClicked => _scaleCardSmall.OnClickAsObservable();
        public Observable<Unit> OnScaleCardMediumClicked => _scaleCardMedium.OnClickAsObservable();
        public Observable<Unit> OnScaleCardLargeClicked => _scaleCardLarge.OnClickAsObservable();
        public Observable<string> OnProjectNameChanged => _projectNameInput.onValueChanged.AsObservable();
        public Observable<Unit> OnResetClicked => _resetButton.OnClickAsObservable();
        public Observable<int> OnStaffSortChanged => _staffSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnStaffAssignBackClicked => _staffAssignBackButton.OnClickAsObservable();
        public Observable<Unit> OnStaffAssignConfirmClicked => _staffAssignConfirmButton.OnClickAsObservable();

        // Tab_InProgress 이벤트
        public Observable<int> OnInProgressSortChanged => _inProgressSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnProjectDetailBackClicked => _projectDetailBackButton.OnClickAsObservable();
        public Observable<Unit> OnServiceStopClicked => _serviceStopButton.OnClickAsObservable();
        public Observable<Unit> OnUpdateClicked => _updateButton.OnClickAsObservable();

        // Panel_UpdateManagement 이벤트
        public Observable<Unit> OnUpdateItemPlanClicked => _updateItem_Plan.OnClickAsObservable();
        public Observable<Unit> OnUpdateItemArtClicked => _updateItem_Art.OnClickAsObservable();
        public Observable<Unit> OnUpdateItemDevClicked => _updateItem_Dev.OnClickAsObservable();
        public Observable<Unit> OnUpdateBackClicked => _updateBackButton.OnClickAsObservable();
        public Observable<Unit> OnUpdateConfirmClicked => _updateConfirmButton.OnClickAsObservable();

        // Tab_Completed 이벤트
        public Observable<int> OnCompletedSortChanged => _completedSortDropdown.OnValueChangedAsObservable();
        public Observable<Unit> OnCompletedDetailBackClicked => _completedDetailBackButton.OnClickAsObservable();

        // Panel_StaffDetailPopup 이벤트
        public Observable<Unit> OnStaffDetailCloseClicked => _staffDetailCloseButton.OnClickAsObservable();

        // Content Transform (Presenter에서 프리팹 Instantiate 위치로 사용)
        public Transform StaffGridContent => _staffGridContent;
        public Transform InProgressListContent => _inProgressListContent;
        public Transform CompletedListContent => _completedListContent;
        public Transform StaffDetailContent => _staffDetailContent;
        public string ProjectNameInput => _projectNameInput.text;

        // 현재 정렬 인덱스
        public int StaffSortIndex => _staffSortDropdown.value;
        public int InProgressSortIndex => _inProgressSortDropdown.value;
        public int CompletedSortIndex => _completedSortDropdown.value;

        private void Awake()
        {
            ShowTab(ProjectTab.NewProject);

            _activeProjectLabel.gameObject.SetActive(false);
            _panelStaffAssign.SetActive(false);
            _panelProjectDetail.SetActive(false);
            _panelUpdateManagement.SetActive(false);
            _completedPanelProjectDetail.SetActive(false);
            _panelStaffDetailPopup.SetActive(false);

            _scaleCardMediumLock.SetActive(true);
            _scaleCardLargeLock.SetActive(true);
            _scaleCardMedium.interactable = false;
            _scaleCardLarge.interactable = false;

            _scaleCardSmallSelectImg.SetActive(false);
            _scaleCardMediumSelectImg.SetActive(false);
            _scaleCardLargeSelectImg.SetActive(false);

            _projectSetupNextButton.interactable = false;
            _staffAssignConfirmButton.interactable = false;
            _serviceStopButton.interactable = false;
            _updateButton.interactable = false;
            _updateConfirmButton.interactable = false;
        }

        public void Show() => _projectPopup.SetActive(true);
        public void Hide() => _projectPopup.SetActive(false);
        public bool IsVisible => _projectPopup.activeSelf;

        public void ShowTab(ProjectTab tab)
        {
            _tabNewProject.SetActive(tab == ProjectTab.NewProject);
            _contentFrame.SetActive(tab == ProjectTab.NewProject);
            _panelProjectSetup.SetActive(tab == ProjectTab.NewProject);
            _activeProjectLabel.gameObject.SetActive(false);
            _panelStaffAssign.SetActive(false);

            _tabInProgress.SetActive(tab == ProjectTab.InProgress);
            _panelProjectDetail.SetActive(false);
            _panelUpdateManagement.SetActive(false);

            _tabCompleted.SetActive(tab == ProjectTab.Completed);
            _completedPanelProjectDetail.SetActive(false);

            SetTabButtonState(_newProjectTabButton, tab == ProjectTab.NewProject);
            SetTabButtonState(_inProgressTabButton, tab == ProjectTab.InProgress);
            SetTabButtonState(_completedTabButton, tab == ProjectTab.Completed);
        }

        private void SetTabButtonState(Button button, bool isActive)
        {
            button.image.sprite = isActive ? _tabActiveSprite : _tabInactiveSprite;
            button.GetComponentInChildren<TextMeshProUGUI>().color = isActive ? _tabActiveLabelColor : _tabInactiveLabelColor;
        }

        // Tab_NewProject 패널 전환
        public void ShowProjectSetup()
        {
            _contentFrame.SetActive(true);
            _activeProjectLabel.gameObject.SetActive(false);
            _panelProjectSetup.SetActive(true);
            _panelStaffAssign.SetActive(false);
        }

        public void ShowStaffAssign()
        {
            _contentFrame.SetActive(false);
            _activeProjectLabel.gameObject.SetActive(false);
            _panelProjectSetup.SetActive(false);
            _panelStaffAssign.SetActive(true);
        }

        public void SetActiveProjectWarningVisible(bool hasActiveProject)
        {
            _activeProjectLabel.gameObject.SetActive(hasActiveProject);
            _panelProjectSetup.SetActive(!hasActiveProject);
        }

        /// <summary>
        /// 선택된 규모 카드 SelectIMG 토글.
        /// </summary>
        public void SetScaleCardSelectImg(ProjectSize selectedScale)
        {
            _scaleCardSmallSelectImg.SetActive(selectedScale == ProjectSize.Small);
            _scaleCardMediumSelectImg.SetActive(selectedScale == ProjectSize.Medium);
            _scaleCardLargeSelectImg.SetActive(selectedScale == ProjectSize.Large);
        }

        public void ClearScaleCardSelectImg()
        {
            _scaleCardSmallSelectImg.SetActive(false);
            _scaleCardMediumSelectImg.SetActive(false);
            _scaleCardLargeSelectImg.SetActive(false);
        }

        // Tab_InProgress 패널 전환
        public void ShowInProgressList()
        {
            _panelProjectDetail.SetActive(false);
            _panelUpdateManagement.SetActive(false);
        }

        public void ShowProjectDetail()
        {
            _panelProjectDetail.SetActive(true);
            _panelUpdateManagement.SetActive(false);
        }

        public void ShowUpdateManagement(string projectName)
        {
            _panelProjectDetail.SetActive(false);
            _panelUpdateManagement.SetActive(true);
            _updateProjectNameLabel.text = projectName;
        }

        public void HideUpdateManagement()
        {
            _panelUpdateManagement.SetActive(false);
            _panelProjectDetail.SetActive(true);
        }

        // Tab_Completed 패널 전환
        public void ShowCompletedList()
        {
            _completedPanelProjectDetail.SetActive(false);
        }

        public void ShowCompletedDetail()
        {
            _completedPanelProjectDetail.SetActive(true);
        }

        public void ShowStaffDetailPopup()
        {
            _tabNewProjectCanvasGroup.interactable = false;
            _tabNewProjectCanvasGroup.blocksRaycasts = false;
            _panelStaffDetailPopup.SetActive(true);
        }

        public void HideStaffDetailPopup()
        {
            _panelStaffDetailPopup.SetActive(false);
            _tabNewProjectCanvasGroup.interactable = true;
            _tabNewProjectCanvasGroup.blocksRaycasts = true;
        }

        // 규모 카드 잠금
        public void SetScaleMediumLocked(bool locked)
        {
            _scaleCardMediumLock.SetActive(locked);
            _scaleCardMedium.interactable = !locked;
        }

        public void SetScaleLargeLocked(bool locked)
        {
            _scaleCardLargeLock.SetActive(locked);
            _scaleCardLarge.interactable = !locked;
        }

        public void SetMinStaffLabel(int planning, int planningMax,
            int art, int artMax, int dev, int devMax)
        {
            _minStaffLabel.text = $"최소 인원: 기획 {planning}/{planningMax}, 아트 {art}/{artMax}, 개발 {dev}/{devMax}";
        }

        public void SetProgressBar(float value)
        {
            _progressBar.value = value;
            _progressValueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        public void SetProjectDetailInfo(string gameName, string scale,
            string genre, string art, string engine)
        {
            _gameNameValue.text = gameName;
            _scaleValue.text = scale;
            _genreValue.text = genre;
            _artValue.text = art;
            _engineValue.text = engine;
        }

        /// <summary>
        /// 프로젝트 상태에 따라 OperationGroup 내 표시 전환.
        /// isInService = true 면 운영 수치 표시, false 면 빈 문구 표시.
        /// </summary>
        public void SetOperationGroupVisible(bool isInService)
        {
            _operationGroup.SetActive(true);
            _operationEmptyLabel.SetActive(!isInService);
            _userCountGroup.SetActive(isInService);
            _salesGroup.SetActive(isInService);
            _maintenanceGroup.SetActive(isInService);
            _profitGroup.SetActive(isInService);
            _revenueGraph.SetActive(isInService);
        }

        public void SetUserCountValue(string value, bool isUp) => SetColoredValue(_userCountValue, value, isUp);
        public void SetUserCountValue(string value, bool isUp, bool highlight)
        {
            if (highlight)
            {
                SetColoredValue(_userCountValue, value, isUp);
                return;
            }

            SetPlainValue(_userCountValue, value);
        }

        public void SetSalesValue(string value, bool isUp) => SetColoredValue(_salesValue, value, isUp);
        public void SetSalesValue(string value, bool isUp, bool highlight)
        {
            if (highlight)
            {
                SetColoredValue(_salesValue, value, isUp);
                return;
            }

            SetPlainValue(_salesValue, value);
        }

        public void SetMaintenanceValue(string value) => _maintenanceValue.text = value;
        public void SetProfitValue(string value, bool isUp) => SetColoredValue(_profitValue, value, isUp);
        public void SetProfitValue(string value, bool isUp, bool highlight)
        {
            if (highlight)
            {
                SetColoredValue(_profitValue, value, isUp);
                return;
            }

            SetPlainValue(_profitValue, value);
        }

        public void SetOperationPendingValues()
        {
            SetPlainValue(_userCountValue, "집계 전");
            SetPlainValue(_salesValue, "집계 전");
            _maintenanceValue.text = "집계 전";
            SetPlainValue(_profitValue, "집계 전");
            DrawRevenueGraph(_revenueGraph, null);
        }

        public void SetRevenueGraphValues(IReadOnlyList<int> values)
        {
            DrawRevenueGraph(_revenueGraph, values);
        }

        public void SetInProgressEmptyVisible(bool visible)
            => _inProgressEmptyLabel.gameObject.SetActive(visible);

        // interactable 제어
        public void SetProjectSetupNextInteractable(bool interactable)
            => _projectSetupNextButton.interactable = interactable;

        public void SetStaffAssignConfirmInteractable(bool interactable)
        {
            _staffAssignConfirmButton.interactable = interactable;
            _staffAssignConfirmButton.image.sprite = interactable ? _confirmActiveSprite : _confirmInactiveSprite;
        }

        public void SetServiceStopInteractable(bool interactable)
            => _serviceStopButton.interactable = interactable;

        public void SetUpdateButtonInteractable(bool interactable)
            => _updateButton.interactable = interactable;

        public void SetUpdateConfirmInteractable(bool interactable)
        {
            _updateConfirmButton.interactable = interactable;
            _updateConfirmButton.image.sprite = interactable ? _updateConfirmActiveSprite : _updateConfirmInactiveSprite;
        }

        /// <summary>
        /// 지난주 완료된 업데이트 항목 오버레이 표시.
        /// </summary>
        public void SetUpdateItemCompletedOverlay(UpdatePart part, bool completed)
        {
            var overlay = part switch
            {
                UpdatePart.Plan => _completedOverlay_Plan,
                UpdatePart.Art => _completedOverlay_Art,
                _ => _completedOverlay_Dev,
            };
            overlay.SetActive(completed);

            var button = part switch
            {
                UpdatePart.Plan => _updateItem_Plan,
                UpdatePart.Art => _updateItem_Art,
                _ => _updateItem_Dev,
            };
            button.interactable = !completed;
        }

        /// <summary>
        /// 업데이트 항목 선택 상태 SelectIMG 토글.
        /// </summary>
        public void SetUpdateItemSelectImg(UpdatePart? selectedPart)
        {
            _selectImg_Plan.SetActive(selectedPart == UpdatePart.Plan);
            _selectImg_Art.SetActive(selectedPart == UpdatePart.Art);
            _selectImg_Dev.SetActive(selectedPart == UpdatePart.Dev);
        }

        // Tab_Completed 수치 표시
        public void SetCompletedProgressBar(float value)
        {
            _completedProgressBar.value = value;
            _completedProgressValueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        public void SetCompletedProjectDetailInfo(string gameName, string scale,
            string genre, string art, string engine)
        {
            _completedGameNameValue.text = gameName;
            _completedScaleValue.text = scale;
            _completedGenreValue.text = genre;
            _completedArtValue.text = art;
            _completedEngineValue.text = engine;
        }

        /// <summary>
        /// 종료 프로젝트 상태에 따라 OperationGroup 표시 전환.
        /// isServiceEnded = true 면 최종 수치 표시, false(개발중단) 면 빈 문구 표시.
        /// </summary>
        public void SetCompletedOperationGroupVisible(bool isServiceEnded)
        {
            _completedOperationGroup.SetActive(true);
            _completedOperationEmptyLabel.SetActive(!isServiceEnded);
            _completedUserCountGroup.SetActive(isServiceEnded);
            _completedSalesGroup.SetActive(isServiceEnded);
            _completedMaintenanceGroup.SetActive(isServiceEnded);
            _completedProfitGroup.SetActive(isServiceEnded);
            _completedRevenueGraph.SetActive(isServiceEnded);
        }

        public void SetCompletedUserCountValue(string value) => _completedUserCountValue.text = value;
        public void SetCompletedSalesValue(string value) => _completedSalesValue.text = value;
        public void SetCompletedMaintenanceValue(string value) => _completedMaintenanceValue.text = value;
        public void SetCompletedProfitValue(string value) => _completedProfitValue.text = value;

        public void SetCompletedOperationPendingValues()
        {
            _completedUserCountValue.text = "집계 전";
            _completedSalesValue.text = "집계 전";
            _completedMaintenanceValue.text = "집계 전";
            _completedProfitValue.text = "집계 전";
            DrawRevenueGraph(_completedRevenueGraph, null);
        }

        public void SetCompletedRevenueGraphValues(IReadOnlyList<int> values)
        {
            DrawRevenueGraph(_completedRevenueGraph, values);
        }

        public void SetCompletedEmptyVisible(bool visible)
            => _completedEmptyLabel.gameObject.SetActive(visible);

        // 상승 빨강 / 하락 파랑
        private void SetColoredValue(TextMeshProUGUI label, string value, bool isUp)
        {
            label.text = value;
            label.color = isUp ? Color.red : Color.blue;
        }

        private void SetPlainValue(TextMeshProUGUI label, string value)
        {
            label.text = value;
            label.color = Color.black;
        }

        private void DrawRevenueGraph(GameObject graphRoot, IReadOnlyList<int> values)
        {
            if (graphRoot == null) return;

            RectTransform graphContent = GetGraphContent(graphRoot);
            if (graphContent == null) return;

            ClearRevenueGraphLines(graphContent);
            if (values == null || values.Count < 2) return;

            int maxValue = 1;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] > maxValue)
                    maxValue = values[i];
            }

            Vector2 size = graphContent.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                size = graphContent.sizeDelta;
            if (size.x <= 0f || size.y <= 0f) return;

            int startSlot = Mathf.Max(0, RevenueGraphSlotCount - values.Count);
            Vector2 previous = GetGraphPoint(values[0], startSlot, maxValue, size);
            for (int i = 1; i < values.Count; i++)
            {
                Vector2 current = GetGraphPoint(values[i], startSlot + i, maxValue, size);
                CreateRevenueGraphLine(graphContent, previous, current, i);
                previous = current;
            }
        }

        private RectTransform GetGraphContent(GameObject graphRoot)
        {
            Transform content = graphRoot.transform.Find("GraphContent");
            if (content != null)
                return content as RectTransform;

            return graphRoot.transform as RectTransform;
        }

        private void ClearRevenueGraphLines(RectTransform graphContent)
        {
            for (int i = graphContent.childCount - 1; i >= 0; i--)
            {
                Transform child = graphContent.GetChild(i);
                if (child.name.StartsWith(RevenueGraphLinePrefix))
                    Destroy(child.gameObject);
            }
        }

        private Vector2 GetGraphPoint(int value, int slotIndex, int maxValue, Vector2 size)
        {
            float x = Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, slotIndex / (float)(RevenueGraphSlotCount - 1));
            float y = Mathf.Lerp(-size.y * 0.5f, size.y * 0.5f, Mathf.Clamp01(value / (float)maxValue));
            return new Vector2(x, y);
        }

        private void CreateRevenueGraphLine(RectTransform graphContent, Vector2 from, Vector2 to, int index)
        {
            var line = new GameObject($"{RevenueGraphLinePrefix}{index}", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(graphContent, false);

            var image = line.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            var rect = line.GetComponent<RectTransform>();
            Vector2 direction = to - from;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = from + direction * 0.5f;
            rect.sizeDelta = new Vector2(direction.magnitude, 3f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }

    public enum ProjectTab { NewProject, InProgress, Completed }

    public enum UpdatePart { Plan, Art, Dev }
}