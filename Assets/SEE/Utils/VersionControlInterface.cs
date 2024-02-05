/// <summary>
/// The interface for the version control systems, which holds
/// the methods every version control system that we use must implement.
/// </summary>
public interface IVersionControl
    {
        /// <summary>
        /// Returns the source code from the revision being compared against.
        /// </summary>
        /// <param name="repositoryPath">The repository, which contains the commit.</param>
        /// <param name="fileName">The file, which contains the content.</param>
        /// <param name="oldCommitID">The commit being compared against.</param>
        /// <returns>the source code from the revision being compared against.</returns>
        string Show(string repositoryPath, string fileName, string oldCommitID);

        /// <summary>
        /// Returns the source code from the revision, that gets compared.
        /// </summary>
        /// <param name="repositoryPath">The repository, which contains the commit.</param>
        /// <param name="fileName">The file, which contains the content.</param>
        /// <param name="oldCommitID">The commit being compared against.</param>
        /// <param name="newCommitID">The commit that gets compared.</param>
        /// <returns>the source code from the revision, that gets compared.</returns>
        string ShowOriginal(string repositoryPath, string fileName, string oldCommitID, string newCommitID);

        /// <summary>
        /// Returns the updated filename.
        /// When a renaming took place, the new name gets returned.
        /// When a file got deleted, the original filename gets returned.
        /// Otherwise null gets returned.
        /// </summary>
        /// <returns>the updated filename</returns>
        string ShowName();
        // Add other version control operations as needed
    }

