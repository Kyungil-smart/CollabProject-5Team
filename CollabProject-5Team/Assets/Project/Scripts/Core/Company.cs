using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    [Header("프리펩 참조")]
    public GameObject[] projectPrefab; // 소형, 중형, 대형 순서 (Project 컴포넌트 포함)

    [Header("회사 정보")]
    public string playerName;
    public string CompanyName;
    public ReactiveProperty<int> gold = new(10000); // 보유 자금
    public int level = 1;                           // 회사 레벨, 회사 증축 상황(소형=1 중형=2 대형=3) 과 같음

    public int ProjectSlots = 1;  // 기획 변경으로 1고정(추후 삭제)

    public List<Project> projects = new(); //기획변경으로 필요없는 리스트, 이전코드 호환용으로 일단 냅둠
    public ReactiveProperty<int> activeProjectCount = new(0); // 현재 프로젝트 보유 여부 0: 없음, 1: 있음
    public Project curProject; // 진행중인 프로젝트는 오직 1개만 존재
    public List<Employee> selectedProjectEmployees = new(); // 신규 프로젝트 UI에서 임시 선택된 직원들
    public List<ProjectCompleted> completedProjects = new(); // 완료된 프로젝트 목록
    public bool hasPendingCompletedProject; // 런타임 전용: 완료 팝업 표시 대기 여부
    public ProjectCompleted pendingCompletedProject; // 런타임 전용: 이번 밤에 팝업으로 표시할 완료 프로젝트

    [Header("사후 관리")]
    public int popularity;   // 회사 인기
    public int reputation;   // 회사 평판
    public int weeklyCost;    // 유지비
    public int dailyProfit;  // 데일리 캐시 (완료 프로젝트 합산)
    public int weeklyProfit; // 데일리캐시를 일주일동안 누적한 값 (UI 히스토리용)
    public int totalRevenue;  // 총 누적 매출 (게임 전체 히스토리용)

    [Header("회사 업그레이드 데이터")]
    [SerializeField] public UpgradeData _upgradeData;

    #region DontDestroyOnLoad 없는 Instance
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;
    private void Awake()
    {
        Instance = this;
    #endregion
    }

    #region 테스트 코드
    // 자식 오브젝트의 Project를 curProject로 세팅하는 "테스트"코드
    //public void InitProjects()
    //{
    //    projects.AddRange(GetComponentsInChildren<Project>());
    //    curProject = projects.Count > 0 ? projects[0] : null;
    //    activeProjectCount.Value = curProject != null ? 1 : 0;

    //    if (curProject == null) return;
    //    foreach (var employee in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
    //    {
    //        curProject.HireEmployee(employee);
    //    }
    //}
    #endregion

    #region 프로젝트 시작 관리
    public Project CreateProject(ProjectSize scale, string projectName)
    {
        var project = InstantiateProject(scale, projectName);

        foreach (var employee in selectedProjectEmployees)
        {
            project.HireEmployee(employee);
        }

        return project;
    }
    private Project InstantiateProject(ProjectSize scale, string projectName)
    {
        var prefab = GetProjectPrefab(scale);

        var projectObject = Instantiate(prefab, transform);
        var project = projectObject.GetComponent<Project>();

        project.InitializeRuntime(projectName);

        return project;
    }
    public void StartNewProject(Project project)
    {
        gold.Value -= project.RequiredCost;
        QuestManager.Instance.ResetWeeklyBonus();

        if (!projects.Contains(project))
            projects.Add(project);

        curProject = project;
        activeProjectCount.Value = 1;
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

    private GameObject GetProjectPrefab(ProjectSize scale)
    {
        int index = scale switch
        {
            ProjectSize.Medium => 1,
            ProjectSize.Large => 2,
            _ => 0,
        };
        return projectPrefab[index];
    }
    #endregion

    #region 프로젝트 완료 처리
    public void CompleteProject(Project project)
    {
        // 이전 데이터 연동
        var record = new ProjectCompleted
        {
            projectName = project.userNamed.Value,
            scale = project.Scale,
            qualityScore = Mathf.RoundToInt(project.qualityScore),
            stabilityScore = Mathf.RoundToInt(project.stabilityScore),
            charmScore = Mathf.RoundToInt(project.charmScore),
            grade = project.Grade,
            genre = project.genre,
            artStyle = project.artStyle,
            engine = project.engine,
        };

        // 초기 정산 (계산영역: 평점·유저수·유지력·굿즈·일일매출·유지비)
        PerkPolicy.InitCompletedStats(record, popularity);

        // 회사 인기 반영
        popularity += PerkPolicy.CalcPopularityDelta(project.Grade);
        ApplyCompletionEmployeeRewards(project);

        // 객체 정리
        _EmployeeManager.Instance.ReleaseProjectEmployees(project.GetAllEmployees());
        completedProjects.Add(record);
        hasPendingCompletedProject = true;
        pendingCompletedProject = record;
        projects.Remove(project);

        curProject = null;

        activeProjectCount.Value = 0;
#if UNITY_EDITOR
        Debug.Log($"[Company] '{record.projectName}' 완료 (등급:{record.grade} 유저:{record.users} 일일매출:{record.dailyGold}G 유지비:{record.dailyCost}G)");
#endif
    }

    // - 완료 프로젝트 일일 정산 -
    public void TickDailyCompletedProjects()
    {
        foreach (var p in completedProjects)
        {
            if (p.isServiceOver) continue;

            // 판매량 재계산
            p.dailySales = PerkPolicy.CalcDailySales(p.scale, p.qualityScore, p.stabilityScore, p.charmScore, p.RetentionFactor, popularity);
            p.dailyGold = PerkPolicy.CalcDailyGold(p.scale, p.dailySales);

            p.weeklySales += p.dailySales;                // 주간 판매량 누적
            p.weeklyGoldAccum += p.dailyGold;              // 주간 매출 누적
            gold.Value += p.dailyGold;                      // 순수익 증가
            p.RetentionFactor -= PerkPolicy.RETENTION_DECAY; // 유지력 감소
        }
    }

    // — 완료 프로젝트 주간 정산 ─
    /// <summary>유지비 차감, 유지력·유저수·매출 재계산, 히스토리 기록, 평판 갱신</summary>
    public void TickWeeklyCompletedProjects()
    {
        foreach (var p in completedProjects)
        {
            if (p.isServiceOver) continue;

            // 이번 주 수치를 지난 주로 백업
            p.prevWeekUsers = p.users;
            p.prevWeekGold = p.weeklyGoldAccum;

            // 히스토리에 이번 주 누적 매출 push
            p.QueueWeeklyGold(p.weeklyGoldAccum);

            // 유저수 재계산 (이탈자 반영)
            p.users = PerkPolicy.CalcUsers(p.scale, p.qualityScore, p.prevWeekUsers);

            // 유지비 차감
            gold.Value -= p.dailyCost;

            // 평판: 이번 주 판매 100당 +1
            reputation += PerkPolicy.CalcReputationGainFromSales(p.weeklySales);
            // 누적매출 증가
            totalRevenue += p.prevWeekGold;

            p.weeklySales = 0;
            p.weeklyGoldAccum = 0;
        }

        // 적자 패널티
        if (gold.Value < 0)
            reputation += PerkPolicy.PENALTY_DEFICIT_HIT;

        // TODO: 적자시 1회 빚 및 게임오버 시스템
    }

    // 프로젝트 완료시 직원 보상 적용
    public void ApplyCompletionEmployeeRewards(Project project)
    {
        int abilityDelta = PerkPolicy.CalcCompletionAbilityDelta(project.Scale, project.Grade);
        int loyaltyDelta = PerkPolicy.CalcCompletionLoyaltyDelta(project.Scale, project.Grade);

        foreach (var employee in project.GetAllEmployees())
        {
            employee.AddAbilityDelta(abilityDelta);
            employee.MutableData.loyalty += loyaltyDelta;
        }
    }
    #endregion

    #region 직원 관리
    public void TickWeeklyEmployees()
    {
        foreach (var e in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            gold.Value -= e.so.weekSalary;
            if (e.WorkStatus != EmployeeWorkStatus.InProject)
                continue; // 프로젝트 중인 직원만 능력치 변화
            e.AddAbilityDelta(PerkPolicy.CalcWeeklyAbilityDelta(e.MutableData.loyalty));

            // 대화 안한 직원 패널티 적용
            if (!e.hasTalkedThisWeek)
            {
                e.MutableData.loyalty -= 5;
                e.MutableData.desire -= 5;
                e.MutableData.fatigue += 5;
                Debug.Log($"[C] 대화 하지않은 직원: {e.name}");
            }
            e.hasTalkedThisWeek = false;
        }
    }
    #endregion

    #region 회사 증축
    // 회사 증축 내부 처리 => 실제 객체 생성은 게임매니저에서 처리
    public void UpgradeOffice()
    {
        level++;
        var data = _upgradeData.GetData(level);
        if (data == null) return;

        // 재화 차감
        gold.Value -= data.GoldCost;

        // 스탯 추가 (현재 레벨업 대상 보상 스탯 반영)
        reputation += data.ReputationBonus;

        // 직원 충성도 강화
        foreach (var emp in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            emp.MutableData.loyalty += data.LoyaltyBonus;
        }

        GameManager.Instance.isUpgradeReserved = true;
    }
#endregion

    #region 세이브/로드
    public void ExportActiveProjectData(SaveData data)
    {
        data.activeProjectsData.hasActiveProject = activeProjectCount.Value > 0;
        if (!data.activeProjectsData.hasActiveProject) return;

        curProject.ExportProjectData(data);
    }

    public void ImportActiveProjectData(SaveData data)
    {
        ClearActiveProjectForLoad();

        var projectData = data.activeProjectsData;
        bool shouldRestoreProject = projectData.hasActiveProject
                                 || !string.IsNullOrEmpty(projectData.project_userNamed);
        if (!shouldRestoreProject) return;

        Project project = InstantiateProject(projectData.project_Scale, projectData.project_userNamed);

        projects.Add(project);
        curProject = project;
        activeProjectCount.Value = 1;

        project.ImportProjectData(data);
    }

    private void ClearActiveProjectForLoad()
    {
        if (curProject != null && !projects.Contains(curProject))
            Destroy(curProject.gameObject);

        foreach (var project in projects)
        {
            if (project != null)
                Destroy(project.gameObject);
        }

        projects.Clear();
        curProject = null;
        activeProjectCount.Value = 0;
        selectedProjectEmployees.Clear();
    }

    public void ExportCompanyData(SaveData data)
    {
        data.company_PlayerName  = this.playerName;
        data.company_CompanyName = this.CompanyName;
        data.company_Gold        = this.gold.Value;
        data.company_Level       = this.level;

        data.company_Popularity = this.popularity;
        data.company_Reputation = this.reputation;

        data.company_WeeklyCost   = this.weeklyCost;
        data.company_DailyProfit  = this.dailyProfit;
        data.company_WeeklyProfit = this.weeklyProfit;
        data.company_TotalRevenue = this.totalRevenue;

        data.completedProjectsData.Clear();

        foreach (var p in completedProjects)
        {
            if (p == null) continue;

            var pData = new ProjectCompletedSaveData
            {
                projectName     = p.projectName,
                scale           = p.scale,
                qualityScore    = p.qualityScore,
                stabilityScore  = p.stabilityScore,
                charmScore      = p.charmScore,
                grade           = p.grade.ToString(),
                genre           = p.genre,
                artStyle        = p.artStyle,
                engine          = p.engine,
                retentionFactor = p.RetentionFactor,
                users           = p.users,
                dailySales      = p.dailySales,
                dailyGold       = p.dailyGold,
                dailyCost       = p.dailyCost,
                weeklySales     = p.weeklySales,
                weeklyGoldAccum = p.weeklyGoldAccum,
                prevWeekUsers   = p.prevWeekUsers,
                prevWeekGold    = p.prevWeekGold,
                isServiceOver   = p.isServiceOver,

                weeklyGoldHistoryList = new List<int>(p.weeklyGoldHistory)
            };

            data.completedProjectsData.Add(pData);
        }
    }

    public void ImportCompanyData(SaveData data)
    {
        this.CompanyName = data.company_CompanyName;
        this.playerName  = data.company_PlayerName;
        this.gold.Value  = data.company_Gold;
        this.level       = data.company_Level;

        this.popularity = data.company_Popularity;
        this.reputation = data.company_Reputation;

        this.weeklyCost   = data.company_WeeklyCost;
        this.dailyProfit  = data.company_DailyProfit;
        this.weeklyProfit = data.company_WeeklyProfit;
        this.totalRevenue = data.company_TotalRevenue;

        completedProjects.Clear();
        hasPendingCompletedProject = false;
        pendingCompletedProject = null;
        if (data.completedProjectsData != null)
        {
            foreach (var pData in data.completedProjectsData)
            {
                var p = new ProjectCompleted
                {
                    projectName     = pData.projectName,
                    scale           = pData.scale,
                    qualityScore    = pData.qualityScore,
                    stabilityScore  = pData.stabilityScore,
                    charmScore      = pData.charmScore,
                    grade           = pData.grade[0],
                    genre           = pData.genre,
                    artStyle        = pData.artStyle,
                    engine          = pData.engine,
                    RetentionFactor = pData.retentionFactor,
                    users           = pData.users,
                    dailySales      = pData.dailySales,
                    dailyGold       = pData.dailyGold,
                    dailyCost       = pData.dailyCost,
                    weeklySales     = pData.weeklySales,
                    weeklyGoldAccum = pData.weeklyGoldAccum,
                    prevWeekUsers   = pData.prevWeekUsers,
                    prevWeekGold    = pData.prevWeekGold,
                    isServiceOver   = pData.isServiceOver
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
    #endregion
}
