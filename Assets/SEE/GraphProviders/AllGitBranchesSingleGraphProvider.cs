using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
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

        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository"), RuntimeTab("Data")]
        public GitRepository RepositoryData = new();

        [OdinSerialize] [ShowInInspector] public int AuthorThreshhold { get; set; } = 1;

        [OdinSerialize] [ShowInInspector] public int CommitThreshhold { get; set; } = 1;
        [OdinSerialize] [ShowInInspector] public bool SimplifyGraph { get; set; }

        /// <summary>
        /// Specifies if SEE should automatically fetch for new commits in the repository <see cref="AllGitBranchesSingleGraphProvider{T}.RepositoryPath"/>.
        ///
        /// This will append the path of this repo to <see cref="GitPoller"/>.
        ///
        /// Note: the repository must be fetch-able without any credentials since we cant store them securely yet.
        /// TODO: Maybe change this in the future 
        /// </summary>
        [OdinSerialize]
        [ShowInInspector]
        public bool AutoFetch { get; set; }

        #region Constants

        private const string NumberOfAuthorsMetricName = "Metric.File.AuthorsNumber";

        private const string NumberOfCommitsMetricName = "Metric.File.Commits";

        private const string NumberOfFileChurnMetricName = "Metric.File.Churn";

        private const string TruckFactorMetricName = "Metric.File.CoreDevs";

        /// <summary>
        /// Used in the calculation of the truck factor.
        ///
        /// Specifies the minimum ratio of the file churn the core devs should be responsible for 
        /// </summary>
        private const float TruckFactorCoreDevRatio = 0.8f;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="city"></param>
        /// <returns></returns>
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

        public async override UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
        {
            var task = await UniTask.RunOnThreadPool(() => GetGraph(graph));
            if (AutoFetch)
            {
                GitPoller poller = GetOrAddGitPollerComponent(city);
                poller.WatchedRepositories.Add(RepositoryData.RepositoryPath.Path);
            }

            return task;
        }

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

                Dictionary<string, GitFileMetricsCollector> fileMetrics = new();

                foreach (var commit in commitList)
                {
                    var changedFilesPath = repo.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);


                    foreach (var changedFile in changedFilesPath)
                    {
                        string filePath = changedFile.Path;
                        if (!includedFiles.Contains(Path.GetExtension(filePath)) ||
                            excludedFiles.Contains(Path.GetExtension(filePath)))
                        {
                            continue;
                        }

                        if (!fileMetrics.ContainsKey(filePath))
                        {
                            fileMetrics.Add(filePath,
                                new GitFileMetricsCollector(1, new HashSet<string> { commit.Author.Email },
                                    changedFile.LinesAdded + changedFile.LinesDeleted));

                            fileMetrics[filePath].AuthorsChurn.Add(commit.Author.Email,
                                changedFile.LinesAdded + changedFile.LinesDeleted);
                        }
                        else
                        {
                            fileMetrics[filePath].NumberOfCommits += 1;
                            fileMetrics[filePath].Authors.Add(commit.Author.Email);
                            fileMetrics[filePath].Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                            fileMetrics[filePath].AuthorsChurn.GetOrAdd(commit.Author.Email, 0);
                            fileMetrics[filePath].AuthorsChurn[commit.Author.Email] +=
                                (changedFile.LinesAdded + changedFile.LinesDeleted);
                        }
                    }
                }

                foreach (var file in fileMetrics)
                {
                    file.Value.TruckFactor = CalculateTruckFactor(file.Value.AuthorsChurn);
                }

                foreach (var file in fileMetrics)
                {
                    Node n = GraphUtils.GetOrAddNode(file.Key, graph.GetNode(repositoryName), graph);
                    n.SetInt(NumberOfAuthorsMetricName, file.Value.Authors.Count);
                    n.SetInt(NumberOfCommitsMetricName, file.Value.NumberOfCommits);
                    n.SetInt(NumberOfFileChurnMetricName, file.Value.Churn);
                    n.SetInt(TruckFactorMetricName, file.Value.TruckFactor);
                }

                if (SimplifyGraph)
                {
                    foreach (var child in graph.GetRoots().First().Children().ToList())
                    {
                        DoSimplyfiGraph(child, graph);
                    }
                }
            }

            return graph;
        }


        /// <summary>
        /// Calculates the truck factor based a LOC-based heuristic by Yamashita et al. for estimating the coreDev set.
        ///
        /// cited by. Ferreira et. al
        ///
        /// Soruce/Math: https://doi.org/10.1145/2804360.2804366, https://doi.org/10.1007/s11219-019-09457-2
        /// </summary>
        /// <returns></returns>
        private static int CalculateTruckFactor(Dictionary<string, int> developersChurn)
        {
            int totalChurn = developersChurn.Select(x => x.Value).Sum();

            HashSet<string> coreDevs = new();

            float cumulativeRatio = 0;
            // Sorting devs by their number of changed files 
            List<string> sortedDevs =
                developersChurn
                    .OrderByDescending(x => x.Value)
                    .Select(x => x.Key)
                    .ToList();
            // Selecting the coreDevs which are responsible for at least 80% of the total churn of a file
            while (cumulativeRatio <= TruckFactorCoreDevRatio)
            {
                string dev = sortedDevs.First();
                float devRatio = (float)developersChurn[dev] / totalChurn;
                cumulativeRatio += devRatio;
                coreDevs.Add(dev);
                sortedDevs.Remove(dev);
            }

            return coreDevs.Count;
        }

        private void DoSimplyfiGraph(Node root, Graph g)
        {
            if (root.Children().ToList().All(x => x.Type != "file"))
            {
                foreach (var child in root.Children().ToList())
                {
                    child.Reparent(root.Parent);
                    DoSimplyfiGraph(child, g);
                }

                if (g.ContainsNode(root))
                {
                    g.RemoveNode(root);
                }
            }
            else
            {
                foreach (var node in root.Children().Where(x => x.Type == "directory").ToList())
                {
                    DoSimplyfiGraph(node, g);
                }
            }
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

    public class GitFileMetricsCollector
    {
        public int NumberOfCommits { get; set; }

        public HashSet<string> Authors { get; set; }

        public Dictionary<string, int> AuthorsChurn { get; set; }

        public int TruckFactor { get; set; }

        /// <summary>
        /// Total sum of changed lines (added and removed)
        /// </summary>
        public int Churn { get; set; }

        public GitFileMetricsCollector()
        {
            Authors = new();
            AuthorsChurn = new();
        }

        public GitFileMetricsCollector(int numberOfCommits, HashSet<string> authors, int churn)
        {
            NumberOfCommits = numberOfCommits;
            Authors = authors;
            Churn = churn;
            AuthorsChurn = new();
            TruckFactor = 0;
        }
    }
}