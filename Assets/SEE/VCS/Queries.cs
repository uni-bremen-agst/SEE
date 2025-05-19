using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.VCS
{
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
        /// Yields all file paths of all branches of given <paramref name="repository"/>.
        /// </summary>
        /// <param name="repository">the repository from which to retrieve the paths</param>
        /// <returns>all file paths</returns>
        public static IEnumerable<string> AllFiles(this Repository repository)
        {
            return repository.Branches
                .SelectMany(x => ListTree(x.Tip.Tree))
                .Distinct();
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
        /// Gets all paths contained in <paramref name="tree"/>. By the nature
        /// of Git, paths always identify files, not directories.
        /// It is equivalent to "git ls-tree --name-only".
        /// </summary>
        /// <param name="tree">The tree whose files are requested.</param>
        /// <returns>a list of paths.</returns>
        /// <remarks>Trees are collections of files and folders to make up the
        /// repository hierarchy.</remarks>
        public static IList<string> ListTree(Tree tree)
        {
            List<string> fileList = new();

            foreach (TreeEntry entry in tree)
            {
                if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    fileList.Add(entry.Path);
                }
                else if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    Tree subtree = (Tree)entry.Target;
                    fileList.AddRange(ListTree(subtree));
                }
            }

            return fileList;
        }
    }
}
