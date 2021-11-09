using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEE.Tools;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Unit tests for GraphWriter and GraphReader.
    /// </summary>
    internal class TestGraphIO
    {
        /// <summary>
        /// The extension of GXL files.
        /// </summary>
        private const string extension = ".gxl";
        /// <summary>
        /// A suffix to be added for GXL backup files. Will be appended at the end
        /// of a filename, just before the extension.
        /// </summary>
        private const string backupSuffix = "-backup";
        /// <summary>
        /// The name of the hierarchical edge type we use for emitting the parent-child
        /// relation among nodes.
        /// </summary>
        private const string hierarchicalEdgeType = "Enclosing";

        //[Test]
        //public void TestReadingRealBigGraph()
        //{
        //    string filename = Application.dataPath + "/../Data/GXL/graphs/bash.gxl";
        //    Performance p = Performance.Begin("Loading GXL file " + filename);
        //    Graph graph = LoadGraph(filename);
        //    p.End();
        //}

        [Test]
        public void TestReadingArchitecture()
        {
            LoadGraph(Application.dataPath + "/../Data/GXL/reflexion/java2rfg/Architecture.gxl");
        }

        [Test]
        public void TestReadingMapping()
        {
            LoadGraph(Application.dataPath + "/../Data/GXL/reflexion/java2rfg/Mapping.gxl");
        }

        [Test]
        public void TestReadingCodeFacts()
        {
            LoadGraph(Application.dataPath + "/../Data/GXL/reflexion/java2rfg/CodeFacts.gxl");
        }

        /// <summary>
        /// Test for a simple artificially created graph.
        /// </summary>
        [Test]
        public void TestGraphWriter()
        {
            string basename = "test";

            // Create and save the initial graph
            Graph outGraph = Create();
            outGraph.Name = "MyGraph";
            // Note: the path will not be stored in the GXL but will be set when the
            // graph is loaded by the GraphReader and it will be used by Equals
            // when comparing two graphs.
            outGraph.Path = basename + extension;

            WriteReadGraph(basename, outGraph);
        }

        /// <summary>
        /// Test with randomly generated graphs with increasing number of nodes.
        /// </summary>
        [Test]
        public void TestRandomGraphWriter()
        {
            RandomGraphs random = new RandomGraphs();
            Constraint leafConstraint = new Constraint("Routine", 10, "calls", 0.01f);
            Constraint innerNodesConstraint = new Constraint("File", 3, "imports", 0.01f);
            List<RandomAttributeDescriptor> attributeConstraints = new List<RandomAttributeDescriptor>()
            {
                new RandomAttributeDescriptor("Metric.LOC", 200, 50),
                new RandomAttributeDescriptor("Metric.Clone_Rate", 0.5f, 0.1f),
            };
            string basename = "random";

            for (int i = 0; i <= 100; i += 10)
            {
                leafConstraint.NodeNumber += i;
                innerNodesConstraint.NodeNumber += i;

                // Create and save the initial graph
                Graph outGraph = random.Create(leafConstraint, innerNodesConstraint, attributeConstraints);
                // When floating point node metrics are stored, they will be rounded and may then differ
                // from the more precise values we have in memory. Hence, we round the memory values.
                RoundMetrics(outGraph);
                outGraph.Name = "Random";
                // Note: the path will not be stored in the GXL but will be set when the
                // graph is loaded by the GraphReader and it will be used by Equals
                // when comparing two graphs.
                outGraph.Path = basename + extension;

                WriteReadGraph(basename, outGraph);
            }
        }

        private void RoundMetrics(Graph graph)
        {
            foreach (Node node in graph.Nodes())
            {
                // Make a copy of the attributes so that we can modify the original
                // values in the loop.
                Dictionary<string, float> floatAttributes = new Dictionary<string, float>(node.FloatAttributes);
                foreach (KeyValuePair<string, float> entry in floatAttributes)
                {
                    node.SetFloat(entry.Key, (float)Math.Round(entry.Value, 2));
                }
            }
        }

        /// <summary>
        /// Writes outGraph to F.
        /// Reads inGraph from F again.
        /// Compares outGraph and inGraph.
        /// Writes inGraph to F'.
        /// Reads backupGraph from F'.
        /// Compares inGraph and backupGraph.
        /// </summary>
        /// <param name="basename">basename of the filename for storing graphs</param>
        /// <param name="outGraph">the initial graph to be written</param>
        private static void WriteReadGraph(string basename, Graph outGraph)
        {
            string filename = basename + extension;

            // We need to finalize the node hierarchy so that the integer node attribute
            // Metric.Level is calculated for outgraph. Otherwise its node would not have
            // this attribute, but the nodes loaded from the saved graph would, which would
            // lead to an artifical discrepancy.
            outGraph.FinalizeNodeHierarchy();

            // Write outGraph
            GraphWriter.Save(filename, outGraph, hierarchicalEdgeType);

            // Read the saved outGraph again
            Graph inGraph = LoadGraph(filename);
            Assert.AreEqual(filename, inGraph.Path);

            // Write the loaded saved initial graph again as a backup
            string backupFilename = basename + backupSuffix + extension;
            GraphWriter.Save(backupFilename, inGraph, hierarchicalEdgeType);

            // Read the backup graph again
            Graph backupGraph = LoadGraph(backupFilename);
            // The path of backupGraph will be backupFilename.
            Assert.AreEqual(backupFilename, backupGraph.Path);
            // For the comparison, we need to reset the path.
            backupGraph.Path = inGraph.Path;

            Assert.That(outGraph.Equals(inGraph));
            Assert.That(backupGraph.Equals(inGraph));
        }

        private static Graph LoadGraph(string filename)
        {
            GraphReader graphReader = new GraphReader(filename, new HashSet<string> { hierarchicalEdgeType });
            graphReader.Load();
            return graphReader.GetGraph();
        }

        private static Node NewNode(Graph graph, string linkname)
        {
            Node result = new Node
            {
                ID = linkname,
                SourceName = linkname,
                Type = "Routine"
            };
            result.SetToggle("Linkage.Is_Definition");
            result.SetString("stringAttribute", "somestring");
            result.SetFloat("Metric.Halstead.Volume", 49.546f);
            result.SetInt("Metric.LOC", 10);
            graph.AddNode(result);
            return result;
        }

        private static Edge NewEdge(Graph graph, Node from, Node to, string type)
        {
            Edge result = new Edge($"{type}#{from.ID}#{to.ID}")
            {
                Type = type,
                Source = from,
                Target = to
            };
            result.SetToggle("Is Real");
            result.SetString("Source.Path", "path");
            result.SetFloat("Pi", 3.14f);
            result.SetInt("Source.Line", 10);
            result.SetInt("Source.Column", 1);
            graph.AddEdge(result);
            return result;
        }

        private static Graph Create()
        {
            Graph graph = new Graph();
            // Note: GXL does currently not support attributes of graphs
            //graph.SetString("Date", "2020-04-02");
            //graph.SetToggle("IsGenerated");

            Node parent = NewNode(graph, "parent");
            Node node1 = NewNode(graph, "node1");
            Node node2 = NewNode(graph, "node2");
            parent.AddChild(node1);
            parent.AddChild(node2);

            Edge edge1 = NewEdge(graph, node1, node2, "call");
            Edge edge2 = NewEdge(graph, node2, node1, "called");

            return graph;
        }
    }
}