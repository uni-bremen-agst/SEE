using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// Handles path globbings.
    /// </summary>
    public static class PathGlobbing
    {
        /// <summary>
        /// Filters <paramref name="paths"/> using the include/exclude patterns given
        /// in <paramref name="pathGlobbing"/>.
        ///
        /// If <paramref name="pathGlobbing"/> is null, <paramref name="paths"/> will be returned.
        ///
        /// Otherwise, a path in <paramref name="paths"/> will be returned if it fulfills at least
        /// one inclusion and does not fulfill any exclusion criteria of <paramref name="pathGlobbing"/>.
        /// Consequently, if <paramref name="pathGlobbing"/> is empty or has no inclusion
        /// criterion, the empty list will be returned.
        ///
        /// If <paramref name="pathGlobbing"/>[i].Value is true, the Key is considered
        /// an inclusion pattern, otherwise as an exclusion pattern.
        ///
        /// The syntax of globbings is according to <see cref="Microsoft.Extensions.FileSystemGlobbing"/>.
        /// </summary>
        /// <param name="paths">paths to be filtered</param>
        /// <param name="pathGlobbing">the include/exclude patterns</param>
        /// <returns>matching files</returns>
        /// <exception cref="System.ArgumentNullException">in case <paramref name="paths"/> is null</exception>
        public static IList<string> Filter(IList<string> paths, IDictionary<string, bool> pathGlobbing = null)
        {
            if (paths == null)
            {
                throw new System.ArgumentNullException(nameof(paths));
            }
            if (pathGlobbing == null)
            {
                return paths;
            }
            return Filter(paths, ToMatcher(pathGlobbing));
        }

        /// <summary>
        /// Filters <paramref name="paths"/> using the include/exclude patterns given
        /// in <paramref name="matcher"/>.
        ///
        /// If <paramref name="matcher"/> is null, <paramref name="paths"/> will be returned.
        ///
        /// Otherwise, a path in <paramref name="paths"/> will be returned if it fulfills at least
        /// one inclusion and does not fulfill any exclusion criteria of <paramref name="matcher"/>.
        /// Consequently, if <paramref name="matcher"/> is empty or has no inclusion criterion,
        /// the empty list will be returned.
        /// </summary>
        /// <param name="paths">all files, unfiltered</param>
        /// <param name="matcher">the <see cref="Matcher"/> to be applied</param>
        /// <returns>filtered files</returns>
        /// <exception cref="System.ArgumentNullException">in case <paramref name="paths"/> is null</exception>
        public static IList<string> Filter(IList<string> paths, Matcher matcher = null)
        {
            if (paths == null)
            {
                throw new System.ArgumentNullException(nameof(paths));
            }
            if (matcher == null)
            {
                return paths;
            }

            List<string> result = new();

            foreach (string file in paths)
            {
                if (Matches(matcher, file))
                {
                    result.Add(file);
                }
            }

            return result;
        }

        /// <summary>
        /// True if and only if <paramref name="path"/> matches the inclusion/exclusion
        /// criteria of given <paramref name="matcher"/>.
        /// </summary>
        /// <param name="matcher">the <see cref="Matcher"/> to be applied</param>
        /// <param name="path">the path to be matched</param>
        /// <returns>true if <paramref name="path"/> fulfills at least one inclusion and
        /// does not fulfill any exclusion criteria of <paramref name="matcher"/></returns>
        public static bool Matches(Matcher matcher, string path)
        {
            return matcher.Match(path).HasMatches;
        }

        /// <summary>
        /// Yields a <see cref="Matcher"/> for the given <paramref name="pathGlobbing"/>.
        /// If <paramref name="pathGlobbing"/>[i].Value is true, the Key is considered
        /// an inclusion pattern, otherwise as an exclusion pattern.
        /// </summary>
        /// <param name="pathGlobbing">the path globbing for inclusion/exclusion</param>
        /// <returns>the corresponding <see cref="Matcher"/></returns>
        public static Matcher ToMatcher(IDictionary<string, bool> pathGlobbing)
        {
            Matcher matcher = new();

            foreach (KeyValuePair<string, bool> pattern in pathGlobbing)
            {
                if (pattern.Value)
                {
                    matcher.AddInclude(pattern.Key);
                }
                else
                {
                    matcher.AddExclude(pattern.Key);
                }
            }

            return matcher;
        }
    }
}
