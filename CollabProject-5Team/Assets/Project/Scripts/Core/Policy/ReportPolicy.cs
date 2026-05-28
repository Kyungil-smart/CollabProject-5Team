// 각종 보고서 계산 정책 모음
public static class ReportPolicy
{
    // 직원 능력치로 보고서 점수 산출
    public static float CalcScore(EmployeeMutableData d)
    {
        float baseScore = (d.property1 + d.property2 + d.property3) / 3f;

        float motivBonus = d.motivation >= 80 ?  5f :     // 의욕에 따른 보너스 계산
                           d.motivation >= 40 ?  0f : -10f;

        return baseScore + motivBonus;
    }

    // 점수로 보고서 등급 결정
    public static ReportGrade CalcGrade(float score)
    {
        if (score >= 70f) return ReportGrade.INNOVATION;// 1등급
        if (score >= 45f) return ReportGrade.STANDARD; // 2등급
        return ReportGrade.SLOPPY;                    // 3등급
    }

    // 승인된 보고서가 파트 점수에 기여하는 값 계산
    public static float CalcContribution(Report report, Employee owner)
    {
        // 계산 공식을 다시 짜야함..


        return 0;
    }
}
