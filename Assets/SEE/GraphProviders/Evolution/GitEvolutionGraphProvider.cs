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

        [OdinSerialize]
        [ShowInInspector, InspectorName("Date Limit"),
         Tooltip("The date until commits should be analysed (DD-MM-YYYY)"), RuntimeTab(GraphProviderFoldoutGroup)]
        public bool SimplifyGraph;

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

                Dictionary<Commit, Patch> commitChanges = new();
                foreach (var commit in commitList)
                {
                    commitChanges.Add(commit, GetFileChanges(commit, repo));
                }

                foreach (var currentCommit in
                         commitChanges.Where(x =>
                             x.Value.All(y =>
                                 includedFiles.Contains(Path.GetExtension(y.Path)) &&
                                 !excludedFiles.Contains(Path.GetExtension(y.Path)))))
                {
                    // All commits between the first commit in commitList and the current commit
                    List<Commit> commitsInBetween =
                        commitList.GetRange(0, commitList.FindIndex(x => x.Sha == currentCommit.Key.Sha) + 1);

                    graph.Add(GetGraphOfCommit(repositoryName, currentCommit.Key, commitsInBetween,
                        commitChanges,
                        includedFiles, excludedFiles, repo));
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
            IDictionary<Commit, Patch> commitChanges, IEnumerable<string> includedFiles,
            IEnumerable<string> excludedFiles, Repository repo)
        {
            Graph g = new Graph(Repository.RepositoryPath.Path);
            g.BasePath = Repository.RepositoryPath.Path;
            GraphUtils.NewNode(g, repoName + "-Evo", "Repository", repoName + "-Evo");

            g.StringAttributes.Add("CommitTimestamp", currentCommit.Author.When.Date.ToString("dd/MM/yyy"));
            g.StringAttributes.Add("CommitId", currentCommit.Sha);


            GitFileMetricRepository metricRepository = new(repo, includedFiles, excludedFiles);

            foreach (var commitInBetween in commitsInBetween)
            {
                metricRepository.ProcessCommit(commitInBetween, commitChanges[commitInBetween]);
            }

            metricRepository.CalculateTruckFactor();

            GitFileMetricsGraphGenerator.FillGraphWithGitMetrics(metricRepository, g, repoName, SimplifyGraph,
                idSuffix: "-Evo");
            // foreach (var file in fileMetrics)
            // {
            //     Node n = GraphUtils.GetOrAddNode(file.Key, rootNode, g, idSuffix: "-evo");
            //     n.SetInt(NumberOfAuthorsMetricName, file.Value.Authors.Count);
            //     n.SetInt(NumberOfCommitsMetricName, file.Value.NumberOfCommits);
            //     n.SetInt("Metric.File.Churn", file.Value.Churn);
            //     n.SetInt(TruckFactorMetricName, file.Value.TruckFactor);
            // }

            return g;
        }

        /// <summary>
        /// Returns all changed files in a commit.
        /// </summary>
        /// <param name="commit">The commit which files should be returned</param>
        /// <param name="repo">The repo</param>
        /// <returns>A list of all changed files (<see cref="PatchEntryChanges"/>)</returns>
        private static Patch GetFileChanges(Commit commit, Repository repo) => repo.Diff
            .Compare<Patch>(commit.Tree, commit.Parents.First().Tree);


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
