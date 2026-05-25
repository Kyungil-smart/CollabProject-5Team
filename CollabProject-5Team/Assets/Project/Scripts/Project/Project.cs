using UnityEngine;

// 진행중인 프로젝트의 상태 데이터
public class Project : MonoBehaviour
{
    public ProjectSO so;
    public int Id => so.id;

    public int day;      // 현재 진행 일수 (영업일 기준)

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
