using TMPro;
using UnityEngine;
using R3;
using UnityEngine.UI;

public class DateTimeTestUI : MonoBehaviour
{
    [Header ("TMP연결")]
    [SerializeField] private TextMeshProUGUI dateText;          // 주차, 요일, 낮/밤 표시
    [SerializeField] private TextMeshProUGUI workStatusText;    // 업무 완료 여부

    [Header("테스트 버튼")]
    [SerializeField] private Button workCompleteButton;         // 업무 완료 버튼
    [SerializeField] private Button exitButton;                 // 퇴근 버튼

    [Header("퇴근 버튼 누르면 닫을 UI창")]
    [SerializeField] private GameObject deskUI;                 // 퇴근 버튼 누르면 닫을 UI창

    [Header("플레이어 참조 연결")]
    [SerializeField] private PlayerMove playerMove;

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
            if (DateTimeManager.Instance.isWorkCompleted)
            {
                if (deskUI != null)
                {
                    deskUI.SetActive(false);
                }

                if (playerMove != null)
                {
                    playerMove.CloseInteractionUI();
                }
            }

            
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
        // 업무 완료 여부에 따라 텍스트 변경, 퇴근 버튼 활성화 / 비활성화
        if (DateTimeManager.Instance.isWorkCompleted)
        {
            workStatusText.text = "Work Status: <color=green>Completed</color>";
            exitButton.interactable = true;
        }

        else
        {
            workStatusText.text = "Work Status: <color=red>Incomplete</color>";
            exitButton.interactable = false;
        }
    }
}
