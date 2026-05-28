using System;

// 직군별 보고서 데이터 형식
[Serializable]
public class Report
{
    public ReportSO  so;    // 보고서 원본 SO
    public Employee  owner; // 작성 직원
    public float     score; // 산출된 직원 점수 (등급 결정 기준)
    public int       grade => so.grade; // 1=INNOVATION, 2=STANDARD, 3=SLOPPY
    public Role      role  => so.role;
}
