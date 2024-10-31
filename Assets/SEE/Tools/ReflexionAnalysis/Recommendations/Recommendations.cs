using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;
using Node = SEE.DataModel.DG.Node;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This object provides the operations to calculate candidate recommendations. 
    /// It sets up a attract function, manages the attraction value matrix and determines 
    /// which nodes are cluster and candidates regarding a given <see cref="RecommendationSettings"/>
    /// object. The configuration of this object can also be updated with different <see cref="RecommendationSettings"/>
    /// objects. This object registers itself to the event system of a given reflexion graph
    /// and receives and processes events of the graph and forwards it a <see cref="AttractFunction"/> object.
    /// </summary>
    public class Recommendations : IObserver<ChangeEvent>
    {
        /// <summary>
        /// The reflexion graph that is used to calculate the recommendations and containing
        /// all candidates and clusters.
        /// </summary>
        public ReflexionGraph ReflexionGraph { get; private set; }

        /// <summary>
        /// The reflexion graph that is used as the oracle graph and providing information 
        /// about the expected mapping.
        /// </summary>
        public ReflexionGraph OracleGraph { get; private set; }

        /// <summary>
        /// Property set to true if a oracle reflexion graph is loaded. False otherwise.
        /// </summary>
        public bool OracleGraphLoaded { get => OracleGraph != null; }

        /// <summary>
        /// <see cref="AttractFunction"/> object that is currently used.
        /// </summary>
        public AttractFunction AttractFunction { get => attractFunction; }
        /// <summary>
        /// Object representing the currently used attractFunction.
        /// </summary>
        private AttractFunction attractFunction;

        /// <summary>
        /// object which calculates candidate sets and recommendations.
        /// </summary>
        IRecommendationFilter recommendationFilter = new HugMeFilter();

        /// <summary>
        /// List of <see cref="MappingPair"/> objects representing all currently calculated attraction values between the 
        /// corresponding node pairs.
        /// </summary>
        public IEnumerable<MappingPair> MappingPairs { get { return recommendationFilter.GetMappingPairs(); } }

        /// <summary>
        /// Set of Nodes containing the unmapped candidates
        /// </summary>
        public HashSet<string> UnmappedCandidates { get; private set; }

        /// <summary>
        /// Subscription returned when registering to a reflexion graph.
        /// </summary>
        private IDisposable subscription;

        /// <summary>
        /// Object used to record information about mapping process.
        /// </summary>
        public CandidateRecommendationStatistics Statistics { get; private set; }

        /// <summary>
        /// Edge type for the edges used in the tree view graph representing the recommendations for a node.
        /// </summary>
        private string recommendationEdgeType = "Recommended With";

        /// <summary>
        /// Delta used to compare attraction values. If the difference of two attraction values is not bigger than the delta
        /// the values are treated as being the same.
        /// </summary>
        public static double ATTRACTION_VALUE_DELTA = 0.001;

        /// <summary>
        /// Construction initializes a new instance of <see cref="Recommendations"/>
        /// </summary>
        public Recommendations()
        {
            Statistics = new CandidateRecommendationStatistics(this);
        }

        /// <summary>
        /// Returns all recommended mapping pairs which the internal filter does allow to be mapped automatically
        /// </summary>
        /// <returns>IEnumerable object containing the mapping pairs.</returns>
        public IEnumerable<MappingPair> GetAutomaticMappings()
        {
            return this.recommendationFilter.GetAutomaticMappings();
        }

        /// <summary>
        /// Returns all mapping pair objects which are currently recommended by the filter.
        /// </summary>
        /// <param name="node">Given Node</param>
        /// <returns>IEnumerable object containing the mapping pairs.</returns>
        public IEnumerable<MappingPair> GetRecommendations()
        {
            return this.recommendationFilter.GetRecommendations();
        }

        /// <summary>
        /// Returns all mapping pair objects which are currently recommended by the filter for the given node.
        /// </summary>
        /// <param name="node">Given Node</param>
        /// <returns>IEnumerable object containing the mapping pairs.</returns>
        public IEnumerable<MappingPair> GetRecommendations(Node node)
        {
            if(IsCandidate(node))
            {
                return this.recommendationFilter.GetRecommendationForCandidate(node.ID);
            } 
            else if(IsCluster(node))
            {
                return this.recommendationFilter.GetRecommendationForCluster(node.ID);
            }

            return new List<MappingPair>();
        }

        /// <summary>
        /// This method updates this object with the given settings and a given reflexion graph.
        /// After the call the attraction function for the given attract function type is initialized 
        /// and the reflexion analysis is rerun for the given graph to calculate the recommendations.
        /// </summary>
        /// <param name="reflexionGraph">Reflexion graph recommendations are calculated for.</param>
        /// <param name="recommendationSettings">setting object containing necessary information to setup the recommendations</param>
        /// <param name="oracleMapping">Optional graph containing a mapping used to construct the <see cref="OracleGraph"/></param>
        public void UpdateConfiguration(ReflexionGraph reflexionGraph,
                                        RecommendationSettings recommendationSettings,
                                        Graph oracleMapping = null)
        {
            if (reflexionGraph == null)
            {
                throw new Exception("Could not update configuration. Reflexion graph is null.");
            }

            ReflexionGraph = reflexionGraph;

            if (recommendationSettings.AttractFunctionConfig == null)
            {
                throw new Exception("Could not update configuration. Attract function config in recommendation settings is null");
            }

            if (oracleMapping != null)
            {
                (Graph implementation, Graph architecture, _) = ReflexionGraph.Disassemble();
                OracleGraph = new ReflexionGraph(implementation, architecture, oracleMapping);
                OracleGraph.RunAnalysis();
            }
            else
            {
                OracleGraph = null;
            }

            recommendationSettings.AttractFunctionConfig.CandidateType = recommendationSettings.CandidateType;
            recommendationSettings.AttractFunctionConfig.ClusterType = recommendationSettings.ClusterType;
            attractFunction = AttractFunction.Create(recommendationSettings.AttractFunctionConfig, this, reflexionGraph);

            // TODO: Handle node reader initialization differently?
            // (This set operation is only necessary for the test cases)
            if (recommendationSettings.NodeReader != null && attractFunction is LanguageAttract)
            {
                ((LanguageAttract)attractFunction).SetNodeReader(recommendationSettings.NodeReader);
            }

            subscription?.Dispose();
            subscription = reflexionGraph.Subscribe(this);

            // Stop and reset the recording
            bool wasActive = Statistics.Active;
            Statistics.Reset();

            Statistics.SetConfigInformation(recommendationSettings);
            recommendationFilter.Reset();
            this.UnmappedCandidates = this.GetUnmappedCandidates().Select(n => n.ID).ToHashSet();

            this.attractFunction.AddAllClusterToUpdate();
            this.attractFunction.AddAllCandidatesToUpdate();

            ReflexionGraph.RunAnalysis();

            // Restart after the analysis was run, so initially/already
            // mapped candidates will not recorded twice
            if (wasActive)
            {
                Statistics.StartRecording();
            }
            this.UpdateRecommendations();
        }


        /// <summary>
        /// This method constructs a graph representing the recommendations for a given Node. 
        /// The graph is used to be visualized within the tree view window. The given Node 
        /// is the root of the graph and the edges pointing to the root represent the attraction 
        /// of other nodes towards the root.
        /// 
        /// TODO: this interface needs work(Make it async)
        /// 
        /// </summary>
        /// <param name="examinedNode">node that will be root of the graph. This usually is the node clicked within the scene.</param>
        /// <returns>Graph representing the attraction of all other nodes to the <paramref name="examinedNode"/></returns>
        public Graph GetRecommendationTree(Node examinedNode)
        {
            List<Node> candidates = examinedNode.IsInArchitecture() ? this.GetCandidates() : this.GetCluster();

            Graph graph = new Graph("", "Recommendations");

            Node examinedNodeClone = (Node)examinedNode.Clone();
            graph.AddNode(examinedNodeClone);
            graph.AddSingleRoot(out _);

            HashSet<string> visisited = new HashSet<string>();
            List<MappingPair> currentMappingPairs = new List<MappingPair>();

            if (MappingPairs.Count() == 0)
            {
                return graph;
            }

            foreach (Node candidate in candidates)
            {
                // skip mapped implementation nodes
                if (candidate.IsInImplementation() && this.ReflexionGraph.MapsTo(candidate) != null)
                {
                    continue;
                }

                MappingPair mappingPair = examinedNode.IsInArchitecture() ? 
                            recommendationFilter.GetMappingPair(candidate.ID,examinedNode.ID)
                          : recommendationFilter.GetMappingPair(examinedNode.ID, candidate.ID);

                if (mappingPair.AttractionValue > 0)
                {
                    currentMappingPairs.Add(mappingPair); 
                }
            }

            currentMappingPairs.Sort((x,y) => y.CompareTo(x));

            foreach (MappingPair mappingPair in currentMappingPairs)
            {
                Node relatedNode = examinedNode.IsInArchitecture() ? mappingPair.Candidate : mappingPair.Cluster;
                visisited.Add(relatedNode.ID);
                Node relatedNodeClone = (Node)relatedNode.Clone();

                relatedNodeClone.ItsGraph = null;
                relatedNodeClone.ID = relatedNode.ID;
                relatedNodeClone.SourceName = relatedNode.ID;
                Edge edge = new Edge(relatedNodeClone, 
                                    examinedNodeClone, 
                                    $"{recommendationEdgeType} {Math.Round(mappingPair.AttractionValue, 4)}");
                graph.AddNode(relatedNodeClone);
                examinedNode.AddChild(relatedNodeClone);
                graph.AddEdge(edge);
            }

            return graph;
        }

        #region eventHandling

        /// <summary>
        /// Completion callback called by the event system.
        /// </summary>
        public void OnCompleted()
        {
            Debug.Log("OnCompleted() from recommendation.");
            this.Statistics?.StopRecording();
        }

        /// <summary>
        /// Error callback called by the event system.
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
            Debug.Log("OnError() from recommendation.");
            this.Statistics?.StopRecording();
        }

        /// <summary>
        /// Receives the change event of the reflexion graph within this object.
        /// 
        /// This call processes the events and forwards the necessary information to the attract function.
        /// 
        /// </summary>
        /// <param name="changeEvent">change event object</param>
        public void OnNext(ChangeEvent changeEvent)
        {
            if (!this.ReflexionGraph.AnalysisInitialized)
            {
                return;
            }

            switch (changeEvent)
            {
                case MapsToChange mapsToEvent:
                    OnNextMapsToChange(mapsToEvent);
                    return;
                case EdgeChange edgeChangeEvent:
                    AttractFunction.HandleChangedState(edgeChangeEvent);
                    return;
                case EdgeEvent edgeEvent:
                    if (edgeEvent.Affected == ReflexionSubgraphs.Architecture)
                    {
                        if (ReflexionGraph.IsSpecified(edgeEvent.Edge))
                        {
                            if (edgeEvent.Change == ChangeType.Addition)
                            {
                                this.AttractFunction.HandleAddArchEdge(edgeEvent.Edge);
                            }
                            else if (edgeEvent.Change == ChangeType.Removal)
                            {
                                this.AttractFunction.HandleRemovedArchEdge(edgeEvent.Edge);
                            }
                        }
                    } 
                    else if(edgeEvent.Affected == ReflexionSubgraphs.Implementation)
                    {
                      // TODO: New add implementation edges need to be handled in the future
                    } 
                    return;
                case NodeEvent nodeEvent:
                    if (nodeEvent.Node.IsInArchitecture())
                    {
                        if (nodeEvent.Change == ChangeType.Addition)
                        {
                            this.AttractFunction.HandleAddCluster(nodeEvent.Node);
                        }
                        else if (nodeEvent.Change == ChangeType.Removal)
                        {
                            this.AttractFunction.HandleRemovedCluster(nodeEvent.Node);
                            this.recommendationFilter.RemoveCluster(nodeEvent.Node.ID);
                        }
                    }
                    return;
                default:
                    break;
            }
        }

        /// <summary>
        /// Receives and forwards the maps to change event to the attract function.
        /// Updates the candidate set and records the chosen choice if recording is 
        /// active.
        /// 
        /// </summary>
        /// <param name="mapsToChange">received event</param>
        private void OnNextMapsToChange(MapsToChange mapsToChange)
        {
            if (!IsCandidate(mapsToChange.Source, this.AttractFunction.CandidateType))
            {
                return;
            }

            UpdateCandidateSet(mapsToChange.Source.ID, mapsToChange.Change);

            if (Statistics.Active)
            {
                MappingPair chosenMappingPair = recommendationFilter.GetMappingPair(mapsToChange.Source.ID, mapsToChange.Target.ID);

                if (chosenMappingPair == null)
                {
                    // For the very first mapped node, nodes removed from the mapping, and already mapped childs
                    // there is no previously calculated mappingpair available.
                    // So we create a corresponding mapping pair manually
                    chosenMappingPair = new MappingPair(mapsToChange.Source, mapsToChange.Target, -1.0d);
                }

                recommendationFilter.RemoveCandidate(mapsToChange.Source.ID);
                AttractFunction.HandleChangedCandidate(mapsToChange.Target, mapsToChange.Source, (ChangeType)mapsToChange.Change);
                UpdateRecommendations();
                chosenMappingPair.ChangeType = (ChangeType)mapsToChange.Change;
                Statistics.RecordChosenMappingPair(chosenMappingPair);
            }
            else
            {
                recommendationFilter.RemoveCandidate(mapsToChange.Source.ID);
                AttractFunction.HandleChangedCandidate(mapsToChange.Target, mapsToChange.Source, (ChangeType)mapsToChange.Change);
            }
        } 
        #endregion

        /// <summary>
        /// Updates the set of unmapped candidates given a node id and the type of change.
        /// </summary>
        /// <param name="candidateId">Candidate id that will be add or removed from the set of unmapped candidates</param>
        /// <param name="change">changetype</param>
        private void UpdateCandidateSet(string candidateId, ChangeType? change)
        {
            if (change == ChangeType.Removal)
            {
                this.UnmappedCandidates.Add(candidateId);
            }
            else if (change == ChangeType.Addition)
            {
                this.UnmappedCandidates.Remove(candidateId);
            }
        }

        /// <summary>
        /// Iterates all cluster which should be updated and recalculates the attraction values 
        /// between the candidates and the cluster to update.
        /// </summary>
        public void UpdateRecommendations()
        {
            foreach (string clusterId in this.AttractFunction.ClusterToUpdate)
            {
                Node cluster = ReflexionGraph.GetNode(clusterId);

                if (cluster == null)
                {
                    this.AttractFunction.RemoveClusterToUpdate(clusterId);
                    recommendationFilter.RemoveCluster(clusterId);
                    UnityEngine.Debug.LogWarning($"Cluster {clusterId} could not be found within the graph.");
                    continue;
                }

                foreach (string candidateId in this.AttractFunction.CandidatesToUpdate)
                {
                    Node candidate = this.ReflexionGraph.GetNode(candidateId);

                    // A check if an 'unmapped' candidate might have been already mapped is still required 
                    // because its parent might was mapped, but the corresponding event was not received yet 
                    if (ReflexionGraph.MapsTo(candidate) != null)
                    {
                        recommendationFilter.RemoveCandidate(candidateId);
                    } 
                    else
                    {
                        double attractionValue = AttractFunction.GetAttractionValue(candidate, cluster);
                        MappingPair mappingPair = new MappingPair(candidate: candidate, cluster: cluster, attractionValue: attractionValue);
                        recommendationFilter.UpdateMappingPair(mappingPair);
                    }
                }
                this.AttractFunction.RemoveClusterToUpdate(clusterId);
            }

            // TODO: delete this(percentile rank calculation
            //if (Statistics?.Active ?? false)
            //{
            //    // Keep track of all attraction values for statistical purposes
            //    // 
            //    // Statistics.RecordMappingPairs(MappingPairs);
            //}
        }

        /// <summary>
        /// Creates mappings pairs that can be used to create an initial mapping based on the given parameters.
        /// 
        /// TODO: change return type to IEnumerable<MappingPair>
        /// 
        /// </summary>
        /// <param name="percentage">percentage describing how many candidates shall be contained in the mapping.</param>
        /// <param name="seed">seed to deriving the randomness to choose the mapped candidates</param>
        /// <param name="reflexionGraph">Reflexiongraph for which the mapping is constructed.</param>
        /// <param name="oracleGraph">Oracle reflexion graph containing used to choose the expected cluster.</param>
        /// <returns>Datastructure describing the mapping</returns>
        /// <exception cref="Exception">Throws if the percentage is not between 0 and 1. Throws if the given parameter objects are null</exception>
        public Dictionary<Node, HashSet<Node>> CreateInitialMapping(double percentage,
                                                                            int seed)
        {
            Dictionary<Node, HashSet<Node>> initialMapping = new Dictionary<Node, HashSet<Node>>();

            if (percentage > 1 || percentage < 0)
            {
                throw new ArgumentException("Parameter percentage have to be a double changeEvent between 0.0 and 1.0");
            }
            if (OracleGraph == null)
            {
                throw new ArgumentException("OracleGraph is null. Cannot generate initial mapping.");
            }
            if (ReflexionGraph == null)
            {
                throw new ArgumentException("ReflexionGraph is null. Cannot generate initial mapping.");
            }

            List<Node> candidates = GetCandidates();

            List<Node> mappedCandidates = GetMappedCandidates();
            
            List<Node> unmappedCandidates = GetUnmappedCandidates();

            List<Node> oracleCluster = GetOracleCluster();

            UnityEngine.Debug.Log($"Generate initial mapping with seed {seed} for {candidates.Count}");
            System.Random rand = new System.Random(seed);

            int candidatesCount = candidates.Count;
            double alreadyMappedNodes = mappedCandidates.Count;
            double artificallyMappedNodes = 0;
            double currentPercentage = 0;

            Dictionary<string, List<Node>> expectedNodes = new();

            foreach (Node currentCluster in oracleCluster)
            {
                expectedNodes[currentCluster.ID] = new List<Node>();
            }

            HashSet<string> candidatesAvailableToMap = new();

            foreach (Node unmappedCandidate in unmappedCandidates)
            {
                if(GetExpectedClusterID(unmappedCandidate.ID) != null)
                {
                    expectedNodes[GetExpectedClusterID(unmappedCandidate.ID)].Add(unmappedCandidate);
                    candidatesAvailableToMap.Add(unmappedCandidate.ID);
                }
            }

            while (currentPercentage < percentage && candidatesAvailableToMap.Count > 0)
            {
                foreach (Node currentCluster in oracleCluster)
                {
                    int countUnmappedExpectedCandidates = expectedNodes[currentCluster.ID].Count();
                    if (countUnmappedExpectedCandidates > 0)
                    {
                        Node nodeToMap = expectedNodes[currentCluster.ID][rand.Next(countUnmappedExpectedCandidates)];

                        if (OracleGraph.MapsTo(nodeToMap).ID != currentCluster.ID)
                        {
                            throw new Exception($"Chosen node for initial mapping is not assigned to the correct cluster. currentCluster.ID={currentCluster.ID} OracleGraph.MapsTo(nodeToMap).ID={OracleGraph.MapsTo(nodeToMap)?.ID}");
                        }

                        AddToInitialMapping(ReflexionGraph.GetNode(currentCluster.ID), nodeToMap);
                        expectedNodes[currentCluster.ID].Remove(nodeToMap);
                        candidatesAvailableToMap.Remove(nodeToMap.ID);
                        artificallyMappedNodes++;
                    }
                    else
                    {
                        continue;
                    }
                }
                currentPercentage = (artificallyMappedNodes + alreadyMappedNodes) / candidatesCount;
            }

            return initialMapping;

            void AddToInitialMapping(Node mapsTo, Node node)
            {
                HashSet<Node> nodes;
                if (!initialMapping.TryGetValue(mapsTo, out nodes))
                {
                    nodes = new HashSet<Node>();
                    initialMapping[mapsTo] = nodes;
                }
                nodes.Add(node);
            }
        }

        /// <summary>
        /// Returns if a mapping of a candidate to a cluster would be a hit.
        /// </summary>
        /// <param name="candidateID">given candidate id</param>
        /// <param name="clusterID">given cluster id</param>
        /// <param name="oracleGraph">reflexion graph used as a oracle</param>
        /// <returns></returns>
        public static bool IsHit(string candidateID, string clusterID, ReflexionGraph oracleGraph)
        {
            HashSet<string> candidateAscendants = oracleGraph.GetNode(candidateID).Ascendants().Select(n => n.ID).ToHashSet();
            HashSet<string> clusterAscendants = oracleGraph.GetNode(clusterID).Ascendants().Select(n => n.ID).ToHashSet();
            return oracleGraph.Edges().Any(e => e.IsInMapping()
                                                && candidateAscendants.Contains(e.Source.ID)
                                                && clusterAscendants.Contains(e.Target.ID));
        }

        /// <summary>
        /// Returns if a mapping of a candidate to a cluster would be a hit.
        /// 
        /// Uses the oracle reflexion graph loaded in this object.
        /// </summary>
        /// <param name="candidateID">given candidate id</param>
        /// <param name="clusterID">given cluster id</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool IsHit(string candidateID, string clusterID)
        {
            if (OracleGraph == null)
            {
                throw new Exception("Cannot determine if node was correctly mapped. No Oracle graph loaded.");
            }

            return IsHit(candidateID, clusterID, OracleGraph);
        }

        /// <summary>
        /// Returns the expected cluster given an oracle graph and a candidate id.
        /// </summary>
        /// <param name="oracleGraph">reflexion graph used as a oracle</param>
        /// <param name="candidateID">given candidate id</param>
        /// <returns></returns>
        public static string GetExpectedClusterID(ReflexionGraph oracleGraph, string candidateID)
        {
            return oracleGraph.MapsTo(oracleGraph.GetNode(candidateID))?.ID;
        }

        /// <summary>
        /// Returns the expected cluster given a candidate id.
        /// 
        /// Uses the oracle reflexion graph loaded in this object.
        /// </summary>
        /// <param name="candidateID"></param>
        /// <returns>Returns the id of the expected cluster. Null if the oracle graph does 
        /// not hold information about the candidate.</returns>
        /// <exception cref="Exception"></exception>
        public string GetExpectedClusterID(string candidateID)
        {
            if (OracleGraph == null)
            {
                throw new Exception($"Cannot determine expected cluster for node ID {candidateID}. No Oracle graph loaded.");
            }

            return GetExpectedClusterID(OracleGraph, candidateID);
        }

    
        /// <summary>
        /// Returns the mapping edge within the oracle graph which determines the expected cluster 
        /// for the node corresponding to the given node ID.
        /// </summary>
        /// <param name="candidateID">given node ID.</param>
        /// <returns>The determing oracle edge</returns>
        /// <exception cref="Exception">Throws an Exception if the oracle mapping is ambigous or incomplete 
        /// for the given node id.</exception>
        public Edge GetOracleEdge(string candidateID)
        {
            List<Edge> oracleEdges = this.OracleGraph.Edges().Where(
            (e) => e.IsInMapping() && e.Source.PostOrderDescendants().Any(n => string.Equals(n.ID, candidateID))).ToList();

            if (oracleEdges.Count > 1)
            {
                throw new Exception("Oracle Mapping is Ambigous.");
            }
            if (oracleEdges.Count == 0)
            {
                throw new Exception($"Oracle Mapping is Incomplete. There is no information about the node {candidateID}");
            }

            return oracleEdges[0];
        }

        /// <summary>
        /// Calculates the rank for a given candidate id within a list of mapping pairs.
        /// </summary>
        /// <param name="candidateID"></param>
        /// <param name="mappingPairs"></param>
        /// <returns></returns>
        public double CalculatePercentileRank(string candidateID,
                                              List<MappingPair> mappingPairs)
        {
            // get corresponding oracle edge to determine all allowed clusters for the candidate
            Edge oracleEdge = this.GetOracleEdge(candidateID);
            return CalculatePercentileRank(candidateID, mappingPairs, oracleEdge);
        }

        /// <summary> 
        /// Calculates the rank for a given candidate id within a list of mapping pairs
        /// regarding an oracle edge. 
        /// 
        /// precondition: oracleEdge describes the mapsto relation for the given candidateID
        /// 
        /// </summary>
        /// <param name="candidateID"></param>
        /// <param name="mappingPairs"></param>
        /// <param name="oracleEdge"></param>
        /// <returns></returns>
        public static double CalculatePercentileRank(string candidateID, 
                                            List<MappingPair> mappingPairs, 
                                            Edge oracleEdge)
        {
            // sort mappings by attractionValue
            mappingPairs.Sort();
         
            // Get all clusters where the candidate would be correctly mapped regarding the oracle edge 
            HashSet<string> clusterIDs = oracleEdge.Target.PostOrderDescendants().Select(c => c.ID).ToHashSet();

            // Get all candidate ids of the mapping pairs which are pointing to a allowed cluster
            List<string> orderedCandidateIds = new List<string>();
            bool containsCandidate = false;
            foreach (MappingPair mappingPair in mappingPairs)
            {
                if(clusterIDs.Contains(mappingPair.ClusterID))
                {
                    if(mappingPair.CandidateID.Equals(candidateID))
                    {
                        containsCandidate = true;
                    }
                    orderedCandidateIds.Add(mappingPair.CandidateID);
                }
            }

            if(!containsCandidate)
            {
                return -1.0;
            }

            // Calculation of percentileRank
            // TODO: divide the list into plateaus, so mappingPairs with the same attraction have the same rank.
            double percentileRank = 1 - (((double)orderedCandidateIds.IndexOf(candidateID)) / orderedCandidateIds.Count);
            percentileRank = Math.Round(percentileRank, 4);
            return percentileRank;
        }

        /// <summary>
        /// Returns all nodes considered to be candidates within a given reflexion graph.
        /// </summary>
        /// <param name="graph">given reflexio graph</param>
        /// <returns>List containing the candidates</returns>
        public List<Node> GetCandidates(ReflexionGraph graph)
        {
            return graph.Nodes().Where(n => this.IsCandidate(n)).ToList();
        }

        /// <summary>
        /// Returns all nodes considered to be candidates within the loaded reflexion graph.
        /// </summary>
        /// <returns>List containing the candidates</returns>
        public List<Node> GetCandidates() 
        {
            return GetCandidates(ReflexionGraph);
        }

        /// <summary>
        /// Returns all nodes considered to be clusters given a reflexion graph.
        /// </summary>
        /// <param name="graph">given reflexio graph</param>
        /// <param name="clusterType"></param>
        /// <returns>List containing the clusters</returns>
        private static List<Node> GetCluster(ReflexionGraph graph, string clusterType)
        {
            return graph.Nodes().Where(n => n.Type.Equals(clusterType) && n.IsInArchitecture()).ToList();
        }

        /// <summary>
        /// Returns all nodes considered to be clusters within the loaded reflexion graph.
        /// </summary>
        /// <returns>List containing the cluster</returns>
        public List<Node> GetCluster()
        {
            return GetCluster(ReflexionGraph, attractFunction.ClusterType);
        }

        public List<Node> GetOracleCluster()
        {
            return GetCluster(OracleGraph, attractFunction.ClusterType);
        }

        /// <summary>
        ///  Returns all nodes considered to be candidates within the loaded reflexion graph 
        ///  which are currently unmpapped.
        /// </summary>
        /// <returns></returns>
        public List<Node> GetUnmappedCandidates()
        {
            return GetCandidates(ReflexionGraph).Where(c => ReflexionGraph.MapsTo(c) == null).ToList();
        }

        /// <summary>
        ///  Returns all nodes considered to be candidates within the loaded reflexion graph 
        ///  which are currently mpapped.
        /// </summary>
        /// <returns></returns>
        public List<Node> GetMappedCandidates()
        {
            return GetCandidates(ReflexionGraph).Where(c => ReflexionGraph.MapsTo(c) != null).ToList();
        }

        /// <summary>
        /// Returns if there a candidates left, which are unmapped.
        /// </summary>
        /// <returns>Returns wether there are unmapped candidates.</returns>
        public bool UnmappedCandidatesLeft()
        {
            return GetUnmappedCandidates().Count > 0;
        }

        /// <summary>
        /// Returns if a given node is considered to be a cluster.
        /// </summary>
        /// <param name="node">given node</param>
        /// <returns>if node is considered to be a cluster</returns>
        public bool IsCluster(Node node)
        {
            return node.Type.Equals(this.AttractFunction.ClusterType) && node.IsInArchitecture();
        }

        /// <summary>
        /// Returns if a given node is considered to be a candidate regarding a given candidate type.
        /// </summary>
        /// <param name="node">given node</param>
        /// <param name="candidateType">given candidate type</param>
        /// <returns>if node is considered to be a candidate</returns>
        /// <returns>true if the node is considered to be a candidate</returns>
        public static bool IsCandidate(Node node, string candidateType)
        {
            return node.Type.Equals(candidateType)
                    && node.IsInImplementation()
                    && !node.ToggleAttributes.Contains("Element.Is_Artificial")
                    && !node.ToggleAttributes.Contains("Element.Is_Anonymous");
        }

        /// <summary>
        /// Returns if a given node is considered to be a candidate.
        /// </summary>
        /// <param name="node">given node</param>
        /// <returns>true if the node is considered to be a candidate</returns>
        public bool IsCandidate(Node node)
        {
            return IsCandidate(node, this.AttractFunction.CandidateType);
        }
    }
}