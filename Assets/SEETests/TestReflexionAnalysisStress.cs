using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.Tools.Architecture
{
    /// <summary>
    /// Stress tests for the reflexion analysis, using the minilax graph as a basis.
    /// </summary>
    internal class TestReflexionAnalysisStress : TestReflexionAnalysis
    {
        /// <summary>
        /// Non-incremental reflexion analysis for minilax example.
        /// </summary>
        [UnityTest]
        public IEnumerator TestMinilaxNonIncrementally() =>
            UniTask.ToCoroutine(async () => await NonIncrementallyAsync("minilax"));

        private async Task NonIncrementallyAsync(string folderName)
        {
            (Graph impl, Graph arch, Graph mapping) = await LoadAllAsync(folderName);
            Performance p = Performance.Begin("Running non-incremental reflexion analysis");
            graph = new ReflexionGraph(impl, arch, mapping);
            graph.Subscribe(this);
            graph.RunAnalysis();
            p.End();
        }

        /// <summary>
        /// Incremental reflexion analysis for minilax example.
        /// </summary>
        [UnityTest]
        public IEnumerator TestMinilaxIncrementally() =>
            UniTask.ToCoroutine(async () => await IncrementallyAsync("minilax"));

        private async Task IncrementallyAsync(string folderName)
        {
            (Graph impl, Graph arch, Graph mapping) = await LoadAllAsync(folderName);
            Performance p = Performance.Begin("Running incremental reflexion analysis");
            // Passing the empty graph as mapping argument to reflexion.
            graph = new ReflexionGraph(impl, arch, new Graph("DUMMYBASEPATH"));
            graph.Subscribe(this);
            graph.RunAnalysis(); // from scratch
            // Now add the mappings incrementally.
            foreach (Edge map in mapping.Edges())
            {
                Node source = graph.GetNode(map.Source.ID);
                Assert.IsTrue(source.IsInImplementation());
                Node target = graph.GetNode(map.Target.ID);
                Assert.IsTrue(target.IsInArchitecture());
                Assert.NotNull(source);
                Assert.NotNull(target);
                graph.AddToMapping(source, target);
            }
            p.End();
        }

        /// <summary>
        /// Compares the result of incremental and non-incremental reflexion analysis for minilax example.
        /// </summary>
        [UnityTest]
        public IEnumerator TestMinilaxComparison() =>
             UniTask.ToCoroutine(async () =>
             {
                 const string folderName = "minilax";
                 await NonIncrementallyAsync(folderName);
                 int[] incrementally = graph.Summary();
                 Teardown();
                 Setup();
                 await IncrementallyAsync(folderName);
                 int[] nonIncrementally = graph.Summary();
                 Assert.AreEqual(incrementally, nonIncrementally);
             });

        private async Task<Graph> LoadAsync(string path)
        {
            string platformPath = Filenames.OnCurrentPlatform(path);
            Debug.Log($"Loading graph from {platformPath}...\n");;
            Graph result = await GraphReader.LoadAsync(platformPath, HierarchicalEdges, basePath: "", logger);
            Assert.That(result, !Is.Null);
            Debug.Log($"Loaded {result.NodeCount} nodes and {result.EdgeCount} edges.\n");
            //result.DumpTree();
            return result;
        }

        private async Task<Tuple<Graph, Graph, Graph>> LoadAllAsync(string folderName)
        {
            string path = $"{Application.streamingAssetsPath}/reflexion/{folderName}/";
            Performance p = Performance.Begin("Loading graphs");
            Graph impl = await LoadAsync($"{path}CodeFacts.gxl.xz");
            Graph arch = await LoadAsync($"{path}Architecture.gxl");
            Graph mapping = await LoadAsync($"{path}Mapping.gxl");
            p.End();
            return new Tuple<Graph, Graph, Graph>(impl, arch, mapping);
        }
    }
}
