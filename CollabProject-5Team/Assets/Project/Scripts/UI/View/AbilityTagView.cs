using TMPro;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 능력치 태그 프리팹 바인딩.
    /// 텍스트 "능력치" 고정, 직군별 스프라이트 교체는 배리언트로 처리.
    /// </summary>
    public sealed class AbilityTagView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _tagLabel;

        private void Awake()
        {
            _tagLabel.text = "능력치";
        }
    }
}