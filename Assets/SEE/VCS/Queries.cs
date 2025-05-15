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

    }
}
