using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GraphProviders.VCS;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// This provider analyses all branches of a given git repository specified in
    /// <see cref="VCSCity.VCSPath"/> within the given time range (<see cref="BranchCity.Date"/>).
    ///
    /// This provider will collect all commits from the latest to the last one
    /// before <see cref="BranchCity.Date"/>.
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
    public class AllGitBranchesSingleGraphProvider : SingleGraphProvider
    {
        #region Attributes

        /// <summary>
        /// The list of file globbings for file inclusion/exclusion.
        /// The key is the globbing pattern and the value is the inclusion status.
        /// If the latter is true, the pattern is included, otherwise it is excluded.
        /// </summary>
        /// <remarks>We use <see cref="Dictionary{TKey, TValue}"/> rather than
        /// <see cref="IDictionary{TKey, TValue}"/> because otherwise our config I/O
        /// would not work.</remarks>
        [OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
        Tooltip("Path globbings and whether they are inclusive (true) or exclusive (false)."),
         RuntimeTab(GraphProviderFoldoutGroup),
         HideReferenceObjectPicker]
        public Dictionary<string, bool> PathGlobbing = new()
        {
            { "**/*", true }
        };

        /// <summary>
        /// This option fill simplify the graph with <see cref="GitFileMetricsGraphGenerator.SimplifyGraph"/>
        /// and combine directories.
        /// </summary>
        [Tooltip("If true, chains in the hierarchy will be simplified.")]
        public bool SimplifyGraph = false;

        /// <summary>
        /// Specifies if SEE should automatically fetch for new commits in the
        /// repository <see cref="RepositoryData"/>.
        ///
        /// This will append the path of this repo to <see cref="GitPoller"/>.
        ///
        /// Note: the repository must be fetch-able without any credentials
        /// since we can't store them securely yet.
        /// </summary>
        [Tooltip("If true, the repository will be polled regularly for new changes")]
        public bool AutoFetch = false;

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

            if (branchCity.VCSPath.Path.IsNullOrWhitespace() || !Directory.Exists(branchCity.VCSPath.Path))
            {
                throw new ArgumentException("Repository path is not set or does not exists");
            }
        }

        /// <summary>
        /// Returns or adds the <see cref="GitPoller"/> component to the current gameobject/code city <paramref name="city"/>.
        /// </summary>
        /// <param name="city">The code city where the <see cref="GitPoller"/> component should be attached.</param>
        /// <returns>The <see cref="GitPoller"/> component</returns>
        private GitPoller GetOrAddGitPollerComponent(SEECity city)
        {
            if (city.TryGetComponent(out GitPoller poller))
            {
                return poller;
            }

            GitPoller newPoller = city.gameObject.AddComponent<GitPoller>();
            newPoller.CodeCity = city;
            newPoller.PollingInterval = PollingInterval;
            newPoller.MarkerTime = MarkerTime;
            return newPoller;
        }

        /// <summary>
        /// Provides the graph of the git history.
        /// </summary>
        /// <param name="graph">The graph of the previous provider.</param>
        /// <param name="city">The city where the graph should be displayed.</param>
        /// <param name="changePercentage">The current status of the process.</param>
        /// <param name="token">Can be used to cancel the action.</param>
        /// <returns>The graph generated from the git repository <see cref="RepositoryData"/>.</returns>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
        {
            if (city is not BranchCity branchCity)
            {
                throw new ArgumentException($"A {nameof(AllGitBranchesSingleGraphProvider)} works only for a {nameof(BranchCity)}.");
            }

            CheckAttributes(branchCity);

            Graph task = await UniTask.RunOnThreadPool(() => GetGraph(graph, changePercentage, branchCity),
                                                       cancellationToken: token);

            // Only add the poller when in play mode.
            if (AutoFetch && Application.isPlaying)
            {
                GitPoller poller = GetOrAddGitPollerComponent(branchCity);
                poller.WatchedRepositories.Add(branchCity.VCSPath.Path);
            }

            return task;
        }

        /// <summary>
        /// Calculates and returns the actual graph.
        ///
        /// This method will collect all commit from all branches which are not older than <see cref="Date"/>.
        /// Then from all these commits the metrics are calculated with
        /// <see cref="GitFileMetricProcessor.ProcessCommit(LibGit2Sharp.Commit,LibGit2Sharp.Patch)"/>.
        /// </summary>
        /// <param name="graph">The input graph.</param>
        /// <param name="changePercentage">The current status of the process.</param>
        /// <param name="branchCity">The <see cref="BranchCity"/> from which the provider was called.</param>
        /// <returns>The generated output graph.</returns>
        private Graph GetGraph(Graph graph, Action<float> changePercentage, BranchCity branchCity)
        {
            // Note: repositoryPath is platform dependent.
            string repositoryPath = branchCity.VCSPath.Path;
            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                throw new Exception("The repository path is not set.");
            }

            if (!Directory.Exists(repositoryPath))
            {
                throw new Exception("The repository path does not exist or is not a directory.");
            }

            graph.BasePath = repositoryPath;

            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);

            GraphUtils.NewNode(graph, repositoryName, DataModel.DG.VCS.RepositoryType, repositoryName);

            // Assuming that CheckAttributes() was already executed so that the date string is neither empty nor malformed.
            DateTime timeLimit = SEEDate.ToDate(branchCity.Date);

            using (Repository repo = new(graph.BasePath))
            {
                CommitFilter filter;

                filter = new CommitFilter { IncludeReachableFrom = repo.Branches };

                IEnumerable<Commit> commitList = repo.Commits
                    .QueryBy(filter)
                    .Where(commit =>
                        DateTime.Compare(commit.Author.When.Date, timeLimit) > 0)
                    // Filter out merge commits.
                    .Where(commit => commit.Parents.Count() <= 1);

                // Select all files of this repo.
                IEnumerable<string> files = repo.Branches
                    .SelectMany(x => VCSGraphProvider.ListTree(x.Tip.Tree))
                    .Distinct();

                GitFileMetricProcessor metricProcessor = new(repo, PathGlobbing, files, branchCity.CombineAuthors, branchCity.AuthorAliasMap);

                int counter = 0;
                int commitLength = commitList.Count();
                foreach (Commit commit in commitList)
                {
                    metricProcessor.ProcessCommit(commit);
                    changePercentage?.Invoke(Mathf.Clamp((float)counter / commitLength, 0, 0.98f));
                    counter++;
                }

                metricProcessor.CalculateTruckFactor();
                GitFileMetricsGraphGenerator.FillGraphWithGitMetrics(metricProcessor, graph, repositoryName,
                    SimplifyGraph);
                changePercentage(1f);
            }

            graph.FinalizeNodeHierarchy();
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
        /// Label for serializing the <see cref="PathGlobbing"/> field.
        /// </summary>
        private const string pathGlobbingLabel = "PathGlobbing";

        /// <summary>
        /// Label for serializing the <see cref="SimplifyGraph"/> field.
        /// </summary>
        private const string simplifyGraphLabel = "SimplifyGraph";

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
            writer.Save(PathGlobbing, pathGlobbingLabel);
            writer.Save(SimplifyGraph, simplifyGraphLabel);
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
            ConfigIO.Restore(attributes, pathGlobbingLabel, ref PathGlobbing);
            ConfigIO.Restore(attributes, simplifyGraphLabel, ref SimplifyGraph);
            ConfigIO.Restore(attributes, autoFetchLabel, ref AutoFetch);
            ConfigIO.Restore(attributes, pollingIntervalLabel, ref PollingInterval);
            ConfigIO.Restore(attributes, markerTimeLabel, ref MarkerTime);
        }

        #endregion Config I/O
    }
}
