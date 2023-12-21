using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibGit2Sharp;
using System.Linq;

public class VersionControlSystems : MonoBehaviour
{
    // TODO: Add description.
    public class GitVersionControl : IVersionControl
    {
        public string Show(string repositoryPath, string branchName, string fileName, string commitIdentifier)
        {
            using (var repo = new Repository(repositoryPath))
            {
                // Get commit for given branch
                var branch = repo.Branches[branchName];
                var commit = repo.Commits.Single(c => c.Id.Sha.StartsWith(commitIdentifier) || c.Id.ToString().StartsWith(commitIdentifier));

                // Get file from commit
                var blob = commit[fileName]?.Target as Blob;

                if (blob != null)
                {
                    // Get content from file
                    var content = blob.GetContentText();
                    return content;
                }
                else
                {
                    return "Datei nicht gefunden.";
                }
            }
        }
    }
    // TODO: Add description.
    public class SvnVersionControl : IVersionControl
    {
        public string Show(string repositoryPath, string branchName, string fileName, string commitIdentifier)
        {
            // Implement SVN-specific logic here
            // Maybe use SharpSvn
            return $"SVN show {branchName}:{fileName}";
        }
    }

    // Add more version control classes as needed

}
