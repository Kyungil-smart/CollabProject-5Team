using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.ProjectPopup Presenter.
    /// 슬롯 선택 → 프로젝트 설정 → 인원 배치 흐름 및 진행 프로젝트 표시 처리.
    /// ProjectSlotItemView, StaffCardView, ProjectListItemView 바인딩은 IBindable 연결 후 활성화.
    /// </summary>
    public sealed class ProjectPresenter : MonoBehaviour
    {
        [SerializeField] private ProjectView  _view;
        [SerializeField] private AlertView    _alertView;
        [SerializeField] private HUDPresenter _hudPresenter;

        [Header("프리팹")]
        [SerializeField] private GameObject _projectSlotItemPrefab;
        [SerializeField] private GameObject _staffCardPrefab;
        [SerializeField] private GameObject _projectListItemPrefab;
        [SerializeField] private GameObject _staffDetailPrefab;

        private ProjectSO   _selectedSlotSO;
        private ProjectSize _selectedScale;
        private int         _selectedSlotIndex = -1;

        // 배치 확정된 직원 목록 (인원 배치 패널)
        private readonly List<Employee> _assignedEmployees = new();

        private void Start()
        {
            BindTabs();
            BindNewProject();
            BindInProgress();
        }

        public void Show()
        {
            _view.Show();
            _view.ShowTab(ProjectTab.NewProject);
            RefreshSlotSelect();
        }

        public void Hide() => _view.Hide();

        private void BindTabs()
        {
            _view.OnNewProjectTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(ProjectTab.NewProject);
                    RefreshSlotSelect();
                })
                .AddTo(this);

            _view.OnInProgressTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(ProjectTab.InProgress);
                    RefreshInProgressList();
                })
                .AddTo(this);
        }

        private void BindNewProject()
        {
            _view.OnScaleCardSmallClicked
                .Subscribe(_ => OnScaleSelected(ProjectSize.small))
                .AddTo(this);

            _view.OnScaleCardMediumClicked
                .Subscribe(_ => OnScaleSelected(ProjectSize.medium))
                .AddTo(this);

            _view.OnScaleCardLargeClicked
                .Subscribe(_ => OnScaleSelected(ProjectSize.large))
                .AddTo(this);

            // 프로젝트 이름 입력 유효성 검사 — 한글 기준 1자 이상일 때 다음 버튼 활성화
            _view.OnProjectNameChanged
                .Subscribe(name =>
                    _view.SetProjectSetupNextInteractable(
                        !string.IsNullOrWhiteSpace(name) && _selectedSlotSO != null
                    )
                )
                .AddTo(this);

            _view.OnProjectSetupBackClicked
                .Subscribe(_ =>
                {
                    _selectedSlotSO = null;
                    _view.ShowSlotSelect();
                })
                .AddTo(this);

            _view.OnProjectSetupNextClicked
                .Subscribe(_ =>
                {
                    _view.ShowStaffAssign();
                    RefreshStaffAssign();
                })
                .AddTo(this);

            _view.OnResetClicked
                .Subscribe(_ =>
                {
                    _assignedEmployees.Clear();
                    RefreshStaffAssign();
                })
                .AddTo(this);

            _view.OnStaffSortChanged
                .Subscribe(_ => RefreshStaffAssign())
                .AddTo(this);

            _view.OnSynergyClicked
                .Subscribe(_ => OnSynergyClicked())
                .AddTo(this);

            _view.OnStaffAssignBackClicked
                .Subscribe(_ => _view.ShowProjectSetup())
                .AddTo(this);

            _view.OnStaffAssignConfirmClicked
                .Subscribe(_ => OnStaffAssignConfirmClicked())
                .AddTo(this);
        }

        private void BindInProgress()
        {
            _view.OnInProgressSortChanged
                .Subscribe(_ => RefreshInProgressList())
                .AddTo(this);

            _view.OnProjectDetailBackClicked
                .Subscribe(_ => _view.ShowInProgressList())
                .AddTo(this);

            _view.OnServiceStopClicked
                .Subscribe(_ => OnServiceStopClicked())
                .AddTo(this);

            _view.OnStaffDetailCloseClicked
                .Subscribe(_ => _view.HideStaffDetailPopup())
                .AddTo(this);
        }

        private void RefreshSlotSelect()
        {
            foreach (Transform child in _view.SlotScrollContent)
                Destroy(child.gameObject);

            int maxSlots = Company.Instance.ProjectSlots;

            for (int i = 0; i < maxSlots; i++)
            {
                var item = Instantiate(_projectSlotItemPrefab, _view.SlotScrollContent);
                bool isOccupied = i < Company.Instance.projects.Count;

                // [TODO: IBindable<ProjectSlotData> 연결 후 활성화]
                // item.GetComponent<IBindable<ProjectSlotData>>().Bind(data);

                int captured = i;
                item.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    if (isOccupied) return;
                    _selectedSlotIndex = captured;
                    _view.ShowProjectSetup();
                    _view.SetProjectSetupNextInteractable(false);

                    // 회사 레벨 기준 규모 잠금 해제
                    int level = Company.Instance.level;
                    _view.SetScaleMediumLocked(level < 2);
                    _view.SetScaleLargeLocked(level < 4);
                });
            }
        }

        private void RefreshStaffAssign()
        {
            foreach (Transform child in _view.StaffGridContent)
                Destroy(child.gameObject);

            var employees = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                .Where(e => !IsEmployeeInProject(e))
                .OrderBy(e => e.so.Name)
                .ToList();

            // 최소 인원 표시 — 선택된 규모 기준
            int max = GetMaxEmployeePerPart(_selectedScale);
            _view.SetMinStaffLabel(
                CountAssigned(Role.PLANNER), max,
                CountAssigned(Role.ARTIST), max,
                CountAssigned(Role.PROGRAMMER), max
            );

            // 확정 조건: 기획/아트/개발 각 1명 이상
            bool canConfirm = CountAssigned(Role.PLANNER) >= 1
                           && CountAssigned(Role.ARTIST)  >= 1
                           && CountAssigned(Role.PROGRAMMER) >= 1;
            _view.SetStaffAssignConfirmInteractable(canConfirm);

            foreach (var employee in employees)
            {
                var card = Instantiate(_staffCardPrefab, _view.StaffGridContent);
                // [TODO: IBindable<Employee> 연결 후 활성화]

                var captured = employee;
                card.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                    OnStaffCardClicked(captured)
                );
            }
        }

        private void RefreshInProgressList()
        {
            foreach (Transform child in _view.InProgressListContent)
                Destroy(child.gameObject);

            var projects = Company.Instance.projects;
            _view.SetInProgressEmptyVisible(projects.Count == 0);

            foreach (var project in projects)
            {
                var item = Instantiate(_projectListItemPrefab, _view.InProgressListContent);
                // [TODO: IBindable<Project> 연결 후 활성화]

                var captured = project;
                item.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                    ShowProjectDetail(captured)
                );
            }
        }

        private void ShowProjectDetail(Project project)
        {
            _view.ShowProjectDetail();
            _view.SetProjectDetailInfo(
                project.userNamed.Value,
                ScaleToString(project.Scale),
                project.genre,
                project.artStyle,
                project.engine
            );
            _view.SetProgressBar(project.ProgressDayBar / 100f);

            bool isCompleted = project.isFinished.Value;
            _view.SetStatusGroupVisible(isCompleted);
            _view.SetServiceStopInteractable(isCompleted && !IsServiceOver(project));
        }

        private void OnScaleSelected(ProjectSize scale)
        {
            _selectedScale = scale;

            int cost = GetRequiredCost(scale);
            bool canAfford = Company.Instance.gold >= cost;

            if (!canAfford)
            {
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            _view.SetProjectSetupNextInteractable(!string.IsNullOrWhiteSpace(GetCurrentProjectName()));
        }

        private void OnStaffCardClicked(Employee employee)
        {
            bool isAssigned = _assignedEmployees.Contains(employee);
            int max = GetMaxEmployeePerPart(_selectedScale);

            if (!isAssigned)
            {
                int roleCount = CountAssigned(employee.so.role);
                if (roleCount >= max)
                {
                    _alertView.ShowAlertPopup($"해당 직군은 최대 {max}명까지 배치 가능합니다.");
                    return;
                }
                _assignedEmployees.Add(employee);
            }
            else
            {
                _assignedEmployees.Remove(employee);
            }

            RefreshStaffAssign();
        }

        private void OnStaffAssignConfirmClicked()
        {
            int cost = GetRequiredCost(_selectedScale);
            if (Company.Instance.gold < cost)
            {
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            _alertView.ShowConfirmPopup(
                $"개발비 {cost:N0}G를 지불하고 프로젝트를 시작하시겠습니까?",
                onConfirm: () =>
                {
                    // [TODO: ProjectSO 기반 Project 생성 연결]
                    Company.Instance.gold -= cost;
                    _hudPresenter.RefreshHUD();
                    _assignedEmployees.Clear();

                    _view.ShowTab(ProjectTab.InProgress);
                    RefreshInProgressList();
                }
            );
        }

        private void OnSynergyClicked()
        {
            _alertView.ShowSynergyPopup();
            // [TODO: SynergyItemView 동적 생성 — 배치 직원 조합 기반]
        }

        private void OnServiceStopClicked()
        {
            _alertView.ShowConfirmPopup("서비스를 종료하시겠습니까?", onConfirm: () =>
            {
                // [TODO: ProjectCompleted.isServiceOver = true 처리]
                RefreshInProgressList();
            });
        }

        private bool IsEmployeeInProject(Employee employee)
        {
            foreach (var project in Company.Instance.projects)
                if (project.GetAllEmployees().Contains(employee)) return true;
            return false;
        }

        private bool IsServiceOver(Project project)
        {
            var completed = Company.Instance.completedProjects
                .FirstOrDefault(p => p.projectName == project.userNamed.Value);
            return completed?.isServiceOver ?? false;
        }

        private int CountAssigned(Role role)
            => _assignedEmployees.Count(e => e.so.role == role);

        private static int GetRequiredCost(ProjectSize scale) => scale switch
        {
            ProjectSize.medium => 10000,
            ProjectSize.large  => 100000,
            _                  => 1000,
        };

        private static int GetMaxEmployeePerPart(ProjectSize scale) => scale switch
        {
            ProjectSize.medium => 2,
            ProjectSize.large  => 3,
            _                  => 1,
        };

        private static string ScaleToString(ProjectSize scale) => scale switch
        {
            ProjectSize.medium => "중규모",
            ProjectSize.large  => "대규모",
            _                  => "소규모",
        };
        
        private string GetCurrentProjectName() => _view.ProjectNameInput;
    }
}