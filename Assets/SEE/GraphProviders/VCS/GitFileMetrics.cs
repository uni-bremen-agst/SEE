using System.Collections.Generic;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Represents the metrics of a file which are collected from a git repository.
    /// </summary>
    public class GitFileMetrics
    {
        /// <summary>
        /// The total number of commits a file has.
        /// </summary>
        public int NumberOfCommits { get; set; }

        /// <summary>
        /// A list of the authors which contributed to this file.
        /// </summary>
        public ISet<FileAuthor> Authors { get; set; }

        /// <summary>
        /// A collection of other files, which where changed together in the same commit.
        /// The key represents the filename and the value the number of common commits.
        /// </summary>
        public IDictionary<string, int> FilesChangesTogether { get; set; }

        /// <summary>
        /// The churn (total number of changed lines) of each author.
        /// </summary>
        public IDictionary<FileAuthor, int> AuthorsChurn { get; set; }

        /// <summary>
        /// The truck/bus factor of this file.
        /// This is the number of contributors which have contributed at least 80% of
        /// the source code.
        /// </summary>
        public int TruckFactor { get; set; }

        /// <summary>
        /// Total sum of changed lines (added or removed).
        /// </summary>
        public int Churn { get; set; }

        /// <summary>
        /// The constructor of <see cref="GitFileMetrics"/>.
        /// </summary>
        /// <param name="numberOfCommits">The number of commits.</param>
        /// <param name="authors">A list of authors.</param>
        /// <param name="churn">The churn.</param>
        public GitFileMetrics(int numberOfCommits, HashSet<FileAuthor> authors, int churn)
        {
            NumberOfCommits = numberOfCommits;
            Authors = authors;
            Churn = churn;
            AuthorsChurn = new Dictionary<FileAuthor, int>();
            FilesChangesTogether = new Dictionary<string, int>();
            TruckFactor = 0;
        }
    }
}
