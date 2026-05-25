using System;

public enum ProjectScale
{
    Small,
    Medium,
    Large,
}

public enum ProjectGrade
{
    None,
    S, // 갓겜    전직원 충성도 +30, 보너스 자금
    A, // 인기작  전직원 충성도 +10
    B, // 평작
    C, // 망겜    전직원 충성도 -5
}

// 진행중인 프로젝트의 상태 데이터
[Serializable]
public class Project
{
    public ProjectSO so;
    public int Id => so.id;

    public int day;      // 현재 진행 일수

    // 투입된 직원
    public Employee[] plannings;
    public Employee[] develops;
    public Employee[] arts;

    public float progress; // 진행도
    // 진행도 연관 수치
    public float qualityScore;   // 완성도 점수 (기획)
    public float stabilityScore; // 안정성 점수 (개발)
    public float charmScore;     // 매력도 점수 (아트)


    // 이벤트 발생으로 인한 수치 변화
    public int   weeklyPlanningCount;
    public float weeklyPlanningWeight;
    public int   weeklyDevelopCount;
    public float weeklyDevelopWeight;
    public int   weeklyArtCount;
    public float weeklyArtWeight;
}
