using GameDevTycoon.UI;
using GameDevTycoon.UI.Ingame;
using R3;
using UnityEngine;

public class HUDBinder : MonoBehaviour, IBindable<DateTimeManager>
{
    HUDView _view;

    public void Bind(DateTimeManager data)
    {
        // day가 변경될 때마다 SetTimeLabel 호출
        data.day.Subscribe(d =>
        {
            int week = data.currentWeek.Value;
            string dayName = DateTimeManager.GetWeekDayName(d);
            _view.SetTimeLabel(week, dayName, false); // 날짜
            _view.SetMoneyLabel(Company.Instance.gold); // 골드
            _view.SetReputationLabel(Company.Instance.reputation); // 평판
        }).AddTo(this);
    }

    private void Awake()
    {
        _view = GetComponent<HUDView>();
    }

    private void Start()
    {
        Bind(DateTimeManager.Instance);
    }
}
