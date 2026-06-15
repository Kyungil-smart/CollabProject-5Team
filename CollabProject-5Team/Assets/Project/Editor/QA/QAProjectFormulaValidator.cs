using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAProjectFormulaValidator : IQAValidator
    {
        private const string ProjectRoot = "Assets/Project/DB/Project";
        private const string EmployeeRoot = "Assets/Project/DB/Employee";

        public string Name => "Project Formula";

        public IEnumerable<QAResult> Run()
        {
            var results = new List<QAResult>();

            ValidateProjectData(results);
            ValidateBasePropertyFormula(results);
            ValidateReportGradeFormula(results);
            ValidateReportScoreFormula(results);
            ValidateCompletedProjectFormula(results);

            foreach (QAResult result in results)
                yield return result;

            yield return new QAResult(
                QASeverity.Info,
                "Project Formula",
                $"프로젝트 공식 QA를 완료했습니다. 발견 항목 {results.Count}건.",
                ProjectRoot);
        }

        private static void ValidateProjectData(List<QAResult> results)
        {
            List<AssetEntry<ProjectSO>> projects =
                QAAssetUtility.FindAssetEntriesByType<ProjectSO>(ProjectRoot);

            if (projects.Count == 0)
            {
                results.Add(new QAResult(
                    QASeverity.Error,
                    "Project Formula",
                    "ProjectSO 데이터가 없습니다. 프로젝트 시작/완료 공식 검증을 진행할 수 없습니다.",
                    ProjectRoot));
                return;
            }

            foreach (AssetEntry<ProjectSO> entry in projects)
            {
                ProjectSO project = entry.Asset;

                if (project.durationDays <= 0)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{project.Name} 프로젝트의 durationDays가 0 이하입니다. 진행도 계산에서 기간 기준이 깨질 수 있습니다.",
                        entry.Path,
                        project));
                }

                if (project.maxEmployeePerPart <= 0)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{project.Name} 프로젝트의 maxEmployeePerPart가 0 이하입니다. 인원 배치가 불가능합니다.",
                        entry.Path,
                        project));
                }

                if (project.requiredCost < 0)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{project.Name} 프로젝트의 requiredCost가 음수입니다.",
                        entry.Path,
                        project));
                }

                if (project.goalScore < 0f || project.goalScore > 100f)
                {
                    results.Add(new QAResult(
                        QASeverity.Warning,
                        "Project Formula",
                        $"{project.Name} 프로젝트의 goalScore가 0~100 범위를 벗어납니다. 현재 값: {project.goalScore}",
                        entry.Path,
                        project));
                }
            }
        }

        private static void ValidateBasePropertyFormula(List<QAResult> results)
        {
            AssertInt(results, "직원 기본 세부 점수 ability=0", 20, PerkPolicy.CalcBaseProperty(0));
            AssertInt(results, "직원 기본 세부 점수 ability=40", 40, PerkPolicy.CalcBaseProperty(40));
            AssertInt(results, "직원 기본 세부 점수 ability=80", 60, PerkPolicy.CalcBaseProperty(80));
            AssertInt(results, "직원 기본 세부 점수 ability=100", 70, PerkPolicy.CalcBaseProperty(100));

            int previous = PerkPolicy.CalcBaseProperty(0);
            for (int ability = 1; ability <= 100; ability++)
            {
                int current = PerkPolicy.CalcBaseProperty(ability);
                if (current < previous)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"직원 기본 세부 점수가 능력치 증가 중 감소했습니다. ability={ability - 1}->{ability}, score={previous}->{current}"));
                    break;
                }

                previous = current;
            }
        }

        private static void ValidateReportGradeFormula(List<QAResult> results)
        {
            AssertInt(results, "보고서 등급 경계 score=70", 1, ReportPolicy.CalcGrade(70f));
            AssertInt(results, "보고서 등급 경계 score=69.9", 2, ReportPolicy.CalcGrade(69.9f));
            AssertInt(results, "보고서 등급 경계 score=45", 2, ReportPolicy.CalcGrade(45f));
            AssertInt(results, "보고서 등급 경계 score=44.9", 3, ReportPolicy.CalcGrade(44.9f));
        }

        private static void ValidateReportScoreFormula(List<QAResult> results)
        {
            List<AssetEntry<EmployeeImmutableData>> employees =
                QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot);

            foreach (AssetEntry<EmployeeImmutableData> entry in employees)
            {
                EmployeeImmutableData employee = entry.Asset;
                float score = ReportPolicy.CalcScore(employee, employee.desire);
                int grade = ReportPolicy.CalcGrade(score);

                if (float.IsNaN(score) || float.IsInfinity(score))
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{employee.Name} 직원의 보고서 점수가 유효하지 않습니다. score={score}",
                        entry.Path,
                        employee));
                }

                if (grade < 1 || grade > 3)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{employee.Name} 직원의 보고서 등급이 1~3 범위를 벗어납니다. score={score}, grade={grade}",
                        entry.Path,
                        employee));
                }
            }
        }

        private static void ValidateCompletedProjectFormula(List<QAResult> results)
        {
            float minRating = PerkPolicy.CalcRating(0f, 0f, 0f);
            if (minRating < 0.5f)
            {
                results.Add(new QAResult(
                    QASeverity.Warning,
                    "Project Formula",
                    $"CalcRating 주석/기획상 평점 최소값은 0.5인데 현재 0점 입력 시 {minRating:F2}가 반환됩니다. 최소 평점 클램프가 필요합니다."));
            }

            float maxRating = PerkPolicy.CalcRating(100f, 100f, 100f);
            AssertFloat(results, "최대 평점 quality/stability/charm=100", 5f, maxRating);

            foreach (ProjectSize size in new[] { ProjectSize.small, ProjectSize.medium, ProjectSize.large })
            {
                ValidateSalesCase(results, size, rating: 0.5f, retention: 0f, popularity: 0);
                ValidateSalesCase(results, size, rating: 3f, retention: 0.5f, popularity: 0);
                ValidateSalesCase(results, size, rating: 5f, retention: 1f, popularity: 100);

                int weeklyCost = PerkPolicy.CalcWeeklyCost(size);
                if (weeklyCost < 0)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{size} 프로젝트 유지비가 음수입니다. weeklyCost={weeklyCost}"));
                }
            }

            AssertInt(results, "매력도 49 굿즈 판매량", 0, PerkPolicy.CalcGoodsSales(49f));
            AssertInt(results, "매력도 50 굿즈 판매량", 0, PerkPolicy.CalcGoodsSales(50f));
            AssertInt(results, "매력도 100 굿즈 판매량", 5000, PerkPolicy.CalcGoodsSales(100f));

            int extremeUsers = PerkPolicy.CalcUsers(ProjectSize.small, projectScore: 0f, prevUsers: 10000);
            if (extremeUsers < 0)
            {
                results.Add(new QAResult(
                    QASeverity.Warning,
                    "Project Formula",
                    $"극단 케이스에서 유저 수가 음수가 될 수 있습니다. small, projectScore=0, prevUsers=10000 => users={extremeUsers}. 서비스 종료/최소값 처리 정책이 필요할 수 있습니다."));
            }

            foreach (ProjectSize size in new[] { ProjectSize.small, ProjectSize.medium, ProjectSize.large })
            {
                int users = PerkPolicy.CalcUsers(size, projectScore: 100f, prevUsers: 0);
                if (users <= 0)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{size} 프로젝트 최고 점수 첫 주 유저 수가 0 이하입니다. users={users}"));
                }
            }
        }

        private static void ValidateSalesCase(
            List<QAResult> results,
            ProjectSize size,
            float rating,
            float retention,
            int popularity)
        {
            int dailySales = PerkPolicy.CalcDailySales(size, rating, retention, popularity);
            if (dailySales < 0)
            {
                results.Add(new QAResult(
                    QASeverity.Error,
                    "Project Formula",
                    $"일일 판매량이 음수입니다. size={size}, rating={rating}, retention={retention}, popularity={popularity}, dailySales={dailySales}"));
            }

            int dailyGold = PerkPolicy.CalcDailyGold(size, dailySales, goodsSales: 0);
            if (dailyGold < 0)
            {
                results.Add(new QAResult(
                    QASeverity.Error,
                    "Project Formula",
                    $"일일 매출이 음수입니다. size={size}, dailySales={dailySales}, dailyGold={dailyGold}"));
            }
        }

        private static void AssertInt(
            List<QAResult> results,
            string label,
            int expected,
            int actual)
        {
            if (expected == actual)
                return;

            results.Add(new QAResult(
                QASeverity.Error,
                "Project Formula",
                $"{label} 기대값이 다릅니다. expected={expected}, actual={actual}"));
        }

        private static void AssertFloat(
            List<QAResult> results,
            string label,
            float expected,
            float actual)
        {
            if (Mathf.Approximately(expected, actual))
                return;

            results.Add(new QAResult(
                QASeverity.Error,
                "Project Formula",
                $"{label} 기대값이 다릅니다. expected={expected:F2}, actual={actual:F2}"));
        }
    }
}
