using System.Collections.Generic;
using R3;
using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    [Header("프리펩 참조")]
    public GameObject smallProjectPrefab; // 프로젝트 프리팹 (Project 컴포넌트 포함)

    [Header("회사 정보")]
    public string Name;
    public int gold;         // 보유 자금
    public int level;        // 회사 레벨

    public int ProjectSlots = 1;  // 기획 변경으로 1고정(추후 삭제)

    public List<Project> projects = new(); // 현재 만들고 있는 프로젝트들
    public ReactiveProperty<int> activeProjectCount = new(0); // 만들고 있는 프로젝트 수
    public Project curProject; // 메인 프로젝트 (UI에 집중적으로 표시)
    public List<Employee> selectedProjectEmployees = new(); // 신규 프로젝트 UI에서 임시 선택된 직원들
    public List<ProjectCompleted> completedProjects = new(); // 완료된 프로젝트 목록

    [Header("사후 관리")]
    public int popularity;   // 회사 인기
    public int reputation;   // 회사 평판
    public int dailyCost;    // 유지비 (프로젝트들 합산)
    public int dailyProfit;  // 데일리 캐시 (완료 프로젝트 합산)
    public int weeklyProfit; // 데일리캐시를 일주일동안 누적한 값 (UI 히스토리용)

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
    }
    private void Start()
    {
        InitProjects();
    }

    // 자식 오브젝트의 Project 컴포넌트를 수집해 projects 리스트에 세팅
    public void InitProjects()
    {
        projects.Clear();
        projects.AddRange(GetComponentsInChildren<Project>());
        if (projects.Count > 0)
        {
            curProject = projects[0];
            activeProjectCount.Value++;
        }
        // _EmployeeManager HaveEmployees들을 curProject에 고용
        foreach (var employee in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            if (curProject != null)
                curProject.HireEmployee(employee);
        }
    }

    #region 프로젝트 시작 관리
    public Project CreateProject(ProjectSize scale, string projectName)
    {
        var prefab = GetProjectPrefab(scale);
        if (prefab == null)
        {
            Debug.LogWarning($"[Company] {scale} 규모 프로젝트 프리팹이 없습니다.");
            return null;
        }

        var projectObject = Instantiate(prefab, transform);
        var project = projectObject.GetComponent<Project>();

        project.InitializeRuntime(projectName);
        foreach (var employee in selectedProjectEmployees)
        {
            project.HireEmployee(employee);
        }

        return project;
    }
    public void StartNewProject(Project project)
    {
        gold -= project.RequiredCost;

        projects.Add(project);
        curProject = project;
        activeProjectCount.Value++;
    }

    public void ClearSelectedProjectEmployees()
    {
        selectedProjectEmployees.Clear();
    }

    public bool ToggleSelectedProjectEmployee(Employee employee, int maxPerPart)
    {
        if (employee == null) return false;

        if (selectedProjectEmployees.Contains(employee))
        {
            selectedProjectEmployees.Remove(employee);
            return true;
        }

        int sameRoleCount = 0;
        foreach (var selectedEmployee in selectedProjectEmployees)
        {
            if (selectedEmployee.so.role == employee.so.role)
            {
                sameRoleCount++;
            }
        }

        if (sameRoleCount >= maxPerPart)
        {
            return false;
        }

        selectedProjectEmployees.Add(employee);
        return true;
    }

    private GameObject GetProjectPrefab(ProjectSize scale) => scale switch
    {
        ProjectSize.small => smallProjectPrefab,
        _ => null,
    };
    #endregion

    // 프로젝트 완료 처리
    public void CompleteProject(Project project)
    {
        // 이전 데이터 연동
        var record = new ProjectCompleted
        {
            projectID = project.Id,
            projectName = project.userNamed.Value,
            scale = project.Scale,
            qualityScore = Mathf.RoundToInt(project.qualityScore),
            stabilityScore = Mathf.RoundToInt(project.stabilityScore),
            charmScore = Mathf.RoundToInt(project.charmScore),
            grade = project.Grade,
        };

        // 초기 정산 (계산영역: 평점·유저수·유지력·굿즈·일일매출·유지비)
        PerkPolicy.InitCompletedStats(record, popularity);

        // 회사 인기 반영
        popularity += PerkPolicy.CalcPopularityDelta(project.Grade);

        // 객체 정리
        completedProjects.Add(record);
        projects.Remove(project);

        if (curProject == project)
            curProject = projects.Count > 0 ? projects[0] : null;

        activeProjectCount.Value--;

        Debug.Log($"[Company] '{record.projectName}' 완료 (등급:{record.grade} 평점:{record.rating:F1} 유저:{record.users} 일일매출:{record.dailyGold}G 유지비:{record.dailyCost}G)");
    }

    // ─ 매 영업일 호출 — 완료 프로젝트 수익 정산 ─
    public void TickDailyCompletedProjects()
    {
        foreach (var p in completedProjects)
        {
            if (p.isServiceOver) continue;

            // 판매량 재계산
            p.goodsSales = PerkPolicy.CalcGoodsSales(p.charmScore);
            p.dailySales = PerkPolicy.CalcDailySales(p.scale, p.Rating, p.RetentionFactor, popularity);
            p.dailyGold = PerkPolicy.CalcDailyGold(p.scale, p.dailySales, p.goodsSales);

            p.weeklyGoldAccum += p.dailyGold;
            gold += (p.dailyGold - p.dailyCost);
            p.RetentionFactor -= PerkPolicy.RETENTION_DECAY; // 유지력 감소
        }
    }

    // ─ 매주 금요일 밤 호출 — 완료 프로젝트 주간 정산 ─
    /// <summary>유지비 차감, 유지력·유저수·매출 재계산, 히스토리 기록, 평판 갱신</summary>
    public void TickWeeklyCompletedProjects()
    {
        foreach (var p in completedProjects)
        {
            if (p.isServiceOver) continue;

            // 주간 정산 (유저수·매출 재계산, 히스토리 push, prevWeek 갱신)
            PerkPolicy.TickWeeklyStats(p);

            // 유지비 차감
            gold -= p.dailyCost;

            // 평판: 이번 주 매출 100G당 +1
            reputation += PerkPolicy.CalcReputationGainFromSales(p.prevWeekGold);

            // 평점 2점 이하 패널티
            if (p.rating <= 2f)
                reputation += PerkPolicy.PENALTY_LOW_RATING;
        }

        // 적자 패널티
        if (gold < 0)
            reputation += PerkPolicy.PENALTY_DEFICIT_HIT;

        // TODO: 적자시 1회 빚 및 게임오버 시스템
    }

    // 방치 패널티 적용
    public void AfkPenaltyApply()
    {
        if (curProject != null)
        {
            foreach (Employee e in curProject.GetAllEmployees())
            {
                if (!e.hasTalkedThisWeek)
                {
                    e.MutableData.loyalty -= 5;
                    e.MutableData.desire -= 5;
                    e.MutableData.fatigue += 5;
                    Debug.Log($"[C] 대화 하지않은 직원: {e.name}");
                }
            }
        }
    }
    // 대화한 직원 초기화
    public void ResetTalkedEmployees()
    {
        if (curProject != null)
        {
            foreach (Employee e in curProject.GetAllEmployees())
            {
                e.hasTalkedThisWeek = false;
            }
        }
    }

    public void ExportCompanyData(SaveData data)
    {
        data.company_Name = this.name;
        data.company_Gold = this.gold;
        data.company_Level = this.level;

        data.company_Popularity = this.popularity;
        data.company_Reputation = this.reputation;

        data.company_DailyCost = this.dailyCost;
        data.company_DailyProfit = this.dailyProfit;
        data.company_weeklyProfit = this.weeklyProfit;

        data.completedProjectsData.Clear();

        foreach (var p in completedProjects)
        {
            if (p == null) continue;

            var pData = new ProjectCompletedSaveData
            {
                projectID = p.projectID,
                projectName = p.projectName,
                scale = p.scale,
                qualityScore = p.qualityScore,
                stabilityScore = p.stabilityScore,
                charmScore = p.charmScore,
                grade = p.grade.ToString(),
                rating = p.Rating,
                retentionFactor = p.RetentionFactor,
                users = p.users,
                dailySales = p.dailySales,
                goodsSales = p.goodsSales,
                dailyGold = p.dailyGold,
                dailyCost = p.dailyCost,
                weeklyGoldAccum = p.weeklyGoldAccum,
                prevWeekUsers = p.prevWeekUsers,
                prevWeekGold = p.prevWeekGold,
                isServiceOver = p.isServiceOver,

                weeklyGoldHistoryList = new List<int>(p.weeklyGoldHistory)
            };

            data.completedProjectsData.Add(pData);
        }
    }

    public void ImportCompanyData(SaveData data)
    {
        this.name = data.company_Name;
        this.gold = data.company_Gold;
        this.level = data.company_Level;

        this.popularity = data.company_Popularity;
        this.reputation = data.company_Reputation;

        completedProjects.Clear();
        if (data.completedProjectsData != null)
        {
            foreach (var pData in data.completedProjectsData)
            {
                var p = new ProjectCompleted
                {
                    projectID = pData.projectID,
                    projectName = pData.projectName,
                    scale = pData.scale,
                    qualityScore = pData.qualityScore,
                    stabilityScore = pData.stabilityScore,
                    charmScore = pData.charmScore,
                    grade = !string.IsNullOrEmpty(pData.grade) ? pData.grade[0] : 'B',
                    Rating = pData.rating,
                    RetentionFactor = pData.retentionFactor,
                    users = pData.users,
                    dailySales = pData.dailySales,
                    goodsSales = pData.goodsSales,
                    dailyGold = pData.dailyGold,
                    dailyCost = pData.dailyCost,
                    weeklyGoldAccum = pData.weeklyGoldAccum,
                    prevWeekUsers = pData.prevWeekUsers,
                    prevWeekGold = pData.prevWeekGold,
                    isServiceOver = pData.isServiceOver
                };

                p.weeklyGoldHistory = new Queue<int>();
                if (pData.weeklyGoldHistoryList != null)
                {
                    foreach (int goldValue in pData.weeklyGoldHistoryList)
                    {
                        p.weeklyGoldHistory.Enqueue(goldValue);
                    }
                }

                completedProjects.Add(p);
            }
        }
    }
}