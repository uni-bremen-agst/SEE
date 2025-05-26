using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;

namespace SEE.VCS
{
    /// <summary>
    /// Queries for the VCS.
    /// </summary>
    internal static class Queries
    {
        /// <summary>
        /// Yields all commits (excluding merge commits) after <paramref name="startDate"/>
        /// until today.
        /// </summary>
        /// <param name="repository">the repository from which to retrieve the commits</param>
        /// <param name="startDate">the date after which commits should be retrieved</param>
        /// <returns></returns>
        public static IEnumerable<Commit> CommitsAfter(this Repository repository, DateTime startDate)
        {
            IEnumerable<Commit> commitList = repository.Commits
                .QueryBy(new CommitFilter { IncludeReachableFrom = repository.Branches })
                // Commits after startDate
                .Where(commit =>
                    DateTime.Compare(commit.Author.When.Date, startDate) > 0)
                // Filter out merge commits.
                .Where(commit => commit.Parents.Count() <= 1);
            return commitList;
        }

        /// <summary>
        /// Yields all distinct file paths of all branches of given <paramref name="repository"/>.
        /// </summary>
        /// <param name="repository">the repository from which to retrieve the paths</param>
        /// <param name="pathGlobbing">the inclusion/exclusion path globbings</param>
        /// <returns>all distinct file paths</returns>
        public static IEnumerable<string> AllFiles(this Repository repository, Globbing pathGlobbing = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            HashSet<string> result = new();
            Matcher matcher = PathGlobbing.ToMatcher(pathGlobbing);
            foreach (Branch branch in repository.Branches)
            {
                ListTree(branch.Tip.Tree, matcher, result);
            }
            return result;
        }

        /// <summary>
        /// Yields the canonical name of all branches in <paramref name="repository"/>.
        /// </summary>
        /// <param name="repository"></param>
        /// <returns>canonical name of all branches</returns>
        public static IEnumerable<string> AllBranches(this Repository repository)
        {
            return repository.Branches.Select(b => b.CanonicalName);
        }

        /// <summary>
        /// Gets all distinct repository-relative paths contained in <paramref name="tree"/>.
        ///
        /// If <paramref name="pathGlobbing"/> is null, all paths are returned.
        /// Otherwise, a path in <paramref name="tree"/> will be returned if it fulfills at least one
        /// inclusion and does not fulfill any exclusion criteria of <paramref name="pathGlobbing"/>.
        ///
        /// By the nature of Git, paths always identify files, not directories.
        /// It is equivalent to "git ls-tree --name-only".
        /// </summary>
        /// <param name="tree">The tree whose files are requested.</param>
        /// <param name="pathGlobbing">the inclusion/exclusion path globbings</param>
        /// <returns>a list of paths.</returns>
        /// <remarks>Trees are collections of files and folders to make up the
        /// repository hierarchy.</remarks>
        public static HashSet<string> ListTree(Tree tree, Globbing pathGlobbing = null)
        {
            return ListTree(tree, PathGlobbing.ToMatcher(pathGlobbing));
        }

        /// <summary>
        /// Gets all distinct repository-relative paths contained in <paramref name="tree"/>
        /// fulfilling at least one inclusion and not fulfilling any exclusion criteria
        /// of <paramref name="matcher"/>.
        ///
        /// By the nature of Git, paths always identify files, not directories.
        ///
        /// It is equivalent to "git ls-tree --name-only".
        /// </summary>
        /// <param name="tree">The tree whose files are requested.</param>
        /// <param name="matcher">the inclusion/exclusion path globbings</param>
        /// <returns>the set of distinct paths.</returns>
        /// <remarks>Trees are collections of files and folders to make up the
        /// repository hierarchy.</remarks>
        public static HashSet<string> ListTree(Tree tree, Matcher matcher = null)
        {
            HashSet<string> result = new();
            ListTree(tree, matcher, result);
            return result;
        }

        /// <summary>
        /// Adds all distinct repository-relative paths contained in <paramref name="tree"/>
        /// fulfilling at least one inclusion and not fulfilling any exclusion criteria
        /// of <paramref name="matcher"/>. The search recursively descends into
        /// the subtrees of <paramref name="tree"/>. The paths are added to <paramref name="paths"/>
        ///
        /// By the nature of Git, paths always identify files, not directories.
        ///
        /// It is equivalent to "git ls-tree --name-only".
        /// </summary>
        /// <param name="tree">The tree whose files are requested.</param>
        /// <param name="matcher">the inclusion/exclusion path globbings</param>
        /// <param name="paths">the set of paths to which the paths are to be added</param>
        /// <returns>the set of distinct paths.</returns>
        private static void ListTree(Tree tree, Matcher matcher, HashSet<string> paths)
        {
            TreeEntry e = tree["Assets"];
            if (e == null)
            {
                UnityEngine.Debug.Log("No Assets folder found in the repository.");
            }
            foreach (TreeEntry entry in tree)
            {
                UnityEngine.Debug.Log(entry.Path);
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    if (matcher == null || matcher.Matches(entry.Path))
                    {
                        paths.Add(entry.Path);
                    }
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    Tree subtree = (Tree)entry.Target;
                    ListTree(subtree, matcher, paths);
                }
            }
        }
    }
}
