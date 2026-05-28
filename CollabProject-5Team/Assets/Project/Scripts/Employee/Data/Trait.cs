using System;
using System.Collections.Generic;
using UnityEngine;

// ── 특성이 속한 파트
public enum TraitPart
{
    Planning, // 기획자 전용
    Develop,  // 프로그래머 전용
    Art,      // 아티스트 전용
}

// ── 영향 항목 (직원 프로퍼티1~3에 반영되는 수치)
public enum TraitStat
{
    // PLANNER
    Creativity,  // 창의성
    Fun,         // 재미
    Precision,   // 정교함

    // PROGRAMMER
    BugControl,  // 버그 제어
    TechPower,   // 기술력
    Optimize,    // 최적화

    // ARTIST
    Visual,      // 비주얼
    Direction,   // 연출력
    Composition, // 구도
}

// ── 긍정/부정
public enum TraitPolarity { Positive, Risk }

// ── 특성 고유 ID
public enum Trait
{
    // PLANNER - IdeaGeneration(아이디어 발상)
    IdeaBank,         // 아이디어 뱅크
    IdeaDrought,      // 아이디어 가뭄

    // PLANNER - StructureDesign(구조 설계)
    Architect,        // 구조 설계자
    StructureBreaker, // 구조 붕괴자

    // PLANNER - TrendSense(트렌드 감각)
    TrendRadar,       // 트렌드 레이더
    TrendBlind,       // 유행 불감증

    // PLANNER - BalanceControl(밸런스 조율)
    GoldenScale,      // 황금 저울
    BalanceBreaker,   // 밸런스 폭주

    // PLANNER - DataAnalysis(데이터 분석)
    NumberDetective,  // 숫자 탐정
    DataIlliterate,   // 데이터맹

    // PROGRAMMER - CodeQuality(코드 품질)
    CleanCode,        // 클린 코드
    HardCode,         // 하드 코드

    // PROGRAMMER - SystemDesign(시스템 설계)
    SystemArchitect,  // 아키텍트
    SpaghettiCook,    // 스파게티 요리사

    // PROGRAMMER - DevSpeed(개발 속도)
    FastDev,          // 빠른 개발
    SlowDev,          // 느린 개발

    // PROGRAMMER - ProblemSolving(문제 해결)
    IssueSolver,      // 이슈 해결사
    ErrorIgnorer,     // 에러 방치

    // PROGRAMMER - TechPreference(기술 성향)
    NewTechLover,     // 신기술 선호
    OldTech,          // 구식 기술

    // ARTIST - CreativeStyle(창작 성향)
    Original,         // 독창적
    Copycat,          // 카피캣

    // ARTIST - Productivity(생산성)
    Prolific,         // 다작형
    SlowStarter,      // 슬로우 스타터

    // ARTIST - Quality(완성도)
    HighQuality,      // 높은 퀄리티
    LowQuality,       // 낮은 퀄리티

    // ARTIST - Expression(표현력)
    SensoryDir,       // 감각적 연출
    FlatDir,          // 밋밋한 연출

    // ARTIST - Composition(구도 감각)
    GoldenFrame,      // 황금 구도
    Cluttered,        // 화면 산만
}

// ── 특성 데이터
[Serializable]
public class TraitData
{
    public Trait         trait;
    public string        displayName;
    public TraitPart     part;
    public TraitPolarity polarity;
    public TraitStat[]   affectedStats;

    public Color DisplayColor => polarity == TraitPolarity.Positive
        ? new Color(0.2f, 0.8f, 0.2f)
        : new Color(0.9f, 0.2f, 0.2f);
}

// ── 특성 테이블 (Get 메서드로 ID 기반 조회)
public static class TraitTable
{
    public static IReadOnlyDictionary<Trait, TraitData> All => _all;

    static readonly Dictionary<Trait, TraitData> _all = new()
    {
        // ── PLANNER ─────────────────────────────────────────
        [Trait.IdeaBank]        = T("아이디어 뱅크",  TraitPart.Planning, TraitPolarity.Positive, TraitStat.Creativity, TraitStat.Fun),
        [Trait.IdeaDrought]     = T("아이디어 가뭄",  TraitPart.Planning, TraitPolarity.Risk,     TraitStat.Creativity, TraitStat.Fun),
        [Trait.Architect]       = T("구조 설계자",    TraitPart.Planning, TraitPolarity.Positive, TraitStat.Precision,  TraitStat.Creativity),
        [Trait.StructureBreaker]= T("구조 붕괴자",    TraitPart.Planning, TraitPolarity.Risk,     TraitStat.Precision,  TraitStat.Creativity),
        [Trait.TrendRadar]      = T("트렌드 레이더",  TraitPart.Planning, TraitPolarity.Positive, TraitStat.Creativity, TraitStat.Fun),
        [Trait.TrendBlind]      = T("유행 불감증",    TraitPart.Planning, TraitPolarity.Risk,     TraitStat.Creativity, TraitStat.Fun),
        [Trait.GoldenScale]     = T("황금 저울",      TraitPart.Planning, TraitPolarity.Positive, TraitStat.Precision,  TraitStat.Fun),
        [Trait.BalanceBreaker]  = T("밸런스 폭주",    TraitPart.Planning, TraitPolarity.Risk,     TraitStat.Precision,  TraitStat.Fun),
        [Trait.NumberDetective] = T("숫자 탐정",      TraitPart.Planning, TraitPolarity.Positive, TraitStat.Precision,  TraitStat.Fun),
        [Trait.DataIlliterate]  = T("데이터맹",       TraitPart.Planning, TraitPolarity.Risk,     TraitStat.Precision,  TraitStat.Fun),

        // ── PROGRAMMER ──────────────────────────────────────────
        [Trait.CleanCode]       = T("클린 코드",       TraitPart.Develop, TraitPolarity.Positive, TraitStat.BugControl, TraitStat.TechPower),
        [Trait.HardCode]        = T("하드 코드",       TraitPart.Develop, TraitPolarity.Risk,     TraitStat.BugControl, TraitStat.TechPower),
        [Trait.SystemArchitect] = T("아키텍트",        TraitPart.Develop, TraitPolarity.Positive, TraitStat.TechPower,  TraitStat.BugControl),
        [Trait.SpaghettiCook]   = T("스파게티 요리사", TraitPart.Develop, TraitPolarity.Risk,     TraitStat.TechPower,  TraitStat.BugControl),
        [Trait.FastDev]         = T("빠른 개발",       TraitPart.Develop, TraitPolarity.Positive, TraitStat.TechPower,  TraitStat.Optimize),
        [Trait.SlowDev]         = T("느린 개발",       TraitPart.Develop, TraitPolarity.Risk,     TraitStat.TechPower,  TraitStat.Optimize),
        [Trait.IssueSolver]     = T("이슈 해결사",     TraitPart.Develop, TraitPolarity.Positive, TraitStat.BugControl, TraitStat.Optimize),
        [Trait.ErrorIgnorer]    = T("에러 방치",       TraitPart.Develop, TraitPolarity.Risk,     TraitStat.BugControl, TraitStat.Optimize),
        [Trait.NewTechLover]    = T("신기술 선호",     TraitPart.Develop, TraitPolarity.Positive, TraitStat.TechPower,  TraitStat.Optimize),
        [Trait.OldTech]         = T("구식 기술",       TraitPart.Develop, TraitPolarity.Risk,     TraitStat.TechPower,  TraitStat.Optimize),

        // ── ARTIST ──────────────────────────────────────────────
        [Trait.Original]        = T("독창적",         TraitPart.Art, TraitPolarity.Positive, TraitStat.Visual,      TraitStat.Direction),
        [Trait.Copycat]         = T("카피캣",         TraitPart.Art, TraitPolarity.Risk,     TraitStat.Visual,      TraitStat.Direction),
        [Trait.Prolific]        = T("다작형",         TraitPart.Art, TraitPolarity.Positive, TraitStat.Composition, TraitStat.Visual),
        [Trait.SlowStarter]     = T("슬로우 스타터",  TraitPart.Art, TraitPolarity.Risk,     TraitStat.Composition, TraitStat.Visual),
        [Trait.HighQuality]     = T("높은 퀄리티",    TraitPart.Art, TraitPolarity.Positive, TraitStat.Visual,      TraitStat.Composition),
        [Trait.LowQuality]      = T("낮은 퀄리티",    TraitPart.Art, TraitPolarity.Risk,     TraitStat.Visual,      TraitStat.Composition),
        [Trait.SensoryDir]      = T("감각적 연출",     TraitPart.Art, TraitPolarity.Positive, TraitStat.Direction,   TraitStat.Visual),
        [Trait.FlatDir]         = T("밋밋한 연출",     TraitPart.Art, TraitPolarity.Risk,     TraitStat.Direction,   TraitStat.Visual),
        [Trait.GoldenFrame]     = T("황금 구도",       TraitPart.Art, TraitPolarity.Positive, TraitStat.Composition, TraitStat.Direction),
        [Trait.Cluttered]       = T("화면 산만",       TraitPart.Art, TraitPolarity.Risk,     TraitStat.Composition, TraitStat.Direction),
    };

    static TraitData T(string name, TraitPart part, TraitPolarity pol,
        TraitStat stat1, TraitStat stat2) => new TraitData
        {
            displayName   = name,
            part          = part,
            polarity      = pol,
            affectedStats = new[] { stat1, stat2 },
        };

    public static TraitData Get(Trait id) => _all[id];
}
