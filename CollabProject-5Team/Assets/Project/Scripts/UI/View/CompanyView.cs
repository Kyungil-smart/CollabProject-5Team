using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.CompanyPopup 담당 View.
    /// 탭 전환, 패널 전환, 버튼 이벤트 발행.
    /// 데이터 바인딩은 Presenter에서 담당.
    /// </summary>
    public sealed class CompanyView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject _companyPopup;

        [Header("TabButtons")]
        [SerializeField] private Button _companyInfoTabButton;
        [SerializeField] private Button _managementStatusTabButton;
        [SerializeField] private Button _expansionTabButton;

        [Header("TabButton Sprites")]
        [SerializeField] private Sprite _tabActiveSprite;
        [SerializeField] private Sprite _tabInactiveSprite;
        [SerializeField] private Color _tabActiveLabelColor;
        [SerializeField] private Color _tabInactiveLabelColor;

        [Header("Tab_CompanyInfo")]
        [SerializeField] private GameObject _tabCompanyInfo;
        [SerializeField] private TextMeshProUGUI _companyNameLabel;
        [SerializeField] private TextMeshProUGUI _officeLevelValue;
        [SerializeField] private TextMeshProUGUI _gameRankingValue;
        [SerializeField] private TextMeshProUGUI _employeeCountValue;
        [SerializeField] private TextMeshProUGUI _releasedGameCountValue;
        [SerializeField] private TextMeshProUGUI _reputationValue;
        [SerializeField] private TextMeshProUGUI _popularityValue;
        [SerializeField] private TextMeshProUGUI _goldValue;
        [SerializeField] private TextMeshProUGUI _totalRevenueValue;

        [Header("Tab_ManagementStatus")]
        [SerializeField] private GameObject _tabManagementStatus;
        [SerializeField] private TMP_Dropdown _filterDropdown;
        [SerializeField] private TextMeshProUGUI _periodLabel;
        [SerializeField] private TextMeshProUGUI _currentColumnHeader;
        [SerializeField] private TextMeshProUGUI _previousColumnHeader;
        [SerializeField] private GameObject _columnDivider;

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
        [SerializeField] private Button _expansionConfirmButton;

        // 고정 카드 버튼 (Lv1~3)
        [SerializeField] private Button _expansionCardLv1;
        [SerializeField] private Button _expansionCardLv2;
        [SerializeField] private Button _expansionCardLv3;

        // Lv2/3만 LockOverlay 존재
        [SerializeField] private GameObject _lockOverlayLv2;
        [SerializeField] private GameObject _lockOverlayLv3;

        // 카드별 SelectIMG
        [SerializeField] private GameObject _selectImgLv1;
        [SerializeField] private GameObject _selectImgLv2;
        [SerializeField] private GameObject _selectImgLv3;

        [Header("ConfirmButton 스프라이트")]
        [SerializeField] private Sprite _confirmActiveSprite;
        [SerializeField] private Sprite _confirmInactiveSprite;

        // Tab 이벤트
        public Observable<Unit> OnCompanyInfoTabClicked => _companyInfoTabButton.OnClickAsObservable();
        public Observable<Unit> OnManagementStatusTabClicked => _managementStatusTabButton.OnClickAsObservable();
        public Observable<Unit> OnExpansionTabClicked => _expansionTabButton.OnClickAsObservable();

        // Tab_ManagementStatus 이벤트
        public Observable<int> OnFilterChanged => _filterDropdown.OnValueChangedAsObservable();

        // Tab_Expansion 이벤트
        public Observable<Unit> OnExpansionConfirmClicked => _expansionConfirmButton.OnClickAsObservable();
        public Observable<int> OnExpansionCardLv1Clicked => _expansionCardLv1.OnClickAsObservable().Select(_ => 1);
        public Observable<int> OnExpansionCardLv2Clicked => _expansionCardLv2.OnClickAsObservable().Select(_ => 2);
        public Observable<int> OnExpansionCardLv3Clicked => _expansionCardLv3.OnClickAsObservable().Select(_ => 3);

        public bool IsVisible => _companyPopup.activeSelf;

        private static void RegisterDropdownSFX(TMP_Dropdown dropdown)
        {
            var trigger = dropdown.gameObject.GetComponent<EventTrigger>()
                          ?? dropdown.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => AudioManager.Instance?.PlaySFXClick());
            trigger.triggers.Add(entry);
        }

        private void Awake()
        {
            RegisterDropdownSFX(_filterDropdown);

            _expansionConfirmButton.interactable = false;
            _expansionConfirmButton.image.sprite = _confirmInactiveSprite;

            _selectImgLv1.SetActive(false);
            _selectImgLv2.SetActive(false);
            _selectImgLv3.SetActive(false);
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
            _tabManagementStatus.SetActive(tab == CompanyTab.ManagementStatus);
            _tabExpansion.SetActive(tab == CompanyTab.Expansion);

            SetTabButtonState(_companyInfoTabButton, tab == CompanyTab.CompanyInfo);
            SetTabButtonState(_managementStatusTabButton, tab == CompanyTab.ManagementStatus);
            SetTabButtonState(_expansionTabButton, tab == CompanyTab.Expansion);
        }

        private void SetTabButtonState(Button button, bool isActive)
        {
            button.image.sprite = isActive ? _tabActiveSprite : _tabInactiveSprite;
            button.GetComponentInChildren<TextMeshProUGUI>().color = isActive ? _tabActiveLabelColor : _tabInactiveLabelColor;
        }

        // Tab_CompanyInfo 수치 표시
        public void SetCompanyInfoLabels(
            string companyName, int officeLevel, int ranking,
            int employeeCount, int releasedGameCount,
            int reputation, int popularity,
            int gold, int totalRevenue)
        {
            _companyNameLabel.text = companyName;
            _officeLevelValue.text = $"{officeLevel} 레벨";
            _gameRankingValue.text = $"{ranking} 위";
            _employeeCountValue.text = $"{employeeCount} 명";
            _releasedGameCountValue.text = $"{releasedGameCount} 개";
            _reputationValue.text = reputation.ToString();
            _popularityValue.text = popularity.ToString();
            _goldValue.text = FormatPolicy.FormatGold(gold);
            _totalRevenueValue.text = FormatPolicy.FormatGold(totalRevenue);
        }

        /// <summary>
        /// 누적 모드에서는 PreviousLabel 전체 비활성. 월간은 이번/지난 비교 표시.
        /// </summary>
        public void SetManagementStatusColumns(ManagementFilter filter, string periodText)
        {
            _periodLabel.text = periodText;

            bool isCumulative = filter == ManagementFilter.Cumulative;

            foreach (var label in _previousLabels)
                label.gameObject.SetActive(!isCumulative);

            _currentColumnHeader.text = isCumulative ? "누적" : "이번 달";
            _previousColumnHeader.gameObject.SetActive(!isCumulative);
            _previousColumnHeader.text = "지난 달";
            _columnDivider.SetActive(!isCumulative);
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
            _expansionConfirmButton.image.sprite = interactable ? _confirmActiveSprite : _confirmInactiveSprite;
        }

        /// <summary>
        /// SelectIMG 토글 및 interactable 설정.
        /// cardIndex: 1~3
        /// </summary>
        public void SetExpansionCardState(int cardIndex, ExpansionCardState state)
        {
            var button = cardIndex switch
            {
                1 => _expansionCardLv1,
                2 => _expansionCardLv2,
                3 => _expansionCardLv3,
                _ => null,
            };

            var selectImg = cardIndex switch
            {
                1 => _selectImgLv1,
                2 => _selectImgLv2,
                3 => _selectImgLv3,
                _ => null,
            };

            if (button == null) return;

            if (selectImg != null)
                selectImg.SetActive(state == ExpansionCardState.Selected);

            button.interactable = state == ExpansionCardState.Unlocked || state == ExpansionCardState.Selected;
        }

        public void SetLockOverlayActive(int cardIndex, bool active)
        {
            var overlay = cardIndex switch
            {
                2 => _lockOverlayLv2,
                3 => _lockOverlayLv3,
                _ => null,
            };

            if (overlay != null)
                overlay.SetActive(active);
        }

        // 수입 행: 검정색, 없으면 "-"
        private static void SetIncomeRow(
            TextMeshProUGUI current, TextMeshProUGUI previous,
            int currentVal, int? previousVal, bool isBold = false)
        {
            current.text = FormatPolicy.FormatGold(currentVal);
            current.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            current.color = Color.black;

            if (previous == null) return;
            previous.text = previousVal.HasValue ? FormatPolicy.FormatGold(previousVal.Value) : "-";
            previous.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            previous.color = Color.black;
        }

        // 지출 행: 빨간색, 값 있으면 괄호 표기
        private static void SetExpenseRow(
            TextMeshProUGUI current, TextMeshProUGUI previous,
            int currentVal, int? previousVal, bool isBold = false)
        {
            current.text = currentVal > 0 ? $"({FormatPolicy.FormatGold(currentVal)})" : "-";
            current.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            current.color = Color.red;

            if (previous == null) return;
            previous.text = previousVal.HasValue && previousVal.Value > 0
                ? $"({FormatPolicy.FormatGold(previousVal.Value)})" : "-";
            previous.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            previous.color = Color.red;
        }

        // 영업이익: 양수 검정 / 음수 빨강 / Bold
        private static void SetProfitRow(
            TextMeshProUGUI current, TextMeshProUGUI previous,
            int currentVal, int? previousVal)
        {
            current.text = currentVal >= 0 ? FormatPolicy.FormatGold(currentVal) : $"({FormatPolicy.FormatGold(-currentVal)})";
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
                previous.text = previousVal.Value >= 0 ? FormatPolicy.FormatGold(previousVal.Value) : $"({FormatPolicy.FormatGold(-previousVal.Value)})";
                previous.color = previousVal.Value >= 0 ? Color.black : Color.red;
            }
            previous.fontStyle = FontStyles.Bold;
        }

    }

    public enum CompanyTab
    {
        CompanyInfo,
        ManagementStatus,
        Expansion
    }

    public enum ManagementFilter
    {
        Monthly,
        Cumulative
    }

}
