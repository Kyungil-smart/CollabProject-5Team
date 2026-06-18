using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.CompanyPopup 담당 View.
    /// 탭 전환, 패널 전환, 버튼 이벤트 발행.
    /// 프리팹 동적 생성 및 데이터 바인딩은 Presenter에서 담당.
    /// </summary>
    public sealed class CompanyView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject _companyPopup;

        [Header("TabButtons")]
        [SerializeField] private Button _companyInfoTabButton;
        [SerializeField] private Button _rankingTabButton;
        [SerializeField] private Button _managementStatusTabButton;
        [SerializeField] private Button _expansionTabButton;

        [Header("Tab_CompanyInfo")]
        [SerializeField] private GameObject _tabCompanyInfo;
        [SerializeField] private Image _logoIcon;
        [SerializeField] private TextMeshProUGUI _companyNameLabel;
        [SerializeField] private TextMeshProUGUI _officeLevelValue;
        [SerializeField] private TextMeshProUGUI _gameRankingValue;
        [SerializeField] private TextMeshProUGUI _employeeCountValue;
        [SerializeField] private TextMeshProUGUI _releasedGameCountValue;
        [SerializeField] private TextMeshProUGUI _reputationValue;
        [SerializeField] private TextMeshProUGUI _popularityValue;
        [SerializeField] private TextMeshProUGUI _cohesionValue;
        [SerializeField] private TextMeshProUGUI _goldValue;
        [SerializeField] private TextMeshProUGUI _totalRevenueValue;

        [Header("Tab_Ranking")]
        [SerializeField] private GameObject _tabRanking;
        [SerializeField] private Transform _rankingListContent;
        [SerializeField] private TextMeshProUGUI _rankingUpdateNoteLabel;

        [Header("Tab_ManagementStatus")]
        [SerializeField] private GameObject _tabManagementStatus;
        [SerializeField] private TMP_Dropdown _filterDropdown;
        [SerializeField] private TextMeshProUGUI _periodLabel;
        [SerializeField] private TextMeshProUGUI _currentColumnHeader;
        [SerializeField] private TextMeshProUGUI _previousColumnHeader;

        // 수입 항목
        [SerializeField] private TextMeshProUGUI _totalIncomeCurrentLabel;
        [SerializeField] private TextMeshProUGUI _totalIncomePreviousLabel;
        [SerializeField] private TextMeshProUGUI _gameSalesCurrentLabel;
        [SerializeField] private TextMeshProUGUI _gameSalesPreviousLabel;
        [SerializeField] private TextMeshProUGUI _otherIncomeCurrentLabel;
        [SerializeField] private TextMeshProUGUI _otherIncomePreviousLabel;

        // 지출 항목
        [SerializeField] private TextMeshProUGUI _totalExpenseCurrentLabel;
        [SerializeField] private TextMeshProUGUI _totalExpensePreviousLabel;
        [SerializeField] private TextMeshProUGUI _laborCostCurrentLabel;
        [SerializeField] private TextMeshProUGUI _laborCostPreviousLabel;
        [SerializeField] private TextMeshProUGUI _devCostCurrentLabel;
        [SerializeField] private TextMeshProUGUI _devCostPreviousLabel;
        [SerializeField] private TextMeshProUGUI _operatingCostCurrentLabel;
        [SerializeField] private TextMeshProUGUI _operatingCostPreviousLabel;
        [SerializeField] private TextMeshProUGUI _marketingCostCurrentLabel;
        [SerializeField] private TextMeshProUGUI _marketingCostPreviousLabel;
        [SerializeField] private TextMeshProUGUI _otherExpenseCurrentLabel;
        [SerializeField] private TextMeshProUGUI _otherExpensePreviousLabel;

        // 영업이익
        [SerializeField] private TextMeshProUGUI _operatingProfitCurrentLabel;
        [SerializeField] private TextMeshProUGUI _operatingProfitPreviousLabel;

        // 누적 모드 시 비활성화할 PreviousLabel 전체 — Inspector에서 순서대로 연결
        [SerializeField] private TextMeshProUGUI[] _previousLabels;

        [Header("Tab_Expansion")]
        [SerializeField] private GameObject _tabExpansion;
        [SerializeField] private Transform _expansionListContent;
        [SerializeField] private Button _expansionConfirmButton;

        // Tab 이벤트
        public Observable<Unit> OnCompanyInfoTabClicked => _companyInfoTabButton.OnClickAsObservable();
        public Observable<Unit> OnRankingTabClicked => _rankingTabButton.OnClickAsObservable();
        public Observable<Unit> OnManagementStatusTabClicked => _managementStatusTabButton.OnClickAsObservable();
        public Observable<Unit> OnExpansionTabClicked => _expansionTabButton.OnClickAsObservable();

        // Tab_ManagementStatus 이벤트
        public Observable<int> OnFilterChanged => _filterDropdown.OnValueChangedAsObservable();

        // Tab_Expansion 이벤트
        public Observable<Unit> OnExpansionConfirmClicked => _expansionConfirmButton.OnClickAsObservable();

        // Content Transform
        public Transform RankingListContent => _rankingListContent;
        public Transform ExpansionListContent => _expansionListContent;

        public bool IsVisible => _companyPopup.activeSelf;

        private void Awake()
        {
            _companyPopup.SetActive(false);
            _expansionConfirmButton.interactable = false;
        }

        public void Show()
        {
            if (IsVisible) return;
            _companyPopup.SetActive(true);
            ShowTab(CompanyTab.CompanyInfo);
        }

        public void Hide() => _companyPopup.SetActive(false);

        public void ShowTab(CompanyTab tab)
        {
            _tabCompanyInfo.SetActive(tab == CompanyTab.CompanyInfo);
            _tabRanking.SetActive(tab == CompanyTab.Ranking);
            _tabManagementStatus.SetActive(tab == CompanyTab.ManagementStatus);
            _tabExpansion.SetActive(tab == CompanyTab.Expansion);
        }

        // Tab_CompanyInfo 수치 표시
        public void SetCompanyInfoLogo(Sprite logo)
        {
            if (_logoIcon != null)
                _logoIcon.sprite = logo;
        }

        public void SetCompanyInfoLabels(
            string companyName, int officeLevel, int ranking,
            int employeeCount, int releasedGameCount,
            int reputation, int popularity, string cohesion,
            int gold, int totalRevenue)
        {
            _companyNameLabel.text = companyName;
            _officeLevelValue.text = $"{officeLevel} 레벨";
            _gameRankingValue.text = $"{ranking} 위";
            _employeeCountValue.text = $"{employeeCount} 명";
            _releasedGameCountValue.text = $"{releasedGameCount} 개";
            _reputationValue.text = FormatK(reputation);
            _popularityValue.text = FormatK(popularity);
            _cohesionValue.text = cohesion;
            _goldValue.text = FormatK(gold);
            _totalRevenueValue.text = FormatK(totalRevenue);
        }

        // Tab_Ranking
        public void SetRankingUpdateNote(string note) => _rankingUpdateNoteLabel.text = note;

        /// <summary>
        /// 누적 모드에서는 PreviousLabel 전체 비활성. 월간/연간은 이번/지난 비교 표시.
        /// </summary>
        public void SetManagementStatusColumns(ManagementFilter filter, string periodText)
        {
            _periodLabel.text = periodText;

            bool isCumulative = filter == ManagementFilter.Cumulative;

            foreach (var label in _previousLabels)
                label.gameObject.SetActive(!isCumulative);

            _currentColumnHeader.text = isCumulative ? "누적" : (filter == ManagementFilter.Monthly ? "이번 달" : "이번 해");
            _previousColumnHeader.gameObject.SetActive(!isCumulative);
            _previousColumnHeader.text = filter == ManagementFilter.Monthly ? "지난 달" : "지난 해";
        }

        public void SetManagementStatusValues(ManagementStatusData current, ManagementStatusData previous)
        {
            SetIncomeRow(_totalIncomeCurrentLabel, _totalIncomePreviousLabel, current.totalIncome, previous?.totalIncome, isBold: true);
            SetIncomeRow(_gameSalesCurrentLabel, _gameSalesPreviousLabel, current.gameSales, previous?.gameSales);
            SetIncomeRow(_otherIncomeCurrentLabel, _otherIncomePreviousLabel, current.otherIncome, previous?.otherIncome);
            SetExpenseRow(_totalExpenseCurrentLabel, _totalExpensePreviousLabel, current.totalExpense, previous?.totalExpense, isBold: true);
            SetExpenseRow(_laborCostCurrentLabel, _laborCostPreviousLabel, current.laborCost, previous?.laborCost);
            SetExpenseRow(_devCostCurrentLabel, _devCostPreviousLabel, current.devCost, previous?.devCost);
            SetExpenseRow(_operatingCostCurrentLabel, _operatingCostPreviousLabel, current.operatingCost, previous?.operatingCost);
            SetExpenseRow(_marketingCostCurrentLabel, _marketingCostPreviousLabel, current.marketingCost, previous?.marketingCost);
            SetExpenseRow(_otherExpenseCurrentLabel, _otherExpensePreviousLabel, current.otherExpense, previous?.otherExpense);
            SetProfitRow(_operatingProfitCurrentLabel, _operatingProfitPreviousLabel, current.operatingProfit, previous?.operatingProfit);
        }

        // Tab_Expansion
        public void SetExpansionConfirmInteractable(bool interactable)
        {
            _expansionConfirmButton.interactable = interactable;
        }

        // 수입 행: 검정색, 없으면 "-"
        private static void SetIncomeRow(
            TextMeshProUGUI current, TextMeshProUGUI previous,
            int currentVal, int? previousVal, bool isBold = false)
        {
            current.text = FormatK(currentVal);
            current.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            current.color = Color.black;

            if (previous == null) return;
            previous.text = previousVal.HasValue ? FormatK(previousVal.Value) : "-";
            previous.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            previous.color = Color.black;
        }

        // 지출 행: 빨간색, 값 있으면 괄호 표기
        private static void SetExpenseRow(
            TextMeshProUGUI current, TextMeshProUGUI previous,
            int currentVal, int? previousVal, bool isBold = false)
        {
            current.text = currentVal > 0 ? $"({FormatK(currentVal)})" : "-";
            current.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            current.color = Color.red;

            if (previous == null) return;
            previous.text = previousVal.HasValue && previousVal.Value > 0
                ? $"({FormatK(previousVal.Value)})" : "-";
            previous.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            previous.color = Color.red;
        }

        // 영업이익: 양수 검정 / 음수 빨강 / Bold
        private static void SetProfitRow(
            TextMeshProUGUI current, TextMeshProUGUI previous,
            int currentVal, int? previousVal)
        {
            current.text = currentVal >= 0 ? FormatK(currentVal) : $"({FormatK(-currentVal)})";
            current.color = currentVal >= 0 ? Color.black : Color.red;
            current.fontStyle = FontStyles.Bold;

            if (previous == null) return;
            if (!previousVal.HasValue)
            {
                previous.text = "-";
                previous.color = Color.black;
            }
            else
            {
                previous.text = previousVal.Value >= 0 ? FormatK(previousVal.Value) : $"({FormatK(-previousVal.Value)})";
                previous.color = previousVal.Value >= 0 ? Color.black : Color.red;
            }
            previous.fontStyle = FontStyles.Bold;
        }

        private static string FormatK(int value) => $"{value:N0}K";
    }

    public enum CompanyTab
    {
        CompanyInfo,
        Ranking,
        ManagementStatus,
        Expansion
    }

    public enum ManagementFilter
    {
        Monthly,
        Annual,
        Cumulative
    }

    public sealed class ManagementStatusData
    {
        public int totalIncome;
        public int gameSales;
        public int otherIncome;
        public int totalExpense;
        public int laborCost;
        public int devCost;
        public int operatingCost;
        public int marketingCost;
        public int otherExpense;
        public int operatingProfit;
    }
}