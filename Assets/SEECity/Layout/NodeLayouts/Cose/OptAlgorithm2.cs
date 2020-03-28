using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;
using System.IO;
using SEE.Game;
using SEE.GO;

namespace SEE.Layout
{
    public class OptAlgorithm2 : GraphRenderer
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
        Dictionary<List<double>, SortedDictionary<string, string>> results = new Dictionary<List<double>, SortedDictionary<string, string>>();

        Dictionary<GameObject, Vector3> mapGameObjectOriginalSize = new Dictionary<GameObject, Vector3>();

        int edgeLengthMin = 0;
        int edgeLengthMax = 300;
        int repulsionStrengthMin = 0;
        int repulsionStrengthMax = 300;


        public OptAlgorithm2(AbstractSEECity settings, float groundLevel, Graph graph, List<SublayoutNode> sublayoutNodes, Dictionary<Node, GameObject> nodeMap, InnerNodeFactory innerNodeFactory, NodeFactory leafNodeFactory) : base(settings)
        {
            this.nodeMap = nodeMap;
            this.graph = graph;
            this.settings = settings;
            this.groundLevel = groundLevel;
            this.sublayoutNodes = sublayoutNodes;
            this.innerNodeFactory = innerNodeFactory;
            this.leafNodeFactory = leafNodeFactory; 

            string path = "Assets/Resources/results.txt";
            //Write some text to the test.txt file
            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine("edgeLength; RepulsionStrength; nodesOverlapping; Area; EdgeCrossings; MultiLevelScaling; SmartRepulsionRange; SmartEdgeLength; CountNodes; CountEdges; CountMaxDepth; CountAvgDepth; CountAvgDensity;");
            writer.Close();


            foreach(GameObject obj in nodeMap.Values)
            {
                Vector3 scale = obj.transform.localScale;
                mapGameObjectOriginalSize.Add(obj, new Vector3(scale.x, scale.y, scale.z));
            }
        }

        public void StartOptAlgorithm()
        {
            //SetScaler(graph);
            //graph.SortHierarchyByName();
            List<Node> nodes = graph.Nodes();

            for (int i = 14; i <15; i += 1)
            {
                for (int j = 0; j < 1; j += 1)
                {
                    for (int a = 0; a < 2; a++)
                    {
                        // smart repulsion range
                        for (int b  = 0; b < 2; b ++)
                        {
                            // smart edge calculation
                            for (int d = 0; d < 2; d++)
                            {

                                // vielleicht auswerten mit welchen die beste Lösung? und dann kann man schaune, ob multilevelscaling etc. das Layout wirklich verbessern?



                                settings.CoseGraphSettings.EdgeLength = i;
                                settings.CoseGraphSettings.RepulsionStrength = j;
                                if (a % 2 == 0)
                                {
                                    settings.CoseGraphSettings.multiLevelScaling = false;
                                }
                                else
                                {
                                    settings.CoseGraphSettings.multiLevelScaling = true;
                                }

                                if (b % 2 == 0)
                                {
                                    settings.CoseGraphSettings.UseSmartRepulsionRangeCalculation = false;
                                }
                                else
                                {
                                    settings.CoseGraphSettings.UseSmartRepulsionRangeCalculation = true;
                                }

                                if (d % 2 == 0)
                                {
                                    settings.CoseGraphSettings.UseSmartIdealEdgeCalculation = false;
                                }
                                else
                                {
                                    settings.CoseGraphSettings.UseSmartIdealEdgeCalculation = true;
                                }
                                // TODO reparairen
                               /* bestLayout = new CoseLayout(groundLevel, leafNodeFactory, false, graph.Edges(), settings, sublayoutNodes).Layout(nodeMap.Values);
                                ICollection<GameObject> gameNodes = bestLayout.Keys;
                                Apply(bestLayout, settings.origin);
                                EdgeDistCalculation(graph, bestLayout.Keys);

                                BoundingBox(gameNodes, out Vector2 FrontCorner, out Vector2 BackCorner, leafNodeFactory, innerNodeFactory);
                                _ = new Measurements(nodeMap, graph, settings, FrontCorner, BackCorner);
                                bestMeasurements = settings.Measurements; */

                                if (bestMeasurements.ContainsKey("Nodes overlapping"))
                                {
                                    int overlappingAmount = Int32.Parse(bestMeasurements["Nodes overlapping"]);
                                    double area = Double.Parse(bestMeasurements["Area (Plane)"]);
                                    double edgeCrossings = Double.Parse(bestMeasurements["Number of edge crossings"]);

                                    if (overlappingAmount == 0)
                                    {
                                        results.Add(new List<double> { i, j }, new SortedDictionary<string, string>(bestMeasurements));
                                        WriteToFile(i, j, overlappingAmount, area, edgeCrossings, a % 2, b % 2, d % 2);
                                    }
                                }

                                foreach (GameObject obj in nodeMap.Values)
                                {
                                    obj.transform.localScale = mapGameObjectOriginalSize[obj];
                                    obj.transform.position = new Vector3(0, 0, 0);
                                }
                            }
                        }

                    }
                }
            }

            List<KeyValuePair<List<double>, SortedDictionary<string, string>>> list = results.ToList();

            list.Sort((pair, pair2) => Int64.Parse(pair.Value["Number of edge crossings"]).CompareTo(Int64.Parse(pair2.Value["Number of edge crossings"])));

            KeyValuePair<List<double>, SortedDictionary<string, string>> first = list.First();

            list.RemoveAll(pair => pair.Value["Number of edge crossings"] != first.Value["Number of edge crossings"]);

            list.Sort((pair, pair2) => Double.Parse(pair.Value["Area (Plane)"]).CompareTo(Double.Parse(pair2.Value["Area (Plane)"])));

            KeyValuePair<List<double>, SortedDictionary<string, string>> result = list.First();
        }

        private void WriteToFile(double edgeLength, double repulsionStrength, double nodesOverlapping, double area, double edgeCrossings, double a, double b, double d)
        {
            string path = "Assets/Resources/results.txt";

            List<double> values = new List<double> { edgeLength, repulsionStrength, nodesOverlapping, area, edgeCrossings, a, b, d};
            //Write some text to the test.txt file
            StreamWriter writer = new StreamWriter(path, true);
            string line = "";
            foreach(double value in values)
            {
                line += (int) value + ";";
            }

            line += CountNodes(graph) + ";";
            line += CountEdges(graph) + ";";
            line += CountDepth(graph) + ";";
            line += CountDepthAvg(graph) + ";";
            line += CountDensityAvg(graph) + ";";

            writer.WriteLine(line);
            writer.Close();
        }

        private int CountNodes(Graph graph)
        {
            return graph.NodeCount;
        }
        private int CountEdges(Graph graph)
        {
            return graph.EdgeCount;
        }

        private int CountDepth(Graph graph)
        {
            return graph.GetMaxDepth();
        }

        private int CountDepthAvg(Graph graph)
        {
            int depth = 0;
            int leafCount = 0;
            foreach(Node node in graph.Nodes())
            {
                if (node.IsLeaf())
                {
                    depth += node.Level;
                    leafCount++;
                }
            }

            return depth / leafCount;
        }

        private int CountDensityAvg(Graph graph)
        {
            int density = 0;
            int countNodes = 0;
            List<Edge> edges = graph.Edges();
            foreach(Node node in graph.Nodes())
            {
                List<Edge> edgeList = edges.Where(edge => edge.Source == node || edge.Target == node).ToList();
                density += edgeList.Count;

                if (node.IsLeaf())
                {
                    countNodes++;
                }
            }
            return density / countNodes;
        }
    }
}

