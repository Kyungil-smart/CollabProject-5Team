using R3;
using System;
using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    public string Name;
    public ReactiveProperty<int> day = new(0); // 영업일 기준 지난 날짜
    public int gold;            // 보유 자금
    public int level;           // 회사 레벨

    public int ProjectSlots  // 값은 임의로 작성
    {
        get
        {
            if (level <= 1) return 1;
            else if (level <= 3) return 2;
            else return 3;
        }
    }

    public Project[] projects;  // 현재 진행중인 프로젝트들

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 테스트용: 첫 번째 직원 고용
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[0].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);

            projects[0].HireEmployee(hiredEmployee);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[1].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);

            projects[0].HireEmployee(hiredEmployee);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[2].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);

            projects[0].HireEmployee(hiredEmployee);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) // 테스트용: 첫 번째 직원 해고
        {
            Employee target = projects[0].plannings[0];
            projects[0].FireEmployee(target);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Employee target = projects[0].develops[0];
            projects[0].FireEmployee(target);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Employee target = projects[0].arts[0];
            projects[0].FireEmployee(target);
        }
    }

    #region 날짜 진행
    public void ProgressDay()
    {
        if (projects.Length == 0 || Array.TrueForAll(projects, p => p.isFinished.Value))
        {
            Debug.Log("[Company] 모든 프로젝트가 종료되어 날짜를 진행할 수 없습니다.");
            return;
        }

        day.Value++;
        foreach (var project in projects)
            project.ProgressDay();

        // 금요일 밤:
        if (day.Value % 5 == 0) ProgressNight();
    }
    public void ProgressNight()
    {
        foreach (var project in projects)
            project.ProgressNight();

        // TODO: 모든 밤 정산이 끝나고 보고서 이벤트 연결
    }

    static readonly string[] WeekDayNames = { "월요일", "화요일", "수요일", "목요일", "금요일" };
    static readonly int[]    MonthDays    = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    public static string GetWeekDayName(int day) => WeekDayNames[day % 5];
    // 영업일(day) 기준으로 "N월 N일 요일" 문자열 반환
    // day=0 → 1월 1일 월요일, day=4 → 1월 5일 금요일, day=5 → 1월 8일 월요일
    public static string GetDateString(int day)
    {
        int week        = day / 5;
        int dayOfWeek   = day % 5;
        int calendarDay = day + week * 2 + 1; // 1-based 달력 날짜 (주말 2일씩 추가)

        int month = 1;
        int remaining = calendarDay;
        while (month <= 12 && remaining > MonthDays[month - 1])
        {
            remaining -= MonthDays[month - 1];
            month++;
        }
        return $"{month}월 {remaining}일 {WeekDayNames[dayOfWeek]}";
    }
    #endregion

    #region 프로젝트 관리
    public void StartNewProject(Project project)
    {
        if (!CanStartNewProject(project)) return;
        gold -= project.RequiredCost;

        projects[projects.Length] = project;
    }

    public bool CanStartNewProject(Project project)
    {
        if (projects.Length < ProjectSlots)
        {
            Debug.Log("프로젝트 슬롯이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        if (gold >= project.RequiredCost)
        {
            Debug.Log("보유 자금이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        return true;
    }
    #endregion
}
