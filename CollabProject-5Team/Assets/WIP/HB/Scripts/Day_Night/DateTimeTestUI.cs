using TMPro;
using UnityEngine;
using R3;
using UnityEngine.UI;
using UnityEngine.Android;

public class DateTimeTestUI : MonoBehaviour
{
    [Header ("TMP연결")]
    [SerializeField] private TextMeshProUGUI dateText;          // 주차, 요일, 낮/밤 표시
    [SerializeField] private TextMeshProUGUI talkCountText;     // 남은 대화 횟수
    [SerializeField] private TextMeshProUGUI workStatusText;    // 업무 완료 여부

    [Header("테스트 버튼")]
    [SerializeField] private Button workCompleteButton;         // 업무 완료 버튼
    [SerializeField] private Button exitButton;                 // 퇴근 버튼

    private void Start()
    {
        // R3구독, 날짜 데이터는 값이 바뀔 떄마다 자동으로 UI갱신
        DateTimeManager.Instance.currentWeek.Subscribe(_ => UpdateCalendarText()).AddTo(this);
        DateTimeManager.Instance.currentDay.Subscribe(_ => UpdateCalendarText()).AddTo(this);
        DateTimeManager.Instance.currentTime.Subscribe(_ => UpdateCalendarText()).AddTo(this);

        // 버튼 클릭 (업무 완료 버튼)
        workCompleteButton.onClick.AddListener(() =>
        {
            DateTimeManager.Instance.CompleteDayWork();
            UpdateUI();    
        });

        // 버튼 클릭 (퇴근하기)
        exitButton.onClick.AddListener(() =>
        {
            DateTimeManager.Instance.OnClickEndDayButton();
            UpdateUI();
        });

        // 게임 시작 시 화면 초기화
        UpdateCalendarText();
        UpdateUI();
    }

    // R3 날짜 변수들이 바뀔 때 알아서 호출되는 UI갱신 함수
    private void UpdateCalendarText()
    {
        int week = DateTimeManager.Instance.currentWeek.Value;
        string day = DateTimeManager.Instance.currentDay.Value.ToString();
        string time = DateTimeManager.Instance.currentTime.Value.ToString();

        dateText.text = $"Week {week} {day} ({time})";
    }

    private void UpdateUI()
    {
        // 대화 횟수 텍스트 갱신
        talkCountText.text = $"Talk Count : {DateTimeManager.Instance.talkCount}";

        // 업무 완료 여부에 따라 텍스트 변경, 퇴근 버튼 활성화 / 비활성화
        if (DateTimeManager.Instance.isWorkCompleted)
        {
            workStatusText.text = "Work Status: <color=green>Completed</color>";
            exitButton.interactable = true;
        }

        else
        {
            workStatusText.text = "Work Status: <color=red>Incompete</color>";
            exitButton.interactable = false;
        }
    }
}
