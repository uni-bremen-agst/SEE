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

        [SerializeField]
        private int iterations = 1;

        [SerializeField]
        private string experimentName = "experiment";

        public string ExperimentName { get => experimentName; set => experimentName = value; }

        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        public int Delta { get; private set; }

        [SerializeField]
        public bool syncExperimentWithView { get; set; }

        [SerializeField]
        public bool IgnoreTieBreakers { get; set; }

        public int Iterations { get => iterations; set => iterations = value; }

        [SerializeField]
        private int masterSeed = 593946;

        public int RootSeed { get => masterSeed; set => masterSeed = value; }

        [SerializeField]
        private double initialMappingPercentage = 0.9;

        public double InitialMappingPercentage { get => initialMappingPercentage; set => initialMappingPercentage = value; }

        [SerializeField]
        // private DirectoryPath outputPath;
        private DataPath outputPath;

        [SerializeField]
        public bool measurePercentileRanks;

        // public DirectoryPath OutputPath { get => outputPath; set => outputPath = value; }
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
                                                        double initialMapping, 
                                                        string name, 
                                                        AttractFunctionType attractFunction,
                                                        float phi = 1.0f)
        {
            RecommendationSettings settings = new RecommendationSettings();
            settings.iterations = n;
            settings.IgnoreTieBreakers = true;
            settings.syncExperimentWithView = false;
            settings.RootSeed = 258506098;
            settings.initialMappingPercentage = initialMapping;
            settings.attractFunctionType = attractFunction;
            settings.CountAttractConfig.Phi = phi;
            settings.AttractFunctionConfig.CandidateType = "Class";
            settings.AttractFunctionConfig.ClusterType = "Cluster";
            settings.OutputPath = new DataPath();
            // settings.OutputPath = new DirectoryPath();
            settings.ExperimentName = name;
            return settings;
        }

    }
}
