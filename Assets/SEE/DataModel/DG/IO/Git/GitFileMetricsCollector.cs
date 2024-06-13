using System.Collections.Generic;
using JetBrains.Annotations;

namespace SEE.DataModel.DG.IO.Git
{
    /// <summary>
    /// Represents the metrics of a file which are collected from a git repository.
    /// </summary>
    public class GitFileMetricsCollector
    {
        /// <summary>
        /// The total number of commits a file has
        /// </summary>
        public int NumberOfCommits { get; set; }

        /// <summary>
        /// A list of the authors which contributed to this file
        /// </summary>
        public HashSet<string> Authors { get; }
        
        public Dictionary<string, int> FilesChangesTogehter { get; }

        /// <summary>
        /// The churn (total number of changed files) of each author
        /// </summary>
        public Dictionary<string, int> AuthorsChurn { get; }

        /// <summary>
        /// The truck/bus factor of this file.
        /// This is the number of contributors which have at least contributed 80% of the source code
        /// </summary>
        public int TruckFactor { get; set; }

        /// <summary>
        /// Total sum of changed lines (added and removed)
        /// </summary>
        public int Churn { get; set; }


        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="numberOfCommits">The number of commits</param>
        /// <param name="authors">A list of authors</param>
        /// <param name="churn">The churn</param>
        public GitFileMetricsCollector(int numberOfCommits, HashSet<string> authors, int churn)
        {
            NumberOfCommits = numberOfCommits;
            Authors = authors;
            Churn = churn;
            AuthorsChurn = new Dictionary<string, int>();
            FilesChangesTogehter = new();
            TruckFactor = 0;
        }
    }
}
