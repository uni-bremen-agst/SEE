using LibGit2Sharp;
using NUnit.Framework;
using SEE.Utils.Paths;
using System;
using System.IO;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="VCS.GitRepository"/> using a locally created, temporary Git repository.
    /// </summary>
    internal class TestLocalGitRepository : TestGitRepository
    {
        // Create a local temporary repository, add a file, commit it.
        // Let's call this the "original" repository.
        // Clone it into another temporary directory.
        // Let's call this the "clone" repository.

        /// <summary>
        /// Signature of a developer.
        /// </summary>
        private static readonly Signature developer =
            new(
                "Jan Muller",
                "muller@example.com",
                new DateTimeOffset(2024, 04, 01, 1, 1, 1, TimeSpan.Zero)
            );

        /// <summary>
        /// Creates a new file in the path <paramref name="path"/> and fills or appends the file with
        /// the given <paramref name="text"/>.
        ///
        /// Then a git commit is made
        /// </summary>
        /// <param name="path">The path of the file</param>
        /// <param name="text">The text the file should have</param>
        /// <param name="author">The author of the commit</param>
        private static void WriteFile(Repository repo, string gitDirPath, string path, string text, Signature author)
        {
            if (Path.GetDirectoryName(path) != "")
            {
                Directory.CreateDirectory(Path.Combine(gitDirPath, Path.GetDirectoryName(path)));
            }

            File.AppendAllText(Path.Combine(gitDirPath, path), text);
            repo.Index.Add(path);
            repo.Index.Write();
            developer.When.AddHours(1);
            repo.Commit("One Commit", author, author);
        }

        private static string originalRepoPath;
        private static string cloneRepoPath;

        [SetUp]
        public static void SetUp()
        {
            originalRepoPath = Path.Combine(Path.GetTempPath(), "OriginalRepo");
            cloneRepoPath = Path.Combine(Path.GetTempPath(), "CloneRepo");
            // Delete both directories if they exist.
            DeleteDirectoryIfItExists(originalRepoPath);
            DeleteDirectoryIfItExists(cloneRepoPath);

            // Create and populate original repository.
            Debug.Log($"Creating original repository at {Repository.Init(originalRepoPath)}\n");
            using Repository original = new(originalRepoPath);
            WriteFile(original, originalRepoPath, "firstFile.cs", "This is a test", developer);

            // Clone original repository into clone repository.
            Debug.Log($"Cloning original repository into {Repository.Clone(originalRepoPath, cloneRepoPath)}\n");
        }

        [Test]
        public void TestSuccessfulCloning()
        {
            Assert.IsTrue(Directory.Exists(originalRepoPath));
            Assert.IsTrue(Directory.Exists(cloneRepoPath));
            Assert.IsTrue(Repository.IsValid(originalRepoPath));
            Assert.IsTrue(Repository.IsValid(cloneRepoPath));

            Assert.IsTrue(File.Exists(Path.Combine(originalRepoPath, "firstFile.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(cloneRepoPath, "firstFile.cs")));
        }

        [Test]
        public void TestFetchRemotes()
        {
            using Repository original = new(originalRepoPath);
            using GitRepository clone = new(new DataPath(cloneRepoPath), null);

            Assert.IsFalse(clone.FetchRemotes());

            WriteFile(original, originalRepoPath, "secondFile.cs", "This is a second test", developer);
            Assert.IsTrue(clone.FetchRemotes());

            // Create a new branch in original repository.
            // Define the name of the new branch.
            string newBranchName = "my-new-feature";

            // Create the new branch pointing to the current commit
            Branch newBranch = original.CreateBranch(newBranchName);
            Debug.Log($"Branch '{newBranch.FriendlyName}' created successfully.\n");
            Assert.IsTrue(clone.FetchRemotes());

            // Commit another file to the new branch.
            Commands.Checkout(original, newBranchName);
            WriteFile(original, originalRepoPath, "thirdFile.cs", "This is a third test", developer);
            Assert.IsTrue(clone.FetchRemotes());

            // Delete the new branch in the original repository.
            // Note: We cannot delete the branch while we are on it.
            Commands.Checkout(original, "master");
            original.Branches.Remove(newBranch);
            Assert.IsTrue(clone.FetchRemotes());
        }

        [TearDown]
        public static void TearDown()
        {
            DeleteDirectoryIfItExists(originalRepoPath);
            DeleteDirectoryIfItExists(cloneRepoPath);
        }
    }
}

