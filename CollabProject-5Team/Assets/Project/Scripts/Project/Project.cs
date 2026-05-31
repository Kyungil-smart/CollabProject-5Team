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
    public ProjectSize Scale => so.scale;
    public int RequiredCost => so.requiredCost;
    public int MaxEmployeePerPart => so.maxEmployeePerPart;
    public int DurationDays => so.durationDays;

    [Header(" 런타임 데이터 ")]
    public int day;      // 현재 진행 일수 (영업일 기준)
    public ReactiveProperty<string> userNamed = new(string.Empty); // 유저가 붙인 프로젝트 이름

    // 투입된 직원
    public Employee[] plannings;
    public Employee[] arts;
    public Employee[] programmer;

    // 진행도 연관 수치
    float _curScore;
    public float CurScore // 현재까지 진행한 주차 점수들의 평균 (0~100)
    {
        get => _curScore;
        set => _curScore = Mathf.Clamp(value, 0f, 100f);
    }
    public float qualityScore;    // 완성도 점수 (기획)
    public float stabilityScore;  // 안정성 점수 (개발)
    public float charmScore;      // 매력도 점수 (아트)
    public float ProgressDayBar => Mathf.Clamp01((float)day / DurationDays) * 100f;

    // 이벤트 발생으로 인한 수치 변화
    //public float weeklyPlanningWeight;
    //public float weeklyDevelopWeight;
    //public float weeklyArtWeight;

    // 보고서 승인 대기 목록 (Friday Night 생성, 역할별 다수)
    public List<Report> pendingReports = new();
    // 플레이어가 역할당 1개씩 선택한 보고서
    public Dictionary<Role, Report> selectedReports = new();

    // 상수
    const int FATIGUE_LESS = 5; // 밤 종료 시 모든직원 피로도 감소량

    // 완료 관련 데이터
    public int nightCount;
    public ReactiveProperty<bool> isFinished = new(false); // 프로젝트 종료 여부
    public char Grade => CurScore switch
    {
        > 90f => 'S',> 75f => 'A',> 60f => 'B',_ => 'C',
    };

    [Header("UI 표시용 데이터")]
    public string genre; public string artStyle; public string engine;

    private void Start()
    {
        userNamed.Value = Name;

        plannings = new Employee[MaxEmployeePerPart];
        programmer = new Employee[MaxEmployeePerPart];
        arts = new Employee[MaxEmployeePerPart];

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
                targetArray = programmer;
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
            Role.PROGRAMMER => programmer,
            Role.ARTIST => arts,
            _ => null,
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

        ReportPolicy.GenerateReportForRole(this, plannings);
        ReportPolicy.GenerateReportForRole(this, programmer);
        ReportPolicy.GenerateReportForRole(this, arts);

        Debug.Log($"[{userNamed.Value}] 보고서 생성 완료: {pendingReports.Count}건");
    }

    // UI에서 파트당 1개 선택 시 호출
    public void SelectReport(Report report)
    {
        selectedReports[report.role] = report;
        Debug.Log($"[{userNamed.Value}] {report.role} 보고서선택: {report.so.title}-{report.trait} ({report.grade}등급)");
    }

    // 선택된 보고서를 모두 승인하여 주차 stat 저장
    public void ApproveSelectedReports()
    {
        nightCount++;

        float qualThisNight = 0f, stabThisNight = 0f, charmThisNight = 0f;

        foreach (var kv in selectedReports)
        {
            Report report = kv.Value;

            // 이번 주 주차 stat 점수 계산 (매 주차 독립)
            float[] weekScores = ReportPolicy.CalcWeeklyStatScores(report);
            float roleAvg = (weekScores[0] + weekScores[1] + weekScores[2] + (TraitTable.Get(report.trait).score * 2)) / 3f;

            switch (report.role)
            {
                case Role.PLANNER:    qualThisNight  = roleAvg; break;
                case Role.PROGRAMMER: stabThisNight  = roleAvg; break;
                case Role.ARTIST:     charmThisNight = roleAvg; break;
            }

            // 피로도 반영
            ReportPolicy.ApplyFatigue(report.owner, report.grade);

            Debug.Log($"{report.role} [{report.so.title} / {report.grade}등급] " +
                      $"직원={report.owner.so.Name} | " +
                      $"s1={weekScores[0]:F1} s2={weekScores[1]:F1} s3={weekScores[2]:F1} 가중치={(TraitTable.Get(report.trait).score * 2)} → 평균={roleAvg:F1}");
        }

        // 주차 점수 → 누적 평균 갱신 (이전 평균에 이번 주차 값을 순차 합산)
        qualityScore   = (qualityScore   * (nightCount - 1) + qualThisNight)  / nightCount;
        stabilityScore = (stabilityScore * (nightCount - 1) + stabThisNight)  / nightCount;
        charmScore     = (charmScore     * (nightCount - 1) + charmThisNight) / nightCount;
        CurScore       = (qualityScore + stabilityScore + charmScore) / 3f;

        Debug.Log($"[{userNamed.Value}] {nightCount}주차 점수 | " +
                  $"완성도={qualThisNight:F1} 안정성={stabThisNight:F1} 매력도={charmThisNight:F1}\n" +
                  $"  누적 평균 → 완성도={qualityScore:F1} 안정성={stabilityScore:F1} 매력도={charmScore:F1} | curScore={CurScore:F1}");

        pendingReports.Clear();
        selectedReports.Clear();

        // 모든 투입 직원 피로도 감소
        foreach (var e in plannings)  e.MutableData.fatigue -= FATIGUE_LESS;
        foreach (var e in arts)       e.MutableData.fatigue -= FATIGUE_LESS;
        foreach (var e in programmer) e.MutableData.fatigue -= FATIGUE_LESS;

        if (day >= DurationDays)
            Finish();
    }
    #endregion

    // 프로젝트 종료
    public void Finish()
    {
        isFinished.Value = true;
        Debug.Log($"[{userNamed.Value}] 프로젝트 완료! ({nightCount}주차) | 등급={Grade}\n" +
                  $"  최종 → 완성도={qualityScore:F1} 안정성={stabilityScore:F1} 매력도={charmScore:F1} | 평균={CurScore:F1}");
    }
}
