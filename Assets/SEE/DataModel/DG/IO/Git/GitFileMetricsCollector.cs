using System.Collections.Generic;
using LibGit2Sharp;

namespace SEE.DataModel.DG.IO.Git
{
    public class GitFileMetricsCollector
    {
        public int NumberOfCommits { get; set; }

        public HashSet<string> Authors { get; set; }

        public Dictionary<string, int> AuthorsChurn { get; set; }

        public int TruckFactor { get; set; }

        /// <summary>
        /// Total sum of changed lines (added and removed)
        /// </summary>
        public int Churn { get; set; }

        public GitFileMetricsCollector()
        {
            Authors = new();
            AuthorsChurn = new();
        }

        public GitFileMetricsCollector(int numberOfCommits, HashSet<string> authors, int churn)
        {
            NumberOfCommits = numberOfCommits;
            Authors = authors;
            Churn = churn;
            AuthorsChurn = new();
            TruckFactor = 0;
        }
    }
}
