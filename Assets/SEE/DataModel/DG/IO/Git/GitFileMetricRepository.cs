using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using LibGit2Sharp;
using SEE.Utils;

namespace SEE.DataModel.DG.IO.Git
{
    /// <summary>
    /// Processes and collects all metrics from all files of a git repository.
    ///
    /// This class works by passing each commit to <see cref="ProcessCommit(LibGit2Sharp.Commit,LibGit2Sharp.Patch)"/> to collect the file changes.
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
    public class GitFileMetricRepository
    {
        /// <summary>
        /// Maps the filename to the collected git file metrics
        /// </summary>
        public Dictionary<string, GitFileMetricsCollector> FileToMetrics { get; } = new();

        /// <summary>
        /// The git repository to collect the metrics from
        /// </summary>
        private readonly Repository gitRepository;

        /// <summary>
        /// A list of file extensions which should be included
        /// </summary>
        private readonly IEnumerable<string> includedFiles;


        /// <summary>
        /// Used in the calculation of the truck factor.
        ///
        /// Specifies the minimum ratio of the file churn the core devs should be responsible for 
        /// </summary>
        private const float TruckFactorCoreDevRatio = 0.8f;


        public GitFileMetricRepository(Repository gitRepository, IEnumerable<string> includedFiles)
        {
            this.gitRepository = gitRepository;
            this.includedFiles = includedFiles;
        }

        /// <summary>
        /// Calculates the truck factor of all files
        /// </summary>
        public void CalculateTruckFactor()
        {
            foreach (var file in FileToMetrics)
            {
                file.Value.TruckFactor = CalculateTruckFactor(file.Value.AuthorsChurn);
            }
        }


        /// <summary>
        /// Calculates the truck factor based a LOC-based heuristic by Yamashita et al (2015). for estimating the coreDev set.
        ///
        /// cited by. Ferreira et. al
        ///
        /// Soruce/Math: https://doi.org/10.1145/2804360.2804366, https://doi.org/10.1007/s11219-019-09457-2
        /// </summary>
        /// <returns>The calculated truck factor</returns>
        private static int CalculateTruckFactor(IReadOnlyDictionary<string, int> developersChurn)
        {
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

        public void ProcessCommit(Commit commit, [CanBeNull] Patch commitChanges)
        {
            if (commitChanges == null || commit == null)
            {
                return;
            }

            foreach (var changedFile in commitChanges)
            {
                string filePath = changedFile.Path;
                if (!includedFiles.Contains(Path.GetExtension(filePath)))
                {
                    continue;
                }

                if (!FileToMetrics.ContainsKey(filePath))
                {
                    FileToMetrics.Add(filePath,
                        new GitFileMetricsCollector(1, new HashSet<string> { commit.Author.Email },
                            changedFile.LinesAdded + changedFile.LinesDeleted));

                    FileToMetrics[filePath].AuthorsChurn.Add(commit.Author.Email,
                        changedFile.LinesAdded + changedFile.LinesDeleted);
                }
                else
                {
                    FileToMetrics[filePath].NumberOfCommits += 1;
                    FileToMetrics[filePath].Authors.Add(commit.Author.Email);
                    FileToMetrics[filePath].Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                    FileToMetrics[filePath].AuthorsChurn.GetOrAdd(commit.Author.Email, 0);
                    foreach (var otherFiles in commitChanges.Where(e => !e.Equals(changedFile)).ToList())
                    {
                        FileToMetrics[filePath].FilesChangesTogehter.GetOrAdd(otherFiles.Path, 0);
                        FileToMetrics[filePath].FilesChangesTogehter[otherFiles.Path] += 1;
                    }
                    
                    FileToMetrics[filePath].AuthorsChurn[commit.Author.Email] +=
                        (changedFile.LinesAdded + changedFile.LinesDeleted);
                }
            }
        }

        /// <summary>
        /// Processes a commit and calculates the metrics.
        ///
        /// This method will get the changes by comparing <paramref name="commit"/> with its parent (if one exists).
        /// If the changes are already calculated <see cref="ProcessCommit(LibGit2Sharp.Commit,LibGit2Sharp.Patch)"/> can be used.
        /// </summary>
        /// <param name="commit"></param>
        public void ProcessCommit(Commit commit)
        {
            if (commit == null)
            {
                return;
            }

            if (commit.Parents.Any())
            {
                var changedFilesPath = gitRepository.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);
                ProcessCommit(commit, changedFilesPath);
            }
            else
            {
                var changedFilesPath = gitRepository.Diff.Compare<Patch>(null, commit.Tree);
                ProcessCommit(commit, changedFilesPath);
            }
        }
    }
}
