using TMPro;
using UnityEngine;
using R3;
using UnityEngine.UI;

public class DateTimeTestUI : MonoBehaviour
{
    public static DateTimeTestUI Instance { get; private set; }

    [Header ("날짜, 업무 완료 여부 TMP")]
    [SerializeField] private TextMeshProUGUI dateText;          // 주차, 요일, 낮/밤 표시
    [SerializeField] private TextMeshProUGUI workStatusText;    // 업무 완료 여부

    [Header("하단 UI창의 업무시작, 퇴근 버튼")]
    [SerializeField] private Button workStartButton;            // 업무 완료 버튼
    [SerializeField] private Button getOffWorkButton;           // 퇴근 버튼

    [Header("업무 UI창")]
    [SerializeField] private GameObject taskUI;                 // 퇴근 버튼 누르면 닫을 UI창

    [Header("업무 UI 내부 업무 완료 버튼")]
    [SerializeField] private Button workCompleteButton;         // 업무 완료 버튼

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // R3구독, 날짜 데이터는 값이 바뀔 떄마다 자동으로 UI갱신
        DateTimeManager.Instance.currentWeek.Subscribe(_ => UpdateCalendarText()).AddTo(this);
        DateTimeManager.Instance.currentDay.Subscribe(_ => UpdateCalendarText()).AddTo(this);
        DateTimeManager.Instance.currentTime.Subscribe(_ => UpdateCalendarText()).AddTo(this);

        // 버튼 클릭 (업무 시작 버튼)
        workStartButton.onClick.AddListener(() =>
        {
            if (taskUI != null)
            {
                taskUI.SetActive(true);
            }   
        });

        // 업무 완료 버튼 클릭 시 업무 끝내고 창 닫기
        if (workCompleteButton != null)
        {
            workCompleteButton.onClick.AddListener(() =>
            {
                // 데이터 완료
                DateTimeManager.Instance.CompleteDayWork();

                // 창 닫기
                if (taskUI != null) taskUI.SetActive(false);

                // UI초기화
                UpdateUI();
            });
        }

        // 버튼 클릭 (퇴근하기)
        getOffWorkButton.onClick.AddListener(() =>
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
        // 업무 완료 여부에 따라 텍스트 변경, 퇴근 버튼 활성화 / 비활성화
        if (DateTimeManager.Instance.isWorkCompleted)
        {
            workStatusText.text = "Work Status: <color=green>Completed</color>";

            // 업무가 끝났으니 업무시작버튼 비활성화
            workStartButton.interactable = false;
            // 퇴근 버튼 활성화
            getOffWorkButton.interactable = true;
        }

        else
        {
            workStatusText.text = "Work Status: <color=red>Incomplete</color>";

            workStartButton.interactable = false;
            getOffWorkButton.interactable = false;
        }
    }

    // 책상 근처에 들어가거나 나올 떄 외부에서 호출될 함수
    public void SetWorkStartButtonInteractable (bool isNearDesk)
    {
        if (DateTimeManager.Instance.isWorkCompleted) return;

        workStartButton.interactable = isNearDesk;
    }

}
