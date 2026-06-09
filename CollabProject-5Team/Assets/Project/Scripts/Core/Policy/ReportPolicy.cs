using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 각종 보고서 계산 정책 모음
public static class ReportPolicy
{
    // 보고서 등급 결정용 점수 계산
    // 직원 기본 점수(20 + ability * 0.5) + 의욕 가중치
    public static float CalcScore(EmployeeImmutableData so, int desire)
    {
        float baseScore = PerkPolicy.CalcBaseProperty(so.ability);

        float motivBonus = desire >= 80 ? 5f :
                           desire >= 40 ? 0f : -10f;

        return baseScore + motivBonus;
    }

    // 보고서 점수로 등급 결정
    public static int CalcGrade(float score)
    {
        if (score >= 70f) return 1;
        if (score >= 45f) return 2;
        return 3;
    }

    // 직원 배열을 기반으로 보고서 초안 생성 → project.pendingReports에 추가
    public static void GenerateReportForRole(Project project, Employee[] employees)
    {
        foreach (Employee e in employees)
        {
            if (e == null) continue;

            float score = CalcScore(e.so, e.MutableData.desire);
            int grade   = CalcGrade(score);

            ReportSO picked = ReportManager.Instance.GetReportsByTrait(e, grade);
            if (picked == null) { Debug.LogWarning($"[ReportPolicy] {e.so.Name} 에 맞는 보고서 SO 없음"); continue; }

            project.pendingReports.Add(new Report { so = picked, owner = e});
        }
    }

    // 이번 주 stat별 최종 점수 계산 (매 주차 독립)
    // 직원 기본점수 + 특성 delta + 특성 가중치
    // 반환값: stats 순서에 맞는 float[3]
    public static float[] CalcWeeklyStatScores(Report report)
    {
        Employee e = report.owner;
        EmployeeMutableData d = e.MutableData;
        TraitStat[] stats = GetRoleStats(e.so.role);

        var scores = new Dictionary<TraitStat, float>
        {
            [stats[0]] = d.ability,
            [stats[1]] = d.ability,
            [stats[2]] = d.ability,
        };

        // 등급별 특성 delta (대표/보조/리스크)
        int mainDelta = report.grade == 1 ? 12 : report.grade == 2 ? 8 : 4;
        int subDelta  = report.grade == 1 ?  6 : report.grade == 2 ? 4 : 2;
        int riskDelta = report.grade == 1 ? -6 : report.grade == 2 ? -4 : -2;

        // 값 적용 (특성 점수 + 가중치)
        ApplyTraitDelta(scores, e.so.mainTrait, mainDelta);
        ApplyTraitDelta(scores, e.so.subTrait,  subDelta);
        ApplyTraitDelta(scores, e.so.riskTrait, riskDelta);

        // 0~100 클램프 후 결과 배열 반환
        float[] result = new float[stats.Length];
        for (int i = 0; i < stats.Length; i++)
            result[i] = Mathf.Clamp(scores[stats[i]], 0f, 100f);

        return result;
    }

    // 특성이 영향을 주는 stat에 delta 적용
    static void ApplyTraitDelta(Dictionary<TraitStat, float> scores, Trait trait, int delta)
    {
        foreach (TraitStat stat in TraitTable.Get(trait).affectedStats)
        {
            if (scores.ContainsKey(stat))
                scores[stat] += delta;
        }
    }

    // 역할에 해당하는 TraitStat 배열 반환 (순서: stat1, stat2, stat3)
    public static TraitStat[] GetRoleStats(Role role) => role switch
    {
        Role.PLANNER    => new[] { TraitStat.Fun,       TraitStat.Creativity, TraitStat.Precision   },
        Role.PROGRAMMER => new[] { TraitStat.TechPower, TraitStat.Optimize,   TraitStat.BugControl  },
        Role.ARTIST     => new[] { TraitStat.Visual,    TraitStat.Direction,  TraitStat.Composition },
        _               => new TraitStat[0],
    };

    // 보고서 등급에 따른 피로도 증가
    public static void ApplyFatigue(Employee e, int grade)
    {
        e.MutableData.fatigue += grade == 1 ? 10 : 20;
    }
}
