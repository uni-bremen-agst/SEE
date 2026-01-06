using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GraphProviders.VCS;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.VCS;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders.Evolution
{
    /// <summary>
    /// Provides an evolution for git repositories similar to <see cref="GitBranchesGraphProvider"/>.
    /// </summary>
    [Serializable]
    public class GitEvolutionGraphProvider : MultiGraphProvider
    {
        /// <summary>
        /// All commits after the date specified in <see cref="Date"/> will be considered.
        /// </summary>
        [InspectorName("Start Date"),
         Tooltip("All commits after the date specified will be considered (" + SEEDate.DateFormat + ")."),
         RuntimeTab(GraphProviderFoldoutGroup)]
        public string Date = "";

        /// <summary>
        /// The git repository which should be analyzed.
        /// </summary>
        [OdinSerialize, ShowInInspector, SerializeReference, HideReferenceObjectPicker,
         Tooltip("The Git repository from which to retrieve the data."),
         ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = "Repository"),
         RuntimeTab("Data")]
        public GitRepository GitRepository = new();

        /// <summary>
        /// Specifies if the resulting graph should be simplified.
        /// This means that directories which only contains other directories will be combined to safe space in the code city.
        /// </summary>
        [Tooltip("If true, chains in the hierarchy will be simplified."),
         RuntimeTab(GraphProviderFoldoutGroup)]
        public bool SimplifyGraph;

        /// <summary>
        /// If this is true, the authors of the commits with similar identities will be combined.
        /// This binding can either be done manually (by specifing the aliases in <see cref="AuthorAliasMap"/>)
        /// or automatically (by setting <see cref="AutoMapAuthors"/> to true).
        /// </summary>
        [Tooltip("If true, the authors of the commits with similar identities will be combined.")]
        public bool CombineAuthors;

        /// <summary>
        /// A dictionary mapping a commit author's identity (<see cref="FileAuthor"/>) to a list of aliases.
        /// This is used to manually group commit authors with similar identities together.
        /// The mapping enables aggregating commit data under a single normalized author identity.
        /// </summary>
        [NonSerialized, OdinSerialize,
         DictionaryDrawerSettings(
              DisplayMode = DictionaryDisplayOptions.CollapsedFoldout,
              KeyLabel = "Author", ValueLabel = "Aliases"),
         Tooltip("Author alias mapping."),
         ShowIf("CombineAuthors"),
         RuntimeShowIf("CombineAuthors"),
         HideReferenceObjectPicker]
        public AuthorMapping AuthorAliasMap = new();

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
            return await UniTask.RunOnThreadPool(() => GetGraph(graph, changePercentage, token), cancellationToken: token);
        }

        /// <summary>
        /// Checks if all attributes are set correctly.
        /// Otherwise, an exception is thrown.
        /// </summary>
        /// <exception cref="ArgumentException">If one attribute is not set correctly.</exception>
        private void CheckAttributes()
        {
            if (!SEEDate.IsValid(Date))
            {
                throw new ArgumentException($"Date {Date} is not a valid date ({SEEDate.DateFormat}).");
            }

            if (GitRepository.RepositoryPath.Path == "" || !Directory.Exists(GitRepository.RepositoryPath.Path) ||
                !Repository.IsValid(GitRepository.RepositoryPath.Path))
            {
                throw new ArgumentException("Repository path is not set or does not point to a valid git repository");
            }
        }

        /// <summary>
        /// Calculates and returns the graph series, one graph for each commit in the git repository.
        /// </summary>
        /// <param name="graph">The input graph series.</param>
        /// <param name="changePercentage">Keeps track of the current progess.</param>
        /// <returns>The calculated graph series.</returns>
        private List<Graph> GetGraph(List<Graph> graph, Action<float> changePercentage, CancellationToken token)
        {
            throw new NotImplementedException("GitEvolutionGraphProvider is not implemented yet.");
            /*
            // This name will be used as the root node of the graph.
            // Its type will be <see cref="DataModel.DG.VCS.RepositoryType"/>.
            // It is the innermost directory of the git repository.
            string repositoryName = Filenames.InnermostDirectoryName(GitRepository.RepositoryPath.Path);

            // The files in the git repository for which nodes should be created.
            HashSet<string> files = GitRepository.AllFiles(token);

            // All commits after this Date will be considered.
            List<Commit> commitsAfterDate = GitRepository.CommitsAfter(SEEDate.ToDate(Date)).Reverse().ToList();
            changePercentage(0.1f);

            token.ThrowIfCancellationRequested();

            // Note from the LibGit2Sharp.Patch documentation:
            // Building a patch is an expensive operation. If you only need to know which files have been added,
            /// deleted, modified, ..., then consider using a simpler <see cref="TreeChanges"/>.

            // A mapping of all considered commits onto their patch.
            Dictionary<Commit, Patch> commitChanges
                = commitsAfterDate.ToDictionary(commit => commit, commit => GitRepository.GetPatchRelativeToParent(commit));
            changePercentage(0.2f);

            // Only those commits that are changing any of the relevant files in the git repository.
            IEnumerable<KeyValuePair<Commit, Patch>> commits
                = commitChanges.Where(x => x.Value.Any(patch => files.Contains(patch.Path)));

            int iteration = 0;
            int commitLength = commits.Count();

            foreach (KeyValuePair<Commit, Patch> currentCommit in commits)
            {
                token.ThrowIfCancellationRequested();

                changePercentage?.Invoke(Mathf.Clamp((float)iteration / commitLength, 0.2f, 0.98f));

                // All commits between the first commit in commitList and the current commit
                List<Commit> commitsInBetween =
                    commitsAfterDate.GetRange(0, commitsAfterDate.FindIndex(x => x.Sha == currentCommit.Key.Sha) + 1);

                graph.Add(GetGraphOfCommit(repositoryName, currentCommit.Key, commitsInBetween,
                                           commitChanges, files));
                iteration++;
            }

            return graph;
            */
        }

        /// <summary>
        /// Returns one evolution step of a commit (<paramref name="currentCommit"/>).
        ///
        /// This graph represents all commits between the setted time limit in <see cref="Date"/> and <paramref name="currentCommit"/>.
        /// </summary>
        /// <param name="repoName">The name of the git repository.</param>
        /// <param name="currentCommit">The current commit to generate the graph.</param>
        /// <param name="commitsInBetween">All commits from the very first commit of the considered
        /// part of the history until <paramref name="currentCommit"/>.</param>
        /// <param name="commitChanges">All changes made by all commits within the evolution range.</param>
        /// <param name="files">The set of files in the git repository to be considered.</param>
        /// <returns>The graph of the evolution step.</returns>
        private Graph GetGraphOfCommit
            (string repoName,
            Commit currentCommit,
            List<Commit> commitsInBetween,
            IDictionary<Commit, Patch> commitChanges,
            HashSet<string> files)
        {
            Graph graph = new(GitRepository.RepositoryPath.Path)
            {
                BasePath = GitRepository.RepositoryPath.Path
            };

            graph.StringAttributes.Add("CommitTimestamp", currentCommit.Author.When.Date.ToString("dd/MM/yyy"));
            graph.StringAttributes.Add("CommitId", currentCommit.Sha);

            GitGraphGenerator.AddNodesForCommits
                (graph, SimplifyGraph, GitRepository, repoName, files, commitsInBetween, commitChanges,
                CombineAuthors, AuthorAliasMap);
            return graph;
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
        /// Label for serializing the <see cref="SimplifyGraph"/> field.
        /// </summary>
        private const string simplifyGraphLabel = "SimplifyGraph";

        /// <summary>
        /// Label of attribute <see cref="CombineAuthors"/> in the configuration file.
        /// </summary>
        private const string combineAuthorsLabel = "CombineAuthors";

        /// <summary>
        /// Label of attribute <see cref="AuthorAliasMap"/> in the configuration file.
        /// </summary>
        private const string authorAliasMapLabel = "AuthorAliasMap";

        /// <summary>
        /// Saves the attributes of this provider to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to save the attributes to.</param>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            GitRepository.Save(writer, gitRepositoryLabel);
            writer.Save(Date, dateLabel);
            writer.Save(SimplifyGraph, simplifyGraphLabel);
            writer.Save(CombineAuthors, combineAuthorsLabel);
            AuthorAliasMap.Save(writer, authorAliasMapLabel);
        }

        /// <summary>
        /// Restores the attributes of this provider from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes to restore from.</param>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            GitRepository.Restore(attributes, gitRepositoryLabel);
            ConfigIO.Restore(attributes, dateLabel, ref Date);
            ConfigIO.Restore(attributes, simplifyGraphLabel, ref SimplifyGraph);
            ConfigIO.Restore(attributes, combineAuthorsLabel, ref CombineAuthors);
            AuthorAliasMap.Restore(attributes, authorAliasMapLabel);
        }
        #endregion Config IO
    }
}
