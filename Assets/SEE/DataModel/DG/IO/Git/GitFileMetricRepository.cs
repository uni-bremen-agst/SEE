using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using LibGit2Sharp;
using SEE.Utils;

namespace SEE.DataModel.DG.IO.Git
{
    public class GitFileMetricRepository
    {
        public Dictionary<string, GitFileMetricsCollector> FileToMetrics { get; } = new();

        private readonly Repository repo;

        private readonly IEnumerable<string> includedFiles;

        private readonly IEnumerable<string> excludedFiles;


        /// <summary>
        /// Used in the calculation of the truck factor.
        ///
        /// Specifies the minimum ratio of the file churn the core devs should be responsible for 
        /// </summary>
        private const float TruckFactorCoreDevRatio = 0.8f;


        public GitFileMetricRepository(Repository repo, IEnumerable<string> includedFiles,
            IEnumerable<string> excludedFiles)
        {
            this.repo = repo;
            this.includedFiles = includedFiles;
            this.excludedFiles = excludedFiles;
        }

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
                if (!includedFiles.Contains(Path.GetExtension(filePath)) ||
                    excludedFiles.Contains(Path.GetExtension(filePath)))
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
                    FileToMetrics[filePath].AuthorsChurn[commit.Author.Email] +=
                        (changedFile.LinesAdded + changedFile.LinesDeleted);
                }
            }
        }

        public void ProcessCommit(Commit commit)
        {
            if (commit == null)
            {
                return;
            }
            var changedFilesPath = repo.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);
            ProcessCommit(commit, changedFilesPath);
        }
    }
}
