using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
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

        [Test]
        public async Task TestGXLGraphProviderAsync()
        {
            SingleGraphProvider provider = new GXLSingleGraphProvider()
            { Path = new DataPath(TestDataPath + "/JLGExample/CodeFacts.gxl.xz") };

            Graph loaded = await provider.ProvideAsync(new Graph(""), NewCity());
            Assert.IsNotNull(loaded);
            Assert.IsTrue(loaded.NodeCount > 0);
            Assert.IsTrue(loaded.EdgeCount > 0);
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
                    { Path = new DataPath(TestDataPath + "/JLGExample/CodeFacts.gxl.xz") };
                    graphPipeline.Add(provider);
                }
                {
                    SingleGraphProvider provider = new JaCoCoGraphProvider()
                    { Path = new DataPath(TestDataPath + "/JLGExample/jacoco.xml") };
                    graphPipeline.Add(provider);
                }

                {
                    SingleGraphProvider provider = new CSVGraphProvider()
                    { Path = new DataPath(TestDataPath + "/JLGExample/CodeFacts.csv") };
                    graphPipeline.Add(provider);
                }

                Graph loaded = await graphPipeline.ProvideAsync(new Graph(""), NewCity());
                Assert.IsNotNull(loaded);
                Assert.IsTrue(loaded.NodeCount > 0);
                Assert.IsTrue(loaded.EdgeCount > 0);

                Assert.IsTrue(loaded.TryGetNode("counter.CountToAThousand.countWithFibbonaci(I;)", out Node node));

                // Metric from JaCoCo report.
                {
                    Assert.IsTrue(node.TryGetInt(JaCoCo.BranchCovered, out int value));
                    Assert.AreEqual(5, value);
                }
                // Metric from CSV import.
                {
                    Assert.IsTrue(node.TryGetInt(Metrics.Prefix + "Developers", out int value));
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

                Assert.IsNotNull(diffGraph);
                Assert.IsTrue(diffGraph.NodeCount > 0);
                Assert.IsTrue(diffGraph.EdgeCount > 0);

                // Just a few checks. The underlying Diff-Merge algorithm is tested in more depth elsewhere.
                {
                    Assert.IsTrue(diffGraph.TryGetNode("p1.c1", out Node node));
                    Assert.IsTrue(node.HasToggle(ChangeMarkers.IsChanged));
                }

                {
                    Assert.IsTrue(diffGraph.TryGetNode("p1.c2", out Node node));
                    Assert.IsTrue(node.HasToggle(ChangeMarkers.IsNew));
                }

                {
                    Assert.IsTrue(diffGraph.TryGetEdge("Call#p1.c1#p1.c4", out Edge edge));
                    Assert.IsTrue(edge.HasToggle(ChangeMarkers.IsDeleted));
                }
            }
        }

        [Test]
        public async Task TestVCSGraphProviderAsync()
        {
            string repositoryPath = Application.dataPath;
            string projectPath = repositoryPath[..repositoryPath.LastIndexOf("/")];
            string projectName = Path.GetFileName(projectPath);

            List<string> expectedPaths = new()
            {
                projectName,
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
                "Assets/SEE/GraphProviders/VCSGraphProvider.cs"
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
            Assert.AreEqual(expectedPaths, pathsFromGraph);
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
            Node fileNode = graph.Nodes().First(t => t.Type == DataModel.DG.VCS.FileType);
            AssertTokenMetricsExist(fileNode);
        }

        private static void AssertTokenMetricsExist(Node node)
        {
            Assert.IsTrue(node.TryGetInt(Metrics.LOC, out int _));
            Assert.IsTrue(node.TryGetInt(Metrics.McCabe, out int _));
            Assert.IsTrue(node.TryGetInt(Halstead.DistinctOperators, out int _));
            Assert.IsTrue(node.TryGetInt(Halstead.DistinctOperands, out int _));
            Assert.IsTrue(node.TryGetInt(Halstead.TotalOperators, out int _));
            Assert.IsTrue(node.TryGetInt(Halstead.TotalOperands, out int _));
            Assert.IsTrue(node.TryGetInt(Halstead.ProgramVocabulary, out int _));
            Assert.IsTrue(node.TryGetInt(Halstead.ProgramLength, out int _));
            Assert.IsTrue(node.TryGetFloat(Halstead.EstimatedProgramLength, out float _));
            Assert.IsTrue(node.TryGetFloat(Halstead.Volume, out float _));
            Assert.IsTrue(node.TryGetFloat(Halstead.Difficulty, out float _));
            Assert.IsTrue(node.TryGetFloat(Halstead.Effort, out float _));
            Assert.IsTrue(node.TryGetFloat(Halstead.TimeRequiredToProgram, out float _));
            Assert.IsTrue(node.TryGetFloat(Halstead.NumberOfDeliveredBugs, out float _));
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
            Assert.IsNotNull(graph);
            Assert.IsTrue(graph.NodeCount > 0);
            Assert.IsTrue(graph.TryGetNode("Assets/SEE/GraphProviders/VCSGraphProvider.cs", out Node node));
            Assert.IsTrue(node.TryGetInt(metric, out int value));
            Assert.AreEqual(expected, value);
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
        /// Returns a new <see cref="SEECity"/> instance.
        /// </summary>
        /// <returns></returns>
        private static SEECity NewCity()
        {
            return new GameObject().AddComponent<SEECity>();
        }
    }
}
