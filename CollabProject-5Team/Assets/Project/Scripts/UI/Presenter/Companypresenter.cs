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
        [SerializeField] private ExpansionItemView _expansionItemPrefab;

        private ManagementFilter _currentFilter = ManagementFilter.Monthly;
        private ExpansionItemView _selectedExpansionCard;

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
                    _selectedExpansionCard = null;
                    _view.SetExpansionConfirmInteractable(false);
                    RefreshExpansionList();
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
        }

        private void RefreshCompanyInfo()
        {
            // [TODO: CompanyManager 연결 후 실제 데이터 바인딩]
            var company = Company.Instance;

            _view.SetCompanyInfoLogo(null);
            _view.SetCompanyInfoLabels(
                companyName: company.Name,
                officeLevel: company.level,
                ranking: 0,        // [TODO: RankingManager 연결]
                employeeCount: GetEmployeeCount(),
                releasedGameCount: company.completedProjects.Count,
                reputation: company.reputation,
                popularity: 0,        // [TODO: CompanySO.popularity 연결]
                cohesion: "좋음",   // [TODO: 내부결속력 단계 문자열 연결]
                gold: company.gold.Value,
                totalRevenue: 0         // [TODO: 누적매출액 연결]
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

        private void RefreshExpansionList()
        {
            foreach (Transform child in _view.ExpansionListContent)
                Destroy(child.gameObject);

            int currentLevel = Company.Instance.level;

            for (int level = 1; level <= 7; level++)
            {
                var data = GetExpansionData(level);
                var state = GetExpansionCardState(level, currentLevel);

                var card = Instantiate(_expansionItemPrefab, _view.ExpansionListContent);
                card.Setup(
                    level: level,
                    sizeSprite: null,   // [TODO: 사무실 크기 스프라이트 연결]
                    cost: data.cost,
                    maxEmployee: data.maxEmployee,
                    description: GetDescriptionText(level, state),
                    state: state,
                    lockCondition1: data.lockCondition1,
                    lockCondition2: data.lockCondition2
                );

                if (state == ExpansionCardState.Unlocked)
                {
                    card.OnCardClicked
                        .Subscribe(clicked => OnExpansionCardClicked(clicked))
                        .AddTo(this);
                }
            }
        }

        private void OnExpansionCardClicked(ExpansionItemView clicked)
        {
            if (_selectedExpansionCard == clicked)
            {
                _selectedExpansionCard.SetSelected(false);
                _selectedExpansionCard = null;
                _view.SetExpansionConfirmInteractable(false);
                return;
            }

            _selectedExpansionCard?.SetSelected(false);
            _selectedExpansionCard = clicked;
            _selectedExpansionCard.SetSelected(true);
            _view.SetExpansionConfirmInteractable(true);
        }

        private void OnExpansionConfirmClicked()
        {
            if (_selectedExpansionCard == null) return;

            var data = GetExpansionData(_selectedExpansionCard.Level);

            if (Company.Instance.gold.Value < data.cost)
            {
                _alertView.ShowAlertPopup("보유 자금이 부족하여 실행할 수 없습니다.");
                return;
            }

            _alertView.ShowConfirmPopup("구매하시겠습니까?", onConfirm: () =>
            {
                // [TODO: CompanyManager 증축 처리 연결]
                Company.Instance.gold.Value -= data.cost;
                _hudPresenter.RefreshHUD();

                _selectedExpansionCard = null;
                _view.SetExpansionConfirmInteractable(false);
                RefreshExpansionList();
            });
        }

        private static ExpansionCardState GetExpansionCardState(int level, int currentLevel)
        {
            if (level < currentLevel) return ExpansionCardState.Owned;
            if (level == currentLevel) return ExpansionCardState.Current;
            if (level == currentLevel + 1) return ExpansionCardState.Unlocked;
            return ExpansionCardState.Locked;
        }

        private static string GetDescriptionText(int level, ExpansionCardState state) => state switch
        {
            ExpansionCardState.Current => "현재 적용된 상태 입니다.",
            ExpansionCardState.Owned => "보유",
            _ => GetExpansionData(level).effects,
        };

        // 기획서(System_회사 통합 시스템 v0.2) 증축 테이블
        // [TODO: SO 연결 후 하드코딩 교체]
        private static ExpansionLevelData GetExpansionData(int level) => level switch
        {
            1 => new ExpansionLevelData(0, 3, "초기 형태", "-", "-"),
            2 => new ExpansionLevelData(100000, 5, "직원들의 충성도 수치 상승\nQA 직원 고용 가능", "-", "-"),
            3 => new ExpansionLevelData(4000, 6, "직원들의 충성도 수치 상승\n중형 프로젝트 해금", "사무실 Level 2 상태", "직원들의 능력치 합 200 이상"),
            4 => new ExpansionLevelData(6000, 8, "직원들의 충성도 수치 상승\n대형 프로젝트 해금\n마케터 직원 고용 가능", "사무실 Level 3 상태\n회사 평판이 20점 이상", "A등급 이상의 프로젝트 이력 보유"),
            5 => new ExpansionLevelData(8000, 11, "회사 평판 10 상승\n게임 판매 가격 10% 증가", "사무실 Level 4 상태\n직원들의 능력치 합 400 이상", "회사 평판이 40점 이상"),
            6 => new ExpansionLevelData(10000, 15, "회사 평판 20 상승\n게임 판매 가격 20% 증가", "사무실 Level 5 상태", "회사 평판이 60점 이상"),
            7 => new ExpansionLevelData(20000, 20, "게임 판매 가격 40% 증가", "사무실 Level 6 상태\n직원들의 능력치 합 1,000 이상", "회사 랭킹 1위 달성"),
            _ => new ExpansionLevelData(0, 0, "-", "-", "-"),
        };

        private static string BuildPeriodLabel()
        {
            // [TODO: DateTimeManager 날짜 계산 API 확정 후 실제 기간 문자열 연결]
            int week = DateTimeManager.Instance.currentWeek.Value;
            return $"{week}주차 기준";
        }

        private static int GetEmployeeCount()
        {
            // [TODO: _EmployeeManager.Instance.haveEmployees 연결]
            return 0;
        }
    }

    internal sealed class ExpansionLevelData
    {
        public readonly int cost;
        public readonly int maxEmployee;
        public readonly string effects;
        public readonly string lockCondition1;
        public readonly string lockCondition2;

        public ExpansionLevelData(int cost, int maxEmployee, string effects,
            string lockCondition1, string lockCondition2)
        {
            this.cost = cost;
            this.maxEmployee = maxEmployee;
            this.effects = effects;
            this.lockCondition1 = lockCondition1;
            this.lockCondition2 = lockCondition2;
        }
    }
}
