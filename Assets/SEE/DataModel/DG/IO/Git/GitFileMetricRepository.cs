using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using SEE.Utils;

namespace SEE.DataModel.DG.IO.Git
{
    public class GitFileMetricRepository
    {
        private Dictionary<string, GitFileMetricsCollector> fileToMetrics = new();

        public Dictionary<string, GitFileMetricsCollector> FileToMetrics => fileToMetrics;

        private Repository repo;

        private IEnumerable<string> includedFiles;

        private IEnumerable<string> excludedFiles;


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
            foreach (var file in fileToMetrics)
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
        private static int CalculateTruckFactor(Dictionary<string, int> developersChurn)
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

        public void ProcessCommit(Commit commit, Patch commitChanges)
        {
            foreach (var changedFile in commitChanges)
            {
                string filePath = changedFile.Path;
                if (!includedFiles.Contains(Path.GetExtension(filePath)) ||
                    excludedFiles.Contains(Path.GetExtension(filePath)))
                {
                    continue;
                }

                if (!fileToMetrics.ContainsKey(filePath))
                {
                    fileToMetrics.Add(filePath,
                        new GitFileMetricsCollector(1, new HashSet<string> { commit.Author.Email },
                            changedFile.LinesAdded + changedFile.LinesDeleted));

                    fileToMetrics[filePath].AuthorsChurn.Add(commit.Author.Email,
                        changedFile.LinesAdded + changedFile.LinesDeleted);
                }
                else
                {
                    fileToMetrics[filePath].NumberOfCommits += 1;
                    fileToMetrics[filePath].Authors.Add(commit.Author.Email);
                    fileToMetrics[filePath].Churn += changedFile.LinesAdded + changedFile.LinesDeleted;
                    fileToMetrics[filePath].AuthorsChurn.GetOrAdd(commit.Author.Email, 0);
                    fileToMetrics[filePath].AuthorsChurn[commit.Author.Email] +=
                        (changedFile.LinesAdded + changedFile.LinesDeleted);
                }
            }
        }

        public void ProcessCommit(Commit commit)
        {
            var changedFilesPath = repo.Diff.Compare<Patch>(commit.Tree, commit.Parents.First().Tree);
            ProcessCommit(commit, changedFilesPath);
        }
    }
}
