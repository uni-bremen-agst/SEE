using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                    AddLineofCodeChurnMetric(graph, VCSPath, OldCommit, NewCommit);
                    AddNumberofDevelopersMetric(graph, VCSPath, OldCommit, NewCommit);
                    AddCommitFrequencyMetric(graph, VCSPath, OldCommit, NewCommit);
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

        /// <summary>
        /// Calculates the number of lines of code added and deleted for each file changed
        /// between two commits and adds them as metrics to <paramref name="graph"/>.
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="vcsPath">the path to the VCS containing the two revisions to be compared</param>
        /// <param name="oldCommit">the older commit that constitutes the baseline of the comparison</param>
        /// <param name="newCommit">the newer commit against which the <paramref name="oldCommit"/> is
        /// to be compared</param>
        protected static void AddLineofCodeChurnMetric(Graph graph, string vcsPath, Commit oldCommit, Commit newCommit)
        {
            using Repository repo = new(vcsPath);
            Patch changes = repo.Diff.Compare<Patch>(oldCommit.Tree, newCommit.Tree);

            foreach (PatchEntryChanges change in changes)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (node.ID.Replace('\\', '/') == change.Path)
                    {
                        node.SetInt(Git.LinesAdded, change.LinesAdded);
                        node.SetInt(Git.LinesDeleted, change.LinesDeleted);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the number of unique developers who contributed to each file for each file changed
        /// between two commits and adds it as a metric to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="vcsPath">the path to the VCS containing the two revisions to be compared</param>
        /// <param name="oldCommit">the older commit that constitutes the baseline of the comparison</param>
        /// <param name="newCommit">the newer commit against which the <paramref name="oldCommit"/> is
        /// to be compared</param>
        protected static void AddNumberofDevelopersMetric(Graph graph, string vcsPath, Commit oldCommit, Commit newCommit)
        {
            using Repository repo = new(vcsPath);
            ICommitLog commits = repo.Commits.QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Topological });

            Dictionary<string, HashSet<string>> uniqueContributorsPerFile = new();

            foreach (Commit commit in commits)
            {
                if (commit.Author.When >= oldCommit.Author.When && commit.Author.When <= newCommit.Author.When)
                {
                    foreach (Commit parent in commit.Parents)
                    {
                        Patch changes = repo.Diff.Compare<Patch>(parent.Tree, commit.Tree);

                        foreach (PatchEntryChanges change in changes)
                        {
                            string filePath = change.Path;
                            string id = commit.Author.Email;

                            if (!uniqueContributorsPerFile.ContainsKey(filePath))
                            {
                                uniqueContributorsPerFile[filePath] = new HashSet<string>();
                            }
                            uniqueContributorsPerFile[filePath].Add(id);
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, HashSet<string>> entry in uniqueContributorsPerFile)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (node.ID.Replace('\\', '/') == entry.Key)
                    {
                        node.SetInt(Git.NumberOfDevelopers, entry.Value.Count);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the number of times each file was changed for each file changed between
        /// two commits and adds it as a metric to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="vcsPath">the path to the VCS containing the two revisions to be compared</param>
        /// <param name="oldCommit">the older commit that constitutes the baseline of the comparison</param>
        /// <param name="newCommit">the newer commit against which the <paramref name="oldCommit"/> is
        /// to be compared</param>
        protected static void AddCommitFrequencyMetric(Graph graph, string vcsPath, Commit oldCommit, Commit newCommit)
        {
            using Repository repo = new(vcsPath);
            ICommitLog commitsBetween = repo.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = newCommit,
                ExcludeReachableFrom = oldCommit
            });

            Dictionary<string, int> fileCommitCounts = new();

            foreach (Commit commit in commitsBetween)
            {
                foreach (Commit parent in commit.Parents)
                {
                    TreeChanges changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);

                    foreach (TreeEntryChanges change in changes)
                    {
                        string filePath = change.Path;

                        if (fileCommitCounts.ContainsKey(filePath))
                        {
                            fileCommitCounts[filePath]++;
                        }
                        else
                        {
                            fileCommitCounts.Add(filePath, 1);
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, int> entry in fileCommitCounts.OrderByDescending(x => x.Value))
            {
                foreach (Node node in graph.Nodes())
                {
                    if (node.ID.Replace('\\', '/') == entry.Key)
                    {
                        node.SetInt(Git.CommitFrequency, entry.Value);
                    }
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
