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
        /// Indicates whether authors with similar attributes (such as name variations)
        /// should be combined during the processing of commit data.
        /// </summary>
        private readonly bool combineSimilarAuthors;

        /// <summary>
        /// Maps an author to their associated alias authors.
        /// Used to group and identify authors that represent the same individual
        /// but may have different names or email identifiers in the commit history.
        /// </summary>
        private readonly Dictionary<GitFileAuthor, List<GitFileAuthor>> authorAliasMap;

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
                    FileToMetrics.Add(file, new GitFileMetrics(0, new HashSet<GitFileAuthor>(), 0));
                }
            }
        }

        public GitFileMetricProcessor(Repository gitRepository, Dictionary<string, bool> pathGlobbing,
            IEnumerable<string> repositoryFiles, bool combineSimilarAuthors,
            Dictionary<GitFileAuthor, List<GitFileAuthor>> authorAliasMap) : this(gitRepository, pathGlobbing,
            repositoryFiles)
        {
            this.combineSimilarAuthors = combineSimilarAuthors;
            this.authorAliasMap = authorAliasMap;
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
        private static int CalculateTruckFactor(IDictionary<GitFileAuthor, int> developersChurn)
        {
            if (developersChurn.Count == 0)
            {
                return 0;
            }

            int totalChurn = developersChurn.Select(x => x.Value).Sum();

            HashSet<GitFileAuthor> coreDevs = new();

            float cumulativeRatio = 0;

            // Sorting devs by their number of changed files
            List<GitFileAuthor> sortedDevs =
                developersChurn
                    .OrderByDescending(x => x.Value)
                    .Select(x => x.Key)
                    .ToList();
            // Selecting the coreDevs which are responsible for at least 80% of the total churn of a file
            while (cumulativeRatio <= TruckFactorCoreDevRatio)
            {
                GitFileAuthor dev = sortedDevs[0];
                float devRatio = (float)developersChurn[dev] / totalChurn;
                cumulativeRatio += devRatio;
                coreDevs.Add(dev);
                sortedDevs.Remove(dev);
            }

            return coreDevs.Count;
        }

        /// <summary>
        /// Retrieves the alias of the specified author if it exists in the alias mapping;
        /// otherwise, returns the original author.
        ///
        /// This will be the author added to the <see cref="GitFileMetrics"/>
        /// </summary>
        /// <param name="author">The author whose alias is being checked.</param>
        /// <returns>A <see cref="GitFileAuthor"/> instance representing the alias of the author if found,
        /// or the original author if no alias exists.</returns>
        private GitFileAuthor GetAuthorAliasIfExist(GitFileAuthor author)
        {
            // If the author is not in the alias map or combining of author aliases is disabled, use the original author
            return GetAuthorAliasParentIfEnabled(author) ?? author;
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

            GitFileAuthor commitAuthor = new(commit.Author.Name, commit.Author.Email);

            foreach (PatchEntryChanges changedFile in commitChanges)
            {
                string filePath = changedFile.Path;

                if (!matcher.Match(filePath).HasMatches)
                {
                    continue;
                }

                GitFileAuthor authorKey = GetAuthorAliasIfExist(commitAuthor);

                if (!FileToMetrics.ContainsKey(filePath))
                {
                    // If the file was not added to the metrics yet, add it
                    FileToMetrics.Add(filePath,
                        new GitFileMetrics(1,
                            new HashSet<GitFileAuthor> { authorKey },
                            changedFile.LinesAdded + changedFile.LinesDeleted));

                    FileToMetrics[filePath].AuthorsChurn.Add(authorKey,
                        changedFile.LinesAdded + changedFile.LinesDeleted);
                }
                else
                {
                    GitFileMetrics changedFileMetrics = FileToMetrics[filePath];
                    changedFileMetrics.NumberOfCommits += 1;
                    changedFileMetrics.Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                    changedFileMetrics.Authors.Add(authorKey);


                    changedFileMetrics.AuthorsChurn.GetOrAdd(authorKey, () => 0);
                    changedFileMetrics.AuthorsChurn[authorKey] += changedFile.LinesAdded + changedFile.LinesDeleted;

                    foreach (var otherFilePath in commitChanges
                                 .Where(e => !e.Equals(changedFile))
                                 .Select(x => x.Path))
                    {
                        changedFileMetrics.FilesChangesTogether.GetOrAdd(otherFilePath, () => 0);
                        changedFileMetrics.FilesChangesTogether[otherFilePath]++;
                    }


                    foreach (string otherFilesPath in commitChanges
                                 .Where(e => !e.Equals(changedFile))
                                 .Select(x => x.Path))
                    {
                        // Processing the files which were changed together with the current file
                        FileToMetrics[filePath].FilesChangesTogether.GetOrAdd(otherFilesPath, () => 0);
                        FileToMetrics[filePath].FilesChangesTogether[otherFilesPath] += 1;
                    }

                    var changedFileLinesDeleted = FileToMetrics[filePath].AuthorsChurn.Keys.First(x =>
                        MatchGitAuthors(x, commitAuthor));
                    FileToMetrics[filePath].AuthorsChurn[changedFileLinesDeleted] +=
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

        private GitFileAuthor GetAuthorAliasParentIfEnabled(GitFileAuthor author)
        {
            if (!combineSimilarAuthors)
            {
                return null;
            }

            return authorAliasMap
                .FirstOrDefault(alias => alias.Value.Any(x => x.Email == author.Email && x.Name == author.Name)).Key;
        }

        /// <summary>
        /// Compares two Git file authors to determine if they match.
        ///
        /// If <see cref="combineSimilarAuthors"/> is true, the authors are considered a match if either their names or emails are the same.
        /// Otherwise, both the name and email must be the same for the authors to match.
        /// </summary>
        /// <param name="author1">The first Git file author to compare.</param>
        /// <param name="author2">The second Git file author to compare.</param>
        /// <returns>
        /// True if the authors match by name or email; otherwise, false.
        /// </returns>
        private bool MatchGitAuthors(GitFileAuthor author1, GitFileAuthor author2)
        {
            return combineSimilarAuthors
                ? author1.Name == author2.Name || author1.Email == author2.Email
                : author1.Name == author2.Name && author1.Email == author2.Email;
        }
    }
}
