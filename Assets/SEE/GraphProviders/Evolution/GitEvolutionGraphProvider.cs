using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GraphProviders.VCS;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders.Evolution
{
    /// <summary>
    /// Provides an evolution for git repositories similar to <see cref="AllGitBranchesSingleGraphProvider"/>.
    /// </summary>
    [Serializable]
    public class GitEvolutionGraphProvider : MultiGraphProvider
    {
        /// <summary>
        /// The git repository which should be analyzed.
        /// </summary>
        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository"), RuntimeTab("Data")]
        public GitRepository GitRepository = new();

        /// <summary>
        /// The date limit until commits should be analyzed.
        /// </summary>
        [InspectorName("Date Limit"),
         Tooltip("The date until commits should be analyzed (DD-MM-YYYY)"), RuntimeTab(GraphProviderFoldoutGroup)]
        public string Date = "";

        /// <summary>
        /// Specifies if the resulting graph should be simplified.
        /// This means that directories which only contains other directories will be combined to safe space in the code city.
        /// </summary>
        [Tooltip("If true, chains in the hierarchy will be simplified."), RuntimeTab(GraphProviderFoldoutGroup)]
        public bool SimplifyGraph;

        /// <summary>
        /// Provides the evolution graph of the git repository.
        ///
        /// This provider will run the calculations on the thread pool.
        /// This can be canceled with <paramref name="token"/>.
        /// </summary>
        /// <param name="graph">The graph series of the previous provider. Will most likely be empty.</param>
        /// <param name="city">The city where the graph series should be displayed.</param>
        /// <param name="changePercentage">Can be used to update the spinner.</param>
        /// <param name="token">CancellationToken to cancel the async operation.</param>
        /// <returns>The resulted graph series.</returns>
        public override async UniTask<List<Graph>> ProvideAsync(List<Graph> graph, AbstractSEECity city,
            Action<float> changePercentage = null,
            CancellationToken token = default)
        {
            CheckAttributes();
            return await UniTask.RunOnThreadPool(() => GetGraph(graph, changePercentage), cancellationToken: token);
        }

        /// <summary>
        /// Checks if all attributes are set correctly.
        /// Otherwise, an exception is thrown.
        /// </summary>
        /// <exception cref="ArgumentException">If one attribute is not set correctly.</exception>
        private void CheckAttributes()
        {
            if (Date == "" || !DateTime.TryParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
            {
                throw new ArgumentException("Date is not set or can't be parsed");
            }

            if (GitRepository.RepositoryPath.Path == "" || !Directory.Exists(GitRepository.RepositoryPath.Path) ||
                !Repository.IsValid(GitRepository.RepositoryPath.Path))
            {
                throw new ArgumentException("Repository path is not set or does not point to a valid git repository");
            }
        }

        /// <summary>
        /// Calculates and returns the actual graph series.
        ///
        /// </summary>
        /// <param name="graph">The input graph series.</param>
        /// <param name="changePercentage">Keeps track of the current progess.</param>
        /// <returns>The calculated graph series.</returns>
        private List<Graph> GetGraph(List<Graph> graph, Action<float> changePercentage)
        {
            DateTime timeLimit = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            Matcher matcher = new();

            foreach (KeyValuePair<string, bool> pattern in GitRepository.PathGlobbing)
            {
                if (pattern.Value)
                {
                    matcher.AddInclude(pattern.Key);
                }
                else
                {
                    matcher.AddExclude(pattern.Key);
                }
            }

            string[] pathSegments = GitRepository.RepositoryPath.Path.Split(Path.DirectorySeparatorChar);
            string repositoryName = pathSegments[^1];

            using (Repository repo = new(GitRepository.RepositoryPath.Path))
            {
                List<Commit> commitList = repo.Commits
                    .QueryBy(new CommitFilter
                    { IncludeReachableFrom = repo.Branches, SortBy = CommitSortStrategies.None })
                    .Where(commit => DateTime.Compare(commit.Author.When.Date, timeLimit) > 0)
                    .Where(commit => commit.Parents.Count() <= 1)
                    .Reverse()
                    .ToList();
                changePercentage(0.1f);

                Dictionary<Commit, Patch> commitChanges =
                    commitList.ToDictionary(commit => commit, commit => GetFileChanges(commit, repo));
                changePercentage(0.2f);

                int counter = 0;
                int commitLength = commitChanges.Where(x =>
                    x.Value.Any(y =>
                        matcher.Match(y.Path).HasMatches)).Count();

                IList<string> files = repo.Branches
                    .SelectMany(x => VCSGraphProvider.ListTree(x.Tip.Tree))
                    .Distinct().ToList();

                // iterate over all commits where at least one file with a file extension in includedFiles is present
                foreach (KeyValuePair<Commit, Patch> currentCommit in
                         commitChanges.Where(x =>
                             x.Value.Any(y =>
                                 matcher.Match(y.Path).HasMatches)))
                {
                    changePercentage?.Invoke(Mathf.Clamp((float)counter / commitLength, 0.2f, 0.98f));

                    // All commits between the first commit in commitList and the current commit
                    List<Commit> commitsInBetween =
                        commitList.GetRange(0, commitList.FindIndex(x => x.Sha == currentCommit.Key.Sha) + 1);

                    graph.Add(GetGraphOfCommit(repositoryName, currentCommit.Key, commitsInBetween,
                        commitChanges,
                        repo, files));
                    counter++;
                }
            }

            return graph;
        }

        /// <summary>
        /// Returns one evolution step of a commit (<paramref name="currentCommit"/>).
        ///
        /// This graph represents all commits between the setted time limit in <see cref="Date"/> and <paramref name="currentCommit"/>.
        /// </summary>
        /// <param name="repoName">The name of the git repository.</param>
        /// <param name="currentCommit">The current commit to generate the graph.</param>
        /// <param name="commitsInBetween">All commits in between these two points.</param>
        /// <param name="commitChanges">All changes made by all commits within the evolution range.</param>
        /// <param name="repo">The git repository in which the commit was made.</param>
        /// <param name="files">A List of all files in the git repository.</param>
        /// <returns>The graoh of the evolution step.</returns>
        private Graph GetGraphOfCommit(string repoName, Commit currentCommit, List<Commit> commitsInBetween,
            IDictionary<Commit, Patch> commitChanges, Repository repo, IList<string> files)
        {
            Graph g = new(GitRepository.RepositoryPath.Path)
            {
                BasePath = GitRepository.RepositoryPath.Path
            };
            GraphUtils.NewNode(g, repoName + "-Evo", DataModel.DG.VCS.RepositoryType, repoName + "-Evo");

            g.StringAttributes.Add("CommitTimestamp", currentCommit.Author.When.Date.ToString("dd/MM/yyy"));
            g.StringAttributes.Add("CommitId", currentCommit.Sha);

            GitFileMetricProcessor metricProcessor = new(repo, GitRepository.PathGlobbing, files);

            foreach (Commit commitInBetween in commitsInBetween)
            {
                metricProcessor.ProcessCommit(commitInBetween, commitChanges[commitInBetween]);
            }

            metricProcessor.CalculateTruckFactor();

            GitFileMetricsGraphGenerator.FillGraphWithGitMetrics(metricProcessor, g, repoName, SimplifyGraph,
                idSuffix: "-Evo");
            return g;
        }

        /// <summary>
        /// Returns all changed files in a commit.
        /// </summary>
        /// <param name="commit">The commit which files should be returned.</param>
        /// <param name="repo">The git repository in which the commit was made.</param>
        /// <returns>A list of all changed files (<see cref="PatchEntryChanges"/>).</returns>
        private static Patch GetFileChanges(Commit commit, Repository repo)
        {
            if (commit.Parents.Any())
            {
                return repo.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);
            }

            return repo.Diff.Compare<Patch>(null, commit.Tree);
        }

        /// <summary>
        /// Returns the kind of this provider.
        /// </summary>
        /// <returns>Returns <see cref="MultiGraphProviderKind.GitEvolution"/>.</returns>
        public override MultiGraphProviderKind GetKind() => MultiGraphProviderKind.GitEvolution;

        #region Config IO

        /// <summary>
        /// The label for <see cref="Date"/> in the configuration file.
        /// </summary>
        private const string dateLabel = "Date";

        /// <summary>
        /// The label for <see cref="GitRepository"/> in the configuration file.
        /// </summary>
        private const string gitRepositoryLabel = "Repository";

        /// <summary>
        /// Saves the attributes of this provider to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to save the attributes to.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            GitRepository.Save(writer, gitRepositoryLabel);
            writer.Save(Date, dateLabel);
        }

        /// <summary>
        /// Restores the attributes of this provider from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes to restore from.</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            GitRepository.Restore(attributes, gitRepositoryLabel);
            ConfigIO.Restore(attributes, dateLabel, ref Date);
        }
        #endregion Config IO
    }
}
