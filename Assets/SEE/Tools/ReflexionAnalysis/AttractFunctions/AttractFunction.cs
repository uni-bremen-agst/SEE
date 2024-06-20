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
            ADCAttract
        }

        /// <summary>
        /// 
        /// </summary>
        public string CandidateType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ClusterType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected ReflexionGraph reflexionGraph;

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<string, double> edgeWeights = new Dictionary<string, double>();

        /// <summary>
        /// 
        /// </summary>
        protected EdgeStatesCache edgeStateCache;

        /// <summary>
        /// 
        /// </summary>
        private HashSet<string> clustersToUpdate;

        /// <summary>
        /// 
        /// </summary>
        private HashSet<string> handledCandidates;

        public HashSet<string> HandledCandidates { get => new HashSet<string>(handledCandidates); }

        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> ClusterToUpdate { get => new HashSet<string>(clustersToUpdate); }

        protected CandidateRecommendation CandidateRecommendation {get;}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reflexionGraph"></param>
        /// <param name="config"></param>
        public AttractFunction(ReflexionGraph reflexionGraph, 
                               CandidateRecommendation candidateRecommendation, 
                               AttractFunctionConfig config)
        {
            this.CandidateRecommendation = candidateRecommendation;
            this.reflexionGraph= reflexionGraph;
            this.CandidateType = config.CandidateType;
            this.ClusterType = config.ClusterType;
            this.edgeWeights = config.EdgeWeights != null ? new(config.EdgeWeights) : new();
            this.edgeStateCache = new EdgeStatesCache(this.reflexionGraph);
            this.clustersToUpdate = new HashSet<string>();
            this.handledCandidates = new HashSet<string>();
        }

        public static AttractFunction Create(AttractFunctionConfig config, 
                                             CandidateRecommendation candidateRecommendation, 
                                             ReflexionGraph reflexionGraph)
        {
            switch (config.AttractFunctionType)
            {
                case AttractFunctionType.CountAttract: 
                    return new CountAttract(reflexionGraph, candidateRecommendation, (CountAttractConfig)config);

                case AttractFunctionType.NBAttract: 
                    return new NBAttract(reflexionGraph, candidateRecommendation, (NBAttractConfig)config);
                    
                case AttractFunctionType.ADCAttract: 
                    return new ADCAttract(reflexionGraph, candidateRecommendation, (ADCAttractConfig)config);
            }
            throw new ArgumentException("Given attractFunctionType is currently not implemented");
        }

        protected double GetEdgeWeight(Edge edge)
        {
            return this.edgeWeights.TryGetValue(edge.Type, out var weight) ? weight : 1.0;
        }

        public void ClearStateCache()
        {
            this.edgeStateCache.ClearCache();
        }

        public void AddClusterToUpdate(string clusterId)
        {
            if (clusterId != null && !this.ClusterToUpdate.Contains(clusterId))
            {
                clustersToUpdate.Add(clusterId);
            }
        }

        public void RemoveClusterToUpdate(string clusterId)
        {
            if (this.ClusterToUpdate.Contains(clusterId))
            {
                clustersToUpdate.Remove(clusterId);
            }
        }

        public abstract void HandleChangedCandidate(Node cluster, Node nodeChangedInMapping, ChangeType changeType);

        private void EnsureSubgraph(ReflexionSubgraphs subgraph, GraphElement element)
        {
            if(!element.IsIn(subgraph))
            {
                throw new NotInSubgraphException(subgraph, element);
            }
        }

        public virtual void HandleAddCluster(Node cluster)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, cluster);
            this.AddClusterToUpdate(cluster.ID);
        }

        public virtual void HandleRemovedCluster(Node cluster)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, cluster);
            this.RemoveClusterToUpdate(cluster.ID);
        }

        public virtual void HandleAddArchEdge(Edge archEdge)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, archEdge);
            this.edgeStateCache.ClearCache();
        }

        public virtual void HandleRemovedArchEdge(Edge archEdge)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, archEdge);
            this.edgeStateCache.ClearCache();
        }

        public abstract void HandleChangedState(EdgeChange edgeChange);

        // TODO: Check if the call HandlingRequired() is still required.
        public bool HandlingRequired(string candidateId, 
                                     ChangeType changeType,
                                     bool updateHandling)
        {
            if (changeType == ChangeType.Addition)
            {
                if (!handledCandidates.Contains(candidateId))
                {
                    if(updateHandling)
                    {
                        handledCandidates.Add(candidateId);
                    }
                    return true;
                }
                return false;
            } 
            else if (changeType == ChangeType.Removal)
            {
                if (handledCandidates.Contains(candidateId))
                {
                    if(updateHandling)
                    {
                        handledCandidates.Remove(candidateId);
                    }
                    return true;

                }
                return false;
            }

            throw new Exception("Unknown change type.");
        }

        public abstract double GetAttractionValue(Node node, Node cluster);

        public abstract string DumpTrainingData();

        public abstract bool EmptyTrainingData();

        public virtual void Reset()
        {
            this.handledCandidates.Clear();
        }
    }
}
