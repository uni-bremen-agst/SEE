using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.DataModel.DG.GraphIndex
{
    /// <summary>
    /// A source-location based node index that allows to search for graph nodes
    /// based on a file path and source line.
    /// </summary>
    internal class SourceRangeIndex
    {
        /// <summary>
        /// Creates the index for <paramref name="graph"/>.
        ///
        /// Parameter <paramref name="getPath"/> is used to index all nodes in the
        /// same file. It is assumed to yield a unique path for all nodes in the
        /// same file. This could be <see cref="GraphElement.Path()"/>, for instance,
        /// but it could as well be the fully qualified name of a class for languages
        /// where a class is declared completely in a single file.
        /// </summary>
        /// <param name="graph">Graph whose nodes are to be indexed.</param>
        /// <param name="getPath">Yields a unique path for a node; its result will be used as a
        /// key in <see cref="files"/> if different from null and non-empty; if it yields null, the
        /// node will be ignored silently; if it yields the empty string, a
        /// warning will be emitted.</param>
        public SourceRangeIndex(Graph graph, Func<Node, string> getPath)
        {
            foreach (Node root in graph.GetRoots())
            {
                BuildIndex(root, getPath);
            }
        }

        /// <summary>
        /// True if the source-range hierarchy is isomorphic to the node hierarchy
        /// of the underlying graph for which the index was calculated.
        ///
        /// There are two kinds of hierarchies: (1) the node hierarchy in the graph,
        /// which is determined syntactically, and (2) the range hierarchy here,
        /// which is determined by the nesting of source-code ranges.
        /// Because there may be nodes in the graph that do not have source-location information,
        /// the two hierarchies do not necessarily need to be isomorphic. The second hierarchy
        /// needs to be only homomorphic to the first one. That is, if N is a node in
        /// the range hierarchy and P its parent in the range hierarchy, then
        /// P must be an ancestor (not necessarily an immediate parent) of N in the node hierarchy.
        /// </summary>
        /// <returns>True if consistent.</returns>
        public bool IsIsomorphic()
        {
            Stack<SourceRange> stack = new();
            return files.Values.SelectMany(file => file.Children).All(IsHomomorphic);

            bool IsHomomorphic(SourceRange range)
            {
                bool result = true;
                if (stack.Count > 0)
                {
                    SourceRange parentRange = stack.Peek();
                    bool isDescendant = range.Node.IsDescendantOf(parentRange.Node);
                    if (!isDescendant)
                    {
                        Debug.LogError($"Range {range} is subsumed by {parentRange}, but {range.Node.ID} is "
                            + $"not a descendant of {parentRange.Node.ID} in the node hierarchy.\n");
                    }
                    result &= isDescendant;
                }
                stack.Push(range);
                result &= range.Children.All(IsHomomorphic);
                stack.Pop();
                return result;
            }
        }

        /// <summary>
        /// The source-code range index as a mapping of the path of a file
        /// onto <see cref="FileRanges"/>. The path is determined by a
        /// delegate provide by the client.
        ///
        /// The children of <see cref="FileRanges"/> are the code ranges
        /// for nodes in the graph whose declaration is contained in that file.
        /// </summary>
        private readonly Dictionary<string, FileRanges> files = new();

        /// <summary>
        /// Returns the innermost <paramref name="node"/> declared in a
        /// file with given full <paramref name="path"/> whose source-code
        /// range encloses the given source <paramref name="line"/>.
        /// If such a node exists, true is returned.
        ///
        /// If no such range exists, false is returned and
        /// <paramref name="node"/> is undefined.
        /// </summary>
        /// <param name="path">Full path of the filename in which to search.</param>
        /// <param name="line">Source line to be searched for.</param>
        /// <param name="node">Found node or undefined if no such node exists.</param>
        /// <returns>True if a node could be found.</returns>
        public bool TryGetValue(string path, int line, out Node node)
        {
            if (files.TryGetValue(path, out FileRanges file))
            {
                SourceRange range = file.Find(line);
                if (range == null)
                {
                    node = null;
                    return false;
                }
                else
                {
                    node = range.Node;
                    return true;
                }
            }
            else
            {
                node = null;
                return false;
            }
        }

        /// <summary>
        /// The number of source-code ranges in the index.
        /// </summary>
        public int Count => NumberOfRanges();

        /// <summary>
        /// Returns the number of source-code ranges in the index by
        /// recursing into the range hierarchy.
        /// </summary>
        /// <returns>Number of source-code ranges in the index.</returns>
        private int NumberOfRanges()
        {
            int count = 0;
            foreach (FileRanges file in files.Values)
            {
                foreach (SourceRange range in file.Children)
                {
                    CountRanges(range, ref count);
                }
            }

            return count;

            static void CountRanges(SourceRange range, ref int count)
            {
                count++;
                foreach (SourceRange child in range.Children)
                {
                    CountRanges(child, ref count);
                }
            }
        }

        /// <summary>
        /// Dumps the index.
        /// </summary>
        /// <remarks>Can be used for debugging.</remarks>
        private void Dump()
        {
            foreach ((string key, FileRanges value) in files)
            {
                Debug.Log($"*** {key} ***\n");
                DumpFile(value);
            }

            void DumpFile(FileRanges file)
            {
                int i = 1;
                foreach (SourceRange range in file.Children)
                {
                    DumpRange(i.ToString(), range);
                    i++;
                }
            }

            void DumpRange(string enumeration, SourceRange range)
            {
                Debug.Log($"{enumeration} {range}\n");
                int i = 1;
                foreach (SourceRange child in range.Children)
                {
                    DumpRange($"{enumeration}.{i}", child);
                    i++;
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="root"/> to the index and then
        /// recurses into its descendants to add these to the index, too.
        /// </summary>
        /// <param name="root">Root node of the graph.</param>
        /// <param name="getPath">Yields a unique path for a node; its result will be used as a
        /// key in <see cref="files"/> if different from null and non-empty; if it yields null, the
        /// node will be ignored silently; if it yields the empty string, a
        /// warning will be emitted.</param>
        private void BuildIndex(Node root, Func<Node, string> getPath)
        {
            AddToIndex(root, getPath);

            foreach (Node child in root.Children())
            {
                BuildIndex(child, getPath);
            }
        }

        /// <summary>
        /// Adds <paramref name="node"/> to the <see cref="files"/> index if it has a filename.
        /// If it does not have a filename, nothing happens.
        ///
        /// If <see cref="files"/> does not yet have an entry for the filename,
        /// a <see cref="FileRanges"/> under this filename will be added at top level.
        ///
        /// Let F be the <see cref="FileRanges"/> representing the file with the node's filename (full
        /// path). Then the <paramref name="node"/> is added via <see cref="FileRanges.Add(Node))"/>
        /// passing F.
        /// </summary>
        /// <param name="node">Graph node to be added.</param>
        /// <param name="getPath">Yields a unique path for a node; its result will be used as a
        /// key in <see cref="files"/> if different from null and non-empty; if it yields null, the
        /// node will be ignored silently; if it yields the empty string, a
        /// warning will be emitted.</param>
        private void AddToIndex(Node node, Func<Node, string> getPath)
        {
            string path = getPath(node);
            // Only nodes with a path can be added to the index because
            // the index is organized by paths. If getPath yields null, the
            // node is to be ignored.
            if (path != null)
            {
                if (path.Length > 0)
                {
                    // If we do not already have a File for path, we will add one to the index.
                    if (!files.TryGetValue(path, out FileRanges file))
                    {
                        files.Add(path, file = new FileRanges());
                    }
                    file.Add(node);
                }
                else
                {
                    Debug.LogWarning($"{node.ID} does not have a path. Will be ignored.\n");
                }
            }
        }
    }
}
