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
            LSIAttract,
            ADCAttract
        }

        /// <summary>
        /// 
        /// </summary>
        protected string candidateType;

        protected ReflexionGraph reflexionGraph;

        protected Dictionary<string, double> edgeWeights = new Dictionary<string, double>();

        public AttractFunction(ReflexionGraph reflexionGraph, string targetType)
        {
            this.reflexionGraph= reflexionGraph;
            this.candidateType = targetType;
        }
        public AttractFunction(ReflexionGraph reflexionGraph, 
                                string targetType, 
                                Dictionary<string, double> edgeWeights) : this(reflexionGraph, targetType)
        {
            this.edgeWeights = edgeWeights;
        }

        public static AttractFunction Create(AttractFunctionType attractFunctionType, ReflexionGraph reflexionGraph, string targetType)
        {
            switch(attractFunctionType)
            {
                case AttractFunctionType.CountAttract: return new CountAttract(reflexionGraph, targetType);
                // TODO: Resolve target language properly
                case AttractFunctionType.NBAttract: return new NBAttract(reflexionGraph, targetType, true, TokenLanguage.Java, true);
            }
            throw new ArgumentException("Given attractFunctionType is currently not implemented");
        }

        protected double GetEdgeWeight(Edge edge)
        {
            return this.edgeWeights.TryGetValue(edge.ID, out var weight) ? weight : 1.0;
        }

        public abstract void HandleMappedEntities(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType);

        public abstract double GetAttractionValue(Node node, Node cluster);

        public abstract string DumpTrainingData();
    }
}
