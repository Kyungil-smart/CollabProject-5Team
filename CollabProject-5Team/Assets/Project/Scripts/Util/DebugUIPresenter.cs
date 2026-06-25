using R3;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// 테스트 전용 디버그 패널 Presenter.
/// 기존 스크립트를 수정하지 않고 UI 호출 및 날짜 조작을 우회 처리.
/// 빌드 시 DebugPanel GO를 비활성화하거나 제거하는 것으로 비활성화.
/// </summary>
public sealed class DebugUIPresenter : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] Button _forceNightLoadButton;
    [SerializeField] Button _nextDayButton;
    [SerializeField] Button _bonusQuestScoreButton;
    [SerializeField] Button _forceTalkedEmployeesButton;
    [SerializeField] Button _addReputationButton;
    [SerializeField] Button _addGoldButton;
    [SerializeField] Button _upgradeOfficeButton;

    private void Start()
    {
        _forceNightLoadButton.OnClickAsObservable()
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
            .Subscribe(_ =>
            {
                DateTimeManager.Instance.OnClickEndDayButton().Forget();
                QuestManager.Instance.ResetForNewDay();
            }).AddTo(this);

        _bonusQuestScoreButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                if (Company.Instance.activeProjectCount.Value > 0)
                {
                    Role role = (Role)Random.Range(0, 3);

                    QuestManager.Instance._weeklyBonusPoints.TryGetValue(role, out int currentPoint);
                    QuestManager.Instance._weeklyBonusPoints[role] = currentPoint + 5;

                    Debug.Log($"[Debug] 퀘스트 완료 점수 +5: {role} ({currentPoint} -> {currentPoint + 1})");
                }
            })
            .AddTo(this);

        _forceTalkedEmployeesButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                foreach (Employee employee in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
                {
                    employee.hasTalkedThisWeek = true;
                }
            })
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
