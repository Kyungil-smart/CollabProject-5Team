using R3;
using System;
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

    // 투입된 직원
    public EmployeeObj[] plannings;
    public EmployeeObj[] develops;
    public EmployeeObj[] arts;

    // 진행도 연관 수치
    public ReactiveProperty<float> progress = new(0f);
    public float ProgressBar
    {
        get => Mathf.Clamp01(progress.Value / GoalScore) * 100f;
        set => progress.Value = Mathf.Clamp(value, 0f, 100f) / 100f * GoalScore; // 바%를 직접 늘릴수도 있음(추후 확장성)
    }

    // 세부 점수 (각 파트가 올리면 progress에 자동 반영)
    public ReactiveProperty<float> qualityScore = new(0f); // 완성도 점수 (기획)
    public ReactiveProperty<float> stabilityScore = new(0f); // 안정성 점수 (개발)
    public ReactiveProperty<float> charmScore = new(0f); // 매력도 점수 (아트)

    // 이벤트 발생으로 인한 수치 변화
    //public float weeklyPlanningWeight;
    //public float weeklyDevelopWeight;
    //public float weeklyArtWeight;

    public ReactiveProperty<bool> isFinished = new(false); // 프로젝트 종료 여부

    private void Start()
    {
        userNamed.Value = Name;

        plannings = new EmployeeObj[MaxEmployeePerPart];
        develops  = new EmployeeObj[MaxEmployeePerPart];
        arts      = new EmployeeObj[MaxEmployeePerPart];

        // 세부 점수가 변경될 때마다 progress 자동 재계산
        Observable.CombineLatest(qualityScore, stabilityScore, charmScore,
            (q, s, c) => q + s + c)
            .Subscribe(total => progress.Value = total)
            .AddTo(this);
    }

    public bool AssignEmployee(EmployeeObj o)
    {
        if (o == null)
        {
            Debug.Log("[Project] 고용할 직원이 null 입니다");
            return false;
        }

        EmployeeObj[] targetArray = null;
        switch (o.e.ImmutableData.partParsed)
        {
            case Part.Planning:
                targetArray = plannings;
                break;
            case Part.Develop:
                targetArray = develops;
                break;
            case Part.Art:
                targetArray = arts;
                break;
            default:
                Debug.LogWarning($"[{userNamed.Value}] {o.e.ImmutableData.employeeName}의 파트({o.e.ImmutableData.partParsed})는 현재 프로젝트 투입 대상이 아닙니다.");
                return false;
        }

        int emptyIndex = Array.FindIndex(targetArray, m => m == null);
        if (emptyIndex < 0)
        {
            Debug.LogWarning($"[{userNamed.Value}] {o.e.ImmutableData.partParsed} 파트 투입 슬롯이 가득 찼습니다.");
            return false;
        }

        targetArray[emptyIndex] = o;
        Debug.Log($"[{userNamed.Value}] {o.e.ImmutableData.employeeName} 직원이 {o.e.ImmutableData.partParsed} 파트로 투입되었습니다.");
        return true;
    }

    // 날짜가 하루 진행될 때마다 호출되는 메서드
    public void ProgressDay()
    {
        if (isFinished.Value) return;

        // ~임의로 계산중~
        //기획자: qualityScore 증가
        foreach (EmployeeObj o in plannings)
        {
            if (o == null) continue;
            qualityScore.Value += (o.e.MutableData.property1 + o.e.MutableData.property2 + o.e.MutableData.property3) / 3f * 0.1f;
        }

        // 개발자: stabilityScore 증가
        foreach (EmployeeObj o in develops)
        {
            if (o == null) continue;
            stabilityScore.Value += (o.e.MutableData.property1 + o.e.MutableData.property2 + o.e.MutableData.property3) / 3f * 0.1f;
        }

        // 아티스트: charmScore 증가
        foreach (EmployeeObj o in arts)
        {
            if (o == null) continue;
            charmScore.Value += (o.e.MutableData.property1 + o.e.MutableData.property2 + o.e.MutableData.property3) / 3f * 0.1f;
        }

        Debug.Log($"{userNamed}: [Day {day}] {Company.GetWeekDayName(day)}종료"); // 날짜 로그 표시중
        day++;
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

    // 프로젝트 종료
    public void Finish()
    {
        isFinished.Value = true;
        Debug.Log($"{userNamed}: 종료 | 진행도: {ProgressBar:F1}% | 소요일: {day}일");

        // 종료 프로젝트 데이터와 연결
    }
}
