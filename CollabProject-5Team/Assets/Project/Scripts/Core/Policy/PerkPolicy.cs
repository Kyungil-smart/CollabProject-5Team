using UnityEngine;

// 직원 업그레이드/성장 정책을 관리하는 정적 클래스
public static class PerkPolicy
{
    // grade별 주 능력치 초기값 (기획변경으로 주석처리)
    //public static int InitStatFromGrade(int grade) => grade switch
    //{
    //    1 => 30,
    //    2 => 20,
    //    3 => 10,
    //    _ =>  5,
    //};

    // 세부 능력치 기본값: 20 + stat * 0.5
    public static int CalcBaseProperty(int stat) => 20 + Mathf.RoundToInt(stat * 0.5f);

    // - 세부 능력치 최종값: 기본값 + 추가 변동값
    // bonus: 특성·이벤트·장비 등으로 인한 추가 변동
    public static int CalcFinalProperty(int stat, int bonus)
        => CalcBaseProperty(stat) + bonus;
}
