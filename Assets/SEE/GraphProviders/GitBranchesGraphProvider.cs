using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using SEE.VCS;
using System;
using System.IO;
using System.Threading;

namespace SEE.GraphProviders
{
    /// <summary>
    /// This generates nodes and their metrics for all files in all branches
    /// of a given git repository as specified.
    ///
    /// The collected metrics are:
    /// <list type="bullet">
    /// <item>Metric.File.Commits</item>
    /// <item><see cref="DataModel.DG.VCS.NumberOfDevelopers"/></item>
    /// <item><see cref="DataModel.DG.VCS.Churn"/></item>
    /// <item><see cref="DataModel.DG.VCS.TruckNumber"/></item>
    /// </list>
    /// </summary>
    [Serializable]
    internal class GitBranchesGraphProvider : GitGraphProvider
    {
        /// <summary>
        /// Checks if all attributes are set correctly.
        /// Otherwise, an exception is thrown.
        /// </summary>
        /// <param name="branchCity">The <see cref="BranchCity"/> where this provider was executed.</param>
        /// <exception cref="ArgumentException">If one attribute is not set correctly.</exception>
        private void CheckAttributes(BranchCity branchCity)
        {
            if (!SEEDate.IsValid(branchCity.Date))
            {
                throw new ArgumentException($"Date is impossible or cannot be parsed. Expected: {SEEDate.DateFormat} Actual: {branchCity.Date}");
            }
        }

        /// <summary>
        /// Provides the graph of the git history.
        /// </summary>
        /// <param name="graph">The graph of the previous provider.</param>
        /// <param name="city">The city where the graph should be displayed.</param>
        /// <param name="changePercentage">The current status of the process.</param>
        /// <param name="token">Can be used to cancel the action.</param>
        /// <returns>The graph generated from the git repository <see cref="RepositoryData"/>.</returns>
        public override async UniTask<Graph> ProvideAsync
            (Graph graph,
             AbstractSEECity city,
             Action<float> changePercentage = null,
             CancellationToken token = default)
        {
            if (city is not BranchCity branchCity)
            {
                throw new ArgumentException($"A {nameof(GitBranchesGraphProvider)} works only for a {nameof(BranchCity)}.");
            }

            CheckAttributes(branchCity);
            Graph task = await UniTask.RunOnThreadPool(() => GetGraph(graph, changePercentage, branchCity, token),
                                                       cancellationToken: token);
            return task;
        }

        /// <summary>
        /// Calculates and returns the actual graph.
        ///
        /// This method will collect all commits from all branches which are not older than <see cref="Date"/>.
        /// Then from all these commits the metrics are calculated with
        /// <see cref="GitGraphGenerator.ProcessCommit(LibGit2Sharp.Commit,LibGit2Sharp.Patch)"/>.
        /// </summary>
        /// <param name="graph">The input graph.</param>
        /// <param name="changePercentage">The current status of the process.</param>
        /// <param name="branchCity">The <see cref="BranchCity"/> from which the provider was called.</param>
        /// <returns>The generated output graph.</returns>
        private Graph GetGraph(Graph graph, Action<float> changePercentage, BranchCity branchCity, CancellationToken token)
        {
            // Note: repositoryPath is platform dependent.
            string repositoryPath = GitRepository.RepositoryPath.Path;
            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                throw new Exception("The repository path is not set.");
            }

            if (!Directory.Exists(repositoryPath))
            {
                throw new Exception($"The repository path {repositoryPath} does not exist or is not a directory.");
            }

            graph.BasePath = repositoryPath;

            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);

            // We are assuming that CheckAttributes() was already executed so that the date string is
            // neither empty nor malformed.
            DateTime startDate = SEEDate.ToDate(branchCity.Date);

            GitGraphGenerator.AddNodesAfterDate
                (graph, SimplifyGraph, GitRepository, repositoryName, startDate,
                 CombineAuthors, AuthorAliasMap,
                 changePercentage, token);
            changePercentage(1f);

            return graph;
        }

        /// <summary>
        /// Returns the kind of this provider.
        /// </summary>
        /// <returns>Returns <see cref="SingleGraphProviderKind.GitAllBranches"/>.</returns>
        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.GitAllBranches;
        }
    }
}
