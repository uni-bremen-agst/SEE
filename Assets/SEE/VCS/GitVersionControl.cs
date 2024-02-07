using LibGit2Sharp;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace SEE.VCS
{
    /// <summary>
    /// Implements the functionality of <see cref="IVersionControl"/> for Git.
    /// </summary>
    public class GitVersionControl : IVersionControl
    {
        /// <summary>
        /// Constructor setting up the repository access.
        /// </summary>
        /// <param name="repositoryPath">the path to a Git repository</param>
        public GitVersionControl(string repositoryPath)
        {
            this.repositoryPath = repositoryPath;
            repo = new(repositoryPath);
        }

        private readonly Repository repo;

        private readonly string repositoryPath;

        /// <summary>
        /// Retrieves and returns the source code of the file with given <paramref name="fileName"/>
        /// in the version control system for the given <paramref name="revisionID"/>.
        /// In case that file does not exist, the empty string will be returned.
        /// </summary>
        /// <param name="fileName">The file whose content is to be retrieved; must be relative to the root of the repository</param>
        /// <param name="revisionID">The identifier of the revision for which the file should be looked up</param>
        /// <returns>the content of <paramref name="fileName"/> at <paramref name="revisionID"/> or empty if
        /// the file does not exist</returns>
        public string Show(string fileName, string revisionID)
        {
            // Get file from commit
            Blob blob = repo.Lookup<Blob>($"{revisionID}:{Path.GetRelativePath(repositoryPath, fileName).Replace("\\", "/")}");

            if (blob != null)
            {
                return blob.GetContentText();
            }
            else
            {
                // File got added and is only available in the other commit.
                return "";
            }
        }

        /// <summary>
        /// Returns the source code from the <see cref="newCommitID"> that gets compared against the <see cref="oldCommitID">.
        /// </summary>
        /// <param name="fileName">The file, which contains the content.</param>
        /// <param name="oldCommitID">The commit being compared against.</param>
        /// <param name="newCommitID">The commit that gets compared.</param>
        /// <returns>the source code from the revision, that gets compared.</returns>
        public string ShowOriginal(string fileName, string oldCommitID, string newCommitID, out string oldFileName)
        {
            oldFileName = fileName;
            Commit newCommit = repo.Lookup<Commit>(newCommitID);
            Commit oldCommit = repo.Lookup<Commit>(oldCommitID);

            CompareOptions compareOptions = new()
            {
                Algorithm = DiffAlgorithm.Myers,
                Similarity = new SimilarityOptions
                {
                    RenameDetectionMode = RenameDetectionMode.Default,
                }
            };

            // Compare the commits
            IEnumerable<TreeEntryChanges> changes = repo.Diff.Compare<TreeChanges>(oldCommit.Tree, newCommit.Tree, compareOptions)
                .Where(change => (change.Path == fileName || change.OldPath == fileName));

            foreach (TreeEntryChanges change in changes)
            {
                // Case: file got renamed.
                if ((change.Status == ChangeKind.Renamed || change.Status == ChangeKind.Copied)
                     && (change.Path == fileName || change.OldPath == fileName))
                {
                    // Change the filename.
                    oldFileName = change.Path;
                    // Get renamed file from commit.
                    Blob renamedBlob = newCommit[change.Path]?.Target as Blob;
                    if (renamedBlob != null)
                    {
                        return renamedBlob.GetContentText();
                    }
                }
                // Case: File got deleted.
                if (change.Status == ChangeKind.Deleted && change.Path == fileName)
                {
                    // Mark filename as deleted.
                    oldFileName = fileName;
                    return "";
                }
            }
            Blob blob = newCommit[fileName]?.Target as Blob;
            return blob.GetContentText();
        }
    }
}
