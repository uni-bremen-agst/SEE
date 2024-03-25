using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public interface ITextClassifier : IEnumerable<string>
    {
        void AddDocument(string clazz, IDocument document);

        void DeleteDocument(string clazz, IDocument document);

        void Reset();

        string ClassifyDocument(IDocument document);

        double ProbabilityForClass(string clazz, IDocument doc);

        Dictionary<string, int> GetTrainingsData(string clazz);
    }
}
