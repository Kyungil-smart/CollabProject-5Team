using R3;
using System;
using System.Collections.Generic;
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
    public Employee[] plannings;
    public Employee[] develops;
    public Employee[] arts;

    // 진행도 연관 수치
    public ReactiveProperty<float> progress = new(0f);
    public float ProgressDayBar => Mathf.Clamp01((float)day / DurationDays) * 100f; // 메인 진행바로 사용
    public float ProgressBar => Mathf.Clamp01(progress.Value / GoalScore) * 100f; // 최고 점수 기준 진행도인데 기획의도는 날짜기준

    // 세부 점수 (각 파트가 올리면 progress에 자동 반영)
    public ReactiveProperty<float> qualityScore = new(0f); // 완성도 점수 (기획)
    public ReactiveProperty<float> stabilityScore = new(0f); // 안정성 점수 (개발)
    public ReactiveProperty<float> charmScore = new(0f); // 매력도 점수 (아트)

    // 이벤트 발생으로 인한 수치 변화
    //public float weeklyPlanningWeight;
    //public float weeklyDevelopWeight;
    //public float weeklyArtWeight;

    public ReactiveProperty<bool> isFinished = new(false); // 프로젝트 종료 여부

    // 보고서 승인 대기 목록 (Friday Night 생성, 역할별 다수)
    public List<Report> pendingReports = new();
    // 플레이어가 역할당 1개씩 선택한 보고서
    public Dictionary<Role, Report> selectedReports = new();

    // 주차별 stat 점수 누적 (Key=TraitStat, Value=주차별 점수 리스트)
    public Dictionary<TraitStat, List<float>> weeklyStatScores = new();
    // 완료된 주차 수
    int _nightCount;


    private void Start()
    {
        userNamed.Value = Name;

        plannings = new Employee[MaxEmployeePerPart];
        develops  = new Employee[MaxEmployeePerPart];
        arts      = new Employee[MaxEmployeePerPart];

        // 세부 점수가 변경될 때마다 progress 자동 재계산
        Observable.CombineLatest(qualityScore, stabilityScore, charmScore,
            (q, s, c) => q + s + c)
            .Subscribe(total => progress.Value = total)
            .AddTo(this);
    }

    public bool HireEmployee(Employee e)
    {
        if (e == null)
        {
            Debug.Log("[Project] 고용할 직원이 null 입니다");
            return false;
        }

        Employee[] targetArray = null;
        switch (e.so.role)
        {
            case Role.PLANNER:
                targetArray = plannings;
                break;
            case Role.PROGRAMMER:
                targetArray = develops;
                break;
            case Role.ARTIST:
                targetArray = arts;
                break;
            default:
                Debug.LogWarning($"[{userNamed.Value}] {e.so.Name}의 파트({e.so.role})고용은 구현되지 않았습니다.");
                return false;
        }

        int emptyIndex = Array.FindIndex(targetArray, m => m == null);
        if (emptyIndex < 0)
        {
            Debug.LogWarning($"[{userNamed.Value}] {e.so.role} 파트 투입 슬롯이 가득 찼습니다.");
            return false;
        }

        targetArray[emptyIndex] = e;
        Debug.Log($"[{userNamed.Value}] {e.so.Name} 직원이 {e.so.role} 파트로 투입되었습니다.");
        return true;
    }

    // 프로젝트에서 직원을 제거하고 해고 처리
    public bool FireEmployee(Employee e)
    {
        if (e == null)
        {
            Debug.LogWarning("[Project] 해고할 직원이 null입니다.");
            return false;
        }

        Employee[] targetArray = e.so.role switch
        {
            Role.PLANNER => plannings,
            Role.PROGRAMMER  => develops,
            Role.ARTIST      => arts,
            _             => null,
        };

        if (targetArray == null)
        {
            Debug.LogWarning($"[{userNamed.Value}] {e.so.Name}의 파트({e.so.role})해고는 구현되지 않았습니다.");
            return false;
        }

        int index = Array.IndexOf(targetArray, e);
        if (index < 0)
        {
            Debug.LogWarning($"[{userNamed.Value}] {e.so.Name}은 이 프로젝트에 투입되어 있지 않습니다.");
            return false;
        }

        targetArray[index] = null;
        Debug.Log($"[{userNamed.Value}] {e.so.Name} 직원이 {e.so.role} 파트에서 제거되었습니다.");

        _EmployeeManager.Instance.FireEmployee(e);
        return true;
    }

    // 날짜가 하루 진행될 때마다 호출되는 메서드
    public void ProgressDay()
    {
        if (isFinished.Value) return;

        Debug.Log($"{userNamed}: [Day {day}] {Company.GetDateString(day)}종료"); // 날짜 로그 표시중
        day++;
    }

    // 금요일 밤(평일 5일 경과 후) 주 1회 호출되는 메서드
    public void ProgressNight()
    {
        Debug.Log($"{userNamed}: 밤 이벤트 발생!");
        // 주간 이벤트 정산 및 초기화

        // 보고서 산출
        GenerateReportDrafts();
    }

    #region 보고서 부분
    // 투입된 직원 데이터를 기반으로 보고서 생성 
    public void GenerateReportDrafts()
    {
        pendingReports.Clear();
        selectedReports.Clear();

        GenerateDraftsForPart(plannings);
        GenerateDraftsForPart(develops);
        GenerateDraftsForPart(arts);

        Debug.Log($"[{userNamed.Value}] 보고서 생성 완료: {pendingReports.Count}건");
    }
    void GenerateDraftsForPart(Employee[] employees)
    {
        foreach (Employee e in employees)
        {
            if (e == null) continue;

            float score = ReportPolicy.CalcScore(e.so, e.MutableData.desire);
            int grade = ReportPolicy.CalcGrade(score);
            ReportSO so = ReportManager.Instance.GetRandomReport(e.so.role, grade);

            if (so == null) { Debug.Log("[Project] 해당하는 보고서 SO 없음"); continue; }

            pendingReports.Add(new Report { so = so, owner = e, score = score });
        }
    }

    // UI에서 파트당 1개 선택 시 호출
    public void SelectReport(Report report)
    {
        selectedReports[report.role] = report;
        Debug.Log($"[{userNamed.Value}] {report.role} 보고서 선택: {report.so.title} ({report.grade}등급)");
    }

    // 선택된 보고서를 모두 승인하여 주차 stat 저장
    public void ApproveSelectedReports()
    {
        _nightCount++;

        foreach (var kv in selectedReports)
        {
            Report report = kv.Value;

            // 이번 주 주차 stat 점수 계산 (매 주차 독립, A값 없음)
            float[] weekScores = ReportPolicy.CalcWeeklyStatScores(report);
            TraitStat[] stats  = ReportPolicy.GetRoleStats(report.role);

            for (int i = 0; i < stats.Length; i++)
            {
                if (!weeklyStatScores.ContainsKey(stats[i]))
                    weeklyStatScores[stats[i]] = new List<float>();
                weeklyStatScores[stats[i]].Add(weekScores[i]);
            }

            // 피로도 반영
            ReportPolicy.ApplyFatigue(report.owner, report.grade);

            Debug.Log($"{report.role} 보고서 승인: ({report.so.title} / {report.grade}등급) | " +
                      $"직원 [{report.owner.so.Name}] " +
                      $"s1={weekScores[0]:F1} s2={weekScores[1]:F1} s3={weekScores[2]:F1} 피로={report.owner.MutableData.fatigue}");
        }

        pendingReports.Clear();
        selectedReports.Clear();

        if (day >= DurationDays)
            Finish();
    }
    #endregion

    // 프로젝트 종료: 주차 stat 평균 → 파트별 점수 → 최종 progress 반영
    public void Finish()
    {
        if (_nightCount == 0) { isFinished.Value = true; return; }

        // stat 평균 계산
        float StatAvg(TraitStat s)
        {
            if (!weeklyStatScores.TryGetValue(s, out var list) || list.Count == 0) return 0f;
            float sum = 0f;
            foreach (float v in list) sum += v;
            return sum / list.Count;
        }

        // 파트별 점수: (세 항목 평균)
        qualityScore.Value   = (StatAvg(TraitStat.Fun) + StatAvg(TraitStat.Creativity) + StatAvg(TraitStat.Precision))   / 3f;
        stabilityScore.Value = (StatAvg(TraitStat.TechPower) + StatAvg(TraitStat.Optimize) + StatAvg(TraitStat.BugControl)) / 3f;
        charmScore.Value     = (StatAvg(TraitStat.Visual) + StatAvg(TraitStat.Direction) + StatAvg(TraitStat.Composition))  / 3f;

        isFinished.Value = true;
        Debug.Log($"{userNamed.Value}: 종료 | 완성도={qualityScore.Value:F1} 안정성={stabilityScore.Value:F1} 매력도={charmScore.Value:F1} | 최종점수={progress.Value:F1}");
    }
}
