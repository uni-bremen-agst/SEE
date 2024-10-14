using Cysharp.Threading.Tasks;
﻿using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;
using SEE.Utils.Paths;

namespace SEE.Tools.Architecture
{
    /// <summary>
    /// Stress tests for the reflexion analysis, using the minilax graph as a basis.
    /// </summary>
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
    internal class TestReflexionAnalysisStress : TestReflexionAnalysis
    {
        /// <summary>
        /// Non-incremental reflexion analysis for minilax example.
        /// </summary>
        [Test]
        public async Task TestMinilaxNonIncrementally()
        {
            await NonIncrementally("minilax");
            //reflexion.DumpArchitecture();
        }

        private async UniTask NonIncrementally(string folderName)
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
        [Test]
        public async Task TestMinilaxIncrementally()
        {
            await Incrementally("minilax");
            //reflexion.DumpArchitecture();
        }

        private async Task Incrementally(string folderName)
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
        [Test]
        public async Task TestMinilaxComparison()
        {
            const string folderName = "minilax";
            await NonIncrementally(folderName);
            int[] incrementally = graph.Summary();
            Teardown();
            Setup();
            await Incrementally(folderName);
            int[] nonIncrementally = graph.Summary();
            Assert.AreEqual(incrementally, nonIncrementally);
        }

        private async UniTask<Graph> LoadAsync(string path)
        {
            DataPath platformPath =new(Filenames.OnCurrentPlatform(path));
            Debug.Log($"Loading graph from {platformPath.Path}...\n");;
            Graph result = await GraphReader.LoadAsync(platformPath, HierarchicalEdges, basePath: "", logger: logger);
            Assert.That(result, !Is.Null);
            Debug.Log($"Loaded {result.NodeCount} nodes and {result.EdgeCount} edges.\n");
            //result.DumpTree();
            return result;
        }
        private async UniTask<(Graph impl, Graph arch, Graph mapping)> LoadAllAsync(string folderName)
        {
            string path = $"{Application.streamingAssetsPath}/reflexion/{folderName}/";
            Performance p = Performance.Begin("Loading graphs");
            Graph impl = await LoadAsync(new($"{path}CodeFacts.gxl.xz"));
            Graph arch = await LoadAsync(new($"{path}Architecture.gxl"));
            Graph mapping = await LoadAsync(new($"{path}Mapping.gxl"));
            p.End();
            return (impl, arch, mapping);
        }
    }
}
