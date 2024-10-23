using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.Utils.Paths;
using System;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    [System.Serializable]
    public class RecommendationSettings
    {
        [SerializeField]
        private AttractFunction.AttractFunctionType attractFunctionType;

        public AttractFunction.AttractFunctionType AttractFunctionType { get => attractFunctionType; set => attractFunctionType = value; }

        // TODO: Range attribute is only forwared towards the corresponding slider, if the fields are public
        [SerializeField]
        [Range(1, 1000)]
        public int iterations = 1;

        [SerializeField]
        private string experimentName = "experiment";

        public string ExperimentName { get => experimentName; set => experimentName = value; }

        [SerializeField]
        public bool syncWithView { get; set; }

        [SerializeField]
        public bool IgnoreTieBreakers { get; set; }

        [SerializeField]
        [Range(1, 999999)]
        public int rootSeed = 593946;

        [SerializeField]
        [Range(0, 1)]
        public float initialMappingPercentage = 0.5f;

        [SerializeField]
        private DataPath outputPath;

        /// <summary>
        /// Node type determining which nodes are considered to be candidates
        /// </summary>
        [SerializeField]
        private string candidateType = "Class";

        /// <summary>
        /// Node type determining which nodes are considered to be candidates
        /// </summary>
         public string CandidateType { get => candidateType; set => candidateType = value; }

        /// <summary>
        /// Node type determining which nodes are considered to be clusters
        /// </summary>
        [SerializeField]
        private string clusterType = "Cluster";

        /// <summary>
        /// Node type determining which nodes are considered to be clusters
        /// </summary>
        public string ClusterType { get => clusterType; set => clusterType = value; }

        /// <summary>
        /// Determines if the training data of a attract function is logged to a file during an experiment
        /// </summary>
        [SerializeField]
        public bool logTrainingData = false;

        // TODO: Delete this field, measuring of percentile ranks was not fully implemented 
        //[SerializeField]
        //public bool measurePercentileRanks;

        public DataPath OutputPath { get => outputPath; set => outputPath = value; }

        public INodeReader NodeReader { get; set; }

        [SerializeField]
        // [ShowIf("@this.attractFunctionType", AttractFunction.AttractFunctionType.CountAttract)]
        private CountAttractConfig countAttractConfig = new CountAttractConfig();
        public CountAttractConfig CountAttractConfig { get => countAttractConfig; set => countAttractConfig = value; }

        [SerializeField]
        // [ShowIf("@this.attractFunctionType", AttractFunction.AttractFunctionType.ADCAttract)]
        private ADCAttractConfig adcAttractConfig = new ADCAttractConfig();
        public ADCAttractConfig ADCAttractConfig { get => adcAttractConfig; set => adcAttractConfig = value; }

        [SerializeField]
        // [ShowIf("@this.attractFunctionType", AttractFunction.AttractFunctionType.NBAttract)]
        private NBAttractConfig nbAttractConfig = new NBAttractConfig();
        public NBAttractConfig NBAttractConfig { get => nbAttractConfig; set => nbAttractConfig = value; }

        public NoAttractConfig noAttractConfig = new NoAttractConfig();

        public AttractFunctionConfig AttractFunctionConfig 
        {
            get
            {
                switch (this.attractFunctionType)
                {
                    case AttractFunction.AttractFunctionType.CountAttract: 
                        return this.countAttractConfig;
                    case AttractFunction.AttractFunctionType.NBAttract: 
                        return this.NBAttractConfig;
                    case AttractFunction.AttractFunctionType.ADCAttract:
                        return this.ADCAttractConfig;
                    case AttractFunctionType.NoAttract:
                        return noAttractConfig;
                }
                throw new Exception("Unknown attract function type within recommendation settings.");
            }
        } 
        
        public static RecommendationSettings CreateGroup(int n, 
                                                        float initialMapping, 
                                                        string name, 
                                                        AttractFunctionType attractFunction,
                                                        float phi = 1.0f)
        {
            RecommendationSettings settings = new RecommendationSettings();
            settings.iterations = n;
            settings.IgnoreTieBreakers = true;
            settings.syncWithView = false;
            settings.rootSeed = 258506098;
            settings.initialMappingPercentage = initialMapping;
            settings.attractFunctionType = attractFunction;
            settings.CountAttractConfig.Phi = phi;
            settings.CandidateType = "Class";
            settings.ClusterType = "Cluster";
            settings.OutputPath = new DataPath();
            settings.ExperimentName = name;
            return settings;
        }

    }
}
