using Cysharp.Threading.Tasks;
using GameDevTycoon.UI;
using GameDevTycoon.UI.Ingame;
using R3;
using UnityEngine;

// HUD 데이터 바인딩 및 낮/밤 전환
public class HUDBinder : MonoBehaviour, IBindable<DateTimeManager>
{
    HUDView _view;
    private void Awake() => _view = GetComponent<HUDView>();

    [SerializeField] GameObject CanvasLoading;
    [SerializeField] GameObject CanvasDayBottom;

    public void Bind(DateTimeManager data)
    {
        // day가 변경시
        data.day.Subscribe(d =>
        {
            string dayName = DateTimeManager.Instance.GetDayName();
            _view.SetTimeLabel($"{data.currentWeek.Value}주 {dayName}"); // 날짜
        }).AddTo(this);
        // week 변경시
        data.currentWeek.Subscribe(d =>
        {
            _view.SetTimeLabel($"{data.currentWeek.Value}주 월요일"); // 날짜
        }).AddTo(this);

        Company.Instance.gold
            .Subscribe(gold => _view.SetMoneyLabel(gold)) // 골드
            .AddTo(this);
    }

    public void SwitchToDay()
    {
        _view.SwitchToDay();
        CanvasDayBottom.SetActive(true);
    }

    public void SwitchToNight()
    {
        _view._dayUI.SetActive(false);
        ShowLoadingScreen();
    }

    private void Start()
    {
        Bind(DateTimeManager.Instance);

        DateTimeManager.OnDay += SwitchToDay;
        DateTimeManager.OnNightLoading += SwitchToNight;

        SwitchToDay(); // 씬 시작 시 낮 상태로 초기화 (OnGameSceneLoad?)
    }

    private void OnDestroy()
    {
        DateTimeManager.OnDay -= SwitchToDay;
        DateTimeManager.OnNightLoading -= SwitchToNight;
    }

    async void ShowLoadingScreen()
    {
        CanvasLoading.SetActive(true);
        await UniTask.Delay(565, cancellationToken: destroyCancellationToken); // 추후 로딩 전환 효과도 넣고...?
        CanvasLoading.SetActive(false);
    }
}
