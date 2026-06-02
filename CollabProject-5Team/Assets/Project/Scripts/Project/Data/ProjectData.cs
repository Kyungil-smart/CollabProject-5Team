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
    public int popularity; // 평점
    public int dailyCost; // 유지비
    public int dailyProfit; // 데일리 캐시

}
