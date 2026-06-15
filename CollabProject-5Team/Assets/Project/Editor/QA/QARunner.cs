using System;
using System.Collections.Generic;

namespace GameDevTycoon.EditorQA
{
    public static class QARunner
    {
        public static List<IQAValidator> CreateDefaultValidators()
        {
            return new List<IQAValidator>
            {
                new QADummyValidator(),
                new QAScriptableObjectDiscoveryValidator(),
                new QASheetSyncValidator(),
                new QAEmployeeValidator(),
                new QAReportValidator(),
                new QAReportSimulationValidator(),
                new QAProjectFormulaValidator(),
                new QAQuestCommentValidator(),
                new QAScenePrefabValidator(),
                new QAPlayFlowValidator()
            };
        }

        public static List<QAResult> RunAll(IEnumerable<IQAValidator> validators)
        {
            var results = new List<QAResult>();

            foreach (IQAValidator validator in validators)
            {
                try
                {
                    results.AddRange(validator.Run());
                }
                catch (Exception ex)
                {
                    results.Add(new QAResult(
                        QASeverity.Error,
                        validator.Name,
                        $"검사 중 예외 발생: {ex.Message}"));
                }
            }

            return results;
        }
    }
}
