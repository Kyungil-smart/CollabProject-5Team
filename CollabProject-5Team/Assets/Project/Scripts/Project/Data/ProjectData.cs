using System;

public enum ProjectSize
{
    small,
    medium,
    large,
}

[Serializable]
public class ProjectCompleted
{
    // 이전 데이터 연동
    public int projectID;
    public string projectName;
    public ProjectSize scale;
    public int qualityScore;
    public int stabilityScore;
    public int charmScore;

    // 완료 데이터
    public char grade;
    public int popularity; // 평판
    public int dailyCost; // 유지비
    public int dailyProfit; // 데일리 캐시

    // 플래그
    public bool isReady; // false면 출시 대기상태
}
