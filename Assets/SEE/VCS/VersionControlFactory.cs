using System;

namespace SEE.VCS
{
    /// <summary>
    /// Yields the version control system.
    /// </summary>
    public static class VersionControlFactory
    {
        /// <summary>
        /// Returns the functionality of the given version control system.
        /// </summary>
        /// <param name="system">the version control system</param>
        /// <returns>the functionality for the given version control system</returns>
        public static IVersionControl GetVersionControl(string system)
        {
            return system.ToLower() switch
            {
                "git" => new GitVersionControl(),
                // Add cases for other version control systems
                _ => throw new ArgumentException("Unsupported version control system", nameof(system)),
            };
        }
    }
}
