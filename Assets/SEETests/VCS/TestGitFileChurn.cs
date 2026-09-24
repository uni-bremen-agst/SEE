using LibGit2Sharp;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="ChurnGraphGenerator"/>. A run emits an account of
    /// the graph derived and a table of the files changed in the period, and
    /// holds both against what the run before produced, so that a change to the
    /// code deriving the graph does not pass unnoticed.
    /// </summary>
    /// <remarks>
    /// Everything reported here is read back out of the graph, which is thus
    /// also a check that what was gathered did reach it.
    ///
    /// Mind that the report depends on the history of the repository as much as
    /// on the code deriving it; see <see cref="Baseline"/> on telling the two
    /// apart.
    /// </remarks>
    internal class TestGitFileChurn
    {
        /// <summary>
        /// The configurations <see cref="TestChurn"/> is run on, one
        /// test case each.
        /// </summary>
        /// <remarks>
        /// A source rather than a <see cref="TestCaseAttribute"/>, because the
        /// argument of an attribute must be a constant expression, and
        /// <c>new DateTime(...)</c> is not one: an object creation never is. The
        /// arrays would have been allowed, an array of constants being a
        /// constant expression, but the date alone rules the attribute out.
        ///
        /// Being a method, this is moreover evaluated when the test is
        /// discovered rather than when this class is initialized, so it may
        /// read the fields above whatever order those are declared in.
        ///
        /// Name every case: with none, all that tells two of them apart in the
        /// test runner is <c>SEE.VCS.Filter</c> over and over.
        /// </remarks>
        /// <returns>One test case per configuration.</returns>
        private static IEnumerable<TestCaseData> Configurations()
        {
            // The beginning of the period to be reported on; commits authored at
            // the instant this day begins at in UTC are still taken into account,
            // that being the instant a date denotes elsewhere in SEE.
            //
            // The further back this reaches, the longer the test runs.
            DateTime startDate = new(2025, 1, 1);

            Filter filter = new
                (// A file is reported on if it matches at least one inclusive
                 // pattern and no exclusive one. Add a pattern with the value
                 // false to leave something out, say { "**/*.Designer.cs", false }.
                 globbing: new Globbing() { { "**/*.cs", true } },
                 // The directories, relative to the root of the repository, whose
                 // files are reported on. Nested directories are included.
                 repositoryPaths: new string[] { "Assets/SEE", "Assets/SEETests" },
                 // Regular expressions selecting the branches to be reported on,
                 // their commits taken together. Each is matched against
                 // the friendly name of a branch as a whole, so "master" is the
                 // local master alone and "origin/.*" every remote-tracking
                 // branch of origin. To catch both masters, write ".*master".
                 //
                 // An expression selecting no branch at all is reported as a
                 // warning. A branch selected by several of them is still
                 // reported on only once.
                 //
                 // The commits reachable from the branches are walked once
                 // over, so selecting many branches costs far less than as many
                 // walks; what they share is visited a single time.
                 branches: new string[] { "master", "origin/.*" });

            GitRepository repositoryConfiguration = new(new DataPath(DataPath.ProjectFolder()), filter);

            yield return new TestCaseData(startDate, repositoryConfiguration)
                .SetName(nameof(TestChurn));
        }

        /// <summary>
        /// Derives the graph of the repository named by
        /// <paramref name="repositoryConfiguration"/>, emits an account of it
        /// and a table of the files changed since <paramref name="startDate"/>
        /// to the console, and holds both against the baseline of the previous
        /// run. Also reports the time the deriving took, so that this may be
        /// run under a profiler.
        /// </summary>
        /// <remarks>
        /// The <see cref="DeepProfiler"/> scope below yields a dotTrace snapshot
        /// covering only the generation, with every method transitively called in
        /// it. How to set dotTrace up for this is documented at
        /// <see cref="DataModel.DG.IO.TestGraphIO.TestReadingRealBigGraph"/>; the
        /// essential part is that "Control profiling" must be set to "Using API",
        /// because otherwise the calls made here are inert.
        ///
        /// The profiling type has to be Timeline. dotTrace offers no other type for
        /// a Unity target: "Only the Timeline type is supported for profiling a
        /// Mono/Unity application." A Timeline snapshot can only be read by
        /// dotTrace's Timeline viewer; it is not a .dtp snapshot and the
        /// command-line Reporter cannot turn it into a report.
        ///
        /// Unlike <see cref="DataModel.DG.IO.TestGraphIO.TestReadingRealBigGraph"/>,
        /// the work here runs synchronously on the thread of this test method, so
        /// its call tree is the one to look at in the viewer.
        /// </remarks>
        /// <param name="startDate">The beginning of the period to be reported on.</param>
        /// <param name="repositoryConfiguration">The repository configuration based on which the
        /// graph is derived.</param>
        [TestCaseSource(nameof(Configurations))]
        public void TestChurn(DateTime startDate, GitRepository repositoryConfiguration)
        {
            string repositoryPath = repositoryConfiguration.RepositoryPath.Path;
            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);
            Graph graph = new(repositoryPath, repositoryName);

            Performance p = Performance.Begin($"Adding nodes for {repositoryName} commits since "
                                              + $"{startDate:yyyy-MM-dd}");
            //using (DeepProfiler.Capture(nameof(TestChurn)))
            {
                ChurnGraphGenerator.AddNodesAfterDate
                    (graph: graph,
                     simplifyGraph: true,
                     repositoryConfiguration: repositoryConfiguration,
                     repositoryName: repositoryName,
                     startDate: startDate,
                     computeCoFileChanges: true,
                     changePercentage: null,
                     token: default);
            }
            p.End(true);

            Check(graph);
            // The instant the date denotes, as ChurnGraphGenerator reads it.
            DateTimeOffset since = new(startDate.Date, TimeSpan.Zero);
            IDictionary<string, string> tips = Tips(repositoryConfiguration);
            string report = GraphAccount.Of(graph) + Report(graph, since, tips.Count);
            Debug.Log(report);
            Baseline.CompareOrWrite(TestContext.CurrentContext.Test.Name,
                                    Context(repositoryConfiguration, since, tips), report);
        }

        /// <summary>
        /// The nodes of <paramref name="graph"/> standing for a file, ordered by
        /// the number of lines added to it, the one with the most first.
        /// </summary>
        /// <param name="graph">The graph whose file nodes are asked for.</param>
        /// <returns>The file nodes.</returns>
        private static IList<Node> Files(Graph graph)
        {
            return graph.Nodes()
                        .Where(node => node.Type == NodeTypes.File)
                        .OrderByDescending(node => Number(node, DataModel.DG.VCS.LinesAdded))
                        .ThenBy(node => node.ID, StringComparer.Ordinal)
                        .ToList();
        }

        /// <summary>
        /// The value of the metric named <paramref name="metric"/> of
        /// <paramref name="node"/>, or nought where it carries none.
        /// </summary>
        /// <param name="node">The node the metric is read from.</param>
        /// <param name="metric">The name of the metric.</param>
        /// <returns>The value of the metric.</returns>
        private static int Number(Node node, string metric)
        {
            return node.TryGetInt(metric, out int value) ? value : 0;
        }

        /// <summary>
        /// The churn of each author of <paramref name="node"/>, which
        /// <see cref="ChurnGraphGenerator"/> records one attribute each of,
        /// named for the author.
        /// </summary>
        /// <param name="node">The node the churn is read from.</param>
        /// <returns>What each author churned of the file.</returns>
        private static IList<KeyValuePair<string, int>> ChurnOfAuthors(Node node)
        {
            string prefix = DataModel.DG.VCS.Churn + ":";
            return node.IntAttributes
                       .Where(attribute => attribute.Key.StartsWith(prefix, StringComparison.Ordinal))
                       .ToList();
        }

        /// <summary>
        /// The authors of <paramref name="node"/> by name, in the order the
        /// attribute naming them holds.
        /// </summary>
        /// <remarks>
        /// The attribute holds identities, that is, a name and an address each,
        /// as <see cref="FileAuthor.ToString"/> writes them. Only the name is
        /// reported, an address being long and saying nothing the name does not.
        /// </remarks>
        /// <param name="node">The node the authors are read from.</param>
        /// <returns>The names of the authors.</returns>
        private static IEnumerable<string> Authors(Node node)
        {
            return node.TryGetString(DataModel.DG.VCS.AuthorsAttributeName, out string authors)
                   ? authors.Split(',').Select(NameOf) : Enumerable.Empty<string>();
        }

        /// <summary>
        /// The name in <paramref name="author"/>, which is an identity as
        /// <see cref="FileAuthor.ToString"/> writes one, that is, a name
        /// followed by an address in angle brackets.
        /// </summary>
        /// <remarks>
        /// The address is cut off rather than the identity being parsed by
        /// <see cref="FileAuthor(string)"/>, which insists on there being at
        /// least one character of a name and would throw on an author who
        /// committed under an address alone.
        /// </remarks>
        /// <param name="author">The identity of an author.</param>
        /// <returns>The name therein.</returns>
        private static string NameOf(string author)
        {
            int address = author.LastIndexOf('<');
            return address < 0 ? author : author.Substring(0, address);
        }

        /// <summary>
        /// The names <paramref name="node"/> carried before it was renamed, or
        /// none where it never was.
        /// </summary>
        /// <param name="node">The node the former names are read from.</param>
        /// <returns>The former names.</returns>
        private static IEnumerable<string> FormerNames(Node node)
        {
            return node.TryGetString(DataModel.DG.VCS.FormerNames, out string names)
                   ? names.Split(',') : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Checks what <see cref="ChurnGraphGenerator"/> guarantees of the graph
        /// it derives, failing the test where <paramref name="graph"/> does not
        /// hold to it.
        /// </summary>
        /// <param name="graph">The graph to be checked.</param>
        private static void Check(Graph graph)
        {
            IList<Node> files = Files(graph);
            Assert.That(files, Is.Not.Empty,
                        "Not one file of the repository has a node. Either the branches "
                        + "reported on hold none, or the paths the session reports differ in "
                        + "form from the paths a comparison of two commits yields.");

            foreach (Node node in files.Where(node => Number(node, DataModel.DG.VCS.NumberOfCommits) > 0))
            {
                IList<KeyValuePair<string, int>> churnOfAuthors = ChurnOfAuthors(node);
                Assert.That(Authors(node), Is.Not.Empty,
                            $"{node.ID} is reported without any author.");
                Assert.That(Number(node, DataModel.DG.VCS.NumberOfDevelopers),
                            Is.EqualTo(churnOfAuthors.Count),
                            $"{node.ID} has churn of a number of authors differing from the "
                            + "number of developers counted for it.");
                // What the authors of a file churned between them is what the
                // file was churned, every change being made by one of them.
                Assert.That(churnOfAuthors.Sum(author => author.Value),
                            Is.EqualTo(Number(node, DataModel.DG.VCS.LinesAdded)
                                       + Number(node, DataModel.DG.VCS.LinesRemoved)),
                            $"The churn of {node.ID} is not what its authors churned.");
                // Its core developers are some of its authors, and where it has
                // any they cannot be none: somebody churned what was churned.
                Assert.That(Number(node, DataModel.DG.VCS.TruckNumber),
                            Is.InRange(1, churnOfAuthors.Count),
                            $"The truck factor of {node.ID} is not a number of its authors.");
            }

            // Being changed by the same commit is symmetric, so one edge stands
            // for the two files; a second one the other way round would say
            // nothing the first does not. And a file is not changed together
            // with itself, so neither is an edge a loop.
            HashSet<string> joined = new();
            foreach (Edge edge in graph.Edges()
                                       .Where(edge => edge.Type == DataModel.DG.VCS.CoChangeType))
            {
                Assert.That(edge.Source, Is.Not.SameAs(edge.Target),
                            $"The co-change edge {edge.ID} is a self loop at {edge.Source.ID}.");
                string pair = string.CompareOrdinal(edge.Source.ID, edge.Target.ID) < 0
                              ? edge.Source.ID + "\n" + edge.Target.ID
                              : edge.Target.ID + "\n" + edge.Source.ID;
                Assert.That(joined.Add(pair), Is.True,
                            $"{edge.Source.ID} and {edge.Target.ID} are joined by more than one "
                            + "co-change edge.");
            }
        }

        /// <summary>
        /// The report on <paramref name="graph"/> as a table, one line per file
        /// changed in the period, the file with the most added lines first.
        /// </summary>
        /// <remarks>
        /// A file untouched in the period has a node all the same, yet nothing
        /// to report beyond its name, so only the changed ones are worth a line
        /// here and the rest are counted in the heading.
        /// </remarks>
        /// <param name="graph">The graph to be reported on.</param>
        /// <param name="since">The beginning of the period reported on.</param>
        /// <param name="branches">How many branches were walked.</param>
        /// <returns>The report.</returns>
        private static string Report(Graph graph, DateTimeOffset since, int branches)
        {
            IList<Node> files = Files(graph);
            IList<Node> changed
                = files.Where(node => Number(node, DataModel.DG.VCS.NumberOfCommits) > 0).ToList();

            StringBuilder result = new();
            // The separators are quoted, because an unquoted '-' and ':' stand
            // for the date and the time separator of the current culture.
            // The number of files stands in the heading so that a report holding
            // a different number of them differs from its baseline in the first
            // line, rather than only somewhere down the table.
            result.AppendLine($"===== {changed.Count} of {files.Count} files changed, "
                              + $"{branches} branches, author date >= "
                              + since.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz")
                              + " =====");
            if (changed.Count == 0)
            {
                result.AppendLine("no commit in the period reported on");
                return result.ToString();
            }

            int width = Math.Max("file".Length, changed.Max(node => node.ID.Length));
            result.AppendLine($"{"added",8}  {"deleted",8}  {"commits",8}  {"file".PadRight(width)}  authors");
            foreach (Node node in changed)
            {
                result.AppendLine($"{Number(node, DataModel.DG.VCS.LinesAdded),8}  "
                                  + $"{Number(node, DataModel.DG.VCS.LinesRemoved),8}  "
                                  + $"{Number(node, DataModel.DG.VCS.NumberOfCommits),8}  "
                                  + $"{node.ID.PadRight(width)}  "
                                  + string.Join(", ", Authors(node)));
                // On a line of its own: a file that has been renamed is the
                // exception, and a name is as long as the one above it.
                IEnumerable<string> formerNames = FormerNames(node);
                if (formerNames.Any())
                {
                    result.AppendLine($"{string.Empty,30}formerly {string.Join(", ", formerNames)}");
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// The commit at the tip of each branch <see cref="ChurnGraphGenerator"/>
        /// walks, which is every one the filter of
        /// <paramref name="repositoryConfiguration"/> holds relevant but the
        /// symbolic references among them, keyed by the name of the branch and
        /// ordered by it.
        /// </summary>
        /// <remarks>
        /// The SHAs are read here, while the session is open, and only they are
        /// handed out. A <see cref="Branch"/> is a handle into the native
        /// repository the session holds; one outliving its session refers to
        /// memory that has been freed, and asking such a one for its
        /// <see cref="Branch.Tip"/> brings the editor down rather than raising
        /// anything that could be caught.
        /// </remarks>
        /// <param name="repositoryConfiguration">The repository whose branches are asked
        /// for.</param>
        /// <returns>The SHA of the tip of each branch walked, keyed by its name.</returns>
        private static IDictionary<string, string> Tips(GitRepository repositoryConfiguration)
        {
            SortedDictionary<string, string> result = new(StringComparer.Ordinal);
            using GitRepositorySession session = repositoryConfiguration.OpenGitSession();
            foreach (Branch branch in session.RelevantBranches()
                                             .Where(branch => !branch.FriendlyName
                                                     .EndsWith("/HEAD", StringComparison.Ordinal)))
            {
                result[branch.FriendlyName] = branch.Tip.Sha;
            }
            return result;
        }

        /// <summary>
        /// The circumstances the graph was derived under, to be recorded beside
        /// the report and not compared with it.
        /// </summary>
        /// <remarks>
        /// The tip of every branch walked is named, so that a report differing
        /// from its baseline because the repository has gained commits can be
        /// told from one differing because the code deriving the graph has
        /// changed.
        /// </remarks>
        /// <param name="repositoryConfiguration">The repository the graph was derived
        /// from.</param>
        /// <param name="since">The beginning of the period reported on.</param>
        /// <param name="tips">The SHA of the tip of each branch walked, keyed by its
        /// name.</param>
        /// <returns>The circumstances.</returns>
        private static string Context(GitRepository repositoryConfiguration, DateTimeOffset since,
                                      IDictionary<string, string> tips)
        {
            Filter filter = repositoryConfiguration.VCSFilter;
            StringBuilder result = new();
            result.AppendLine($"repository: {repositoryConfiguration.RepositoryPath.Path}");
            result.AppendLine($"since:      {since:o}");
            result.AppendLine("paths:      "
                              + string.Join(", ", filter.RepositoryPaths
                                                  ?? Array.Empty<string>()));
            result.AppendLine("branches:   "
                              + string.Join(", ", filter.Branches ?? Enumerable.Empty<string>()));
            foreach (KeyValuePair<string, string> tip in tips)
            {
                result.AppendLine($"tip {tip.Key} {tip.Value}");
            }
            return result.ToString();
        }
    }
}
