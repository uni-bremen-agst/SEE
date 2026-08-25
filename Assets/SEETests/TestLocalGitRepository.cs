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

        /// <summary>
        /// The name of the file that <see cref="SetUp"/> commits to the original
        /// repository and that is expected in both repositories afterwards.
        /// </summary>
        private const string firstFile = "firstFile.cs";

        /// <summary>
        /// Path of the original repository, created by <see cref="SetUp"/>.
        /// </summary>
        private static string originalRepoPath;

        /// <summary>
        /// Path of the clone of the original repository, created by <see cref="SetUp"/>.
        /// </summary>
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
            WriteFile(original, originalRepoPath, firstFile, "This is a test", developer);

            // Clone original repository into clone repository.
            Debug.Log($"Cloning original repository into {Repository.Clone(originalRepoPath, cloneRepoPath)}\n");
        }

        [Test]
        public void TestSuccessfulCloning()
        {
            Assert.That(new DirectoryInfo(originalRepoPath), Does.Exist);
            Assert.That(new DirectoryInfo(cloneRepoPath), Does.Exist);
            Assert.That(Repository.IsValid(originalRepoPath), Is.True,
                        $"{originalRepoPath} is not a valid Git repository.");
            Assert.That(Repository.IsValid(cloneRepoPath), Is.True,
                        $"{cloneRepoPath} is not a valid Git repository.");

            Assert.That(new FileInfo(Path.Combine(originalRepoPath, firstFile)), Does.Exist);
            Assert.That(new FileInfo(Path.Combine(cloneRepoPath, firstFile)), Does.Exist);
        }

        [Test]
        public void TestFetchRemotes()
        {
            using Repository original = new(originalRepoPath);
            GitRepository clone = new(new DataPath(cloneRepoPath), null);

            Assert.That(clone.FetchRemotes(), Is.False, "There is nothing to be fetched yet.");

            WriteFile(original, originalRepoPath, "secondFile.cs", "This is a second test", developer);
            Assert.That(clone.FetchRemotes(), Is.True, "The new commit must have been fetched.");

            // Create a new branch in original repository.
            // Define the name of the new branch.
            string newBranchName = "my-new-feature";

            // Create the new branch pointing to the current commit
            Branch newBranch = original.CreateBranch(newBranchName);
            Debug.Log($"Branch '{newBranch.FriendlyName}' created successfully.\n");
            Assert.That(clone.FetchRemotes(), Is.True, "The new branch must have been fetched.");

            // Commit another file to the new branch.
            Commands.Checkout(original, newBranchName);
            WriteFile(original, originalRepoPath, "thirdFile.cs", "This is a third test", developer);
            Assert.That(clone.FetchRemotes(), Is.True,
                        "The commit on the new branch must have been fetched.");

            // Delete the new branch in the original repository.
            // Note: We cannot delete the branch while we are on it.
            Commands.Checkout(original, "master");
            original.Branches.Remove(newBranch);
            Assert.That(clone.FetchRemotes(), Is.True,
                        "The deletion of the branch must have been fetched.");
        }

        [TearDown]
        public static void TearDown()
        {
            DeleteDirectoryIfItExists(originalRepoPath);
            DeleteDirectoryIfItExists(cloneRepoPath);
        }
    }
}

