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
            AssertFloat(results, "점수 가중치 score=25", 0f, PerkPolicy.CalcScoreWeight(25f));
            AssertFloat(results, "점수 가중치 score=75", 0.5f, PerkPolicy.CalcScoreWeight(75f));
            AssertFloat(results, "점수 가중치 score=100", 0.75f, PerkPolicy.CalcScoreWeight(100f));

            if (PerkPolicy.CalcScoreWeight(0f) < 0f)
            {
                results.Add(new QAResult(
                    QASeverity.Error,
                    "Project Formula",
                    "점수 가중치가 음수로 내려갑니다. 판매량 계산에서 최소 0 처리가 필요합니다."));
            }

            foreach (ProjectSize size in new[] { ProjectSize.Small, ProjectSize.Medium, ProjectSize.Large })
            {
                ValidateSalesCase(results, size, quality: 25f, stability: 25f, charm: 25f, retention: 0f, popularity: 0);
                ValidateSalesCase(results, size, quality: 50f, stability: 50f, charm: 50f, retention: 0.5f, popularity: 0);
                ValidateSalesCase(results, size, quality: 100f, stability: 100f, charm: 100f, retention: 1f, popularity: 100);

                int weeklyCost = PerkPolicy.CalcWeeklyCost(size);
                if (weeklyCost < 0)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        "Project Formula",
                        $"{size} 프로젝트 유지비가 음수입니다. weeklyCost={weeklyCost}"));
                }
            }

            int extremeUsers = PerkPolicy.CalcUsers(ProjectSize.Small, projectScore: 0f, prevUsers: 10000);
            if (extremeUsers < 0)
            {
                results.Add(new QAResult(
                    QASeverity.Warning,
                    "Project Formula",
                    $"극단 케이스에서 유저 수가 음수가 될 수 있습니다. small, projectScore=0, prevUsers=10000 => users={extremeUsers}. 서비스 종료/최소값 처리 정책이 필요할 수 있습니다."));
            }

            foreach (ProjectSize size in new[] { ProjectSize.Small, ProjectSize.Medium, ProjectSize.Large })
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
            float quality,
            float stability,
            float charm,
            float retention,
            int popularity)
        {
            int dailySales = PerkPolicy.CalcDailySales(size, quality, stability, charm, retention, popularity);
            if (dailySales < 0)
            {
                results.Add(new QAResult(
                    QASeverity.Error,
                    "Project Formula",
                    $"일일 판매량이 음수입니다. size={size}, quality={quality}, stability={stability}, charm={charm}, retention={retention}, popularity={popularity}, dailySales={dailySales}"));
            }

            int dailyGold = PerkPolicy.CalcDailyGold(size, dailySales);
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
