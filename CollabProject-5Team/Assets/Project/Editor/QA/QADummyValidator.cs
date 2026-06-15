using System.Collections.Generic;

namespace GameDevTycoon.EditorQA
{
    public sealed class QADummyValidator : IQAValidator
    {
        public string Name => "QA Window Smoke Test";

        public IEnumerable<QAResult> Run()
        {
            yield return new QAResult(
                QASeverity.Info,
                "Smoke Test",
                "QA 에디터 창이 정상적으로 실행되었습니다.");
        }
    }
}
