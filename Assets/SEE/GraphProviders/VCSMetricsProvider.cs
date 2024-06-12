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
    public class VCSMetricsProvider : GraphProvider
    {
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
            if (graph == null)
            {
                throw new NotImplementedException();
            }
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }
            if (city is DiffCity diffcity)
            {
                string oldRevision = diffcity.OldRevision;
                string newRevision = diffcity.NewRevision;
                string vcsPath = diffcity.VCSPath.Path;

                if (string.IsNullOrEmpty(vcsPath))
                {
                    throw new ArgumentException("Empty VCS Path.\n");
                }
                if (!Directory.Exists(vcsPath))
                {
                    throw new ArgumentException($"Directory {vcsPath} does not exist.\n");
                }
                if (string.IsNullOrEmpty(oldRevision))
                {
                    throw new ArgumentException("Empty old Revision.\n");
                }
                if (string.IsNullOrEmpty(newRevision))
                {
                    throw new ArgumentException("Empty new Revision.\n");
                }

                using (Repository repo = new(vcsPath))
                {
                    Commit oldCommit = repo.Lookup<Commit>(oldRevision);
                    Commit newCommit = repo.Lookup<Commit>(newRevision);

                    VCSMetrics.AddLineofCodeChurnMetric(graph, vcsPath, oldCommit, newCommit);
                    VCSMetrics.AddNumberofDevelopersMetric(graph, vcsPath, oldCommit, newCommit);
                    VCSMetrics.AddCommitFrequencyMetric(graph, vcsPath, oldCommit, newCommit);
                }
                return UniTask.FromResult(graph);
            }
            else
            {
                throw new ArgumentException($"To generate VCS metrics, the given city should be a {nameof(DiffCity)}.\n");
            }
        }

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.VCSMetrics;
        }

        protected override void SaveAttributes(ConfigWriter writer)
        {
            // Nothing to be saved. This class has not attributes.
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            // Nothing to be restored. This class has not attributes.
        }
    }
}
