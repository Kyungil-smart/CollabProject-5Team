using R3;
using UnityEngine;
using UnityEngine.UI;
using GameDevTycoon.UI.Ingame;
using Cysharp.Threading.Tasks;

/// <summary>
/// 테스트 전용 디버그 패널 Presenter.
/// 기존 스크립트를 수정하지 않고 UI 호출 및 날짜 조작을 우회 처리.
/// 빌드 시 DebugPanel GO를 비활성화하거나 제거하는 것으로 비활성화.
/// </summary>
public sealed class DebugUIPresenter : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button _workCompleteButton;
    [SerializeField] private Button _nextDayButton;
    [SerializeField] private Button _openHRButton;
    [SerializeField] private Button _openProjectButton;
    [SerializeField] private Button _openReportButton;
    [SerializeField] private Button _addGoldButton;
    [SerializeField] private Button _upgradeOfficeButton;

    // ReportPresenter.OnNightStarted()는 private이므로 이벤트를 통해 우회
    private HRPresenter _hrPresenter;
    private ProjectPresenter _projectPresenter;
    private ReportPresenter _reportPresenter;

    private void Awake()
    {
        _hrPresenter = FindFirstObjectByType<HRPresenter>();
        _projectPresenter = FindFirstObjectByType<ProjectPresenter>();
        _reportPresenter = FindFirstObjectByType<ReportPresenter>();
    }

    private void Start()
    {
        _workCompleteButton.OnClickAsObservable()
            .Subscribe(_ => OnWorkCompleteClicked())
            .AddTo(this);

        _nextDayButton.OnClickAsObservable()
            .Subscribe(_ => DateTimeManager.Instance.OnClickEndDayButton().Forget())
            .AddTo(this);

        _openHRButton.OnClickAsObservable()
            .Subscribe(_ => _hrPresenter?.Show())
            .AddTo(this);

        _openProjectButton.OnClickAsObservable()
            .Subscribe(_ => _projectPresenter?.Show())
            .AddTo(this);

        _openReportButton.OnClickAsObservable()
            .Subscribe(_ => TriggerReportOpen())
            .AddTo(this);

        _addGoldButton.OnClickAsObservable()
            .Subscribe(_ => Company.Instance.gold.Value += 10000)
            .AddTo(this);
        
        _upgradeOfficeButton.OnClickAsObservable()
            .Subscribe(_ => GameManager.Instance?.UpgradeOfficeAsync().Forget())
            .AddTo(this);
    }

    private void OnWorkCompleteClicked()
    {
        // 퀘스트 완료 조건 없이 isWorkCompleted 플래그만 강제 세팅
        // DateTimeManager.CompleteDayWork()는 플레이어 애니메이션·OnWorkCompleted 이벤트를 같이 호출하므로 플래그만 직접 세팅
        DateTimeManager.Instance.isWorkCompleted = true;
    }

    private void TriggerReportOpen()
    {
        // ReportPresenter.OnNightStarted()는 private이므로
        // OnNight 이벤트를 발행해 정상 흐름과 동일하게 진입
        // 단, 금요일 밤 상태가 아니면 보고서 데이터(pendingReports)가 없어 빈 화면으로 열릴 수 있음
        //_reportPresenter?.OpenForDebug();
    }
}
