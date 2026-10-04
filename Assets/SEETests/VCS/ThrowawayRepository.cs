using LibGit2Sharp;
using SEE.Graphs.Utils;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.IO;
using System.Text;

namespace SEE.VCS
{
    /// <summary>
    /// A git repository in the temporary directory, built commit by commit for
    /// a test and deleted afterwards.
    /// </summary>
    /// <remarks>
    /// The commits are made directly in the object database rather than
    /// through the working tree, so that their parents and their dates are
    /// exactly as stated, and no merge has to be carried out. The working tree
    /// stays empty.
    /// </remarks>
    internal sealed class ThrowawayRepository : IDisposable
    {
        /// <summary>
        /// The repository.
        /// </summary>
        private readonly Repository repo;

        /// <summary>
        /// The instant day 0 of <see cref="Make"/> denotes.
        /// </summary>
        private readonly DateTimeOffset start;

        /// <summary>
        /// The directory the repository lives in.
        /// </summary>
        internal string Location { get; }

        /// <summary>
        /// Creates an empty repository in a directory of the temporary directory
        /// named <paramref name="name"/>, deleting whatever a previous run may
        /// have left there.
        /// </summary>
        /// <param name="name">The name of the directory.</param>
        /// <param name="start">The instant day 0 of <see cref="Make"/> denotes.</param>
        internal ThrowawayRepository(string name, DateTimeOffset start)
        {
            this.start = start;
            Location = Path.Combine(Path.GetTempPath(), name);
            if (Directory.Exists(Location))
            {
                Filenames.DeleteReadOnlyDirectory(Location);
            }
            Directory.CreateDirectory(Location);
            repo = new Repository(Repository.Init(Location));
        }

        /// <summary>
        /// Makes a commit whose tree holds a single file.
        /// </summary>
        /// <param name="message">The message of the commit.</param>
        /// <param name="path">The name of the file, relative to the root of the
        /// repository and separated by <c>/</c>.</param>
        /// <param name="content">The content of the file.</param>
        /// <param name="developer">The author and committer of the commit.</param>
        /// <param name="day">The number of days after the start the commit is made at.</param>
        /// <param name="parents">The parents of the commit.</param>
        /// <returns>The commit.</returns>
        internal Commit Make(string message, string path, string content, string developer,
                             int day, params Commit[] parents)
        {
            Blob blob;
            using (MemoryStream stream = new(Encoding.UTF8.GetBytes(content)))
            {
                blob = repo.ObjectDatabase.CreateBlob(stream);
            }
            TreeDefinition definition = new();
            definition.Add(path, blob, Mode.NonExecutableFile);
            LibGit2Sharp.Tree tree = repo.ObjectDatabase.CreateTree(definition);

            Signature signature = new(developer, developer.Replace(' ', '.') + "@example.com",
                                      start.AddDays(day));
            return repo.ObjectDatabase.CreateCommit(signature, signature, message, tree,
                                                    parents, prettifyMessage: false);
        }

        /// <summary>
        /// Creates the branch HEAD refers to, pointing at <paramref name="tip"/>.
        /// Its name depends on the configuration of git. It is the only branch
        /// there is.
        /// </summary>
        /// <param name="tip">The commit the branch is to point at.</param>
        internal void Branch(Commit tip)
        {
            repo.Refs.Add(repo.Refs.Head.TargetIdentifier, tip.Id);
        }

        /// <summary>
        /// The configuration of the repository: every C# file in
        /// <paramref name="repositoryPaths"/>, every branch.
        /// </summary>
        /// <param name="repositoryPaths">The directories taken into account; none for the
        /// whole repository.</param>
        /// <returns>The configuration.</returns>
        internal GitRepository Configuration(params string[] repositoryPaths)
        {
            Filter filter = new(globbing: new Globbing() { { "**/*.cs", true } },
                                repositoryPaths: repositoryPaths.Length == 0 ? null : repositoryPaths,
                                branches: null);
            return new GitRepository(new DataPath(Location), filter);
        }

        /// <summary>
        /// Deletes the repository.
        /// </summary>
        public void Dispose()
        {
            repo.Dispose();
            if (Directory.Exists(Location))
            {
                Filenames.DeleteReadOnlyDirectory(Location);
            }
        }
    }
}
