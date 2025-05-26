using Microsoft.Extensions.FileSystemGlobbing;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;

namespace SEE.VCS
{
    /// <summary>
    /// A filter for VCS queries.
    /// </summary>
    internal class Filter
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="globbing">the inclusion/exclusion file globbings</param>
        /// <param name="repositoryPaths">local paths starting at the root of the repository</param>
        /// <param name="branches">the name of the branches</param>
        public Filter(Globbing globbing = null, IEnumerable<string> repositoryPaths = null, IEnumerable<string> branches = null)
        {
            Matcher = PathGlobbing.ToMatcher(globbing);
            RepositoryPaths = repositoryPaths?.ToArray();
            Branches = branches?.ToHashSet();
        }

        /// <summary>
        /// The <see cref="Matcher"/> corresponding to the <see cref="Globbing"/> parameter
        /// passed to the constructor.
        ///
        /// The matcher can be null in case every file will pass the filter.
        ///
        /// If the matcher is not null, a file will only pass the filter if it fulfills at least
        /// one inclusion criterion and does not fulfill any of the exclusion criteria.
        /// </summary>
        public Matcher Matcher { get; set; }

        /// <summary>
        /// Directory paths relative to the root of the repository, where the forward slash is
        /// used as a delimiter between directories.
        ///
        /// If this value is null or empty, the files of all subdirectories in the repository
        /// will be considered. Otherwise, a file must exist in one of the subdirectories
        /// in <see cref="RepositoryPaths"/> to pass the filter.
        /// </summary>
        public string[] RepositoryPaths;

        /// <summary>
        /// The set of branches whose files are to be considered.
        ///
        /// If null or empty, all currently existing branches will be considered. Otherwise
        /// only files that exist in at least one of those branches will pass the filter.
        /// </summary>
        public HashSet<string> Branches;
    }
}
