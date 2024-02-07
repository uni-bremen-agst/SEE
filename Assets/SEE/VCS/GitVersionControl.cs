using LibGit2Sharp;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Implements the functionality of <see cref="IVersionControl"/> for Git.
    /// </summary>
    internal class GitVersionControl : IVersionControl
    {
        /// <summary>
        /// Constructor setting up the repository access.
        /// </summary>
        /// <param name="repositoryPath">the path to a Git repository</param>
        internal GitVersionControl(string repositoryPath)
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

        /// <summary>
        /// Returns true if the repository has a file named <paramref name="fileName"/>
        /// in the revision with the <paramref name="newCommitID"/>.
        ///
        /// If true, the name of that file in the revision with <paramref name="oldCommitID"/>
        /// will be contained in <paramref name="oldFilename"/>. The value of <paramref name="oldFilename"/>
        /// will be empty, if the file did not exist in the <paramref name="oldCommitID"/>.
        ///
        /// If false, <paramref name="oldFilename"/> will be undefined.
        /// <paramref name="oldFilename"/> will be name of that file in the <paramref name="oldCommitID"/>
        /// or the empty string
        ///
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="oldCommitID"></param>
        /// <param name="newCommitID"></param>
        /// <param name="oldFilename"></param>
        /// <returns></returns>
        /// <exception cref="UnknownCommitID"></exception>
        public Change GetFileChange(string fileName, string oldCommitID, string newCommitID, out string oldFilename)
        {
            Commit newCommit = repo.Lookup<Commit>(newCommitID);
            if (newCommit == null)
            {
                throw new UnknownCommitID($"Unknown commit id {newCommit} for git repository at {repositoryPath}");
            }

            Commit oldCommit = repo.Lookup<Commit>(oldCommitID);
            if (oldCommit == null)
            {
                throw new UnknownCommitID($"Unknown commit id {oldCommit} for git repository at {repositoryPath}");
            }

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
                .Where(change => change.Path == fileName || change.OldPath == fileName);

            oldFilename = null;

            int numberOfIterations = 0;
            // Note: changes.Count can only be 0 (no change what so ever) or 1.
            // We are still using a loop to obtain the change even though the loop
            // has only one iteration at most.
            foreach (TreeEntryChanges change in changes)
            {
                numberOfIterations++;
                Dump(change);
                Assert.IsTrue(change.Path == fileName || change.OldPath == fileName);
                switch (change.Status)
                {
                    case ChangeKind.Unmodified:
                        oldFilename = change.Path;
                        return Change.Unmodified;

                    case ChangeKind.Added:
                        // File was added in the newer commit.
                        return Change.Added;

                    case ChangeKind.Deleted:
                        // File was deleted in the newer commit, but existed in the earlier commit.
                        oldFilename = change.OldPath;
                        return Change.Deleted;

                    case ChangeKind.Modified:
                        // File was modified in the newer commit.
                        oldFilename = change.Path;
                        Assert.AreEqual(fileName, change.OldPath);
                        return Change.Modified;

                    case ChangeKind.Renamed:
                        // File was renamed in the newer commit.
                        oldFilename = change.OldPath;
                        return Change.Renamed;

                    case ChangeKind.Copied:
                        oldFilename += change.OldPath;
                        return Change.Copied;

                    case ChangeKind.Ignored:
                        return Change.Ignored;

                    case ChangeKind.Untracked:
                        return Change.Untracked;

                    case ChangeKind.TypeChanged:
                        oldFilename = change.Path;
                        return Change.TypeChanged;

                    case ChangeKind.Unreadable:
                        return Change.Unreadable;

                    case ChangeKind.Conflicted:
                        oldFilename = change.OldPath;
                        return Change.Conflicted;

                    default:
                        throw new System.NotImplementedException($"Unhandled change status: {change.Status}");
                }
            }
            Assert.IsTrue(numberOfIterations <= 1);
            return Change.Unknown;
        }

        private void Dump(TreeEntryChanges c)
        {
            Debug.Log($"Path={c.Path} OldPath={c.OldPath} Status={c.Status} Exists={c.Exists}\n");
        }
    }
}
