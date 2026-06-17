using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class Company : MonoBehaviour
{
    public static Company Instance;

    [Header("프리펩 참조")]
    public GameObject[] projectPrefab; // 소형, 중형, 대형 순서 (Project 컴포넌트 포함)

    [Header("회사 정보")]
    public string Name;
    public ReactiveProperty<int> gold = new(10000); // 보유 자금
    public int level;                               // 회사 레벨

    public int ProjectSlots = 1;  // 기획 변경으로 1고정(추후 삭제)

    public List<Project> projects = new(); // 현재 만들고 있는 프로젝트들
    public ReactiveProperty<int> activeProjectCount = new(0); // 만들고 있는 프로젝트 수
    public Project curProject; // 메인 프로젝트 (UI에 집중적으로 표시)
    public List<Employee> selectedProjectEmployees = new(); // 신규 프로젝트 UI에서 임시 선택된 직원들
    public List<ProjectCompleted> completedProjects = new(); // 완료된 프로젝트 목록

    [Header("사후 관리")]
    public int popularity;   // 회사 인기
    public int reputation;   // 회사 평판
    public int weeklyCost;    // 유지비
    public int dailyProfit;  // 데일리 캐시 (완료 프로젝트 합산)
    public int weeklyProfit; // 데일리캐시를 일주일동안 누적한 값 (UI 히스토리용)
    public int totalRevenue;  // 총 누적 매출 (게임 전체 히스토리용)

    [Header("회사 업그레이드 데이터")]
    private UpgradeData _upgradeData = new UpgradeData();

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);

        if (_upgradeData == null)
        {
            // UpgradeData가 일반 클래스라면 아래와 같이 생성
            _upgradeData = new UpgradeData();
        }
        _upgradeData.Init();
    #endregion
    }

    // private void OnEnable()
    // {
    //     _upgradeData.Init();
    // }

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
        gold.Value -= project.RequiredCost;
        QuestManager.Instance.ResetWeeklyBonus();

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
        ProjectSize.medium => projectPrefab[1],
        ProjectSize.large => projectPrefab[2],
        _ => projectPrefab[0],
    };
    #endregion

    // 프로젝트 완료 처리
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
        };

        // 초기 정산 (계산영역: 평점·유저수·유지력·굿즈·일일매출·유지비)
        PerkPolicy.InitCompletedStats(record, popularity);

        // 회사 인기 반영
        popularity += PerkPolicy.CalcPopularityDelta(project.Grade);
        ApplyCompletionEmployeeRewards(project);

        // 객체 정리
        _EmployeeManager.Instance.ReleaseProjectEmployees(project.GetAllEmployees());
        completedProjects.Add(record);
        projects.Remove(project);

        curProject = null;

        activeProjectCount.Value--;
#if UNITY_EDITOR
        Debug.Log($"[Company] '{record.projectName}' 완료 (등급:{record.grade} 평점:{record.rating:F1} 유저:{record.users} 일일매출:{record.dailyGold}G 유지비:{record.dailyCost}G)");
#endif
    }

    // 완료시 직원 보상 적용
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

    public void TickWeeklyEmployees()
    {
        var employeeManager = _EmployeeManager.Instance;

        foreach (var employee in employeeManager.haveEmployees.haveEmployeeList)
        {
            gold.Value -= employee.so.weekSalary;
            if (employee.WorkStatus != EmployeeWorkStatus.InProject)
                continue; // 프로젝트 중인 직원만 능력치 증가
            employee.AddAbilityDelta(PerkPolicy.CalcWeeklyAbilityDelta(employee.MutableData.loyalty));
        }
    }

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
            gold.Value += (p.dailyGold - p.dailyCost);
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
            gold.Value -= p.dailyCost;

            // 평판: 이번 주 매출 100G당 +1
            reputation += PerkPolicy.CalcReputationGainFromSales(p.prevWeekGold);

            // 평점 2점 이하 패널티
            if (p.rating <= 2f)
                reputation += PerkPolicy.PENALTY_LOW_RATING;
        }

        // 적자 패널티
        if (gold.Value < 0)
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

    // 회사 증축 가능 판단 
    public bool CheckCanUpgrade()
    {
        int curLevel = GameManager.Instance._currentOfficeIndex;
            
        if (curLevel >= _upgradeData.UpgradeRequiredDatas.Count || curLevel < 0) return false;

        UpgradeRequired req = _upgradeData.UpgradeRequiredDatas[curLevel];

        if (reputation < req.reputation) return false;

        return true;
    }

    // 회사 증축
    // 회사 증축
    public void UpgradeOffice()
    {
        int currentLevel = GameManager.Instance._currentOfficeIndex;

        if (!CheckCanUpgrade()) return;
        if (currentLevel >= _upgradeData.UpgradeRequiredDatas.Count) return;

        UpgradeRequired req = _upgradeData.UpgradeRequiredDatas[currentLevel];

        if (gold.Value < req.gold)
        {
            Debug.LogWarning("골드가 부족합니다.");
            return;
        }

        // 재화 차감
        gold.Value -= req.gold;

        // 스탯 추가 (현재 레벨업 대상 보상 스탯 반영)
        reputation += req.reputation;

        // 직원 충성도 강화 (SO 오염 방지 -> MutableData에 반영)
        var empList = _EmployeeManager.Instance?.haveEmployees?.haveEmployeeList;
        if (empList != null)
        {
            foreach (var emp in empList)
            {
                if (emp?.MutableData != null)
                {
                    emp.MutableData.loyalty += req.loyality;
                }
            }
        }

        // 대기 및 연출 제어를 위해 비동기 실행 흐름으로 호출
        GameManager.Instance.UpgradeOfficeAsync().Forget();
    }

    #region 세이브/로드
    public void ExportCompanyData(SaveData data)
    {
        data.company_Name  = this.name;
        data.company_Gold  = this.gold.Value;
        data.company_Level = this.level;

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
                rating          = p.Rating,
                retentionFactor = p.RetentionFactor,
                users           = p.users,
                dailySales      = p.dailySales,
                goodsSales      = p.goodsSales,
                dailyGold       = p.dailyGold,
                dailyCost       = p.dailyCost,
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
        this.name       = data.company_Name;
        this.gold.Value = data.company_Gold;
        this.level      = data.company_Level;

        this.popularity = data.company_Popularity;
        this.reputation = data.company_Reputation;

        this.weeklyCost   = data.company_WeeklyCost;
        this.dailyProfit  = data.company_DailyProfit;
        this.weeklyProfit = data.company_WeeklyProfit;
        this.totalRevenue = data.company_TotalRevenue;

        completedProjects.Clear();
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
                    grade           = !string.IsNullOrEmpty(pData.grade) ? pData.grade[0] : 'B',
                    Rating          = pData.rating,
                    RetentionFactor = pData.retentionFactor,
                    users           = pData.users,
                    dailySales      = pData.dailySales,
                    goodsSales      = pData.goodsSales,
                    dailyGold       = pData.dailyGold,
                    dailyCost       = pData.dailyCost,
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