using System.Text;
using System.Xml.Linq;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    [System.Serializable]
    public abstract class AttractFunctionConfig
    {
        private AttractFunctionType attractFunctionType;

        [SerializeField]
        private string candidateType = "File";

        public string CandidateType { get => candidateType; set => candidateType = value; }

        [SerializeField]
        private string clusterType = "Cluster";

        public string ClusterType { get => clusterType; set => clusterType = value; }

        public abstract AttractFunctionType AttractFunctionType { get; }

        public virtual XElement ToXElement()
        {
            StringBuilder percentileRanksStr = new StringBuilder();

            XElement configElement = new XElement("AttractFunction", percentileRanksStr.ToString());
            XAttribute type = new XAttribute("Type", AttractFunctionType.ToString());
            XAttribute candidateType = new XAttribute("CandidateType", AttractFunctionType.ToString());
            XAttribute clusterType = new XAttribute("ClusterType", AttractFunctionType.ToString());

            configElement.Add(type);
            configElement.Add(candidateType);
            configElement.Add(clusterType);

            return configElement;
        }
    }
}
