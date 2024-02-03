using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibGit2Sharp;
using System.Linq;
using DiffMatchPatch;
using System.IO;

/// <summary>
/// Contains all version control systems as subclasses, which derives from IVersionControl,
/// so that all have the same functionality.
/// </summary>
public class VersionControlSystems : MonoBehaviour
{
    /// <summary>
    /// Implements the functionality of IVersionControl for Git.
    /// </summary>
    public class GitVersionControl : IVersionControl
    {
        /// <summary>
        /// The name of the file, when it got deleted, or renamed.
        /// </summary>
        private string name = null;

        /// <summary>
        /// Returns the source code from the revision being compared against.
        /// </summary>
        /// <returns>the source code from the revision being compared against.</returns>
        public string Show(string repositoryPath, string fileName, string commitID)
        {
            Repository repo = new(repositoryPath);
            // Get file from commit
            Blob blob = repo.Lookup<Blob>($"{commitID}:{Path.GetRelativePath(repositoryPath, fileName).Replace("\\", "/")}");

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
        /// Returns the source code from the revision, that gets compared.
        /// </summary>
        /// <returns>the source code from the revision, that gets compared.</returns>
        public string ShowOriginal(string repositoryPath, string fileName, string oldCommitID, string newCommitID)
        {
            Repository repo = new(repositoryPath);
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
            var changes = repo.Diff.Compare<TreeChanges>(oldCommit.Tree, newCommit.Tree, compareOptions)
                .Where(change => (change.Path == fileName || change.OldPath == fileName));

            foreach (TreeEntryChanges change in changes)
            {
                // Case: file got renamed.
                if ((change.Status == ChangeKind.Renamed || change.Status == ChangeKind.Copied) &&
                    (change.Path == fileName || change.OldPath == fileName))
                {
                    // Change the filename in the codewindow.
                    name = change.Path;
                    // Get renamed file from commit.
                    var renamedBlob = newCommit[change.Path]?.Target as Blob;
                    if (renamedBlob != null)
                    {
                        return renamedBlob.GetContentText();
                    }
                }
                // Case: File got deleted.
                if (change.Status == ChangeKind.Deleted && change.Path == fileName)
                {
                    // Mark filename in the codewindow as deleted.
                    name = fileName;
                    return "";
                }
            }
            Blob blob = newCommit[fileName]?.Target as Blob;
            return blob.GetContentText();
        }

        /// <summary>
        /// Returns the updated filename.
        /// When a renaming took place, the new name gets returned.
        /// When a file got deleted, the original filename gets returned.
        /// Otherwise null gets returned.
        /// </summary>
        /// <returns>the updated filename</returns>
        public string ShowName()
        {
            return name;
        }
    }
        /// <summary>
        /// Implements the functionality of IVersionControl for SVN.
        /// </summary>
        public class SvnVersionControl : IVersionControl
        {
            /// <summary>
            /// Returns the source code from the revision being compared against.
            /// </summary>
            /// <returns>the source code from the revision being compared against.</returns>
            public string Show(string repositoryPath, string fileName, string oldCommitID)
            {
                // Implement SVN-specific logic here
                // Maybe use SharpSvn
                return $"SVN show {fileName}";
            }
            
            /// <summary>
            /// Returns the source code from the revision, that gets compared.
            /// </summary>
            /// <returns>the source code from the revision, that gets compared.</returns>
            public string ShowOriginal(string repositoryPath, string fileName, string oldCommitID, string newCommitID)
            {
                // Implement SVN-specific logic here
                // Maybe use SharpSvn
                return $"SVN show {fileName}";
            }
            
            /// <summary>
            /// Returns the updated filename.
            /// When a renaming took place, the new name gets returned.
            /// When a file got deleted, the original filename gets returned.
            /// Otherwise null gets returned.
            /// </summary>
            /// <returns>the updated filename</returns>
            public string ShowName() { return null; }
        }

        // Add more version control classes as needed

}