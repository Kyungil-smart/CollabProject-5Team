using R3;
using UnityEngine;

// 진행중인 프로젝트의 상태 데이터
public class Project : MonoBehaviour
{
    [Header(" 초기값 데이터 ")]
    public ProjectSO so;
    public int Id => so.id;
    public string Name => so.Name;
    public string Desc => so.desc;
    public ProjectScale Scale => so.scale;
    public int RequiredCost => so.requiredCost;
    public int MaxEmployeePerPart => so.maxEmployeePerPart;
    public int DurationDays => so.durationDays;
    public float GoalScore => so.goalScore;

    [Header(" 런타임 데이터 ")]
    public int day;      // 현재 진행 일수 (영업일 기준)

    // 투입된 직원 (임시로 EmployeeSO 사용)
    public EmployeeSO[] plannings;
    public EmployeeSO[] develops;
    public EmployeeSO[] arts;

    // 진행도 연관 수치
    public ReactiveProperty<float> progress = new(0f);
    public float ProgressBar
    {
        get => Mathf.Clamp01(progress.Value / GoalScore) * 100f;
        set => progress.Value = Mathf.Clamp(value, 0f, 100f) / 100f * GoalScore;
    }
    public float qualityScore;   // 완성도 점수 (기획)
    public float stabilityScore; // 안정성 점수 (개발)
    public float charmScore;     // 매력도 점수 (아트)

    // 이벤트 발생으로 인한 수치 변화
    public int weeklyPlanningCount;
    public float weeklyPlanningWeight;
    public int weeklyDevelopCount;
    public float weeklyDevelopWeight;
    public int weeklyArtCount;
    public float weeklyArtWeight;

    // 날짜가 하루 진행될 때마다 호출되는 메서드
    public void ProgressDay()
    {
        day++;
    }

    // 금요일 밤(평일 5일 경과 후) 주 1회 호출되는 메서드
    public void ProgressNight()
    {
        // 정산

        // 보고서 이벤트 연결

        // 주간 이벤트 수치 초기화
        weeklyPlanningCount = 0;
        weeklyPlanningWeight = 0f;
        weeklyDevelopCount = 0;
        weeklyDevelopWeight = 0f;
        weeklyArtCount = 0;
        weeklyArtWeight = 0f;
    }
}
