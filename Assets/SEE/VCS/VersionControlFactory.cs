using System;

namespace SEE.VCS
{
    /// <summary>
    /// Yields the version control system.
    /// </summary>
    internal static class VersionControlFactory
    {
        /// <summary>
        /// Returns the functionality of the given version control system.
        /// </summary>
        /// <param name="vcsKind">the version control system</param>
        /// <param name="repositoryPath">the path to the root of the repository</param>
        /// <returns>the functionality for the given version control system</returns>
        internal static IVersionControl GetVersionControl(VCSKind vcsKind, string repositoryPath)
        {
            return vcsKind switch
            {
                VCSKind.Git => new GitVersionControl(repositoryPath),
                // Add cases for other version control systems
                _ => throw new ArgumentException("Unsupported version control system", nameof(vcsKind)),
            };
        }
    }
}
