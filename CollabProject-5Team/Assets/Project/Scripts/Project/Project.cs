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
    public ReactiveProperty<string> userNamed = new("a"); // 유저가 붙인 프로젝트 이름

    // 투입된 직원
    public Employee[] plannings;
    public Employee[] arts;
    public Employee[] programmer;
    public List<Employee> GetAllEmployees()
    {
        var result = new List<Employee>();
        foreach (var arr in new[] { plannings, programmer, arts })
            foreach (var e in arr)
                if (e != null) result.Add(e);
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

    // 이벤트 발생으로 인한 수치 변화
    //public float weeklyPlanningWeight;
    //public float weeklyDevelopWeight;
    //public float weeklyArtWeight;

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
        EnsureInitialized();
        userNamed.Value = projectName;
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
    private void EnsureInitialized()
    {
        if (_isRuntimeInitialized) return;

        userNamed.Value = Name;
        plannings = new Employee[MaxEmployeePerPart];
        programmer = new Employee[MaxEmployeePerPart];
        arts = new Employee[MaxEmployeePerPart];
        _isRuntimeInitialized = true;
    }

    public bool HireEmployee(Employee e)
    {
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

        if (Array.IndexOf(targetArray, e) >= 0)
            return true;

        int emptyIndex = Array.FindIndex(targetArray, m => m == null);
        if (emptyIndex < 0)
        {
            Debug.LogWarning($"[{userNamed.Value}] {e.so.role} 파트 투입 슬롯이 가득 찼습니다.");
            return false;
        }

        _EmployeeManager.Instance.MarkProjectEmployee(e); //직원 상태 변경


        targetArray[emptyIndex] = e;
        Debug.Log($"[{userNamed.Value}] {e.so.Name} 직원이 {e.so.role} 파트로 투입되었습니다.");
        return true;
    }

    // 프로젝트에서 직원을 제거하고 해고 처리
    public bool FireEmployee(Employee e)
    {
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

        Debug.Log($"{userNamed}: [Day {day}] {DateTimeManager.GetDateString(day)}종료"); // 날짜 로그 표시중
        day++;
    }

    // 금요일 밤(평일 5일 경과 후) 주 1회 호출되는 메서드
    public void ProgressNight()
    {
        Debug.Log($"{userNamed}: 밤 이벤트 발생!");
        // 주간 정산
        foreach (var e in GetAllEmployees())
        {
            e.SaveCurrentData();
        }

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
        Debug.Log($"[{userNamed.Value}] {nightCount}주차 점수 | " +
                  $"완성도={qualThisNight:F1} 안정성={stabThisNight:F1} 매력도={charmThisNight:F1}\n" +
                  $"  누적 평균 → 완성도={qualityScore:F1} 안정성={stabilityScore:F1} 매력도={charmScore:F1} | curScore={CurScore:F1}");
#endif
        foreach (var report in pendingReports)
        {
            if (!acceptedReports.Contains(report))
                ReportPolicy.ApplyRejectedFatigue(report.owner);
        }

        pendingReports.Clear();
        selectedReports.Clear();
        isReportDraftsReady = false;

        if (day >= DurationDays)
            Finish();
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
        data.activeProjectsData.project_Id = Id;
        data.activeProjectsData.project_Name = name;
        data.activeProjectsData.project_Desc = Desc;
        data.activeProjectsData.project_Scale = Scale;
        data.activeProjectsData.project_RequiredCost = RequiredCost;
        data.activeProjectsData.project_MaxEmployeePerpart = MaxEmployeePerPart;
        data.activeProjectsData.project_DurationDays = DurationDays;

        data.activeProjectsData.project_day = day;
        data.activeProjectsData.project_userNamed = userNamed.Value;

        data.activeProjectsData.project_PlanningEmployeeIds = ConvertEmpArrayToIdList(plannings);
        data.activeProjectsData.project_ProgrammerEmployeeIds = ConvertEmpArrayToIdList(programmer);
        data.activeProjectsData.project_ArtistEmployeeIds = ConvertEmpArrayToIdList(arts);

        data.activeProjectsData.project_QualityScore = qualityScore;
        data.activeProjectsData.project_StabilityScore = stabilityScore;
        data.activeProjectsData.project_CharmScore = charmScore;
        data.activeProjectsData.project_CurScore = CurScore;
    }
    public void ImportProjectData(SaveData data)
    {
        this.so.id = data.activeProjectsData.project_Id;
        this.so.name = data.activeProjectsData.project_Name;
        this.so.desc = data.activeProjectsData.project_Desc;
        this.so.scale = data.activeProjectsData.project_Scale;
        this.so.requiredCost = data.activeProjectsData.project_RequiredCost;
        this.so.maxEmployeePerPart = data.activeProjectsData.project_MaxEmployeePerpart;
        this.so.durationDays = data.activeProjectsData.project_DurationDays;

        this.day = data.activeProjectsData.project_day;

        EnsureInitialized();
        this.userNamed.Value = data.activeProjectsData.project_userNamed;

        this.qualityScore = data.activeProjectsData.project_QualityScore;
        this.charmScore = data.activeProjectsData.project_CharmScore;
        this.CurScore = data.activeProjectsData.project_CurScore;

        RestoreEmployeeArray(data.activeProjectsData.project_PlanningEmployeeIds, plannings);
        RestoreEmployeeArray(data.activeProjectsData.project_ProgrammerEmployeeIds, programmer);
        RestoreEmployeeArray(data.activeProjectsData.project_ArtistEmployeeIds, arts);
    }
    private List<int> ConvertEmpArrayToIdList(Employee[] arr)
    {
        var list = new List<int>();
        if (arr == null) return list;

        foreach (var emp in arr)
        {
            list.Add(emp != null && emp.so != null ? emp.so.id : -1);
        }
        return list;
    }
    private void RestoreEmployeeArray(List<int> ids, Employee[] targetArr)
    {
        if (ids == null || targetArr == null) return;

        var hiredList = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;

        for (int i = 0; i < targetArr.Length && i < ids.Count; i++)
        {
            int empId = ids[i];
            if (empId == -1)
            {
                targetArr[i] = null;
            }
            else
            {
                targetArr[i] = hiredList.Find(e => e.so.id == empId);
                if (targetArr[i] != null)
                    _EmployeeManager.Instance.MarkProjectEmployee(targetArr[i]);
            }
        }
    }
    #endregion
}
