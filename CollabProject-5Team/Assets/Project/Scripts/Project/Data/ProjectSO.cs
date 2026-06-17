using UnityEngine;

/// <summary> 초기 프로젝트 값을 담은 SO </summary>
[CreateAssetMenu(fileName = "ProjectSO", menuName = "Scriptable Objects/ProjectSO")]
public class ProjectSO : ScriptableObject
{
    [Header("[ 프로젝트 정보 ]")]
    public string Name;      // 이름

    public ProjectSize scale;      // 규모 => 아래 값들 연관
    public int requiredCost;        // 개발비 
    public int maxEmployeePerPart;  // 파트별 최대 투입 인원
    public int durationDays;        // 개발 기간 (영업일 단위)
}
