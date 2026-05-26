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

// ── 같은 카테고리는 중복 부여 불가
public enum TraitCategory
{
    // Planning
    IdeaGeneration,  // 아이디어 발상
    StructureDesign, // 구조 설계
    TrendSense,      // 트렌드 감각
    BalanceControl,  // 밸런스 조율
    DataAnalysis,    // 데이터 분석

    // Develop
    CodeQuality,     // 코드 품질
    SystemDesign,    // 시스템 설계
    DevSpeed,        // 개발 속도
    ProblemSolving,  // 문제 해결
    TechPreference,  // 기술 성향

    // Art
    CreativeStyle,   // 창작 성향
    Productivity,    // 생산성
    Quality,         // 완성도
    Expression,      // 표현력
    Composition,     // 구도 감각
}

// ── 영향 항목 (프로젝트 점수에 반영되는 스탯)
public enum TraitStat
{
    // Planning
    Creativity,  // 창의성
    Fun,         // 재미
    Precision,   // 정교함

    // Develop
    BugControl,  // 버그 제어
    TechPower,   // 기술력
    Optimize,    // 최적화

    // Art
    Visual,      // 비주얼
    Direction,   // 연출력
    Composition, // 구도
}

// ── 긍정/부정
public enum TraitPolarity { Positive, Negative }

// ── 특성 고유 ID (파트별 카테고리 x 2극성)
public enum TraitId
{
    // Planning - IdeaGeneration(아이디어 발상)
    IdeaBank,         // 아이디어 뱅크  +3
    IdeaDrought,      // 아이디어 가뭄  -3

    // Planning - StructureDesign(구조 설계)
    Architect,        // 구조 설계자    +3
    StructureBreaker, // 구조 붕괴자    -3

    // Planning - TrendSense(트렌드 감각)
    TrendRadar,       // 트렌드 레이더  +2
    TrendBlind,       // 유행 불감증    -2

    // Planning - BalanceControl(밸런스 조율)
    GoldenScale,      // 황금 저울      +2
    BalanceBreaker,   // 밸런스 폭주    -2

    // Planning - DataAnalysis(데이터 분석)
    NumberDetective,  // 숫자 탐정      +1
    DataIlliterate,   // 데이터맹       -1

    // Develop - CodeQuality(코드 품질)
    CleanCode,        // 클린 코드      +3
    HardCode,         // 하드 코드      -3

    // Develop - SystemDesign(시스템 설계)
    SystemArchitect,  // 아키텍트       +3
    SpaghettiCook,    // 스파게티 요리사 -3

    // Develop - DevSpeed(개발 속도)
    FastDev,          // 빠른 개발      +2
    SlowDev,          // 느린 개발      -2

    // Develop - ProblemSolving(문제 해결)
    IssueSolver,      // 이슈 해결사    +2
    ErrorIgnorer,     // 에러 방치      -2

    // Develop - TechPreference(기술 성향)
    NewTechLover,     // 신기술 선호    +3
    OldTech,          // 구식 기술      -3

    // Art - CreativeStyle(창작 성향)
    Original,         // 독창적         +3
    Copycat,          // 카피캣         -3

    // Art - Productivity(생산성)
    Prolific,         // 다작형         +2
    SlowStarter,      // 슬로우 스타터  -2

    // Art - Quality(완성도)
    HighQuality,      // 높은 퀄리티    +3
    LowQuality,       // 낮은 퀄리티    -3

    // Art - Expression(표현력)
    SensoryDir,       // 감각적 연출    +2
    FlatDir,          // 밋밋한 연출    -2

    // Art - Composition(구도 감각)
    GoldenFrame,      // 황금 구도      +1
    Cluttered,        // 화면 산만      -1
}

// ── 특성 데이터
[Serializable]
public class TraitData
{
    public TraitId       id;
    public string        displayName;
    public TraitPart     part;
    public TraitCategory category;
    public TraitPolarity polarity;
    public int           score;
    public TraitStat[]   affectedStats;

    public Color DisplayColor => polarity == TraitPolarity.Positive
        ? new Color(0.2f, 0.8f, 0.2f)
        : new Color(0.9f, 0.2f, 0.2f);
}

// ── 특성 테이블 (Get 메서드로 ID 기반 조회)
public static class TraitTable
{
    public static IReadOnlyDictionary<TraitId, TraitData> All => _all;

    static readonly Dictionary<TraitId, TraitData> _all = new()
    {
        // ── Planning ─────────────────────────────────────────
        [TraitId.IdeaBank]        = T("아이디어 뱅크",  TraitPart.Planning, TraitCategory.IdeaGeneration,  TraitPolarity.Positive, +3, TraitStat.Creativity, TraitStat.Fun),
        [TraitId.IdeaDrought]     = T("아이디어 가뭄",  TraitPart.Planning, TraitCategory.IdeaGeneration,  TraitPolarity.Negative, -3, TraitStat.Creativity, TraitStat.Fun),
        [TraitId.Architect]       = T("구조 설계자",    TraitPart.Planning, TraitCategory.StructureDesign,  TraitPolarity.Positive, +3, TraitStat.Precision,  TraitStat.Creativity),
        [TraitId.StructureBreaker]= T("구조 붕괴자",    TraitPart.Planning, TraitCategory.StructureDesign,  TraitPolarity.Negative, -3, TraitStat.Precision,  TraitStat.Creativity),
        [TraitId.TrendRadar]      = T("트렌드 레이더",  TraitPart.Planning, TraitCategory.TrendSense,       TraitPolarity.Positive, +2, TraitStat.Creativity, TraitStat.Fun),
        [TraitId.TrendBlind]      = T("유행 불감증",    TraitPart.Planning, TraitCategory.TrendSense,       TraitPolarity.Negative, -2, TraitStat.Creativity, TraitStat.Fun),
        [TraitId.GoldenScale]     = T("황금 저울",      TraitPart.Planning, TraitCategory.BalanceControl,   TraitPolarity.Positive, +2, TraitStat.Precision,  TraitStat.Fun),
        [TraitId.BalanceBreaker]  = T("밸런스 폭주",    TraitPart.Planning, TraitCategory.BalanceControl,   TraitPolarity.Negative, -2, TraitStat.Precision,  TraitStat.Fun),
        [TraitId.NumberDetective] = T("숫자 탐정",      TraitPart.Planning, TraitCategory.DataAnalysis,     TraitPolarity.Positive, +1, TraitStat.Precision,  TraitStat.Fun),
        [TraitId.DataIlliterate]  = T("데이터맹",       TraitPart.Planning, TraitCategory.DataAnalysis,     TraitPolarity.Negative, -1, TraitStat.Precision,  TraitStat.Fun),

        // ── Develop ──────────────────────────────────────────
        [TraitId.CleanCode]       = T("클린 코드",       TraitPart.Develop, TraitCategory.CodeQuality,    TraitPolarity.Positive, +3, TraitStat.BugControl, TraitStat.TechPower),
        [TraitId.HardCode]        = T("하드 코드",       TraitPart.Develop, TraitCategory.CodeQuality,    TraitPolarity.Negative, -3, TraitStat.BugControl, TraitStat.TechPower),
        [TraitId.SystemArchitect] = T("아키텍트",        TraitPart.Develop, TraitCategory.SystemDesign,   TraitPolarity.Positive, +3, TraitStat.TechPower,  TraitStat.BugControl),
        [TraitId.SpaghettiCook]   = T("스파게티 요리사", TraitPart.Develop, TraitCategory.SystemDesign,   TraitPolarity.Negative, -3, TraitStat.TechPower,  TraitStat.BugControl),
        [TraitId.FastDev]         = T("빠른 개발",       TraitPart.Develop, TraitCategory.DevSpeed,       TraitPolarity.Positive, +2, TraitStat.TechPower,  TraitStat.Optimize),
        [TraitId.SlowDev]         = T("느린 개발",       TraitPart.Develop, TraitCategory.DevSpeed,       TraitPolarity.Negative, -2, TraitStat.TechPower,  TraitStat.Optimize),
        [TraitId.IssueSolver]     = T("이슈 해결사",     TraitPart.Develop, TraitCategory.ProblemSolving,  TraitPolarity.Positive, +2, TraitStat.BugControl, TraitStat.Optimize),
        [TraitId.ErrorIgnorer]    = T("에러 방치",       TraitPart.Develop, TraitCategory.ProblemSolving,  TraitPolarity.Negative, -2, TraitStat.BugControl, TraitStat.Optimize),
        [TraitId.NewTechLover]    = T("신기술 선호",     TraitPart.Develop, TraitCategory.TechPreference,  TraitPolarity.Positive, +3, TraitStat.TechPower,  TraitStat.Optimize),
        [TraitId.OldTech]         = T("구식 기술",       TraitPart.Develop, TraitCategory.TechPreference,  TraitPolarity.Negative, -3, TraitStat.TechPower,  TraitStat.Optimize),

        // ── Art ──────────────────────────────────────────────
        [TraitId.Original]        = T("독창적",         TraitPart.Art, TraitCategory.CreativeStyle,  TraitPolarity.Positive, +3, TraitStat.Visual,     TraitStat.Direction),
        [TraitId.Copycat]         = T("카피캣",         TraitPart.Art, TraitCategory.CreativeStyle,  TraitPolarity.Negative, -3, TraitStat.Visual,     TraitStat.Direction),
        [TraitId.Prolific]        = T("다작형",         TraitPart.Art, TraitCategory.Productivity,   TraitPolarity.Positive, +2, TraitStat.Composition, TraitStat.Visual),
        [TraitId.SlowStarter]     = T("슬로우 스타터",  TraitPart.Art, TraitCategory.Productivity,   TraitPolarity.Negative, -2, TraitStat.Composition, TraitStat.Visual),
        [TraitId.HighQuality]     = T("높은 퀄리티",    TraitPart.Art, TraitCategory.Quality,        TraitPolarity.Positive, +3, TraitStat.Visual,      TraitStat.Composition),
        [TraitId.LowQuality]      = T("낮은 퀄리티",    TraitPart.Art, TraitCategory.Quality,        TraitPolarity.Negative, -3, TraitStat.Visual,      TraitStat.Composition),
        [TraitId.SensoryDir]      = T("감각적 연출",     TraitPart.Art, TraitCategory.Expression,     TraitPolarity.Positive, +2, TraitStat.Direction,   TraitStat.Visual),
        [TraitId.FlatDir]         = T("밋밋한 연출",     TraitPart.Art, TraitCategory.Expression,     TraitPolarity.Negative, -2, TraitStat.Direction,   TraitStat.Visual),
        [TraitId.GoldenFrame]     = T("황금 구도",       TraitPart.Art, TraitCategory.Composition,    TraitPolarity.Positive, +1, TraitStat.Composition, TraitStat.Direction),
        [TraitId.Cluttered]       = T("화면 산만",       TraitPart.Art, TraitCategory.Composition,    TraitPolarity.Negative, -1, TraitStat.Composition, TraitStat.Direction),
    };

    static TraitData T(string name, TraitPart part, TraitCategory cat, TraitPolarity pol, int score,
        TraitStat stat1, TraitStat stat2) => new TraitData
        {
            displayName   = name,
            part          = part,
            category      = cat,
            polarity      = pol,
            score         = score,
            affectedStats = new[] { stat1, stat2 },
        };

    public static TraitData Get(TraitId id) => _all[id];
}
