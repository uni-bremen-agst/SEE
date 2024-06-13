using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Scanner;
using SEE.Utils.Paths;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Test cases for concrete subclasses of <see cref="GraphProvider"/>.
    /// </summary>
    internal class TestGraphProviders
    {
        [UnityTest]
        public IEnumerator TestGXLGraphProvider() =>
            UniTask.ToCoroutine(async () =>
            {
                GraphProvider provider = new GXLGraphProvider()
                { Path = new FilePath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl.xz") };

                GameObject go = new();
                SEECity city = go.AddComponent<SEECity>();

                Graph loaded = await provider.ProvideAsync(new Graph(""), city);
                Assert.IsNotNull(loaded);
                Assert.IsTrue(loaded.NodeCount > 0);
                Assert.IsTrue(loaded.EdgeCount > 0);
            });

        [UnityTest]
        public IEnumerator TestCSVJaCoCoGXLGraphProvider() =>
            UniTask.ToCoroutine(async () =>
            {
                GameObject go = new();
                SEECity city = go.AddComponent<SEECity>();

                PipelineGraphProvider pipeline = new();

                {
                    GraphProvider provider = new GXLGraphProvider()
                    { Path = new FilePath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl.xz") };
                    pipeline.Add(provider);
                }
                {
                    GraphProvider provider = new JaCoCoGraphProvider()
                    { Path = new FilePath(Application.streamingAssetsPath + "/JLGExample/jacoco.xml") };
                    pipeline.Add(provider);
                }

                {
                    GraphProvider provider = new CSVGraphProvider()
                    { Path = new FilePath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.csv") };
                    pipeline.Add(provider);
                }

                Graph loaded = await pipeline.ProvideAsync(new Graph(""), city);
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
            });

        [UnityTest]
        public IEnumerator TestMergeDiffGraphProvider() =>
                UniTask.ToCoroutine(async () =>
                {
                    GameObject go = new();
                    SEECity city = go.AddComponent<SEECity>();

                    // Newer graph
                    Graph graph;
                    {
                        GraphProvider provider = new GXLGraphProvider()
                        { Path = new FilePath(Application.streamingAssetsPath + "/mini-evolution/CodeFacts-5.gxl") };
                        graph = await provider.ProvideAsync(new Graph(""), city);
                    }

                    {
                        // Older graph
                        GraphProvider provider = new GXLGraphProvider()
                        { Path = new FilePath(Application.streamingAssetsPath + "/mini-evolution/CodeFacts-1.gxl") };

                        MergeDiffGraphProvider mergeDiffProvider = new()
                        {
                            OldGraph = provider
                        };

                        Graph diffGraph = await mergeDiffProvider.ProvideAsync(graph, city);

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
                });

        [Test]
        public async Task TestVCSGraphProviderAsync()
        {
            Graph graph = await GetVCSGraphAsync();

            List<string> pathsFromGraph = new();
            foreach (GraphElement elem in graph.Elements())
            {
                pathsFromGraph.Add(elem.ID);
            }
            const int numberOfCSFiles = 759;
            Assert.AreEqual(numberOfCSFiles, pathsFromGraph.Count());

            string repositoryPath = Application.dataPath;
            string projectPath = repositoryPath[..repositoryPath.LastIndexOf("/")];
            string projectName = Path.GetFileName(projectPath);

            List<string> actualList = new()
            {
                projectName,
                ".gitignore",
                "Assets",
                "Assets/Scenes.meta",
                "Assets/Scenes",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/SampleScene.unity.meta",
                "Packages",
                "Packages/manifest.json",
                "ProjectSettings",
                "ProjectSettings/AudioManager.asset",
                "ProjectSettings/ClusterInputManager.asset",
                "ProjectSettings/DynamicsManager.asset",
                "ProjectSettings/EditorBuildSettings.asset",
                "ProjectSettings/EditorSettings.asset",
                "ProjectSettings/GraphicsSettings.asset",
                "ProjectSettings/InputManager.asset",
                "ProjectSettings/NavMeshAreas.asset",
                "ProjectSettings/Physics2DSettings.asset",
                "ProjectSettings/PresetManager.asset",
                "ProjectSettings/ProjectSettings.asset",
                "ProjectSettings/ProjectVersion.txt",
                "ProjectSettings/QualitySettings.asset",
                "ProjectSettings/TagManager.asset",
                "ProjectSettings/TimeManager.asset",
                "ProjectSettings/UnityConnectSettings.asset",
                "ProjectSettings/VFXManager.asset",
                "ProjectSettings/XRSettings.asset"
            };

            Assert.IsTrue(actualList.OrderByDescending(x => x).ToList().SequenceEqual(pathsFromGraph.OrderByDescending(x => x).ToList()));
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
            Debug.Log(node.ToString());
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

        [UnityTest]
        public IEnumerator TestVCSMetrics() =>
            UniTask.ToCoroutine(async () =>
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
                    Assert.AreEqual(3, value);
                }
                {
                    Assert.IsTrue(node.TryGetInt(DataModel.DG.VCS.CommitFrequency, out int value));
                    Assert.AreEqual(12, value);
                }
            });

        private static async Task<Graph> GetVCSGraphAsync()
        {
            GameObject go = new();
            SEECity city = go.AddComponent<SEECity>();

            Dictionary<string, bool> pathGlobbing = new()
                {
                    { "Assets/SEE/**/*.cs", true }
                };

            VCSGraphProvider provider = new()
            {
                RepositoryPath = new DirectoryPath(Path.GetDirectoryName(Application.dataPath)),
                BaselineCommitID = "a5fe5e6a2692f41aeb8448d5114000e6f82e605e",
                CommitID = "0878f91f900dc90d89c594c521ac1d3b9edd7097",
                PathGlobbing = pathGlobbing
            };

            return await provider.ProvideAsync(new Graph(""), city);
        }
    }
}
