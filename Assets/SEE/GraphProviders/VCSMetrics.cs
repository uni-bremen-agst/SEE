using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.VCS;
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
        /// Adds VCS metrics to all nodes in <paramref name="graph"/> based on the
        /// VCS information derived from <paramref name="repository"/>. The metrics are gathered
        /// in between the <paramref name="oldRevision"/> and <paramref name="newRevision"/>.
        /// If <paramref name="oldRevision"/> or <paramref name="newRevision"/> is null or empty,
        /// an exception is thrown.
        /// </summary>
        /// <param name="graph">The graph where the metric should be added.</param>
        /// <param name="repository">The repository from which the file content is retrieved.</param>
        /// <param name="oldRevision">The starting commit ID (baseline).</param>
        /// <param name="newRevision">The ending commit.</param>
        /// <exception cref="System.Exception">thrown if <paramref name="oldRevision"/>
        /// or <paramref name="newRevision"/> is null or empty or if they do not
        /// refer to an existing revision</exception>
        internal static void AddMetrics(Graph graph, GitRepository repository, string oldRevision, string newRevision)
        {
            if (string.IsNullOrWhiteSpace(oldRevision))
            {
                throw new System.Exception("The old revision must neither be null nor empty.");
            }
            if (string.IsNullOrWhiteSpace(newRevision))
            {
                throw new System.Exception("The new revision must neither be null nor empty.");
            }
            Commit oldCommit = repository.GetCommit(oldRevision);
            if (oldCommit == null)
            {
                throw new System.Exception($"There is no revision {oldCommit}");
            }
            Commit newCommit = repository.GetCommit(newRevision);
            if (newCommit == null)
            {
                throw new System.Exception($"There is no revision {newCommit}");
            }

            AddLinesOfCodeChurnMetric(graph, repository, oldCommit, newCommit);
            AddNumberOfDevelopersMetric(graph, repository, oldCommit, newCommit);
            AddCommitFrequencyMetric(graph, repository, oldCommit, newCommit);
        }

        /// <summary>
        /// Calculates the number of lines of code added and deleted for each file changed
        /// between two commits and adds them as metrics to <paramref name="graph"/>.
        /// <param name="graph">an existing graph where to add the metrics</param>
        /// <param name="repository">the VCS containing the two revisions to be compared</param>
        /// <param name="oldCommit">the older commit that constitutes the baseline of the comparison</param>
        /// <param name="newCommit">the newer commit against which the <paramref name="oldCommit"/> is
        private static void AddLinesOfCodeChurnMetric(Graph graph, GitRepository repository, Commit oldCommit, Commit newCommit)
        {
            Patch changes = repository.Diff(oldCommit, newCommit);

            foreach (PatchEntryChanges change in changes)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (node.ID == change.Path)
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
        /// <param name="repository">the VCS containing the two revisions to be compared</param>
        /// <param name="oldCommit">the older commit that constitutes the baseline of the comparison</param>
        /// <param name="newCommit">the newer commit against which the <paramref name="oldCommit"/> is
        /// to be compared</param>
        private static void AddNumberOfDevelopersMetric(Graph graph, GitRepository repository, Commit oldCommit, Commit newCommit)
        {
            ICommitLog commits = repository.CommitLog();

            Dictionary<string, HashSet<string>> uniqueContributorsPerFile = new();

            foreach (Commit commit in commits)
            {
                if (oldCommit.Author.When <= commit.Author.When && commit.Author.When <= newCommit.Author.When)
                {
                    foreach (Commit parent in commit.Parents)
                    {
                        Patch changes = repository.Diff(parent, commit);

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
                    if (node.ID == entry.Key)
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
        /// <param name="repository">the VCS containing the two revisions to be compared</param>
        /// <param name="oldCommit">the older commit that constitutes the baseline of the comparison</param>
        /// <param name="newCommit">the newer commit against which the <paramref name="oldCommit"/> is
        /// to be compared</param>
        private static void AddCommitFrequencyMetric(Graph graph, GitRepository repository, Commit oldCommit, Commit newCommit)
        {
            ICommitLog commitsBetween = repository.CommitLog(oldCommit, newCommit);

            Dictionary<string, int> fileCommitCounts = new();

            foreach (Commit commit in commitsBetween)
            {
                foreach (Commit parent in commit.Parents)
                {
                    TreeChanges changes = repository.TreeDiff(parent, commit);

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
                    if (node.ID == entry.Key)
                    {
                        node.SetInt(DataModel.DG.VCS.CommitFrequency, entry.Value);
                    }
                }
            }
        }
    }
}
