using UnityEngine;

// 주요 업그레이드/성장 정책을 관리하는 정적 클래스
public static class PerkPolicy
{
    // Rank별 주 능력치 초기값
    public static int InitStatFromRank(Rank rank) => rank switch
    {
        Rank.S => 30,
        Rank.A => 20,
        Rank.B => 10,
        _      =>  5,
    };

    // 세부 능력치 기본값: 20 + stat * 0.5
    public static int CalcBaseProperty(int stat) => 20 + Mathf.RoundToInt(stat * 0.5f);

    // - 세부 능력치 최종값: 기본값 + 추가 변동값
    // bonus: 특성·이벤트·장비 등으로 인한 추가 변동
    public static int CalcFinalProperty(int stat, int bonus)
        => CalcBaseProperty(stat) + bonus;
}
