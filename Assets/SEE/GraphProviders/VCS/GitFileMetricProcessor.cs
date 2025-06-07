using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Utils;
using SEE.VCS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Processes and collects all metrics from all files of a git repository.
    /// </example>
    public static class GitFileMetricProcessor
    {
        /// <summary>
        /// A mapping of filenames (relative paths in a repository) onto their <see cref="GitFileMetrics"/>.
        /// </summary>
        private class FileToMetrics : Dictionary<string, GitFileMetrics> { }

        /// <summary>
        /// Returns a new <see cref="FileToMetrics"/> mapping containing empty <see cref="GitFileMetrics"/>
        /// for each and only the filenames contained in <paramref name="repositoryFiles"/>.
        /// </summary>
        /// <param name="repositoryFiles">A set of a files whose metrics should be calculated</param>
        private static FileToMetrics ToFileToMetrics(HashSet<string> repositoryFiles)
        {
            FileToMetrics fileToMetrics = new();
            foreach (string file in repositoryFiles)
            {
                fileToMetrics.Add(file, new GitFileMetrics(0, new HashSet<FileAuthor>(), 0));
            }
            return fileToMetrics;
        }

        #region Truck Factor
        /// <summary>
        /// Used in the calculation of the truck factor.
        ///
        /// Specifies the minimum ratio of the file churn the core devs should be responsible for
        /// </summary>
        private const float truckFactorCoreDevRatio = 0.8f;

        /// <summary>
        /// Calculates and adds the truck factor of all files in <paramref name="fileToMetrics"/>.
        /// </summary>
        /// <param name="fileToMetrics">Where to add the truck factor</param>
        private static void CalculateTruckFactor(FileToMetrics fileToMetrics)
        {
            foreach (KeyValuePair<string, GitFileMetrics> file in fileToMetrics)
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
        private static int CalculateTruckFactor(IDictionary<FileAuthor, int> developersChurn)
        {
            if (developersChurn.Count == 0)
            {
                return 0;
            }

            int totalChurn = developersChurn.Select(x => x.Value).Sum();

            HashSet<FileAuthor> coreDevs = new();

            float cumulativeRatio = 0;

            // Sorting devs by their number of changed files
            List<FileAuthor> sortedDevs =
                developersChurn
                    .OrderByDescending(x => x.Value)
                    .Select(x => x.Key)
                    .ToList();

            // Selecting the coreDevs which are responsible for at least 80% of the total churn of a file
            while (cumulativeRatio <= truckFactorCoreDevRatio)
            {
                FileAuthor dev = sortedDevs.First();
                cumulativeRatio += (float)((float)developersChurn[dev] / totalChurn);
                coreDevs.Add(dev);
                sortedDevs.Remove(dev);
            }

            return coreDevs.Count;
        }

        #endregion Truck Factor

        #region Author Aliasing
        /// <summary>
        /// If <paramref name="consultAliasMap"/> is false, the original <paramref name="author"/>
        /// will be returned.
        /// Otherwise: Returns the alias of the specified <paramref name="author"/> if it exists in the alias mapping;
        /// or else returns the original <paramref name="author"/>.
        ///
        /// For two <see cref="FileAuthor"/>s to match, they must have same name and email address,
        /// where the string comparison for both facets is case-insensitive.
        /// </summary>
        /// <param name="author">The author whose alias is to be retrieved.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false.</param>
        /// <returns>A <see cref="FileAuthor"/> instance representing the alias of the author if found,
        /// or the original author if no alias exists or if <paramref name="consultAliasMap"/> is false.</returns>
        private static FileAuthor GetAuthorAliasIfExists(FileAuthor author, bool consultAliasMap, AuthorMapping authorAliasMap)
        {
            // FIXME: Move this code to GitRepository.
            // If the author is not in the alias map or combining of author aliases is disabled, use the original author
            return ResolveAuthorAliasIfEnabled(author, consultAliasMap, authorAliasMap) ?? author;

            static FileAuthor ResolveAuthorAliasIfEnabled(FileAuthor author, bool combineSimilarAuthors, AuthorMapping authorAliasMap)
            {
                if (!combineSimilarAuthors)
                {
                    return null;
                }

                return authorAliasMap
                    .FirstOrDefault(alias => alias.Value.Any(x => String.Equals(x.Email, author.Email, StringComparison.OrdinalIgnoreCase)
                                                               && String.Equals(x.Name,  author.Name,  StringComparison.OrdinalIgnoreCase))).Key;
            }
        }
        #endregion Author Aliasing

        /// <summary>
        /// Calculates <see cref="GitFileMetrics"/> for all <paramref name="files"/> and adds
        /// these to their corresponding nodes in the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the file metrics.</param>
        /// <param name="simplifyGraph">If true, single chains of directory nodes in the node hierarchy
        /// will be collapsed into the inner most directory node</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="files">The files for which to calculate the metrics.</param>
        /// <param name="commitsInBetween">The metrics will be gathered for only the commits in this list.</param>
        /// <param name="commitChanges">The changes associated with each commit in <paramref name="commitsInBetween"/>;
        /// for each element in <paramref name="commitsInBetween"/> there must be a corresponding entry in
        /// <paramref name="commitChanges"/>.</param>
        internal static void AddVCSFileMetrics
            (Graph graph,
             bool simplifyGraph,
             string repositoryName,
             HashSet<string> files,
             List<Commit> commitsInBetween,
             IDictionary<Commit, Patch> commitChanges)
        {
            FileToMetrics fileToMetrics = ToFileToMetrics(files);

            foreach (Commit commitInBetween in commitsInBetween)
            {
                ProcessCommit(fileToMetrics, commitInBetween, commitChanges[commitInBetween], false, null);
            }

            CalculateTruckFactor(fileToMetrics);

            GitFileMetricsGraphGenerator.FillGraphWithGitMetrics
               (fileToMetrics, graph, repositoryName, simplifyGraph, idSuffix: "-Evo");
        }

        /// <summary>
        /// Processes a commit and calculates the metrics.
        /// </summary>
        /// <param name="fileToMetrics">metrics will be calculated for the files therein and added to this map</param>
        /// <param name="commit">The commit that should be processed</param>
        /// <param name="commitChanges">The changes the commit has made. This will be most likely the
        /// changes between this commit and its parent. Can be null.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        private static void ProcessCommit
            (FileToMetrics fileToMetrics,
            Commit commit,
            Patch commitChanges,
            bool consultAliasMap,
            AuthorMapping authorAliasMap)
        {
            if (commitChanges == null || commit == null)
            {
                return;
            }

            HashSet<string> files = new(fileToMetrics.Keys);

            FileAuthor commitAuthor = new(commit.Author.Name, commit.Author.Email);

            foreach (PatchEntryChanges changedFile in commitChanges)
            {
                string filePath = changedFile.Path;

                if (!files.Contains(filePath))
                {
                    continue;
                }

                FileAuthor authorKey = GetAuthorAliasIfExists(commitAuthor, consultAliasMap, authorAliasMap);

                if (!fileToMetrics.ContainsKey(filePath))
                {
                    // If the file was not added to the metrics yet, add it
                    fileToMetrics.Add(filePath,
                        new GitFileMetrics(1,
                            new HashSet<FileAuthor> { authorKey },
                            changedFile.LinesAdded + changedFile.LinesDeleted));

                    fileToMetrics[filePath].AuthorsChurn.Add(authorKey,
                        changedFile.LinesAdded + changedFile.LinesDeleted);
                }
                else
                {
                    GitFileMetrics changedFileMetrics = fileToMetrics[filePath];
                    changedFileMetrics.NumberOfCommits += 1;
                    changedFileMetrics.Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                    changedFileMetrics.Authors.Add(authorKey);
                    changedFileMetrics.AuthorsChurn.GetOrAdd(authorKey, () => 0);
                    changedFileMetrics.AuthorsChurn[authorKey] += changedFile.LinesAdded + changedFile.LinesDeleted;

                    foreach (string otherFilePath in commitChanges
                                 .Where(e => !e.Equals(changedFile))
                                 .Select(x => x.Path))
                    {
                        // Processing the files which were changed together with the current file
                        changedFileMetrics.FilesChangesTogether.GetOrAdd(otherFilePath, () => 0);
                        changedFileMetrics.FilesChangesTogether[otherFilePath]++;
                    }

                    fileToMetrics[filePath].AuthorsChurn[authorKey] +=
                        (changedFile.LinesAdded + changedFile.LinesDeleted);
                }
            }
        }

        /// <summary>
        /// Calculates <see cref="GitFileMetrics"/> for all relevant files in <paramref name="repository"/> and adds
        /// these to their corresponding nodes in the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the file metrics.</param>
        /// <param name="simplifyGraph">If true, single chains of directory nodes in the node hierarchy
        /// will be collapsed into the inner most directory node</param>
        /// <param name="repositoryName">The name of the repository.</param>
        /// <param name="startDate">The date after which commits in the history should be considered.
        /// Older commits will be ignored.</param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        /// <param name="changePercentage">To report the progress.</param>
        internal static void AddVCSFileMetrics
            (Graph graph,
             bool simplifyGraph,
             GitRepository repository,
             string repositoryName,
             DateTime startDate,
             bool consultAliasMap,
             AuthorMapping authorAliasMap,
             Action<float> changePercentage)
        {
            IList<Commit> commitList = repository.CommitsAfter(startDate);

            HashSet<string> files = repository.AllFiles();

            FileToMetrics fileToMetrics = ToFileToMetrics(files);

            int counter = 0;
            int commitLength = commitList.Count();
            foreach (Commit commit in commitList)
            {
                ProcessCommit(fileToMetrics, repository, commit, consultAliasMap, authorAliasMap);
                changePercentage?.Invoke(Mathf.Clamp((float)counter / commitLength, 0, 0.98f));
                counter++;
            }

            CalculateTruckFactor(fileToMetrics);
            GitFileMetricsGraphGenerator.FillGraphWithGitMetrics
                (fileToMetrics, graph, repositoryName, simplifyGraph);
        }

        /// <summary>
        /// If <paramref name="commit"/> is null, nothing happens.
        ///
        /// Otherwise, the metrics will be calculated between <paramref name="commit"/> and
        /// its first parents. If a commit has no parent, <paramref name="commit"/> is the
        /// very first commit in the version history, which is perfectly okay.
        /// </summary>
        /// <param name="fileToMetrics">Metrics will be calculated for the files therein and added to this map</param>
        /// <param name="gitRepository">The diff will be retrieved from this repository.</param>
        /// <param name="commit">The commit that should be processed assumed to belong to <paramref name="gitRepository"/></param>
        /// <param name="consultAliasMap">If <paramref name="authorAliasMap"/> should be consulted at all.</param>
        /// <param name="authorAliasMap">Where to to look up an alias. Can be null if <paramref name="consultAliasMap"/>
        /// is false</param>
        private static void ProcessCommit
            (FileToMetrics fileToMetrics,
            GitRepository gitRepository,
            Commit commit,
            bool consultAliasMap,
            AuthorMapping authorAliasMap)
        {
            if (commit == null)
            {
                return;
            }

            Patch changedFilesPath = commit.Parents.Any()
                ? gitRepository.Diff(commit, commit.Parents.First())
                : gitRepository.Diff(null, commit);

            ProcessCommit(fileToMetrics, commit, changedFilesPath, consultAliasMap, authorAliasMap);
        }
    }
}
