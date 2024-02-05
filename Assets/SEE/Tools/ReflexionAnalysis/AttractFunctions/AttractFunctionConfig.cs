using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    [System.Serializable]
    public abstract class AttractFunctionConfig
    {
        AttractFunctionType attractFunction;

        [SerializeField]
        private string candidateType = "File";

        public string CandidateType { get => candidateType; set => candidateType = value; }

        [SerializeField]
        private string clusterType = "Cluster";

        public string ClusterType { get => clusterType; set => clusterType = value; }
        public AttractFunctionType AttractFunctionType 
        { 
            get => attractFunction; 
            protected set => attractFunction = value; 
        }
    }
}
