using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Net.Dashboard.Model.Metric;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;
using Debug = UnityEngine.Debug;
using Node = SEE.DataModel.DG.Node;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateRecommendation : IObserver<ChangeEvent>
    {
        /// <summary>
        /// 
        /// </summary>
        private static double ATTRACTION_VALUE_DELTA = 0.001;
        
        /// <summary>
        /// 
        /// </summary>
        private ReflexionGraph reflexionGraph;

        /// <summary>
        /// Object representing the attractFunction
        /// </summary>
        private AttractFunction attractFunction;

        /// <summary>
        /// 
        /// </summary>
        private string candidateType;

        /// <summary>
        /// 
        /// </summary>
        private string clusterType = "Cluster";

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

        /// <summary>
        /// TODO: integrate into configuration
        /// </summary>
        public bool UseCDA
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private AttractFunctionType? attractFunctionType;

        /// <summary>
        /// 
        /// </summary>
        public AttractFunctionType? AttractFunctionType
        {
            get => attractFunctionType;
        }

        public ReflexionGraph ReflexionGraph
        {
            get => reflexionGraph;
        }

        public string CandidateType
        {
            get => candidateType;
        }

        public string ClusterType
        {
            get => clusterType;
        } 

        public CandidateRecommendationStatistics Statistics { get; private set; }

        public CandidateRecommendation()
        {
            recommendations = new Dictionary<Node, HashSet<MappingPair>>();
            mappingPairs = new Dictionary<string, MappingPair>();
            Statistics = new CandidateRecommendationStatistics();
        }

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
                                        AttractFunctionType? attractFunctionType, 
                                        string candidateType)
        {
            this.reflexionGraph = reflexionGraph;
            this.attractFunctionType = attractFunctionType;
            this.candidateType = candidateType;
            if (reflexionGraph == null || candidateType == null || attractFunctionType == null) return;
            attractFunction = AttractFunction.Create((AttractFunctionType)attractFunctionType, 
                                                      reflexionGraph, 
                                                      CandidateType);

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
            if(wasActive) Statistics.StartRecording();
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
                // Debug.Log("Handle Change in Mapping... " + edgeEvent.ToString());

                // TODO: is this safe?
                if (edgeEvent.Change == null) return;

                // Get targeted childs of currently mapped node
                List<Node> nodesChangedInMapping = new List<Node>();
                edgeEvent.Edge.Source.GetTargetedChilds(nodesChangedInMapping, 
                                   node => node.Type.Equals(candidateType) && node.IsInImplementation());

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
                        Statistics.RecordChosenMappingPair(chosenMappingPair, (ChangeType)edgeEvent.Change);
                    }
                } 
                else
                {
                    AttractFunction.HandleChangedNodes(edgeEvent.Edge.Target, nodesChangedInMapping, (ChangeType)edgeEvent.Change);
                    UpdateRecommendations();
                }
            }
        }

        private List<Node> GetCandidates()
        {
            return reflexionGraph.Nodes().Where(n => n.Type.Equals(CandidateType) && n.IsInImplementation()).ToList();
        }

        private List<Node> GetCluster()
        {
            return reflexionGraph.Nodes().Where(n => n.Type.Equals(ClusterType) && n.IsInArchitecture()).ToList();
        }

        private void UpdateRecommendations()
        {
            List<Node> unmappedCandidates = GetCandidates().Where(c => reflexionGraph.MapsTo(c) == null).ToList();
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

        public class MappingPair : IComparable<MappingPair>
        {
            private double? attractionValue;

            public double AttractionValue
            {   
                get
                {
                    return attractionValue ?? -1.0;
                }
                set
                {
                    if(attractionValue == null)
                    {
                        attractionValue = value;
                    } 
                    else
                    {
                        throw new Exception("Cannot override Attractionvalue.");
                    }
                } 
            }

            [JsonIgnore]
            public Node Candidate { get; }

            [JsonIgnore]
            public Node Cluster { get; }

            private string clusterID;

            private string candidateID;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ChangeType? ChangeType { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public DateTime? ChosenAt { get; set; }

            public string ClusterID 
            { 
                get 
                { 
                    return Cluster != null ? Cluster.ID : clusterID; 
                }
                set 
                {
                    if (Cluster != null || clusterID != null) throw new Exception("Cannot override ClusterID"); 
                    clusterID = value; 
                }
            }

            public string CandidateID 
            { 
                get 
                { 
                    return Candidate != null ? Candidate.ID : candidateID; 
                }
                set
                {
                    if (Candidate != null || candidateID != null) throw new Exception("Cannot override CandidateID");
                    candidateID = value;
                }
            }

            public MappingPair(Node candidate, Node cluster, double attractionValue)
            {
                this.Cluster = cluster;
                this.Candidate = candidate;
                this.attractionValue = attractionValue;
            }

            public int CompareTo(MappingPair other)
            {
                if (this == other) return 0;
                return this.AttractionValue.CompareTo(other.AttractionValue);
            }

            public override bool Equals(object obj)
            {
                if(obj == null || GetType() != obj.GetType())
                {
                    return false;
                } 

                MappingPair mappingPair = (MappingPair)obj;

                return this.Cluster.Equals(mappingPair.Cluster)
                    && this.Candidate.Equals(mappingPair.Candidate)
                    && Math.Abs(this.AttractionValue - mappingPair.AttractionValue) < ATTRACTION_VALUE_DELTA;
            }

            public override int GetHashCode()
            {
                // truncate value depending on the defined delta to erase decimal places
                double truncatedValue = Math.Truncate(AttractionValue / ATTRACTION_VALUE_DELTA) * ATTRACTION_VALUE_DELTA;
                return HashCode.Combine(this.Cluster.ID, this.Candidate.ID, truncatedValue);
            }
        }

    }
}
