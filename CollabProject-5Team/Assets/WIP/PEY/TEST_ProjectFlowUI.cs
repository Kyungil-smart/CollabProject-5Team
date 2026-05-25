using R3;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 첫 프로젝트 흐름을 테스트하기위한 임시 UI
public class TEST_ProjectFlowUI : MonoBehaviour
{
    [Header(" UI 연결 ")]
    [SerializeField] TMP_InputField projectNameInput;
    [SerializeField] TMP_Text projectNameText;
    [SerializeField] Slider progressBar;
    [SerializeField] TMP_Text dayText;
    [SerializeField] Button dayPlusButton;

    [Header(" 테스트 대상 ")]
    [SerializeField] int projectIndex = 0; // projects 배열 중 몇 번째 프로젝트를 표시할지

    static readonly Color NormalDayColor = Color.white;
    static readonly Color ExpiredDayColor = Color.red;
    void Start()
    {
        var company = Company.Instance;

        // Day 텍스트: Company.day 변경 시 자동 갱신
        company.day
            .Subscribe(d => dayText.text = $"Day {d}")
            .AddTo(this);

        // Day+ 버튼
        dayPlusButton.OnClickAsObservable()
            .Subscribe(_ => company.ProgressDay())
            .AddTo(this);


        var project = company.projects[projectIndex];

        // 프로젝트 이름 표시
        project.userNamed
            .Subscribe(n => projectNameText.text = n)
            .AddTo(this);

        // InputField → project.userNamed 변경
        projectNameInput.onValueChanged.AsObservable()
            .Subscribe(v => project.userNamed.Value = v)
            .AddTo(this);

        // 진행도 표시: Project.progress 변경 시
        project.progress
            .Subscribe(raw =>
            {
                float pct = project.GoalScore > 0f
                    ? Mathf.Clamp01(raw / project.GoalScore) * 100f
                    : 0f;
                progressBar.value = pct;
            })
            .AddTo(this);

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
