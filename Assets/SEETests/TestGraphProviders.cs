using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
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
        public IEnumerator TestGXLGraphProviderAsync() =>
            UniTask.ToCoroutine(async () =>
            {
                GraphProvider provider = new GXLGraphProvider()
                { Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl") };

                GameObject go = new();
                SEECity city = go.AddComponent<SEECity>();

                Graph loaded = await provider.ProvideAsync(new Graph(""), city);
                Assert.IsNotNull(loaded);
                Assert.IsTrue(loaded.NodeCount > 0);
                Assert.IsTrue(loaded.EdgeCount > 0);
            });

        [UnityTest]
        public IEnumerator TestCSVJaCoCoGXLGraphProviderAsync() =>
            UniTask.ToCoroutine(async () =>
            {
                GameObject go = new();
                SEECity city = go.AddComponent<SEECity>();

                PipelineGraphProvider pipeline = new();

                {
                    GraphProvider provider = new GXLGraphProvider()
                    { Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl") };
                    pipeline.Add(provider);
                }
                {
                    GraphProvider provider = new JaCoCoGraphProvider()
                    { Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/JLGExample/jacoco.xml") };
                    pipeline.Add(provider);
                }

                {
                    GraphProvider provider = new CSVGraphProvider()
                    { Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/JLGExample/CodeFacts.csv") };
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

                //GraphWriter.Save("result.gxl", loaded, "Enclosing");
            });
    }
}
