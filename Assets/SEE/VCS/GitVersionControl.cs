using LibGit2Sharp;
using System.Linq;
using System;
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
        /// <param name="repositoryPath">The path to a Git repository.</param>
        /// <exception cref="ArgumentException">Thrown in case <paramref name="repositoryPath"/>
        /// is null or just whitespace.</exception>
        internal GitVersionControl(string repositoryPath)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                throw new ArgumentException("Repository path must not be null or empty.");
            }
            this.repositoryPath = repositoryPath;
            repo = new(repositoryPath);
        }

        /// <summary>
        /// The repository.
        /// </summary>
        private readonly Repository repo;

        /// <summary>
        /// The path to the root of the repository.
        /// </summary>
        private readonly string repositoryPath;

        /// <summary>
        /// See <see cref="IVersionControl.Show(string, string)"/>.
        /// </summary>
        /// <param name="pathInRepository">The path of the file to be shown relative to the root 
        /// of the repository; must be in the syntax of the underlying type of repository (here: git,
        /// where forward slashes are used to separate path elements).</param>
        /// <param name="commitID">The commit ID to show the file from.</param>
        /// <returns>The content of the file at the given path in the given commit or
        /// the empty string if the file does not exist (in which case a warning
        /// is emitted).</returns>
        /// <exception cref="ArgumentException">Thrown in case <paramref name="pathInRepository"/>
        /// or <paramref name="commitID"/> are null or just whitespace.</exception>
        public string Show(string pathInRepository, string commitID)
        {
            if (string.IsNullOrWhiteSpace(pathInRepository))
            {
                throw new ArgumentException("Path must not be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(commitID))
            {
                throw new ArgumentException("Commit ID must not be null or empty.");
            }
            
            Blob blob = repo.Lookup<Blob>($"{commitID}:{pathInRepository}");

            if (blob != null)
            {
                return blob.GetContentText();
            }
            else
            {
                // File does not exist.
                Debug.LogWarning($"Looking up file {pathInRepository} in revision {commitID} of git repository at {repositoryPath} failed.\n");
                return "";
            }
        }

        /// <summary>
        /// See <see cref="IVersionControl.GetFileChange(string, string, string, out string)"/>.
        /// </summary>
        public Change GetFileChange(string pathInRepository, string oldCommitID, string newCommitID, out string oldFilename)
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
                .Where(change => change.Path == pathInRepository || change.OldPath == pathInRepository);

            Change result = Change.Unknown;
            oldFilename = null;

            int numberOfIterations = 0;
            // Note: changes.Count can only be 0 (no change what so ever) or 1.
            // We are still using a loop to obtain the change even though the loop
            // has only one iteration at most.
            foreach (TreeEntryChanges change in changes)
            {
                numberOfIterations++;
                // Dump(change);
                Assert.IsTrue(change.Path == pathInRepository || change.OldPath == pathInRepository);
                switch (change.Status)
                {
                    case ChangeKind.Unmodified:
                        oldFilename = change.Path;
                        result = Change.Unmodified;
                        break;

                    case ChangeKind.Added:
                        // File was added in the newer commit.
                        result = Change.Added;
                        break;

                    case ChangeKind.Deleted:
                        // File was deleted in the newer commit, but existed in the earlier commit.
                        oldFilename = change.OldPath;
                        result = Change.Deleted;
                        break;

                    case ChangeKind.Modified:
                        // File was modified in the newer commit.
                        oldFilename = change.Path;
                        Assert.AreEqual(pathInRepository, change.OldPath);
                        result = Change.Modified;
                        break;

                    case ChangeKind.Renamed:
                        // File was renamed in the newer commit.
                        oldFilename = change.OldPath;
                        result = Change.Renamed;
                        break;

                    case ChangeKind.Copied:
                        oldFilename = change.OldPath;
                        result = Change.Copied;
                        break;

                    case ChangeKind.Ignored:
                        result = Change.Ignored;
                        break;

                    case ChangeKind.Untracked:
                        result = Change.Untracked;
                        break;

                    case ChangeKind.TypeChanged:
                        oldFilename = change.Path;
                        result = Change.TypeChanged;
                        break;

                    case ChangeKind.Unreadable:
                        result = Change.Unreadable;
                        break;

                    case ChangeKind.Conflicted:
                        oldFilename = change.OldPath;
                        result = Change.Conflicted;
                        break;

                    default:
                        throw new System.NotImplementedException($"Unhandled change status: {change.Status}");
                }
            }
            // FIXME (#703): This may or may not be removed.
            Assert.IsTrue(numberOfIterations <= 1);
            return result;
        }

        /// <summary>
        /// Dumps <paramref name="c"/>. Can be used for debugging.
        /// </summary>
        /// <param name="c">Change to be dumped.</param>
        private void Dump(TreeEntryChanges c)
        {
            Debug.Log($"Path={c.Path} OldPath={c.OldPath} Status={c.Status} Exists={c.Exists}\n");
        }
    }
}
