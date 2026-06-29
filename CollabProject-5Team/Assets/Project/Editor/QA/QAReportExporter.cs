using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public static class QAReportExporter
    {
        public static void ExportMarkdown(List<QAResult> results)
        {
            if (results == null || results.Count == 0)
            {
                EditorUtility.DisplayDialog("QA 리포트 내보내기", "내보낼 QA 결과가 없습니다. 먼저 QA 검사를 실행해주세요.", "확인");
                return;
            }

            string defaultName = $"QA_Report_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            string path = EditorUtility.SaveFilePanel(
                "QA 리포트 내보내기",
                Application.dataPath,
                defaultName,
                "md");

            if (string.IsNullOrEmpty(path))
                return;

            File.WriteAllText(path, BuildMarkdown(results), Encoding.UTF8);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[QA Report Export] {path}");
        }

        public static string BuildMarkdown(List<QAResult> results)
        {
            int errorCount = results.Count(r => r.Severity == QASeverity.Error);
            int warningCount = results.Count(r => r.Severity == QASeverity.Warning);
            int infoCount = results.Count(r => r.Severity == QASeverity.Info);

            var builder = new StringBuilder();
            builder.AppendLine("# Prototype QA Report");
            builder.AppendLine();
            builder.AppendLine($"- 내보낸 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"- Error: {errorCount}");
            builder.AppendLine($"- Warning: {warningCount}");
            builder.AppendLine($"- Info: {infoCount}");
            builder.AppendLine("- 구분: 기획 데이터 QA / 개발 연결 QA / 공통 QA");
            builder.AppendLine();

            AppendSummaryByCategory(builder, results);
            AppendSummaryByOwner(builder, results);
            AppendSummaryByTriage(builder, results);
            AppendSummaryByWorkType(builder, results);
            AppendSummaryByPriority(builder, results);
            AppendTopIssueGroups(builder, results);
            AppendResults(builder, results);

            return builder.ToString();
        }

        private static void AppendSummaryByCategory(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## 검사 영역 요약");
            builder.AppendLine();
            builder.AppendLine("| 검사 영역 | Error | Warning | Info | Total |");
            builder.AppendLine("|---|---:|---:|---:|---:|");

            foreach (var group in results.GroupBy(r => r.Category).OrderBy(g => g.Key))
            {
                int error = group.Count(r => r.Severity == QASeverity.Error);
                int warning = group.Count(r => r.Severity == QASeverity.Warning);
                int info = group.Count(r => r.Severity == QASeverity.Info);
                builder.AppendLine($"| {Escape(group.Key)} | {error} | {warning} | {info} | {group.Count()} |");
            }

            builder.AppendLine();
        }

        private static void AppendSummaryByOwner(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## 확인 담당 요약");
            builder.AppendLine();
            builder.AppendLine("| 확인 담당 | Error | Warning | Info | Total |");
            builder.AppendLine("|---|---:|---:|---:|---:|");

            foreach (var group in results.GroupBy(GetOwnerStatus).OrderBy(g => GetOwnerOrder(g.Key)))
            {
                int error = group.Count(r => r.Severity == QASeverity.Error);
                int warning = group.Count(r => r.Severity == QASeverity.Warning);
                int info = group.Count(r => r.Severity == QASeverity.Info);
                builder.AppendLine($"| {Escape(group.Key)} | {error} | {warning} | {info} | {group.Count()} |");
            }

            builder.AppendLine();
        }

        private static void AppendSummaryByTriage(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## 조치 분류 요약");
            builder.AppendLine();
            builder.AppendLine("| 조치 분류 | Error | Warning | Info | Total |");
            builder.AppendLine("|---|---:|---:|---:|---:|");

            foreach (var group in results.GroupBy(GetTriageStatus).OrderBy(g => GetTriageOrder(g.Key)))
            {
                int error = group.Count(r => r.Severity == QASeverity.Error);
                int warning = group.Count(r => r.Severity == QASeverity.Warning);
                int info = group.Count(r => r.Severity == QASeverity.Info);
                builder.AppendLine($"| {Escape(group.Key)} | {error} | {warning} | {info} | {group.Count()} |");
            }

            builder.AppendLine();
        }

        private static void AppendSummaryByWorkType(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## 작업 유형 요약");
            builder.AppendLine();
            builder.AppendLine("| 작업 유형 | Error | Warning | Info | Total |");
            builder.AppendLine("|---|---:|---:|---:|---:|");

            foreach (var group in results.GroupBy(GetWorkType).OrderBy(g => GetWorkTypeOrder(g.Key)))
            {
                int error = group.Count(r => r.Severity == QASeverity.Error);
                int warning = group.Count(r => r.Severity == QASeverity.Warning);
                int info = group.Count(r => r.Severity == QASeverity.Info);
                builder.AppendLine($"| {Escape(group.Key)} | {error} | {warning} | {info} | {group.Count()} |");
            }

            builder.AppendLine();
        }

        private static void AppendSummaryByPriority(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## 우선순위 요약");
            builder.AppendLine();
            builder.AppendLine("| 우선순위 | Count |");
            builder.AppendLine("|---|---:|");

            foreach (var group in results
                         .Select(QAResultAdvisor.GetAdvice)
                         .GroupBy(a => a.Priority)
                         .OrderBy(g => g.Key))
            {
                builder.AppendLine($"| {Escape(group.Key)} | {group.Count()} |");
            }

            builder.AppendLine();
        }

        private static void AppendTopIssueGroups(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## 동일 유형 Top 10");
            builder.AppendLine();
            builder.AppendLine("| Count | Severity | 확인 담당 | 조치 분류 | 작업 유형 | 유형 |");
            builder.AppendLine("|---:|---|---|---|---|---|");

            foreach (var group in results
                         .GroupBy(GetGroupKey)
                         .Select(g => new { First = g.First(), Count = g.Count() })
                         .OrderByDescending(g => g.Count)
                         .ThenBy(g => GetSeverityOrder(g.First))
                         .Take(10))
            {
                QAResult first = group.First;
                builder.AppendLine(
                    $"| {group.Count} | {first.Severity} | {Escape(GetOwnerStatus(first))} | {Escape(GetTriageStatus(first))} | {Escape(GetWorkType(first))} | {Escape(GetNormalizedMessage(first))} |");
            }

            builder.AppendLine();
        }

        private static void AppendResults(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## 상세 결과");
            builder.AppendLine();

            foreach (QAResult result in results
                         .OrderBy(GetSeverityOrder)
                         .ThenBy(r => QAResultAdvisor.GetAdvice(r).Priority)
                         .ThenBy(r => r.Category))
            {
                QAResultAdvice advice = QAResultAdvisor.GetAdvice(result);

                builder.AppendLine($"### [{result.Severity}] {result.Category}");
                builder.AppendLine();
                builder.AppendLine($"- 메시지: {Escape(result.Message)}");
                builder.AppendLine($"- 확인 담당: {Escape(GetOwnerStatus(result))}");
                builder.AppendLine($"- 조치 분류: {Escape(GetTriageStatus(result))}");
                builder.AppendLine($"- 작업 유형: {Escape(GetWorkType(result))}");
                builder.AppendLine($"- 우선순위: {advice.Priority}");
                builder.AppendLine($"- 예상 원인: {Escape(advice.ExpectedCause)}");
                builder.AppendLine($"- 관련 코드/영역: `{Escape(advice.RelatedCode)}`");
                builder.AppendLine($"- 해결 힌트: {Escape(advice.FixHint)}");

                if (!string.IsNullOrWhiteSpace(result.AssetPath))
                    builder.AppendLine($"- 에셋: `{Escape(result.AssetPath)}`");

                builder.AppendLine();
            }
        }

        private static int GetSeverityOrder(QAResult result)
        {
            return result.Severity switch
            {
                QASeverity.Error => 0,
                QASeverity.Warning => 1,
                QASeverity.Info => 2,
                _ => 3
            };
        }

        private static string GetOwnerStatus(QAResult result)
        {
            if (result.Severity == QASeverity.Info)
                return "공통 QA";

            if (IsDevelopmentOwnerResult(result))
                return "개발 QA";

            if (IsDesignOwnerResult(result))
                return "기획 QA";

            return "공통 QA";
        }

        private static int GetOwnerOrder(string owner)
        {
            return owner switch
            {
                "기획 QA" => 0,
                "개발 QA" => 1,
                "공통 QA" => 2,
                _ => 3
            };
        }

        private static string GetTriageStatus(QAResult result)
        {
            if (result.Severity == QASeverity.Info)
                return "정보";

            if (IsDataPendingResult(result))
                return "데이터 입력 대기";

            if (IsDesignReviewResult(result))
                return "기획/정책 확인";

            if (result.Severity == QASeverity.Error)
                return "즉시 수정";

            return "확인 필요";
        }

        private static int GetTriageOrder(string status)
        {
            return status switch
            {
                "즉시 수정" => 0,
                "기획/정책 확인" => 1,
                "데이터 입력 대기" => 2,
                "확인 필요" => 3,
                "정보" => 4,
                _ => 5
            };
        }

        private static string GetGroupKey(QAResult result)
        {
            return string.Join(
                "|",
                result.Severity.ToString(),
                result.Category ?? string.Empty,
                GetOwnerStatus(result),
                GetTriageStatus(result),
                GetNormalizedMessage(result));
        }

        private static string GetWorkType(QAResult result)
        {
            string normalized = GetNormalizedMessage(result);

            if (normalized == "버튼 Inspector OnClick 미연결")
                return "UI 버튼 연결";

            if (normalized == "Missing Script"
                || normalized == "깨진 Object Reference"
                || normalized == "필수 참조 누락")
                return "프리팹/씬 참조 복구";

            if (normalized == "targetSOList 미연결 SO"
                || normalized == "targetSOList 외부 폴더 SO 연결")
                return "시트 동기화 확인";

            if (normalized == "주간 코멘트 조건 조합 누락")
                return "주간 코멘트 보강";

            if (normalized == "직원 특성 조합별 보고서 구간 누락"
                || normalized == "직원별 보고서 생성 후보 없음"
                || normalized == "1인 팀 조합 보고서 후보 누락")
                return "보고서 데이터 보강";

            if (IsDesignReviewResult(result))
                return "공식/기준값 확인";

            if (IsDataPendingResult(result))
                return "데이터 확인";

            if (result.Severity == QASeverity.Error)
                return "즉시 실행 오류 수정";

            return "기타 확인";
        }

        private static int GetWorkTypeOrder(string workType)
        {
            return workType switch
            {
                "즉시 실행 오류 수정" => 0,
                "프리팹/씬 참조 복구" => 1,
                "UI 버튼 연결" => 2,
                "보고서 데이터 보강" => 3,
                "주간 코멘트 보강" => 4,
                "시트 동기화 확인" => 5,
                "공식/기준값 확인" => 6,
                "데이터 확인" => 7,
                "기타 확인" => 8,
                _ => 9
            };
        }

        private static string GetNormalizedMessage(QAResult result)
        {
            string message = result?.Message ?? string.Empty;

            if (Contains(message, "targetSOList에는 연결되어 있지 않습니다")
                || Contains(message, "targetSOList 미연결 SO"))
                return "targetSOList 미연결 SO";

            if (Contains(message, "같은 폴더 밖"))
                return "targetSOList 외부 폴더 SO 연결";

            if (Contains(message, "직원의 특성 조합")
                && Contains(message, "보고서 구간"))
                return "직원 특성 조합별 보고서 구간 누락";

            if (Contains(message, "보고서 생성 후보가 없습니다"))
                return "직원별 보고서 생성 후보 없음";

            if (Contains(message, "1인 팀 조합에서 보고서 후보가 비는"))
                return "1인 팀 조합 보고서 후보 누락";

            if (Contains(message, "코멘트 조합이 비어 있습니다"))
                return "주간 코멘트 조건 조합 누락";

            if (Contains(message, "버튼에 Inspector OnClick 연결이 없습니다"))
                return "버튼 Inspector OnClick 미연결";

            if (Contains(message, "Missing Script"))
                return "Missing Script";

            if (Contains(message, "깨진 Object Reference"))
                return "깨진 Object Reference";

            if (Contains(message, "필수 참조가 비어 있습니다"))
                return "필수 참조 누락";

            return message;
        }

        private static bool IsDesignOwnerResult(QAResult result)
        {
            return IsDataPendingResult(result)
                || IsDesignReviewResult(result)
                || Contains(result.Category, "Sheet Sync")
                || Contains(result.Category, "Employee")
                || Contains(result.Category, "Report")
                || Contains(result.Category, "Quest")
                || Contains(result.Category, "Comment");
        }

        private static bool IsDevelopmentOwnerResult(QAResult result)
        {
            return Contains(result.Category, "Scene")
                || Contains(result.Category, "Prefab")
                || Contains(result.Category, "Play Flow")
                || Contains(result.Message, "Missing Script")
                || Contains(result.Message, "깨진 Object Reference")
                || Contains(result.Message, "OnClick")
                || Contains(result.Message, "Presenter")
                || Contains(result.Message, "View")
                || Contains(result.Message, "Manager가 없어");
        }

        private static bool IsDataPendingResult(QAResult result)
        {
            return Contains(result.Category, "Report Coverage")
                || Contains(result.Category, "Report Simulation")
                || Contains(result.Category, "Report Team Simulation")
                || Contains(result.Message, "보고서 후보")
                || Contains(result.Message, "보고서 구간")
                || Contains(result.Message, "아직 입력")
                || Contains(result.Message, "조합 누락")
                || Contains(result.Message, "coverage");
        }

        private static bool IsDesignReviewResult(QAResult result)
        {
            return Contains(result.Category, "Project Formula")
                || Contains(result.Message, "기획")
                || Contains(result.Message, "정책")
                || Contains(result.Message, "공식")
                || Contains(result.Message, "클램프")
                || Contains(result.Message, "최소값")
                || Contains(result.Message, "극단 케이스");
        }

        private static bool Contains(string value, string searchText)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
