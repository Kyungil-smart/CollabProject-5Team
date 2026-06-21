using UnityEngine;

// 직원/회사/완료프로젝트 업그레이드/성장 정책을 관리하는 정적 클래스
public static class PerkPolicy
{
    #region 직원 파트
    // 직원 세부 능력치 기본값: 20 + ability * 0.5
    public static int CalcBaseProperty(int ability) => 20 + Mathf.RoundToInt(ability * 0.5f);

    public static int CalcWeeklyAbilityDelta(int loyalty)
    {
        if (loyalty >= 81) return 5;
        if (loyalty >= 61) return 3;
        if (loyalty >= 41) return 2;
        if (loyalty >= 21) return 1;
        return 0;
    }

    public static int CalcCompletionAbilityDelta(ProjectSize size, char grade)
    {
        return size switch
        {
            ProjectSize.medium => grade switch
            {
                'S' => 12,
                'A' => 9,
                'B' => 6,
                _ => 2,
            },
            ProjectSize.large => grade switch
            {
                'S' => 18,
                'A' => 14,
                'B' => 9,
                _ => 4,
            },
            _ => grade switch
            {
                'S' => 8,
                'A' => 6,
                'B' => 4,
                _ => 1,
            },
        };
    }

    public static int CalcCompletionLoyaltyDelta(ProjectSize size, char grade)
    {
        return size switch
        {
            ProjectSize.medium => grade switch
            {
                'S' => 6,
                'A' => 3,
                'B' => -1,
                _ => -3,
            },
            ProjectSize.large => grade switch
            {
                'S' => 10,
                'A' => 6,
                'B' => -2,
                _ => -6,
            },
            _ => grade switch
            {
                'S' => 4,
                'A' => 2,
                'B' => -1,
                _ => -2,
            },
        };
    }
    #endregion

    #region 완료 프로젝트
    /// <summary>
    /// 프로젝트 완료 시 ProjectCompleted의 계산할 필드들을 정산
    /// </summary>
    public static void InitCompletedStats(ProjectCompleted data, int companyPopularity)
    {
        data.RetentionFactor = 1f;
        data.users           = CalcUsers(data.scale, data.qualityScore, prevUsers: 0);
        data.dailySales      = CalcDailySales(data.scale, data.qualityScore, data.stabilityScore, data.charmScore, data.RetentionFactor, companyPopularity);
        data.dailyGold       = CalcDailyGold(data.scale, data.dailySales);
        data.dailyCost       = CalcWeeklyCost(data.scale);
    }

    // ─ 유지력 계수 ─
    public const float RETENTION_DECAY  = 0.05f; // 매주 감소
    public const float RETENTION_UPDATE = 0.2f;  // 업데이트 승인 시 회복

    // - 유저수 ─
    const int SMALL_USERS  = 500; // 기본 유저수
    const int MEDIUM_USERS = 1000;
    const int LARGE_USERS  = 2000;
    static int BaseUsers(ProjectSize size) => size switch
    {
        ProjectSize.medium => MEDIUM_USERS,
        ProjectSize.large  => LARGE_USERS,
        _                  => SMALL_USERS,
    };

    /// <summary>
    /// 유저수 = 기본유저수 + (프로젝트점수*10) - 누적이탈자
    /// 누적이탈자 = prevUsers / 10  (첫 주는 0)
    /// </summary>
    public static int CalcUsers(ProjectSize size, float projectScore, int prevUsers)
    {
        int attrition = Mathf.RoundToInt(prevUsers / 10f);
        int users = BaseUsers(size) + Mathf.RoundToInt(projectScore * 10f) - attrition;
        return users;
    }

    // - 판매량 ─
    const int SMALL_BASE_SALES  = 100;
    const int MEDIUM_BASE_SALES = 500;
    const int LARGE_BASE_SALES  = 2500;
    static int BaseSales(ProjectSize size) => size switch
    {
        ProjectSize.medium => MEDIUM_BASE_SALES,
        ProjectSize.large  => LARGE_BASE_SALES,
        _                  => SMALL_BASE_SALES,
    };


    /// <summary>
    /// 일일 판매량 = 일일 판매 지수 * (완성도 가중치 + 안정성 가중치 + 매력도 가중치)
    /// </summary>
    public static int CalcDailySales(ProjectSize size, float quality, float stability, float charm, float retentionFactor, int companyPopularity)
    {
        float dailySalesIndex = BaseSales(size)
                              * (1f + companyPopularity / 100f)
                              * retentionFactor;

        float sales = dailySalesIndex * CalcScoreWeight(quality)
                    + dailySalesIndex * CalcScoreWeight(stability)
                    + dailySalesIndex * CalcScoreWeight(charm);

        return Mathf.RoundToInt(sales);
    }

    // -점수 가중치 (처음값: 50)
    const float SCORE_WEIGHT_BASELINE = 50f;

    public static float CalcScoreWeight(float score)
        => Mathf.Max(0f, (score - SCORE_WEIGHT_BASELINE) / 100f);

    // - 매출 가중치 (gold) ─
    const int SMALL_FACTOR  = 1000;
    const int MEDIUM_FACTOR = 1500;
    const int LARGE_FACTOR  = 2000;
    static int SalesFactor(ProjectSize size) => size switch
    {
        ProjectSize.medium => MEDIUM_FACTOR,
        ProjectSize.large  => LARGE_FACTOR,
        _                  => SMALL_FACTOR,
    };

    /// <summary>
    /// 일일 매출 = 일일 판매량 * 프로젝트 규모별 금액
    /// </summary>
    public static int CalcDailyGold(ProjectSize size, int dailySales)
        => dailySales * SalesFactor(size);

    // ─ 유지비 ─
    const int SMALL_COST  = 100;
    const int MEDIUM_COST = 1000;
    const int LARGE_COST  = 10000;
    /// <summary>규모별 주간 유지비</summary>
    public static int CalcWeeklyCost(ProjectSize size) => size switch
    {
        ProjectSize.medium => MEDIUM_COST,
        ProjectSize.large  => LARGE_COST,
        _                  => SMALL_COST,
    };
    #endregion

    #region 회사 파트
    // ─ 인기 ─
    // 인기 : 프로젝트 출시 종료 시 최종등급에 따라 변동, 상한선 제한 없음
    /// <summary>프로젝트 완료 등급에 따른 회사 인기 변동량</summary>
    public static int CalcPopularityDelta(char grade) => grade switch
    {
        'S' => 20,
        'A' => 10,
        'B' => 5,
        _   => -20,
    };

    // ─ 평판 ─
    // 평판 : 0부터 시작, 감소 가능
    /// <summary>주간 판매량 100장 돌파 시마다 +1</summary>
    public static int CalcReputationGainFromSales(int weeklySales)
        => weeklySales / 100;

    // 평판 감소 상수
    public const int PENALTY_SPY_FAIL    = -10; // 스파이 행위 적발 실패 시
    public const int PENALTY_DEFICIT_HIT = -20; // 회사 자금 적자 시 즉시
    public const int PENALTY_DEFICIT_WEEK= -10; // 적자 유지 주차마다
    #endregion
}
