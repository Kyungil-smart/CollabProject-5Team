using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 각종 보고서 계산 정책 모음
public static class ReportPolicy
{
    const int MaxMissingReportWarnings = 30;
    static readonly HashSet<string> MissingReportWarningKeys = new();

    // 보고서 등급 결정용 점수 계산
    // 직원 기본 점수(20 + ability * 0.5) + 의욕 가중치
    public static float CalcScore(EmployeeImmutableData so, int desire)
    {
        float baseScore = PerkPolicy.CalcBaseProperty(so.ability);

        float motivBonus = desire >= 80 ? 5f :
                           desire >= 40 ? 0f : -5f;

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
    public static void GenerateReportForRole(Project project, IEnumerable<Employee> employees)
    {
        foreach (Employee e in employees)
        {
            float score = CalcScore(e.so, e.MutableData.desire);
            int grade   = CalcGrade(score);
            int isStartRepo = project.day <= 5 ? 1 : 0;

            ReportSO picked = ReportManager.Instance.GetReportsByTrait(e, grade, isStartRepo);
            if (picked == null)
            {
                LogMissingReportOnce(e, grade, isStartRepo);
                continue;
            }

            Report report = new Report { so = picked, owner = e };
            
            project.pendingReports.Add(report);
        }
    }

    static void LogMissingReportOnce(Employee e, int grade, int startRepo)
    {
        string key = $"{e.so.id}:{e.so.mainTrait}:{e.so.riskTrait}:{e.so.subTrait}:{grade}:{startRepo}";
        if (!MissingReportWarningKeys.Add(key))
            return;

        if (MissingReportWarningKeys.Count <= MaxMissingReportWarnings)
        {
            Debug.LogWarning(
                $"[ReportPolicy] {e.so.Name} 에 맞는 보고서 SO 없음. "
                + $"grade={grade}, startRepo={startRepo}, "
                + $"traits={e.so.mainTrait}/{e.so.riskTrait}/{e.so.subTrait}");
            return;
        }

        if (MissingReportWarningKeys.Count == MaxMissingReportWarnings + 1)
            Debug.LogWarning("[ReportPolicy] 보고서 SO 누락 Warning이 많아 이후 동일 계열 로그를 생략합니다. 상세 검증은 Prototype QA 창에서 확인하세요.");
    }

    // 이번 주 stat별 최종 점수 계산 (매 주차 독립)
    // 직원 기본점수 + 특성 delta + 특성 가중치
    // 반환값: stats 순서에 맞는 float[3]
    public static float[] CalcWeeklyStatScores(Report report)
    {
        Employee e = report.owner;
        EmployeeMutableData d = e.MutableData;
        TraitStat[] stats = GetRoleStats(e.so.role);

        int ability = CalcLoyaltyAdjustedAbility(d.ability, d.loyalty);
        var scores = new Dictionary<TraitStat, float>
        {
            [stats[0]] = ability,
            [stats[1]] = ability,
            [stats[2]] = ability,
        };

        // 등급별 특성 delta (대표/보조/리스크)
        int mainDelta = report.grade == 1 ? 15 : report.grade == 2 ? 11 : 7;
        int subDelta  = report.grade == 1 ?  7 : report.grade == 2 ? 5 : 3;
        int riskDelta = report.grade == 1 ? -8 : report.grade == 2 ? -6 : -4;

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

    public static int CalcLoyaltyAdjustedAbility(int ability, int loyalty)
    {
        float rate = loyalty >= 81 ? 1.1f :
                     loyalty >= 61 ? 1.05f :
                     loyalty >= 41 ? 1.0f :
                     loyalty >= 21 ? 0.95f : 0.9f;

        return Mathf.Clamp((int)(ability * rate), 0, 100);
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
    public static void ApplyHighFatigueSelectionPenalty(Employee e)
    {
        if (e.MutableData.fatigue < 80) return;

        e.MutableData.desire -= 20;
        e.MutableData.loyalty -= 20;
    }

    public static void ApplyAcceptedFatigue(Employee e, int grade)
    {
        e.MutableData.fatigue += CalcAcceptedFatigueDelta(grade);
    }

    public static void ApplyRejectedFatigue(Employee e)
    {
        e.MutableData.fatigue -= 5;
    }

    static int CalcAcceptedFatigueDelta(int grade) => grade switch
    {
        1 => 5,
        2 => Random.value < 0.5f ? 5 : 10,
        _ => 10,
    };
}
