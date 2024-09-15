using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.Utils;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Processes and collects all metrics from all files of a git repository.
    ///
    /// This class works by passing each commit to
    /// <see cref="ProcessCommit(LibGit2Sharp.Commit,LibGit2Sharp.Patch)"/> to collect the file changes.
    ///
    /// The calculated metrics for each file are stored in <see cref="FileToMetrics"/>
    /// </summary>
    /// <example>
    /// <code>
    /// GitFileMetricRepository fileRepo = new(gitRepo, includedFiles, excludedFiles);
    /// foreach (var commit in gitRepo.Commits)
    /// {
    ///     fileRepo.ProcessCommit(commit);
    /// }
    /// // Get the calculated metrics
    /// fileRepo.FileToMetrics;
    ///
    /// </code>
    /// </example>
    public class GitFileMetricProcessor
    {
        /// <summary>
        /// Maps the filename to the collected git metrics of that file
        /// </summary>
        public IDictionary<string, GitFileMetrics> FileToMetrics { get; }
            = new Dictionary<string, GitFileMetrics>();

        /// <summary>
        /// The git repository to collect the metrics from
        /// </summary>
        private readonly Repository gitRepository;

        /// <summary>
        /// A list of file extensions which should be included
        /// </summary>
        private readonly IDictionary<string, bool> pathGlobbing;

        /// <summary>
        /// Matcher is used to check by a glob pattern if a file should be included in the analysis or not.
        /// </summary>
        private readonly Matcher matcher;

        /// <summary>
        /// Used in the calculation of the truck factor.
        ///
        /// Specifies the minimum ratio of the file churn the core devs should be responsible for
        /// </summary>
        private const float TruckFactorCoreDevRatio = 0.8f;

        /// <summary>
        /// Creates a new instance of <see cref="GitFileMetricProcessor"/>
        /// </summary>
        /// <param name="gitRepository">The git repository you want to collect the metrics from</param>
        /// <param name="pathGlobbing">A dictionary of path glob patterns you want to include or exclude</param>
        /// <param name="repositoryFiles">A list of a files which should be displayed in the code-city</param>
        public GitFileMetricProcessor(Repository gitRepository, IDictionary<string, bool> pathGlobbing,
            IEnumerable<string> repositoryFiles)
        {
            this.gitRepository = gitRepository;
            this.pathGlobbing = pathGlobbing;
            this.matcher = new();

            foreach (KeyValuePair<string, bool> pattern in this.pathGlobbing)
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

            foreach (string file in repositoryFiles)
            {
                if (matcher.Match(file).HasMatches)
                {
                    FileToMetrics.Add(file, new GitFileMetrics(0, new HashSet<string>(), 0));
                }
            }
        }

        /// <summary>
        /// Calculates the truck factor of all files
        /// </summary>
        public void CalculateTruckFactor()
        {
            foreach (KeyValuePair<string, GitFileMetrics> file in FileToMetrics)
            {
                file.Value.TruckFactor = CalculateTruckFactor(file.Value.AuthorsChurn);
            }
        }

        /// <summary>
        /// Calculates the truck factor based on a LOC-based heuristic by Yamashita et al. (2015)
        /// for estimating the coreDev set. Cited by Ferreira et al.
        ///
        /// Source/Math: https://doi.org/10.1145/2804360.2804366, https://doi.org/10.1007/s11219-019-09457-2
        /// </summary>
        /// <param name="developersChurn">The churn of each developer</param>
        /// <returns>The calculated truck factor</returns>
        private static int CalculateTruckFactor(IDictionary<string, int> developersChurn)
        {
            if (developersChurn.Count == 0)
            {
                return 0;
            }

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
            while (cumulativeRatio <= TruckFactorCoreDevRatio)
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
        /// Processes a commit and calculates the metrics.
        /// </summary>
        /// <param name="commit">The commit that should be processed</param>
        /// <param name="commitChanges">The changes the commit has made. This will be most likely the
        /// changes between this commit and its parent</param>
        public void ProcessCommit(Commit commit, [CanBeNull] Patch commitChanges)
        {
            if (commitChanges == null || commit == null)
            {
                return;
            }

            foreach (PatchEntryChanges changedFile in commitChanges)
            {
                string filePath = changedFile.Path;
                if (!matcher.Match(filePath).HasMatches)
                {
                    continue;
                }

                if (!FileToMetrics.ContainsKey(filePath))
                {
                    FileToMetrics.Add(filePath,
                        new GitFileMetrics(1, new HashSet<string> { commit.Author.Email },
                            changedFile.LinesAdded + changedFile.LinesDeleted));

                    FileToMetrics[filePath].AuthorsChurn.Add(commit.Author.Email,
                        changedFile.LinesAdded + changedFile.LinesDeleted);
                }
                else
                {
                    FileToMetrics[filePath].NumberOfCommits += 1;
                    FileToMetrics[filePath].Authors.Add(commit.Author.Email);
                    FileToMetrics[filePath].Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                    FileToMetrics[filePath].AuthorsChurn.GetOrAdd(commit.Author.Email, () => 0);
                    foreach (PatchEntryChanges otherFiles in commitChanges.Where(e => !e.Equals(changedFile)).ToList())
                    {
                        FileToMetrics[filePath].FilesChangesTogether.GetOrAdd(otherFiles.Path, () => 0);
                        FileToMetrics[filePath].FilesChangesTogether[otherFiles.Path] += 1;
                    }

                    FileToMetrics[filePath].AuthorsChurn[commit.Author.Email] +=
                        (changedFile.LinesAdded + changedFile.LinesDeleted);
                }
            }
        }

        /// <summary>
        /// Processes a commit and calculates the metrics.
        ///
        /// This method will get the changes by comparing <paramref name="commit"/> with its
        /// parent (if one exists).
        /// If the changes are already calculated
        /// <see cref="ProcessCommit(LibGit2Sharp.Commit,LibGit2Sharp.Patch)"/> can be used.
        /// </summary>
        /// <param name="commit">the commit to be processed</param>
        public void ProcessCommit(Commit commit)
        {
            if (commit == null)
            {
                return;
            }

            if (commit.Parents.Any())
            {
                Patch changedFilesPath = gitRepository.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);
                ProcessCommit(commit, changedFilesPath);
            }
            else
            {
                Patch changedFilesPath = gitRepository.Diff.Compare<Patch>(null, commit.Tree);
                ProcessCommit(commit, changedFilesPath);
            }
        }
    }
}
