using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using GameDevTycoon.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.ProjectPopup Presenter.
    /// 프로젝트 설정 → 인원 배치 흐름 및 진행/종료 프로젝트 표시 처리.
    /// 신규 프로젝트는 한 번에 한 개만 진행 가능. 진행 중이면 안내 문구 표시 후 입력 비활성.
    /// StaffCardView, ProjectListItemView 바인딩은 IBindable 연결 후 활성화.
    /// </summary>
    public sealed class ProjectPresenter : MonoBehaviour, IBottomNightUI
    {
        private const ProjectSize UnselectedScale = (ProjectSize)(-1);

        [SerializeField] private ProjectView _view;
        [SerializeField] private AlertView _alertView;
        [SerializeField] private HUDPresenter _hudPresenter;

        [Header("프리팹")]
        [SerializeField] private List<StaffCardPrefabEntry> _staffCardPrefabs;
        [SerializeField] private GameObject _projectListItemPrefab;
        [SerializeField] private GameObject _staffDetailPrefab;

        [Header("업데이트 데이터")]
        [SerializeField] private List<ProjectUpdateSO> _allUpdateSos = new();

        private ProjectSize _selectedScale;
        private Project _currentDetailProject;
        private ProjectCompleted _currentServiceRecord;
        private UpdatePart? _selectedUpdatePart;
        private readonly Dictionary<int, ProjectUpdateSO> _updateById = new();
        private readonly Dictionary<(ProjectSize size, Role role), List<ProjectUpdateSO>> _updatesBySizeRole = new();
        private readonly Dictionary<UpdatePart, ProjectUpdateSO> _currentUpdateOptions = new();

        public bool IsVisible => _view.IsVisible;

        private void Start()
        {
            _selectedScale = UnselectedScale;
            InitUpdateList();
            BindTabs();
            BindNewProject();
            BindInProgress();
            BindUpdateManagement();
            BindCompleted();
        }

        public void Show()
        {
            _view.Show();
            _view.ShowTab(ProjectTab.NewProject);
            RefreshNewProject();
        }

        public void Hide()
        {
            ClearSelectedEmployees();
            _view.Hide();
        }

        private void InitUpdateList()
        {
            _updateById.Clear();
            _updatesBySizeRole.Clear();

            foreach (ProjectUpdateSO so in _allUpdateSos)
            {
                if (so == null) continue;

                _updateById[so.id] = so;

                var key = (so.size, so.role);
                if (!_updatesBySizeRole.TryGetValue(key, out var list))
                {
                    list = new List<ProjectUpdateSO>();
                    _updatesBySizeRole[key] = list;
                }

                list.Add(so);
            }
        }

        private void BindTabs()
        {
            _view.OnNewProjectTabClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowTab(ProjectTab.NewProject);
                    RefreshNewProject();
                })
                .AddTo(this);

            _view.OnInProgressTabClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowTab(ProjectTab.InProgress);
                    RefreshInProgressList();
                })
                .AddTo(this);

            _view.OnCompletedTabClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowTab(ProjectTab.Completed);
                    RefreshCompletedList();
                })
                .AddTo(this);
        }

        private void BindNewProject()
        {
            _view.OnScaleCardSmallClicked
                .Subscribe(_ => OnScaleSelected(ProjectSize.Small))
                .AddTo(this);

            _view.OnScaleCardMediumClicked
                .Subscribe(_ => OnScaleSelected(ProjectSize.Medium))
                .AddTo(this);

            _view.OnScaleCardLargeClicked
                .Subscribe(_ => OnScaleSelected(ProjectSize.Large))
                .AddTo(this);

            // 프로젝트 이름 입력 유효성 검사 — 한글 기준 1자 이상일 때 다음 버튼 활성화
            _view.OnProjectNameChanged
                .Subscribe(name =>
                    _view.SetProjectSetupNextInteractable(
                        !string.IsNullOrWhiteSpace(name) && _selectedScale != UnselectedScale
                    )
                )
                .AddTo(this);

            _view.OnProjectSetupNextClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowStaffAssign();
                    RefreshStaffAssign();
                })
                .AddTo(this);

            _view.OnResetClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    ClearSelectedEmployees();
                    RefreshStaffAssign();
                })
                .AddTo(this);

            _view.OnStaffSortChanged
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); RefreshStaffAssign(); })
                .AddTo(this);

            _view.OnStaffAssignBackClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    ClearSelectedEmployees();
                    _view.ShowProjectSetup();
                })
                .AddTo(this);

            _view.OnStaffAssignConfirmClicked
                .Subscribe(_ => OnStaffAssignConfirmClicked())
                .AddTo(this);
        }

        private void BindInProgress()
        {
            _view.OnInProgressSortChanged
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); RefreshInProgressList(); })
                .AddTo(this);

            _view.OnProjectDetailBackClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _view.ShowInProgressList(); })
                .AddTo(this);

            _view.OnServiceStopClicked
                .Subscribe(_ => OnServiceStopClicked())
                .AddTo(this);

            _view.OnUpdateClicked
                .Subscribe(_ => OnUpdateClicked())
                .AddTo(this);

            _view.OnStaffDetailCloseClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _view.HideStaffDetailPopup(); })
                .AddTo(this);
        }

        private void BindUpdateManagement()
        {
            _view.OnUpdateItemPlanClicked
                .Subscribe(_ => OnUpdateItemSelected(UpdatePart.Plan))
                .AddTo(this);

            _view.OnUpdateItemArtClicked
                .Subscribe(_ => OnUpdateItemSelected(UpdatePart.Art))
                .AddTo(this);

            _view.OnUpdateItemDevClicked
                .Subscribe(_ => OnUpdateItemSelected(UpdatePart.Dev))
                .AddTo(this);

            _view.OnUpdateBackClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _selectedUpdatePart = null;
                    _view.HideUpdateManagement();
                })
                .AddTo(this);

            _view.OnUpdateConfirmClicked
                .Subscribe(_ => OnUpdateConfirmClicked())
                .AddTo(this);
        }

        private void BindCompleted()
        {
            _view.OnCompletedSortChanged
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); RefreshCompletedList(); })
                .AddTo(this);

            _view.OnCompletedDetailBackClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _view.ShowCompletedList(); })
                .AddTo(this);
        }

        private void RefreshNewProject()
        {
            RefreshScaleCardInfo();

            bool hasActiveProject = Company.Instance.activeProjectCount.Value > 0;
            _view.SetActiveProjectWarningVisible(hasActiveProject);

            if (!hasActiveProject)
            {
                _selectedScale = UnselectedScale;
                _view.ClearScaleCardSelectImg();
                _view.SetProjectSetupNextInteractable(false);

                int level = Company.Instance.level;
                _view.SetScaleMediumLocked(level < 2);
                _view.SetScaleLargeLocked(level < 3);
            }
        }

        private void RefreshStaffAssign()
        {
            foreach (Transform child in _view.StaffGridContent)
                Destroy(child.gameObject);

            var raw = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                .Where(e => !IsEmployeeInProject(e));

            // 0:직군순 1:이름순 2:능력치순 3:배치순
            var employees = _view.StaffSortIndex switch
            {
                1 => raw.OrderBy(e => e.so.Name),
                2 => raw.OrderByDescending(e => e.MutableData.ability),
                3 => raw.OrderByDescending(e => IsSelected(e)),
                _ => raw.OrderBy(e => GetRoleOrder(e.so.role)).ThenBy(e => e.so.Name),
            };

            var employeeList = employees.ToList();

            // 최소 인원 표시 — 선택된 규모 기준
            int max = GetMaxEmployeePerPart(_selectedScale);
            _view.SetMinStaffLabel(
                CountAssigned(Role.PLANNER), max,
                CountAssigned(Role.ARTIST), max,
                CountAssigned(Role.PROGRAMMER), max
            );

            // 확정 조건: 기획/아트/개발 각 1명 이상
            bool canConfirm = CountAssigned(Role.PLANNER) >= 1
                           && CountAssigned(Role.ARTIST) >= 1
                           && CountAssigned(Role.PROGRAMMER) >= 1;
            _view.SetStaffAssignConfirmInteractable(canConfirm);

            foreach (var employee in employeeList)
            {
                var prefab = GetStaffCardPrefab(employee.so.role);
                if (prefab == null) continue;

                var cardGO = Instantiate(prefab, _view.StaffGridContent);
                var cardView = cardGO.GetComponent<StaffCardView>();
                cardGO.GetComponent<IBindable<Employee>>().Bind(employee);

                bool isAssigned = IsSelected(employee);
                bool isInEducation = false; // [TODO: 교육 시스템 연결 후 처리]
                var assignState = isAssigned ? StaffAssignState.Assigned
                                : isInEducation ? StaffAssignState.InEducation
                                : StaffAssignState.Default;
                cardView.SetAssignState(assignState);

                var captured = employee;

                cardGO.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    cardView.SetSelected(!cardView.IsOverlayVisible);
                });

                cardView.OnInfoClicked
                    .Subscribe(_ => OnStaffInfoClicked(captured))
                    .AddTo(this);

                cardView.OnAssignClicked
                    .Subscribe(_ => OnStaffCardClicked(captured))
                    .AddTo(this);
            }
        }

        private void RefreshInProgressList()
        {
            foreach (Transform child in _view.InProgressListContent)
                Destroy(child.gameObject);

            var rows = GetInProgressRows();
            _view.SetInProgressEmptyVisible(rows.Count == 0);

            // 0:진행순 1:이름순 2:매출순
            var sorted = _view.InProgressSortIndex switch
            {
                1 => rows.OrderBy(row => row.Name).ToList(),
                2 => rows.OrderByDescending(row => row.Revenue).ToList(),
                _ => rows,
            };

            for (int i = 0; i < sorted.Count; i++)
            {
                var row = sorted[i];
                var item = Instantiate(_projectListItemPrefab, _view.InProgressListContent);
                var itemView = item.GetComponent<ProjectListItemView>();

                itemView.SetNumber(i + 1);

                if (row.Project != null)
                {
                    itemView.Bind(row.Project);
                    var captured = row.Project;
                    item.GetComponentInChildren<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                    {
                        AudioManager.Instance?.PlaySFXClick();
                        ShowProjectDetail(captured);
                    });
                    continue;
                }

                itemView.Bind(row.Record);
                var capturedRecord = row.Record;
                item.GetComponentInChildren<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    ShowServiceProjectDetail(capturedRecord);
                });
            }
        }

        private void RefreshCompletedList()
        {
            foreach (Transform child in _view.CompletedListContent)
                Destroy(child.gameObject);

            var completed = Company.Instance.completedProjects?
                .Where(record => record.isServiceOver)
                .ToList();
            _view.SetCompletedEmptyVisible(completed == null || completed.Count == 0);

            if (completed == null) return;

            // 0:진행순 1:이름순 2:매출순
            var sorted = _view.CompletedSortIndex switch
            {
                1 => completed.OrderBy(r => r.projectName).ToList(),
                2 => completed.OrderByDescending(GetCurrentRevenue).ToList(),
                _ => completed.ToList(),
            };

            for (int i = 0; i < sorted.Count; i++)
            {
                var record = sorted[i];
                var item = Instantiate(_projectListItemPrefab, _view.CompletedListContent);
                var itemView = item.GetComponent<ProjectListItemView>();

                itemView.Bind(record);
                itemView.SetNumber(i + 1);

                var captured = record;
                item.GetComponentInChildren<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    ShowCompletedDetail(captured);
                });
            }
        }

        private void ShowProjectDetail(Project project)
        {
            _currentDetailProject = project;
            _currentServiceRecord = null;
            _view.ShowProjectDetail();
            _view.SetProjectDetailInfo(
                project.userNamed.Value,
                ScaleToString(project.Scale),
                project.genre,
                project.artStyle,
                project.engine
            );
            _view.SetProgressBar(project.ProgressDayBar);

            _view.SetOperationGroupVisible(false);
            _view.SetServiceStopInteractable(false);
            _view.SetUpdateButtonInteractable(false);
        }

        private void ShowServiceProjectDetail(ProjectCompleted record)
        {
            _currentDetailProject = null;
            _currentServiceRecord = record;
            _view.ShowProjectDetail();
            _view.SetProjectDetailInfo(
                record.projectName,
                ScaleToString(record.scale),
                record.genre,
                record.artStyle,
                record.engine
            );
            _view.SetProgressBar(100f);
            _view.SetOperationGroupVisible(true);
            SetServiceOperationValues(record);
            _view.SetServiceStopInteractable(!record.isServiceOver);
            _view.SetUpdateButtonInteractable(!record.isServiceOver);
        }

        private void ShowCompletedDetail(ProjectCompleted record)
        {
            _view.ShowCompletedDetail();

            _view.SetCompletedProjectDetailInfo(
                record.projectName,
                ScaleToString(record.scale),
                genre: record.genre,
                art: record.artStyle,
                engine: record.engine
            );

            _view.SetCompletedProgressBar(1f);

            _view.SetCompletedOperationGroupVisible(true);
            SetCompletedOperationValues(record);
        }

        private void OnUpdateClicked()
        {
            if (_currentServiceRecord == null || _currentServiceRecord.isServiceOver) return;

            AudioManager.Instance?.PlaySFXClick();
            _selectedUpdatePart = null;
            _currentUpdateOptions.Clear();
            _view.SetUpdateConfirmInteractable(false);
            _view.SetUpdateItemSelectImg(null);

            BindUpdateItem(UpdatePart.Plan);
            BindUpdateItem(UpdatePart.Art);
            BindUpdateItem(UpdatePart.Dev);

            _view.ShowUpdateManagement(_currentServiceRecord.projectName);
        }

        private void OnUpdateItemSelected(UpdatePart part)
        {
            if (_currentServiceRecord == null) return;

            ProjectUpdateSO update = GetCurrentUpdateOption(part);
            if (update == null || IsUpdateCompleted(_currentServiceRecord, part)) return;

            AudioManager.Instance?.PlaySFXClick();
            _selectedUpdatePart = part;
            _view.SetUpdateItemSelectImg(part);
            _view.SetUpdateConfirmInteractable(true);
        }

        private void OnUpdateConfirmClicked()
        {
            if (_selectedUpdatePart == null || _currentServiceRecord == null) return;

            UpdatePart part = _selectedUpdatePart.Value;
            ProjectUpdateSO update = GetCurrentUpdateOption(part);
            if (update == null || IsUpdateCompleted(_currentServiceRecord, part)) return;

            if (Company.Instance.gold.Value < update.cost)
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup("보유 자금이 부족하여 실행할 수 없습니다.");
                return;
            }

            AudioManager.Instance?.PlaySFXAlert();
            _alertView.ShowConfirmPopup(
                $"업데이트비용 {FormatPolicy.FormatGold(update.cost)} 지불해야합니다. 진행 하시겠습니까?",
                onConfirm: () =>
                {
                    AudioManager.Instance?.PlaySFXPositive();
                    ApplyProjectUpdate(_currentServiceRecord, part, update);

                    _selectedUpdatePart = null;
                    _view.SetUpdateItemSelectImg(null);
                    _view.SetUpdateConfirmInteractable(false);
                    BindUpdateItem(part);
                    SetServiceOperationValues(_currentServiceRecord);
                    _hudPresenter?.RefreshHUD();
                    RefreshInProgressList();
                }
            );
        }

        private void BindUpdateItem(UpdatePart part)
        {
            ProjectUpdateSO update = GetOrAssignUpdate(_currentServiceRecord, part);
            bool completed = IsUpdateCompleted(_currentServiceRecord, part);
            bool hasData = update != null;

            string title = hasData ? (update.Name?.Trim() ?? string.Empty) : "업데이트 없음";
            string desc = hasData ? (update.desc?.Trim() ?? string.Empty) : "해당 규모의 업데이트 데이터가 없습니다.";
            string cost = hasData ? FormatPolicy.FormatGold(update.cost) : "-";

            if (hasData)
                _currentUpdateOptions[part] = update;
            else
                _currentUpdateOptions.Remove(part);

            _view.SetUpdateItemInfo(part, title, desc, cost, hasData && !completed);
            _view.SetUpdateItemCompletedOverlay(part, completed);
        }

        private ProjectUpdateSO GetCurrentUpdateOption(UpdatePart part)
        {
            return _currentUpdateOptions.TryGetValue(part, out ProjectUpdateSO update)
                ? update
                : GetOrAssignUpdate(_currentServiceRecord, part);
        }

        private ProjectUpdateSO GetOrAssignUpdate(ProjectCompleted record, UpdatePart part)
        {
            if (record == null) return null;

            int updateId = GetUpdateId(record, part);
            if (updateId > 0 && _updateById.TryGetValue(updateId, out ProjectUpdateSO savedUpdate))
                return savedUpdate;

            Role role = GetUpdateRole(part);
            var key = (record.scale, role);
            if (!_updatesBySizeRole.TryGetValue(key, out List<ProjectUpdateSO> candidates) || candidates.Count == 0)
                return null;

            ProjectUpdateSO picked = candidates[Random.Range(0, candidates.Count)];
            SetUpdateId(record, part, picked.id);
            return picked;
        }

        private void ApplyProjectUpdate(ProjectCompleted record, UpdatePart part, ProjectUpdateSO update)
        {
            Company.Instance.gold.Value -= update.cost;
            Company.Instance.curManagementStatus.devCost += update.cost;
            Company.Instance.cumulativeManagementStatus.devCost += update.cost;
            Company.Instance.curManagementStatus.Recalculate();
            Company.Instance.cumulativeManagementStatus.Recalculate();

            record.RetentionFactor += 0.1f;
            SetUpdateCompleted(record, part, true);
            record.isUpdatePending = HasCompletedUpdate(record);
        }

        private static Role GetUpdateRole(UpdatePart part)
        {
            switch (part)
            {
                case UpdatePart.Plan:
                    return Role.PLANNER;
                case UpdatePart.Art:
                    return Role.ARTIST;
                default:
                    return Role.PROGRAMMER;
            }
        }

        private static int GetUpdateId(ProjectCompleted record, UpdatePart part)
        {
            switch (part)
            {
                case UpdatePart.Plan:
                    return record.planUpdateId;
                case UpdatePart.Art:
                    return record.artUpdateId;
                default:
                    return record.devUpdateId;
            }
        }

        private static void SetUpdateId(ProjectCompleted record, UpdatePart part, int updateId)
        {
            switch (part)
            {
                case UpdatePart.Plan:
                    record.planUpdateId = updateId;
                    break;
                case UpdatePart.Art:
                    record.artUpdateId = updateId;
                    break;
                case UpdatePart.Dev:
                    record.devUpdateId = updateId;
                    break;
            }
        }

        private static bool IsUpdateCompleted(ProjectCompleted record, UpdatePart part)
        {
            switch (part)
            {
                case UpdatePart.Plan:
                    return record.planUpdateCompleted;
                case UpdatePart.Art:
                    return record.artUpdateCompleted;
                default:
                    return record.devUpdateCompleted;
            }
        }

        private static void SetUpdateCompleted(ProjectCompleted record, UpdatePart part, bool completed)
        {
            switch (part)
            {
                case UpdatePart.Plan:
                    record.planUpdateCompleted = completed;
                    break;
                case UpdatePart.Art:
                    record.artUpdateCompleted = completed;
                    break;
                case UpdatePart.Dev:
                    record.devUpdateCompleted = completed;
                    break;
            }
        }

        private static bool HasCompletedUpdate(ProjectCompleted record)
        {
            return record.planUpdateCompleted || record.artUpdateCompleted || record.devUpdateCompleted;
        }

        private void OnScaleSelected(ProjectSize scale)
        {
            ProjectSO projectSo = Company.Instance.GetProjectTemplate(scale);
            int cost = projectSo.requiredCost;
            bool canAfford = Company.Instance.gold.Value >= cost;

            if (!canAfford)
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            _selectedScale = scale;
            AudioManager.Instance?.PlaySFXClick();
            _view.SetScaleCardSelectImg(scale);
            _view.SetProjectSetupNextInteractable(!string.IsNullOrWhiteSpace(GetCurrentProjectName()));
        }

        private void OnStaffCardClicked(Employee employee)
        {
            if (_selectedScale == UnselectedScale) return;

            int max = GetMaxEmployeePerPart(_selectedScale);
            bool toggled = Company.Instance.ToggleSelectedProjectEmployee(employee, max);
            if (!toggled)
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup($"해당 직군은 최대 {max}명까지 배치 가능합니다.");
                return;
            }

            AudioManager.Instance?.PlaySFXClick();

            RefreshStaffAssign();
        }

        private void OnStaffAssignConfirmClicked()
        {
            ProjectSO projectSo = Company.Instance.GetProjectTemplate(_selectedScale);
            int cost = projectSo.requiredCost;
            if (Company.Instance.gold.Value < cost)
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            AudioManager.Instance?.PlaySFXAlert();
            _alertView.ShowConfirmPopup(
                $"개발비 {FormatPolicy.FormatGold(cost)}를 지불하고 프로젝트를 시작하시겠습니까?",
                onConfirm: () =>
                {
                    var project = Company.Instance.CreateProject(_selectedScale, GetCurrentProjectName());
                    if (project == null)
                    {
                        AudioManager.Instance?.PlaySFXAlert();
                        _alertView.ShowAlertPopup("프로젝트 생성에 실패했습니다.");
                        return;
                    }

                    AudioManager.Instance?.PlaySFXPositive();
                    Company.Instance.StartNewProject(project);
                    _hudPresenter.RefreshHUD();
                    ClearSelectedEmployees();

                    _view.ShowTab(ProjectTab.InProgress);
                    RefreshInProgressList();
                }
            );
        }

        private void OnStaffInfoClicked(Employee employee)
        {
            AudioManager.Instance?.PlaySFXClick();
            foreach (Transform child in _view.StaffDetailContent)
                Destroy(child.gameObject);

            var go = Instantiate(_staffDetailPrefab, _view.StaffDetailContent);
            go.GetComponent<EmployeeDetailView>().Bind(employee);

            _view.ShowStaffDetailPopup();
        }

        private void OnServiceStopClicked()
        {
            if (_currentServiceRecord == null) return;

            AudioManager.Instance?.PlaySFXAlert();
            _alertView.ShowConfirmPopup("게임 서비스를 종료하겠습니까?", onConfirm: () =>
            {
                AudioManager.Instance?.PlaySFXNegative();
                _currentServiceRecord.isServiceOver = true;
                _currentDetailProject = null;
                _currentServiceRecord = null;
                _view.ShowInProgressList();
                RefreshInProgressList();
            });
        }

        private List<ProjectListRow> GetInProgressRows()
        {
            var result = new List<ProjectListRow>();

            if (Company.Instance.activeProjectCount.Value > 0)
                result.Add(new ProjectListRow(Company.Instance.curProject));

            foreach (ProjectCompleted record in Company.Instance.completedProjects)
            {
                if (!record.isServiceOver)
                    result.Add(new ProjectListRow(record));
            }

            return result;
        }

        private bool IsEmployeeInProject(Employee employee)
        {
            if (Company.Instance.activeProjectCount.Value <= 0) return false;
            return Company.Instance.curProject.GetAllEmployees().Contains(employee);
        }

        private void SetServiceOperationValues(ProjectCompleted record)
        {
            int settledWeekCount = record.weeklyGoldHistory?.Count ?? 0;
            if (settledWeekCount == 0)
            {
                _view.SetOperationPendingValues();
                return;
            }

            int revenue = GetCurrentRevenue(record);
            int revenueDelta = revenue - record.prevWeekGold;
            int userDelta = record.users - record.prevWeekUsers;
            int profit = revenue - record.dailyCost;
            int previousProfit = record.prevWeekGold - record.dailyCost;
            int profitDelta = profit - previousProfit;
            bool hasUserDelta = record.prevWeekUsers > 0;
            bool hasRevenueDelta = record.prevWeekGold > 0 && settledWeekCount > 1;

            _view.SetUserCountValue(FormatValueWithDelta(record.users, userDelta, "명", hasUserDelta), userDelta >= 0, hasUserDelta);
            _view.SetSalesValue(FormatGoldWithDelta(revenue, revenueDelta, hasRevenueDelta), revenueDelta >= 0, hasRevenueDelta);
            _view.SetMaintenanceValue(FormatPolicy.FormatGold(record.dailyCost));
            _view.SetProfitValue(FormatGoldWithDelta(profit, profitDelta, hasRevenueDelta), profitDelta >= 0, hasRevenueDelta);
            _view.SetRevenueGraphValues(GetRevenueGraphValues(record));
        }

        private void SetCompletedOperationValues(ProjectCompleted record)
        {
            int settledWeekCount = record.weeklyGoldHistory?.Count ?? 0;
            if (settledWeekCount == 0)
            {
                _view.SetCompletedOperationPendingValues();
                return;
            }

            int revenue = GetCurrentRevenue(record);
            int profit = revenue - record.dailyCost;

            _view.SetCompletedUserCountValue($"{record.users:N0}명");
            _view.SetCompletedSalesValue(FormatPolicy.FormatGold(revenue));
            _view.SetCompletedMaintenanceValue(FormatPolicy.FormatGold(record.dailyCost));
            _view.SetCompletedProfitValue(FormatPolicy.FormatGold(profit));
            _view.SetCompletedRevenueGraphValues(GetRevenueGraphValues(record));
        }

        private static int GetCurrentRevenue(ProjectCompleted record)
        {
            if (record.weeklyGoldHistory == null || record.weeklyGoldHistory.Count == 0)
                return 0;

            int latestRevenue = 0;
            foreach (int weekGold in record.weeklyGoldHistory)
            {
                latestRevenue = weekGold;
            }

            return latestRevenue;
        }

        private static List<int> GetRevenueGraphValues(ProjectCompleted record)
        {
            var values = new List<int>();

            if (record.weeklyGoldHistory != null)
                values.AddRange(record.weeklyGoldHistory);

            while (values.Count > 4)
                values.RemoveAt(0);

            return values;
        }

        private static string FormatValueWithDelta(int value, int delta, string suffix, bool showDelta)
        {
            if (!showDelta)
                return $"{value:N0}{suffix}";

            string arrow = delta >= 0 ? "▲" : "▼";
            return $"{value:N0}{suffix} ({Mathf.Abs(delta):N0}{arrow})";
        }

        private static string FormatGoldWithDelta(int value, int delta, bool showDelta)
        {
            if (!showDelta)
                return FormatPolicy.FormatGold(value);

            string arrow = delta >= 0 ? "▲" : "▼";
            return $"{FormatPolicy.FormatGold(value)} ({FormatPolicy.FormatGold(Mathf.Abs(delta))}{arrow})";
        }

        private int CountAssigned(Role role)
            => Company.Instance.selectedProjectEmployees.Count(e => e.so.role == role);

        private bool IsSelected(Employee employee)
            => Company.Instance.selectedProjectEmployees.Contains(employee);

        private void ClearSelectedEmployees()
            => Company.Instance.ClearSelectedProjectEmployees();

        private void RefreshScaleCardInfo()
        {
            SetScaleCardInfo(ProjectSize.Small);
            SetScaleCardInfo(ProjectSize.Medium);
            SetScaleCardInfo(ProjectSize.Large);
        }

        private void SetScaleCardInfo(ProjectSize scale)
        {
            ProjectSO projectSo = Company.Instance.GetProjectTemplate(scale);
            int weeks = Mathf.CeilToInt(Mathf.Max(0, projectSo.durationDays) / 5f);
            _view.SetScaleCardInfo(
                scale,
                $"개발기간 : {weeks}주",
                $"비       용 : {FormatPolicy.FormatGold(projectSo.requiredCost)}"
            );
        }

        private static int GetMaxEmployeePerPart(ProjectSize scale) => scale switch
        {
            ProjectSize.Medium => 2,
            ProjectSize.Large => 3,
            _ => 1,
        };

        private static string ScaleToString(ProjectSize scale) => scale switch
        {
            ProjectSize.Medium => "중규모",
            ProjectSize.Large => "대규모",
            _ => "소규모",
        };

        private string GetCurrentProjectName() => _view.ProjectNameInput;

        private static int GetRoleOrder(Role role) => role switch
        {
            Role.PLANNER => 0,
            Role.ARTIST => 1,
            Role.PROGRAMMER => 2,
            _ => 3,
        };

        private GameObject GetStaffCardPrefab(Role role)
        {
            foreach (var entry in _staffCardPrefabs)
                if (entry.role == role) return entry.prefab;
            return null;
        }

        private sealed class ProjectListRow
        {
            public readonly Project Project;
            public readonly ProjectCompleted Record;
            public readonly string Name;
            public readonly int Revenue;

            public ProjectListRow(Project project)
            {
                Project = project;
                Name = project.userNamed.Value;
                Revenue = 0;
            }

            public ProjectListRow(ProjectCompleted record)
            {
                Record = record;
                Name = record.projectName;
                Revenue = GetCurrentRevenue(record);
            }
        }
    }

    [System.Serializable]
    public sealed class StaffCardPrefabEntry
    {
        public Role role;
        public GameObject prefab;
    }
}
