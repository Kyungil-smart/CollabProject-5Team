using R3;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization; // 이름 변경 시 기존 참조 유지
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
    [FormerlySerializedAs("_workCompleteButton")] [SerializeField] Button _forceNightLoad;
    [SerializeField] private Button _nextDayButton;
    [SerializeField] private Button _openHRButton;
    [SerializeField] private Button _openProjectButton;
    [FormerlySerializedAs("_openReportButton")] [SerializeField] Button _addReputationButton;
    [SerializeField] private Button _addGoldButton;
    [SerializeField] private Button _upgradeOfficeButton;

    private HRPresenter _hrPresenter;
    private ProjectPresenter _projectPresenter;

    private void Awake()
    {
        _hrPresenter = FindFirstObjectByType<HRPresenter>();
        _projectPresenter = FindFirstObjectByType<ProjectPresenter>();
    }

    private void Start()
    {
        _forceNightLoad.OnClickAsObservable()
            .Subscribe(_ =>
            {
                DateTimeManager DTM = DateTimeManager.Instance;

                int currentWeek = DTM.currentWeek.Value;
                DTM.currentWeek.Value = currentWeek;
                DTM.currentDay = DayOfWeek.Friday;
                DTM.currentTime = TimeOfDay.Day;
                DTM.day.Value = (currentWeek - 1) * 5 + 4;

                if (Company.Instance.activeProjectCount.Value > 0)
                {
                    Project curProject = Company.Instance.curProject;
                    int targetProjectDay = (curProject.day / 5) * 5 + 4;
                    if (curProject.DurationDays > 0 && targetProjectDay >= curProject.DurationDays)
                        targetProjectDay = Mathf.Max(curProject.day, curProject.DurationDays - 1);
                    curProject.day = Mathf.Max(curProject.day, targetProjectDay);
                }

                DTM.OnClickEndDayButton().Forget(); // 바로 밤으로 사기치기
            })
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

        _addReputationButton.OnClickAsObservable()
            .Subscribe(_ => Company.Instance.reputation += 25)
            .AddTo(this);

        _addGoldButton.OnClickAsObservable()
            .Subscribe(_ => Company.Instance.gold.Value += 10000)
            .AddTo(this);
        
        _upgradeOfficeButton.OnClickAsObservable()
            .Subscribe(_ => GameManager.Instance.UpgradeOfficeAsync().Forget())
            .AddTo(this);
    }
}
