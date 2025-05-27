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
        /// Yields the canonical name of all branches in <paramref name="repository"/>.
        /// </summary>
        /// <param name="repository"></param>
        /// <returns>canonical name of all branches</returns>
        public static IEnumerable<string> AllBranches(this Repository repository)
        {
            return repository.Branches.Select(b => b.CanonicalName);
        }

        /// <summary>
        /// Yields all distinct file paths of the given <paramref name="repository"/>
        /// that fulfill the given <paramref name="filter"/>. If <paramref name="filter"/>
        /// is null, all files of all branches will be retrieved.
        ///
        /// If <paramref name="filter"/> is different from null, the constraints for a
        /// file to be reported are as follows:
        ///
        /// If attribute <see cref="Filter.Branches"/> of <paramref name="filter"/>
        /// is null or empty, the file can be contained in any of the current branches of the <paramref name="repository"/>.
        /// Otherwise, a file must be contained in at leasts one of the branches described in attribute
        /// <see cref="Filter.Branches"/>. The actual matching is done using the <see cref="Filter.Matches(Branch)"/>,
        /// thus, a branch is considered only if its FriendlyName is matched by at least one of the
        /// regular expressions in <see cref="Filter.Branches"/>.
        ///
        /// If attribute <see cref="Filter.RepositoryPaths"/> is null or empty, every file in
        /// the repository will be considered. Otherwise only the files contained in any of
        /// the subdirectories of the <paramref name="repository"/> listed in the <see cref="Filter.RepositoryPaths"/>.
        ///
        /// If attribute <see cref="Filter.Matcher"/> is null, a file path can take on any name.
        /// Otherwise a file will be reported only if it fulfills at least on inclusive criterion
        /// of <see cref="Filter.Matcher"/> and does not violate any exclusive criterion in
        /// <see cref="Filter.Matcher"/>.
        /// </summary>
        /// <param name="repository">the repository from which to retrieve the paths</param>
        /// <param name="filter">the filter to be used to retrieve the files</param>
        /// <returns>all distinct file paths</returns>
        public static IEnumerable<string> AllFiles(this Repository repository, Filter filter = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            HashSet<string> result = new();
            foreach (Branch branch in repository.Branches)
            {
                if (filter == null || filter.Matches(branch))
                {
                    AllFiles(branch.Tip.Tree, filter, result);
                }
            }
            return result;
        }

        /// <summary>
        /// Yields all distinct file paths of the given <paramref name="tree"/>
        /// that fulfill the given <paramref name="filter"/>. If <paramref name="filter"/>
        /// is null, all files of <paramref name="tree"/> will be retrieved.
        ///
        /// If <paramref name="filter"/> is different from null, the constraints for a
        /// file to be reported are as follows:
        ///
        /// If attribute <see cref="Filter.RepositoryPaths"/> is null or empty, every file in
        /// the <paramref name="tree"/> will be considered. Otherwise only the files contained in any of
        /// the subdirectories of <paramref name="tree"/> listed in the <see cref="Filter.RepositoryPaths"/>.
        ///
        /// If attribute <see cref="Filter.Matcher"/> is null, a file path can take on any name.
        /// Otherwise a file will be reported only if it fulfills at least on inclusive criterion
        /// of <see cref="Filter.Matcher"/> and does not violate any exclusive criterion in
        /// <see cref="Filter.Matcher"/>.
        /// </summary>
        /// <param name="tree">the tree for which to retrieve the files</param>
        /// <param name="filter">the filter to be used to retrieve the files</param>
        /// <returns>all distinct file paths</returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="tree"/> is null</exception>
        internal static ICollection<string> AllFiles(Tree tree, Filter filter = null)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }
            HashSet<string> result = new();
            AllFiles(tree, filter, result);
            return result;
        }

        /// <summary>
        /// Adds the distinct filenames in the given <paramref name="tree"/> passing
        /// the criteria <see cref="Filter.RepositoryPaths"/> and <see cref="Filter.Matcher"/>
        /// of the given <paramref name="filter"/>.
        /// </summary>
        /// <param name="tree">the tree for which to retrieve the files</param>
        /// <param name="filter">the filter to be used to retrieve the files</param>
        /// <param name="paths">where the passing files are to be added</param>
        /// <exception cref="Exception">thrown if attribute <see cref="Filter.RepositoryPaths"/> of
        /// <paramref name="filter"/> is different from null and at least one of the paths in
        /// <see cref="Filter.RepositoryPaths"/> does not exist in the <paramref name="tree"/>
        /// or does not denote a directory.</exception>
        private static void AllFiles(Tree tree, Filter filter, HashSet<string> paths)
        {
            if (filter.RepositoryPaths == null || filter.RepositoryPaths.Length == 0)
            {
                AllFiles(tree, filter.Matcher, paths);
            }
            else
            {
                foreach (string repositoryPath in filter.RepositoryPaths)
                {
                    if (!string.IsNullOrWhiteSpace(repositoryPath))
                    {
                        AllFiles(Find(tree, repositoryPath), filter.Matcher, paths);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the descendant tree for the given <paramref name="repositoryPath"/>.
        /// <paramref name="repositoryPath"/> is assumed to be a path relative to <paramref name="tree"/>.
        /// The delimiter to separate different subtrees is assumed to be the forward slash.
        /// For instance, "Assets/SEE/UI" identifies the folder UI, nested in folder SEE nested in folder
        /// Assets.
        /// </summary>
        /// <param name="tree">the root tree</param>
        /// <param name="repositoryPath">relative path of descendants nested in <paramref name="tree"/></param>
        /// <returns>the subtree</returns>
        /// <exception cref="Exception">thrown if <paramref name="repositoryPath"/> does not match
        /// any descendant in <paramref name="tree"/> or if <paramref name="repositoryPath"/>
        /// is not a tree (for instance, a blob).</exception>
        private static Tree Find(Tree tree, string repositoryPath)
        {
            TreeEntry result = tree[repositoryPath];
            if (result == null)
            {
                throw new Exception($"The path {repositoryPath} does not exist in the repository.");
            }

            if (result.TargetType == TreeEntryTargetType.Tree)
            {
                return (Tree)result.Target;
            }
            else
            {
                throw new Exception($"The path {repositoryPath} is not a directory in the repository.");
            }
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
        private static void AllFiles(Tree tree, Matcher matcher, HashSet<string> paths)
        {
            foreach (TreeEntry entry in tree)
            {
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
                    AllFiles(subtree, matcher, paths);
                }
            }
        }
    }
}
