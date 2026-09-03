using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO.ReportImports;
using SEE.DataModel.DG.IO.GXL;
using SEE.Game.City;
using SEE.Utils;
using SEE.Utils.Paths;
using SEE.VCS;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Test cases for concrete subclasses of <see cref="GraphProvider"/>.
    /// </summary>
    /// <remarks>We do want to use the UnityTest attribute for the test methods
    /// listed in this class because otherwise they would be run asynchronously
    /// in which case they may interfere with each other regarding the
    /// logging output. I saw a test run in which <see cref="TestGXLGraphProviderAsync"/>
    /// picked up the error messages of another test case.</remarks>
    internal class TestGraphProviders
    {
        private string TestDataPath => Application.dataPath + "/../Data";

        /// <summary>
        /// Path to JaCoCo GXL file relative to <see cref="TestDataPath"/>.
        /// </summary>
        private const string jacocoGXL = "/jacoco/jacoco.gxl.xz";

        /// <summary>
        /// Path to the JaCoCo report file for the JaCoCo code relative to <see cref="TestDataPath"/>.
        /// </summary>
        private const string jacocoXML = "/jacoco/jacoco-results.xml";

        /// <summary>
        /// Path to the additional metric file for the JaCoCo code relative to <see cref="TestDataPath"/>.
        /// </summary>
        private const string jacocoCSV = "/jacoco/jacoco.csv";

        [Test]
        public async Task TestGXLGraphProviderAsync()
        {
            SingleGraphProvider provider = new GXLSingleGraphProvider()
            { Path = new DataPath(TestDataPath + jacocoGXL) };

            Graph loaded = await provider.ProvideAsync(new Graph(""), NewCity());
            Assert.That(loaded, Is.Not.Null);
            // The number of nodes in the JaCoCo GXL file can be determined by running the
            // following command in our project root of SEE:
            // xz -dc ./Data/jacoco.gxl.xz | grep "<node id=" | wc -l
            Assert.That(loaded.NodeCount, Is.EqualTo(1585));
            // The JaCoCo GXL file does not contain any edges, so we expect the edge count to be 0.
            Assert.That(loaded.EdgeCount, Is.EqualTo(0));
        }

        [Test]
        public async Task TestCSVJaCoCoGXLGraphProviderAsync()
        {
            DataModel.DG.GraphIndex.FileRanges.ReportMissingSourceRange = false;

            try
            {
                SingleGraphPipelineProvider graphPipeline = new();

                {
                    SingleGraphProvider provider = new GXLSingleGraphProvider()
                    { Path = new DataPath(TestDataPath + jacocoGXL) };
                    graphPipeline.Add(provider);
                }
                {
                    SingleGraphProvider provider = new ReportGraphProvider()
                    {
                        Path = new DataPath(TestDataPath + jacocoXML),
                        ParsingConfig = new JaCoCoParsingConfig()
                    };
                    graphPipeline.Add(provider);
                }

                {
                    SingleGraphProvider provider = new CSVGraphProvider()
                    { Path = new DataPath(TestDataPath + jacocoCSV) };
                    graphPipeline.Add(provider);
                }

                Graph loaded = await graphPipeline.ProvideAsync(new Graph(""), NewCity());

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.NodeCount, Is.GreaterThan(0));
                Assert.That(loaded.EdgeCount, Is.EqualTo(0));
                Assert.That(loaded.TryGetNode("org.jacoco.core.tools.ExecFileLoader.getExecutionDataStore()", out Node node),
                            Is.True, "Node counter.CountToAThousand.countWithFibbonaci(I;) is missing.");


                // Metric from CSV import.
                {
                    Assert.That(node.TryGetInt(Metrics.Prefix + "Developers", out int value), Is.True,
                                $"Node {node.ID} has no metric {Metrics.Prefix}Developers.");
                    Assert.That(value, Is.EqualTo(3));
                }
                // Metrics from JaCoCo report.
                {
                    Assert.IsTrue(node.TryGetInt(JaCoCo.InstructionMissed, out int value));
                    Assert.AreEqual(0, value);
                }
                {
                    Assert.IsTrue(node.TryGetInt(JaCoCo.InstructionCovered, out int value));
                    Assert.AreEqual(3, value);
                }
            }
            finally
            {
                DataModel.DG.GraphIndex.FileRanges.ReportMissingSourceRange = true;
            }
        }

        [Test]
        public async Task TestMergeDiffGraphProviderAsync()
        {
            // Newer graph
            Graph graph;
            {
                SingleGraphProvider provider = new GXLSingleGraphProvider()
                { Path = new DataPath(Application.streamingAssetsPath + "/mini-evolution/CodeFacts-5.gxl") };
                graph = await provider.ProvideAsync(new Graph(""), NewCity());
            }

            {
                // Older graph
                SingleGraphProvider provider = new GXLSingleGraphProvider()
                { Path = new DataPath(Application.streamingAssetsPath + "/mini-evolution/CodeFacts-1.gxl") };

                MergeDiffGraphProvider mergeDiffProvider = new()
                {
                    OldGraph = provider
                };

                Graph diffGraph = await mergeDiffProvider.ProvideAsync(graph, NewCity());

                Assert.That(diffGraph, Is.Not.Null);
                Assert.That(diffGraph.NodeCount, Is.GreaterThan(0));
                Assert.That(diffGraph.EdgeCount, Is.GreaterThan(0));

                // Just a few checks. The underlying Diff-Merge algorithm is tested in more depth elsewhere.
                {
                    Assert.That(diffGraph.TryGetNode("p1.c1", out Node node), Is.True,
                                "Node p1.c1 is missing in the diff graph.");
                    Assert.That(node.HasToggle(ChangeMarkers.IsChanged), Is.True,
                                $"{node.ID} has no toggle {ChangeMarkers.IsChanged}.");
                }

                {
                    Assert.That(diffGraph.TryGetNode("p1.c2", out Node node), Is.True,
                                "Node p1.c2 is missing in the diff graph.");
                    Assert.That(node.HasToggle(ChangeMarkers.IsNew), Is.True,
                                $"{node.ID} has no toggle {ChangeMarkers.IsNew}.");
                }

                {
                    Assert.That(diffGraph.TryGetEdge("Call#p1.c1#p1.c4", out Edge edge), Is.True,
                                "Edge Call#p1.c1#p1.c4 is missing in the diff graph.");
                    Assert.That(edge.HasToggle(ChangeMarkers.IsDeleted), Is.True,
                                $"{edge.ID} has no toggle {ChangeMarkers.IsDeleted}.");
                }
            }
        }

        [Test]
        public async Task TestVCSGraphProviderAsync()
        {
            List<string> expectedPaths = new()
            {
                "Assets/SEE/GraphProviders",
                "Assets/SEE/GraphProviders/CSVGraphProvider.cs",
                "Assets/SEE/GraphProviders/DashboardGraphProvider.cs",
                "Assets/SEE/GraphProviders/FileBasedGraphProvider.cs",
                "Assets/SEE/GraphProviders/GXLGraphProvider.cs",
                "Assets/SEE/GraphProviders/GraphProvider.cs",
                "Assets/SEE/GraphProviders/GraphProviderFactory.cs",
                "Assets/SEE/GraphProviders/GraphProviderKind.cs",
                "Assets/SEE/GraphProviders/JaCoCoGraphProvider.cs",
                "Assets/SEE/GraphProviders/LSPGraphProvider.cs",
                "Assets/SEE/GraphProviders/MergeDiffGraphProvider.cs",
                "Assets/SEE/GraphProviders/PipelineGraphProvider.cs",
                "Assets/SEE/GraphProviders/ReflexionGraphProvider.cs",
                "Assets/SEE/GraphProviders/VCSGraphProvider.cs",
            };
            expectedPaths.Sort();

            Graph graph = await GetVCSGraphAsync(false);

            // Node IDs are unique, so we can use a list.
            List<string> pathsFromGraph = new();
            foreach (Node node in graph.Nodes())
            {
                pathsFromGraph.Add(node.ID);
            }
            pathsFromGraph.Sort();
            Assert.That(pathsFromGraph, Is.EqualTo(expectedPaths));
        }

        /// <summary>
        /// Prints all <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">Paths to be printed.</param>
        /// <remarks>Can be used for debugging.</remarks>
        private void Dump(List<string> paths)
        {
            foreach (string path in paths)
            {
                Debug.Log(path + "\n");
            }
        }

        /// <summary>
        /// Checks whether a random file has the token metrics we expect.
        /// Note that we do not evaluate their values. This kind of test is
        /// is done in the test case <see cref="SEE.Scanner.TestTokenMetrics"/>.
        /// </summary>
        [Test]
        public async Task TestExistenceOfTokenMetricsAsync()
        {
            Graph graph = await GetVCSGraphAsync();
            Node fileNode = graph.Nodes().First(t => t.Type == DataModel.DG.NodeTypes.File);
            AssertTokenMetricsExist(fileNode);
        }

        private static void AssertTokenMetricsExist(Node node)
        {
            Assert.That(node.TryGetInt(Metrics.LOC, out int _), Is.True,
                        $"Node {node.ID} has no metric {Metrics.LOC}.");
            Assert.That(node.TryGetInt(Metrics.McCabe, out int _), Is.True,
                        $"Node {node.ID} has no metric {Metrics.McCabe}.");
            Assert.That(node.TryGetInt(Halstead.DistinctOperators, out int _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.DistinctOperators}.");
            Assert.That(node.TryGetInt(Halstead.DistinctOperands, out int _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.DistinctOperands}.");
            Assert.That(node.TryGetInt(Halstead.TotalOperators, out int _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.TotalOperators}.");
            Assert.That(node.TryGetInt(Halstead.TotalOperands, out int _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.TotalOperands}.");
            Assert.That(node.TryGetInt(Halstead.ProgramVocabulary, out int _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.ProgramVocabulary}.");
            Assert.That(node.TryGetInt(Halstead.ProgramLength, out int _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.ProgramLength}.");
            Assert.That(node.TryGetFloat(Halstead.EstimatedProgramLength, out float _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.EstimatedProgramLength}.");
            Assert.That(node.TryGetFloat(Halstead.Volume, out float _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.Volume}.");
            Assert.That(node.TryGetFloat(Halstead.Difficulty, out float _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.Difficulty}.");
            Assert.That(node.TryGetFloat(Halstead.Effort, out float _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.Effort}.");
            Assert.That(node.TryGetFloat(Halstead.TimeRequiredToProgram, out float _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.TimeRequiredToProgram}.");
            Assert.That(node.TryGetFloat(Halstead.NumberOfDeliveredBugs, out float _), Is.True,
                        $"Node {node.ID} has no metric {Halstead.NumberOfDeliveredBugs}.");
        }

        [Test]
        [Category("SkipOnCI")]  // We do a checkout with fetch-depth 1 in CI, so we cannot get all VCS metrics.
        // Is not equivalent to:
        //   git diff --shortstat a5fe5e6a2692f41aeb8448d5114000e6f82e605e 0878f91f900dc90d89c594c521ac1d3b9edd7097 -- Assets/SEE/GraphProviders/VCSGraphProvider.cs
        // because the latter compares only the first commit to the second commit,
        // but does not include the commits in between. Our churn metrics, however,
        // are based on all commits between the two.
        [TestCase(DataModel.DG.VCS.LinesAdded, 284)]
        [TestCase(DataModel.DG.VCS.LinesRemoved, 320)]
        // Should be equivalent to:
        // git log 0878f91f900dc90d89c594c521ac1d3b9edd7097 ^a5fe5e6a2692f41aeb8448d5114000e6f82e605e -- Assets/SEE/GraphProviders/VCSGraphProvider.cs|grep ^Author|sort -u|wc -l
        [TestCase(DataModel.DG.VCS.NumberOfDevelopers, 3)]
        // Should be equivalent to:
        // git log 0878f91f900dc90d89c594c521ac1d3b9edd7097 ^a5fe5e6a2692f41aeb8448d5114000e6f82e605e --name-status| grep VCSGraphProvider.cs | wc -l
        // git rev-list --topo-order --reverse --no-merges a5fe5e6a2692f41aeb8448d5114000e6f82e605e..0878f91f900dc90d89c594c521ac1d3b9edd7097 -- Assets/SEE/GraphProviders/VCSGraphProvider.cs|wc -l
        [TestCase(DataModel.DG.VCS.NumberOfCommits, 11)]
        public async Task TestVCSMetricsAsync(string metric, int expected)
        {
            Graph graph = await GetVCSGraphAsync();
            Assert.That(graph, Is.Not.Null);
            Assert.That(graph.NodeCount, Is.GreaterThan(0));
            Assert.That(graph.TryGetNode("Assets/SEE/GraphProviders/VCSGraphProvider.cs", out Node node),
                        Is.True, "Node Assets/SEE/GraphProviders/VCSGraphProvider.cs is missing.");
            Assert.That(node.TryGetInt(metric, out int value), Is.True,
                        $"Node {node.ID} has no metric {metric}.");
            Assert.That(value, Is.EqualTo(expected));
        }

        /// <summary>
        /// Saves the given <paramref name="graph"/> to a temporary file.
        /// Can be used to debug the graph provider.
        /// </summary>
        /// <param name="graph">Graph to be saved.</param>
        private void Save(Graph graph)
        {
            string filename = Path.GetTempFileName();
            GraphWriter.Save(filename, graph, "Part_Of");
            Debug.Log($"Graph saved to {filename}.\n");
        }

        /// <summary>
        /// The graph consisting of all C# files in folder Assets/SEE/GraphProviders in
        /// any of the branches of our SEE repository between two specific commits.
        /// </summary>
        /// <param name="simplifyGraph">if true, the graph will be simplified</param>
        /// <returns>graph consisting of all C# files in folder Assets/SEE/GraphProviders</returns>
        private static async Task<Graph> GetVCSGraphAsync(bool simplifyGraph = false)
        {
            Globbing pathGlobbing = new()
                {
                    { "**/*.cs", true }
                };

            IEnumerable<string> repositoryPaths = new[]
            {
                "Assets/SEE/GraphProviders",
            };

            BetweenCommitsGraphProvider provider = new()
            {
                GitRepository = new GitRepository
                                     (new DataPath(Path.GetDirectoryName(Application.dataPath)),
                                      new Filter(globbing: pathGlobbing, repositoryPaths: repositoryPaths, branches: null)),
                BaselineCommitID = "a5fe5e6a2692f41aeb8448d5114000e6f82e605e", // May 10 11:50:16 2024
                CommitID = "0878f91f900dc90d89c594c521ac1d3b9edd7097",         // May 19 18:16:08 2024
                SimplifyGraph = simplifyGraph,
            };

            return await provider.ProvideAsync(new Graph(""), NewCity());
        }

        /// <summary>
        /// Returns a new <see cref="SEECity"/> instance (attached to a new,
        /// otherwise empty <see cref="GameObject"/>.
        /// </summary>
        /// <returns>New <see cref="SEECity"/> instance.</returns>
        private static SEECity NewCity()
        {
            return new GameObject().AddComponent<SEECity>();
        }
    }
}
