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

    // 의욕 상태에 따른 보고서내용들
    public string contentNormal;
    public string contentHigh;
    public string contentLow;

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        title = data[1].Trim();
        trait = ParseEnum<Trait>(data[2].Trim());
        grade = ParseInt(data[3]);
        role = ParseEnum<Role>(data[4].Trim());
        contentNormal = data[5].Trim();
        contentHigh = data[6].Trim();
        contentLow = data[7].Trim();
    }
}
