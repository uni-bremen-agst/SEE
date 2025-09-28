using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.VCS;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

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
        #region Attributes

        /// <summary>
        /// The poller which will regularly fetch the repository for new changes.
        /// </summary>
        private GitPoller poller;

        /// <summary>
        /// Backing field for <see cref="AutoFetch"/>.
        /// </summary>
        [OdinSerialize, NonSerialized, HideInInspector]
        private bool autoFetch = false;

        /// <summary>
        /// Specifies if SEE should automatically fetch for new commits in the
        /// repository <see cref="RepositoryData"/>.
        ///
        /// This will append the path of this repo to <see cref="GitPoller"/>.
        ///
        /// Note: the repository must be fetch-able without any credentials
        /// since we can't store them securely yet.
        /// </summary>
        [ShowInInspector,
            Tooltip("If true, the repository will be polled regularly for new changes.")]
        public bool AutoFetch
        {
            get => autoFetch;
            set
            {
                if (value != autoFetch)
                {
                    autoFetch = value;
                    if (autoFetch)
                    {
                        poller?.Start();
                    }
                    else
                    {
                        poller?.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// The interval in seconds in which git fetch should be called.
        /// </summary>
        [Tooltip("The interval in seconds in which the repository should be polled. Used only if Auto Fetch is true."),
            EnableIf(nameof(AutoFetch)), Range(5, 200)]
        public int PollingInterval = 5;

        /// <summary>
        /// If file changes where picked up by the <see cref="GitPoller"/>, the affected files
        /// will be marked. This field specifies for how long these markers should appear.
        /// </summary>
        [Tooltip(
             "The time in seconds for how long the node markers should be shown for newly added or modified nodes."),
         EnableIf(nameof(AutoFetch)), Range(5, 200)]
        public int MarkerTime = 10;

        #endregion

        #region Methods

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

            if (AutoFetch) // && Application.isPlaying)
            {
                // We can create the poller only now that we know the city.
                poller = new GitPoller
                {
                    MarkerTime = MarkerTime,
                    PollingInterval = PollingInterval,
                    CodeCity = branchCity,
                    Repository = GitRepository
                };
                poller.Start();
            }

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

            // Assuming that CheckAttributes() was already executed so that the date string is neither empty nor malformed.
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

        #endregion

        #region Config I/O

        /// <summary>
        /// Label for serializing the <see cref="AutoFetch"/> field.
        /// </summary>
        private const string autoFetchLabel = "AutoFetch";

        /// <summary>
        /// Label for serializing the <see cref="PollingInterval"/> field.
        /// </summary>
        private const string pollingIntervalLabel = "PollingInterval";

        /// <summary>
        /// Label for serializing the <see cref="MarkerTime"/> field.
        /// </summary>
        private const string markerTimeLabel = "MarkerTime";

        /// <summary>
        /// Saves the attributes of this provider to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to save the attributes to.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            base.SaveAttributes(writer);
            writer.Save(AutoFetch, autoFetchLabel);
            writer.Save(PollingInterval, pollingIntervalLabel);
            writer.Save(MarkerTime, markerTimeLabel);
        }

        /// <summary>
        /// Restores the attributes of this provider from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes to restore from.</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            base.RestoreAttributes(attributes);
            ConfigIO.Restore(attributes, autoFetchLabel, ref autoFetch);
            ConfigIO.Restore(attributes, pollingIntervalLabel, ref PollingInterval);
            ConfigIO.Restore(attributes, markerTimeLabel, ref MarkerTime);
        }

        #endregion Config I/O
    }
}
