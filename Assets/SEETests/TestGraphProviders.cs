using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Paths;
using System.Collections;
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
                { Path = NewDataPath("CodeFacts.gxl.xz") };

                GameObject go = new();
                SEECity city = go.AddComponent<SEECity>();

                Graph loaded = await provider.ProvideAsync(new Graph(""), city);
                Assert.IsNotNull(loaded);
                Assert.IsTrue(loaded.NodeCount > 0);
                Assert.IsTrue(loaded.EdgeCount > 0);
            });

        private static DataPath NewDataPath(string filename)
        {
            return new DataPath()
            {
                Path = Application.streamingAssetsPath + "/" + filename
            };
        }

        [UnityTest]
        public IEnumerator TestCSVJaCoCoGXLGraphProvider() =>
            UniTask.ToCoroutine(async () =>
            {
                GameObject go = new();
                SEECity city = go.AddComponent<SEECity>();

                PipelineGraphProvider pipeline = new();

                {
                    GraphProvider provider = new GXLGraphProvider()
                    { Path = NewDataPath("JLGExample/CodeFacts.gxl.xz") };
                    pipeline.Add(provider);
                }
                {
                    GraphProvider provider = new JaCoCoGraphProvider()
                    { Path = NewDataPath("JLGExample/jacoco.xml") };
                    pipeline.Add(provider);
                }

                {
                    GraphProvider provider = new CSVGraphProvider()
                    { Path = NewDataPath("JLGExample/CodeFacts.csv") };
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
                    Assert.IsTrue(node.TryGetInt("Metric.Developers", out int value));
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
                        { Path = NewDataPath("mini-evolution/CodeFacts-5.gxl") };
                        graph = await provider.ProvideAsync(new Graph(""), city);
                    }

                    {
                        // Older graph
                        GraphProvider provider = new GXLGraphProvider()
                        { Path = NewDataPath("mini-evolution/CodeFacts-1.gxl") };

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
    }
}
