using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Calculates metrics between two revisions from a git repository and adds these to a graph.
    /// </summary>
    [Serializable]
    public class GitGraphProvider : GraphProvider
    {
        /// <summary>
        /// The path to the VCS containing the two revisions to be compared.
        /// </summary>
        private string VCSPath = string.Empty;

        /// <summary>
        /// The older revision that constitutes the baseline of the comparison.
        /// </summary>
        private string OldRevision = string.Empty;

        /// <summary>
        /// The newer revision against which the <see cref="OldRevision"/> is to be compared.
        /// </summary>
        private string NewRevision = string.Empty;

        /// <summary>
        /// Calculates metrics between two revisions from a git repository and adds these to <paramref name="graph"/>.
        /// The resulting graph is returned.
        /// </summary>
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="city">this value is currently ignored</param>
        /// <param name="changePercentage">this value is currently ignored</param>
        /// <param name="token">this value is currently ignored</param>
        /// <returns>the input <paramref name="graph"/> with metrics added</returns>
        /// <exception cref="ArgumentException">thrown in case <see cref="Path"/>
        /// is undefined or does not exist or <paramref name="city"/> is null</exception>
        /// <exception cref="NotImplementedException">thrown in case <paramref name="graph"/> is
        /// null; this is currently not supported.</exception>
        public override UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                    Action<float> changePercentage = null,
                                                    CancellationToken token = default)
        {
            CheckArguments(city);
            if (graph == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                using (Repository repo = new(VCSPath))
                {
                    Commit OldCommit = repo.Lookup<Commit>(OldRevision);
                    Commit NewCommit = repo.Lookup<Commit>(NewRevision);

                    VCSMetrics.AddLineofCodeChurnMetric(graph, VCSPath, OldCommit, NewCommit);
                    VCSMetrics.AddNumberofDevelopersMetric(graph, VCSPath, OldCommit, NewCommit);
                    VCSMetrics.AddCommitFrequencyMetric(graph, VCSPath, OldCommit, NewCommit);
                }
                return UniTask.FromResult(graph);
            }
        }

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.Git;
        }

        /// <summary>
        /// Checks whether the assumptions on <see cref="VCSPath"/> and
        /// <see cref="OldRevision"/> and <see cref="NewRevision"/> and <paramref name="city"/> hold.
        /// If not, exceptions are thrown accordingly.
        /// </summary>
        /// <param name="city">To be checked</param>
        /// <exception cref="ArgumentException">thrown in case <see cref="VCSPath"/>,
        /// or <see cref="OldRevision"/> or <see cref="NewRevision"/>
        /// is undefined or does not exist or <paramref name="city"/> is null or is not a DiffCity</exception>
        protected void CheckArguments(AbstractSEECity city)
        {
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
            else
            {
                if (city is DiffCity diffcity)
                {
                    OldRevision = diffcity.OldRevision;
                    NewRevision = diffcity.NewRevision;
                    VCSPath = diffcity.VCSPath.Path;

                    if (string.IsNullOrEmpty(VCSPath))
                    {
                        throw new ArgumentException("Empty VCS Path.\n");
                    }
                    if (!Directory.Exists(VCSPath))
                    {
                        throw new ArgumentException($"Directory {VCSPath} does not exist.\n");
                    }
                    if (string.IsNullOrEmpty(OldRevision))
                    {
                        throw new ArgumentException("Empty Old Revision.\n");
                    }
                    if (string.IsNullOrEmpty(NewRevision))
                    {
                        throw new ArgumentException("Empty New Revision.\n");
                    }
                }
                else
                {
                    throw new ArgumentException($"To generate Git metrics, the given city should be a {nameof(DiffCity)}.\n");
                }
            }
        }

        #region ConfigIO

        /// <summary>
        /// Label of attribute <see cref="VCSPath"/> in the configuration file.
        /// </summary>
        private const string vcsPathLabel = "VCSPath";

        /// <summary>
        /// Label of attribute <see cref="OldRevision"/> in the configuration file.
        /// </summary>
        private const string oldRevisionLabel = "OldRevision";

        /// <summary>
        /// Label of attribute <see cref="NewRevision"/> in the configuration file.
        /// </summary>
        private const string newRevisionLabel = "NewRevision";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(VCSPath, vcsPathLabel);
            writer.Save(OldRevision, oldRevisionLabel);
            writer.Save(NewRevision, newRevisionLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, vcsPathLabel, ref VCSPath);
            ConfigIO.Restore(attributes, oldRevisionLabel, ref OldRevision);
            ConfigIO.Restore(attributes, newRevisionLabel, ref NewRevision);
        }

        #endregion
    }
}
