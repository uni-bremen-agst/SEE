using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO.Git;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// This provider analyses all branches of the given git repository specified in <see cref="RepositoryData"/> within the given time range (<see cref="Date"/>).
    ///
    /// This provider will collect all commits from the latest to the last one before <see cref="Date"/>.
    ///
    /// The collected metrics are:
    /// <list type="bullet">
    /// <item>Metric.File.Commits</item>
    /// <item>Metric.File.AuthorsNumber</item>
    /// <item>Metric.File.Churn</item>
    /// <item>Metric.File.CoreDevs</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class AllGitBranchesSingleGraphProvider : SingleGraphProvider
    {
        /// <summary>
        /// The date limit until commits should be analysed
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, InspectorName("Date Limit"),
         Tooltip("The date until commits should be analysed (DD/MM/YYYY)"), RuntimeTab(GraphProviderFoldoutGroup)]
        public string Date = "";

        /// <summary>
        /// The repository from where the data should be fetched
        /// </summary>
        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository"), RuntimeTab("Data")]
        public GitRepository RepositoryData = new();

        /// <summary>
        /// This option fill simplify the graph with <see cref="GitFileMetricsGraphGenerator.DoSimplyfiGraph"/> and combine directories.
        /// </summary>
        [OdinSerialize]
        [ShowInInspector]
        public bool SimplifyGraph { get; set; }

        /// <summary>
        /// Specifies if SEE should automatically fetch for new commits in the repository <see cref="RepositoryData"/>.
        ///
        /// This will append the path of this repo to <see cref="GitPoller"/>.
        ///
        /// Note: the repository must be fetch-able without any credentials since we cant store them securely yet.
        /// </summary>
        [OdinSerialize]
        [ShowInInspector]
        public bool AutoFetch { get; set; }

        private void CheckAttributes()
        {
            if (Date == "" || !DateTime.TryParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
            {
                throw new ArgumentException("Date is not set or cant be parsed");
            }

            if (RepositoryData.RepositoryPath.Path == "" || !Directory.Exists(RepositoryData.RepositoryPath.Path))
            {
                throw new ArgumentException("Repository path is not set or does not exists");
            }
        }

        /// <summary>
        /// Returns or adds the <see cref="GitPoller"/> component to the current gameobject/code city <paramref name="city"/>. 
        /// </summary>
        /// <param name="city">The code city where the <see cref="GitPoller"/> component should be attached.</param>
        /// <returns>The <see cref="GitPoller"/> component</returns>
        private static GitPoller GetOrAddGitPollerComponent(AbstractSEECity city)
        {
            if (city.TryGetComponent(out GitPoller poller))
            {
                return poller;
            }

            GitPoller newPoller = city.gameObject.AddComponent<GitPoller>();
            newPoller.CodeCity = (SEECity)city;
            return newPoller;
        }

        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
        {
            CheckAttributes();

            var task = await UniTask.RunOnThreadPool(() => GetGraph(graph), cancellationToken: token);
            if (AutoFetch)
            {
                GitPoller poller = GetOrAddGitPollerComponent(city);
                poller.WatchedRepositories.Add(RepositoryData.RepositoryPath.Path);
            }

            return task;
        }

        /// <summary>
        /// Calculates and returns the actual graph
        /// </summary>
        /// <param name="graph">The input graph</param>
        /// <returns>The generated output graph</returns>
        private Graph GetGraph(Graph graph)
        {
            graph.BasePath = RepositoryData.RepositoryPath.Path;
            string[] pathSegments = RepositoryData.RepositoryPath.Path.Split(Path.DirectorySeparatorChar);

            string repositoryName = pathSegments[^1];

            GraphUtils.NewNode(graph, repositoryName, "Repository", pathSegments[^1]);

            DateTime timeLimit = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            IEnumerable<string> includedFiles = RepositoryData.PathGlobbing
                .Where(path => path.Value)
                .Select(path => path.Key);

            IEnumerable<string> excludedFiles = RepositoryData.PathGlobbing
                .Where(path => !path.Value)
                .Select(path => path.Key);

            using (var repo = new Repository(RepositoryData.RepositoryPath.Path))
            {
                IEnumerable<Commit> commitList = repo.Commits
                    .QueryBy(new CommitFilter { IncludeReachableFrom = repo.Branches })
                    .Where(commit => DateTime.Compare(commit.Author.When.Date, timeLimit) > 0)
                    // Filter out merge commits
                    .Where(commit => commit.Parents.Count() == 1);

                GitFileMetricRepository metricRepository = new(repo, includedFiles, excludedFiles);

                foreach (var commit in commitList)
                {
                    metricRepository.ProcessCommit(commit);
                }

                metricRepository.CalculateTruckFactor();

                GitFileMetricsGraphGenerator.FillGraphWithGitMetrics(metricRepository, graph, repositoryName,
                    SimplifyGraph);
            }

            return graph;
        }


        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.GitAllBranches;
        }

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Dictionary<string, bool> pathGlobbing = string.IsNullOrEmpty(RepositoryData.PathGlobbing.ToString())
                ? null
                : RepositoryData.PathGlobbing;
            RepositoryData.RepositoryPath.Save(writer, "Path");
            writer.Save(pathGlobbing, pathGlobbingLabel);
            writer.Save(Date, "Date");
            writer.Save(SimplifyGraph, "SimplifyGraph");
            writer.Save(AutoFetch, "AutoFetch");
        }

        private const string pathGlobbingLabel = "PathGlobbing";

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, pathGlobbingLabel, ref RepositoryData.PathGlobbing);
            RepositoryData.RepositoryPath.Restore(attributes, "Path");
            ConfigIO.Restore(attributes, "Date", ref Date);
            var simplifyGraph = SimplifyGraph;
            ConfigIO.Restore(attributes, "SimplifyGraph", ref simplifyGraph);
            SimplifyGraph = simplifyGraph;
            var autoFetch = AutoFetch;
            ConfigIO.Restore(attributes, "AutoFetch", ref autoFetch);
            AutoFetch = autoFetch;
        }
    }
}
