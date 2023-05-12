using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;
using Debug = UnityEngine.Debug;
using Node = SEE.DataModel.DG.Node;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateRecommendation : MonoBehaviour, IObserver<ChangeEvent>
    {
        private ReflexionGraph reflexionGraph; 

        /// <summary>
        /// Object representing the attractFunction
        /// </summary>
        public AttractFunction attractFunction;

        [SerializeField]
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
                if(reflexionGraph != null)
                {
                    // TODO: How to update the attracfuction. How to compensate the missing onNext callbacks if attractFunction changes.
                    attractFunction = AttractFunction.Create(attractFunctionType, reflexionGraph, targetType);
                }
            }
        }

        public string targetType;

        public CandidateRecommendation()
        {
            targetType = "Class";
        }

        /// <summary>
        /// 
        /// /summary>
        public bool useCDA;

        public ReflexionGraph ReflexionGraph
        {
            get => reflexionGraph;
            set
            {
                reflexionGraph = value;
                // TODO: Can a ReflexionGraph change after loading? How to update the attractfuction.           
                attractFunction = AttractFunction.Create(attractFunctionType, value, targetType);
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
            Debug.Log($"OnNext() from recommendation. edgeEvent.Affected={value.Affected} value.GetType()={value.GetType()}");
            if (value is EdgeEvent edgeEvent && edgeEvent.Affected == ReflexionSubgraph.Mapping)
            {
                Debug.Log(edgeEvent.ToString());
                attractFunction.MappingChanged(edgeEvent);
           
                // TODO: Subgraph clones, use a technique which avoids cloning
                Graph targetedNodes = reflexionGraph.SubgraphByNodeType(new string[] { targetType });
                Graph clusters = reflexionGraph.SubgraphByNodeType(new string[] { "Cluster" });

                double maxAttractionValue = double.MinValue;

                // Dictionary representing the the mapping of nodes and their clusters regarding the highest 
                // attraction value
                Dictionary<Node,HashSet<Node>> mostAttractedNodes = new Dictionary<Node,HashSet<Node>>();    

                double delta = 0.01;

                foreach (Node cluster in clusters.Nodes())
                {
                    foreach (Node node in targetedNodes.Nodes())
                    {
                        if (reflexionGraph.MapsTo(node) != null) continue;
                        double attractionValue = attractFunction.GetAttractionValue(node, cluster);
                        if (attractionValue > maxAttractionValue)
                        {
                            mostAttractedNodes.Clear();
                            mostAttractedNodes.Add(cluster, new HashSet<Node>() { node });
                            maxAttractionValue = attractionValue;
                        }
                        else if (Math.Abs(maxAttractionValue - attractionValue) < delta)
                        {
                            HashSet<Node> nodes;
                            if (mostAttractedNodes.TryGetValue(cluster, out nodes))
                            {
                                nodes.Add(node);
                            }
                            else
                            {
                                mostAttractedNodes.Add(cluster, new HashSet<Node>() { node });
                            }
                        }
                    } 
                }

                foreach (Node cluster in mostAttractedNodes.Keys)
                {
                    NodeOperator nodeOperator;
                    List<NodeOperator> nodeOperators = new List<NodeOperator>();

                    nodeOperator = cluster.GameObject().AddOrGetComponent<NodeOperator>();
                    nodeOperators.Add(nodeOperator);

                    foreach (Node entity in mostAttractedNodes[cluster])
                    {
                        nodeOperators.Add(entity.GameObject().AddOrGetComponent<NodeOperator>());
                    }

                    nodeOperators.ForEach((n) => n.Blink(10, 2));
                }
            }
        }
    }
}
