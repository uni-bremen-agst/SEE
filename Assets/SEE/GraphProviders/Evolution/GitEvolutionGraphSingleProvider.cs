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
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders
{
    [Serializable]
    public class GitEvolutionGraphProvider : MultiGraphProvider
    {
        #region Constants

        private const string NumberOfAuthorsMetricName = "Metric.Authors.Number";

        private const string NumberOfCommitsMetricName = "Metric.File.Commits";

        private const string TruckFactorMetricName = "Metric.File.TruckFactor";

        #endregion

        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository"), RuntimeTab("Data")]
        public GitRepository Repository = new();

        /// <summary>
        /// The date limit until commits should be analysed
        /// </summary>
        [OdinSerialize]
        [ShowInInspector, InspectorName("Date Limit"),
         Tooltip("The date until commits should be analysed (DD-MM-YYYY)"), RuntimeTab(GraphProviderFoldoutGroup)]
        public string Date = "";

        public override UniTask<List<Graph>> ProvideAsync(List<Graph> graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default) =>
            UniTask.RunOnThreadPool(() => GetGraph(graph), cancellationToken: token);


        private List<Graph> GetGraph(List<Graph> graph)
        {
            DateTime timeLimit = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            IEnumerable<string> includedFiles = Repository.PathGlobbing
                .Where(path => path.Value == true)
                .Select(path => path.Key);

            IEnumerable<string> excludedFiles = Repository.PathGlobbing
                .Where(path => path.Value == false)
                .Select(path => path.Key);

            string[] pathSegments = Repository.RepositoryPath.Path.Split(Path.DirectorySeparatorChar);
            string repositoryName = pathSegments[^1];

            using (var repo = new Repository(Repository.RepositoryPath.Path))
            {
                List<Commit> commitList = repo.Commits
                    .QueryBy(new CommitFilter { IncludeReachableFrom = repo.Branches })
                    .Where(commit => DateTime.Compare(commit.Author.When.Date, timeLimit) > 0)
                    .Where(commit => commit.Parents.Count() == 1)
                    .Reverse()
                    .ToList();

                Dictionary<Commit, List<PatchEntryChanges>> commitChanges = new();
                foreach (var commit in commitList)
                {
                    commitChanges.Add(commit, GetFileChanges(commit, repo));
                }

                foreach (var currentCommit in commitList)
                {
                    // All commits between the first commit in commitList and the current commit
                    List<Commit> commitsInBetween =
                        commitList.GetRange(0, commitList.FindIndex(x => x.Sha == currentCommit.Sha) + 1);

                    graph.Add(GetGraphOfCommit(repositoryName, currentCommit, commitsInBetween,
                        commitChanges,
                        includedFiles, excludedFiles));
                }
            }

            return graph;
        }

        /// <summary>
        /// Returns one evolution step of a commit (<paramref name="currentCommit"/>).
        ///
        /// This graph represents all commits between the setted time limit in <see cref="Date"/> and <paramref name="currentCommit"/>
        /// </summary>
        /// <param name="repoName">The name of the git repository</param>
        /// <param name="currentCommit">The current commit to generate the graph</param>
        /// <param name="commitsInBetween">All commits in between these two points</param>
        /// <param name="commitChanges">All changes made by all commits within the evolution range</param>
        /// <param name="includedFiles">All included file extensions</param>
        /// <param name="excludedFiles">All excluded file extensions</param>
        /// <returns>The graoh of the evolution step</returns>
        private Graph GetGraphOfCommit(string repoName, Commit currentCommit, List<Commit> commitsInBetween,
            Dictionary<Commit, List<PatchEntryChanges>> commitChanges, IEnumerable<string> includedFiles,
            IEnumerable<string> excludedFiles)
        {
            Graph g = new Graph(Repository.RepositoryPath.Path);
            g.BasePath = Repository.RepositoryPath.Path;
            Node rootNode = GraphUtils.NewNode(g, repoName + "-Evo", "Repository", repoName + "-Evo");

            g.StringAttributes.Add("CommitTimestamp", currentCommit.Author.When.Date.ToString("dd/MM/yyy"));
            g.StringAttributes.Add("CommitId", currentCommit.Sha);

            Dictionary<string, GitFileMetricsCollector> fileMetrics = new();

            foreach (var commitInBetween in commitsInBetween)
            {
                foreach (var changedFile in commitChanges[commitInBetween])
                {
                    string filePath = changedFile.Path;
                    if (!includedFiles.Contains(Path.GetExtension(filePath)) ||
                        excludedFiles.Contains(Path.GetExtension(filePath)))
                    {
                        continue;
                    }


                    GitFileMetricsCollector metricsCollector =
                        fileMetrics.GetOrAdd(filePath, new GitFileMetricsCollector());
                    metricsCollector.NumberOfCommits += 1;
                    metricsCollector.Authors.Add(currentCommit.Author.Email);
                    metricsCollector.Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                    metricsCollector.AuthorsChurn.GetOrAdd(currentCommit.Author.Email, 0);
                    metricsCollector.AuthorsChurn[currentCommit.Author.Email] +=
                        (changedFile.LinesAdded + changedFile.LinesDeleted);
                }
            }

            foreach (var file in fileMetrics)
            {
                file.Value.TruckFactor = CalculateTruckFactor(file.Value.AuthorsChurn);
            }

            foreach (var file in fileMetrics)
            {
                Node n = GraphUtils.GetOrAddNode(file.Key, rootNode, g, idSuffix: "-evo");
                n.SetInt(NumberOfAuthorsMetricName, file.Value.Authors.Count);
                n.SetInt(NumberOfCommitsMetricName, file.Value.NumberOfCommits);
                n.SetInt("Metric.File.Churn", file.Value.Churn);
                n.SetInt(TruckFactorMetricName, file.Value.TruckFactor);
            }

            return g;
        }

        /// <summary>
        /// Calculates the truck factor with a LOC-based heuristic by Yamashita et al.
        /// The truck factor are the amount of core-developers which are responsible for 80% of the code (changes). 
        ///
        /// The algorithm was described by. Ferreira et. al
        ///
        /// Soruce/Math: https://doi.org/10.1145/2804360.2804366, https://doi.org/10.1007/s11219-019-09457-2
        /// </summary>
        /// <returns>The calculated truck-factor</returns>
        private static int CalculateTruckFactor(Dictionary<string, int> developersChurn)
        {
            if (!developersChurn.Any())
                return 0;
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
            while (cumulativeRatio <= 0.8f)
            {
                string dev = sortedDevs.First();
                float devRatio = (float)developersChurn[dev] / totalChurn;
                cumulativeRatio += devRatio;
                coreDevs.Add(dev);
                sortedDevs.Remove(dev);
            }

            return coreDevs.Count;
        }

        /// <summary>
        /// Returns all changed files in a commit.
        /// </summary>
        /// <param name="commit">The commit which files should be returned</param>
        /// <param name="repo">The repo</param>
        /// <returns>A list of all changed files (<see cref="PatchEntryChanges"/>)</returns>
        private static List<PatchEntryChanges> GetFileChanges(Commit commit, Repository repo) => repo.Diff
            .Compare<Patch>(commit.Tree, commit.Parents.First().Tree).Select(x => x).ToList();


        public override MultiGraphProviderKind GetKind()
        {
            return MultiGraphProviderKind.GitEvolution;
        }

        protected override void SaveAttributes(ConfigWriter writer)
        {
            Repository.RepositoryPath.Save(writer, "RepositoryPath");
            Dictionary<string, bool> pathGlobbing = string.IsNullOrEmpty(Repository.PathGlobbing.ToString())
                ? null
                : Repository.PathGlobbing;
            writer.Save(pathGlobbing, "PathGlobing");
            writer.Save(Date, "Date");
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            Repository.RepositoryPath.Restore(attributes, "RepositoryPath");
            ConfigIO.Restore(attributes, "PathGlobing", ref Repository.PathGlobbing);
            ConfigIO.Restore(attributes, "Date", ref Date);
        }
    }
}