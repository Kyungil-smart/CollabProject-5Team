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
        // day가 변경될 때마다 SetTimeLabel 호출
        data.day.Subscribe(d =>
        {
            // [TODO: DateTimeManager year/month 데이터 확정 후 형식 변경]
            string dayName = DateTimeManager.Instance.GetDayName();
            _view.SetTimeLabel($"{data.currentWeek.Value}주 {dayName}"); // 날짜
            _view.SetMoneyLabel(Company.Instance.gold); // 골드
        }).AddTo(this);
        // week 변경시
        data.currentWeek.Subscribe(d =>
        {
            // [TODO: DateTimeManager year/month 데이터 확정 후 형식 변경]
            _view.SetTimeLabel($"{data.currentWeek.Value}주 월요일"); // 날짜
            _view.SetMoneyLabel(Company.Instance.gold); // 골드
        }).AddTo(this);

        // 퇴근 버튼 클릭 시 다음 날짜로 진행
        //_view.OnWorkStartClicked
        //    .Subscribe(_ => data.OnClickEndDayButton())
        //    .AddTo(this);

        // 밤 종료 버튼 클릭시
        //_view.OnNightQuitClicked
        //    .Subscribe(_ =>
        //    {
        //        data.OnClickEndDayButton();
        //        _view.SwitchToDay();
        //    }).AddTo(this);
    }

    public void SwitchToDay()
    {
        _view.SwitchToDay();
        if (CanvasDayBottom != null)
            CanvasDayBottom.SetActive(true);
    }

    public void SwitchToNight()
    {
        _view._dayUI.SetActive(false);
        //if (CanvasDayBottom != null)
        //    CanvasDayBottom.SetActive(false);
        ShowLoadingScreen();
    }

    private void Start()
    {
        Bind(DateTimeManager.Instance);

        DateTimeManager.OnDay += SwitchToDay;
        DateTimeManager.OnNightLoading += SwitchToNight;

        SwitchToDay(); // 씬 시작 시 낮 상태로 초기화 (OnGameSceneLoad 에서 처리)
    }

    private void OnDestroy()
    {
        DateTimeManager.OnDay -= SwitchToDay;
        DateTimeManager.OnNightLoading -= SwitchToNight;
    }

    async void ShowLoadingScreen()
    {
        if (CanvasLoading != null)
        {
            CanvasLoading.SetActive(true);
            await UniTask.Delay(565, cancellationToken: destroyCancellationToken); // 추후 로딩 전환 효과도 넣고...?
            CanvasLoading.SetActive(false);
        }
    }
}