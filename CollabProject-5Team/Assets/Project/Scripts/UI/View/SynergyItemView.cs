using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 시너지 팝업 자리별 카드 프리팹 바인딩.
    /// SynergyPopup.SynergyScroll.Content에 최대 11개 동적 생성.
    /// 빈 슬롯은 SetEmpty() 호출.
    /// </summary>
    public sealed class SynergyItemView : MonoBehaviour, IBindable<Employee>
    {
        [SerializeField] private Image           _cardFrame;
        [SerializeField] private Image           _deskCharImage;
        [SerializeField] private TextMeshProUGUI _mbtiLabel;
        [SerializeField] private TextMeshProUGUI _synergyCombLabel;
        [SerializeField] private TextMeshProUGUI _abilityLabel;
        [SerializeField] private TextMeshProUGUI _buffLabel;
        [SerializeField] private TextMeshProUGUI _finalAbilityLabel;
        [SerializeField] private TextMeshProUGUI _finalAbilityWeightLabel;

        [Header("빈 슬롯 색상")]
        [SerializeField] private Color _emptyColor;
        [SerializeField] private Color _activeColor;

        private static readonly Color ColorUp   = Color.red;
        private static readonly Color ColorDown = Color.blue;

        public void Bind(Employee employee)
        {
            _cardFrame.color = _activeColor;

            var so      = employee.so;
            var mutable = employee.MutableData;

            // [TODO: 책상 배치 이미지는 다른 팀원 담당 — 자리 번호 포함]
            _mbtiLabel.text = MbtiToString(so.mbtiParsed);

            // [TODO: 시너지 조합 로직 확정 후 실제 계산 연결]
            _synergyCombLabel.text = "-";

            _abilityLabel.text = so.ability.ToString();

            // [TODO: 시너지 버프/디버프 계산 로직 확정 후 연결]
            SetAbilityBuff(0, mutable.ability);
        }

        public void SetEmpty()
        {
            _cardFrame.color         = _emptyColor;
            _mbtiLabel.text          = string.Empty;
            _synergyCombLabel.text   = string.Empty;
            _abilityLabel.text       = string.Empty;
            _buffLabel.text          = string.Empty;
            _finalAbilityLabel.text  = string.Empty;
            _finalAbilityWeightLabel.text = string.Empty;

            if (_deskCharImage != null)
                _deskCharImage.color = _emptyColor;
        }

        private void SetAbilityBuff(int buffDelta, int baseAbility)
        {
            int finalAbility = baseAbility + buffDelta;

            if (buffDelta > 0)
            {
                _buffLabel.text          = $"+{buffDelta}%";
                _buffLabel.color         = ColorUp;
                _finalAbilityLabel.text  = $"{finalAbility} ({buffDelta}▲)";
                _finalAbilityLabel.color = ColorUp;
            }
            else if (buffDelta < 0)
            {
                _buffLabel.text          = $"{buffDelta}%";
                _buffLabel.color         = ColorDown;
                _finalAbilityLabel.text  = $"{finalAbility} ({Mathf.Abs(buffDelta)}▼)";
                _finalAbilityLabel.color = ColorDown;
            }
            else
            {
                _buffLabel.text          = "-";
                _buffLabel.color         = Color.white;
                _finalAbilityLabel.text  = finalAbility.ToString();
                _finalAbilityLabel.color = Color.white;
            }

            _finalAbilityWeightLabel.text = $"기본 {baseAbility}";
        }

        private static string MbtiToString(MbtiFlags mbti)
        {
            string ei = mbti.HasFlag(MbtiFlags.E) ? "E" : "I";
            string sn = mbti.HasFlag(MbtiFlags.S) ? "S" : "N";
            string tf = mbti.HasFlag(MbtiFlags.F) ? "F" : "T";
            string jp = mbti.HasFlag(MbtiFlags.J) ? "J" : "P";
            return $"{ei}{sn}{tf}{jp}";
        }
    }
}