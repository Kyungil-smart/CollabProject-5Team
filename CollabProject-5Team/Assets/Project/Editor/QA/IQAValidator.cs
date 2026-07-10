using System.Collections.Generic;

namespace GameDevTycoon.EditorQA
{
    public interface IQAValidator
    {
        string Name { get; }
        IEnumerable<QAResult> Run();
    }
}
