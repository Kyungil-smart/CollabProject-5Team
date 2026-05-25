using UnityEngine;

/// <summary> 초기 프로젝트 값을 담은 SO </summary>
[CreateAssetMenu(fileName = "ProjectSO", menuName = "Scriptable Objects/ProjectSO")]
public class ProjectSO : SheetDataSOBase
{
    [Header("[ 프로젝트 정보 ]")]
    public string Name;      // 이름
    [TextArea] public string desc;  // 설명

    public ProjectScale scale;      // 규모 => 아래 값들 연관
    public int requiredCost;        // 개발비 
    public int maxEmployeePerPart;  // 파트별 최대 투입 인원
    public int durationDays;        // 개발 기간 (일 단위)

    public float goalScore; // 목표 점수

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        Name = data[1];
        // 시트가 나오면 완성...
    }
}
