using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public interface ITextClassifier : IEnumerable<string>
    {
        void AddDocument(string clazz, Document document);

        void DeleteDocument(string clazz, Document document);

        string ClassifyDocument(Document document);

        double ProbabilityForClass(string clazz, Document doc);

        Dictionary<string, int> GetTrainingsData(string clazz);
    }
}
