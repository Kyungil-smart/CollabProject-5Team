using TMPro;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직원 특성 태그 프리팹 바인딩.
    /// EmployeeDetailView, ApplicantDetailView 앵커에 동적 생성.
    /// </summary>
    public sealed class TraitTagView : MonoBehaviour, IBindable<Trait>
    {
        [SerializeField] private TextMeshProUGUI _tagLabel;

        public void Bind(Trait trait)
        {
            TraitData data = TraitTable.Get(trait);
            _tagLabel.text = $"#{data.displayName}";
            _tagLabel.color = data.DisplayColor;
        }
    }
}