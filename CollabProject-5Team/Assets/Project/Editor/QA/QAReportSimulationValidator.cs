using System.Collections.Generic;
using System.Linq;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAReportSimulationValidator : IQAValidator
    {
        private const string EmployeeRoot = "Assets/Project/DB/Employee";
        private const string ReportRoot = "Assets/Project/DB/Report";

        private static readonly Role[] CoreProjectRoles =
        {
            Role.PLANNER,
            Role.ARTIST,
            Role.PROGRAMMER
        };

        public string Name => "Report Generation Simulation";

        public IEnumerable<QAResult> Run()
        {
            List<AssetEntry<EmployeeImmutableData>> employees =
                QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot);
            List<AssetEntry<ReportSO>> reports =
                QAAssetUtility.FindAssetEntriesByType<ReportSO>(ReportRoot);

            var reportLookup = reports
                .Where(r => r.Asset.trait != Trait.None)
                .GroupBy(r => (r.Asset.trait, r.Asset.startRepo, r.Asset.grade))
                .ToDictionary(g => g.Key, g => g.Select(e => e.Asset).ToList());

            int simulatedEmployees = 0;
            int missingCases = 0;

            foreach (AssetEntry<EmployeeImmutableData> employeeEntry in employees)
            {
                EmployeeImmutableData employee = employeeEntry.Asset;
                if (!CoreProjectRoles.Contains(employee.role))
                    continue;

                simulatedEmployees++;

                float score = ReportPolicy.CalcScore(employee, employee.desire);
                int grade = ReportPolicy.CalcGrade(score);

                foreach (int startRepo in new[] { 1, 0 })
                {
                    List<ReportSO> candidates = FindCandidates(reportLookup, employee, grade, startRepo);
                    if (candidates.Count > 0)
                        continue;

                    missingCases++;
                    yield return new QAResult(
                        QASeverity.Warning,
                        "Report Simulation",
                        $"{employee.Name}({employee.role}) 보고서 생성 후보가 없습니다. score={score:F1}, grade={grade}, startRepo={startRepo}, traits={GetTraitSummary(employee)}",
                        employeeEntry.Path,
                        employeeEntry.Asset);
                }
            }

            foreach (QAResult result in ValidateOnePersonTeamCombinations(employees, reportLookup))
                yield return result;

            yield return new QAResult(
                QASeverity.Info,
                "Report Simulation",
                $"직원 {simulatedEmployees}명 기준 보고서 생성 시뮬레이션을 완료했습니다. 후보 누락 {missingCases}건.",
                employees.Count > 0 ? employees[0].Path : EmployeeRoot,
                employees.Count > 0 ? employees[0].Asset : null);
        }

        private static IEnumerable<QAResult> ValidateOnePersonTeamCombinations(
            List<AssetEntry<EmployeeImmutableData>> employees,
            Dictionary<(Trait trait, int startRepo, int grade), List<ReportSO>> reportLookup)
        {
            List<AssetEntry<EmployeeImmutableData>> planners = GetEmployeesByRole(employees, Role.PLANNER);
            List<AssetEntry<EmployeeImmutableData>> artists = GetEmployeesByRole(employees, Role.ARTIST);
            List<AssetEntry<EmployeeImmutableData>> programmers = GetEmployeesByRole(employees, Role.PROGRAMMER);

            if (planners.Count == 0 || artists.Count == 0 || programmers.Count == 0)
            {
                yield return new QAResult(
                    QASeverity.Error,
                    "Report Simulation",
                    $"1인 팀 조합 시뮬레이션에 필요한 핵심 직군 직원이 부족합니다. 기획 {planners.Count}명, 아트 {artists.Count}명, 개발 {programmers.Count}명.",
                    EmployeeRoot);
                yield break;
            }

            int totalCombinations = planners.Count * artists.Count * programmers.Count;
            int failedCombinations = 0;
            const int maxReportedFailures = 12;

            foreach (AssetEntry<EmployeeImmutableData> planner in planners)
            foreach (AssetEntry<EmployeeImmutableData> artist in artists)
            foreach (AssetEntry<EmployeeImmutableData> programmer in programmers)
            {
                foreach (int startRepo in new[] { 1, 0 })
                {
                    bool plannerOk = CanGenerateReport(planner.Asset, reportLookup, startRepo);
                    bool artistOk = CanGenerateReport(artist.Asset, reportLookup, startRepo);
                    bool programmerOk = CanGenerateReport(programmer.Asset, reportLookup, startRepo);

                    if (plannerOk && artistOk && programmerOk)
                        continue;

                    failedCombinations++;
                    if (failedCombinations > maxReportedFailures)
                        continue;

                    yield return new QAResult(
                        QASeverity.Warning,
                        "Report Team Simulation",
                        $"1인 팀 조합에서 보고서 후보가 비는 직군이 있습니다. startRepo={startRepo}, 기획={planner.Asset.Name}({ToOkText(plannerOk)}), 아트={artist.Asset.Name}({ToOkText(artistOk)}), 개발={programmer.Asset.Name}({ToOkText(programmerOk)})",
                        EmployeeRoot);
                }
            }

            if (failedCombinations > maxReportedFailures)
            {
                yield return new QAResult(
                    QASeverity.Warning,
                    "Report Team Simulation",
                    $"1인 팀 조합 실패가 {failedCombinations}건입니다. 화면에는 최초 {maxReportedFailures}건만 표시했습니다.",
                    EmployeeRoot);
            }

            yield return new QAResult(
                QASeverity.Info,
                "Report Team Simulation",
                $"1인 팀 조합 {totalCombinations}개를 startRepo=1/0 기준으로 시뮬레이션했습니다. 실패 {failedCombinations}건.",
                EmployeeRoot);
        }

        private static List<AssetEntry<EmployeeImmutableData>> GetEmployeesByRole(
            List<AssetEntry<EmployeeImmutableData>> employees,
            Role role)
        {
            return employees
                .Where(e => e.Asset.role == role)
                .OrderBy(e => e.Asset.id)
                .ToList();
        }

        private static bool CanGenerateReport(
            EmployeeImmutableData employee,
            Dictionary<(Trait trait, int startRepo, int grade), List<ReportSO>> reportLookup,
            int startRepo)
        {
            float score = ReportPolicy.CalcScore(employee, employee.desire);
            int grade = ReportPolicy.CalcGrade(score);
            return FindCandidates(reportLookup, employee, grade, startRepo).Count > 0;
        }

        private static List<ReportSO> FindCandidates(
            Dictionary<(Trait trait, int startRepo, int grade), List<ReportSO>> reportLookup,
            EmployeeImmutableData employee,
            int grade,
            int startRepo)
        {
            var candidates = new List<ReportSO>();
            foreach (Trait trait in GetEmployeeTraits(employee))
            {
                if (trait == Trait.None)
                    continue;

                if (reportLookup.TryGetValue((trait, startRepo, grade), out List<ReportSO> found))
                    candidates.AddRange(found);
            }

            return candidates;
        }

        private static IEnumerable<Trait> GetEmployeeTraits(EmployeeImmutableData employee)
        {
            yield return employee.mainTrait;
            yield return employee.riskTrait;
            yield return employee.subTrait;
        }

        private static string GetTraitSummary(EmployeeImmutableData employee)
        {
            return string.Join(
                ", ",
                GetEmployeeTraits(employee)
                    .Where(trait => trait != Trait.None)
                    .Select(GetTraitLabel));
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

        private static string ToOkText(bool value)
        {
            return value ? "OK" : "Missing";
        }
    }
}
