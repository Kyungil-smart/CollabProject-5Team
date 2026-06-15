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
                EditorUtility.DisplayDialog("QA Report Export", "내보낼 QA 결과가 없습니다. 먼저 전체 검사를 실행해주세요.", "확인");
                return;
            }

            string defaultName = $"QA_Report_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            string path = EditorUtility.SaveFilePanel(
                "QA Report Export",
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
            builder.AppendLine($"- Exported At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"- Error: {errorCount}");
            builder.AppendLine($"- Warning: {warningCount}");
            builder.AppendLine($"- Info: {infoCount}");
            builder.AppendLine();

            AppendSummaryByCategory(builder, results);
            AppendSummaryByPriority(builder, results);
            AppendResults(builder, results);

            return builder.ToString();
        }

        private static void AppendSummaryByCategory(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## Category Summary");
            builder.AppendLine();
            builder.AppendLine("| Category | Error | Warning | Info | Total |");
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

        private static void AppendSummaryByPriority(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## Priority Summary");
            builder.AppendLine();
            builder.AppendLine("| Priority | Count |");
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

        private static void AppendResults(StringBuilder builder, List<QAResult> results)
        {
            builder.AppendLine("## Results");
            builder.AppendLine();

            foreach (QAResult result in results
                         .OrderBy(GetSeverityOrder)
                         .ThenBy(r => QAResultAdvisor.GetAdvice(r).Priority)
                         .ThenBy(r => r.Category))
            {
                QAResultAdvice advice = QAResultAdvisor.GetAdvice(result);

                builder.AppendLine($"### [{result.Severity}] {result.Category}");
                builder.AppendLine();
                builder.AppendLine($"- Message: {Escape(result.Message)}");
                builder.AppendLine($"- Priority: {advice.Priority}");
                builder.AppendLine($"- Expected Cause: {Escape(advice.ExpectedCause)}");
                builder.AppendLine($"- Related Code: `{Escape(advice.RelatedCode)}`");
                builder.AppendLine($"- Fix Hint: {Escape(advice.FixHint)}");

                if (!string.IsNullOrWhiteSpace(result.AssetPath))
                    builder.AppendLine($"- Asset: `{Escape(result.AssetPath)}`");

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

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
