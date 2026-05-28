using UnityEngine;

// 직군별 보고서 데이터 (SO)
[CreateAssetMenu(fileName = "Report_", menuName = "Scriptable Objects/ReportSO")]
public class ReportSO : SheetDataSOBase
{
    public Part        part;
    public ReportGrade grade;
    public Trait   mainTrait; // 대표 특성

    public string title;
    public string desc;
    //...

    public override void SetData(string[] data)
    {
        // 시트가 나오면 완성...
    }
}
