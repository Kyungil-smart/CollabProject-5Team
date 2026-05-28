using System.Collections.Generic;
using UnityEngine;

// 각종 보고서 계산 정책 모음
public static class ReportPolicy
{
    // 직원 능력치로 보고서 점수 산출
    public static float CalcScore(EmployeeMutableData d)
    {
        float baseScore = (d.property1 + d.property2 + d.property3) / 3f;

        float motivBonus = d.desire >= 80 ? 5f :     // 의욕에 따른 보너스 계산
                           d.desire >= 40 ? 0f : -10f;

        return baseScore + motivBonus;
    }

    // 보고서 점수로 보고서 등급 결정 (1=INNOVATION, 2=STANDARD, 3=SLOPPY)
    public static int CalcGrade(float score)
    {
        if (score >= 70f) return 1;
        if (score >= 45f) return 2;
        return 3;
    }

    // 승인된 보고서의 기여값 계산 + 직원 bonus 실제 반영
    // finalScore(파트 평균)를 반환하고, 직원의 bonus1~3을 업데이트한다
    public static float CalcContribution(Report report, Dictionary<TraitStat, float> prevStatScores)
    {
        Employee e = report.owner;
        Role role = e.so.role;
        EmployeeMutableData d = e.MutableData;

        // 역할에 해당하는 TraitStat 3개를 property1~3과 매핑
        TraitStat[] stats = GetRoleStats(role);
        var effectScores = new Dictionary<TraitStat, float>
        {
            [stats[0]] = d.property1,
            [stats[1]] = d.property2,
            [stats[2]] = d.property3,
        };

        // 등급별 delta 결정
        int mainDelta = report.grade == 1 ? 12 : report.grade == 2 ? 8 : 4;
        int subDelta  = report.grade == 1 ?  6 : report.grade == 2 ? 4 : 2;
        int riskDelta = report.grade == 1 ? -6 : report.grade == 2 ? -4 : -2;

        // 대표/보조/리스크 특성이 영향 주는 stat에 delta 적용
        ApplyTraitDelta(effectScores, e.so.mainTrait, mainDelta);
        ApplyTraitDelta(effectScores, e.so.subTrait,  subDelta);
        ApplyTraitDelta(effectScores, e.so.riskTrait, riskDelta);

        // 각 stat에 최종 공식 적용: A + (100 - A) × (effectScore / 100)
        //  A = 지난 주 기존 점수
        int baseProperty = PerkPolicy.CalcBaseProperty(d.ability);
        float total = 0f;
        float[] finalEffects = new float[stats.Length];
        for (int i = 0; i < stats.Length; i++)
        {
            //float A = prevStatScores.TryGetValue(stats[i], out float prev) ? prev : 0f;
            float A = 0f; // 임시
            float finalEffect = A + (100f - A) * (effectScores[stats[i]] / 100f);
            finalEffects[i] = finalEffect;
            total += finalEffect;
        }

        // 이번 주 기여 결과를 bonus 환산하여 직원 스텟에 실제 반영
        int db1 = Mathf.RoundToInt(finalEffects[0] - d.property1);
        int db2 = Mathf.RoundToInt(finalEffects[1] - d.property2);
        int db3 = Mathf.RoundToInt(finalEffects[2] - d.property3);
        e.UpdateBonus(db1, db2, db3);

        // 파트의 TraitStat 3개 평균 반환
        return total / stats.Length;
    }

    // 특성의 영향을 주는 스텟에 delta 적용
    static void ApplyTraitDelta(Dictionary<TraitStat, float> effectScores, Trait trait, int delta)
    {
        TraitData data = TraitTable.Get(trait);
        foreach (TraitStat stat in data.affectedStats)
        {
            if (effectScores.ContainsKey(stat))
                effectScores[stat] += delta;
        }
    }

    // 역할에 해당하는 TraitStat 배열 반환 (순서: property1, 2, 3)
    public static TraitStat[] GetRoleStats(Role role) => role switch
    {
        Role.PLANNER     => new[] { TraitStat.Creativity, TraitStat.Fun,       TraitStat.Precision    },
        Role.PROGRAMMER  => new[] { TraitStat.BugControl, TraitStat.TechPower, TraitStat.Optimize     },
        Role.ARTIST      => new[] { TraitStat.Visual,     TraitStat.Direction, TraitStat.Composition  },
        _ => new TraitStat[0],
    };

    // 보고서 등급에 따른 직원 피로도 증가 적용 (1=INNOVATION: +5, 2/3: +15)
    public static void ApplyFatigue(Employee e, int grade)
    {
        int delta = grade == 1 ? 5 : 15;
        e.MutableData.fatigue += delta;
    }
}

