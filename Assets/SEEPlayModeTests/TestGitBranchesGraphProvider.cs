using NUnit.Framework;
using SEE.Game.City;
using SEE.DataModel.DG;
using SEE.Utils;
using UnityEngine;
using System.Collections.Generic;
using SEE.Utils.Paths;
using System.Threading.Tasks;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Tests for <see cref="GitBranchesGraphProvider"/> that need to be run
    /// in play mode. If we want to profile our code, we need play mode.
    /// </summary>
    internal class TestGitBranchesGraphProvider
    {
        /// <summary>
        /// Test for <see cref="GitBranchesGraphProvider.ProvideAsync(Graph, AbstractSEECity, System.Action{float}, System.Threading.CancellationToken)"/> where we analyze our own repository. This task takes a
        /// long time.
        /// </summary>
        /// <returns>task</returns>
        [Test]
        public async Task TestSEEAsync()
        {
            GameObject gameObject = new("BranchCity");
            BranchCity branchCity = gameObject.AddComponent<BranchCity>();
            branchCity.Date = "2025/01/01";

            branchCity.DataProvider = new SingleGraphPipelineProvider();

            GitBranchesGraphProvider provider = new();
            provider.GitRepository.RepositoryPath.Path = Filenames.OnCurrentPlatform(DataPath.ProjectFolder());
            provider.GitRepository.VCSFilter.Branches = new HashSet<string>() { "origin/875*" };
            provider.GitRepository.VCSFilter.RepositoryPaths = new string[] { "Assets/SEE/GraphProviders" };
            provider.GitRepository.VCSFilter.Globbing = new() { { "**/*.cs", true } };
            provider.SimplifyGraph = true;
            provider.CombineAuthors = false;

            branchCity.DataProvider.Add(provider);
            Graph graph = await branchCity.DataProvider.ProvideAsync(new Graph(), branchCity);
            Assert.That(graph.NodeCount, Is.GreaterThan(0));
        }
    }
}
