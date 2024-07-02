using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Abstract class used to implement attract functions. Attract functions have to react to different 
    /// changes within the reflexion graph. They can update custom datastructures which are then used to calculate 
    /// attraction values between differnet nodes.
    /// </summary>
    [Serializable]
    public abstract class AttractFunction
    {
        /// <summary>
        /// Enum representing types of different attract functions.
        /// </summary>
        public enum AttractFunctionType
        {
            CountAttract,
            NBAttract,
            ADCAttract
        }

        /// <summary>
        /// Node type that is considered to be a candidate node
        /// </summary>
        public string CandidateType { get; set; }

        /// <summary>
        /// Node type that is considered to be a cluster node
        /// </summary>
        public string ClusterType { get; set; }

        /// <summary>
        /// Reflexion graph this attract function is reading on.
        /// </summary>
        protected ReflexionGraph reflexionGraph;

        /// <summary>
        /// Dictionary containing weights for different edge types.
        /// </summary>
        protected Dictionary<string, double> edgeWeights = new Dictionary<string, double>();

        /// <summary>
        /// <see cref="EdgeStateCache"/> object that is used to retrieve edge states for a given mapping constellation.
        /// </summary>
        protected EdgeStateCache edgeStateCache;

        /// <summary>
        /// Set of cluster ids for which the candidate attraction values need to be updated.
        /// The concrete implementation of the attract function is responsible to manage this Set.
        /// </summary>
        private HashSet<string> clustersToUpdate;

        /// <summary>
        /// Current candidates which are mapped and were handled by the attract function.
        /// </summary>
        private HashSet<string> handledCandidates;

        /// <summary>
        /// Current candidates which are mapped and were handled by the attract function.
        /// </summary>
        public HashSet<string> HandledCandidates { get => new HashSet<string>(handledCandidates); }

        /// <summary>
        /// Set of cluster ids for which the candidate attraction values need to be updated.
        /// </summary>
        public HashSet<string> ClusterToUpdate { get => new HashSet<string>(clustersToUpdate); }

        /// <summary>
        /// <see cref="CandidateRecommendation"/> object used to retrieve necessary information
        /// </summary>
        protected CandidateRecommendation CandidateRecommendation {get;}

        /// <summary>
        /// Constructor which initializes an <see cref="AttractFunction"/> object.
        /// </summary>
        /// <param name="graph">Reflexion graph this attraction function is reading on.</param>
        /// <param name="candidateRecommendation">CandidateRecommendation object which uses and is used by the created attract function.</param>
        /// <param name="config">Configuration objects containing parameters to configure a attraction function</param>
        protected AttractFunction(ReflexionGraph reflexionGraph, 
                               CandidateRecommendation candidateRecommendation, 
                               AttractFunctionConfig config)
        {
            this.CandidateRecommendation = candidateRecommendation;
            this.reflexionGraph= reflexionGraph;
            this.CandidateType = config.CandidateType;
            this.ClusterType = config.ClusterType;
            this.edgeWeights = config.EdgeWeights != null ? new(config.EdgeWeights) : new();
            this.edgeStateCache = new EdgeStateCache(this.reflexionGraph);
            this.clustersToUpdate = new HashSet<string>();
            this.handledCandidates = new HashSet<string>();
        }

        /// <summary>
        /// Static factory call to create an <see cref="AttractFunction"/> object given a <see cref="AttractFunctionConfig"/> object.
        /// </summary>
        /// <param name="config">Configuration used to create the AttractFunction</param>
        /// <param name="candidateRecommendation">CandidateRecommendation object which uses and is used by the created attract function.</param>
        /// <param name="reflexionGraph">reflexion graph containing the candidates and clusters</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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

        /// <summary>
        /// Returns the edge weight for a given edge.
        /// </summary>
        /// <param name="edge">given edge</param>
        /// <returns>specified weight for type of the edge. 1.0 if the type is unknown.</returns>
        protected double GetEdgeWeight(Edge edge)
        {
            return this.edgeWeights.TryGetValue(edge.Type, out var weight) ? weight : 1.0;
        }

        /// <summary>
        /// Call to clear the edge state cache object.
        /// </summary>
        public void ClearStateCache()
        {
            this.edgeStateCache.ClearCache();
        }

        /// <summary>
        /// Adds all cluster within the reflexion graph to the set of cluster for which attraction 
        /// values need to be updated.
        /// </summary>
        public void AddAllClusterToUpdate()
        {
            foreach (Node cluster in CandidateRecommendation.GetCluster())
            {
                this.AddClusterToUpdate(cluster.ID);
            }
        }

        /// <summary>
        /// Adds a cluster id of the reflexion graph to the set of cluster for which attraction 
        /// values need to be updated.
        /// </summary>
        /// <param name="clusterId">given cluster id</param>
        protected void AddClusterToUpdate(string clusterId)
        {
            if (clusterId != null && !this.ClusterToUpdate.Contains(clusterId))
            {
                clustersToUpdate.Add(clusterId);
            }
        }

        /// <summary>
        /// Removes a cluster id of the reflexion graph from the set of cluster for which attraction 
        /// values need to be updated. 
        /// </summary>
        /// <param name="clusterId">given cluster id</param>
        public void RemoveClusterToUpdate(string clusterId)
        {
            if (this.ClusterToUpdate.Contains(clusterId))
            {
                clustersToUpdate.Remove(clusterId);
            }
        }

        /// <summary>
        /// Method that ensures for a given <see cref="GraphElement"/> object that is in a given subgraph.
        /// </summary>
        /// <param name="subgraph">given subgraph.</param>
        /// <param name="element">given graph element.</param>
        /// <exception cref="NotInSubgraphException">Throws if the given <paramref name="element"/> 
        /// is not within the given <paramref name="subgraph"/></exception>
        private void EnsureSubgraph(ReflexionSubgraphs subgraph, GraphElement element)
        {
            if (!element.IsIn(subgraph))
            {
                throw new NotInSubgraphException(subgraph, element);
            }
        }

        /// <summary>
        /// This Method calculates the current attraction between a given candidate node and a given candidate cluster.
        /// The attraction have to be positively orientied and above 0.
        /// </summary>
        /// <param name="node">given candidate node</param>
        /// <param name="cluster">given cluster node</param>
        /// <returns></returns>
        public abstract double GetAttractionValue(Node node, Node cluster);

        /// <summary>
        /// This method is called if a node was add or removed from a given cluster. 
        /// During this call data structures can be updated which may be used in <see cref="GetAttractionValue"/>() 
        /// </summary>
        /// <param name="cluster">given cluster node</param>
        /// <param name="changedNode">candidate node which was add or removed from the mapping.</param>
        /// <param name="changeType">given change type</param>
        public abstract void HandleChangedCandidate(Node cluster, Node changedNode, ChangeType changeType);

        /// <summary>
        /// This method is called if the state of an edge changes.
        /// During this call data structures can be updated which may be used in <see cref="GetAttractionValue"/>() 
        /// </summary>
        /// <param name="edgeChange">given EdgeChange event</param>
        public abstract void HandleChangedState(EdgeChange edgeChange);

        /// <summary>
        /// The method is called when a new cluster was add to the reflexion graph.
        /// </summary>
        /// <param name="cluster"></param>
        public virtual void HandleAddCluster(Node cluster)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, cluster);
            this.AddClusterToUpdate(cluster.ID);
        }

        /// <summary>
        /// The method is called when a cluster was removed from the reflexion graph.
        /// </summary>
        /// <param name="cluster"></param>
        public virtual void HandleRemovedCluster(Node cluster)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, cluster);
            this.RemoveClusterToUpdate(cluster.ID);
        }

        /// <summary>
        /// The method is called when a new architecture edge was add to the reflexion graph.
        /// This call clears the edge state cache to keep the states consistent.
        /// </summary>
        /// <param name="archEdge"></param>
        public virtual void HandleAddArchEdge(Edge archEdge)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, archEdge);
            this.edgeStateCache.ClearCache();
        }

        /// <summary>
        /// The method is called when a architecture edge was removed to the reflexion graph.
        /// This call clears the edge state cache to keep the states consistent.
        /// </summary>
        /// <param name="archEdge"></param>
        public virtual void HandleRemovedArchEdge(Edge archEdge)
        {
            EnsureSubgraph(ReflexionSubgraphs.Architecture, archEdge);
            this.edgeStateCache.ClearCache();
        }

        /// <summary>
        /// Checks for a given candidate id if the attraction function need to handle it. 
        /// This method is used to avoid multiple handling of the same candidate.
        /// 
        /// TODO: Check if the call HandlingRequired() is still required.
        /// 
        /// </summary>
        /// <param name="candidateId">given candidate id.</param>
        /// <param name="changeType">given change type</param>
        /// <param name="updateHandling">if set to true, the given candidate id 
        /// is considered to be handled after this method is called.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Method which returns a string describing the current training data of the attract function.
        /// Can be used for debug purposes.
        /// </summary>
        /// <returns>string describing the data used for calculations.</returns>
        public abstract string DumpTrainingData();

        /// <summary>
        /// Method which returns true if all data structures used by the attract function 
        /// are empty. Should be returning true if no candidates are mapped.
        /// 
        /// This method is used for consistency checking.
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract bool EmptyTrainingData();

        /// <summary>
        /// Resets the handled candidates.
        /// TODO: Still necessary?
        /// </summary>
        public virtual void Reset()
        {
            this.handledCandidates.Clear();
        }
    }
}
