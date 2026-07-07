using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_NightBackground Presenter.
    /// DateTimeManager의 낮/밤 전환 이벤트를 구독해 NightBackgroundView에 반영.
    /// </summary>
    public sealed class NightBackgroundPresenter : MonoBehaviour
    {
        [SerializeField] private NightBackgroundView _view;

        private void Start()
        {
            DateTimeManager.OnDay += _view.Hide;
            DateTimeManager.OnReportEnd += _view.Show;
        }

        private void OnDestroy()
        {
            DateTimeManager.OnDay -= _view.Hide;
            DateTimeManager.OnReportEnd -= _view.Show;
        }
    }
}