using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QABuildPrecheck : IPreprocessBuildWithReport
    {
        private const int MaxPreviewMessages = 8;

        public int callbackOrder => -1000;

        [MenuItem("Tools/QA/4. Run Build Precheck", false, 104)]
        public static void RunFromMenu()
        {
            List<QAResult> results = QARunner.RunAll(QARunner.CreateDefaultValidators());
            int errorCount = results.Count(r => r.Severity == QASeverity.Error);
            int warningCount = results.Count(r => r.Severity == QASeverity.Warning);
            int infoCount = results.Count(r => r.Severity == QASeverity.Info);

            string message = BuildSummaryMessage(results, errorCount, warningCount, infoCount);
            EditorUtility.DisplayDialog("QA Build Precheck", message, "확인");
            LogSummary(results, errorCount, warningCount, infoCount);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            List<QAResult> results = QARunner.RunAll(QARunner.CreateDefaultValidators());
            int errorCount = results.Count(r => r.Severity == QASeverity.Error);
            int warningCount = results.Count(r => r.Severity == QASeverity.Warning);
            int infoCount = results.Count(r => r.Severity == QASeverity.Info);

            LogSummary(results, errorCount, warningCount, infoCount);

            if (errorCount <= 0)
                return;

            string message = BuildSummaryMessage(results, errorCount, warningCount, infoCount);

            if (Application.isBatchMode)
                throw new BuildFailedException(message);

            bool cancelBuild = EditorUtility.DisplayDialog(
                "QA Build Precheck",
                message,
                "빌드 취소",
                "무시하고 빌드");

            if (cancelBuild)
                throw new BuildFailedException("QA Build Precheck에서 사용자가 빌드를 취소했습니다.");
        }

        private static string BuildSummaryMessage(
            List<QAResult> results,
            int errorCount,
            int warningCount,
            int infoCount)
        {
            IEnumerable<QAResult> previewErrors = results
                .Where(r => r.Severity == QASeverity.Error)
                .Take(MaxPreviewMessages);

            string preview = string.Join(
                "\n",
                previewErrors.Select(r => $"- [{r.Category}] {r.Message}"));

            if (string.IsNullOrEmpty(preview))
                preview = "- Error 없음";

            return $"QA 결과: Error {errorCount} / Warning {warningCount} / Info {infoCount}\n\n"
                + "빌드 전에 확인할 Error 미리보기:\n"
                + preview
                + "\n\n전체 상세 결과는 Tools > QA > 2. Data & Connection QA에서 확인할 수 있습니다.";
        }

        private static void LogSummary(
            List<QAResult> results,
            int errorCount,
            int warningCount,
            int infoCount)
        {
            Debug.Log($"[QA Build Precheck] Error {errorCount} / Warning {warningCount} / Info {infoCount}");

            foreach (QAResult result in results.Where(r => r.Severity == QASeverity.Error).Take(MaxPreviewMessages))
                Debug.LogError($"[QA Build Precheck] [{result.Category}] {result.Message}");
        }
    }
}
