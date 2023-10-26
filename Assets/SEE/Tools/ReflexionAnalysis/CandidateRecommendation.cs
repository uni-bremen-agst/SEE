using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Newtonsoft.Json;
using SEE.DataModel;
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

        private Dictionary<string, MappingPair> mappingPairs; 

        /// <summary>
        /// 
        /// </summary>
        public AttractFunction AttractFunction { get => attractFunction; }

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
            get
            {
                return attractFunctionType;
            }
            set
            {
                UpdateConfiguration(ReflexionGraph, value, CandidateType);
            }
        }

        public ReflexionGraph ReflexionGraph
        {
            get => reflexionGraph;
            set
            {
                UpdateConfiguration(value, AttractFunctionType, CandidateType);
            }
        }

        public string CandidateType
        {
            get => candidateType;
            set
            {
                UpdateConfiguration(ReflexionGraph, AttractFunctionType, value);
            }
        }

        public CandidateRecommendationStatistics Statistics { get; private set; }

        public CandidateRecommendation()
        {
            recommendations = new Dictionary<Node, HashSet<MappingPair>>();
            mappingPairs = new Dictionary<string, MappingPair>();
            Statistics = new CandidateRecommendationStatistics();
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
            bool wasActive = Statistics.Active;

            // Stop and reset the recording
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
                Debug.Log("Handle Change in Mapping... " + edgeEvent.ToString());

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

                        AttractFunction.HandleMappedEntities(edgeEvent.Edge.Target, new List<Node> { nodeChangedInMapping }, (ChangeType)edgeEvent.Change);
                        UpdateRecommendations();
                        Statistics.RecordChosenMappingPair(chosenMappingPair, (ChangeType)edgeEvent.Change);
                    }
                } 
                else
                {
                    AttractFunction.HandleMappedEntities(edgeEvent.Edge.Target, nodesChangedInMapping, (ChangeType)edgeEvent.Change);
                    UpdateRecommendations();
                }
            }
        }

        private void UpdateRecommendations()
        {
            List<Node> candidates = reflexionGraph.Nodes().Where(n => n.Type.Equals(CandidateType) && n.IsInImplementation()).ToList();
            List<Node> clusters = reflexionGraph.Nodes().Where(n => n.Type.Equals("Cluster") && n.IsInArchitecture()).ToList();

            double maxAttractionValue = double.MinValue;

            recommendations.Clear();
            mappingPairs.Clear();
            Debug.Log($"Calculate attraction values... candidates.Count={candidates.Count} clusters.Count={clusters.Count}");

            foreach (Node cluster in clusters)
            {
                foreach (Node candidate in candidates)
                {
                    // Skip already mapped nodes
                    if (reflexionGraph.MapsTo(candidate) != null) continue;
                    
                    // Calculate the attraction value for current node and current cluster
                    double attractionValue = AttractFunction.GetAttractionValue(candidate, cluster);

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

        public bool IsRecommendationDefinite()
        {
            Node cluster = Recommendations.Keys.First<Node>();
            HashSet<MappingPair> candidates = Recommendations[cluster];
            return Recommendations.Keys.Count == 1 && candidates.Count == 1;
        }

        public MappingPair GetDefiniteRecommendation()
        {
            if(IsRecommendationDefinite())
            {
                Node cluster = Recommendations.Keys.First<Node>();
                return Recommendations[cluster].FirstOrDefault<MappingPair>();
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
