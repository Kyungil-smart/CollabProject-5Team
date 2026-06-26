using TMPro;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// MBTI 태그 프리팹 바인딩.
    /// </summary>
    public sealed class MBTITagView : MonoBehaviour, IBindable<MbtiFlags>
    {
        [SerializeField] private TextMeshProUGUI _tagLabel;

        public void Bind(MbtiFlags mbti)
        {
            _tagLabel.text = MbtiToString(mbti);
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