using RTG;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    [Serializable]
    public abstract class AttractFunction
    {
        /// <summary>
        /// 
        /// </summary>
        public enum AttractFunctionType
        {
            CountAttract,
            NBAttract,
            ADCAttract
        }

        public string CandidateType { get; set; }
        public string ClusterType { get; set; }

        protected ReflexionGraph reflexionGraph;

        protected Dictionary<string, double> edgeWeights = new Dictionary<string, double>();

        public AttractFunction(ReflexionGraph reflexionGraph, AttractFunctionConfig config)
        {
            this.reflexionGraph= reflexionGraph;
            this.CandidateType = config.CandidateType;
            this.ClusterType = config.ClusterType;
        }
        public AttractFunction(ReflexionGraph reflexionGraph,
                               AttractFunctionConfig config,
                               Dictionary<string, double> edgeWeights) : this(reflexionGraph, config)
        {
            this.edgeWeights = edgeWeights;
        }

        public static AttractFunction Create(AttractFunctionConfig config, ReflexionGraph reflexionGraph)
        {
            switch (config.AttractFunctionType)
            {
                case AttractFunctionType.CountAttract: 
                    return new CountAttract(reflexionGraph,(CountAttractConfig)config);

                case AttractFunctionType.NBAttract: 
                    return new NBAttract(reflexionGraph, (NBAttractConfig)config);
                    
                case AttractFunctionType.ADCAttract: 
                    return new ADCAttract(reflexionGraph, (ADCAttractConfig)config);
            }
            throw new ArgumentException("Given attractFunctionType is currently not implemented");
        }

        protected double GetEdgeWeight(Edge edge)
        {
            return this.edgeWeights.TryGetValue(edge.ID, out var weight) ? weight : 1.0;
        }

        public abstract void HandleChangedNodes(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType);

        public abstract double GetAttractionValue(Node node, Node cluster);

        public abstract string DumpTrainingData();
    }
}
