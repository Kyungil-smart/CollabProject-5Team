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
        data.Rating          = CalcRating(data.qualityScore, data.stabilityScore, data.charmScore);
        data.RetentionFactor = 1f;
        data.users           = CalcUsers(data.scale, data.qualityScore, prevUsers: 0);
        data.goodsSales      = CalcGoodsSales(data.charmScore);
        data.dailySales      = CalcDailySales(data.scale, data.Rating, data.RetentionFactor, companyPopularity);
        data.dailyGold       = CalcDailyGold(data.scale, data.dailySales, data.goodsSales);
        data.dailyCost       = CalcWeeklyCost(data.scale);
    }

    // ─ 유지력 계수 ─
    public const float RETENTION_DECAY  = 0.05f; // 매주 감소
    public const float RETENTION_UPDATE = 0.2f;  // 업데이트 승인 시 회복

    // - 프로젝트 평점 -
    /// <summary>평점 = (완성도/100)*2 + (안정성/100)*2 + (매력도/100), 클램프 0.5~5</summary>
    public static float CalcRating(float quality, float stability, float charm)
    {
        float raw = (quality / 100f) * 2f + (stability / 100f) * 2f + (charm / 100f);
        return raw;
    }

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
    /// 일일 판매량 = 기본판매량 * (1 + 회사인기/100) * (평점/5)^2 * 유지력계수
    /// </summary>
    public static int CalcDailySales(ProjectSize size, float rating, float retentionFactor, int companyPopularity)
    {
        float sales = BaseSales(size)
                    * (1f + companyPopularity / 100f)
                    * Mathf.Pow(rating / 5f, 2f)
                    * retentionFactor;
        return Mathf.RoundToInt(sales);
    }

    // - 매출 가중치 (gold) ─
    const int SMALL_FACTOR  = 1000;
    const int MEDIUM_FACTOR = 3000;
    const int LARGE_FACTOR  = 6000;
    static int SalesFactor(ProjectSize size) => size switch
    {
        ProjectSize.medium => MEDIUM_FACTOR,
        ProjectSize.large  => LARGE_FACTOR,
        _                  => SMALL_FACTOR,
    };

    // ─ 굿즈 ─
    const float GOODS_CHARM_THRESHOLD = 50f;

    /// <summary>굿즈 판매량 = (매력도 - 50) * 100, 매력도 50 미만이면 0</summary>
    public static int CalcGoodsSales(float charm)
    {
        if (charm < GOODS_CHARM_THRESHOLD) return 0;
        return Mathf.RoundToInt((charm - GOODS_CHARM_THRESHOLD) * 100f);
    }

    /// <summary>
    /// 일일 매출 = 일일판매량 * 규모별가중치 + 굿즈판매량 * 굿즈 가격(=규모별가중치)
    /// </summary>
    public static int CalcDailyGold(ProjectSize size, int dailySales, int goodsSales)
        => dailySales * SalesFactor(size) + goodsSales * SalesFactor(size);

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

    // ─ 주간 정산 (금요일 밤마다) ─
    /// <summary>
    /// 주간 정산: 유지력 감소 → 유저수 재계산 → 판매량/매출 재계산
    /// prevWeek* 에 이번 주 수치를 저장한 뒤 갱신한다.
    /// </summary>
    public static void TickWeeklyStats(ProjectCompleted data)
    {
        // 이번 주 수치를 지난 주로 백업
        data.prevWeekUsers = data.users;
        data.prevWeekGold  = data.weeklyGoldAccum;

        // 히스토리에 이번 주 누적 매출 push
        data.QueueWeeklyGold(data.weeklyGoldAccum);
        data.weeklyGoldAccum = 0;

        // 유저수 재계산 (이탈자 반영)
        data.users = CalcUsers(data.scale, data.qualityScore, data.prevWeekUsers);
    }
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
    public const int PENALTY_LOW_RATING  = -5;  // 평점 2점 이하 프로젝트 존재시 매주
    public const int PENALTY_SPY_FAIL    = -10; // 스파이 행위 적발 실패 시
    public const int PENALTY_DEFICIT_HIT = -20; // 회사 자금 적자 시 즉시
    public const int PENALTY_DEFICIT_WEEK= -10; // 적자 유지 주차마다
    #endregion
}
