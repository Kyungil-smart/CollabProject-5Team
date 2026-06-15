using System.Collections.Generic;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAScriptableObjectDiscoveryValidator : IQAValidator
    {
        public string Name => "ScriptableObject Discovery";

        public IEnumerable<QAResult> Run()
        {
            foreach (QAResult result in CountAssets<EmployeeImmutableData>("Employee SO"))
                yield return result;

            foreach (QAResult result in CountAssets<ReportSO>("Report SO"))
                yield return result;

            foreach (QAResult result in CountAssets<QuestSO>("Quest SO"))
                yield return result;

            foreach (QAResult result in CountAssets<ProjectSO>("Project SO"))
                yield return result;
        }

        private static IEnumerable<QAResult> CountAssets<T>(string label) where T : ScriptableObject
        {
            List<AssetEntry<T>> assets = QAAssetUtility.FindAssetEntriesByType<T>(QAAssetUtility.DatabaseRoot);

            if (assets.Count == 0)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "SO Discovery",
                    $"{label}를 찾지 못했습니다.",
                    QAAssetUtility.DatabaseRoot);
                yield break;
            }

            AssetEntry<T> first = assets[0];
            yield return new QAResult(
                QASeverity.Info,
                "SO Discovery",
                $"{label} {assets.Count}개를 찾았습니다.",
                first.Path,
                first.Asset);
        }
    }
}
