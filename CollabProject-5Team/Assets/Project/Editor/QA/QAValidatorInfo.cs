using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAValidatorInfo
    {
        public string Name { get; }
        public string Summary { get; }
        public IReadOnlyList<string> Checks { get; }

        public QAValidatorInfo(string name, string summary, IEnumerable<string> checks)
        {
            Name = name;
            Summary = summary;
            Checks = checks.ToArray();
        }
    }

    public static class QAValidatorInfoRegistry
    {
        private static readonly Dictionary<string, QAValidatorInfo> Infos = new()
        {
            ["QA Window Smoke Test"] = new QAValidatorInfo(
                "QA Window Smoke Test",
                "QA 에디터가 정상 실행되는지 확인합니다.",
                new[]
                {
                    "QA 창 실행 여부",
                    "검사 결과 표시 파이프라인 동작 여부"
                }),

            ["ScriptableObject Discovery"] = new QAValidatorInfo(
                "ScriptableObject Discovery",
                "핵심 데이터 SO가 프로젝트 안에 존재하는지 집계합니다.",
                new[]
                {
                    "Employee SO 개수",
                    "Report SO 개수",
                    "Quest SO 개수",
                    "Project SO 개수"
                }),

            ["Sheet Sync"] = new QAValidatorInfo(
                "Sheet Sync",
                "구글시트 연결 설정과 Unity SO 연결 상태를 검사합니다.",
                new[]
                {
                    "DataRequestSet URL/gid/startRow",
                    "DataRequestSet index 중복",
                    "targetSOList null/빈 항목",
                    "targetSOList 내부 id 중복",
                    "DB 폴더 SO와 targetSOList 연결 불일치"
                }),

            ["Employee Data"] = new QAValidatorInfo(
                "Employee Data",
                "직원 데이터의 기본 유효성과 특성 연결을 검사합니다.",
                new[]
                {
                    "직원 id 중복/범위",
                    "이름/직군/등급",
                    "능력치/의욕/피로도/충성도 0~100",
                    "고용 비용/주급 음수 여부",
                    "직원 특성 누락/직군 불일치",
                    "상태별 초상화 누락"
                }),

            ["Report Data"] = new QAValidatorInfo(
                "Report Data",
                "보고서 데이터와 런타임 조회 키를 검사합니다.",
                new[]
                {
                    "보고서 id 중복",
                    "제목/본문 누락",
                    "직군/특성 불일치",
                    "ReportManager 조회 키 중복",
                    "직원 특성 조합 기준 보고서 커버리지"
                }),

            ["Report Generation Simulation"] = new QAValidatorInfo(
                "Report Generation Simulation",
                "직원 데이터 기준으로 보고서 후보가 생성 가능한지 시뮬레이션합니다.",
                new[]
                {
                    "직원 능력치/의욕 기반 보고서 점수 계산",
                    "보고서 등급 계산",
                    "startRepo=1/0 후보 존재 여부",
                    "기획/아트/개발 1인 팀 조합 시뮬레이션"
                }),

            ["Project Formula"] = new QAValidatorInfo(
                "Project Formula",
                "프로젝트 진행/완성 관련 핵심 공식의 경계값을 검사합니다.",
                new[]
                {
                    "직원 기본 세부 점수 공식",
                    "보고서 등급 경계값",
                    "프로젝트 SO 기간/인원/비용/목표 점수",
                    "평점 최소값 정책 불일치",
                    "판매량/매출/굿즈/유저 수 경계값"
                }),

            ["Quest / Comment Data"] = new QAValidatorInfo(
                "Quest / Comment Data",
                "퀘스트와 주간 코멘트 데이터의 기본 유효성을 검사합니다.",
                new[]
                {
                    "퀘스트 id 중복",
                    "목표 수/활성 오브젝트/완료 대사",
                    "역할 조건",
                    "코멘트 id 중복",
                    "의욕/피로도/충성도 조건 조합 커버리지"
                }),

            ["Scene / Prefab References"] = new QAValidatorInfo(
                "Scene / Prefab References",
                "씬/프리팹의 Unity 연결 상태와 필수 참조를 검사합니다.",
                new[]
                {
                    "Missing Script",
                    "Broken Object Reference",
                    "Button OnClick 대상/메서드 누락",
                    "GameScene 필수 매니저 존재 여부",
                    "보고서/프로젝트/인사/퀘스트 UI 참조 누락"
                }),

            ["Play Flow"] = new QAValidatorInfo(
                "Play Flow",
                "낮 업무 시작부터 금요일 밤 보고서와 밤 퇴근까지 핵심 플레이 루프 연결을 검사합니다.",
                new[]
                {
                    "GameScene 플레이 루프 필수 매니저 존재 여부",
                    "DateTimeManager 낮/밤 이벤트 및 메서드 계약",
                    "HUD 업무 시작 버튼과 플레이어 책상 연결",
                    "QuestManager 일일 업무 생성/완료 연결",
                    "퀘스트 활성 오브젝트 이름과 씬 배치 일치 여부",
                    "금요일 밤 ReportPresenter/ReportView/보고서 카드 연결",
                    "밤 하단 인사관리/프로젝트관리 Presenter 존재 여부"
                })
        };

        public static QAValidatorInfo Get(IQAValidator validator)
        {
            if (validator == null)
                return null;

            if (Infos.TryGetValue(validator.Name, out QAValidatorInfo info))
                return info;

            return new QAValidatorInfo(
                validator.Name,
                "등록된 설명이 없는 QA 검사입니다.",
                Array.Empty<string>());
        }
    }
}
