using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Configuration object which holds common parameters to configure a <see cref="AttractFunction"/> object.
    /// </summary>
    [System.Serializable]
    public abstract class AttractFunctionConfig
    {     
       /// <summary>
       /// Node type determining which nodes are considered to be candidates
       /// </summary>
        private string candidateType = "Class";

        /// <summary>
        /// Node type determining which nodes are considered to be candidates
        /// </summary>
         public string CandidateType { get => candidateType; set => candidateType = value; }

        /// <summary>
        /// Node type determining which nodes are considered to be clusters
        /// </summary>
        private string clusterType = "Cluster";

        /// <summary>
        /// Node type determining which nodes are considered to be clusters
        /// </summary>
         public string ClusterType { get => clusterType; set => clusterType = value; }

        /// <summary>
        /// Attract function type defining the concrete <see cref="AttractFunction"/> object.
        /// </summary>
        public abstract AttractFunctionType AttractFunctionType { get; }

        /// <summary>
        /// Dictionary defining the weight of different edge types which may used for attraction calculation. 
        /// </summary>
        [SerializeField]
        public Dictionary<string, double> EdgeWeights { get; set; }

        /// <summary>
        /// Writes all configuration parameters in an <see cref="XElement"/> object.
        /// </summary>
        /// <returns>The xml element object.</returns>
        public virtual XElement ToXElement()
        {
            XElement configElement = new XElement("AttractFunction");
            XAttribute type = new XAttribute("Type", AttractFunctionType.ToString());
            XAttribute candidateType = new XAttribute("CandidateType", CandidateType);
            XAttribute clusterType = new XAttribute("ClusterType", ClusterType);

            configElement.Add(type);
            configElement.Add(candidateType);
            configElement.Add(clusterType);

            return configElement;
        }
    }
}
