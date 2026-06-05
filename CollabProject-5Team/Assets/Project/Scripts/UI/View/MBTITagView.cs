using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// MBTI 태그 프리팹 바인딩.
    /// MBTI 유형별 색상은 Inspector에서 지정.
    /// </summary>
    public sealed class MBTITagView : MonoBehaviour, IBindable<MbtiFlags>
    {
        [SerializeField] private Image           _tagBG;
        [SerializeField] private TextMeshProUGUI _tagLabel;

        [Header("MBTI 계열별 색상")]
        [SerializeField] private Color _intColor;   // IN 계열
        [SerializeField] private Color _entColor;   // EN 계열
        [SerializeField] private Color _istColor;   // IS 계열
        [SerializeField] private Color _estColor;   // ES 계열

        public void Bind(MbtiFlags mbti)
        {
            _tagLabel.text = MbtiToString(mbti);
            _tagBG.color   = MbtiToColor(mbti);
        }

        private static string MbtiToString(MbtiFlags mbti)
        {
            string ei = mbti.HasFlag(MbtiFlags.E) ? "E" : "I";
            string sn = mbti.HasFlag(MbtiFlags.S) ? "S" : "N";
            string tf = mbti.HasFlag(MbtiFlags.F) ? "F" : "T";
            string jp = mbti.HasFlag(MbtiFlags.J) ? "J" : "P";
            return $"{ei}{sn}{tf}{jp}";
        }

        private Color MbtiToColor(MbtiFlags mbti)
        {
            bool isE = mbti.HasFlag(MbtiFlags.E);
            bool isS = mbti.HasFlag(MbtiFlags.S);

            return (isE, isS) switch
            {
                (false, false) => _intColor,
                (true,  false) => _entColor,
                (false, true)  => _istColor,
                (true,  true)  => _estColor,
            };
        }
    }
}