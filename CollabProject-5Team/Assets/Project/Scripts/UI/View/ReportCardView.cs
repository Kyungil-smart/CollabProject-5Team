using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 작업 제안서 카드 프리팹 바인딩.
    /// Panel_ReportReview Content에 직군별로 동적 생성.
    /// </summary>
    public sealed class ReportCardView : MonoBehaviour, IBindable<Report>
    {
        [SerializeField] private Image _profileIcon;
        [SerializeField] private TextMeshProUGUI _reportTitleLabel;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _tagsLabel;
        [SerializeField] private GameObject _adoptStamp;
        [SerializeField] private GameObject _disabledOverlay;
        [SerializeField] private Button _cardButton;

        public Observable<Report> OnCardClicked { get; private set; }

        private Report _report;

        private void Awake()
        {
            _adoptStamp.SetActive(false);
            _disabledOverlay.SetActive(false);
        }

        public void Bind(Report report)
        {
            _report = report;

            var so = report.owner.so;
            _profileIcon.sprite = report.owner.MutableData.desire >= 40
                ? so.iconNormal
                : so.iconCaution;
            _reportTitleLabel.text = report.so.title;
            _nameLabel.text = so.Name;
            _tagsLabel.text = $"#{TraitTable.Get(report.owner.so.mainTrait).displayName}";

            OnCardClicked = _cardButton.OnClickAsObservable()
                .Select(_ => _report);
        }

        public void SetAdopted(bool adopted)
        {
            _adoptStamp.SetActive(adopted);
        }

        public void SetDisabled(bool disabled)
        {
            _disabledOverlay.SetActive(disabled);
            _cardButton.interactable = !disabled;
        }
    }
}