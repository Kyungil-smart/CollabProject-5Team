using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    [Header("회사 정보")]
    public string Name;
    public int gold;         // 보유 자금
    public int level;        // 회사 레벨

    public int ProjectSlots  // 값은 임의로 작성
    {
        get
        {
            if (level <= 1) return 1;
            else if (level <= 3) return 2;
            else return 3;
        }
    }
    public List<Project> projects = new(); // 현재 진행중인 프로젝트들
    public Project curProject; // 메인 프로젝트 (UI에 집중적으로 표시)
    public List<ProjectCompleted> completedProjects = new(); // 완료된 프로젝트 목록

    [Header("사후 관리")]
    public int popularity;   // 회사 인기
    public int reputation;   // 회사 평판
    public int dailyCost;    // 유지비 (프로젝트들 합산)
    public int dailyProfit;  // 데일리 캐시 (완료 프로젝트 합산)

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
        curProject = projects[0];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 테스트용: 첫 번째 직원 고용
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[0].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);
            curProject.HireEmployee(hiredEmployee);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[1].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);
            curProject.HireEmployee(hiredEmployee);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[2].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);
            curProject.HireEmployee(hiredEmployee);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) // 테스트용: 첫 번째 직원 해고
        {
            Employee target = curProject?.plannings[0];
            curProject.FireEmployee(target);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Employee target = curProject?.programmer[0];
            curProject.FireEmployee(target);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Employee target = curProject?.arts[0];
            curProject.FireEmployee(target);
        }
    }

    #region 프로젝트 관리
    public void StartNewProject(Project project)
    {
        if (!CanStartNewProject(project)) return;
        gold -= project.RequiredCost;

        projects.Add(project);
    }

    public bool CanStartNewProject(Project project)
    {
        if (projects.Count >= ProjectSlots)
        {
            Debug.Log("프로젝트 슬롯이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        if (gold < project.RequiredCost)
        {
            Debug.Log("보유 자금이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        return true;
    }

    // 프로젝트 완료 처리
    public void CompleteProject(Project project)
    {
        // 이전 데이터 연동
        var record = new ProjectCompleted
        {
            projectID      = project.Id,
            projectName    = project.userNamed.Value,
            scale          = project.Scale,
            qualityScore   = Mathf.RoundToInt(project.qualityScore),
            stabilityScore = Mathf.RoundToInt(project.stabilityScore),
            charmScore     = Mathf.RoundToInt(project.charmScore),
            grade          = project.Grade,
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
    #endregion
}
