using LibGit2Sharp;
using NUnit.Framework;
using SEE.GraphProviders.VCS;
using SEE.Graphs;
using System;
using static SEE.Graphs.VCS;

namespace SEE.VCS
{
    /// <summary>
    /// Tests that <see cref="ChurnGraphGenerator"/> follows a file renamed
    /// into the directories taken into account from outside them, for either
    /// of its two entry points.
    /// </summary>
    /// <remarks>
    /// The history every test works on is this:
    ///
    /// <code>
    ///   C0 -- C1 -- R
    /// </code>
    ///
    /// C0 adds <see cref="firstName"/>; C1 renames it to
    /// <see cref="secondName"/> and adds a line; R renames it to
    /// <see cref="lastName"/>, which lies in <see cref="directory"/>, and
    /// deletes that line again. Only <see cref="directory"/> is taken into
    /// account, so the first two names lie outside it.
    ///
    /// A comparison narrowed to <see cref="directory"/> shows R as adding the
    /// file, its former name never having been compared, and leaves out C1
    /// and C0 altogether. Every commit, every developer but the one of R, and
    /// every line but those of the file as a whole would be lost, and the
    /// developer of R would be taken for the author of all of it. C1 also
    /// shows that a chain of renames outside <see cref="directory"/> is
    /// followed, not just the last one.
    /// </remarks>
    internal class TestChurnRenameIntoScope
    {
        /// <summary>
        /// The directory taken into account.
        /// </summary>
        private const string directory = "Assets/SEE";

        /// <summary>
        /// The name of the file as C0 adds it, outside <see cref="directory"/>.
        /// </summary>
        private const string firstName = "legacy/G.cs";

        /// <summary>
        /// The name of the file after C1, outside <see cref="directory"/>.
        /// </summary>
        private const string secondName = "legacy/F.cs";

        /// <summary>
        /// The name of the file after R, in <see cref="directory"/>. It is also the
        /// ID of its node.
        /// </summary>
        private const string lastName = directory + "/F.cs";

        /// <summary>
        /// The content of the file as C0 adds it and as R leaves it.
        /// </summary>
        private const string shortContent
            = "class F\n"
            + "{\n"
            + "    int One() { return 1; }\n"
            + "    int Two() { return 2; }\n"
            + "    int Three() { return 3; }\n"
            + "}\n";

        /// <summary>
        /// The content of the file as C1 leaves it: one line more than
        /// <see cref="shortContent"/>.
        /// </summary>
        private const string longContent
            = "class F\n"
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
        /// The temporary repository.
        /// </summary>
        private ThrowawayRepository repository;

        /// <summary>
        /// The commit adding the file, C0.
        /// </summary>
        private Commit initial;

        /// <summary>
        /// The commit renaming the file into <see cref="directory"/>, R.
        /// </summary>
        private Commit rename;

        /// <summary>
        /// Creates the temporary repository holding the history described in
        /// the remarks on this class, each commit by a developer of its own.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            repository = new ThrowawayRepository("seeChurnRenameIntoScopeTest", start);

            initial = repository.Make("C0", firstName, shortContent, "Ann Smith", 0);
            Commit edit = repository.Make("C1", secondName, longContent, "Bob Brown", 1, initial);
            rename = repository.Make("R", lastName, shortContent, "Cat Green", 2, edit);
            repository.Branch(rename);
        }

        /// <summary>
        /// Deletes the temporary repository.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            repository?.Dispose();
        }

        /// <summary>
        /// Over a period taking in the whole history, every commit must be
        /// counted for the file under its last name, R as a rename deleting a
        /// line rather than as adding the file.
        /// </summary>
        [Test]
        public void TestPeriod()
        {
            Graph graph = Period(start);

            AssertChurn(graph, commits: 3, developers: 3, linesAdded: 7, linesRemoved: 1);
        }

        /// <summary>
        /// Over a period taking in R alone, R must still be counted as a rename
        /// deleting a line, and both former names must be known, although C1,
        /// which yields the first one, is walked for its renames alone.
        /// </summary>
        [Test]
        public void TestPeriodAfterRenames()
        {
            Graph graph = Period(start.AddDays(2));

            AssertChurn(graph, commits: 1, developers: 1, linesAdded: 0, linesRemoved: 1);
        }

        /// <summary>
        /// Over the range from C0 to R, C1 and R must be counted for the file
        /// under its last name.
        /// </summary>
        [Test]
        public void TestRange()
        {
            Graph graph = new(repository.Location, nameof(TestRange));
            ChurnGraphGenerator.AddNodesForCommit
                (graph: graph,
                 simplifyGraph: false,
                 repositoryConfiguration: repository.Configuration(directory),
                 repositoryName: nameof(TestRange),
                 commitID: rename.Sha,
                 baselineCommitID: initial.Sha,
                 addCoChangeEdges: false,
                 changePercentage: null,
                 token: default);

            AssertChurn(graph, commits: 2, developers: 2, linesAdded: 1, linesRemoved: 1);
        }

        /// <summary>
        /// The graph of the period beginning on the day of <paramref name="since"/>.
        /// </summary>
        /// <param name="since">An instant on the first day of the period.</param>
        /// <returns>The graph.</returns>
        private Graph Period(DateTimeOffset since)
        {
            Graph graph = new(repository.Location, nameof(Period));
            ChurnGraphGenerator.AddNodesAfterDate
                (graph: graph,
                 simplifyGraph: false,
                 repositoryConfiguration: repository.Configuration(directory),
                 repositoryName: nameof(Period),
                 startDate: since.UtcDateTime.Date,
                 addCoChangeEdges: false,
                 changePercentage: null,
                 token: default);
            return graph;
        }

        /// <summary>
        /// Asserts that <paramref name="graph"/> holds a node for the file under
        /// its last name, carrying the given counts and both former names.
        /// </summary>
        /// <param name="graph">The graph to be checked.</param>
        /// <param name="commits">The number of commits expected to be counted.</param>
        /// <param name="developers">The number of developers expected to be counted.</param>
        /// <param name="linesAdded">The number of lines expected to be counted as added.</param>
        /// <param name="linesRemoved">The number of lines expected to be counted as removed.</param>
        private static void AssertChurn(Graph graph, int commits, int developers,
                                        int linesAdded, int linesRemoved)
        {
            Node node = graph.GetNode(lastName);
            Assert.That(node, Is.Not.Null, $"There is no node {lastName}.");
            Assert.That(node.IntAttributes[NumberOfCommits], Is.EqualTo(commits));
            Assert.That(node.IntAttributes[NumberOfDevelopers], Is.EqualTo(developers));
            Assert.That(node.IntAttributes[LinesAdded], Is.EqualTo(linesAdded),
                        "Counting R as adding the file rather than as renaming it adds every line.");
            Assert.That(node.IntAttributes[LinesRemoved], Is.EqualTo(linesRemoved));
            Assert.That(node.StringAttributes[FormerNames],
                        Is.EqualTo(secondName + "," + firstName));
        }
    }
}
