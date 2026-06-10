using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 회사증축 탭 레벨 카드 프리팹 바인딩.
    /// Tab_Expansion.ExpansionList Content에 동적 생성. 최대 7개.
    /// 카드 상태: Current / Owned / Unlocked / Selected / Locked
    /// </summary>
    public sealed class ExpansionItemView : MonoBehaviour
    {
        [Header("카드 버튼")]
        [SerializeField] private Button _cardButton;

        [Header("정보")]
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Image _sizeImage;
        [SerializeField] private TextMeshProUGUI _costLabel;
        [SerializeField] private TextMeshProUGUI _maxEmployeeLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;

        [Header("잠금 오버레이")]
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private TextMeshProUGUI _lockConditionLabel1;
        [SerializeField] private TextMeshProUGUI _lockConditionLabel2;

        [Header("상태별 스프라이트")]
        [SerializeField] private Sprite _spriteCurrentOrOwned;
        [SerializeField] private Sprite _spriteUnlocked;
        [SerializeField] private Sprite _spriteSelected;
        [SerializeField] private Sprite _spriteLocked;

        public Observable<ExpansionItemView> OnCardClicked { get; private set; }

        private ExpansionCardState _state;
        private int _level;

        public int Level => _level;

        private void Awake()
        {
            OnCardClicked = _cardButton.OnClickAsObservable()
                .Select(_ => this);
        }

        public void Setup(int level, Sprite sizeSprite, int cost, int maxEmployee,
            string description, ExpansionCardState state,
            string lockCondition1 = "", string lockCondition2 = "")
        {
            _level = level;
            _state = state;

            _levelLabel.text = $"Level {level}";
            _sizeImage.sprite = sizeSprite;
            _costLabel.text = cost > 0 ? $"돈 : {cost:N0}G" : "돈 : 0G";
            _maxEmployeeLabel.text = $"최대직원수 : {maxEmployee}명";
            _descriptionLabel.text = description;

            ApplyState(state, lockCondition1, lockCondition2);
        }

        public void SetSelected(bool selected)
        {
            if (_state == ExpansionCardState.Current || _state == ExpansionCardState.Owned) return;

            _state = selected ? ExpansionCardState.Selected : ExpansionCardState.Unlocked;
            ApplyVisual(_state);
        }

        private void ApplyState(ExpansionCardState state, string lockCondition1, string lockCondition2)
        {
            bool isLocked = state == ExpansionCardState.Locked;
            _lockOverlay.SetActive(isLocked);
            _cardButton.interactable = state == ExpansionCardState.Unlocked;

            if (isLocked)
            {
                _lockConditionLabel1.text = lockCondition1;
                _lockConditionLabel2.text = lockCondition2;
            }

            ApplyVisual(state);
        }

        private void ApplyVisual(ExpansionCardState state)
        {
            _cardButton.image.sprite = state switch
            {
                ExpansionCardState.Current => _spriteCurrentOrOwned,
                ExpansionCardState.Owned => _spriteCurrentOrOwned,
                ExpansionCardState.Unlocked => _spriteUnlocked,
                ExpansionCardState.Selected => _spriteSelected,
                ExpansionCardState.Locked => _spriteLocked,
                _ => _spriteLocked,
            };
        }
    }

    public enum ExpansionCardState
    {
        Current,
        Owned,
        Unlocked,
        Selected,
        Locked,
    }
}