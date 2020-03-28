using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SEE.Layout
{
    public class OptAlgorithm: GraphRenderer
    {
        Dictionary<Node, GameObject> nodeMap;
        Graph graph;
        AbstractSEECity settings;
        NodeFactory leafNodeFactory;
        InnerNodeFactory innerNodeFactory;
        float groundLevel;
        Dictionary<GameObject, NodeTransform> bestLayout;
        Dictionary<string, string> optimalMeasurements;
        SortedDictionary<string, string> bestMeasurements;
        int maxIterations = 2500;
        int iteration = 0;
        double bestScore = Mathf.Infinity;
        List<SublayoutNode> sublayoutNodes;
        Dictionary<string, double> values = new Dictionary<string, double>();
        Dictionary<string, double> oldValues = new Dictionary<string, double>();
        Dictionary<List<Double>, Measurements> results = new Dictionary<List<double>, Measurements>();


        public OptAlgorithm(AbstractSEECity settings, float groundLevel, NodeFactory leafNodeFactory, InnerNodeFactory innerNodeFactory, Graph graph, List<SublayoutNode> sublayoutNodes, Dictionary<Node, GameObject> nodeMap): base (settings)
        {
            this.nodeMap = nodeMap;
            this.graph = graph;
            this.settings = settings;
            this.leafNodeFactory = leafNodeFactory;
            this.innerNodeFactory = innerNodeFactory;
            this.groundLevel = groundLevel;
            this.sublayoutNodes = sublayoutNodes;

            CreateOptimalSolutionMeasurements();
            /*bestLayout = new CoseLayout(groundLevel, leafNodeFactory, false, graph.Edges(), settings, sublayoutNodes).Layout(nodeMap.Values);
            Apply(bestLayout, settings.origin);
            EdgeLayout(graph, bestLayout.Keys);*/
        }

        public bool StartOptAlgorithm()
        {
            while(iteration < maxIterations)
            {
                EvaluateLayout(bestLayout);
                Debug.Log("best score: "+bestScore);

                if (bestScore == 0)
                {
                    return true;
                }

                iteration++;
                MutateSolution();
                /*bestLayout = new CoseLayout(groundLevel, leafNodeFactory, false, graph.Edges(), settings, sublayoutNodes).Layout(nodeMap.Values);
                Apply(bestLayout, settings.origin);
                EdgeLayout(graph, bestLayout.Keys);*/

                oldValues = SetValues(settings.CoseGraphSettings.EdgeLength, settings.CoseGraphSettings.GravityStrength, settings.CoseGraphSettings.CompoundGravityStrength, settings.CoseGraphSettings.RepulsionStrength);
                values = SetValues(settings.CoseGraphSettings.EdgeLength, settings.CoseGraphSettings.GravityStrength, settings.CoseGraphSettings.CompoundGravityStrength, settings.CoseGraphSettings.RepulsionStrength);

            }

            return false;
        }

        private void ResetSolution()
        {
            values = oldValues;
            settings.CoseGraphSettings.EdgeLength = (int) values["EdgeLength"];
            settings.CoseGraphSettings.GravityStrength = values["GravityStrength"];
            settings.CoseGraphSettings.CompoundGravityStrength = values["CompoundGravityStrength"];
            settings.CoseGraphSettings.RepulsionStrength = values["RepulsionStrength"];
        }

        private Dictionary<string, double> SetValues(int edgeLength, double gravityStrength, double compoundGravityStrength, double RepulsionStrength)
        {
            Dictionary<string, double> values = new Dictionary<string, double>();
            values.Add("EdgeLength", edgeLength);
            values.Add("GravityStrength", gravityStrength);
            values.Add("CompoundGravityStrength", compoundGravityStrength);
            values.Add("RepulsionStrength", RepulsionStrength);
            return values; 
        }

        private void MutateSolution()
        {
            int random = Random.Range(0, 4);
            int minusPlus = random % 2 == 0 ? 1 : -1;

            switch (random)
            {
                case 0:
                    if ((settings.CoseGraphSettings.EdgeLength + 1 * minusPlus) > 0)
                    {
                        settings.CoseGraphSettings.EdgeLength += 1 * minusPlus;
                    } else
                    {
                        settings.CoseGraphSettings.EdgeLength += 1;
                    }
                    
                    break;
                /*case 1:
                    settings.CoseGraphSettings.UseSmartIdealEdgeCalculation = !settings.CoseGraphSettings.UseSmartIdealEdgeCalculation;
                    break;
                /*case 2:
                    settings.CoseGraphSettings.UseSmartRepulsionRangeCalculation = !settings.CoseGraphSettings.UseSmartRepulsionRangeCalculation;
                    break;*/
                case 1:
                    if ((settings.CoseGraphSettings.GravityStrength + 0.1 * minusPlus) > 0)
                    {
                        settings.CoseGraphSettings.GravityStrength += 0.1 * minusPlus;
                    }
                    else
                    {
                        settings.CoseGraphSettings.GravityStrength += 0.1;
                    }

                    break;
                case 2:
                    if ((settings.CoseGraphSettings.CompoundGravityStrength + 0.1 * minusPlus) > 0)
                    {
                        settings.CoseGraphSettings.CompoundGravityStrength += 0.1 * minusPlus;
                    }
                    else
                    {
                        settings.CoseGraphSettings.CompoundGravityStrength += 0.1;
                    }
                    break;
                case 3:
                    if ((settings.CoseGraphSettings.RepulsionStrength + 1 * minusPlus) > 0)
                    {
                        settings.CoseGraphSettings.RepulsionStrength += 1 * minusPlus;
                    }
                    else
                    {
                        settings.CoseGraphSettings.RepulsionStrength += 1;
                    }
                    break;
                    /*case 5:
                        settings.CoseGraphSettings.multiLevelScaling = !settings.CoseGraphSettings.multiLevelScaling;
                        break;*/ 
            }

            oldValues = values;
            values = SetValues(settings.CoseGraphSettings.EdgeLength, settings.CoseGraphSettings.GravityStrength, settings.CoseGraphSettings.CompoundGravityStrength, settings.CoseGraphSettings.RepulsionStrength);

            Debug.Log("edgeLength: " + settings.CoseGraphSettings.EdgeLength + ", " +
                // "UseSmartIdealEdgeCalculation: "+ settings.CoseGraphSettings.UseSmartIdealEdgeCalculation+", " +
                "GravityStrength: " + settings.CoseGraphSettings.GravityStrength + ", " +
                "CompoundGravityStrength: " + settings.CoseGraphSettings.CompoundGravityStrength + ", " +
                "RepulsionStrength: " + settings.CoseGraphSettings.RepulsionStrength + ", ");
               // "multiLevelScaling: " + settings.CoseGraphSettings.multiLevelScaling);
        }

        private void EvaluateLayout(Dictionary<GameObject, NodeTransform> layout)
        {
           /* GraphRenderer.BoundingBox(nodeMap.Values, out Vector2 leftFrontCorner, out Vector2 rightBackCorner, leafNodeFactory, innerNodeFactory);
            _ = new Measurements(nodeMap, graph, settings, leftFrontCorner, rightBackCorner);
            bestMeasurements = settings.Measurements;
            double score = EvaluateMeasurements();

            if (score < bestScore)
            {
                bestScore = score;
            } else
            {
                ResetSolution();
            }*/
        }


        private void CreateOptimalSolutionMeasurements()
        {
            Dictionary<string, string> optimal = new Dictionary<string, string>();
            optimal.Add("Nodes overlapping", "0");
            optimal.Add("Number of edge crossings", "0");
            optimal.Add("Straight Edge length average", settings.CoseGraphSettings.EdgeLength.ToString());
            this.optimalMeasurements = optimal;
        }

        private double EvaluateMeasurements()
        {
            double diff = 0;

            foreach(KeyValuePair<string, string> kvp in optimalMeasurements)
            {
                if (bestMeasurements.ContainsKey(kvp.Key))
                {
                    double optimal = Double.Parse(kvp.Value);
                    string value;
                    bestMeasurements.TryGetValue(kvp.Key, out value);
                    double best = Double.Parse(value);

                    diff += Math.Pow(best - optimal,2);
                } else
                {
                    throw new System.Exception("measurements does not contain important key");
                }
            }

            diff = Math.Sqrt(diff);
            return diff;
        }
    }
}

