using UnityEngine;

/// <summary> 초기 프로젝트 값을 담은 SO </summary>
[CreateAssetMenu(fileName = "ProjectSO", menuName = "Scriptable Objects/ProjectSO")]
public class ProjectSO : ScriptableObject// 프로젝트 시트가 있나?
{
    [Header("[ 프로젝트 정보 ]")]
    public string projectName;      // 이름
    [TextArea] public string desc;  // 설명

    public ProjectScale scale;      // 규모
    public int requiredCost;        // 개발비 => 규모에 따라 자동??
    //public int maxEmployeePerPart;  // 파트별 최대 투입 인원 => 규모에 따라 자동
    //public int durationWeeks;       // 개발 기간 (주 단위) => 규모에 따라 자동?

    //public float goalScore; // 목표 점수 => 프로젝트 규모에 따라 자동??

    // 추후 이미지등 추가 가능
}
