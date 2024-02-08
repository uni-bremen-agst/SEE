namespace SEE.VCS
{
    /// <summary>
    /// The interface for the version control systems, which holds
    /// the methods every version control system that we use must implement.
    /// </summary>
    internal interface IVersionControl
    {
        /// <summary>
        /// Retrieves and returns the content of the file with given <paramref name="fileName"/>
        /// in the version control system for the given <paramref name="commitID"/>.
        /// In case that file does not exist, the empty string will be returned.
        /// </summary>
        /// <param name="fileName">The file whose content is to be retrieved; must be relative to the root of the repository</param>
        /// <param name="commitID">The identifier of the revision for which the file should be looked up</param>
        /// <returns>the content of <paramref name="fileName"/> at <paramref name="commitID"/> or empty if
        /// the file does not exist</returns>
        public string Show(string fileName, string commitID);

        /// <summary>
        /// Returns the kind of <see cref="Change"/> of the file with <paramref name="fileName"/>
        /// from <paramref name="oldCommitID"/> to <paramref name="newCommitID"/>.
        ///
        /// If the result is <see cref="Change.Unmodified"/> or <see cref="ChangeKind.Modified"/>
        /// or <see cref="ChangeKind.TypeChanged"/>,  <paramref name="oldFilename"/> will be equal to
        /// <paramref name="fileName"/>.
        ///
        /// If the result is <see cref="Change.Deleted"/>, <see cref="Change.Renamed"/>,
        /// or <see cref="Change.Copied"/>, <paramref name="oldFilename"/> will be the name
        /// the file had in <paramref name="oldCommitID"/> and differs from <paramref name="fileName"/>.
        /// Whether <see cref="Change.Copied"/> will ever be returned, depends upon the settings
        /// for the comparison of the two commits.
        ///
        /// In all other cases, <paramref name="oldFilename"/> will be <c>null</c>.
        ///
        /// For clarifications, a file is <see cref="Change.Added"/> if it exists in <paramref name="newCommitID"/>
        /// but not in <paramref name="oldCommitID"/>. Vice versa, a file is <see cref="Change.Deleted"/> if
        /// it exists in <paramref name="oldCommitID"/> but not in <paramref name="newCommitID"/>.
        /// </summary>
        /// <param name="fileName">the file for which to retrieve a change</param>
        /// <param name="oldCommitID">the commit id against which to compare <paramref name="fileName"/>
        /// in <paramref name="newCommitID"/></param>
        /// <param name="newCommitID">the new commit id</param>
        /// <param name="oldFilename">the name <paramref name="fileName"/> had in <paramref name="oldCommitID"/></param>
        /// <returns>the kind of change for the file between the two given commits</returns>
        /// <exception cref="UnknownCommitID">thrown if <paramref name="oldCommitID"/> or
        /// <paramref name="newCommitID"/> are no valid commit id</exception>
        public Change GetFileChange(string fileName, string oldCommitID, string newCommitID, out string oldFilename);
    }
}
