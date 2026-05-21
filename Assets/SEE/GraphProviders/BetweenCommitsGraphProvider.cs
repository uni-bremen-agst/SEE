using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading;
using SEE.VCS;
using SEE.GraphProviders.VCS;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Creates a graph based on the content of a version control system
    /// between two commits.
    /// Nodes represent directories and files. Their nesting corresponds to
    /// the directory structure of the repository. Files are leaf nodes.
    /// File nodes contain metrics that can be gathered based on a simple
    /// lexical analysis, such as Halstead, McCabe and lines of code, as
    /// well as from the version control system, such as number of developers,
    /// number of commits, or code churn.
    /// </summary>
    internal class BetweenCommitsGraphProvider : GitGraphProvider
    {
        /// <summary>
        /// The commit id.
        /// </summary>
        [ShowInInspector, Tooltip("The commit id for which to generate the graph."), HideReferenceObjectPicker]
        public string CommitID = string.Empty;

        /// <summary>
        /// The commit id of the baseline. The VCS metrics will be gathered for the time
        /// between <see cref="BaselineCommitID"/> and <see cref="CommitID"/>.
        /// If <see cref="BaselineCommitID"/> is null or empty, no VCS metrics are gathered.
        /// </summary>
        [ShowInInspector, Tooltip("VCS metrics will be gathered relative to this commit id. If undefined, no VCS metrics will be gathered."),
            HideReferenceObjectPicker]
        public string BaselineCommitID = string.Empty;

        /// <summary>
        /// <inheritdoc cref="GraphProvider.GetKind"/>.
        /// </summary>
        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.VCS;
        }

        /// <summary>
        /// Loads the metrics and nodes from <see cref="GitRepository"/> and
        /// <see cref="CommitID"/> into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded.</param>
        /// <param name="city">This parameter is currently ignored.</param>
        /// <param name="changePercentage">Callback to report progress from 0 to 1.</param>
        /// <param name="token">Cancellation token.</param>
        public override async UniTask<Graph> ProvideAsync
            (Graph graph,
             AbstractSEECity city,
             Action<float> changePercentage = null,
             CancellationToken token = default)
        {
            CheckArguments(city);
            return await UniTask.FromResult<Graph>(GitGraphGenerator.AddNodesForCommit
                                                      (graph, SimplifyGraph, GitRepository, CommitID, BaselineCommitID,
                                                       CombineAuthors, AuthorAliasMap,
                                                       changePercentage, token));
        }

        /// <summary>
        /// Checks whether the assumptions on <see cref="RepositoryPath"/> and
        /// <see cref="CommitID"/> and <paramref name="city"/> hold.
        /// If not, exceptions are thrown accordingly.
        /// </summary>
        /// <param name="city">To be checked.</param>
        /// <exception cref="ArgumentException">Thrown in case <see cref="RepositoryPath"/>,
        /// or <see cref="CommitID"/>
        /// is undefined or does not exist or <paramref name="city"/> is null.</exception>
        protected void CheckArguments(AbstractSEECity city)
        {
            if (GitRepository == null)
            {
                throw new ArgumentException("GitRepository is null.\n");
            }
            if (string.IsNullOrEmpty(GitRepository.RepositoryPath.Path))
            {
                throw new ArgumentException("Empty repository path.\n");
            }
            if (!Directory.Exists(GitRepository.RepositoryPath.Path))
            {
                throw new ArgumentException($"Directory {GitRepository.RepositoryPath.Path} does not exist.\n");
            }
            if (string.IsNullOrEmpty(CommitID))
            {
                throw new ArgumentException("Empty CommitID.\n");
            }
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
        }

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="CommitID"/> in the configuration file.
        /// </summary>
        private const string commitIDLabel = "CommitID";
        /// <summary>
        /// Label of attribute <see cref="BaselineCommitID"/> in the configuration file.
        /// </summary>
        private const string baselineCommitIDLabel = "BaselineCommitID";

        /// <summary>
        /// <inheritdoc cref="GraphProvider.SaveAttributes(ConfigWriter)"/>.
        /// </summary>
        protected override void SaveAttributes(ConfigWriter writer)
        {
            base.SaveAttributes(writer);
            writer.Save(CommitID, commitIDLabel);
            writer.Save(BaselineCommitID, baselineCommitIDLabel);
        }

        /// <summary>
        /// <inheritdoc cref="GraphProvider.RestoreAttributes(Dictionary{string, object})"/>.
        /// </summary>
        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            base.RestoreAttributes(attributes);
            ConfigIO.Restore(attributes, commitIDLabel, ref CommitID);
            ConfigIO.Restore(attributes, baselineCommitIDLabel, ref BaselineCommitID);
        }

        #endregion
    }
}
