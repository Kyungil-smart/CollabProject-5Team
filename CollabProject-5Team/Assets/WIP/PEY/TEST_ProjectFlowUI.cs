using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 첫 프로젝트 흐름을 테스트하기위한 임시 UI
public class TEST_ProjectFlowUI : MonoBehaviour
{
    [Header(" UI 연결 ")]
    [SerializeField] TMP_InputField projectNameInput;
    [SerializeField] TMP_Text projectNameText;
    [SerializeField] Slider progressDayBar;
    [SerializeField] Slider progressBar;
    [SerializeField] TMP_Text dayText;
    [SerializeField] Button dayPlusButton;

    static readonly Color NormalDayColor = Color.white;
    static readonly Color ExpiredDayColor = Color.red;

    TEST_ReportUI _reportUI; // 밤 이벤트 후 연결
    void Start()
    {
        _reportUI = GetComponent<TEST_ReportUI>();

        var company = Company.Instance;
        var project = company.curProject;

        // Day 텍스트: Company.day 변경 시 자동 갱신
        company.day
            .Subscribe(d => dayText.text = $"Day {d}")
            .AddTo(this);

        // Day+ 버튼: ProgressDay 후 밤 이벤트(5일 배수)면 보고서 UI 시작
        dayPlusButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                company.ProgressDay();
                progressDayBar.value = project.ProgressDayBar;

                // ProgressDay 내부에서 day%5==0이면 ProgressNight에 GenerateReportDrafts까지 호출됨
                // 임시로 ProgressDay 후 바로 보고서 UI 시작하도록 함
                if (company.day.Value % 5 == 0)
                    _reportUI?.StartReportFlow();
            })
            .AddTo(this);


        // 프로젝트 이름 표시
        project.userNamed
            .Subscribe(n => projectNameText.text = n)
            .AddTo(this);

        // InputField → project.userNamed 변경
        projectNameInput.onValueChanged.AsObservable()
            .Subscribe(v => project.userNamed.Value = v)
            .AddTo(this);

        // 진행도 표시: Project.progress 변경 시
        //project.curScore
        //    .Subscribe(_ =>
        //    {
        //        progressBar.value = project.ProgressBar;
        //    })
        //    .AddTo(this);

        // 프로젝트 종료: Day 텍스트 빨강 & 버튼 비활성화
        project.isFinished
            .Subscribe(finished =>
            {
                dayText.color = finished ? ExpiredDayColor : NormalDayColor;
                dayPlusButton.interactable = !finished;
            })
            .AddTo(this);
    }
}
