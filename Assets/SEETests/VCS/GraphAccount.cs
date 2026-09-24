using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEE.VCS
{
    /// <summary>
    /// An account of a graph in a few numbers, for holding against a baseline.
    /// </summary>
    /// <remarks>
    /// A graph itself is far too large to keep in a baseline, yet a change to
    /// the way one is built should not pass unnoticed. These numbers move
    /// whenever the nodes, the edges or their attributes do.
    /// </remarks>
    internal static class GraphAccount
    {
        /// <summary>
        /// The account of <paramref name="graph"/> that is held against a
        /// baseline: how many nodes and edges of each type it holds, and what its
        /// metrics come to over all of them.
        /// </summary>
        /// <remarks>
        /// The graph itself is far too large to keep in a baseline, yet a change
        /// to the way it is built should not pass unnoticed. These few numbers
        /// move whenever the nodes or their attributes do.
        /// </remarks>
        /// <param name="graph">The graph to be accounted for.</param>
        /// <returns>The account.</returns>
        internal static string Of(Graph graph)
        {
            StringBuilder result = new();
            result.AppendLine("===== graph =====");
            foreach (IGrouping<string, Node> ofType in graph.Nodes()
                                                            .GroupBy(node => node.Type)
                                                            .OrderBy(group => group.Key,
                                                                     StringComparer.Ordinal))
            {
                result.AppendLine($"{ofType.Count(),8}  nodes of type {ofType.Key}");
            }
            foreach (string metric in new string[] { DataModel.DG.VCS.NumberOfDevelopers,
                                                     DataModel.DG.VCS.NumberOfCommits,
                                                     DataModel.DG.VCS.LinesAdded,
                                                     DataModel.DG.VCS.LinesRemoved,
                                                     DataModel.DG.VCS.Churn,
                                                     DataModel.DG.VCS.TruckNumber,
                                                     Metrics.LOC,
                                                     Metrics.Comments,
                                                     Metrics.NumberOfTokens,
                                                     Metrics.McCabe })
            {
                int sum = graph.Nodes().Sum(node => node.TryGetInt(metric, out int value) ? value : 0);
                result.AppendLine($"{sum,8}  {metric} over all nodes");
            }
            // One attribute per author of a file, so neither their number nor
            // their sum is had by asking for a name known beforehand.
            string perAuthor = DataModel.DG.VCS.Churn + ":";
            IEnumerable<KeyValuePair<string, int>> churnOfAuthors
                = graph.Nodes().SelectMany(node => node.IntAttributes)
                       .Where(attribute => attribute.Key.StartsWith(perAuthor, StringComparison.Ordinal));
            result.AppendLine($"{churnOfAuthors.Count(),8}  attributes {perAuthor}<author>");
            result.AppendLine($"{churnOfAuthors.Sum(attribute => attribute.Value),8}  "
                              + $"{perAuthor}<author> over all nodes");
            // Counted, not summed: the Halstead metrics are fractions, and a
            // fraction written out is a poor thing to hold a baseline against.
            result.AppendLine($"{graph.Nodes().Count(HasHalstead),8}  nodes with "
                              + $"{Halstead.Prefix}* attributes");
            foreach (IGrouping<string, Edge> ofType in graph.Edges()
                                                            .GroupBy(edge => edge.Type)
                                                            .OrderBy(group => group.Key,
                                                                     StringComparer.Ordinal))
            {
                result.AppendLine($"{ofType.Count(),8}  edges of type {ofType.Key}");
            }
            int together
                = graph.Edges()
                       .Sum(edge => edge.TryGetInt(DataModel.DG.VCS.ChangedTogether, out int value)
                                    ? value : 0);
            result.AppendLine($"{together,8}  {DataModel.DG.VCS.ChangedTogether} over all edges");
            return result.ToString();
        }

        /// <summary>
        /// Whether <paramref name="node"/> carries any of the Halstead metrics.
        /// </summary>
        /// <param name="node">The node to be checked.</param>
        /// <returns>True if and only if it carries one.</returns>
        private static bool HasHalstead(Node node)
        {
            return node.FloatAttributes.Keys.Any(name => name.StartsWith(Halstead.Prefix,
                                                                         StringComparison.Ordinal));
        }
    }
}
