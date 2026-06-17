using R3;
using System;
using System.Collections.Generic;
using UnityEngine;

// 진행중인 프로젝트의 상태 데이터
public class Project : MonoBehaviour
{
    [Header(" 초기값 데이터 ")]
    public ProjectSO so;
    public string Name => so.Name;
    public ProjectSize Scale => so.scale;
    public int RequiredCost => so.requiredCost;
    public int MaxEmployeePerPart => so.maxEmployeePerPart;
    public int DurationDays => so.durationDays;

    [Header(" 런타임 데이터 ")]
    public int day;      // 현재 진행 일수 (영업일 기준)
    public ReactiveProperty<string> userNamed = new("a"); // 유저가 붙인 프로젝트 이름

    // 투입된 직원
    public List<Employee> plannings = new();
    public List<Employee> arts = new();
    public List<Employee> programmer = new();
    public List<Employee> GetAllEmployees()
    {
        var result = new List<Employee>();
        result.AddRange(plannings);
        result.AddRange(programmer);
        result.AddRange(arts);
        return result;
    }

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

    // 보고서 승인 대기 목록 (Friday Night 생성, 역할별 다수)
    public List<Report> pendingReports = new();
    // 플레이어가 역할당 1개씩 선택한 보고서
    public Dictionary<Role, Report> selectedReports = new();

    // 완료 관련 데이터
    public int nightCount;
    public ReactiveProperty<bool> isFinished = new(false); // 프로젝트 종료 여부
    public char Grade => CurScore switch
    {
        > 90f => 'S',
        > 75f => 'A',
        > 60f => 'B',
        _ => 'C',
    };

    [Header("UI 표시용 데이터")]
    public string genre; public string artStyle; public string engine;

    // 보고서 생성완료시 true
    public bool isReportDraftsReady;

    bool _isRuntimeInitialized;
    public void InitializeRuntime(string projectName)
    {
        if (_isRuntimeInitialized) return;
        _isRuntimeInitialized = true;

        userNamed.Value = projectName;
        plannings = new();
        programmer = new();
        arts = new();
        day = 0;
        nightCount = 0;
        qualityScore = 0f;
        stabilityScore = 0f;
        charmScore = 0f;
        CurScore = 0f;
        isFinished.Value = false;
        pendingReports.Clear();
        selectedReports.Clear();
        isReportDraftsReady = false;
    }

    public bool HireEmployee(Employee e)
    {
        List<Employee> targetList = null;
        switch (e.so.role)
        {
            case Role.PLANNER:
                targetList = plannings;
                break;
            case Role.PROGRAMMER:
                targetList = programmer;
                break;
            case Role.ARTIST:
                targetList = arts;
                break;
            default:
                Debug.LogWarning($"[{userNamed.Value}] {e.so.Name}의 파트({e.so.role})고용은 구현되지 않았습니다.");
                return false;
        }

        if (targetList.Contains(e))
            return true;

        _EmployeeManager.Instance.MarkProjectEmployee(e);

        targetList.Add(e);
        Debug.Log($"[{userNamed.Value}] {e.so.Name} 직원이 {e.so.role} 파트로 투입되었습니다.");
        return true;
    }

    // 프로젝트에서 직원을 제거
    public void RemoveEmployee(Employee e)
    {
        List<Employee> targetList = e.so.role switch
        {
            Role.PLANNER => plannings,
            Role.PROGRAMMER => programmer,
            Role.ARTIST => arts,
            _ => null,
        };

        targetList.Remove(e);
        Debug.Log($"[{userNamed.Value}] {e.so.Name} 직원이 {e.so.role} 파트에서 제거되었습니다.");
    }

    // 날짜가 하루 진행될 때마다 호출되는 메서드
    public void ProgressDay()
    {
        if (isFinished.Value) return;

        Debug.Log($"{userNamed}: [Day {day}] {DateTimeManager.GetDateString(day)}종료"); // 날짜 로그 표시중
        day++;
    }

    // 금요일 밤(평일 5일 경과 후) 주 1회 호출되는 메서드
    public void ProgressNight()
    {
        // 주간 정산
        foreach (var e in GetAllEmployees())
        {
            e.SaveCurrentData();
        }

        // 마지막 목표날이 아니라면 보고서 산출
        if (day < DurationDays) GenerateReportDrafts();
        else Finish();
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

        isReportDraftsReady = true;
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

        var acceptedReports = new HashSet<Report>(selectedReports.Values);

        foreach (var kv in selectedReports)
        {
            Report report = kv.Value;

            // 이번 주 주차 stat 점수 계산 (매 주차 독립)
            float[] weekScores = ReportPolicy.CalcWeeklyStatScores(report);
            float roleAvg = (weekScores[0] + weekScores[1] + weekScores[2] + (TraitTable.Get(report.trait).score * 2)) / 3f;

            switch (report.role)
            {
                case Role.PLANNER:
                    qualThisNight = roleAvg;
                    if (nightCount == 1) genre = report.so.uiCategory;
                    break;
                case Role.ARTIST:
                    charmThisNight = roleAvg;
                    if (nightCount == 1) artStyle = report.so.uiCategory;
                    break;
                case Role.PROGRAMMER:
                    stabThisNight = roleAvg;
                    if (nightCount == 1) engine = report.so.uiCategory;
                    break;
            }

            // 피로도 반영
            ReportPolicy.ApplyHighFatigueSelectionPenalty(report.owner);
            ReportPolicy.ApplyAcceptedFatigue(report.owner, report.grade);
#if UNITY_EDITOR
            Debug.Log($"{report.role} [{report.so.title} / {report.grade}등급] " +
                      $"직원={report.owner.so.Name} | " +
                      $"s1={weekScores[0]:F1} s2={weekScores[1]:F1} s3={weekScores[2]:F1} 가중치={(TraitTable.Get(report.trait).score * 2)} → 평균={roleAvg:F1}");
#endif
        }

        // 평일 일일 퀘스트 클리어 누적 포인트를 소급 적용
        qualThisNight = qualThisNight + QuestManager.Instance.GetWeeklyBonus(Role.PLANNER);
        stabThisNight = stabThisNight + QuestManager.Instance.GetWeeklyBonus(Role.PROGRAMMER);
        charmThisNight = charmThisNight + QuestManager.Instance.GetWeeklyBonus(Role.ARTIST);
        QuestManager.Instance.ResetWeeklyBonus();

        // 주차 점수 → 누적 평균 갱신 (이전 평균에 이번 주차 값을 순차 합산)
        qualityScore = (qualityScore * (nightCount - 1) + qualThisNight) / nightCount;
        stabilityScore = (stabilityScore * (nightCount - 1) + stabThisNight) / nightCount;
        charmScore = (charmScore * (nightCount - 1) + charmThisNight) / nightCount;
        CurScore = (qualityScore + stabilityScore + charmScore) / 3f;
#if UNITY_EDITOR
        Debug.Log($"<color=green>[{userNamed.Value}] {nightCount}주차 점수 | " +
                  $"완성도={qualThisNight:F1} 안정성={stabThisNight:F1} 매력도={charmThisNight:F1}\n" +
                  $"  누적 평균 → 완성도={qualityScore:F1} 안정성={stabilityScore:F1} 매력도={charmScore:F1} | curScore={CurScore:F1}</color>");
#endif
        foreach (var report in pendingReports)
        {
            if (!acceptedReports.Contains(report))
                ReportPolicy.ApplyRejectedFatigue(report.owner);
        }

        pendingReports.Clear();
        selectedReports.Clear();
        isReportDraftsReady = false;
    }
    #endregion

    // 프로젝트 종료
    public void Finish()
    {
        isFinished.Value = true;
#if UNITY_EDITOR
        Debug.Log($"[{userNamed.Value}] 프로젝트 완료! ({nightCount}주차) | 등급={Grade}\n" +
                  $"  최종 → 완성도={qualityScore:F1} 안정성={stabilityScore:F1} 매력도={charmScore:F1} | 평균={CurScore:F1}");
#endif
        Company.Instance.CompleteProject(this);
    }

    #region 세이브/로드
    public void ExportProjectData(SaveData data)
    {
        // so 대신에 프로젝트 존재 여부와 규모를 확인

        data.activeProjectsData.project_day = day;
        data.activeProjectsData.project_userNamed = userNamed.Value;

        data.activeProjectsData.project_PlanningEmployeeIds = ConvertEmployeeListToIdList(plannings);
        data.activeProjectsData.project_ProgrammerEmployeeIds = ConvertEmployeeListToIdList(programmer);
        data.activeProjectsData.project_ArtistEmployeeIds = ConvertEmployeeListToIdList(arts);

        data.activeProjectsData.project_QualityScore = qualityScore;
        data.activeProjectsData.project_StabilityScore = stabilityScore;
        data.activeProjectsData.project_CharmScore = charmScore;
        data.activeProjectsData.project_CurScore = CurScore;
    }
    public void ImportProjectData(SaveData data)
    {
        // so 대신에 프로젝트 존재 여부와 규모를 확인

        this.day = data.activeProjectsData.project_day;

        this.userNamed.Value = data.activeProjectsData.project_userNamed;

        this.qualityScore = data.activeProjectsData.project_QualityScore;
        this.stabilityScore = data.activeProjectsData.project_StabilityScore;
        this.charmScore = data.activeProjectsData.project_CharmScore;
        this.CurScore = data.activeProjectsData.project_CurScore;

        RestoreEmployeeList(data.activeProjectsData.project_PlanningEmployeeIds, plannings);
        RestoreEmployeeList(data.activeProjectsData.project_ProgrammerEmployeeIds, programmer);
        RestoreEmployeeList(data.activeProjectsData.project_ArtistEmployeeIds, arts);
    }
    private List<int> ConvertEmployeeListToIdList(List<Employee> employees)
    {
        var list = new List<int>();

        foreach (var emp in employees)
        {
            list.Add(emp.so.id);
        }
        return list;
    }
    private void RestoreEmployeeList(List<int> ids, List<Employee> targetList)
    {
        targetList.Clear();
        var hiredList = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;

        foreach (int empId in ids)
        {
            if (empId == -1) continue;

            Employee employee = hiredList.Find(e => e.so.id == empId);
            if (employee == null) continue;

            targetList.Add(employee);
            _EmployeeManager.Instance.MarkProjectEmployee(employee);
        }
    }
    #endregion
}
