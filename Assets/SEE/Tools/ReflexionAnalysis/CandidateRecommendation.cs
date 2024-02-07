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
    public class CandidateRecommendation : IObserver<ChangeEvent>
    {
        /// <summary>
        /// 
        /// </summary>
        public static double ATTRACTION_VALUE_DELTA = 0.001;

        /// <summary>
        /// 
        /// </summary>
        public ReflexionGraph ReflexionGraph { get; private set; }

        public ReflexionGraph OracleGraph { get; private set; }

        /// <summary>
        /// Object representing the attractFunction
        /// </summary>
        private AttractFunction attractFunction;

        /// <summary>
        /// 
        /// </summary>
        private string recommendationEdgeType = "Recommended With";

        /// <summary>
        /// Dictionary representing the the mapping of nodes and their clusters regarding the highest 
        /// attraction value
        /// </summary>
        private Dictionary<Node, HashSet<MappingPair>> recommendations;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Node, HashSet<MappingPair>> Recommendations { get => recommendations; set => recommendations = value; }

        /// <summary>
        /// 
        /// </summary>
        public List<MappingPair> MappingPairs { get { return mappingPairs.Values.ToList(); } }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, MappingPair> mappingPairs; 

        /// <summary>
        /// 
        /// </summary>
        public AttractFunction AttractFunction { get => attractFunction; }

        private IDisposable subscription;

        public CandidateRecommendationStatistics Statistics { get; private set; }

        public CandidateRecommendation()
        {
            recommendations = new Dictionary<Node, HashSet<MappingPair>>();
            mappingPairs = new Dictionary<string, MappingPair>();
            Statistics = new CandidateRecommendationStatistics();
        }

        // TODO: this interface needs work
        public Graph GetRecommendationTree(Node examinedNode)
        {
            List<Node> relatedNodes = examinedNode.IsInArchitecture() ? this.GetCandidates() : this.GetCluster();

            Graph graph = new Graph("", "Recommendations");

            Node examinedNodeClone = (Node)examinedNode.Clone();
            graph.AddNode(examinedNodeClone);
            graph.AddSingleRoot(out _);

            HashSet<string> visisited = new HashSet<string>();
            List<MappingPair> currentMappingPairs = new List<MappingPair>();

            if (MappingPairs.Count == 0) return graph;

            foreach (Node relatedNode in relatedNodes)
            {
                // skip mapped implementation nodes
                if (relatedNode.IsInImplementation() && this.ReflexionGraph.MapsTo(relatedNode) != null) continue;
                string key = examinedNode.IsInArchitecture() ? relatedNode.ID + examinedNode.ID : examinedNode.ID + relatedNode.ID;
                currentMappingPairs.Add(mappingPairs[key]);
            }

            currentMappingPairs.Sort((x,y) => y.CompareTo(x));

            foreach (MappingPair mappingPair in currentMappingPairs)
            {
                Node relatedNode = examinedNode.IsInArchitecture() ? mappingPair.Candidate : mappingPair.Cluster;
                visisited.Add(relatedNode.ID);
                Node relatedNodeClone = (Node)relatedNode.Clone();

                relatedNodeClone.ItsGraph = null;
                relatedNodeClone.ID = $"{relatedNode.ID}";
                Edge edge = new Edge(relatedNodeClone, 
                                    examinedNodeClone, 
                                    $"{recommendationEdgeType} {Math.Round(mappingPair.AttractionValue, 4)}");
                graph.AddNode(relatedNodeClone);
                examinedNode.AddChild(relatedNodeClone);
                graph.AddEdge(edge);
            }

            return graph;
        }

        public Graph GetRecommendationTree()
        {
            Graph graph = new Graph("", "Recommendations");

            foreach (Node cluster in recommendations.Keys)
            {
                Node clusterClone = (Node)cluster.Clone();
                graph.AddNode(clusterClone);
                foreach (MappingPair mappingPair in recommendations[cluster])
                {
                    Node candidate = mappingPair.Candidate;
                    Node candidateClone = (Node) candidate.Clone();
                    Edge edge = new Edge(candidateClone, clusterClone, recommendationEdgeType);
                    clusterClone.AddChild(candidateClone);
                    graph.AddNode(candidateClone);
                    graph.AddEdge(edge);
                }
            }
 
            return graph;
        }

        public void UpdateConfiguration(ReflexionGraph reflexionGraph, 
                                        MappingExperimentConfig config,
                                        Graph oracleMapping = null)
        {
            try
            {
                if (reflexionGraph == null)
                {
                    throw new Exception("Could not update configuration. Reflexion graph is null.");
                }

                ReflexionGraph = reflexionGraph;

                if (config.AttractFunctionConfig == null)
                {
                    throw new Exception("Could not update configuration. Attract function config is null");
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

                attractFunction = AttractFunction.Create(config.AttractFunctionConfig, reflexionGraph);

                subscription?.Dispose();
                subscription = reflexionGraph.Subscribe(this);

                // Stop and reset the recording
                bool wasActive = Statistics.Active;
                Statistics.Reset();
                Statistics.SetCandidateRecommendation(this);
                recommendations.Clear();
                mappingPairs.Clear();
                ReflexionGraph.RunAnalysis();


                // Restart after the analysis was run, so initially/already
                // mapped candidates will not recorded twice
                // TODO: Does the CsvFile really have to be public?
                if (wasActive) Statistics.StartRecording(Statistics.CsvFile);
            }
            catch (Exception e) 
            {
                UnityEngine.Debug.LogError($"Could not update Candidate Recommendation configuration.{Environment.NewLine}{e}");
                throw e;
            }
        }

        public void OnCompleted()
        {
            Debug.Log("OnCompleted() from recommendation.");
        }

        public void OnError(Exception error)
        {
            Debug.Log("OnError() from recommendation.");
        }

        public void OnNext(ChangeEvent value)
        {
            if (value is EdgeEvent edgeEvent && edgeEvent.Affected == ReflexionSubgraphs.Mapping)
            {
                // Debug.Log($"In Recommendations: Handle Change in Mapping... {edgeEvent.ToString()} sender: {edgeEvent.Sender}");

                // TODO: is this safe?
                if (edgeEvent.Change == null) return;

                // Get targeted childs of currently mapped node
                List<Node> nodesChangedInMapping = new List<Node>();
                edgeEvent.Edge.Source.GetTargetedChilds(nodesChangedInMapping, 
                                   node => node.Type.Equals(attractFunction.CandidateType) && node.IsInImplementation());

                if (Statistics.Active)
                {
                    // Update and calculate attraction values for each mapped node
                    // to make sure the statistic is consistent
                    foreach (Node nodeChangedInMapping in nodesChangedInMapping)
                    {
                        MappingPair chosenMappingPair;
                        if (!mappingPairs.TryGetValue(nodeChangedInMapping.ID + edgeEvent.Edge.Target.ID, out chosenMappingPair))
                        {
                            // For the very first mapped node and nodes removed form the mapping
                            // there is no previously calculated mappingpair available.
                            // So we create a corresponding mapping pair manually
                            chosenMappingPair = new MappingPair(nodeChangedInMapping, edgeEvent.Edge.Target, -1.0d);
                        }

                        AttractFunction.HandleChangedNodes(edgeEvent.Edge.Target, new List<Node> { nodeChangedInMapping }, (ChangeType)edgeEvent.Change);
                        UpdateRecommendations();
                        chosenMappingPair.ChangeType = (ChangeType)edgeEvent.Change;
                        Statistics.RecordChosenMappingPair(chosenMappingPair);
                    }
                } 
                else
                {
                    AttractFunction.HandleChangedNodes(edgeEvent.Edge.Target, nodesChangedInMapping, (ChangeType)edgeEvent.Change);
                    UpdateRecommendations();
                }
            }
        }

        private void UpdateRecommendations()
        {
            List<Node> unmappedCandidates = GetUnmappedCandidates();
            List<Node> clusters = GetCluster();

            double maxAttractionValue = double.MinValue;

            recommendations.Clear();
            mappingPairs.Clear();
            // Debug.Log($"Calculate attraction values... candidates.Count={unmappedCandidates.Count} clusters.Count={clusters.Count}");

            foreach (Node cluster in clusters)
            {
                foreach (Node candidate in unmappedCandidates)
                {
                    // Calculate the attraction value for current node and current cluster
                    double attractionValue = AttractFunction.GetAttractionValue(candidate, cluster);
                    
                    // Debug.Log($"Candidate {candidate.ID} attracted to cluster {cluster.ID} with attraction value {attractionValue}");

                    // Keep track of all attractions for statistical purposes
                    MappingPair mappingPair = new MappingPair(candidate: candidate, cluster: cluster, attractionValue: attractionValue);
                    mappingPairs.Add(candidate.ID + cluster.ID, mappingPair);

                    // Only do a recommendation if attraction is above 0
                    if (attractionValue <= 0) continue;

                    if (maxAttractionValue < attractionValue)
                    {
                        recommendations.Clear();
                        recommendations.Add(cluster, new HashSet<MappingPair>() { mappingPair });
                        maxAttractionValue = attractionValue;
                    }
                    else if (Math.Abs(maxAttractionValue - attractionValue) < ATTRACTION_VALUE_DELTA)
                    {
                        HashSet<MappingPair> nodes;
                        if (recommendations.TryGetValue(cluster, out nodes))
                        {
                            nodes.Add(mappingPair);
                        }
                        else
                        {
                            recommendations.Add(cluster, new HashSet<MappingPair>() { mappingPair });
                        }
                    }
                }
            }

            if (Statistics?.Active ?? false)
            {
                Statistics.RecordMappingPairs(MappingPairs);
            }
        }

        private static Dictionary<Node, HashSet<Node>> CreateInitialMapping(double percentage,
                                                                            int seed,
                                                                            string candidateType,
                                                                            ReflexionGraph reflexionGraph,
                                                                            ReflexionGraph oracleGraph)
        {
            Dictionary<Node, HashSet<Node>> initialMapping = new Dictionary<Node, HashSet<Node>>();
            if (percentage > 1 || percentage < 0) throw new Exception("Parameter percentage have to be a double value between 0.0 and 1.0");
            if (oracleGraph == null) throw new Exception("OracleGraph is null. Cannot generate initial mapping.");
            if (reflexionGraph == null) throw new Exception("ReflexionGraph is null. Cannot generate initial mapping.");

            List<Node> candidates = GetCandidates(reflexionGraph, candidateType);

            Random rand = new Random(seed);

            int implementationNodesCount = candidates.Count;
            HashSet<int> usedIndices = new HashSet<int>();
            double alreadyMappedNodesCount = 0;
            double artificallyMappedNodes = 0;
            double currentPercentage = 0;
            for (int i = 0; i < implementationNodesCount && currentPercentage < percentage;)
            {
                // manage next random index
                int randomIndex = rand.Next(implementationNodesCount);
                if (usedIndices.Contains(randomIndex)) continue;
                Node node = candidates[randomIndex];
                usedIndices.Add(randomIndex);

                // check if the current node is already mapped
                Node mapsTo = reflexionGraph.MapsTo(node);
                if (mapsTo == null)
                {
                    Node oracleMapsTo = oracleGraph.MapsTo(node);
                    mapsTo = reflexionGraph.GetNode(oracleMapsTo.ID);
                    if (mapsTo != null)
                    {
                        AddToInitialMapping(mapsTo, node);
                        artificallyMappedNodes++;
                    }
                }
                else
                {
                    alreadyMappedNodesCount++;
                }

                currentPercentage = (artificallyMappedNodes + alreadyMappedNodesCount) / implementationNodesCount;
                i++;
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

        public Dictionary<Node, HashSet<Node>> CreateInitialMapping(double percentage,
                                                                    int seed,
                                                                    ReflexionGraph graph)
        {
            return CreateInitialMapping(percentage, seed, AttractFunction.CandidateType, graph, OracleGraph);
        }

        public static bool IsHit(string candidateID, string clusterID, ReflexionGraph oracleGraph)
        {
            HashSet<string> candidateAscendants = oracleGraph.GetNode(candidateID).Ascendants().Select(n => n.ID).ToHashSet();
            HashSet<string> clusterAscendants = oracleGraph.GetNode(clusterID).Ascendants().Select(n => n.ID).ToHashSet();
            return oracleGraph.Edges().Any(e => e.IsInMapping()
                                                && candidateAscendants.Contains(e.Source.ID)
                                                && clusterAscendants.Contains(e.Target.ID));
        }

        public bool IsHit(string candidateID, string clusterID)
        {
            if (OracleGraph == null)
            {
                throw new Exception("Cannot determine if node was correctly mapped. No Oracle graph loaded.");
            }

            return IsHit(candidateID, clusterID, OracleGraph);
        }

        public static string GetExpectedClusterID(ReflexionGraph oracleGraph, string candidateID)
        {
            return oracleGraph.MapsTo(oracleGraph.GetNode(candidateID))?.ID;
        }

        public  string GetExpectedClusterID(string candidateID)
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

            if (oracleEdges.Count > 1) throw new Exception("Oracle Mapping is Ambigous.");
            if (oracleEdges.Count == 0)
            {
                // UnityEngine.Debug.LogWarning($"Oracle Mapping is Incomplete. There is no information about the node {candidateID}");
                throw new Exception($"Oracle Mapping is Incomplete. There is no information about the node {candidateID}");
            }

            return oracleEdges[0];
        }

        public double CalculatePercentileRank(string candidateID,
                                              List<MappingPair> mappingPairs)
        {
            // get corresponding oracle edge to determine all allowed clusters for the candidate
            Edge oracleEdge = this.GetOracleEdge(candidateID);
            return CalculatePercentileRank(candidateID, mappingPairs, oracleEdge);
        }

        /// <summary>
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


        public static bool IsRecommendationDefinite(Dictionary<Node, HashSet<MappingPair>> recommendations)
        {
            Node cluster = recommendations.Keys.First<Node>();
            HashSet<MappingPair> candidates = recommendations[cluster];
            return recommendations.Keys.Count == 1 && candidates.Count == 1;
        }

        public static MappingPair GetDefiniteRecommendation(Dictionary<Node, HashSet<MappingPair>> recommendations)
        {
            if(IsRecommendationDefinite(recommendations))
            {
                Node cluster = recommendations.Keys.First<Node>();
                return recommendations[cluster].FirstOrDefault<MappingPair>();
            } 
            else
            {
                return null;
            }
        }

        public static List<Node> GetCandidates(ReflexionGraph graph, string candidateType)
        {
            return graph.Nodes().Where(n => n.Type.Equals(candidateType) && n.IsInImplementation()).ToList();
        }

        public List<Node> GetCandidates()
        {
            return GetCandidates(ReflexionGraph, attractFunction.CandidateType);
        }

        public static List<Node> GetCluster(ReflexionGraph graph, string clusterType)
        {
            return graph.Nodes().Where(n => n.Type.Equals(clusterType) && n.IsInArchitecture()).ToList();
        }

        public List<Node> GetCluster()
        {
            return GetCluster(ReflexionGraph, attractFunction.ClusterType);
        }

        public static List<Node> GetUnmappedCandidates(ReflexionGraph graph, string candidateType)
        {
            return GetCandidates(graph, candidateType).Where(c => graph.MapsTo(c) == null).ToList();
        }

        public List<Node> GetUnmappedCandidates()
        {
            return GetUnmappedCandidates(ReflexionGraph, attractFunction.CandidateType);
        }

        public static List<Node> GetMappedCandidates(ReflexionGraph graph, string candidateType)
        {
            return GetCandidates(graph, candidateType).Where(c => graph.MapsTo(c) != null).ToList();
        }

        public List<Node> GetMappedCandidates()
        {
            return GetMappedCandidates(ReflexionGraph, attractFunction.CandidateType);
        }
    }
}
