using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        RecommendedNodes recommendedNodes = new RecommendedNodes();

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Node, HashSet<MappingPair>> Recommendations { get
            {
                // TODO: ADJUST INTERFACE!
                Dictionary<Node, HashSet<MappingPair>> recommendations = new();
                foreach (MappingPair mappingPair in recommendedNodes.Recommendations.Values)
                {
                    Node cluster = ReflexionGraph.GetNode(mappingPair.ClusterID);
                    HashSet<MappingPair> mappingPairs;
                    if (!recommendations.TryGetValue(cluster, out mappingPairs))
                    {
                        recommendations[cluster] = new HashSet<MappingPair>();
                        mappingPairs = recommendations[cluster];
                    }
                    mappingPairs.Add(mappingPair);
                }
                
                return recommendations; 
            } 
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<MappingPair> MappingPairs { get { return recommendedNodes.MappingPairs; } }

        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> UnmappedCandidates { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public AttractFunction AttractFunction { get => attractFunction; }

        /// <summary>
        /// 
        /// </summary>
        private IDisposable subscription;

        public CandidateRecommendationStatistics Statistics { get; private set; }

        public CandidateRecommendation()
        {
            Statistics = new CandidateRecommendationStatistics();
        }

        // TODO: this interface needs work(Make it async)
        /// <summary>
        /// 
        /// </summary>
        /// <param name="examinedNode"></param>
        /// <returns></returns>
        public Graph GetRecommendationTree(Node examinedNode)
        {
            List<Node> relatedNodes = examinedNode.IsInArchitecture() ? this.GetCandidates() : this.GetCluster();

            Graph graph = new Graph("", "Recommendations");

            Node examinedNodeClone = (Node)examinedNode.Clone();
            graph.AddNode(examinedNodeClone);
            graph.AddSingleRoot(out _);

            HashSet<string> visisited = new HashSet<string>();
            List<MappingPair> currentMappingPairs = new List<MappingPair>();

            if (MappingPairs.Count() == 0) return graph;

            foreach (Node relatedNode in relatedNodes)
            {
                // skip mapped implementation nodes
                if (relatedNode.IsInImplementation() && this.ReflexionGraph.MapsTo(relatedNode) != null) continue;
                MappingPair mappingPair = examinedNode.IsInArchitecture() ? 
                            recommendedNodes.GetMappingPair(relatedNode.ID,examinedNode.ID)
                          : recommendedNodes.GetMappingPair(examinedNode.ID, relatedNode.ID);
                
                currentMappingPairs.Add(mappingPair);
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Graph GetRecommendationTree()
        {
            Graph graph = new Graph("", "Recommendations");

            foreach (Node cluster in Recommendations.Keys)
            {
                Node clusterClone = (Node)cluster.Clone();
                graph.AddNode(clusterClone);
                foreach (MappingPair mappingPair in Recommendations[cluster])
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reflexionGraph"></param>
        /// <param name="config"></param>
        /// <param name="oracleMapping"></param>
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

                attractFunction = AttractFunction.Create(config.AttractFunctionConfig, this, reflexionGraph);

                subscription?.Dispose();
                subscription = reflexionGraph.Subscribe(this);

                // Stop and reset the recording
                bool wasActive = Statistics.Active;
                Statistics.Reset();
                Statistics.SetCandidateRecommendation(this);
                Statistics.SetConfigInformation(config);
                recommendedNodes.Reset();
                this.UnmappedCandidates = this.GetUnmappedCandidates().Select(n => n.ID).ToHashSet();

                foreach(Node cluster in this.GetCluster())
                {
                    this.attractFunction.AddClusterToUpdate(cluster.ID);
                }

                ReflexionGraph.RunAnalysis();

                // Restart after the analysis was run, so initially/already
                // mapped candidates will not recorded twice
                if (wasActive)
                {
                    Statistics.StartRecording();
                }
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
            if(value is MapsToChange mapsToEvent)
            {
                // Debug.Log($"mapsToEvent.Source={mapsToEvent.Source.ID} mapsToEvent.Target={mapsToEvent.Target.ID} change={mapsToEvent.Change} isSourceCandidate={IsCandidate(mapsToEvent.Source, this.AttractFunction.CandidateType)}");
                if (!IsCandidate(mapsToEvent.Source, this.AttractFunction.CandidateType))
                {
                    return;
                }

                UpdateCandidateSet(mapsToEvent.Source.ID, mapsToEvent.Change);

                if (Statistics.Active)
                {
                    // Update and calculate attraction values for each mapped node
                    // to make sure the statistic is consistent
                    MappingPair chosenMappingPair = recommendedNodes.GetMappingPair(mapsToEvent.Source.ID, mapsToEvent.Target.ID);

                    if (chosenMappingPair == null)
                    {
                        // For the very first mapped node, nodes removed from the mapping, and already mapped childs
                        // there is no previously calculated mappingpair available.
                        // So we create a corresponding mapping pair manually

                        //Debug.Log($"Could not find mappingPair for candidate={candidateChangedInMapping.ID} and cluster {edgeEvent.Edge.Target.ID}" +
                        //    $"in Recommendations. Create one MappingPair manually.");

                        // TODO: move this into recommendation class.
                        chosenMappingPair = new MappingPair(mapsToEvent.Source, mapsToEvent.Target, -1.0d);
                    }

                    recommendedNodes.RemoveCandidate(mapsToEvent.Source.ID);
                    AttractFunction.HandleChangedCandidate(mapsToEvent.Target, mapsToEvent.Source, (ChangeType)mapsToEvent.Change);
                    UpdateRecommendations();
                    chosenMappingPair.ChangeType = (ChangeType)mapsToEvent.Change;
                    Debug.Log($"Record chosen mapping Pair:{chosenMappingPair.CandidateID} -'{chosenMappingPair.AttractionValue}'-> {chosenMappingPair.ClusterID}");
                    Statistics.RecordChosenMappingPair(chosenMappingPair);
                }
                else
                {
                    recommendedNodes.RemoveCandidate(mapsToEvent.Source.ID);
                    AttractFunction.HandleChangedCandidate(mapsToEvent.Target, mapsToEvent.Source, (ChangeType)mapsToEvent.Change);
                }
            }

            if (value is EdgeEvent edgeEvent)
            {
                if(edgeEvent.Affected == ReflexionSubgraphs.Architecture)
                {
                    // TODO: Reset edgeStateCache here with correct criteria
                    // AttractFunction.ClearStateCache();
                    // UnityEngine.Debug.Log("TODO: Architecture might have changed. Edge State cache in attract function needs to be invalidated.");
                    return;
                }
            }
        }

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
            else
            {
                throw new Exception("Unkown Changetype in ChangeEvent. Can not process ChangeEvent when calculating recommendations.");
            }
        }

        private void GetImplicitlyMappedCandidates(List<Node> implicitlyMappedChilds, Node node, ReflexionGraph graph)
        {
            List<Node> childs = new List<Node>();

            if (this.IsCandidate(node))
            {
                implicitlyMappedChilds.Add(node);
            }

            foreach (Node child in node.Children())
            {
                if(!graph.IsExplicitlyMapped(child))
                {
                    GetImplicitlyMappedCandidates(implicitlyMappedChilds, child, graph);
                }
            }
            return;
        }

        public void UpdateRecommendations()
        {
            foreach (string clusterId in this.AttractFunction.ClusterToUpdate)
            {
                Node cluster = ReflexionGraph.GetNode(clusterId);

                if (cluster == null)
                {
                    throw new Exception($"No node found for the cluster id {clusterId}.");
                }

                foreach (string candidateId in this.UnmappedCandidates)
                {
                    Node candidate = this.ReflexionGraph.GetNode(candidateId);

                    // A check if an 'unmapped' candidate might have been already mapped is still required 
                    // because its parent might was mapped, but the corresponding event was not received yet 
                    if (ReflexionGraph.MapsTo(candidate) != null)
                    {
                        recommendedNodes.RemoveCandidate(candidateId);
                    } 
                    else
                    {
                        double attractionValue = AttractFunction.GetAttractionValue(candidate, cluster);
                        // Debug.Log($"Candidate {candidate.ID} attracted to cluster {cluster.ID} with attraction value {attractionValue}");
                        MappingPair mappingPair = new MappingPair(candidate: candidate, cluster: cluster, attractionValue: attractionValue);
                        recommendedNodes.UpdateMappingPair(mappingPair);
                    }
                }
                this.AttractFunction.RemoveClusterToUpdate(clusterId);
            }

            if (Statistics?.Active ?? false)
            {
                // Keep track of all attractions for statistical purposes
                Statistics.RecordMappingPairs(MappingPairs);
            }
        }

        private /*static*/ Dictionary<Node, HashSet<Node>> CreateInitialMapping(double percentage,
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

            UnityEngine.Debug.Log($"Generate initial mapping with seed {seed} for {candidates.Count}");
            System.Random rand = new System.Random(seed);

            int candidatesCount = candidates.Count;
            HashSet<int> usedIndices = new HashSet<int>();
            double alreadyMappedNodesCount = 0;
            double artificallyMappedNodes = 0;
            double currentPercentage = 0;
            for (int i = 0; i < candidatesCount && currentPercentage < percentage;)
            {
                // manage next random index
                int randomIndex = rand.Next(candidatesCount);
                if (usedIndices.Contains(randomIndex)) continue;
                Node node = candidates[randomIndex];
                usedIndices.Add(randomIndex);

                // check if the current node is already mapped
                Node mapsTo = reflexionGraph.MapsTo(node);
                if (mapsTo == null)
                {
                    Node oracleMapsTo = oracleGraph.MapsTo(node);

                    if (oracleMapsTo == null)
                    {                                       
                        Debug.LogWarning($"There is no information where to map node {node.ID} " +
                                         $"within the oracle graph {Environment.NewLine}{node}");
                        i++;
                        continue;
                    }

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

                currentPercentage = (artificallyMappedNodes + alreadyMappedNodesCount) / candidatesCount;
                // UnityEngine.Debug.Log($"Node={node.ID} currentPercentage={currentPercentage} artificallyMappedNodes={artificallyMappedNodes} alreadyMappedNodesCount={alreadyMappedNodesCount} candidatesCount={candidatesCount}");
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

        public Dictionary<Node, HashSet<Node>> CreateInitialMapping(double percentage,
                                                            int seed)
        {
            return CreateInitialMapping(percentage, seed, AttractFunction.CandidateType, ReflexionGraph, OracleGraph);
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

        public string GetExpectedClusterID(string candidateID)
        {
            if (OracleGraph == null)
            {
                throw new Exception($"Cannot determine expected cluster for node ID {candidateID}. No Oracle graph loaded.");
            }

            return GetExpectedClusterID(OracleGraph, candidateID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static ReflexionGraph GenerateOracleMapping(Graph implementation, string oracleInstructions)
        {
            string currentMode = string.Empty;

            Graph architecture = new Graph(implementation.BasePath, "Architecture");
            Graph oracleMapping = new Graph(implementation.BasePath, "OracleMapping");

            // Open the file for reading using StreamReader
            using (StreamReader sr = new StreamReader(oracleInstructions))
            {
                string line;
                // Read and display lines from the file until the end of the file is reached
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Replace(" ", "");
                    
                    if(line.IsNullOrWhitespace())
                    {
                        break;
                    }
                    
                    if(line.Contains(":"))
                    {
                        currentMode = line;
                        continue;
                    }

                    switch(currentMode)
                    {
                        case "cluster:":
                            AddCluster(architecture, line);
                        break;
                        case "relations:":
                            AddClusterRelation(architecture, line);
                        break;
                        case "mapping:":
                            AddOracleRelation(implementation, architecture, oracleMapping, line);
                        break;
                        default:
                            throw new Exception($"Unknown instruction mode when processing oracle instructions: {currentMode}");

                    }
                }
            }
            
            ReflexionGraph oracleGraph = new ReflexionGraph(implementation: implementation,
                                          architecture: architecture,
                                          mapping: oracleMapping);

            return oracleGraph; 

            void AddCluster(Graph arch, string line) 
            {
                UnityEngine.Debug.Log($"line='{line}'");
                Node cluster = new Node();
                cluster.ID = line;
                cluster.Type = "Cluster";
                arch.AddNode(cluster);
            }

            void AddClusterRelation(Graph arch, string line)
            {
                string[] nodes = line.Split(',');
                Node source = arch.GetNode(nodes[0]);
                Node target = arch.GetNode(nodes[1]);
                Edge edge = new Edge(source, target, "Source_Dependency");
                arch.AddEdge(edge);
            }

            void AddOracleRelation(Graph impl, Graph arch, Graph oracle, string line) 
            {
                string[] nodes = line.Split(',');

                Node implNode = impl.GetNode(nodes[0]);
                Node archNode = arch.GetNode(nodes[1]);

                Node source = new Node();
                source.ID = implNode.ID;
                Node target = new Node();
                target.ID = archNode.ID;

                oracle.AddNode(source);
                oracle.AddNode(target);
                Edge edge = new Edge(source, target, "Maps_To");
                oracle.AddEdge(edge);
            }
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

        public /*static*/ List<Node> GetCandidates(ReflexionGraph graph, string candidateType)
        {
            return graph.Nodes().Where(n => this.IsCandidate(n)).ToList();
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

        public /*static*/ List<Node> GetUnmappedCandidates(ReflexionGraph graph, string candidateType)
        {
            return GetCandidates(graph, candidateType).Where(c => graph.MapsTo(c) == null).ToList();
        }

        public List<Node> GetUnmappedCandidates()
        {
            return GetUnmappedCandidates(ReflexionGraph, attractFunction.CandidateType);
        }

        public /*static*/ List<Node> GetMappedCandidates(ReflexionGraph graph, string candidateType)
        {
            return GetCandidates(graph, candidateType).Where(c => graph.MapsTo(c) != null).ToList();
        }

        public List<Node> GetMappedCandidates()
        {
            return GetMappedCandidates(ReflexionGraph, attractFunction.CandidateType);
        }

        public static bool IsCandidate(Node node, string candidateType)
        {
            return node.Type.Equals(candidateType)
                    && node.IsInImplementation()
                    && !node.ToggleAttributes.Contains("Element.Is_Artificial")
                    && !node.ToggleAttributes.Contains("Element.Is_Anonymous");
        }

        public static bool IsUnmappedCandidate(Node node, string candidateType, ReflexionGraph oracleGraph, ReflexionGraph graph)
        {
            return IsCandidate(node, candidateType) && graph.MapsTo(node) == null;
        }

        public bool IsCandidate(Node node)
        {
            return IsCandidate(node, this.AttractFunction.CandidateType);
        }

        public bool IsUnmappedCandidate(Node node)
        {
            return IsCandidate(node) && this.ReflexionGraph.MapsTo(node) == null;
        }
    }
}
