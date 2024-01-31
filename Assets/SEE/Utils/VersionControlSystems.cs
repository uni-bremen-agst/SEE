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
        string name = null;
        public string Show(string repositoryPath, string fileName, string commitID)
        {
            using (var repo = new Repository(repositoryPath))
            {
                // Get file from commit
                var blob = repo.Lookup<Blob>($"{commitID}:{Path.GetRelativePath(repositoryPath, fileName).Replace("\\","/")}");

                if (blob != null)
                {
                    // Get content from file
                    var content = blob.GetContentText();
                    return content;
                }
                else
                {
                    // FIXME: Just for debugging.
                    return $"{commitID}:{Path.GetRelativePath(repositoryPath, fileName).Replace("\\", "/")}";
                }
            }
        }
        public string ShowOriginal(string repositoryPath, string fileName, string oldCommitID, string newCommitID)
        {
            var repo = new Repository(repositoryPath);
            // Get commit, to find possible renames.
            var newCommit = repo.Lookup<Commit>(newCommitID);
            var oldCommit = repo.Lookup<Commit>(oldCommitID);

            // Set up the diff options.
            var compareOptions = new CompareOptions
            {
                Algorithm = DiffAlgorithm.Myers,
                Similarity = new SimilarityOptions
                {
                    RenameDetectionMode = RenameDetectionMode.CopiesHarder,
                    RenameThreshold = 20,
                    BreakRewriteThreshold = 70,
                    RenameFromRewriteThreshold = 99,
                    CopyThreshold = 90,
                    RenameLimit = 1,
                }
            };
            // Compare the commits
            var changes = repo.Diff.Compare<TreeChanges>(oldCommit.Tree, newCommit.Tree, compareOptions);
            foreach (TreeEntryChanges change in changes)
            {
                // FIXME: Just for debugging.
                string changeInfo = change.Status.ToString();
                string changeInff = change.OldPath.ToString();
                string changeInf = change.Path.ToString();
                string a = change.Exists.ToString();
                string b = change.OldExists.ToString();
                Debug.Log(changeInfo);
                Debug.Log(changeInff);
                Debug.Log(changeInf);
                Debug.Log(a);
                Debug.Log(b);
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
                // Case: File got Added.
                if (change.Status == ChangeKind.Added && change.Path == fileName)
                {
                    return "";
                }
                // Case: File got deleted.
                if (change.Status == ChangeKind.Deleted && change.Path == fileName)
                {
                    return "";
                }
                // Case: File is unmodified.
                if (change.Status == ChangeKind.Unmodified && change.Path == fileName)
                {
                    var blob = newCommit[fileName]?.Target as Blob;
                    return blob.GetContentText();
                }
                // Case: File got modified.
                if (change.Status == ChangeKind.Modified && change.Path == fileName)
                {
                    var changedBlob = newCommit[change.Path]?.Target as Blob;
                    return changedBlob.GetContentText();
                }
            }
            // FIXME: Just for debugging.
            return fileName;
        }

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
            public string Show(string repositoryPath, string fileName, string oldCommitID)
            {
                // Implement SVN-specific logic here
                // Maybe use SharpSvn
                return $"SVN show {fileName}";
            }
            public string ShowOriginal(string repositoryPath, string fileName, string oldCommitID, string newCommitID)
            {
                // Implement SVN-specific logic here
                // Maybe use SharpSvn
                return $"SVN show {fileName}";
            }
            public string ShowName() { return null; }
        }

        // Add more version control classes as needed

}