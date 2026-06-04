using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직원 특성 태그 프리팹 바인딩.
    /// EmployeeDetailView.TraitGroup, ApplicantDetailView.TraitGroup에 3개 고정 배치.
    /// </summary>
    public sealed class TraitTagView : MonoBehaviour, IBindable<Trait>
    {
        [SerializeField] private Image           _tagBG;
        [SerializeField] private TextMeshProUGUI _tagLabel;

        public void Bind(Trait trait)
        {
            TraitData data = TraitTable.Get(trait);
            _tagLabel.text = $"#{data.displayName}";
            _tagBG.color   = data.DisplayColor;
        }
    }
}