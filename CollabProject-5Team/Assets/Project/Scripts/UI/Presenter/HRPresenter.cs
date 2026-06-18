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
            BindFire();
            BindEducation();
        }

        public void Show()
        {
            _view.Show();
            _view.ShowTab(HRTab.EmployeeManage);
            RefreshEmployeeManageList();
        }

        public void Hide() => _view.Hide();

        private void BindTabs()
        {
            _view.OnEmployeeManageTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(HRTab.EmployeeManage);
                    RefreshEmployeeManageList();
                })
                .AddTo(this);

            _view.OnHireTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(HRTab.Hire);
                    _view.ShowHireMain();
                })
                .AddTo(this);

            _view.OnFireTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(HRTab.Fire);
                    RefreshFireList();
                })
                .AddTo(this);

            _view.OnEducationTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(HRTab.Education);
                    RefreshEducationList();
                })
                .AddTo(this);
        }

        private void BindEmployeeManage()
        {
            // [TODO: 드롭다운 정렬 기능 확정 후 활성화]
            // _view.OnEmployeeManageSortChanged
            //     .Subscribe(_ => RefreshEmployeeManageList())
            //     .AddTo(this);

            _view.OnEmployeeManageEducationClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(HRTab.Education);
                    RefreshEducationList();
                })
                .AddTo(this);

            _view.OnEmployeeManageFireClicked
                .Subscribe(_ => OnFireButtonClicked(_selectedEmployee))
                .AddTo(this);

            _view.OnEmployeeManageBackClicked
                .Subscribe(_ =>
                {
                    _selectedEmployee = null;
                    _view.ShowEmployeeManageList();
                })
                .AddTo(this);
        }

        private void BindHire()
        {
            _view.OnRecruitClicked
                .Subscribe(_ =>
                {
                    _view.ShowRecruit();
                    InitRecruitSliders();
                })
                .AddTo(this);

            _view.OnApplicantClicked
                .Subscribe(_ =>
                {
                    _view.ShowApplicantList();
                    RefreshApplicantList();
                })
                .AddTo(this);

            _view.OnRecruitBackClicked
                .Subscribe(_ => _view.ShowHireMain())
                .AddTo(this);

            _view.OnRecruitConfirmClicked
                .Subscribe(_ => OnRecruitConfirmClicked())
                .AddTo(this);

            _view.OnApplicantListBackClicked
                .Subscribe(_ => _view.ShowHireMain())
                .AddTo(this);

            // [TODO: 드롭다운 정렬 기능 확정 후 활성화]
            // _view.OnApplicantSortChanged
            //     .Subscribe(_ => RefreshApplicantList())
            //     .AddTo(this);

            _view.OnFinalHireClicked
                .Subscribe(_ => OnFinalHireClicked())
                .AddTo(this);

            _view.OnHireClicked
                .Subscribe(_ => OnHireClicked())
                .AddTo(this);

            _view.OnCancelHireClicked
                .Subscribe(_ => OnCancelHireClicked())
                .AddTo(this);

            _view.OnApplicantDetailBackClicked
                .Subscribe(_ =>
                {
                    _selectedApplicant = null;
                    _view.ShowApplicantList();
                })
                .AddTo(this);
        }

        private void BindFire()
        {
            // [TODO: 드롭다운 정렬 기능 확정 후 활성화]
            // _view.OnFireSortChanged
            //     .Subscribe(_ => RefreshFireList())
            //     .AddTo(this);

            _view.OnFireConfirmClicked
                .Subscribe(_ => OnFireButtonClicked(_selectedEmployee))
                .AddTo(this);

            _view.OnFireBackClicked
                .Subscribe(_ =>
                {
                    _selectedEmployee = null;
                    _view.ShowFireList();
                })
                .AddTo(this);
        }

        private void BindEducation()
        {
            // [TODO: 드롭다운 정렬 기능 확정 후 활성화]
            // _view.OnEducationSortChanged
            //     .Subscribe(_ => RefreshEducationList())
            //     .AddTo(this);

            _view.OnEducationClicked
                .Subscribe(_ =>
                {
                    if (_selectedEmployee == null) return;
                    _view.ShowEducationCourse();
                    _selectedCourseIndex = -1;
                    _view.SetEducationCourseConfirmInteractable(false);
                })
                .AddTo(this);

            _view.OnCourseSelected
                .Subscribe(index =>
                {
                    _selectedCourseIndex = index;
                    _view.SetEducationCourseConfirmInteractable(true);
                })
                .AddTo(this);

            _view.OnEducationDetailBackClicked
                .Subscribe(_ =>
                {
                    _selectedEmployee = null;
                    _view.ShowEducationList();
                })
                .AddTo(this);

            _view.OnEducationCourseConfirmClicked
                .Subscribe(_ => OnEducationCourseConfirmClicked())
                .AddTo(this);

            _view.OnEducationCourseBackClicked
                .Subscribe(_ => _view.ShowEducationDetail())
                .AddTo(this);
        }

        private void InitRecruitSliders()
        {
            _sliderDisposables.Clear();

            int level = Company.Instance.level;

            foreach (var slider in _view.AllSliders)
            {
                bool isLocked = slider.Role == Role.QA && level < 2
                             || slider.Role == Role.MARKETING && level < 4;
                slider.Setup(isLocked);

                slider.OnCountChanged
                    .Subscribe(_ => RefreshRecruitCost())
                    .AddTo(_sliderDisposables);
            }

            RefreshRecruitCost();
        }

        private void RefreshRecruitCost()
        {
            int totalCount = _view.AllSliders.Sum(s => s.Count);
            int totalCost = totalCount * 1000;
            _view.SetTotalRecruitInfo(totalCount, totalCost);
            _view.SetRecruitConfirmInteractable(totalCount > 0);
        }

        private void RefreshEmployeeManageList()
        {
            var employees = GetSortedEmployees();

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
                    _selectedEmployee = captured;
                    RefreshEmployeeDetail(captured, _view.EmployeeManageDetailContent, isEducationContext: false);
                    _view.ShowEmployeeManageDetail();

                    bool isBusy = IsEmployeeBusy(captured);
                    _view.SetEducationButtonInteractable(!isBusy);
                });
            }
        }

        private void RefreshApplicantList()
        {
            foreach (Transform child in _view.ApplicantScrollContent)
                Destroy(child.gameObject);

            var applicants = _EmployeeManager.Instance.employeeList.leftEmployees.Values
                .Select(go => go.GetComponent<Employee>())
                .Where(e => e != null)
                .ToList();

            foreach (var applicant in applicants)
            {
                var card = Instantiate(_applicantCardPrefab, _view.ApplicantScrollContent);
                card.GetComponent<IBindable<Employee>>().Bind(applicant);

                // 마케팅, QA는 잠금 처리 — 버튼 비활성 + LockOverlay 활성
                bool isLocked = applicant.so.role == Role.MARKETING || applicant.so.role == Role.QA;
                if (isLocked)
                {
                    card.GetComponent<UnityEngine.UI.Button>().interactable = false;
                    continue;
                }

                var captured = applicant;
                card.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    _selectedApplicant = captured;
                    RefreshApplicantDetail(captured);
                    _view.ShowApplicantDetail();

                    _view.SetHireButtonInteractable(true);
                    _view.SetCancelHireButtonInteractable(false);
                });
            }
        }

        private void RefreshFireList()
        {
            var employees = GetSortedEmployees();

            foreach (Transform child in _view.FireListContent)
                Destroy(child.gameObject);

            _view.SetFireCountLabel(employees.Count);

            foreach (var employee in employees)
            {
                var card = Instantiate(_employeeCardPrefab, _view.FireListContent);
                card.GetComponent<IBindable<Employee>>().Bind(employee);

                var captured = employee;
                card.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    _selectedEmployee = captured;
                    RefreshEmployeeDetail(captured, _view.FireDetailContent, isEducationContext: false);
                    _view.ShowFireDetail();
                });
            }
        }

        private void RefreshEducationList()
        {
            var employees = GetSortedEmployees()
                .Where(e => !IsEmployeeBusy(e))
                .ToList();

            foreach (Transform child in _view.EducationListContent)
                Destroy(child.gameObject);

            _view.SetEducationCountLabel(employees.Count);

            foreach (var employee in employees)
            {
                var card = Instantiate(_employeeCardPrefab, _view.EducationListContent);
                card.GetComponent<IBindable<Employee>>().Bind(employee);

                var captured = employee;
                card.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() =>
                {
                    _selectedEmployee = captured;
                    RefreshEmployeeDetail(captured, _view.EducationDetailContent, isEducationContext: true);
                    _view.ShowEducationDetail();
                    _view.SetEducationButtonInteractable(true);
                });
            }
        }

        private void RefreshEmployeeDetail(Employee employee, Transform content, bool isEducationContext)
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);

            var detail = Instantiate(_employeeDetailPrefab, content);
            detail.GetComponent<IBindable<Employee>>().Bind(employee);
        }

        private void RefreshApplicantDetail(Employee applicant)
        {
            foreach (Transform child in _view.ApplicantDetailContent)
                Destroy(child.gameObject);

            var detail = Instantiate(_applicantDetailPrefab, _view.ApplicantDetailContent);
            detail.GetComponent<IBindable<Employee>>().Bind(applicant);
        }

        private void OnRecruitConfirmClicked()
        {
            int cost = CalculateRecruitCost();
            if (Company.Instance.gold.Value < cost)
            {
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            Company.Instance.gold.Value -= cost;
            _hudPresenter.RefreshHUD();

            foreach (var slider in _view.AllSliders)
                slider.ResetSelection();

            _view.SetRecruitButtonInteractable(false);
            _view.SetRecruitButtonLabel(true);
            _view.ShowHireMain();
        }

        private void OnFinalHireClicked()
        {
            if (_selectedApplicant == null) return;

            int cost = _selectedApplicant.so.hiringCost;
            if (Company.Instance.gold.Value < cost)
            {
                _alertView.ShowAlertPopup("보유 자금이 부족합니다.");
                return;
            }

            Company.Instance.gold.Value -= cost;
            _EmployeeManager.Instance.HireEmployee(_selectedApplicant.so.id);
            _hudPresenter.RefreshHUD();

            _view.SetApplicantButtonLabel(true);
            _view.ShowHireMain();
        }

        private void OnHireClicked()
        {
            if (_selectedApplicant == null) return;
            _view.SetHireButtonInteractable(false);
            _view.SetCancelHireButtonInteractable(true);
            _view.SetFinalHireInteractable(true);
        }

        private void OnCancelHireClicked()
        {
            _view.SetHireButtonInteractable(true);
            _view.SetCancelHireButtonInteractable(false);
            _view.SetFinalHireInteractable(false);
        }

        private void OnFireButtonClicked(Employee employee)
        {
            if (employee == null) return;

            int severancePay = employee.so.hiringCost;
            string comment = employee.so.fireText;

            _alertView.ShowFireConfirmPopup(
                comment,
                employee.so.iconNormal,
                $"퇴직금 {severancePay:N0}G를 지불해야합니다. 정말로 해고 하시겠습니까?",
                onConfirm: () =>
                {
                    Company.Instance.gold.Value -= severancePay;
                    _EmployeeManager.Instance.FireEmployee(employee);
                    _hudPresenter.RefreshHUD();

                    _alertView.ShowNoticePopup(
                        employee.so.fireText2,
                        employee.so.Name,
                        employee.so.iconNormal
                    );

                    _selectedEmployee = null;
                    _view.ShowFireList();
                    RefreshFireList();
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
                _alertView.ShowAlertPopup(e.Message);
                return;
            }

            _selectedEmployee = null;
            _selectedCourseIndex = -1;
            _view.ShowEducationList();
            RefreshEducationList();
        }

        /// <summary>
        /// 직원 목록 이름순 반환.
        /// [TODO: 드롭다운 정렬 기능 확정 후 분기 처리]
        /// </summary>
        private List<Employee> GetSortedEmployees()
        {
            return _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                .OrderBy(e => e.so.Name)
                .ToList();
        }

        /// <summary>
        /// 프로젝트 참여 중이거나 교육 중인 직원 여부.
        /// </summary>
        private bool IsEmployeeBusy(Employee employee)
        {
            if (Company.Instance.curProject != null)
                if (Company.Instance.curProject.GetAllEmployees().Contains(employee)) return true;
            return false;
        }

        private int CalculateRecruitCost()
        {
            return _view.AllSliders.Sum(s => s.Count) * 1000;
        }
    }
}