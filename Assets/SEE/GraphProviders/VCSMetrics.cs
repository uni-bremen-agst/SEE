using LibGit2Sharp;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Calculates metrics between two revisions from a version control system
    /// and adds these to a graph.
    /// </summary>
    public static class VCSMetrics
    {
        /// <summary>
        /// Calculates the number of lines of code added and deleted for each file changed
        /// between two commits and adds them as metrics to <paramref name="graph"/>.
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="vcsPath">the path to the VCS containing the two revisions to be compared</param>
        /// <param name="oldCommit">the older commit that constitutes the baseline of the comparison</param>
        /// <param name="newCommit">the newer commit against which the <paramref name="oldCommit"/> is
        /// to be compared</param>
        public static void AddLineofCodeChurnMetric(Graph graph, string vcsPath, Commit oldCommit, Commit newCommit)
        {
            using Repository repo = new(vcsPath);
            AddLinesOfCodeChurnMetric(graph, repo, oldCommit, newCommit);
        }

        private static void AddLinesOfCodeChurnMetric(Graph graph, Repository repo, Commit oldCommit, Commit newCommit)
        {
            Patch changes = repo.Diff.Compare<Patch>(oldCommit.Tree, newCommit.Tree);

            foreach (PatchEntryChanges change in changes)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (node.ID.Replace('\\', '/') == change.Path)
                    {
                        node.SetInt(DataModel.DG.VCS.LinesAdded, change.LinesAdded);
                        node.SetInt(DataModel.DG.VCS.LinesDeleted, change.LinesDeleted);
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
        public static void AddNumberofDevelopersMetric(Graph graph, string vcsPath, Commit oldCommit, Commit newCommit)
        {
            using Repository repo = new(vcsPath);
            AddNumberOfDevelopersMetric(graph, repo, oldCommit, newCommit);
        }

        private static void AddNumberOfDevelopersMetric(Graph graph, Repository repo, Commit oldCommit, Commit newCommit)
        {
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
                        node.SetInt(DataModel.DG.VCS.NumberOfDevelopers, entry.Value.Count);
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
        public static void AddCommitFrequencyMetric(Graph graph, string vcsPath, Commit oldCommit, Commit newCommit)
        {
            using Repository repo = new(vcsPath);
            AddCommitFrequencyMetric(graph, repo, oldCommit, newCommit);
        }

        private static void AddCommitFrequencyMetric(Graph graph, Repository repo, Commit oldCommit, Commit newCommit)
        {
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
                        node.SetInt(DataModel.DG.VCS.CommitFrequency, entry.Value);
                    }
                }
            }
        }

        internal static void AddMetrics(Graph graph, Repository repo, string oldRevision, string newRevision)
        {
            Commit oldCommit = repo.Lookup<Commit>(oldRevision);
            Commit newCommit = repo.Lookup<Commit>(newRevision);

            AddLinesOfCodeChurnMetric(graph, repo, oldCommit, newCommit);
            AddNumberOfDevelopersMetric(graph, repo, oldCommit, newCommit);
            AddCommitFrequencyMetric(graph, repo, newCommit, oldCommit);
        }
    }
}
