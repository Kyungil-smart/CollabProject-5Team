using System.Collections.Generic;
using R3;
using UnityEngine;
using GameDevTycoon.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.CompanyPopup Presenter.
    /// 탭 전환, 경영현황 필터, 회사증축 선택/결정 처리.
    /// SO/Manager 미확정 항목은 [TODO]로 표기.
    /// </summary>
    public sealed class CompanyPresenter : MonoBehaviour, IBottomNightUI
    {
        [SerializeField] private CompanyView _view;
        [SerializeField] private AlertView _alertView;
        [SerializeField] private HUDPresenter _hudPresenter;

        [Header("프리팹")]
        [SerializeField] private RankingItemView _rankingItemPrefab;

        private ManagementFilter _currentFilter = ManagementFilter.Monthly;

        // 현재 선택된 카드 인덱스 (1~3), 미선택 시 -1
        private int _selectedCardIndex = -1;

        // 카드별 현재 상태 캐싱 — SetSelected 시 Unlocked 복원에 사용
        private readonly ExpansionCardState[] _cardStates = new ExpansionCardState[4];

        public bool IsVisible => _view.IsVisible;

        private void Start()
        {
            BindTabs();
            BindManagementStatus();
            BindExpansion();
        }

        public void Show()
        {
            _view.Show();
            RefreshCompanyInfo();
        }

        public void Hide() => _view.Hide();

        private void BindTabs()
        {
            _view.OnCompanyInfoTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(CompanyTab.CompanyInfo);
                    RefreshCompanyInfo();
                })
                .AddTo(this);

            _view.OnRankingTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(CompanyTab.Ranking);
                    RefreshRankingList();
                })
                .AddTo(this);

            _view.OnManagementStatusTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(CompanyTab.ManagementStatus);
                    RefreshManagementStatus();
                })
                .AddTo(this);

            _view.OnExpansionTabClicked
                .Subscribe(_ =>
                {
                    _view.ShowTab(CompanyTab.Expansion);
                    ResetExpansionSelection();
                    RefreshExpansionCards();
                })
                .AddTo(this);
        }

        private void BindManagementStatus()
        {
            // OnValueChangedAsObservable 즉시 발화 특성으로 인한 NullReference 방지
            _view.OnFilterChanged
                .Skip(1)
                .Subscribe(index =>
                {
                    _currentFilter = index switch
                    {
                        1 => ManagementFilter.Annual,
                        2 => ManagementFilter.Cumulative,
                        _ => ManagementFilter.Monthly,
                    };
                    RefreshManagementStatus();
                })
                .AddTo(this);
        }

        private void BindExpansion()
        {
            _view.OnExpansionConfirmClicked
                .Subscribe(_ => OnExpansionConfirmClicked())
                .AddTo(this);

            _view.OnExpansionCardLv1Clicked
                .Subscribe(OnExpansionCardClicked)
                .AddTo(this);

            _view.OnExpansionCardLv2Clicked
                .Subscribe(OnExpansionCardClicked)
                .AddTo(this);

            _view.OnExpansionCardLv3Clicked
                .Subscribe(OnExpansionCardClicked)
                .AddTo(this);
        }

        private void RefreshCompanyInfo()
        {
            // [TODO: CompanyManager 연결 후 실제 데이터 바인딩]
            var company = Company.Instance;

            _view.SetCompanyInfoLogo(null);
            _view.SetCompanyInfoLabels(
                companyName: company.Name,
                officeLevel: company.level,
                ranking: 0,
                employeeCount: GetEmployeeCount(),
                releasedGameCount: company.completedProjects.Count,
                reputation: company.reputation,
                popularity: company.popularity,
                cohesion: "좋음",   // [TODO: 내부결속력 단계 문자열 연결]
                gold: company.gold.Value,
                totalRevenue: 0     // [TODO: 누적매출액 연결]
            );
        }

        private void RefreshRankingList()
        {
            foreach (Transform child in _view.RankingListContent)
                Destroy(child.gameObject);

            // [TODO: RankingManager 연결 후 실제 경쟁사 목록 바인딩]
            var playerData = new RankingItemData
            {
                rank = 1,
                companyName = Company.Instance.Name,
                reputation = Company.Instance.reputation,
                popularity = 0,
                totalRevenue = 0,
                isPlayer = true,
            };

            var item = Instantiate(_rankingItemPrefab, _view.RankingListContent);
            item.Bind(playerData);

            _view.SetRankingUpdateNote("• 매년 첫번째 주에 업데이트");
        }

        private void RefreshManagementStatus()
        {
            // [TODO: 경영 기록 Manager 연결 후 실제 기간별 수치 바인딩]
            string periodText = BuildPeriodLabel();
            _view.SetManagementStatusColumns(_currentFilter, periodText);

            var current = new ManagementStatusData();
            ManagementStatusData previous = _currentFilter == ManagementFilter.Cumulative
                ? null
                : new ManagementStatusData();

            _view.SetManagementStatusValues(current, previous);
        }

        private void RefreshExpansionCards()
        {
            if (Company.Instance._upgradeData == null)
            {
                Debug.LogError("(UpgradeData)가 인스펙터에 할당되지 않았습니다!");
                return;
            }

            int currentLevel = Company.Instance.level;

            for (int cardIndex = 1; cardIndex <= 3; cardIndex++)
            {
                // cardIndex 1~3은 레벨 1~3에 대응
                var state = GetExpansionCardState(cardIndex, currentLevel);
                _cardStates[cardIndex] = state;

                _view.SetExpansionCardState(cardIndex, state);

                // Lv1은 LockOverlay 없음
                if (cardIndex >= 2)
                    _view.SetLockOverlayActive(cardIndex, state == ExpansionCardState.Locked);
            }
        }

        private void OnExpansionCardClicked(int cardIndex)
        {
            // Unlocked 상태 카드만 클릭 가능 — interactable로 이미 막혀있지만 이중 방어
            if (_cardStates[cardIndex] != ExpansionCardState.Unlocked &&
                _cardStates[cardIndex] != ExpansionCardState.Selected) return;

            if (_selectedCardIndex == cardIndex)
            {
                // 같은 카드 재클릭 시 선택 해제
                _cardStates[cardIndex] = ExpansionCardState.Unlocked;
                _view.SetExpansionCardState(cardIndex, ExpansionCardState.Unlocked);
                _selectedCardIndex = -1;
                _view.SetExpansionConfirmInteractable(false);
                return;
            }

            // 이전 선택 카드 복원
            if (_selectedCardIndex != -1)
            {
                _cardStates[_selectedCardIndex] = ExpansionCardState.Unlocked;
                _view.SetExpansionCardState(_selectedCardIndex, ExpansionCardState.Unlocked);
            }

            _selectedCardIndex = cardIndex;
            _cardStates[cardIndex] = ExpansionCardState.Selected;
            _view.SetExpansionCardState(cardIndex, ExpansionCardState.Selected);
            _view.SetExpansionConfirmInteractable(true);
        }

        private void OnExpansionConfirmClicked()
        {
            if (_selectedCardIndex == -1) return;

            // cardIndex와 레벨이 1:1 대응
            int targetLevel = _selectedCardIndex;
            var data = Company.Instance._upgradeData?.GetData(targetLevel);

            if (data == null) return;

            if (Company.Instance.gold.Value < data.GoldCost)
            {
                _alertView.ShowAlertPopup("보유 자금이 부족하여 실행할 수 없습니다.");
                return;
            }

            if (!Company.Instance.CheckCanUpgrade(targetLevel))
            {
                _alertView.ShowAlertPopup("증축 조건을 만족하지 않습니다.");
                return;
            }

            _alertView.ShowConfirmPopup("구매하시겠습니까?", onConfirm: () =>
            {
                Company.Instance.UpgradeOffice(targetLevel);

                ResetExpansionSelection();
                RefreshExpansionCards();
                RefreshCompanyInfo();
                _hudPresenter.RefreshHUD();
            });
        }

        private void ResetExpansionSelection()
        {
            _selectedCardIndex = -1;
            _view.SetExpansionConfirmInteractable(false);
        }

        private static ExpansionCardState GetExpansionCardState(int cardIndex, int currentLevel)
        {
            if (cardIndex < currentLevel) return ExpansionCardState.Owned;
            if (cardIndex == currentLevel) return ExpansionCardState.Current;
            if (cardIndex == currentLevel + 1) return ExpansionCardState.Unlocked;
            return ExpansionCardState.Locked;
        }

        private static string BuildPeriodLabel()
        {
            // [TODO: DateTimeManager 날짜 계산 API 확정 후 실제 기간 문자열 연결]
            int week = DateTimeManager.Instance.currentWeek.Value;
            return $"{week}주차 기준";
        }

        private static int GetEmployeeCount()
        {
            return _EmployeeManager.Instance.haveEmployees.haveEmployeeList.Count;
        }
    }
}