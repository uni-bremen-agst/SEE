using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.GraphProviders.VCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEE.VCS
{
    /// <summary>
    /// Reads a graph of the churn of a repository back out: what it should hold
    /// in any case, and how to describe one so that a description may be held
    /// against a baseline.
    /// </summary>
    /// <remarks>
    /// Everything here goes by the graph alone and by nothing that built it, so
    /// that two implementations deriving such a graph can be held against one
    /// another. That is what it exists for.
    /// </remarks>
    internal static class ChurnReport
    {
        /// <summary>
        /// The report on <paramref name="graph"/>: an account of it in a few
        /// numbers, then a table with one line per file changed, the file with
        /// the most added lines first.
        /// </summary>
        /// <remarks>
        /// A file that was not changed has a node all the same, yet nothing to
        /// report beyond its name, so only the changed ones are worth a line
        /// here and the rest are counted in the heading.
        /// </remarks>
        /// <param name="graph">The graph to be reported on.</param>
        /// <param name="what">What was taken into account, stated in the heading beside
        /// the number of files. Say which commits, or from when and over which
        /// branches.</param>
        /// <returns>The report.</returns>
        internal static string Of(Graph graph, string what)
        {
            IList<Node> files = Files(graph);
            IList<Node> changed
                = files.Where(node => Number(node, DataModel.DG.VCS.NumberOfCommits) > 0).ToList();

            StringBuilder result = new();
            result.Append(GraphAccount.Of(graph));
            // The number of files stands in the heading so that a report holding
            // a different number of them differs from its baseline in the first
            // line, rather than only somewhere down the table.
            result.AppendLine($"===== {changed.Count} of {files.Count} files changed, "
                              + what + " =====");
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
        /// Checks what holds of any graph of the churn of a repository, failing
        /// the test where <paramref name="graph"/> does not hold to it.
        /// </summary>
        /// <param name="graph">The graph to be checked.</param>
        internal static void Check(Graph graph)
        {
            IList<Node> files = Files(graph);
            Assert.That(files, Is.Not.Empty,
                        "Not one file of the repository has a node. Either the commits "
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
        /// The churn of each author of <paramref name="node"/>, one attribute
        /// each being recorded of it, named for the author.
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
    }
}
