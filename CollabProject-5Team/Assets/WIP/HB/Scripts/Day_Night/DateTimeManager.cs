using System.Collections.Generic;
using R3;
using UnityEngine;

public class DateTimeManager : MonoBehaviour
{
    // 어디서나 부를 수 있도록 싱글톤
    public static DateTimeManager Instance { get; private set; }


    [Header("현재 게임 날짜 상태(R3 반응형 변수)")]
    public ReactiveProperty<int> currentWeek = new(1);
    public ReactiveProperty<DayOfWeek> currentDay = new(DayOfWeek.Monday);
    public ReactiveProperty<TimeOfDay> currentTime = new(TimeOfDay.Day);

    [Header("오늘 하루 상태 값")]
    public bool isWorkCompleted = false;        // 일일 업무 완료 여부
    private HashSet<string> talkedNpcsToday = new HashSet<string>();


    private void Awake()
    {
        // 중복 방지, 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
    /// 업무 완료됐을 때 호출할 함수
    /// </summary>
    public void CompleteDayWork()
    {
        isWorkCompleted = true;
    }

    /// <summary>
    /// NPC가 대화가 가능한 상태인지
    /// </sumary>
    public int GetDialogueState(string npcName)
    {
        // 업무를 마치지 않았다면 일반 대화만 가능
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

            ResetDayStatus();
        }

        // 금요일 밤에 퇴근하면 다음 주 월요일 낮으로 전환
        else if (currentDay.Value == DayOfWeek.Friday && currentTime.Value == TimeOfDay.Night)
        {
            // 1주차씩 상승
            currentWeek.Value++;
            currentDay.Value = DayOfWeek.Monday;
            currentTime.Value = TimeOfDay.Day;

            ResetDayStatus();
        }

        // 월~목 낮에 퇴근하면 다음 날 낮으로
        else
        {
            // 요일 하나 이동
            currentDay.Value++;
            // 낮으로
            currentTime.Value = TimeOfDay.Day;

            ResetDayStatus();
        }
    }
}
