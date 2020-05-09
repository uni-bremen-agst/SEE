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
using OdinSerializer.Utilities;

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
        //Dictionary<List<double>, SortedDictionary<string, string>> results = new Dictionary<List<double>, SortedDictionary<string, string>>();

        Dictionary<ILayoutNode, Vector3> mapGameObjectOriginalSize = new Dictionary<ILayoutNode, Vector3>();

        int maxNumberOfGraphs = 500;
        int totalNumberOfGraphs = 0;

        int CountLeafNodes = -1;
        int CountInnerNodes = -1;
        float EdgeDensityLeafNode = -0.1f;


        string pathPrefix = "Assets/Resources/Results/";
        string globalPath = "Assets/Resources/globalResults.txt";
        string path = "";
        //string firstLine = "edgeLength; RepulsionStrength; nodesOverlapping; Area; EdgeCrossings; MultiLevelScaling; SmartRepulsionRange; SmartEdgeLength; CountNodes; CountEdges; CountMaxDepth; CountAvgDepth; CountAvgDensity;";

        GameObject parent;

        OptAlgoIterationsRun itValue = new OptAlgoIterationsRun();

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
        /*
         * - Wo ist der Start-/ End Wert
         * - Wie groß sind die Schritte
         *
         *
         */

        public void StartOptAlgorithm(OptAlgoIterations it, bool save)
        {
            settings.CoseGraphSettings.multiLevelScaling = false;
            settings.CoseGraphSettings.UseSmartRepulsionRangeCalculation = false;
            settings.CoseGraphSettings.UseSmartIdealEdgeCalculation = false;

            for (int i = it.edgeLength.start; i < it.edgeLength.end; i += it.edgeLength.iterationStep)
            {
                for (int j = it.repulsionRange.start; j < it.repulsionRange.end; j += it.repulsionRange.iterationStep)
                {
                    /*for (int a = it.multiLevelScaling.start; a < it.multiLevelScaling.end; a += it.multiLevelScaling.iterationStep)
                    {
                        // smart repulsion range
                        for (int b = it.smartRepulsionRange.start; b < it.smartRepulsionRange.end; b += it.smartRepulsionRange.iterationStep)
                        {
                            // smart edge calculation
                            for (int d = it.smartEdgeLengthCalculation.start; d < it.smartEdgeLengthCalculation.end; d += it.smartEdgeLengthCalculation.iterationStep)
                            {*/
                                // vielleicht auswerten mit welchen die beste Lösung? und dann kann man schaune, ob multilevelscaling etc. das Layout wirklich verbessern?

                                settings.CoseGraphSettings.EdgeLength = (int)i;
                                 settings.CoseGraphSettings.RepulsionStrength = j;
                                /*if (a % 2 == 0)
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
                                }*/

                                NodeLayout nodeLayout = new CoseLayout(groundLevel, settings);

                                nodeLayout.Apply(layoutNodes.Cast<ILayoutNode>().ToList(), graph.Edges(), new List<SublayoutLayoutNode>());
                                NodeLayout.Move(layoutNodes.Cast<ILayoutNode>().ToList(), settings.origin);

                                EdgeDistCalculation(graph, layoutNodes);

                                BoundingBox(layoutNodes, out Vector2 FrontCorner, out Vector2 BackCorner);
                                Measurements measurements = new Measurements(layoutNodes, graph, FrontCorner, BackCorner);

                                var overlappingNodes = measurements.OverlappingGameNodes;
                                if (overlappingNodes <= 0)
                                {
                                    WriteToFile(i, j, overlappingNodes, measurements.Area, measurements.EdgeCrossings);
                                }

                                foreach (GameNode layoutNode in layoutNodes)
                                {
                                    layoutNode.Scale = mapGameObjectOriginalSize[layoutNode];
                                    layoutNode.CenterPosition = new Vector3(0, 0, 0);
                                }
                            /*}
                        }
                    }*/
                }
            }

            Tuple<int, int> values = BestSolution(save: save);

            if (values != null && !save)
            {
                int edgeLength = values.Item1;
                int repulsionStrength = values.Item2;

                itValue.specific.edgeLength.start = Math.Max(1, edgeLength - 5);
                itValue.specific.edgeLength.end = edgeLength + 5;
                itValue.specific.repulsionRange.start = Math.Max(1, repulsionStrength - 5);
                itValue.specific.repulsionRange.end = repulsionStrength + 5;

                StartOptAlgorithm(it: itValue.specific, save: true);
            }

            // TODO wenn durch grobes Ausprobieren eine Lösung gefunden, dann nochmal genauer probieren 
        }

        private int GetMinValue(List<String> file, int index)
        {
            int minValue = Int32.MaxValue;

            file.ForEach(line => {
                var values = line.Split(';');

                string valueString = values[index];
                int value = ParseInt(valueString);

                if (value < minValue)
                {
                    minValue = value;
                }
            });

            return minValue;
        }


        private int ParseInt(string str)
        {
            int value = Int32.MaxValue;
            try
            {
                value = Int32.Parse(str);
            }
            catch (FormatException)
            {
                if (str == "0")
                {
                    value = 0;
                }
            }
            return value;
        }

        private Tuple<int, int> BestSolution(bool save)
        {
            int edgeCrossingIndex = 4;
            int areaIndex = 3;
            int edgeLengthIndex = 0;

            var linesToKeep = File.ReadLines(path).ToList();

            if (linesToKeep.Count < 1)
            {
                return null;
            }

            int minEdgeCrossing = GetMinValue(file: linesToKeep, index: edgeCrossingIndex);

            linesToKeep = linesToKeep.Where(line => {
                var values = line.Split(';');
                int value = ParseInt(values[edgeCrossingIndex]);

                return value == minEdgeCrossing;
            }).ToList();

            int minArea = GetMinValue(file: linesToKeep, index: areaIndex);

            linesToKeep = linesToKeep.Where(line => {
                var values = line.Split(';');
                int value = ParseInt(values[areaIndex]);

                return value == minArea;
            }).ToList();

            String finalLine = "";
            if (linesToKeep.Count == 1)
            {
                finalLine = linesToKeep.First();
            } else if (linesToKeep.Count > 1)
            {
                linesToKeep.Sort((line1, line2) =>
                {
                    var values1 = line1.Split(';');
                    var values2 = line2.Split(';');

                    int value1 = ParseInt(values1[edgeLengthIndex]);  
                    int value2 = ParseInt(values2[edgeLengthIndex]); 

                    return value1.CompareTo(value2);
                });

                finalLine = linesToKeep.First();
            }

            if (finalLine.Count() > 0)
            {
                if (save)
                {
                    StreamWriter writer = new StreamWriter(globalPath, true);
                    writer.WriteLine(finalLine);
                    writer.Close();
                }


                var finalValues = finalLine.Split(';');

                int edgeLength = ParseInt(finalValues[0]);
                int repulsionStrength = ParseInt(finalValues[1]);

                return new Tuple<int, int>(edgeLength, repulsionStrength);
            }

            return null;

            //File.Delete(path);
        }


        private void WriteToFile(int edgeLength, double repulsionStrength, double nodesOverlapping, double area, double edgeCrossings)//, double a, double b, double d)
        {
            List<double> values = new List<double> { repulsionStrength, nodesOverlapping, area, edgeCrossings };//, a, b, d };
            //Write some text to the test.txt file
            StreamWriter writer = new StreamWriter(path, true);
            string line = "";

            line += edgeLength + ";";
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
            // 1, 301, Random.Range(1, 30), Random.Range(0.001f, 0.021f)
            Constraint LeafConstraint = new Tools.Constraint("Class", Random.Range(1, 50), "calls", Random.Range(0.001f, 0.021f));
            // 1, 101, Random.Range(1, 5)
            Constraint InnerNodeConstraint = new Tools.Constraint("Package", Random.Range(1, 10), "uses", 0f);
            SEECityRandom.DefaultAttributeMean = 10;
            SEECityRandom.DefaultAttributeStandardDerivation = 3;
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

            foreach (ILayoutNode gameNode in layoutNodes)
            {
                Vector3 scale = gameNode.Scale;
                mapGameObjectOriginalSize.Add(gameNode, new Vector3(scale.x, scale.y, scale.z));
            }

            StartOptAlgorithm(it: itValue.initial, save: true);

            //AddToParent(gameNodes, parent);
            // add the decorations, too
            //AddToParent(AddDecorations(gameNodes), parent);

            // create the laid out edges
            //AddToParent(EdgeLayout(graph, layoutNodes), parent);
            // add the plane surrounding all game objects for nodes

            //BoundingBox(layoutNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            //GameObject plane = NewPlane(leftFrontCorner, rightBackCorner);
            //AddToParent(plane, parent);

            totalNumberOfGraphs++;

            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                GameNode node = layoutNode as GameNode;
                GameObject obj = node.GetGameObject();
                Destroyer.DestroyGameObject(obj);
            }

            if (graph != null)
            {
                graph.Destroy();
            }
            graph = null;


            if (totalNumberOfGraphs != maxNumberOfGraphs)
            {
                Draw();
            }
        }

        public override void Draw(Graph graph, GameObject parent)
        {
            //SetupFile(path: globalPath);
            path = pathPrefix + totalNumberOfGraphs + ".txt";
            SetupFile(path: path);
            base.Draw(CreateRandomCity(), parent);
            this.parent = parent;

           // combine();


        }

        private void Draw()
        {
            mapGameObjectOriginalSize = new Dictionary<ILayoutNode, Vector3>();
            path = pathPrefix + totalNumberOfGraphs + ".txt";
            SetupFile(path: path);
            base.Draw(CreateRandomCity(), parent);
        }

        private void SetupFile(String path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (StreamWriter writer = File.CreateText(path))
            {
                //writer.WriteLine(firstLine);
            }
           
        }

        private void GetAverage()
        {
            Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();
            Dictionary<int, int> newResult = new Dictionary<int, int>();


            var linesToKeep = File.ReadLines("Assets/Resources/repulsionForce.txt").ToList();

            if (linesToKeep.Count > 1)
            {
                linesToKeep.ForEach(line =>
                {
                    var values = line.Split(';');
                    int countEdges = ParseInt(values[1]);
                    int repulsionForce = ParseInt(values[2]);

                    

                    if (result.ContainsKey(repulsionForce))
                    {
                        result[repulsionForce].Add(countEdges);
                    }
                    else
                    {
                        result.Add(repulsionForce, new List<int>(countEdges));
                    }
                });

            }

            result.ForEach(entry => {
                var length = entry.Value.Count;

                var sum = 0;

                entry.Value.ForEach(item => {
                    sum += item;
                });


                if (length > 0)
                {
                    var avg = sum / length;

                    newResult.Add(entry.Key, avg);
                }


            });

            using (StreamWriter writer = File.CreateText("Assets/Resources/repulsionForceResult.txt"))
            {

                newResult.ForEach(kvp => {
                    string line = kvp.Key + ";" + kvp.Value + ";";
                    writer.WriteLine(line);
                });
            }
        }


        private void combine()
        {
            Dictionary<Tuple<int, int>, List<int>> result = new Dictionary<Tuple<int, int>, List<int>>();

            Dictionary<Tuple<int, int>, int> newResult = new Dictionary<Tuple<int, int>, int>();


            var linesToKeep = File.ReadLines("Assets/Resources/repulsionForce.txt").ToList();

            if (linesToKeep.Count > 1)
            {
                linesToKeep.ForEach(line =>
                {
                    var values = line.Split(';');
                    int countNodes = ParseInt(values[0]);
                    int countEdges = ParseInt(values[1]);
                    int repulsionForce = ParseInt(values[2]);

                    var tuple = new Tuple<int, int>(countNodes, countEdges);

                    if (result.ContainsKey(tuple))
                    {
                        result[tuple].Add(repulsionForce);
                    }
                    else
                    {
                        result.Add(tuple, new List<int>(repulsionForce));
                    }
                });

            }

            result.ForEach( entry =>{
                var length = entry.Value.Count;

                var sum = 0;

                entry.Value.ForEach(item => {
                    sum += item;
                });


                if (length > 0)
                {
                    var avg = sum / length;

                    newResult.Add(entry.Key, avg);
                }

                
            });

            using (StreamWriter writer = File.CreateText("Assets/Resources/repulsionForceResult.txt"))
            {

                newResult.ForEach( kvp => {
                    string line = kvp.Key.Item1 + ";" + kvp.Key.Item2 + ";" + kvp.Value;
                writer.WriteLine(line);
                });
            }
        }
    }

    public class IterationConstraint
    {
        public int start;

        public int end;

        public int iterationStep;

        public IterationConstraint(int start, int end, int iterationStep)
        {
            this.start = start;
            this.end = end;
            this.iterationStep = iterationStep;
        }
    }

    public class OptAlgoIterations
    {
        // edge lngth is int 
        public IterationConstraint edgeLength = new IterationConstraint(start: 1, end: 30, iterationStep: 1);

        public IterationConstraint repulsionRange = new IterationConstraint(start: 1, end: 20, iterationStep: 1);

        public IterationConstraint multiLevelScaling = new IterationConstraint(start: 0, end: 2, iterationStep: 1);

        public IterationConstraint smartRepulsionRange = new IterationConstraint(start: 0, end: 2, iterationStep: 1);

        public IterationConstraint smartEdgeLengthCalculation = new IterationConstraint(start: 0, end: 2, iterationStep: 1);

        public OptAlgoIterations()
        {
            //
        }

    }

    public class OptAlgoIterationsRun
    {
        public readonly OptAlgoIterations initial = new OptAlgoIterations();

        public OptAlgoIterations specific = new OptAlgoIterations();

        public OptAlgoIterationsRun()
        {
            specific.edgeLength.iterationStep = 1;
            specific.repulsionRange.iterationStep = 1;
        }
    }
}