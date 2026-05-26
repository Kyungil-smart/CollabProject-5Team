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
    public ReactiveProperty<string> userNamed = new(string.Empty); // 유저가 붙인 프로젝트 이름

    // 투입된 직원 (임시로 EmployeeSO 사용)
    public EmployeeSO[] plannings;
    public EmployeeSO[] develops;
    public EmployeeSO[] arts;

    // 진행도 연관 수치
    public ReactiveProperty<float> progress = new(0f);
    public float ProgressBar
    {
        get => Mathf.Clamp01(progress.Value / GoalScore) * 100f;
        set => progress.Value = Mathf.Clamp(value, 0f, 100f) / 100f * GoalScore; // 바%를 직접 늘릴수도 있음(추후 확장성)
    }
    public float qualityScore;   // 완성도 점수 (기획)
    public float stabilityScore; // 안정성 점수 (개발)
    public float charmScore;     // 매력도 점수 (아트)

    // 이벤트 발생으로 인한 수치 변화
    //public float weeklyPlanningWeight;
    //public float weeklyDevelopWeight;
    //public float weeklyArtWeight;

    public ReactiveProperty<bool> isFinished = new(false); // 프로젝트 종료 여부

    private void Start()
    {
        userNamed.Value = Name; // 초기값으로 SO의 Name 사용
    }

    // 날짜가 하루 진행될 때마다 호출되는 메서드
    public void ProgressDay()
    {
        if (isFinished.Value) return;


        // 직원의 스텟에 따라 진행도 증가
        foreach (EmployeeSO so in plannings)
        {
            float contribution = (so.property1 + so.property2 + so.property3) / 3f; // 임시 계산식
            progress.Value += contribution * 0.1f; // 임시 가중치
        }
        foreach (EmployeeSO so in develops)
        {
            float contribution = (so.property1 + so.property2 + so.property3) / 3f; // 임시 계산식
            progress.Value += contribution * 0.1f; // 임시 가중치
        }
        foreach(EmployeeSO so in plannings)
        {
            float contribution = (so.property1 + so.property2 + so.property3) / 3f; // 임시 계산식
            progress.Value += contribution * 0.1f; // 임시 가중치
        }

        Debug.Log($"{userNamed}: [Day {day}] {Company.GetWeekDayName(day)}종료"); // 날짜 로그 표시중
        day++;
    }

    // 프로젝트 종료
    public void Finish()
    {
        isFinished.Value = true;
        Debug.Log($"{userNamed}: 종료 | 진행도: {ProgressBar:F1}% | 소요일: {day}일");
    }

    // 금요일 밤(평일 5일 경과 후) 주 1회 호출되는 메서드
    public void ProgressNight()
    {
        Debug.Log($"{userNamed}: 밤 이벤트 발생!");
        // 주간 이벤트 정산 및 초기화

        if (day >= DurationDays)
        {
            Finish(); // 기간이 되면 프로젝트 종료
        }

        // 정산 알림
    }
}
