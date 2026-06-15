using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QASheetSyncValidator : IQAValidator
    {
        private const string SearchRoot = "Assets";
        private const string GoogleSheetHost = "docs.google.com/spreadsheets/d/";

        public string Name => "Sheet Sync";

        public IEnumerable<QAResult> Run()
        {
            List<AssetEntry<DataRequestSet>> sets =
                QAAssetUtility.FindAssetEntriesByType<DataRequestSet>(SearchRoot);

            if (sets.Count == 0)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Sheet Sync",
                    "DataRequestSet 에셋을 찾지 못했습니다. 구글시트 연동 QA를 진행할 수 없습니다.",
                    SearchRoot);
                yield break;
            }

            foreach (QAResult result in ValidateSetIndexes(sets))
                yield return result;

            foreach (AssetEntry<DataRequestSet> entry in sets)
            {
                foreach (QAResult result in ValidateSet(entry))
                    yield return result;
            }

            yield return new QAResult(
                QASeverity.Info,
                "Sheet Sync",
                $"DataRequestSet {sets.Count}개 검사를 완료했습니다.",
                SearchRoot);
        }

        private static IEnumerable<QAResult> ValidateSetIndexes(List<AssetEntry<DataRequestSet>> sets)
        {
            foreach (IGrouping<int, AssetEntry<DataRequestSet>> group in sets.GroupBy(s => s.Asset.index))
            {
                if (group.Key <= 0)
                {
                    foreach (AssetEntry<DataRequestSet> entry in group)
                    {
                        yield return new QAResult(
                            QASeverity.Warning,
                            "Sheet Sync",
                            $"{entry.Asset.name} DataRequestSet index가 0 이하입니다. 메뉴/조회용 index로 쓰기 어렵습니다.",
                            entry.Path,
                            entry.Asset);
                    }
                }

                if (group.Count() <= 1)
                    continue;

                string paths = string.Join(", ", group.Select(g => g.Path));
                foreach (AssetEntry<DataRequestSet> entry in group)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Sheet Sync",
                        $"DataRequestSet index가 중복됩니다. index={group.Key}, paths={paths}",
                        entry.Path,
                        entry.Asset);
                }
            }
        }

        private static IEnumerable<QAResult> ValidateSet(AssetEntry<DataRequestSet> entry)
        {
            DataRequestSet set = entry.Asset;

            foreach (QAResult result in ValidateSheetUrl(entry))
                yield return result;

            if (set.startRow < 1)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Sheet Sync",
                    $"{set.name} startRow가 1 미만입니다. 현재 값: {set.startRow}",
                    entry.Path,
                    set);
            }

            if (set.targetSOList == null)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Sheet Sync",
                    $"{set.name} targetSOList가 null입니다.",
                    entry.Path,
                    set);
                yield break;
            }

            if (set.targetSOList.Count == 0)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Sheet Sync",
                    $"{set.name} targetSOList가 비어 있습니다.",
                    entry.Path,
                    set);
            }

            foreach (QAResult result in ValidateTargetList(entry))
                yield return result;

            foreach (QAResult result in ValidateFolderSync(entry))
                yield return result;
        }

        private static IEnumerable<QAResult> ValidateSheetUrl(AssetEntry<DataRequestSet> entry)
        {
            DataRequestSet set = entry.Asset;
            string url = set.sheetData.url;

            if (string.IsNullOrWhiteSpace(url))
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Sheet Sync",
                    $"{set.name} 구글시트 URL이 비어 있습니다.",
                    entry.Path,
                    set);
                yield break;
            }

            if (!url.Contains(GoogleSheetHost))
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Sheet Sync",
                    $"{set.name} URL이 일반적인 구글 스프레드시트 주소 형식이 아닙니다. url={url}",
                    entry.Path,
                    set);
            }

            if (!url.Contains("gid="))
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Sheet Sync",
                    $"{set.name} URL에 gid 값이 없습니다. SheetData.Load에서 gid 파싱 실패 가능성이 있습니다.",
                    entry.Path,
                    set);
            }

            if (!url.Contains("/edit?") && !url.Contains("/export?"))
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Sheet Sync",
                    $"{set.name} URL이 edit/export 형식이 아닙니다. SheetData.Load 파싱 규칙과 맞는지 확인이 필요합니다.",
                    entry.Path,
                    set);
            }
        }

        private static IEnumerable<QAResult> ValidateTargetList(AssetEntry<DataRequestSet> entry)
        {
            DataRequestSet set = entry.Asset;
            var seenIds = new Dictionary<int, SheetDataSOBase>();

            for (int i = 0; i < set.targetSOList.Count; i++)
            {
                SheetDataSOBase so = set.targetSOList[i];
                if (so == null)
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Sheet Sync",
                        $"{set.name} targetSOList #{i} 항목이 비어 있습니다.",
                        entry.Path,
                        set);
                    continue;
                }

                string soPath = AssetDatabase.GetAssetPath(so);

                if (so.id <= 0)
                {
                    yield return new QAResult(
                        QASeverity.Warning,
                        "Sheet Sync",
                        $"{set.name}에 연결된 {so.name} SO의 id가 0 이하입니다. 시트 id 매칭 실패 가능성이 있습니다.",
                        soPath,
                        so);
                }

                if (seenIds.TryGetValue(so.id, out SheetDataSOBase previous))
                {
                    yield return new QAResult(
                        QASeverity.Error,
                        "Sheet Sync",
                        $"{set.name} targetSOList 안에서 id가 중복됩니다. id={so.id}, first={previous.name}, duplicate={so.name}",
                        soPath,
                        so);
                }
                else
                {
                    seenIds.Add(so.id, so);
                }

                if (so.row > 0 && so.row < set.startRow)
                {
                    yield return new QAResult(
                        QASeverity.Warning,
                        "Sheet Sync",
                        $"{so.name} SO의 row 값이 DataRequestSet startRow보다 작습니다. row={so.row}, startRow={set.startRow}",
                        soPath,
                        so);
                }
            }
        }

        private static IEnumerable<QAResult> ValidateFolderSync(AssetEntry<DataRequestSet> entry)
        {
            DataRequestSet set = entry.Asset;
            string folderPath = System.IO.Path.GetDirectoryName(entry.Path)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(folderPath))
                yield break;

            List<SheetDataSOBase> folderSos = FindSheetDataSos(folderPath);

            var linked = new HashSet<SheetDataSOBase>(set.targetSOList.Where(so => so != null));

            foreach (SheetDataSOBase so in folderSos)
            {
                if (linked.Contains(so))
                    continue;

                yield return new QAResult(
                    QASeverity.Warning,
                    "Sheet Sync",
                    $"{folderPath} 폴더에 {so.name} SO가 있지만 {set.name} targetSOList에는 연결되어 있지 않습니다.",
                    AssetDatabase.GetAssetPath(so),
                    so);
            }

            foreach (SheetDataSOBase so in linked)
            {
                string soPath = AssetDatabase.GetAssetPath(so);
                if (string.IsNullOrEmpty(soPath))
                    continue;

                if (!soPath.StartsWith(folderPath + "/"))
                {
                    yield return new QAResult(
                        QASeverity.Warning,
                        "Sheet Sync",
                        $"{set.name} targetSOList에 같은 폴더 밖의 SO가 연결되어 있습니다. so={so.name}, path={soPath}",
                        soPath,
                        so);
                }
            }

            yield return new QAResult(
                QASeverity.Info,
                "Sheet Sync",
                $"{set.name}: targetSOList {set.targetSOList.Count}개 / 같은 폴더 SheetDataSO {folderSos.Count}개",
                entry.Path,
                set);
        }

        private static List<SheetDataSOBase> FindSheetDataSos(string folderPath)
        {
            var results = new List<SheetDataSOBase>();
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SheetDataSOBase asset = AssetDatabase.LoadAssetAtPath<SheetDataSOBase>(path);
                if (asset != null)
                    results.Add(asset);
            }

            return results;
        }
    }
}
