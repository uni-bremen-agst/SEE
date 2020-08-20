// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.DataModel;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;
using System.IO;
using SEE.Game;
using SEE.Tools;
using static SEE.Game.AbstractSEECity;
using SEE.Utils;

namespace SEE.Layout
{
    public class OptAlgorithmGraphRenderer : GraphRenderer
    {
        /// <summary>
        /// the layout nodes
        /// </summary>
        List<ILayoutNode> layoutNodes;

        /// <summary>
        /// the graph
        /// </summary>
        Graph graph;

        /// <summary>
        /// the type of the optimization
        /// </summary>
        private readonly OptTypes type = OptTypes.CompareNodeLayouts;

        /// <summary>
        /// A dictionary holding layoutNodes with there inital size
        /// </summary>
        Dictionary<ILayoutNode, Vector3> mapGameObjectOriginalSize = new Dictionary<ILayoutNode, Vector3>();

        /// <summary>
        /// maximum number of graphs 
        /// </summary>
        private readonly int maxNumberOfGraphs = 250;

        /// <summary>
        /// the current number of graph 
        /// </summary>
        int totalNumberOfGraphs = 0;

        /// <summary>
        /// the total number of repetitions
        /// </summary>
        private readonly int totalReps = 1;

        /// <summary>
        /// the current number repetitions
        /// </summary>
        int currentReps = 0;

        /// <summary>
        /// number of leaf nodes
        /// </summary>
        int CountLeafNodes = -1;

        /// <summary>
        /// number of inner nodes 
        /// </summary>
        int CountInnerNodes = -1;

        /// <summary>
        /// edge density for the leaf nodes
        /// </summary>
        float EdgeDensityLeafNode = -0.1f;

        /// <summary>
        /// path prefix for result folder
        /// </summary>
        private readonly string pathPrefix = "Assets/Resources/Results/";

        /// <summary>
        /// path prefix for the global result file
        /// </summary>
        private readonly string globalPath = "Assets/Resources/globalResults.txt";

        /// <summary>
        /// the current path
        /// </summary>
        private string path = "";

        /// <summary>
        /// the path to the file for comparing the node layouts with each other 
        /// </summary>
        private string CompareNodeLayoutsPath
        {
            get
            {
                return "Assets/Resources/compareNodelayouts" + currentReps + ".txt";
            }
        } 

        /// <summary>
        /// the parent gameobject
        /// </summary>
        private GameObject parent;

        /// <summary>
        /// holding parameter values for the iterativ process
        /// </summary>
        private readonly OptAlgoIterationsRun itValue = new OptAlgoIterationsRun();

        public OptAlgorithmGraphRenderer(AbstractSEECity settings) : base(settings)
        {
            // do nothing
        }

        /// <summary>
        /// Starts and performs the iterative calculation process
        /// </summary>
        /// <param name="it">the parameter values for the iterations</param>
        /// <param name="save">true if the results should be saved</param>
        public void StartOptAlgorithm(OptAlgoIterations it, bool save)
        {
            settings.CoseGraphSettings.multiLevelScaling = false;
            settings.CoseGraphSettings.UseSmartRepulsionRangeCalculation = false;
            settings.CoseGraphSettings.UseSmartIdealEdgeCalculation = false;

            for (int i = it.edgeLength.start; i < it.edgeLength.end; i += it.edgeLength.iterationStep)
            {
                for (int j = it.repulsionRange.start; j < it.repulsionRange.end; j += it.repulsionRange.iterationStep)
                {

                    settings.CoseGraphSettings.EdgeLength = (int)i;
                    settings.CoseGraphSettings.RepulsionStrength = j;

                    NodeLayout nodeLayout = new CoseLayout(groundLevel, settings);

                    nodeLayout.Apply(layoutNodes.Cast<ILayoutNode>().ToList(), graph.Edges(), new List<SublayoutLayoutNode>());
                    NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x);
                    NodeLayout.MoveTo(layoutNodes, parent.transform.position);
                    
                    BoundingBox(layoutNodes, out Vector2 FrontCorner, out Vector2 BackCorner);
                    Measurements measurements = new Measurements(layoutNodes, graph, FrontCorner, BackCorner);

                    var overlappingNodes = measurements.OverlappingGameNodes;
                    if (overlappingNodes <= 0)
                    {
                        WriteToFile(i, j, overlappingNodes, measurements.Area, measurements.EdgeCrossings);
                    }

                    foreach (GameNode layoutNode in layoutNodes)
                    {
                        layoutNode.LocalScale = mapGameObjectOriginalSize[layoutNode];
                        layoutNode.CenterPosition = new Vector3(0, 0, 0);
                    }

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
        }

        /// <summary>
        /// Calculates a layout for a given node layout
        /// </summary>
        /// <param name="nodeLayout">the nodelayout to use</param>
        public void CalcLayout(NodeLayouts nodeLayout)
        {
            NodeLayout layout = CoseHelper.GetNodelayout(nodeLayout, groundLevel, leafNodeFactory.Unit, settings);

            Performance p = Performance.Begin("layout name" + settings.NodeLayout + ", layout of nodes");

            if (layout.UsesEdgesAndSublayoutNodes())
            {
                layout.Apply(layoutNodes.Cast<ILayoutNode>().ToList(), graph.Edges(), new List<SublayoutLayoutNode>());
            }
            else
            {
                layout.Apply(layoutNodes.Cast<ILayoutNode>().ToList());
            }
            p.End();

            BoundingBox(layoutNodes, out Vector2 FrontCorner, out Vector2 BackCorner);
            Measurements measurements = new Measurements(layoutNodes, graph, FrontCorner, BackCorner, p);

            WriteResultsToFile(measurements, nodeLayout);

            foreach (GameNode layoutNode in layoutNodes)
            {
                layoutNode.LocalScale = mapGameObjectOriginalSize[layoutNode];
                layoutNode.CenterPosition = new Vector3(0, 0, 0);
            }
        }

        /// <summary>
        /// Calculates the layout for all nodelayouts
        /// </summary>
        public void CalcLayoutForNodelayouts()
        {
            NodeLayouts nodeLayout = GetNodeLayoutForIteration();
            CalcLayout(nodeLayout); 
        }

        public NodeLayouts GetNodeLayoutForIteration()
        {
            return NodeLayouts.CirclePacking;

            //int mod = totalNumberOfGraphs % 6;
            //NodeLayouts nodeLayout;
            //
            //switch (mod)
            //{
            //    case 0:
            //        nodeLayout = NodeLayouts.CompoundSpringEmbedder;
            //        break;
            //    case 1:
            //        nodeLayout = NodeLayouts.EvoStreets;
            //        break;
            //    case 2:
            //        nodeLayout = NodeLayouts.Balloon;
            //        break;
            //    case 3:
            //        nodeLayout = NodeLayouts.RectanglePacking;
            //        break;
            //    case 4:
            //        nodeLayout = NodeLayouts.Treemap;
            //        break;
            //    case 5:
            //        nodeLayout = NodeLayouts.CirclePacking;
            //        break;
            //    default:
            //        nodeLayout = NodeLayouts.CompoundSpringEmbedder;
            //        break;
            //}
            //return nodeLayout;
        }

        /// <summary>
        /// Writes the measurements/ graph properties of a layout to the file
        /// </summary>
        /// <param name="measurements"></param>
        /// <param name="nodeLayout"></param>
        public void WriteResultsToFile(Measurements measurements, NodeLayouts nodeLayout)
        {
            StreamWriter writer = new StreamWriter(CompareNodeLayoutsPath, true);
            string line = "";

            var name = nodeLayout.ToString();

            //line += "graphID; nodeLayout;  countNodes; CountEdges; CountDepth; CountDepthAvg; CountDensityAvg; CountLeafNodes; CountInnderNodes; EdgeDensityLeafNode; Area; NodesOverlapping; NumberEdgeCrossings; EdgeAvg; EdgeAvgArea; EdgeMax; EdgeMaxArea; EdgeMin; EdgeMinArea; EdgeStandardDeviation; EdgeStandardDeviationArea; EdgeLengthTotal; EdgeLengthTotalArea; EdgeVariance; EdgeVarianceArea; NodePerformance; NodePerformanceInMilli; ";
            line += totalNumberOfGraphs + ";";
            line += name + ";";
            line += CountNodes(graph) + ";";
            line += CountEdges(graph) + ";";
            line += CountDepth(graph) + ";";
            line += CountDepthAvg(graph) + ";";
            line += CountDensityAvg(graph) + ";";
            line += CountLeafNodes + ";";
            line += CountInnerNodes + ";";
            line += EdgeDensityLeafNode + ";";

            SortedDictionary<string, string> m = measurements.ToStringDictionary();

            foreach (KeyValuePair<string, string> kvp in m)
            {
                line += kvp.Value + ";";
            }

            line += measurements.NodePerformanceInMilliSeconds.ToString();

            writer.WriteLine(line);
            writer.Close();
        }

        /// <summary>
        /// returns the smallest value from a file for a given value index
        /// </summary>
        /// <param name="file">the file</param>
        /// <param name="index">the index of the value</param>
        /// <returns></returns>
        private int GetMinValue(List<String> file, int index)
        {
            int minValue = Int32.MaxValue;

            file.ForEach(line =>
            {
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

        /// <summary>
        /// parses a string to an int 
        /// </summary>
        /// <param name="str">the string to parse </param>
        /// <returns></returns>
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

        /// <summary>
        /// Finds and saves the best solution to a file and returns a tuple containing the edgeLength and repulsionStrength
        /// </summary>
        /// <param name="save">indicates if the best solution should be saved</param>
        /// <returns>a tuple containing the edgeLength and repulsionStrength</returns>
        private Tuple<int, int> BestSolution(bool save)
        {
            int edgeCrossingIndex = 4;
            int areaIndex = 3;
            int edgeLengthIndex = 0;

            var linesToKeep = new List<string>();
            if (File.Exists(path))
            {
                linesToKeep = File.ReadLines(path).ToList();
            } else
            {
                linesToKeep = new List<string>();
            }

            if (linesToKeep.Count < 1)
            {
                return null;
            }

            int minEdgeCrossing = GetMinValue(file: linesToKeep, index: edgeCrossingIndex);

            linesToKeep = linesToKeep.Where(line =>
            {
                var values = line.Split(';');
                int value = ParseInt(values[edgeCrossingIndex]);

                return value == minEdgeCrossing;
            }).ToList();

            int minArea = GetMinValue(file: linesToKeep, index: areaIndex);

            linesToKeep = linesToKeep.Where(line =>
            {
                var values = line.Split(';');
                int value = ParseInt(values[areaIndex]);

                return value == minArea;
            }).ToList();

            String finalLine = "";
            if (linesToKeep.Count == 1)
            {
                finalLine = linesToKeep.First();
            }
            else if (linesToKeep.Count > 1)
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

        /// <summary>
        /// Writes the results to a result file
        /// </summary>
        /// <param name="edgeLength">the edgeLength</param>
        /// <param name="repulsionStrength">the repulsionStrength</param>
        /// <param name="nodesOverlapping">true if nodes overlapp</param>
        /// <param name="area">the area used for the layout</param>
        /// <param name="edgeCrossings">number of edge crossings</param>
        private void WriteToFile(int edgeLength, double repulsionStrength, double nodesOverlapping, double area, double edgeCrossings)
        {
            List<double> values = new List<double> { repulsionStrength, nodesOverlapping, area, edgeCrossings };

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

        /// <summary>
        /// Returns the number of nodes for a given graph
        /// </summary>
        /// <param name="graph">a graph</param>
        /// <returns>count of nodes</returns>
        private int CountNodes(Graph graph)
        {
            return graph.NodeCount;
        }

        /// <summary>
        /// Returns the number of edges for a given graph
        /// </summary>
        /// <param name="graph">a graph</param>
        /// <returns>count of edges</returns>
        private int CountEdges(Graph graph)
        {
            return graph.EdgeCount;
        }

        /// <summary>
        /// Returns maximal depth of a graph
        /// </summary>
        /// <param name="graph">a graph</param>
        /// <returns>max depth</returns>
        private int CountDepth(Graph graph)
        {
            return graph.GetMaxDepth();
        }

        /// <summary>
        /// count the average depth of nodes for a graph
        /// </summary>
        /// <param name="graph">the graph</param>
        /// <returns>the average depth</returns>
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

        /// <summary>
        /// Calc the average edge density for a node of a given graph
        /// </summary>
        /// <param name="graph">a graph</param>
        /// <returns>average edge density</returns>
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

        /// <summary>
        /// Creates a Random City 
        /// </summary>
        /// <returns>the created graph</returns>
        private Graph CreateRandomCity()
        {
            // 1, 301, Random.Range(1, 30), Random.Range(0.001f, 0.021f)
            // Random.Range(1, 100), "calls", Random.Range(0.001f, 0.05f)
            Constraint LeafConstraint = new Tools.Constraint("Class", Random.Range(1, 50), "calls", Random.Range(0.001f, 0.05f));
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

        /// <summary>
        /// Draw a city 
        /// </summary>
        /// <param name="graph">the graph to draw</param>
        /// <param name="parent">the parent gameObject</param>
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
                Vector3 scale = gameNode.LocalScale;
                mapGameObjectOriginalSize.Add(gameNode, new Vector3(scale.x, scale.y, scale.z));
            }

            if (type == OptTypes.CompareNodeLayouts)
            {
                CalcLayoutForNodelayouts();
            }
            else
            {
                StartOptAlgorithm(it: itValue.initial, save: true);
            }

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

            to_layout_node = new Dictionary<Node, ILayoutNode>();

            if (totalNumberOfGraphs != maxNumberOfGraphs)
            {
                Draw();
            } else
            {
                currentReps++;
                if (currentReps != totalReps)
                {
                    totalNumberOfGraphs = 0;
                    Draw();
                }
            }
        }

        /// <summary>
        /// calculates the layout for a given graph
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="parent"></param>
        public override void Draw(Graph graph, GameObject parent)
        {
            settings.CoseGraphSettings.UseSmartIdealEdgeCalculation = false;
            settings.CoseGraphSettings.UseSmartMultilevelScaling = false;
            settings.CoseGraphSettings.UseSmartRepulsionRangeCalculation = false;
            settings.CoseGraphSettings.multiLevelScaling = false;
            settings.CoseGraphSettings.useCalculationParameter = false;

            if (type == OptTypes.CompareNodeLayouts)
            {
                SetupFile(CompareNodeLayoutsPath);

                StreamWriter writer = new StreamWriter(CompareNodeLayoutsPath, true);
                string line = "";

                line += "graphID; nodeLayout;  countNodes; CountEdges; CountDepth; CountDepthAvg; CountDensityAvg; CountLeafNodes; CountInnderNodes; EdgeDensityLeafNode; Area; NodesOverlapping; NumberEdgeCrossings; EdgeAvg; EdgeAvgArea; EdgeMax; EdgeMaxArea; EdgeMin; EdgeMinArea; EdgeStandardDeviation; EdgeStandardDeviationArea; EdgeLengthTotal; EdgeLengthTotalArea; EdgeVariance; EdgeVarianceArea; NodePerformance;  NodePerformanceInMilli;";
                writer.WriteLine(line);
                writer.Close();

                settings.CoseGraphSettings.useItertivCalclation = true;
            }
            else
            {
                settings.CoseGraphSettings.useItertivCalclation = false;
                //SetupFile(path: globalPath);
                path = pathPrefix + totalNumberOfGraphs + ".txt";
                SetupFile(path: path);

                using (var stream = File.CreateText(path))
                {

                }
            }


            base.Draw(CreateRandomCity(), parent);
            this.parent = parent;
        }

        /// <summary>
        /// Resets and prefairs for a new graph and starts calculation process
        /// </summary>
        private void Draw()
        {
            mapGameObjectOriginalSize = new Dictionary<ILayoutNode, Vector3>();

            if (type == OptTypes.FindGoodParameter)
            {
                path = pathPrefix + totalNumberOfGraphs + ".txt";
                SetupFile(path: path);
            }

            base.Draw(CreateRandomCity(), parent);
        }

        /// <summary>
        /// Sets up a file
        /// </summary>
        /// <param name="path">the path for the file</param>
        private void SetupFile(String path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// a class holding parameter values 
    /// </summary>
    public class IterationConstraint
    {
        /// <summary>
        /// the start value
        /// </summary>
        public int start;

        /// <summary>
        /// the end value
        /// </summary>
        public int end;

        /// <summary>
        /// The value by which the parameter value is increased within each iteration
        /// </summary>
        public int iterationStep;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="start"> the start value</param>
        /// <param name="end">the end value</param>
        /// <param name="iterationStep">The value by which the parameter value is increased within each iteration</param>
        public IterationConstraint(int start, int end, int iterationStep)
        {
            this.start = start;
            this.end = end;
            this.iterationStep = iterationStep;
        }
    }

    public class OptAlgoIterations
    {
        /// <summary>
        /// parameter values for edge length
        /// </summary>
        public IterationConstraint edgeLength = new IterationConstraint(start: 1, end: 30, iterationStep: 1);

        /// <summary>
        /// parameter values for repulsion range
        /// </summary>
        public IterationConstraint repulsionRange = new IterationConstraint(start: 1, end: 20, iterationStep: 1);

        public OptAlgoIterations()
        {
            //
        }

    }

    /// <summary>
    /// paramter values gfor the optimization 
    /// </summary>
    public class OptAlgoIterationsRun
    {
        /// <summary>
        /// the inital values
        /// </summary>
        public readonly OptAlgoIterations initial = new OptAlgoIterations();

        /// <summary>
        /// more specific values
        /// </summary>
        public OptAlgoIterations specific = new OptAlgoIterations();

        public OptAlgoIterationsRun()
        {
            specific.edgeLength.iterationStep = 1;
            specific.repulsionRange.iterationStep = 1;
        }
    }

    /// <summary>
    /// Types of the optimization algorithm
    /// </summary>
    public enum OptTypes
    {
        /// <summary>
        /// Try to find good parameters for random generated graphs
        /// </summary>
        FindGoodParameter,

        /// <summary>
        /// Calculates Layouts for all nodeLayouts for random generated graphs
        /// </summary>
        CompareNodeLayouts
    }
}