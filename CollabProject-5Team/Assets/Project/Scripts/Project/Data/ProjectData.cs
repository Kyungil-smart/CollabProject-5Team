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

[Serializable]
public class ProjectCompleted
{
    // 이전 데이터 연동
    public int projectID;
    public string projectName;
    public ProjectScale scale;
    public int qualityScore;
    public int stabilityScore;
    public int charmScore;

    // 완료 데이터
    public ProjectGrade grade;
    public int popularity;
    public int dailyCost; // 유지비
    public int dailyProfit; // 데일리 캐시
}
