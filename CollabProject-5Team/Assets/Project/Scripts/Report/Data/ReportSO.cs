using UnityEngine;

// 직군별 보고서 데이터 (SO)
[CreateAssetMenu(fileName = "Report_", menuName = "Scriptable Objects/ReportSO")]
public class ReportSO : SheetDataSOBase
{
    public string title;  // 보고서 제목
    public Trait  trait;  // 표시 특성
    public int startRepo; // 1 = 1주차 보고서, 0 = 이후 랜덤 적용 보고서
    public int    grade;
    public Role   role;
    [TextArea] public string content;
    public string uiCategory;

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        title = data[1].Trim();
        trait = ParseKoreanTrait(data[2]);
        startRepo = ParseInt(data[3]);
        grade = ParseInt(data[4]);
        role = ParseEnum<Role>(data[5]);
        content = data[6];
        uiCategory = data[7];
    }
}
