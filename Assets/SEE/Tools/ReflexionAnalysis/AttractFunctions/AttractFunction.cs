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

        public AttractFunction(ReflexionGraph reflexionGraph, string candidateType)
        {
            this.reflexionGraph= reflexionGraph;
            this.candidateType = candidateType;
        }
        public AttractFunction(ReflexionGraph reflexionGraph, 
                                string candidateType, 
                                Dictionary<string, double> edgeWeights) : this(reflexionGraph, candidateType)
        {
            this.edgeWeights = edgeWeights;
        }

        public static AttractFunction Create(AttractFunctionType attractFunctionType, ReflexionGraph reflexionGraph, string candidateType)
        {
            // TODO: Resolve target language properly, when creating AttractFunctions
            switch (attractFunctionType)
            {
                case AttractFunctionType.CountAttract: return new CountAttract(reflexionGraph, candidateType);

                case AttractFunctionType.NBAttract: return new NBAttract(reflexionGraph, candidateType, useStandardTerms:true, TokenLanguage.Plain, useCda:true);

                case AttractFunctionType.ADCAttract: return new ADCAttract(reflexionGraph, candidateType, TokenLanguage.Plain);
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
