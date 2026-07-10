using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAResult
    {
        public QASeverity Severity { get; }
        public string Category { get; }
        public string Message { get; }
        public string AssetPath { get; }
        public Object Target { get; }

        public QAResult(
            QASeverity severity,
            string category,
            string message,
            string assetPath = "",
            Object target = null)
        {
            Severity = severity;
            Category = category;
            Message = message;
            AssetPath = assetPath;
            Target = target;
        }
    }
}
