using Accord.Statistics.Models.Fields.Features;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using InControl.NativeDeviceProfiles;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    [System.Serializable]
    public class MappingExperimentConfig
    {
        [SerializeField]
        private AttractFunction.AttractFunctionType attractFunctionType;

        public AttractFunction.AttractFunctionType AttractFunctionType { get => attractFunctionType; set => attractFunctionType = value; }

        [SerializeField]
        private int iterations = 100;
         
        public int Iterations { get => iterations; set => iterations = value; }

        [SerializeField]
        private int masterSeed = 593946;

        public int MasterSeed { get => masterSeed; set => masterSeed = value; }

        [SerializeField]
        private double initialMappingPercentage = 0.5;

        public double InitialMappingPercentage { get => initialMappingPercentage; set => initialMappingPercentage = value; }

        [SerializeField]
        private DirectoryPath outputPath;

        public DirectoryPath OutputPath { get => outputPath; set => outputPath = value; }

        [SerializeField]
        [ShowIf("@this.attractFunctionType", AttractFunction.AttractFunctionType.CountAttract)]
        private CountAttractConfig countAttractConfig = null;

        [SerializeField]
        [ShowIf("@this.attractFunctionType", AttractFunction.AttractFunctionType.ADCAttract)]
        private ADCAttractConfig ADCAttractConfig = null;

        [SerializeField]
        [ShowIf("@this.attractFunctionType", AttractFunction.AttractFunctionType.NBAttract)]
        private NBAttractConfig NBAttractConfig = null;

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
                }
                return null;
            }
            set 
            {
                attractFunctionType = value.AttractFunctionType;
                switch (value.AttractFunctionType)
                {
                    case AttractFunction.AttractFunctionType.CountAttract:
                        this.countAttractConfig = (CountAttractConfig)value;
                        break;
                    case AttractFunction.AttractFunctionType.NBAttract:
                        this.NBAttractConfig = (NBAttractConfig)value;
                        break;
                    case AttractFunction.AttractFunctionType.ADCAttract:
                        this.ADCAttractConfig = (ADCAttractConfig)value;
                        break;
                }
            }
        } 
        
    }
}
