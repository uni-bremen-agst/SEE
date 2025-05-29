using LibGit2Sharp;
using System.Collections.Generic;
using System.Linq;

namespace SEE.VCS
{
    /// <summary>
    /// Queries for the VCS.
    /// </summary>
    internal static class Queries
    {
        /// <summary>
        /// Returns the hashes of all tip commits from all branches in the given
        /// repositories (identified by their <paramref name="repositoryPaths"/>).
        /// </summary>
        /// <param name="repositoryPaths">the paths of the repositories to be
        /// queried</param>
        /// <returns>A mapping from a repository path to a list of the hashes of all tip
        /// commits.</returns>
        public static Dictionary<string, List<string>> GetTipHashes(IEnumerable<string> repositoryPaths)
        {
            Dictionary<string, List<string>> result = new();
            foreach (string repoPath in repositoryPaths)
            {
                using Repository repo = new(repoPath);
                result.Add(repoPath, repo.Branches.Select(x => x.Tip.Sha).ToList());
            }
            return result;
        }

    }
}
