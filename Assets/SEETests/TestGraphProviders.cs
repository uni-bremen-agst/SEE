using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils;
using SEE.Utils.Paths;
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
        [Test]
        public async Task TestGXLGraphProviderAsync()
        {
            SingleGraphProvider provider = new GXLSingleGraphProvider()
            { Path = new DataPath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl.xz") };

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
                    { Path = new DataPath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl.xz") };
                    graphPipeline.Add(provider);
                }
                {
                    SingleGraphProvider provider = new JaCoCoGraphProvider()
                    { Path = new DataPath(Application.streamingAssetsPath + "/JLGExample/jacoco.xml") };
                    graphPipeline.Add(provider);
                }

                {
                    SingleGraphProvider provider = new CSVGraphProvider()
                    { Path = new DataPath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.csv") };
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
                "Assets",
                "Assets/SEE",
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

            Graph graph = await GetVCSGraphAsync();

            List<string> pathsFromGraph = new();
            foreach (GraphElement elem in graph.Elements())
            {
                pathsFromGraph.Add(elem.ID);
            }
            Assert.AreEqual(expectedPaths.Count, pathsFromGraph.Count());
            Assert.IsTrue(expectedPaths.OrderByDescending(x => x).ToList().SequenceEqual(pathsFromGraph.OrderByDescending(x => x).ToList()));
        }

        /// <summary>
        /// Checks whether a random file has the token metrics we expect.
        /// Note that we do not evaluate their values. This kind of test is
        /// is done in the test case <see cref="SEE.Scanner.TestTokenMetrics"/>.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestExistenceOfTokenMetricsAsync()
        {
            Graph graph = await GetVCSGraphAsync();
            Node fileNode = graph.Nodes().First(t => t.Type == "File");
            AssertTokenMetricsExist(fileNode);
        }

        private static void AssertTokenMetricsExist(Node node)
        {
            Assert.IsTrue(node.TryGetInt(Metrics.Prefix + "LOC", out int _));
            Assert.IsTrue(node.TryGetInt(Metrics.Prefix + "McCabe_Complexity", out int _));
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
        public async Task TestVCSMetricsAsync()
        {
            Graph graph = await GetVCSGraphAsync();
            Assert.IsNotNull(graph);
            Assert.IsTrue(graph.NodeCount > 0);

            Assert.IsTrue(graph.TryGetNode("Assets/SEE/GraphProviders/VCSGraphProvider.cs", out Node node));

            {
                Assert.IsTrue(node.TryGetInt(DataModel.DG.VCS.LinesAdded, out int value));
                Assert.AreEqual(157, value);
            }
            {
                Assert.IsTrue(node.TryGetInt(DataModel.DG.VCS.LinesDeleted, out int value));
                Assert.AreEqual(193, value);
            }
            {
                Assert.IsTrue(node.TryGetInt(DataModel.DG.VCS.NumberOfDevelopers, out int value));
                Assert.AreEqual(4, value);
            }
            {
                Assert.IsTrue(node.TryGetInt(DataModel.DG.VCS.CommitFrequency, out int value));
                Assert.AreEqual(12, value);
            }
        }

        /// <summary>
        /// The graph consisting of all C# files in folder Assets/SEE/GraphProviders.
        /// </summary>
        /// <returns>graph consisting of all C# files in folder Assets/SEE/GraphProviders</returns>
        private static async Task<Graph> GetVCSGraphAsync()
        {
            Globbing pathGlobbing = new()
                {
                    { "Assets/SEE/GraphProviders/**/*.cs", true }
                };

            VCSGraphProvider provider = new()
            {
                GitRepository = new GitRepository
                                     (new DataPath(Path.GetDirectoryName(Application.dataPath)),
                                     new SEE.VCS.Filter(globbing: pathGlobbing, repositoryPaths: null, branches: null)),
                BaselineCommitID = "a5fe5e6a2692f41aeb8448d5114000e6f82e605e",
                CommitID = "0878f91f900dc90d89c594c521ac1d3b9edd7097",
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
