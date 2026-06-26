using R3;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DateTimeManager : MonoBehaviour
{
    // 어디서나 부를 수 있도록 싱글톤
    public static DateTimeManager Instance { get; private set; }

    [Header("현재 게임 날짜 상태")]
    public ReactiveProperty<int> currentWeek = new(1);
    public DayOfWeek currentDay = DayOfWeek.Monday; // 요일
    public TimeOfDay currentTime = TimeOfDay.Day;   // 낮밤
    public ReactiveProperty<int> day = new(0); // 영업일 기준 지난 날짜

    [Header("오늘 하루 상태 값")]
    public bool isWorkCompleted = false;        // 일일 업무 완료 여부
    public bool isEventQuest = false;           // 이벤트 퀘스트 대화 진행 여부
    private HashSet<string> talkedNpcsToday = new HashSet<string>();

    public static Action OnDay;  // 낮
    public static event Action OnWorkCompleted;
    public static event Action OnNightLoading;
    public static event Action OnNight;// 밤
    public static event Action OnWeekStarted; // 월요일 아침에만 호출할 이벤트

    public static Action OnReportEnd;

    private float playTime = 0f;

    public static Func<UniTask> OnDateChangedVisual;
    public static Func<UniTask> OnTimeChangedVisual;
    public static Action OnDateUIChanged;

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    #endregion
    }

    void Update()
    {
        playTime += Time.deltaTime;
    }

    public string GetPlayTime()
    {
        int minutes = Mathf.FloorToInt(playTime / 60F);
        int seconds = Mathf.FloorToInt(playTime - minutes * 60);

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// 새로운 하루가 시작될 때 리셋하는 함수
    /// </summary>
    private void ResetDayStatus()
    {
        isWorkCompleted = false;
        isEventQuest = false;
        talkedNpcsToday.Clear();

        // 상태만 Ready로 초기화 - 실제 시작은 WorkStart 버튼 클릭 시 HUDPresenter에서 호출
        QuestManager.Instance.dailyQuestState.Value = QuestState.Ready;
        EventQuestManager.Instance?.ResetForNewDay();
    }

    /// <summary>
    /// 업무 완료됐을 때 호출할 함수
    /// </summary>
    public void CompleteDayWork()
    {
        isWorkCompleted = true;

        GameManager.Instance.player.WorkCompleteAnim();

        OnWorkCompleted?.Invoke();
    }

    /// <summary>
    /// NPC가 대화가 가능한 상태인지
    /// </sumary>
    public int GetDialogueState(string npcName, bool isStoryQuest = false)
    {
        // 스토리 퀘스트 대화라면, 업무 완료 여부와 상관없이 대화 가능
        if (isStoryQuest)
        {
            if (talkedNpcsToday.Contains(npcName))
                return 2;

            return 1;
        }

        // 업무를 마치지 않았다면 대화 불가
        if (!isWorkCompleted)
        {
            return 0;
        }

        // 업무를 마쳤는데, 이미 해당 NPC와 대화를 나눴다면
        if (talkedNpcsToday.Contains(npcName))
        {
            return 2;
        }

        // 업무 마쳤고, 해당 NPC와 오늘 첫 대화
        return 1;
    }

    /// <summary>
    /// 특별 대화 시작 시 호출 — 이번 주 대화 직원으로 등록 (방치 패널티 면제)
    /// </summary>
    public void MarkTalkedThisWeek(Employee e)
    {
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

    #region 회사=>프로젝트 날짜 진행 연동
    /// <summary>
    /// 퇴근 버튼을 누르면 다음 날짜를 계산하는 로직
    /// </summary>
    [ContextMenu("퇴근 처리")]
    public async UniTask OnClickEndDayButton()
    {
        if (currentDay == DayOfWeek.Friday && currentTime == TimeOfDay.Day)
        {
            if (OnTimeChangedVisual != null)
            {
                await OnTimeChangedVisual.Invoke();
            }
            else
            {
                await ProcessDateLogic();
                OnDateUIChanged?.Invoke();
            }
        }
        else
        {
            if (OnDateChangedVisual != null)
            {
                await OnDateChangedVisual.Invoke();
            }
            else
            {
                await ProcessDateLogic();
                OnDateUIChanged?.Invoke();
            }
        }
    }

    public async UniTask ProcessDateLogic()
    {
        // 금요일 낮에 퇴근하면 금요일 밤으로 전환
        if (currentDay == DayOfWeek.Friday && currentTime == TimeOfDay.Day)
        {
            // 금요일 낮 업무 종료 시 NPC 퇴근
            GameManager.Instance.LeaveWorkNPCs();
            currentTime = TimeOfDay.Night;

            OnNightLoading?.Invoke();// 밤

            Progress();
        }

        // 금요일 밤에 퇴근하면 다음 주 월요일 낮으로 전환
        else if (currentDay == DayOfWeek.Friday && currentTime == TimeOfDay.Night)
        {
            if (currentWeek.Value % 4 == 0)
                Company.Instance.CloseManagementMonth();

            // 1주차씩 상승
            currentWeek.Value++;
            currentDay = DayOfWeek.Monday;
            currentTime = TimeOfDay.Day;

            //GameManager.Instance.HiredNPCGoToWork().Forget();
            // 맵 업그레이드 적용
            await GameManager.Instance.TryProcessUpgradeAsync();

            // 월요일 낮이 되면 퇴근했던 직원 다시 생성
            GameManager.Instance.HiredNPCGoToWork().Forget();

            ResetDayStatus();
            _EmployeeManager.Instance.TryProcessDailyLeave();
            OnDay?.Invoke();// 낮
            OnWeekStarted?.Invoke(); // 월요일 아침
        }
        // 월~목 낮에 퇴근하면 다음 날 낮으로
        else
        {
            // 요일 하나 이동
            currentDay++;
            // 낮으로
            currentTime = TimeOfDay.Day;

            Progress();
            ResetDayStatus();
            _EmployeeManager.Instance.TryProcessDailyLeave();
            OnDay?.Invoke();
        }
    }

    // 내부적으로 영업일을 진행시킴
    public void Progress()
    {
        if (Company.Instance.activeProjectCount.Value > 0)
            Company.Instance.curProject.Progress();

        // 완료 프로젝트 일일 수익 정산
        Company.Instance.TickDailyCompletedProjects();

        // 금요일 밤:
        day.Value++;
        if (day.Value % 5 == 0) ProgressNight();
    }
    public void ProgressNight()
    {
        // 완료 프로젝트 주간 정산
        _EmployeeManager.Instance.TickWeeklyTraining();
        Company.Instance.TickWeeklyEmployees();
        Company.Instance.TickWeeklyOfficeCost();
        Company.Instance.TickWeeklyCompletedProjects();

        if (Company.Instance.activeProjectCount.Value > 0)
            Company.Instance.curProject.ProgressNight();

        _EmployeeManager.Instance.RegisterLeavePendingEmployees();
        _EmployeeManager.Instance.GenerateWeeklyApplicants();
        OnNight?.Invoke();
    }

    public string GetDayName()
    {
        return currentDay switch
        {
            DayOfWeek.Monday => "월요일",
            DayOfWeek.Tuesday => "화요일",
            DayOfWeek.Wednesday => "수요일",
            DayOfWeek.Thursday => "목요일",
            _ => "금요일",
        };
    }

    // 영업일(day) 기준으로 "00년 00월 0주 월요일" 문자열 반환
    // day=0   → 01년 01월 1주 월요일
    // day=4   → 01년 01월 1주 금요일
    // day=5   → 01년 01월 2주 월요일
    public static string GetDateString(int day)
    {
        const int daysPerWeek = 5;
        const int weeksPerMonth = 4;
        const int monthsPerYear = 12;
        const int daysPerMonth = daysPerWeek * weeksPerMonth; // 20
        const int daysPerYear = daysPerMonth * monthsPerYear; // 240

        int year = day / daysPerYear + 1;
        int dayOfYear = day % daysPerYear;

        int month = dayOfYear / daysPerMonth + 1;
        int dayOfMonth = dayOfYear % daysPerMonth;

        int weekOfMonth = dayOfMonth / daysPerWeek + 1;
        int dayOfWeek = dayOfMonth % daysPerWeek;

        return $"{year:D2}년 {month:D2}월 {weekOfMonth}주 {WeekDayNames[dayOfWeek]}";
    }
    static readonly string[] WeekDayNames = { "월요일", "화요일", "수요일", "목요일", "금요일" };
    // 단순 "0월 0주차" 반환
    public static string GetMonthWeekString(int day)
    {
        int month = (day % 240) / 20 + 1;
        int week = (day % 20) / 5 + 1;
        return $"{month}월 {week}주차";
    }

    #endregion

    public void ExportSaveData(SaveData data)
    {
        data.currentWeek     = this.currentWeek.Value;
        data.currentDay      = this.currentDay;
        data.currentTime     = this.currentTime;
        data.day             = this.day.Value;
        data.isWorkCompleted = this.isWorkCompleted;
        data.playTime        = this.playTime;
    }

    public void ImportSaveData(SaveData data)
    {
        if (data == null) return;

        this.currentWeek.Value = data.currentWeek;
        this.currentDay        = data.currentDay;
        this.currentTime       = data.currentTime;
        this.day.Value         = data.day;
        this.isWorkCompleted   = data.isWorkCompleted;
        this.talkedNpcsToday.Clear();
        this.playTime          = data.playTime;
    }
}
