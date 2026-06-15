using System.Collections.Generic;
using System.Linq;

namespace GameDevTycoon.EditorQA
{
    public sealed class QAEmployeeValidator : IQAValidator
    {
        public string Name => "Employee Data";

        public IEnumerable<QAResult> Run()
        {
            List<AssetEntry<EmployeeImmutableData>> employees =
                QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>("Assets/Project/DB/Employee");

            foreach (QAResult result in ValidateDuplicateIds(employees))
                yield return result;

            foreach (AssetEntry<EmployeeImmutableData> entry in employees)
            {
                foreach (QAResult result in ValidateEmployee(entry))
                    yield return result;
            }

            yield return new QAResult(
                QASeverity.Info,
                "Employee",
                $"직원 데이터 {employees.Count}개 검사를 완료했습니다.",
                employees.Count > 0 ? employees[0].Path : QAAssetUtility.DatabaseRoot,
                employees.Count > 0 ? employees[0].Asset : null);
        }

        private static IEnumerable<QAResult> ValidateDuplicateIds(
            List<AssetEntry<EmployeeImmutableData>> employees)
        {
            foreach (var group in employees.GroupBy(e => e.Asset.id).Where(g => g.Key != 0 && g.Count() > 1))
            {
                string paths = string.Join(", ", group.Select(e => e.Path));
                AssetEntry<EmployeeImmutableData> first = group.First();
                yield return new QAResult(
                    QASeverity.Error,
                    "Employee",
                    $"직원 ID {group.Key}가 중복됩니다. 대상: {paths}",
                    first.Path,
                    first.Asset);
            }
        }

        private static IEnumerable<QAResult> ValidateEmployee(AssetEntry<EmployeeImmutableData> entry)
        {
            EmployeeImmutableData employee = entry.Asset;

            if (employee.id <= 0)
                yield return Error(entry, "직원 ID가 1 이상이어야 합니다.");

            if (string.IsNullOrWhiteSpace(employee.Name))
                yield return Error(entry, "직원 이름이 비어 있습니다.");

            foreach (QAResult result in ValidateRange(entry, "ability", employee.ability))
                yield return result;
            foreach (QAResult result in ValidateRange(entry, "desire", employee.desire))
                yield return result;
            foreach (QAResult result in ValidateRange(entry, "fatigue", employee.fatigue))
                yield return result;
            foreach (QAResult result in ValidateRange(entry, "loyalty", employee.loyalty))
                yield return result;

            if (employee.hiringCost < 0)
                yield return Error(entry, "고용 비용이 음수입니다.");

            if (employee.weekSalary < 0)
                yield return Error(entry, "주급이 음수입니다.");

            if (employee.grade <= 0)
                yield return Warning(entry, "직원 등급이 1 이상이어야 합니다.");

            foreach (QAResult result in ValidateTrait(entry, employee.mainTrait, "대표 특성", allowNone: false))
                yield return result;
            foreach (QAResult result in ValidateTrait(entry, employee.subTrait, "보조 특성", allowNone: true))
                yield return result;
            foreach (QAResult result in ValidateTrait(entry, employee.riskTrait, "리스크 특성", allowNone: false))
                yield return result;

            if (employee.iconNormal == null)
                yield return Warning(entry, "Normal 상태 초상화가 비어 있습니다.");
            if (employee.iconCaution == null)
                yield return Warning(entry, "Caution 상태 초상화가 비어 있습니다.");
            if (employee.iconCritical == null)
                yield return Warning(entry, "Critical 상태 초상화가 비어 있습니다.");
        }

        private static IEnumerable<QAResult> ValidateRange(
            AssetEntry<EmployeeImmutableData> entry,
            string fieldName,
            int value)
        {
            if (value < 0 || value > 100)
            {
                yield return Error(entry, $"{fieldName} 값이 0~100 범위를 벗어났습니다. 현재값: {value}");
            }
        }

        private static IEnumerable<QAResult> ValidateTrait(
            AssetEntry<EmployeeImmutableData> entry,
            Trait trait,
            string label,
            bool allowNone)
        {
            EmployeeImmutableData employee = entry.Asset;

            if (trait == Trait.None)
            {
                if (!allowNone)
                    yield return Error(entry, $"{label}이 None입니다.");
                yield break;
            }

            if (!QATraitUtility.TryGetTraitData(trait, out TraitData traitData))
            {
                yield return Error(entry, $"{label} '{trait}'가 TraitTable에 없습니다.");
                yield break;
            }

            if (!QATraitUtility.TryGetExpectedTraitRole(employee.role, out TraitRole expectedRole))
            {
                yield return Warning(entry, $"{employee.role} 직군은 현재 1차 직원 특성 QA 대상이 아닙니다.");
                yield break;
            }

            if (traitData.role != expectedRole)
            {
                yield return Error(
                    entry,
                    $"{label} '{traitData.displayName}'의 직군({traitData.role})이 직원 직군({employee.role})과 맞지 않습니다.");
            }
        }

        private static QAResult Error(AssetEntry<EmployeeImmutableData> entry, string message)
        {
            return new QAResult(QASeverity.Error, "Employee", message, entry.Path, entry.Asset);
        }

        private static QAResult Warning(AssetEntry<EmployeeImmutableData> entry, string message)
        {
            return new QAResult(QASeverity.Warning, "Employee", message, entry.Path, entry.Asset);
        }
    }
}
