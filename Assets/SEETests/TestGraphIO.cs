using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.Tools.RandomGraphs;
using SEE.Utils;
using SEE.Utils.Paths;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Unit tests for GraphWriter and GraphReader.
    /// </summary>
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
    internal class TestGraphIO
    {
        /// <summary>
        /// The extension of GXL files.
        /// </summary>
        private const string NormalExtension = ".gxl";
        /// <summary>
        /// The extension of compressed GXL files.
        /// </summary>
        private const string CompressedExtension = ".gxl.xz";
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

        [Test]
        public async Task TestReadingRealBigGraph()
        {
            DataPath path = new(Application.streamingAssetsPath + "/SEE/CodeFacts.gxl.xz");
            Performance p = Performance.Begin("Loading big GXL file " + path);
            await LoadGraphAsync(path);
            p.End();
        }

        [Test]
        public async Task TestReadingArchitecture()
        {
            await LoadGraphAsync(new(Application.dataPath + "/../Data/GXL/reflexion/java2rfg/Architecture.gxl"));
        }

        [Test]
        public async Task TestReadingMapping()
        {
            await LoadGraphAsync(new(Application.dataPath + "/../Data/GXL/reflexion/java2rfg/Mapping.gxl"));
        }

        [Test]
        public async Task TestReadingCodeFacts()
        {
            await LoadGraphAsync(new(Application.dataPath + "/../Data/GXL/reflexion/java2rfg/CodeFacts.gxl.xz"));
        }

        private static bool CompressedWritingSupported()
        {
            if (Environment.GetEnvironmentVariable("RUNNER_OS") == "Linux")
            {
                // Not supported on CI, because docker image for game-ci/unity-test-runner is based on Ubuntu 18.04,
                // which does not support liblzma properly.
                Assert.Ignore("Saving compressed GXL files is not yet supported on the CI runner.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Test for a simple artificially created graph.
        /// </summary>
        [Test, Sequential]
        public async Task TestGraphWriter([Values(true, false)] bool compress)
        {
            const string basename = "test";

            // Create and save the initial graph
            Graph outGraph = Create();
            outGraph.Name = "MyGraph";
            // Note: the path will not be stored in the GXL but will be set when the
            // graph is loaded by the GraphReader and it will be used by Equals
            // when comparing two graphs.
            outGraph.Path = basename + (compress ? NormalExtension : CompressedExtension);

            if (!compress || CompressedWritingSupported())
            {
                await WriteReadGraphAsync(basename, outGraph, compress);
            }
        }

        /// <summary>
        /// Test with randomly generated graphs with increasing number of nodes.
        /// </summary>
        [Test, Sequential]
        public async Task TestRandomGraphWriterAsync([Values(true, false)] bool compress)
        {
            Constraint leafConstraint = new("Routine", 10, "calls", 0.01f);
            Constraint innerNodesConstraint = new("File", 3, "imports", 0.01f);
            List<RandomAttributeDescriptor> attributeConstraints = new()
            {
                new RandomAttributeDescriptor(Metrics.Prefix + "LOC", 200, 50, -10, 100),
                new RandomAttributeDescriptor(Metrics.Prefix + "Clone_Rate", 0.5f, 0.1f, -0.5f, 1.3f),
            };
            const string basename = "random";

            for (int i = 0; i <= 100; i += 10)
            {
                leafConstraint.NodeNumber += i;
                innerNodesConstraint.NodeNumber += i;

                // Create and save the initial graph
                Graph outGraph = RandomGraphs.Create(leafConstraint, innerNodesConstraint, attributeConstraints);
                // When floating point node metrics are stored, they will be rounded and may then differ
                // from the more precise values we have in memory. Hence, we round the memory values.
                RoundMetrics(outGraph);
                outGraph.Name = "Random";
                // Note: the path will not be stored in the GXL but will be set when the
                // graph is loaded by the GraphReader and it will be used by Equals
                // when comparing two graphs.
                outGraph.Path = basename + (compress ? NormalExtension : CompressedExtension);

                if (!compress || CompressedWritingSupported())
                {
                    await WriteReadGraphAsync(basename, outGraph, compress);
                }
            }
        }

        private static void RoundMetrics(Graph graph)
        {
            foreach (Node node in graph.Nodes())
            {
                // Make a copy of the attributes so that we can modify the original
                // values in the loop.
                Dictionary<string, float> floatAttributes = new(node.FloatAttributes);
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
        /// <param name="compress">whether to LZMA compress the graph</param>
        private static async UniTask WriteReadGraphAsync(string basename, Graph outGraph, bool compress)
        {
            string Extension = compress ? CompressedExtension : NormalExtension;
            DataPath path = new(basename + Extension);
            DataPath backupPath = new(basename + backupSuffix + Extension);

            // We need to finalize the node hierarchy so that the integer node attribute
            // Metric.Level is calculated for outgraph. Otherwise its node would not have
            // this attribute, but the nodes loaded from the saved graph would, which would
            // lead to an artificial discrepancy.
            outGraph.FinalizeNodeHierarchy();

            try
            {
                // Write outGraph
                GraphWriter.Save(path.Path, outGraph, hierarchicalEdgeType);

                // Read the saved outGraph again
                Graph inGraph = await LoadGraphAsync(path);
                Assert.AreEqual(path.Path, inGraph.Path);

                // Write the loaded saved initial graph again as a backup
                GraphWriter.Save(backupPath.Path, inGraph, hierarchicalEdgeType);

                // Read the backup graph again
                Graph backupGraph = await LoadGraphAsync(backupPath);
                // The path of backupGraph will be backupFilename.
                Assert.AreEqual(backupPath.Path, backupGraph.Path);
                // For the comparison, we need to reset the path.
                backupGraph.Path = inGraph.Path = outGraph.Path;

                Assert.AreEqual(outGraph, inGraph);
                Assert.AreEqual(backupGraph, inGraph);
            }
            finally
            {
                FileIO.DeleteIfExists(path.Path);
                FileIO.DeleteIfExists(backupPath.Path);
            }
        }

        private static async UniTask<Graph> LoadGraphAsync(DataPath path)
        {
            return await GraphReader.LoadAsync(path, new HashSet<string> { hierarchicalEdgeType }, basePath: "");
        }

        private static Node NewNode(Graph graph, string linkname)
        {
            Node result = new()
            {
                ID = linkname,
                SourceName = linkname,
                Type = "Routine"
            };
            result.SetToggle("Linkage.Is_Definition");
            result.SetString("stringAttribute", "somestring");
            result.SetFloat(Halstead.Volume, 49.546f);
            result.SetInt(Metrics.Prefix + "LOC", 10);
            graph.AddNode(result);
            return result;
        }

        private static Edge NewEdge(Graph graph, Node from, Node to, string type)
        {
            Edge result = new(from, to, type);
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
            Graph graph = new("DUMMYBASEPATH");
            // Note: GXL does currently not support attributes of graphs
            //graph.SetString("Date", "2020-04-02");
            //graph.SetToggle("IsGenerated");

            Node parent = NewNode(graph, "parent");
            Node node1 = NewNode(graph, "node1");
            Node node2 = NewNode(graph, "node2");
            parent.AddChild(node1);
            parent.AddChild(node2);

            NewEdge(graph, node1, node2, "call");
            NewEdge(graph, node2, node1, "called");

            return graph;
        }
    }
}
