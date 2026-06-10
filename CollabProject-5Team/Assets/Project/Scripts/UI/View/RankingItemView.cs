using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 랭킹 탭 행 프리팹 바인딩.
    /// Tab_Ranking.RankingList Content에 동적 생성. 최대 30개.
    /// 유저 회사 행은 노란색 강조. 등수에 따라 RankIcon 스프라이트 교체.
    /// </summary>
    public sealed class RankingItemView : MonoBehaviour, IBindable<RankingItemData>
    {
        [Header("순위 아이콘")]
        [SerializeField] private Image _rankIcon;

        [Header("회사 정보")]
        [SerializeField] private TextMeshProUGUI _companyNameLabel;
        [SerializeField] private TextMeshProUGUI _reputationLabel;
        [SerializeField] private TextMeshProUGUI _popularityLabel;
        [SerializeField] private TextMeshProUGUI _totalRevenueLabel;

        [Header("배경")]
        [SerializeField] private Image _rowBackground;

        [Header("트로피 스프라이트 (1~3등 전용)")]
        [SerializeField] private Sprite _iconGold;
        [SerializeField] private Sprite _iconSilver;
        [SerializeField] private Sprite _iconBronze;
        [SerializeField] private Sprite _iconDefault;

        [Header("강조 색상")]
        [SerializeField] private Color _playerRowColor;
        [SerializeField] private Color _defaultRowColor;

        public void Bind(RankingItemData data)
        {
            _companyNameLabel.text = data.companyName;
            _reputationLabel.text = FormatK(data.reputation);
            _popularityLabel.text = FormatK(data.popularity);
            _totalRevenueLabel.text = FormatK(data.totalRevenue);

            _rankIcon.sprite = GetRankSprite(data.rank);
            _rowBackground.color = data.isPlayer ? _playerRowColor : _defaultRowColor;
        }

        private Sprite GetRankSprite(int rank) => rank switch
        {
            1 => _iconGold,
            2 => _iconSilver,
            3 => _iconBronze,
            _ => _iconDefault,
        };

        private static string FormatK(int value) => $"{value:N0}K";
    }

    public sealed class RankingItemData
    {
        public int rank;
        public string companyName;
        public int reputation;
        public int popularity;
        public int totalRevenue;
        public bool isPlayer;
    }
}