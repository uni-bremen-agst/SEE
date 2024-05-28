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
using SEE.GameObjects;
using SEE.GO;
using SEE.UI.Notification;
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
        #region Attributes

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
        [OdinSerialize] [ShowInInspector] public bool SimplifyGraph = false;

        /// <summary>
        /// Specifies if SEE should automatically fetch for new commits in the repository <see cref="RepositoryData"/>.
        ///
        /// This will append the path of this repo to <see cref="GitPoller"/>.
        ///
        /// Note: the repository must be fetch-able without any credentials since we cant store them securely yet.
        /// </summary>
        [OdinSerialize] [ShowInInspector] public bool AutoFetch = false;

        [OdinSerialize, ShowInInspector, EnableIf(nameof(AutoFetch)), Range(5, 200)]
        public int PollingInterval = 5;

        [OdinSerialize, ShowInInspector, EnableIf(nameof(AutoFetch)), Range(5, 200)]
        public int MarkerTime = 10;


        private float progressPercantage = 0f;

        #endregion

        #region Constants

        private const string pathGlobbingLabel = "PathGlobbing";

        private const string dataLabel = "Date";

        private const string pathLabel = "Path";

        private const string simplifyGraphLabel = "SimplifyGraph";

        private const string autoFetchLabel = "AutoFetch";

        #endregion


        #region Methods

        /// <summary>
        /// Checks if all attributes are set correctly.
        /// Otherwise an exception is thrown.
        /// </summary>
        /// <exception cref="ArgumentException">If one attribute is not set correctly</exception>
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
        /// Provides the graph of the git history 
        /// </summary>
        /// <param name="graph">The graph of the previous provider</param>
        /// <param name="city">The city where the graph should be displayed</param>
        /// <param name="changePercentage"></param>
        /// <param name="token">Can be used to cancel the action</param>
        /// <returns>The graph generated from the git repository <see cref="RepositoryData"/></returns>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
        {
            CheckAttributes();

            var task = await UniTask.RunOnThreadPool(() => GetGraph(graph, changePercentage), cancellationToken: token);
            if (AutoFetch)
            {
                if (city is not SEECity seeCity)
                {
                    ShowNotification.Warn("Can't enable auto fetch",
                        "Automatically fetching git repos is only supported in SEECity");
                    return task;
                }
                // Only add the poller when in playing mode
                if (Application.isPlaying)
                {
                    GitPoller poller = GetOrAddGitPollerComponent(seeCity);
                    poller.WatchedRepositories.Add(RepositoryData.RepositoryPath.Path);
                }
            }

            return task;
        }

        /// <summary>
        /// Calculates and returns the actual graph.
        /// 
        /// This method will collect all commit from all branches which are not older than <see cref="Date"/>.
        /// Then from all these commits the metrics are calculated with <see cref="GitFileMetricRepository.ProcessCommit(LibGit2Sharp.Commit,LibGit2Sharp.Patch)"/>
        /// </summary>
        /// <param name="graph">The input graph</param>
        /// <param name="changePercentage"></param>
        /// <returns>The generated output graph</returns>
        private Graph GetGraph(Graph graph, Action<float> changePercentage)
        {
            graph.BasePath = RepositoryData.RepositoryPath.Path;
            string[] pathSegments = RepositoryData.RepositoryPath.Path.Split(Path.DirectorySeparatorChar);

            string repositoryName = pathSegments[^1];

            GraphUtils.NewNode(graph, repositoryName, GraphUtils.RepositoryTypeName, pathSegments[^1]);

            // Assuming that CheckAttributes() was already executed so that the date string is not empty nor malformed.  
            DateTime timeLimit = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            // Analogue to VCSGraphProvider 
            IEnumerable<string> includedFiles = RepositoryData.PathGlobbing
                .Where(path => path.Value)
                .Select(path => path.Key);

            using (var repo = new Repository(RepositoryData.RepositoryPath.Path))
            {
                IEnumerable<Commit> commitList = repo.Commits
                    .QueryBy(new CommitFilter { IncludeReachableFrom = repo.Branches })
                    .Where(commit => DateTime.Compare(commit.Author.When.Date, timeLimit) > 0)
                    // Filter out merge commits
                    .Where(commit => commit.Parents.Count() <= 1);
                GitFileMetricRepository metricRepository = new(repo, includedFiles);

                int counter = 0;
                int commitLength = commitList.Count();
                foreach (var commit in commitList)
                {
                    metricRepository.ProcessCommit(commit);
                    changePercentage?.Invoke(Mathf.Clamp((float)counter / commitLength, 0, 0.98f));
                    counter++;
                }

                //changePercentage(0.5f);


                metricRepository.CalculateTruckFactor();
                //changePercentage(0.75f);
                GitFileMetricsGraphGenerator.FillGraphWithGitMetrics(metricRepository, graph, repositoryName,
                    SimplifyGraph);
                changePercentage(1f);
            }

            return graph;
        }

        /// <summary>
        /// Returns the kind of this provider
        /// </summary>
        /// <returns>Returns <see cref="SingleGraphProviderKind.GitAllBranches"/></returns>
        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.GitAllBranches;
        }

        /// <summary>
        /// Saves the attributes of this provider to <paramref name="writer"/>
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to save the attributes to</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            Dictionary<string, bool> pathGlobbing = string.IsNullOrEmpty(RepositoryData.PathGlobbing.ToString())
                ? null
                : RepositoryData.PathGlobbing;
            RepositoryData.RepositoryPath.Save(writer, pathLabel);
            writer.Save(pathGlobbing, pathGlobbingLabel);
            writer.Save(Date, dataLabel);
            writer.Save(SimplifyGraph, simplifyGraphLabel);
            writer.Save(AutoFetch, autoFetchLabel);
        }

        /// <summary>
        /// Restores the attributes of this provider from <paramref name="attributes"/>
        /// </summary>
        /// <param name="attributes">The attributes to restore from</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, pathGlobbingLabel, ref RepositoryData.PathGlobbing);
            RepositoryData.RepositoryPath.Restore(attributes, pathLabel);
            ConfigIO.Restore(attributes, dataLabel, ref Date);
            var simplifyGraph = SimplifyGraph;
            ConfigIO.Restore(attributes, simplifyGraphLabel, ref simplifyGraph);
            SimplifyGraph = simplifyGraph;
            var autoFetch = AutoFetch;
            ConfigIO.Restore(attributes, autoFetchLabel, ref autoFetch);
            AutoFetch = autoFetch;
        }

        #endregion
    }
}
