using System.Collections.Generic;
using System.Linq;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAReportValidator : IQAValidator
    {
        private const string ReportRoot = "Assets/Project/DB/Report";
        private const string EmployeeRoot = "Assets/Project/DB/Employee";

        public string Name => "Report Data";

        public IEnumerable<QAResult> Run()
        {
            List<AssetEntry<ReportSO>> reports =
                QAAssetUtility.FindAssetEntriesByType<ReportSO>(ReportRoot);
            List<AssetEntry<EmployeeImmutableData>> employees =
                QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot);

            foreach (QAResult result in ValidateDuplicateIds(reports))
                yield return result;

            foreach (QAResult result in ValidateRuntimeLookupDuplicateKeys(reports))
                yield return result;

            foreach (AssetEntry<ReportSO> entry in reports)
            {
                foreach (QAResult result in ValidateReport(entry))
                    yield return result;
            }

            foreach (QAResult result in ValidateEmployeeReportCoverage(employees, reports))
                yield return result;

            yield return new QAResult(
                QASeverity.Info,
                "Report",
                $"보고서 데이터 {reports.Count}개 검사를 완료했습니다.",
                reports.Count > 0 ? reports[0].Path : ReportRoot,
                reports.Count > 0 ? reports[0].Asset : null);
        }

        private static IEnumerable<QAResult> ValidateDuplicateIds(List<AssetEntry<ReportSO>> reports)
        {
            foreach (var group in reports.GroupBy(e => e.Asset.id).Where(g => g.Key != 0 && g.Count() > 1))
            {
                AssetEntry<ReportSO> first = group.First();
                string paths = string.Join(", ", group.Select(e => e.Path));
                yield return Error(first, $"보고서 ID {group.Key}가 중복됩니다. 대상: {paths}");
            }
        }

        private static IEnumerable<QAResult> ValidateRuntimeLookupDuplicateKeys(List<AssetEntry<ReportSO>> reports)
        {
            var duplicateGroups = reports
                .GroupBy(e => (e.Asset.trait, e.Asset.startRepo, e.Asset.grade))
                .Where(g => g.Key.trait != Trait.None && g.Count() > 1)
                .ToList();

            if (duplicateGroups.Count == 0)
                yield break;

            var first = duplicateGroups[0].First();
            string samples = string.Join(
                " / ",
                duplicateGroups
                    .Take(8)
                    .Select(g => $"{g.Key.trait}:startRepo={g.Key.startRepo},grade={g.Key.grade},count={g.Count()}"));

            yield return Warning(
                first,
                $"ReportManager 조회 키 중복 그룹 {duplicateGroups.Count}개가 있습니다. 현재 런타임 딕셔너리에서는 같은 키의 마지막 항목만 남을 수 있습니다. 예시: {samples}");
        }

        private static IEnumerable<QAResult> ValidateReport(AssetEntry<ReportSO> entry)
        {
            ReportSO report = entry.Asset;

            if (report.id <= 0)
                yield return Error(entry, "보고서 ID가 1 이상이어야 합니다.");

            if (string.IsNullOrWhiteSpace(report.title))
                yield return Error(entry, "보고서 제목이 비어 있습니다.");

            if (string.IsNullOrWhiteSpace(report.content))
                yield return Warning(entry, "보고서 본문이 비어 있습니다.");

            if (report.startRepo != 0 && report.startRepo != 1)
                yield return Error(entry, $"startRepo 값은 0 또는 1이어야 합니다. 현재값: {report.startRepo}");

            if (report.grade < 1 || report.grade > 3)
                yield return Error(entry, $"보고서 등급은 1~3 범위여야 합니다. 현재값: {report.grade}");

            if (report.trait == Trait.None)
            {
                yield return Error(entry, "보고서 특성이 None입니다.");
                yield break;
            }

            if (!QATraitUtility.TryGetTraitData(report.trait, out TraitData traitData))
            {
                yield return Error(entry, $"보고서 특성 '{report.trait}'가 TraitTable에 없습니다.");
                yield break;
            }

            if (!QATraitUtility.TryGetExpectedTraitRole(report.role, out TraitRole expectedRole))
            {
                yield return Warning(entry, $"{report.role} 직군은 현재 1차 보고서 특성 QA 대상이 아닙니다.");
                yield break;
            }

            if (traitData.role != expectedRole)
            {
                yield return Error(
                    entry,
                    $"보고서 특성 '{traitData.displayName}'의 직군({traitData.role})이 보고서 직군({report.role})과 맞지 않습니다.");
            }
        }

        private static IEnumerable<QAResult> ValidateEmployeeReportCoverage(
            List<AssetEntry<EmployeeImmutableData>> employees,
            List<AssetEntry<ReportSO>> reports)
        {
            var lookup = new HashSet<(Trait trait, int startRepo, int grade)>(
                reports.Select(r => (r.Asset.trait, r.Asset.startRepo, r.Asset.grade)));

            foreach (AssetEntry<EmployeeImmutableData> employeeEntry in employees)
            {
                EmployeeImmutableData employee = employeeEntry.Asset;
                Trait[] traits =
                {
                    employee.mainTrait,
                    employee.riskTrait,
                    employee.subTrait
                };

                for (int startRepo = 0; startRepo <= 1; startRepo++)
                {
                    var missingGrades = new List<int>();
                    for (int grade = 1; grade <= 3; grade++)
                    {
                        bool hasCandidate = traits.Any(trait =>
                            trait != Trait.None && lookup.Contains((trait, startRepo, grade)));

                        if (hasCandidate) continue;

                        missingGrades.Add(grade);
                    }

                    if (missingGrades.Count == 0)
                        continue;

                    string traitNames = string.Join(
                        ", ",
                        traits
                            .Where(trait => trait != Trait.None)
                            .Select(GetTraitLabel));

                    yield return new QAResult(
                        QASeverity.Warning,
                        "Report Coverage",
                        $"{employee.Name} 직원의 특성 조합({traitNames})으로 아직 입력되지 않은 보고서 구간이 있습니다. startRepo={startRepo}, missingGrades={string.Join("/", missingGrades)}",
                        employeeEntry.Path,
                        employeeEntry.Asset);
                }
            }
        }

        private static string GetTraitLabel(Trait trait)
        {
            if (QATraitUtility.TryGetTraitData(trait, out TraitData data)
                && !string.IsNullOrWhiteSpace(data.displayName))
            {
                return data.displayName;
            }

            return trait.ToString();
        }

        private static QAResult Error(AssetEntry<ReportSO> entry, string message)
        {
            return new QAResult(QASeverity.Error, "Report", message, entry.Path, entry.Asset);
        }

        private static QAResult Warning(AssetEntry<ReportSO> entry, string message)
        {
            return new QAResult(QASeverity.Warning, "Report", message, entry.Path, entry.Asset);
        }
    }
}
