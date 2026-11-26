#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.Game.City;
using SEE.Graphs;
using SEE.Utils;
using Range = SEE.Graphs.Range;

namespace SEE.Tools.AI
{
    /// <summary>
    /// Maps a source location (file and line) onto the graph node declared there, for the
    /// <c>node_at</c> tool of <see cref="AgentCommandServer"/>.
    ///
    /// The nodes of each city's loaded graph are grouped by their path (see
    /// <see cref="GraphElement.Path()"/>); the groups are built when a city is first asked
    /// and rebuilt when the city has loaded another graph.
    ///
    /// <see cref="Graphs.GraphIndex.SourceRangeIndex"/> is not used because it throws
    /// an exception for overlapping source ranges, which would make the whole city unusable.
    /// </summary>
    internal class SourceLocator
    {
        /// <summary>How a node was found for a line.</summary>
        internal enum MatchKind
        {
            /// <summary>The node's source range encloses the line; the innermost such node is used.</summary>
            Range,

            /// <summary>
            /// No source range encloses the line; the node declared last at or before the line is used.
            /// It may be wrong if the line lies behind the end of that node.
            /// </summary>
            NearestDeclaration
        }

        /// <summary>The nodes of a loaded graph, grouped by file path.</summary>
        private class CityFiles
        {
            /// <summary>The graph the groups were built from.</summary>
            internal Graph Graph;

            /// <summary>
            /// The nodes with a source line, by their path with '/' as separator
            /// (compared ignoring case).
            /// </summary>
            internal Dictionary<string, List<Node>> Files;
        }

        /// <summary>The groups per city.</summary>
        private readonly Dictionary<AbstractSEECity, CityFiles> cache = new();

        /// <summary>
        /// Finds the node declared at <paramref name="line"/> of <paramref name="file"/>.
        /// </summary>
        /// <param name="file">Absolute or relative path of the file, with '/' or '\' as separator.
        /// The longest path of a node that matches the end of <paramref name="file"/> (or whose end
        /// matches <paramref name="file"/>) is used.</param>
        /// <param name="line">The line (1-based).</param>
        /// <param name="cityName">Name of the only city to search, or null for all cities.</param>
        /// <param name="node">The node found, or null.</param>
        /// <param name="match">How <paramref name="node"/> was found.</param>
        /// <param name="problem">Why no node was found, or the candidate files if the file is
        /// ambiguous; null on success.</param>
        /// <returns>Whether a node was found.</returns>
        internal bool TryLocate(string file, int line, string cityName,
                                out Node node, out MatchKind match, out string problem)
        {
            node = null;
            match = MatchKind.Range;
            if (string.IsNullOrWhiteSpace(file))
            {
                problem = "No file given.";
                return false;
            }
            if (line < 1)
            {
                problem = "The line must be at least 1.";
                return false;
            }

            string wanted = Normalize(file);
            List<(int score, string path, List<Node> nodes)> candidates = new();
            foreach (AbstractSEECity city in UnityEngine.Object.FindObjectsByType<AbstractSEECity>(FindObjectsSortMode.None))
            {
                if (cityName != null && !string.Equals(city.name, cityName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                foreach ((string path, List<Node> nodes) in FilesOf(city))
                {
                    int score = Score(wanted, path);
                    if (score > 0)
                    {
                        candidates.Add((score, path, nodes));
                    }
                }
            }

            if (candidates.Count == 0)
            {
                problem = $"No node of {(cityName != null ? $"city '{cityName}'" : "any city")} is declared in '{file}'.";
                return false;
            }
            int best = candidates.Max(c => c.score);
            List<(int score, string path, List<Node> nodes)> bestCandidates = candidates.Where(c => c.score == best).ToList();
            // The same file in several cities is fine; different files are not.
            List<string> bestPaths = bestCandidates.Select(c => c.path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (bestPaths.Count > 1)
            {
                problem = $"'{file}' matches several files: {string.Join(", ", bestPaths)}. Give a longer path.";
                return false;
            }

            List<Node> inFile = bestCandidates.SelectMany(c => c.nodes).ToList();
            node = InnermostEnclosing(inFile, line);
            if (node == null)
            {
                match = MatchKind.NearestDeclaration;
                node = NearestDeclaration(inFile, line);
            }
            problem = node == null ? $"No node in '{bestPaths[0]}' is declared at or before line {line}." : null;
            return node != null;
        }

        /// <summary>
        /// The innermost drawn node containing <paramref name="node"/>: the node itself if it is
        /// drawn, otherwise its nearest drawn ancestor.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The drawn node, or null if neither the node nor an ancestor is drawn.</returns>
        internal static Node DrawnNodeOrAncestor(Node node)
        {
            for (Node current = node; current != null; current = current.Parent)
            {
                GameObject gameNode = current.GameObject();
                if (gameNode != null && gameNode.activeInHierarchy)
                {
                    return current;
                }
            }
            return null;
        }

        /// <summary>
        /// The nodes of <paramref name="city"/>'s loaded graph grouped by path; built on first use
        /// and whenever the city has loaded another graph.
        /// </summary>
        /// <param name="city">The city.</param>
        /// <returns>The groups; empty if the city has no loaded graph.</returns>
        private Dictionary<string, List<Node>> FilesOf(AbstractSEECity city)
        {
            Graph graph = city.LoadedGraph;
            if (graph == null)
            {
                cache.Remove(city);
                return new();
            }
            if (cache.TryGetValue(city, out CityFiles cached) && cached.Graph == graph)
            {
                return cached.Files;
            }

            Dictionary<string, List<Node>> files = new(StringComparer.OrdinalIgnoreCase);
            foreach (Node node in graph.Nodes())
            {
                if (string.IsNullOrEmpty(node.Filename) || !node.SourceLine.HasValue)
                {
                    continue;
                }
                string path = Normalize(node.Path());
                if (!files.TryGetValue(path, out List<Node> nodes))
                {
                    files.Add(path, nodes = new());
                }
                nodes.Add(node);
            }
            cache[city] = new() { Graph = graph, Files = files };
            return files;
        }

        /// <summary>
        /// How well the node path <paramref name="path"/> matches the requested file
        /// <paramref name="wanted"/>; both use '/' as separator.
        /// </summary>
        /// <param name="wanted">The requested file, absolute or relative.</param>
        /// <param name="path">The path of a node, usually relative to the city's source directory.</param>
        /// <returns><see cref="int.MaxValue"/> if both are equal; the length of the shorter one if
        /// one ends with the other at a directory boundary; otherwise 0.</returns>
        private static int Score(string wanted, string path)
        {
            const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
            if (wanted.Equals(path, ignoreCase))
            {
                return int.MaxValue;
            }
            if (wanted.EndsWith("/" + path, ignoreCase))
            {
                return path.Length;
            }
            return path.EndsWith("/" + wanted, ignoreCase) ? wanted.Length : 0;
        }

        /// <summary>
        /// The node with the smallest explicit source range that encloses <paramref name="line"/>.
        /// </summary>
        /// <param name="nodes">The nodes declared in one file.</param>
        /// <param name="line">The line.</param>
        /// <returns>The node, or null if no explicit range encloses the line.</returns>
        private static Node InnermostEnclosing(IEnumerable<Node> nodes, int line)
        {
            Node result = null;
            int resultLines = int.MaxValue;
            foreach (Node node in nodes)
            {
                if (node.TryGetRange(GraphElement.SourceRangeAttribute, out Range range)
                    && Encloses(range, line)
                    && (result == null || range.Lines < resultLines
                        || (range.Lines == resultLines && node.Level > result.Level)))
                {
                    result = node;
                    resultLines = range.Lines;
                }
            }
            return result;

            // The end line is exclusive, unless the range ends within that line
            // (see Range.Lines).
            static bool Encloses(Range range, int line) =>
                line >= range.StartLine
                && (line < range.EndLine || (line == range.EndLine && range.EndCharacter is > 0));
        }

        /// <summary>
        /// The node declared last at or before <paramref name="line"/>; of several on the
        /// same line, the deepest one in the node hierarchy.
        /// </summary>
        /// <param name="nodes">The nodes declared in one file; all have a source line.</param>
        /// <param name="line">The line.</param>
        /// <returns>The node, or null if all nodes are declared after the line.</returns>
        private static Node NearestDeclaration(IEnumerable<Node> nodes, int line) =>
            nodes.Where(n => n.SourceLine.Value <= line)
                 .OrderByDescending(n => n.SourceLine.Value)
                 .ThenByDescending(n => n.Level)
                 .FirstOrDefault();

        /// <summary>A path with '/' as separator and without surrounding blanks.</summary>
        /// <param name="path">The path.</param>
        /// <returns>The normalized path.</returns>
        private static string Normalize(string path) => path.Trim().Replace('\\', '/');
    }
}
#endif
