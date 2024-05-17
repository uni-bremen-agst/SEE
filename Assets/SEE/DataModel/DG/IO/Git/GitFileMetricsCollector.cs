using System.Collections.Generic;

namespace SEE.DataModel.DG.IO.Git
{
    public class GitFileMetricsCollector
    {
        public int NumberOfCommits { get; set; }

        public HashSet<string> Authors { get; }

        public Dictionary<string, int> AuthorsChurn { get; }

        public int TruckFactor { get; set; }

        /// <summary>
        /// Total sum of changed lines (added and removed)
        /// </summary>
        public int Churn { get; set; }
        

        public GitFileMetricsCollector(int numberOfCommits, HashSet<string> authors, int churn)
        {
            NumberOfCommits = numberOfCommits;
            Authors = authors;
            Churn = churn;
            AuthorsChurn = new Dictionary<string, int>();
            TruckFactor = 0;
        }
    }
}
