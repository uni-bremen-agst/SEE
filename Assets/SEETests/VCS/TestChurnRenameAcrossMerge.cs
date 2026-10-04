using LibGit2Sharp;
using NUnit.Framework;
using SEE.GraphProviders.VCS;
using SEE.Graphs;
using SEE.Graphs.Utils;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.IO;
using System.Text;
using static SEE.Graphs.VCS;

namespace SEE.VCS
{
    /// <summary>
    /// Tests that <see cref="ChurnGraphGenerator"/> follows a rename across
    /// a merge, for either of its two entry points.
    /// </summary>
    /// <remarks>
    /// The history every test works on is this:
    ///
    /// <code>
    ///   C0 -- R ------- M
    ///     \            /
    ///      S1 --------
    /// </code>
    ///
    /// C0 adds <see cref="formerName"/>; R, on the first line, renames it to
    /// <see cref="laterName"/>; S1, on the second line, edits it under its
    /// former name, and is committed after R; M merges the two lines, keeping
    /// the later name and the edit. Only M is reachable from a branch: the
    /// second line has been merged and its branch deleted, so the former name
    /// survives nowhere.
    ///
    /// S1 and R are not ancestors of one another, so a topological walk does
    /// not order them, and S1 being the more recent one, it is reached first.
    /// The rename is not known at that point unless the merge, which is
    /// reached before either, has revealed it: compared against S1, M renames
    /// the file. Were it not followed there, the churn of S1 would be filed
    /// under the former name, which does not survive, and be dropped.
    ///
    /// The commits are made directly in the object database rather than
    /// through the working tree, so that their parents and their dates are
    /// exactly as stated, and no merge has to be carried out.
    /// </remarks>
    internal class TestChurnRenameAcrossMerge
    {
        /// <summary>
        /// The name of the file before R renames it.
        /// </summary>
        private const string formerName = "A.cs";

        /// <summary>
        /// The name of the file after R renames it. It is also the ID of its node.
        /// </summary>
        private const string laterName = "B.cs";

        /// <summary>
        /// The content of the file as C0 adds it.
        /// </summary>
        private const string initialContent
            = "class A\n"
            + "{\n"
            + "    int One() { return 1; }\n"
            + "    int Two() { return 2; }\n"
            + "    int Three() { return 3; }\n"
            + "}\n";

        /// <summary>
        /// The content of the file as S1 leaves it: one method more than
        /// <see cref="initialContent"/>.
        /// </summary>
        private const string editedContent
            = "class A\n"
            + "{\n"
            + "    int One() { return 1; }\n"
            + "    int Two() { return 2; }\n"
            + "    int Three() { return 3; }\n"
            + "    int Four() { return 4; }\n"
            + "}\n";

        /// <summary>
        /// The instant C0 is made at. Every later commit is made a day after
        /// the one before it.
        /// </summary>
        private static readonly DateTimeOffset start = new(2024, 4, 1, 12, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// The author of C0, R and M.
        /// </summary>
        private const string mainDeveloper = "John Doe";

        /// <summary>
        /// The author of S1.
        /// </summary>
        private const string featureDeveloper = "Jan Mueller";

        /// <summary>
        /// Path to the temporary repository.
        /// </summary>
        private string gitDirPath;

        /// <summary>
        /// The temporary repository.
        /// </summary>
        private Repository repo;

        /// <summary>
        /// The commit renaming the file, R.
        /// </summary>
        private Commit rename;

        /// <summary>
        /// The merge, M.
        /// </summary>
        private Commit merge;

        /// <summary>
        /// Creates the temporary repository holding the history described in
        /// the remarks on this class.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            gitDirPath = Path.Combine(Path.GetTempPath(), "seeChurnRenameAcrossMergeTest");
            if (Directory.Exists(gitDirPath))
            {
                Filenames.DeleteReadOnlyDirectory(gitDirPath);
            }
            Directory.CreateDirectory(gitDirPath);
            repo = new Repository(Repository.Init(gitDirPath));

            Commit initial = Make("C0", formerName, initialContent, mainDeveloper, 0);
            rename = Make("R", laterName, initialContent, mainDeveloper, 1, initial);
            Commit edit = Make("S1", formerName, editedContent, featureDeveloper, 2, initial);
            merge = Make("M", laterName, editedContent, mainDeveloper, 3, rename, edit);

            // The branch HEAD refers to, whatever its name, which depends on
            // the configuration of git. It is the only branch there is.
            repo.Refs.Add(repo.Refs.Head.TargetIdentifier, merge.Id);
        }

        /// <summary>
        /// Deletes the temporary repository.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            repo?.Dispose();
            if (Directory.Exists(gitDirPath))
            {
                Filenames.DeleteReadOnlyDirectory(gitDirPath);
            }
        }

        /// <summary>
        /// Over a period, the churn of S1 must be counted for the file under
        /// its later name, along with that of C0 and R.
        /// </summary>
        [Test]
        public void TestPeriod()
        {
            Graph graph = new(gitDirPath, nameof(TestPeriod));
            ChurnGraphGenerator.AddNodesAfterDate
                (graph: graph,
                 simplifyGraph: false,
                 repositoryConfiguration: Configuration(),
                 repositoryName: nameof(TestPeriod),
                 startDate: start.UtcDateTime.Date,
                 addCoChangeEdges: false,
                 changePercentage: null,
                 token: default);

            AssertChurn(graph, commits: 3, developers: 2);
        }

        /// <summary>
        /// Over the range from R to M, the churn of S1 must be counted for the
        /// file under its later name. R is the baseline and thus not walked,
        /// so the merge is the only commit walked that shows the rename.
        /// </summary>
        [Test]
        public void TestRange()
        {
            Graph graph = new(gitDirPath, nameof(TestRange));
            ChurnGraphGenerator.AddNodesForCommit
                (graph: graph,
                 simplifyGraph: false,
                 repositoryConfiguration: Configuration(),
                 repositoryName: nameof(TestRange),
                 commitID: merge.Sha,
                 baselineCommitID: rename.Sha,
                 addCoChangeEdges: false,
                 changePercentage: null,
                 token: default);

            AssertChurn(graph, commits: 1, developers: 1);
        }

        /// <summary>
        /// Asserts that <paramref name="graph"/> holds a node for the file
        /// under its later name only, carrying the given counts.
        /// </summary>
        /// <param name="graph">The graph to be checked.</param>
        /// <param name="commits">The number of commits expected to be counted.</param>
        /// <param name="developers">The number of developers expected to be counted.</param>
        private static void AssertChurn(Graph graph, int commits, int developers)
        {
            Assert.That(graph.GetNode(formerName), Is.Null,
                        $"The former name {formerName} survives nowhere.");
            Node node = graph.GetNode(laterName);
            Assert.That(node, Is.Not.Null, $"There is no node {laterName}.");
            Assert.That(node.IntAttributes[NumberOfCommits], Is.EqualTo(commits),
                        "The churn of S1 is missing unless the rename is followed across the merge.");
            Assert.That(node.IntAttributes[NumberOfDevelopers], Is.EqualTo(developers));
        }

        /// <summary>
        /// The configuration of the temporary repository: every C# file,
        /// every branch.
        /// </summary>
        /// <returns>The configuration.</returns>
        private GitRepository Configuration()
        {
            Filter filter = new(globbing: new Globbing() { { "**/*.cs", true } },
                                repositoryPaths: null,
                                branches: null);
            return new GitRepository(new DataPath(gitDirPath), filter);
        }

        /// <summary>
        /// Makes a commit whose tree holds a single file.
        /// </summary>
        /// <param name="message">The message of the commit.</param>
        /// <param name="path">The name of the file.</param>
        /// <param name="content">The content of the file.</param>
        /// <param name="developer">The author and committer of the commit.</param>
        /// <param name="day">The number of days after <see cref="start"/> the commit is
        /// made at.</param>
        /// <param name="parents">The parents of the commit.</param>
        /// <returns>The commit.</returns>
        private Commit Make(string message, string path, string content, string developer,
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
    }
}
