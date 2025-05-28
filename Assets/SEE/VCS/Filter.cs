using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// A filter for VCS queries.
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    public class Filter
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="globbing">the inclusion/exclusion file globbings</param>
        /// <param name="repositoryPaths">local paths starting at the root of the repository</param>
        /// <param name="branches">the name of the branches</param>
        public Filter(Globbing globbing = null, IEnumerable<string> repositoryPaths = null, IEnumerable<string> branches = null)
        {
            Globbing = globbing;
            RepositoryPaths = repositoryPaths?.ToArray();
            Branches = branches?.ToHashSet();
        }

        /// <summary>
        /// The inclusion/exclusion file globbings for relevant files in the repository.
        /// Can be null or empty, in which case all files will pass the filter.
        /// </summary>
        /// <summary>
        /// The list of file globbings for file inclusion/exclusion.
        /// The key is the globbing pattern and the value is the inclusion status.
        /// If the latter is true, the pattern is included, otherwise it is excluded.
        /// </summary>
        /// <remarks>We use <see cref="Dictionary{TKey, TValue}"/> rather than
        /// <see cref="IDictionary{TKey, TValue}"/> because otherwise our config I/O
        /// would not work.</remarks>
        [OdinSerialize]
        [ShowInInspector, ListDrawerSettings(ShowItemCount = true),
        Tooltip("Path globbings and whether they are inclusive (true) or exclusive (false)."),
         HideReferenceObjectPicker]
         public Globbing Globbing = new()
         {
            { "**/*", true }
         };

        /// <summary>
        /// The <see cref="Matcher"/> corresponding to the <see cref="Globbing"/> parameter
        /// passed to the constructor.
        ///
        /// The matcher can be null in case every file will pass the filter.
        ///
        /// If the matcher is not null, a file will only pass the filter if it fulfills at least
        /// one inclusion criterion and does not fulfill any of the exclusion criteria.
        /// </summary>
        public Matcher Matcher => PathGlobbing.ToMatcher(Globbing);

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

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="Globbing"/> in the configuration file.
        /// </summary>
        private const string globbingLabel = "Globbing";

        /// <summary>
        /// Label of attribute <see cref="RepositoryPaths"/> in the configuration file.
        /// </summary>
        private const string repositoryPathsLabel = "RepositoryPaths";

        /// <summary>
        /// Label of attribute <see cref="Branches"/> in the configuration file.
        /// </summary>
        private const string branchesLabel = "Branches";

        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Globbing, globbingLabel);
            writer.Save(RepositoryPaths, repositoryPathsLabel);
            writer.Save(Branches, branchesLabel);
            writer.EndGroup();
        }

        public void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                ConfigIO.Restore(attributes, globbingLabel, ref Globbing);
                ConfigIO.Restore(attributes, repositoryPathsLabel, ref RepositoryPaths);
                ConfigIO.Restore(attributes, branchesLabel, ref Branches);
            }
        }

        #endregion
    }
}
