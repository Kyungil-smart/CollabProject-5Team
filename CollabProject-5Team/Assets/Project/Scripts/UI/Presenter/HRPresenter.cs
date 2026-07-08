using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using GameDevTycoon.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.HRPopup Presenter.
    /// 직원 목록 표시, 고용/해고/교육 흐름 처리.
    /// EmployeeCardView, EmployeeDetailView, ApplicantCardView, ApplicantDetailView
    /// 프리팹 바인딩은 IBindable 연결 후 활성화.
    /// </summary>
    public sealed class HRPresenter : MonoBehaviour, IBottomNightUI
    {
        private const int RecruitCostPerEmployee = 10000;

        [SerializeField] private HRView _view;
        [SerializeField] private AlertView _alertView;
        [SerializeField] private HUDPresenter _hudPresenter;

        [Header("프리팹")]
        [SerializeField] private GameObject _employeeCardPrefab;
        [SerializeField] private GameObject _employeeDetailPrefab;
        [SerializeField] private GameObject _applicantCardPrefab;
        [SerializeField] private GameObject _applicantDetailPrefab;

        private Employee _selectedEmployee;
        private Employee _selectedApplicant;
        private int _selectedCourseIndex = -1;

        private CompositeDisposable _sliderDisposables = new();

        public bool IsVisible => _view.IsVisible;

        private void Start()
        {
            BindTabs();
            BindEmployeeManage();
            BindHire();

            DateTimeManager.OnWeekStarted += ResetOnMondayUI;
        }

        private void OnDestroy()
        {
            DateTimeManager.OnWeekStarted -= ResetOnMondayUI;
        }

        public void Show()
        {
            _view.Show();
            RefreshEmployeeManageList();
            _view.ShowTab(HRTab.EmployeeManage);
        }

        public void Hide() => _view.Hide();

        private void BindTabs()
        {
            _view.OnEmployeeManageTabClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowTab(HRTab.EmployeeManage);
                    RefreshEmployeeManageList();
                })
                .AddTo(this);

            _view.OnHireTabClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowTab(HRTab.Hire);
                    _view.ShowHireMain();
                })
                .AddTo(this);
        }

        private void BindEmployeeManage()
        {
            _view.OnEmployeeManageSortChanged
                .Skip(1)
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); RefreshEmployeeManageList(); })
                .AddTo(this);

            _view.OnEmployeeManageEducationClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    if (_selectedEmployee == null) return;

                    // 현재 교육 중인 직원이면 해당 과정 인덱스를 넘겨 EducationOverlay 표시
                    var training = _EmployeeManager.Instance.GetTraining(_selectedEmployee);
                    int inTrainingIndex = -1;
                    if (training != null)
                    {
                        var courses = _EmployeeManager.Instance.trainingCourses;
                        inTrainingIndex = courses.FindIndex(c => c.courseName == training.course.courseName);
                    }

                    _selectedCourseIndex = -1;
                    _view.SetEducationCourseConfirmInteractable(false);
                    _view.ShowEducationCourse(inTrainingIndex);
                    InitCourseCostValues();
                })
                .AddTo(this);

            _view.OnEmployeeManageFireClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXNegative(); OnFireButtonClicked(_selectedEmployee); })
                .AddTo(this);

            _view.OnEmployeeManageBackClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _selectedEmployee = null;
                    _view.ShowEmployeeManageList();
                })
                .AddTo(this);

            _view.OnCourseSelected
                .Subscribe(index =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    // 이전 SelectIMG 해제 후 새 선택 반영
                    if (_selectedCourseIndex >= 0)
                        _view.SetCourseSelectImg(_selectedCourseIndex, false);

                    _selectedCourseIndex = index;
                    _view.SetCourseSelectImg(index, true);
                    _view.SetEducationCourseConfirmInteractable(true);
                })
                .AddTo(this);

            _view.OnEducationCourseConfirmClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXPositive(); OnEducationCourseConfirmClicked(); })
                .AddTo(this);

            _view.OnEducationCourseBackClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ResetCourseSelection();
                    _selectedCourseIndex = -1;
                    _view.ShowEmployeeManageDetail();
                })
                .AddTo(this);
        }

        private void BindHire()
        {
            _view.OnRecruitClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    foreach (var slider in _view.AllSliders)
                        slider.ResetSelection();

                    RefreshRecruitCost();
                    _view.ShowRecruit();
                    InitRecruitSliders();
                })
                .AddTo(this);

            _view.OnApplicantClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _view.ShowApplicantList();
                    RefreshApplicantList();
                })
                .AddTo(this);

            _view.OnRecruitBackClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _view.ShowHireMain(); })
                .AddTo(this);

            _view.OnRecruitConfirmClicked
                .Subscribe(_ => OnRecruitConfirmClicked())
                .AddTo(this);

            _view.OnApplicantListBackClicked
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); _view.ShowHireMain(); })
                .AddTo(this);

            _view.OnApplicantSortChanged
                .Skip(1)
                .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); RefreshApplicantList(); })
                .AddTo(this);

            //_view.OnFinalHireClicked
            //    .Subscribe(_ => OnFinalHireClicked())
            //    .AddTo(this);

            _view.OnHireClicked
                .Subscribe(_ => OnHireClicked())
                .AddTo(this);

            _view.OnApplicantDetailBackClicked
                .Subscribe(_ =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _selectedApplicant = null;
                    _view.ShowApplicantList();
                })
                .AddTo(this);
        }

        /// <summary>
        /// trainingCourses 데이터 기반으로 CourseCostValue 텍스트 세팅.
        /// </summary>
        private void InitCourseCostValues()
        {
            var courses = _EmployeeManager.Instance.trainingCourses;
            _view.SetCourseCostValues(
                FormatCourseCost(courses, 0),
                FormatCourseCost(courses, 1),
                FormatCourseCost(courses, 2)
            );
        }

        private static string FormatCourseCost(System.Collections.Generic.List<EmployeeTrainingCourse> courses, int index)
        {
            if (index >= courses.Count) return string.Empty;
            return $"{courses[index].cost:N0}G";
        }

        private void InitRecruitSliders()
        {
            _sliderDisposables.Clear();

            foreach (var slider in _view.AllSliders)
            {
                slider.Setup(false);

                slider.OnCountChanged
                    .Subscribe(_ => { AudioManager.Instance?.PlaySFXClick(); RefreshRecruitCost(); })
                    .AddTo(_sliderDisposables);

                slider.OnSelectedChanged += RefreshRecruitCost;

                _sliderDisposables.Add(Disposable.Create(() => slider.OnSelectedChanged -= RefreshRecruitCost));
            }

            RefreshRecruitCost();
        }

        private void RefreshRecruitCost()
        {
            int totalCount = _view.AllSliders.Sum(s => s.Count);
            int totalCost = totalCount * RecruitCostPerEmployee;
            _view.SetTotalRecruitInfo(totalCount, totalCost);
            _view.SetRecruitConfirmInteractable(totalCount > 0);
        }

        private void RefreshEmployeeManageList()
        {
            var employees = GetSortedEmployees(_view.EmployeeManageSortIndex);

            foreach (Transform child in _view.EmployeeGridContent)
                Destroy(child.gameObject);

            _view.SetEmployeeManageCountLabel(employees.Count);

            foreach (var employee in employees)
            {
                var card = Instantiate(_employeeCardPrefab, _view.EmployeeGridContent);
                card.GetComponent<IBindable<Employee>>().Bind(employee);

                var captured = employee;
                card.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _selectedEmployee = captured;
                    RefreshEmployeeDetail(captured, _view.EmployeeManageDetailContent);
                    _view.ShowEmployeeManageDetail();

                    bool isBusy = IsEmployeeBusy(captured);
                    _view.SetEducationButtonInteractable(!isBusy);
                    _view.SetFireButtonInteractable(_EmployeeManager.Instance.canLeaveSelf);
                });
            }
        }

        private void RefreshApplicantList()
        {
            foreach (Transform child in _view.ApplicantScrollContent)
                Destroy(child.gameObject);

            var applicants = _EmployeeManager.Instance.GetCurrentApplicantEmployees();

            // 0:이름순 1:직군순 2:능력치순
            var sortedApplicants = _view.ApplicantSortIndex switch
            {
                1 => applicants.OrderBy(e => GetRoleOrder(e.so.role)).ThenBy(e => e.so.Name),
                2 => applicants.OrderByDescending(e => e.so.ability),
                _ => applicants.OrderBy(e => e.so.Name),
            };

            foreach (var applicant in sortedApplicants.ToList())
            {
                var card = Instantiate(_applicantCardPrefab, _view.ApplicantScrollContent);
                card.GetComponent<IBindable<Employee>>().Bind(applicant);

                var btn = card.GetComponent<UnityEngine.UI.Button>();
                if (btn == null) continue;

                bool isLocked = applicant.so.role == Role.MARKETING || applicant.so.role == Role.QA;
                if (isLocked)
                {
                    btn.interactable = false;
                    continue;
                }

                var captured = applicant;
                btn.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFXClick();
                    _selectedApplicant = captured;
                    RefreshApplicantDetail(captured);
                    _view.ShowApplicantDetail();
                    _view.SetHireButtonInteractable(true);
                });
            }
        }

        private void RefreshEmployeeDetail(Employee employee, Transform content)
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);

            var detail = Instantiate(_employeeDetailPrefab, content);
            detail.GetComponent<IBindable<Employee>>().Bind(employee);
        }

        private void RefreshApplicantDetail(Employee applicant)
        {
            // 기존 상세 내용 지우기
            foreach (Transform child in _view.ApplicantDetailContent)
                Destroy(child.gameObject);

            // 프리팹 생성
            var detail = Instantiate(_applicantDetailPrefab, _view.ApplicantDetailContent);
            detail.GetComponent<IBindable<EmployeeImmutableData>>().Bind(applicant.so);

            //  HRView에 Hire Button
            _view.HireButton.onClick.RemoveAllListeners();
            _view.HireButton.onClick.AddListener(() =>
            {
                Debug.Log($"[고정버튼 클릭] {applicant.so.Name} 채용 시도");
                HireApplicant(applicant);
            });
        }

        private void OnRecruitConfirmClicked()
        {
            int cost = CalculateRecruitCost();
            if (Company.Instance.gold.Value < cost)
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            List<RecruitRequest> requests = _view.AllSliders
                .Where(s => s.Count > 0)
                .Select(s => new RecruitRequest(s.Role, s.Count))
                .ToList();

            AudioManager.Instance?.PlaySFXPositive();
            _EmployeeManager.Instance.RegisterRecruitRequests(requests);

            Company.Instance.gold.Value -= cost;
            Company.Instance.curManagementStatus.otherExpense += cost;
            Company.Instance.cumulativeManagementStatus.otherExpense += cost;
            Company.Instance.curManagementStatus.Recalculate();
            Company.Instance.cumulativeManagementStatus.Recalculate();

            _hudPresenter.RefreshHUD();
            foreach (var slider in _view.AllSliders)
                slider.ResetSelection();
            _view.SetRecruitButtonInteractable(false);
            _view.SetRecruitButtonLabel(true);
            _view.ShowHireMain();
        }

        // 직원 실제 고용 처리
        private void HireApplicant(Employee applicant)
        {
            if (!GameManager.Instance.CanHireMore())
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup("사무실에 자리가 없습니다. 직원을 해고하거나 사무실을 증축하세요");
                return;
            }

            if (_EmployeeManager.Instance.haveEmployees.haveEmployeeList.Exists(e => e.so.id == applicant.so.id))
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup("이미 고용된 직원입니다.");
                return;
            }

            int cost = applicant.so.hiringCost;
            if (Company.Instance.gold.Value < cost)
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            Company.Instance.gold.Value -= cost;
            Company.Instance.curManagementStatus.laborCost += cost;
            Company.Instance.cumulativeManagementStatus.laborCost += cost;
            Company.Instance.curManagementStatus.Recalculate();
            Company.Instance.cumulativeManagementStatus.Recalculate();

            AudioManager.Instance?.PlaySFXPositive();
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(applicant);
            _EmployeeManager.Instance.lastHiredEmployee = hiredEmployee;
            _EmployeeManager.Instance.currentApplicants.Remove(applicant);
            _hudPresenter.RefreshHUD();

            _selectedApplicant = null;
            RefreshApplicantList();
            _view.SetApplicantButtonLabel(true);
            _view.ShowApplicantList();
            AudioManager.Instance?.PlaySFXAlert();
            _alertView.ShowAlertPopup($"{applicant.so.Name}님을 채용했습니다.\n월요일에 출근합니다!");
        }

        private void OnHireClicked()
        {
            if (_selectedApplicant == null) return;
            _view.SetHireButtonInteractable(false);
        }

        private void OnFireButtonClicked(Employee employee)
        {
            if (employee == null) return;

            int severancePay = employee.so.hiringCost;
            string comment = employee.so.fireText;

            AudioManager.Instance?.PlaySFXAlert();
            _alertView.ShowFireConfirmPopup(
                comment,
                employee.so.iconNormal,
                $"퇴직금 {severancePay:N0}G를 지불해야합니다. 정말로 해고 하시겠습니까?",
                onConfirm: () =>
                {
                    Company.Instance.gold.Value -= severancePay;
                    Company.Instance.curManagementStatus.laborCost += severancePay;
                    Company.Instance.cumulativeManagementStatus.laborCost += severancePay;
                    Company.Instance.curManagementStatus.Recalculate();
                    Company.Instance.cumulativeManagementStatus.Recalculate();

                    _EmployeeManager.Instance.FireEmployee(employee);
                    GameManager.Instance.RemoveNpcFromScene(employee);
                    _hudPresenter.RefreshHUD();

                    AudioManager.Instance?.PlaySFXAlert();
                    _alertView.ShowNoticePopup(
                        employee.so.fireText2,
                        employee.so.Name,
                        employee.so.iconNormal
                    );

                    _selectedEmployee = null;
                    _view.ShowEmployeeManageList();
                    RefreshEmployeeManageList();
                }
            );
        }

        private void OnEducationCourseConfirmClicked()
        {
            if (_selectedEmployee == null || _selectedCourseIndex < 0) return;

            try
            {
                _EmployeeManager.Instance.StartTraining(_selectedEmployee, _selectedCourseIndex);
                _hudPresenter.RefreshHUD();
            }
            catch (System.InvalidOperationException e)
            {
                AudioManager.Instance?.PlaySFXAlert();
                _alertView.ShowAlertPopup(e.Message);
                return;
            }

            // 결정 완료 후 선택 상태 초기화
            _view.ResetCourseSelection();
            _selectedEmployee = null;
            _selectedCourseIndex = -1;
            _view.ShowEmployeeManageList();
            RefreshEmployeeManageList();
        }

        /// <summary>
        /// 드랍다운 인덱스 기준 직원 목록 정렬 반환.
        /// 0:이름순 1:직군순 2:능력치순
        /// </summary>
        private List<Employee> GetSortedEmployees(int sortIndex)
        {
            var raw = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;
            return sortIndex switch
            {
                1 => raw.OrderBy(e => GetRoleOrder(e.so.role)).ThenBy(e => e.so.Name).ToList(),
                2 => raw.OrderByDescending(e => e.MutableData.ability).ToList(),
                _ => raw.OrderBy(e => e.so.Name).ToList(),
            };
        }

        /// <summary>
        /// 프로젝트 참여 중이거나 교육 중인 직원 여부.
        /// </summary>
        private bool IsEmployeeBusy(Employee employee)
        {
            if (Company.Instance.activeProjectCount.Value > 0)
                if (Company.Instance.curProject.GetAllEmployees().Contains(employee)) return true;
            return false;
        }

        private int CalculateRecruitCost()
        {
            return _view.AllSliders.Sum(s => s.Count) * RecruitCostPerEmployee;
        }

        private static int GetRoleOrder(Role role) => role switch
        {
            Role.PLANNER => 0,
            Role.ARTIST => 1,
            Role.PROGRAMMER => 2,
            Role.QA => 3,
            Role.MARKETING => 4,
            _ => 5,
        };

        // 월요일이 지원 UI초기화
        private void ResetOnMondayUI()
        {
            // 직원모집버튼 상태 초기화
            _view.SetRecruitButtonInteractable(true);
            _view.SetRecruitButtonLabel(false);

            // 지원자 리스트 버튼 초기화
            _view.SetApplicantButtonLabel(false);
        }
    }
}
