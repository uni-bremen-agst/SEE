using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
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

        // Dictionary representing the the mapping of nodes and their clusters regarding the highest 
        // attraction value
        private Dictionary<Node, HashSet<Node>> recommendations;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Node, HashSet<Node>> Recommendations { get => recommendations; set => recommendations = value; }

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
            recommendations = new Dictionary<Node, HashSet<Node>>();
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

            double delta = 0.01;

            Debug.Log("Calculate attraction values...");

            foreach (Node cluster in clusters)
            {
                foreach (Node node in targetedNodes)
                {
                    // Skip already mapped nodes
                    if (reflexionGraph.MapsTo(node) != null) continue;

                    // Calculate the attraction value for current node and current cluster
                    double attractionValue = AttractFunction.GetAttractionValue(node, cluster);

                    // Only do a recommendation if attraction is above 0
                    if (attractionValue <= 0) continue;

                    if (maxAttractionValue < attractionValue)
                    {
                        recommendations.Clear();
                        recommendations.Add(cluster, new HashSet<Node>() { node });
                        maxAttractionValue = attractionValue;
                    }
                    else if (Math.Abs(maxAttractionValue - attractionValue) < delta)
                    {
                        HashSet<Node> nodes;
                        if (recommendations.TryGetValue(cluster, out nodes))
                        {
                            nodes.Add(node);
                        }
                        else
                        {
                            recommendations.Add(cluster, new HashSet<Node>() { node });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<Node, HashSet<Node>> CloneRecommendations()
        {
            Dictionary<Node, HashSet<Node>> clone  = new Dictionary<Node, HashSet<Node>>();
            foreach (Node cluster in Recommendations.Keys)
            {
                clone[cluster] = Recommendations[cluster];
            }
            return clone;
        }

        // Currently not used.
        public class Recommendation : IComparable<Recommendation>
        {
            private HashSet<Node> cluster;

            private Dictionary<Node, Node> recommendations;

            double attractionValue;

            public HashSet<Node> Node { get { return cluster; } }
            public double AttractionValue { get { return attractionValue; } }

            private Recommendation(Node node, double attractionValue)
            {
                this.cluster = new HashSet<Node>() { node };
                this.attractionValue = attractionValue;
            }

            private void Add(Node node)
            {

            }

            public int CompareTo(Recommendation other)
            {
                if (this == other) return 0;
                return this.AttractionValue.CompareTo(other.AttractionValue);
            }
        }
    }
}
