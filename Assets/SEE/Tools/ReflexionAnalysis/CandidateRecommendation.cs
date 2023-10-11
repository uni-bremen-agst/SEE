using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Newtonsoft.Json;
using SEE.DataModel;
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
        private ReflexionGraph reflexionGraph;

        private static double AttractionValueDelta = 0.001;

        // Dictionary representing the the mapping of nodes and their clusters regarding the highest 
        // attraction value
        private Dictionary<Node, HashSet<MappingPair>> recommendations;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Node, HashSet<MappingPair>> Recommendations { get => recommendations; set => recommendations = value; }

        public List<MappingPair> MappingPairs { get; private set; }

        public string TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// Object representing the attractFunction
        /// </summary>
        private AttractFunction attractFunction;

        public AttractFunction AttractFunction { get => attractFunction; }

        public bool UseCDA
        {
            get;
            set;
        }

        private AttractFunctionType attractFunctionType;

        public AttractFunctionType AttractFunctionType
        {
            get
            {
                return attractFunctionType;
            }
            set
            {
                attractFunctionType = value;
                if (reflexionGraph != null)
                {
                    Debug.Log("created attract function");
                    // TODO: How to update the attractfuction. How to compensate the missing onNext callbacks if attractFunction changes.
                    attractFunction = AttractFunction.Create(attractFunctionType, reflexionGraph, TargetType);
                }
            }
        }

        public CandidateRecommendation()
        {
            recommendations = new Dictionary<Node, HashSet<MappingPair>>();
            MappingPairs = new List<MappingPair>();
        }

        public ReflexionGraph ReflexionGraph
        {
            get => reflexionGraph;
            set
            {
                reflexionGraph = value;

                // TODO: Can a ReflexionGraph change after loading? How to update the attractfuction.           
                attractFunction = AttractFunction.Create(attractFunctionType, value, TargetType);
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
            if (value is EdgeEvent edgeEvent && edgeEvent.Affected == ReflexionSubgraph.Mapping)
            {
                Debug.Log(edgeEvent.ToString());
                Debug.Log("Handle Change in Mapping...");
                AttractFunction.MappingChanged(edgeEvent);
                UpdateRecommendations();
            }
        }

        private void UpdateRecommendations()
        {
            List<Node> targetedNodes = reflexionGraph.Nodes().Where(n => n.Type.Equals(TargetType) && n.IsInImplementation()).ToList();
            List<Node> clusters = reflexionGraph.Nodes().Where(n => n.Type.Equals("Cluster") && n.IsInArchitecture()).ToList();

            double maxAttractionValue = double.MinValue;

            recommendations.Clear();
            MappingPairs.Clear();
            Debug.Log($"Calculate attraction values... targetedNodes.Count={targetedNodes.Count} clusters.Count={clusters.Count}");

            foreach (Node cluster in clusters)
            {
                foreach (Node candidate in targetedNodes)
                {
                    // Skip already mapped nodes
                    if (reflexionGraph.MapsTo(candidate) != null) continue;
                    
                    // Calculate the attraction value for current node and current cluster
                    double attractionValue = AttractFunction.GetAttractionValue(candidate, cluster);

                    // Keep track of all attractions for statistical purposes
                    MappingPair mappingPair = new MappingPair(candidate: candidate, cluster: cluster, attractionValue: attractionValue);
                    MappingPairs.Add(mappingPair);

                    // Only do a recommendation if attraction is above 0
                    if (attractionValue <= 0) continue;

                    if (maxAttractionValue < attractionValue)
                    {
                        recommendations.Clear();
                        recommendations.Add(cluster, new HashSet<MappingPair>() { mappingPair });
                        maxAttractionValue = attractionValue;
                    }
                    else if (Math.Abs(maxAttractionValue - attractionValue) < AttractionValueDelta)
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
                    && Math.Abs(this.AttractionValue - mappingPair.AttractionValue) < AttractionValueDelta;
            }

            public override int GetHashCode()
            {
                // truncate value depending on the defined delta to erase decimal places
                double truncatedValue = Math.Truncate(AttractionValue / AttractionValueDelta) * AttractionValueDelta;
                return HashCode.Combine(this.Cluster.ID, this.Candidate.ID, truncatedValue);
            }
        }
    }
}
