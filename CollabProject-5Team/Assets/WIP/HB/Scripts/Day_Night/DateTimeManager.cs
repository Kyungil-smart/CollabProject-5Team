using R3;
using System.Collections.Generic;
using UnityEngine;

public class DateTimeManager : MonoBehaviour
{
    // 어디서나 부를 수 있도록 싱글톤
    public static DateTimeManager Instance { get; private set; }

    [Header("현재 게임 날짜 상태(R3 반응형 변수)")]
    public ReactiveProperty<int> currentWeek = new(1);
    public ReactiveProperty<DayOfWeek> currentDay = new(DayOfWeek.Monday);
    public ReactiveProperty<TimeOfDay> currentTime = new(TimeOfDay.Day);
    public ReactiveProperty<int> day = new(0); // 영업일 기준 지난 날짜

    [Header("오늘 하루 상태 값")]
    public bool isWorkCompleted = false;        // 일일 업무 완료 여부
    private HashSet<string> talkedNpcsToday = new HashSet<string>();

    // 이번 주에 대화한 직원 ID 목록 (방치 패널티 판정용)
    private HashSet<Employee> _talkedEmployeesThisWeek = new HashSet<Employee>();

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
        ResetDayStatus();
    }

    /// <summary>
    /// 새로운 하루가 시작될 때 리셋하는 함수
    /// </summary>
    private void ResetDayStatus()
    {
        isWorkCompleted = false;
        talkedNpcsToday.Clear();
    }
    /// <summary>
    /// 새로운 주가 시작될 때 리셋하는 함수
    /// </summary>
    private void ResetWeekStatus()
    {
        _talkedEmployeesThisWeek.Clear();
        Company.Instance.curProject.GetAllEmployees().ForEach(e => e.hasTalkedThisWeek = false);
    }

    /// <summary>
    /// 업무 완료됐을 때 호출할 함수
    /// </summary>
    public void CompleteDayWork()
    {
        isWorkCompleted = true;
        Debug.Log("[DTM] 임무 완료");

        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            GameManager.Instance.player.WorkCompleteAnim();
        }
    }

    /// <summary>
    /// NPC가 대화가 가능한 상태인지
    /// </sumary>
    public int GetDialogueState(string npcName)
    {
        // 업무를 마치지 않았다면 대화 불가
        if (!isWorkCompleted)
        {
            return 0;
        }

        // 업무를 마쳤는데, 이미 해당 NPC와 특별 대화를 나눴다면
        if (talkedNpcsToday.Contains(npcName))
        {
            return 2;
        }

        // 업무 마쳤고, 해당 NPC와 첫 대화라면 특별 대화
        return 1;
    }

    /// <summary>
    /// 특별 대화 시작 시 호출 — 이번 주 대화 직원으로 등록 (방치 패널티 면제)
    /// </summary>
    public void MarkTalkedThisWeek(Employee e)
    {
        _talkedEmployeesThisWeek.Add(e);
        e.hasTalkedThisWeek = true;
    }

    /// <summary>
    /// NPC와 특별 대화를 마쳤다면 해당 NPC를 저장함
    /// </summary>
    public void CompleteSpecialDialogue(string npcName)
    {
        if (!talkedNpcsToday.Contains(npcName))
        {
            talkedNpcsToday.Add(npcName);
        }
    }

    /// <summary>
    /// 퇴근 버튼을 누르면 다음 날짜를 계산하는 로직
    /// </summary>
    public void OnClickEndDayButton()
    {
        // 업무가 끝나지 않았다면 퇴근 불가
        if (!isWorkCompleted)
        {
            return;
        }

        // 금요일 낮에 퇴근하면 금요일 밤으로 전환
        if (currentDay.Value == DayOfWeek.Friday && currentTime.Value == TimeOfDay.Day)
        {
            currentTime.Value = TimeOfDay.Night;

            currentWeek.Value++;
            ProgressDay();
            ResetDayStatus();
        }
        // 금요일 밤에 퇴근하면 다음 주 월요일 낮으로 전환
        else if (currentDay.Value == DayOfWeek.Friday && currentTime.Value == TimeOfDay.Night)
        {
            // 방치 패널티: 이번 주 미대화 직원 충성도 -5, 피로도 +10
            foreach (Employee e in Company.Instance.curProject.GetAllEmployees())
            {
                if (!e.hasTalkedThisWeek)
                {
                    e.MutableData.loyalty -= 5;
                    e.MutableData.fatigue += 10;
                }
            }

            // 1주차씩 상승
            currentDay.Value = DayOfWeek.Monday;
            currentTime.Value = TimeOfDay.Day;

            ResetDayStatus();
            ResetWeekStatus();
        }
        // 월~목 낮에 퇴근하면 다음 날 낮으로
        else
        {
            // 요일 하나 이동
            currentDay.Value++;
            // 낮으로
            currentTime.Value = TimeOfDay.Day;

            ProgressDay();
            ResetDayStatus();
        }
    }


    #region 날짜 진행
    public void ProgressDay()
    {
        day.Value++;
        foreach (var project in Company.Instance.projects)
            project.ProgressDay();

        // 금요일 밤:
        if (day.Value % 5 == 0) ProgressNight();
    }
    public void ProgressNight()
    {
        foreach (var project in Company.Instance.projects) project.ProgressNight();
    }

    static readonly string[] WeekDayNames = { "월요일", "화요일", "수요일", "목요일", "금요일" };
    static readonly int[] MonthDays = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
    public static string GetWeekDayName(int day) => WeekDayNames[day % 5];

    // 영업일(day) 기준으로 "N월 N일 요일" 문자열 반환
    // day=0 → 1월 1일 월요일, day=4 → 1월 5일 금요일, day=5 → 1월 8일 월요일
    public static string GetDateString(int day)
    {
        int week = day / 5;
        int dayOfWeek = day % 5;
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

}
