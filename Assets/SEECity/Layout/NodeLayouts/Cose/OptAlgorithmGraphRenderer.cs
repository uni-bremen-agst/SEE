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
using SEE.Tools;

namespace SEE.Layout
{
    public class OptAlgorithmGraphRenderer : GraphRenderer
    {
        List<ILayoutNode> layoutNodes;
        Graph graph;
        NodeFactory leafNodeFactory;
        float groundLevel;
        Dictionary<string, string> optimalMeasurements;
        SortedDictionary<string, string> bestMeasurements;
        int maxIterations = 2500;
        int iteration = 0;
        double bestScore = Mathf.Infinity;
        List<SublayoutLayoutNode> sublayoutNodes = new List<SublayoutLayoutNode>();
        Dictionary<string, double> values = new Dictionary<string, double>();
        Dictionary<string, double> oldValues = new Dictionary<string, double>();
        Dictionary<List<double>, SortedDictionary<string, string>> results = new Dictionary<List<double>, SortedDictionary<string, string>>();

        Dictionary<ILayoutNode, Vector3> mapGameObjectOriginalSize = new Dictionary<ILayoutNode, Vector3>();

        int edgeLengthMin = 0;
        int edgeLengthMax = 300;
        int repulsionStrengthMin = 0;
        int repulsionStrengthMax = 300;

        int maxNumberOfGraphs = 2;
        int totalNumberOfGraphs = 0;

        int CountLeafNodes = -1;
        int CountInnerNodes = -1;
        double EdgeDensityLeafNode = -0.1;

        GameObject parent; 

        public OptAlgorithmGraphRenderer(AbstractSEECity settings) : base(settings)
        {

        }

        public OptAlgorithmGraphRenderer(AbstractSEECity settings, float groundLevel, Graph graph, List<SublayoutNode> sublayoutNodes, List<ILayoutNode> layoutNodes) : base(settings)
        {
            this.layoutNodes = layoutNodes;
            this.graph = graph;
            this.groundLevel = groundLevel;
           
            string path = "Assets/Resources/results.txt";
            //Write some text to the test.txt file
            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine("edgeLength; RepulsionStrength; nodesOverlapping; Area; EdgeCrossings; MultiLevelScaling; SmartRepulsionRange; SmartEdgeLength; CountNodes; CountEdges; CountMaxDepth; CountAvgDepth; CountAvgDensity;");
            writer.Close();


            foreach (ILayoutNode gameNode in layoutNodes)
            {
                Vector3 scale = gameNode.Scale;
                mapGameObjectOriginalSize.Add(gameNode, new Vector3(scale.x, scale.y, scale.z));
            }
        }

        public void StartOptAlgorithm()
        {
            List<Node> nodes = graph.Nodes();

            for (int i = 1; i < 15; i += 1)
            {
                for (int j = 1; j < 10; j += 1)
                {
                    for (int a = 0; a < 2; a++)
                    {
                        // smart repulsion range
                        for (int b = 0; b < 2; b++)
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

                                NodeLayout nodeLayout = new CoseLayout(groundLevel, settings, leafNodeFactory);

                                nodeLayout.Apply(layoutNodes.Cast<ILayoutNode>().ToList(), graph.Edges(), new List<SublayoutLayoutNode>());
                                NodeLayout.Move(layoutNodes.Cast<ILayoutNode>().ToList(), settings.origin);

                                EdgeDistCalculation(graph, layoutNodes);

                                BoundingBox(layoutNodes, out Vector2 FrontCorner, out Vector2 BackCorner);
                                _ = new Measurements(layoutNodes.Cast<GameNode>().ToList(), graph, settings, FrontCorner, BackCorner);
                                bestMeasurements = settings.Measurements;

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

                                foreach (GameNode layoutNode in layoutNodes)
                                {
                                    layoutNode.Scale = mapGameObjectOriginalSize[layoutNode];
                                    layoutNode.CenterPosition = new Vector3(0, 0, 0);
                                }
                            }
                        }

                    }
                }
            }

            List<KeyValuePair<List<double>, SortedDictionary<string, string>>> list = results.ToList();

            list.Sort((pair, pair2) => Int64.Parse(pair.Value["Number of edge crossings"]).CompareTo(Int64.Parse(pair2.Value["Number of edge crossings"])));

            if (list.Count > 0)
            {
                KeyValuePair<List<double>, SortedDictionary<string, string>> first = list.First();

                list.RemoveAll(pair => pair.Value["Number of edge crossings"] != first.Value["Number of edge crossings"]);

                list.Sort((pair, pair2) => Double.Parse(pair.Value["Area (Plane)"]).CompareTo(Double.Parse(pair2.Value["Area (Plane)"])));

                KeyValuePair<List<double>, SortedDictionary<string, string>> result = list.First();
            }

        }

        private void WriteToFile(double edgeLength, double repulsionStrength, double nodesOverlapping, double area, double edgeCrossings, double a, double b, double d)
        {
            string path = "Assets/Resources/results.txt";

            List<double> values = new List<double> { edgeLength, repulsionStrength, nodesOverlapping, area, edgeCrossings, a, b, d };
            //Write some text to the test.txt file
            StreamWriter writer = new StreamWriter(path, true);
            string line = "";
            foreach (double value in values)
            {
                line += (int)value + ";";
            }

            line += CountNodes(graph) + ";";
            line += CountEdges(graph) + ";";
            line += CountDepth(graph) + ";";
            line += CountDepthAvg(graph) + ";";
            line += CountDensityAvg(graph) + ";";
            line += CountLeafNodes + ";";
            line += CountInnerNodes + ";";
            line += EdgeDensityLeafNode + ";";

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
            foreach (Node node in graph.Nodes())
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
            foreach (Node node in graph.Nodes())
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

        private Graph CreateRandomCity()
        {
            Constraint LeafConstraint = new Tools.Constraint("Class", Random.Range(1, 301), "calls", Random.Range(0.001f, 0.021f));
            Constraint InnerNodeConstraint = new Tools.Constraint("Package", Random.Range(1, 101), "uses", 0f);
            SEECityRandom.DefaultAttributeMean = 10;
            SEECityRandom.DefaultAttributeStandardDerivation = 2000;
            List<RandomAttributeDescriptor> LeafAttributes = SEECityRandom.Defaults();

            CountLeafNodes = LeafConstraint.NodeNumber;
            EdgeDensityLeafNode = LeafConstraint.EdgeDensity;
            CountInnerNodes = InnerNodeConstraint.NodeNumber;

            RandomGraphs randomGraphs = new RandomGraphs();
            return randomGraphs.Create(LeafConstraint, InnerNodeConstraint, LeafAttributes);
        }

        protected override void DrawCity(Graph graph, GameObject parent)
        {
            this.graph = graph;

            List<Node> nodes = graph.Nodes();

            Dictionary<Node, GameObject> nodeMap = CreateBlocks(nodes);
            Dictionary<Node, GameObject>.ValueCollection gameNodes;
            AddInnerNodes(nodeMap, nodes);

            // calculate and apply the node layout
            gameNodes = nodeMap.Values;
            layoutNodes = ToLayoutNodes(gameNodes).Cast<ILayoutNode>().ToList();

            string path = "Assets/Resources/results.txt";
            //Write some text to the test.txt file
            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine("edgeLength; RepulsionStrength; nodesOverlapping; Area; EdgeCrossings; MultiLevelScaling; SmartRepulsionRange; SmartEdgeLength; CountNodes; CountEdges; CountMaxDepth; CountAvgDepth; CountAvgDensity; CountLeafNodes; CountInnerNodes; EdgeDensityLeafNodes;");
            writer.Close();

            foreach (ILayoutNode gameNode in layoutNodes)
            {
                Vector3 scale = gameNode.Scale;
                mapGameObjectOriginalSize.Add(gameNode, new Vector3(scale.x, scale.y, scale.z));
            }

            StartOptAlgorithm();

            AddToParent(gameNodes, parent);
            // add the decorations, too
            AddToParent(AddDecorations(gameNodes), parent);

            // create the laid out edges
            AddToParent(EdgeLayout(graph, layoutNodes), parent);
            // add the plane surrounding all game objects for nodes

            BoundingBox(layoutNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            GameObject plane = NewPlane(leftFrontCorner, rightBackCorner);
            AddToParent(plane, parent);

            totalNumberOfGraphs++;
            if (totalNumberOfGraphs != maxNumberOfGraphs)
            {
                Draw();
            }
        }

        public override void Draw(Graph graph, GameObject parent)
        {
            base.Draw(CreateRandomCity(), parent);
            this.parent = parent;
        }

        private void Draw()
        {
            base.Draw(CreateRandomCity(), parent);
        }
    }
}

