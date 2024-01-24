using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibGit2Sharp;
using System.Linq;
using DiffMatchPatch;

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
        public string Show(string repositoryPath, string fileName, string oldCommitID, string newCommitID)
        {
            using (var repo = new Repository(repositoryPath))
            {
                // Get commit.
                var oldCommit = repo.Lookup<Commit>(oldCommitID);


                // Get file from commit
                var blob = oldCommit[fileName]?.Target as Blob;

                if (blob != null)
                {
                    // Get content from file
                    var content = blob.GetContentText();
                    return content;
                }
                else
                {
                    // Get commit, to find possible renames.
                    var newCommit = repo.Lookup<Commit>(newCommitID);

                    // Set up the diff options.
                    var compareOptions = new CompareOptions
                    {
                        Similarity = new SimilarityOptions
                        {
                            RenameDetectionMode = RenameDetectionMode.Default,
                            RenameThreshold = 50,
                        }
                    };

                    // Compare the commits
                    var changes = repo.Diff.Compare<TreeChanges>(newCommit.Tree, oldCommit.Tree, compareOptions);
                    foreach (TreeEntryChanges change in changes)
                    {
                        string changeInfo = change.Status.ToString();
                        string changeInff = change.OldPath.ToString();
                        string changeInf = change.Path.ToString();
                        Debug.Log(changeInfo);
                        Debug.Log(changeInff);
                        Debug.Log(changeInf);
                        if ((change.Status == ChangeKind.Renamed || change.Status == ChangeKind.TypeChanged) &&
                            (change.Path == fileName || change.OldPath == fileName))
                        {
                            name = change.Path;
                            // Get renamed file from commit.
                            var renamedBlob = oldCommit[change.Path]?.Target as Blob;
                            if (renamedBlob != null)
                            {
                                return renamedBlob.GetContentText();
                            }
                        }
                    }
                    return "Datei nicht gefunden.";
                }
            }
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
            public string Show(string repositoryPath, string fileName, string oldCommitID, string newCommitID)
            {
                // Implement SVN-specific logic here
                // Maybe use SharpSvn
                return $"SVN show {fileName}";
            }
            
            public string ShowName() { return null; }
        }

        // Add more version control classes as needed

}