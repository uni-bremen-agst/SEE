using LibGit2Sharp;
using SEE.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="VCS.GitRepository"/>.
    /// </summary>
    internal class TestGitRepository
    {
        /// <summary>
        /// URL for the test project on GitHub.
        /// </summary>
        protected const string testRepositoryUrl = "https://github.com/koschke/TestProjectForSEE.git";

        /// <summary>
        /// The access token for the test repository at <see cref="testRepositoryUrl"/>.
        /// </summary>
        /// <remarks>DO NOT CHECK IN YOUR TOKEN!</remarks>
        protected const string testRepositoryAccessToken = "";

        /// <summary>
        /// Yields the local temporary directory path where the repository
        /// at <paramref name="repositoryUrl"/> can be checked out. It will
        /// a subdirectory of the system's temporary directory named after
        /// the last part of the repository URL.
        ///
        /// For instance, LocalPath("https://github.com/koschke/TestProjectForSEE.git")
        /// yields something like "C:\Users\koschke\AppData\Local\Temp\TestProjectForSEE.git"
        /// on a Windows machine.
        /// </summary>
        /// <param name="repositoryUrl">the URL to the repository</param>
        /// <returns>local temporary directory path</returns>
        protected static string LocalPath(string repositoryUrl)
        {
            return Path.Combine(Path.GetTempPath(), Filenames.Basename(repositoryUrl, '/'));
        }

        /// <summary>
        /// Deletes the directory at <paramref name="path"/> if it exists.
        /// </summary>
        /// <param name="path"></param>
        protected static void DeleteDirectoryIfItExists(string path)
        {
            if (Directory.Exists(path))
            {
                // Remove read-only attribute from directory and all its contents
                // so that we can delete everything.
                DirectoryInfo directory = new(path) { Attributes = FileAttributes.Normal };

                foreach (FileSystemInfo info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }

                directory.Delete(true);
            }
        }

        /// <summary>
        /// Prints the given <paramref name="values"/>.
        /// </summary>
        /// <typeparam name="T">type of the values</typeparam>
        /// <param name="values">the values to be printed</param>
        protected static void Print<T>(IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                Debug.Log(value.ToString() + "\n");
            }
        }

        /// <summary>
        /// Prints the given <paramref name="patch"/> to the debug log.
        /// </summary>
        /// <param name="patch">to be printed</param>
        protected static void Print(Patch patch)
        {
            foreach (PatchEntryChanges entry in patch)
            {
                Debug.Log($"Path: {entry.Path}\n");
                Debug.Log($"OldPath: {entry.OldPath}\n");
                Debug.Log($"Status: {entry.Status}\n");
                Debug.Log($"LinesAdded: {entry.LinesAdded}\n");
                Debug.Log($"LinesDeleted: {entry.LinesDeleted}\n");
                Debug.Log($"Patch: {entry.Patch}\n");
            }
        }
    }
}
