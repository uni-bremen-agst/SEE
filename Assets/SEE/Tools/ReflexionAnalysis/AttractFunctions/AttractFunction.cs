using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;

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
        protected string targetType;

        protected ReflexionGraph reflexionGraph;

        public AttractFunction(ReflexionGraph reflexionGraph, string targetType)
        {
            this.reflexionGraph= reflexionGraph;
            this.targetType = targetType;
        }

        public void MappingChanged(EdgeEvent edgeEvent)
        {
            // Can source be a cluster? Are they other types than cluster which are representing
            // architecture packages, Use HashSet?
            if (!edgeEvent.Edge.Target.Type.Equals("Cluster")) return;
            
            // TODO: is this safe?
            if (edgeEvent.Change == null) return;

            Node cluster = edgeEvent.Edge.Target;
            Node entity = edgeEvent.Edge.Source;

            // Get targeted childs of currently mapped node
            List<Node> mappedEntities = new List<Node>();
            GetTargetedChilds(entity, mappedEntities, targetType, ReflexionSubgraph.Implementation);

            this.HandleMappedEntities(cluster, mappedEntities, (ChangeType)edgeEvent.Change);
        }

        // TODO: was this done before? Do as Linq?
        private void GetTargetedChilds(Node node, List<Node> entities, string type, ReflexionSubgraph subgraph)
        {
            if (node.Type.Equals(type) && node.IsIn(subgraph))
            {
                entities.Add(node);
            }
            foreach (Node child in node.Children())
            {
                GetTargetedChilds(child, entities, type, subgraph);
            }
            return;
        }

        public static AttractFunction Create(AttractFunctionType attractFunctionType, ReflexionGraph reflexionGraph, string targetType)
        {
            switch(attractFunctionType)
            {
                case AttractFunctionType.CountAttract: return new CountAttract(reflexionGraph, targetType);
                case AttractFunctionType.NBAttract: return new NBAttract(reflexionGraph, targetType, false, true);
            }
            throw new ArgumentException("Given attractFunctionType is currently not implemented");
        }

        public abstract void HandleMappedEntities(Node cluster, List<Node> targetedEntities, ChangeType changeType);

        public abstract double GetAttractionValue(Node node, Node cluster);
    }
}
