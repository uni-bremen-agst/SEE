using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.Utils.Paths;
using System;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// Class containing serializable settings to configure the real time recommendations within the 
    /// scene and the execution of experiments. The settings are serialized within the Unity editor 
    /// and the runtime menu.
    /// </summary>
    [System.Serializable]
    public class RecommendationSettings
    {
        /// <summary>
        /// Attract function that should be used
        /// </summary>
        [SerializeField]
        private AttractFunction.AttractFunctionType attractFunctionType;

        /// <summary>
        /// Attract function that should be used
        /// </summary>
        public AttractFunction.AttractFunctionType AttractFunctionType { get => attractFunctionType; set => attractFunctionType = value; }

        // TODO: Range attribute is only forwared towards the corresponding slider, if the fields are public
        /// <summary>
        /// Integer defining the number of runs during the experiment
        /// </summary>
        [SerializeField]
        [Range(1, 1000)]
        public int iterations = 3;

        /// <summary>
        /// Experiment name used to name the result files.
        /// </summary>
        [SerializeField]
        private string experimentName = "experiment";

        /// <summary>
        /// Experiment name used to name the result files.
        /// </summary>
        public string ExperimentName { get => experimentName; set => experimentName = value; }

        /// <summary>
        /// Determines if nodes are moved towards the architecture layers within the scence when they are automatically mapped.
        /// </summary>
        [SerializeField]
        public bool moveNodes { get; set; }

        /// <summary>
        /// Flag determining if during the process of automated mapping within the scene 
        /// recommendations are resolved by randomness or the mapping choice dialog.
        /// If it is set to true randomness is used.
        /// </summary>
        [SerializeField]
        public bool IgnoreTieBreakers { get; set; }

        /// <summary>
        /// Seed used to generate randomnness or other seeds during the experiment.
        /// </summary>
        [SerializeField]
        [Range(1, 999999)]
        public int rootSeed = 593946;

        /// <summary>
        /// Size of the initial mapping used while creating a initial mapping within the scene or during an experiment.
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        public float initialMappingPercentage = 0.3f;

        /// <summary>
        /// Path where the recommendation component saves output files, like statistical results or .gxl files.
        /// </summary>
        [SerializeField]
        private DataPath outputPath;

        /// <summary>
        /// Path where the recommendation component saves output files, like statistical results or .gxl files.
        /// </summary>
        public DataPath OutputPath { get => outputPath; set => outputPath = value; }

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

        // TODO: measuring of percentile ranks was not fully implemented 
        //[SerializeField]
        //public bool measurePercentileRanks;

        /// <summary>
        /// Node Reader object that will be used for language based attract languages.
        /// This object can be set in to allow the usage of a custom 
        /// reader. This field is allowed to be null.
        /// </summary>
        public INodeReader NodeReader { get; set; }

        /// <summary>
        /// Config object to configure CountAttract
        /// </summary>
        [SerializeField]
        private CountAttractConfig countAttractConfig = new CountAttractConfig();

        /// <summary>
        /// Config object to configure CountAttract
        /// </summary>
        public CountAttractConfig CountAttractConfig { get => countAttractConfig; set => countAttractConfig = value; }

        /// <summary>
        /// Config object to configure ADCAttract
        /// </summary>
        [SerializeField]
        private ADCAttractConfig adcAttractConfig = new ADCAttractConfig();

        /// <summary>
        /// Config object to configure ADCAttract
        /// </summary>
        public ADCAttractConfig ADCAttractConfig { get => adcAttractConfig; set => adcAttractConfig = value; }

        /// <summary>
        /// Config object to configure NBAttract
        /// </summary>
        [SerializeField]
        private NBAttractConfig nbAttractConfig = new NBAttractConfig();

        /// <summary>
        /// Config object to configure NBAttract
        /// </summary>
        public NBAttractConfig NBAttractConfig { get => nbAttractConfig; set => nbAttractConfig = value; }

        /// <summary>
        /// Config object to configure NoAttract
        /// </summary>
        public NoAttractConfig noAttractConfig = new NoAttractConfig();

        /// <summary>
        /// Returns the attract function config object matching the value of the field <see cref="attractFunctionType"/>
        /// </summary>
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
        
        /// <summary>
        /// Creates group evaluation experiment.
        /// </summary>
        /// <param name="n">number iterations</param>
        /// <param name="initialMapping">initial mapping size</param>
        /// <param name="name">experiment name</param>
        /// <param name="attractFunction">used attract function type</param>
        /// <param name="phi">optional phi parameter used for count attract</param>
        /// <returns></returns>
        public static RecommendationSettings CreateGroup(int n, 
                                                        float initialMapping, 
                                                        string name, 
                                                        AttractFunctionType attractFunction,
                                                        float phi = 1.0f)
        {
            RecommendationSettings settings = new RecommendationSettings();
            settings.iterations = n;
            settings.IgnoreTieBreakers = true;
            settings.moveNodes = false;
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
