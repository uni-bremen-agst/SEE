using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
        ///
        /// The names in this set can be regular expressions.
        /// </summary>
        public HashSet<string> Branches;

        /// <summary>
        /// True if any of the regular expressions in <see cref="Branches"/>
        /// matches a part the FriendlyName of the given <paramref name="branch"/>.
        ///
        /// For instance, if "71" is in <see cref="Branches"/>, then every branch
        /// containing "71" in its FriendlyName will match, such as "feature/710-fix-bug"
        /// or "feature/fix-bug-711".
        /// </summary>
        /// <param name="branch">branch whose FriendlyName is to be matched</param>
        /// <returns>true if there is at least one regular expressions
        /// in <see cref="Branches"/> matches the FriendlyName of
        /// <paramref name="branch"/></returns>
        public bool Matches(Branch branch)
        {
            if (Branches == null || Branches.Count == 0)
            {
                return true; // all branches match
            }
            return Matches(branch.FriendlyName);

            bool Matches(string branchName)
            {
                return Branches.Any(b => Regex.IsMatch(branchName, b));
            }
        }
    }
}
