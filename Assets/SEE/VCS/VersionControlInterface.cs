namespace SEE.VCS
{
    /// <summary>
    /// The interface for the version control systems, which holds
    /// the methods every version control system that we use must implement.
    /// </summary>
    internal interface IVersionControl
    {
        /// <summary>
        /// Returns the source code from the revision being compared against.
        /// </summary>
        /// <param name="repositoryPath">The repository, which contains the commit.</param>
        /// <param name="fileName">The file, which contains the content.</param>
        /// <param name="oldCommitID">The commit being compared against.</param>
        /// <returns>the source code from the revision being compared against.</returns>
        public string Show(string fileName, string oldCommitID);

        /// <summary>
        /// Returns the source code from the revision, that gets compared.
        /// </summary>
        /// <param name="repositoryPath">The repository, which contains the commit.</param>
        /// <param name="fileName">The file, which contains the content.</param>
        /// <param name="oldCommitID">The commit being compared against.</param>
        /// <param name="newCommitID">The commit that gets compared.</param>
        /// <returns>the source code from the revision, that gets compared.</returns>
        public string ShowOriginal(string fileName, string oldCommitID, string newCommitID, out string oldFileName);

        public Change GetFileChange(string fileName, string oldCommitID, string newCommitID, out string oldFilename);
    }
}
