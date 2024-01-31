using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel.DG.SourceRange
{
    /// <summary>
    /// A source-location based node index that allows to search for graph nodes
    /// based on a file path and source line.
    /// </summary>
    internal partial class SourceRangeIndex
    {
        /// <summary>
        /// Creates the index for <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph whose nodes are to be indexed</param>
        public SourceRangeIndex(Graph graph)
        {
            foreach (Node root in graph.GetRoots())
            {
                BuildIndex(root);
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
        /// <returns>true if consistent</returns>
        public bool IsIsomorphic()
        {
            bool result = true;

            Stack<Range> stack = new();

            return files.Values.SelectMany(file => file.Children).All(IsHomomorphic);

            bool IsHomomorphic(Range range)
            {
                bool result = true;
                if (stack.Count > 0)
                {
                    Range parentRange = stack.Peek();
                    bool isdescendant = range.Node.IsDescendantOf(parentRange.Node);
                    if (!isdescendant)
                    {
                        Debug.LogError($"Range {range} is subsumed by {parentRange}, but {range.Node.ID} is "
                            + $"not a descendant of {parentRange.Node.ID} in the node hierarchy.\n");
                    }
                    result &= isdescendant;
                }
                stack.Push(range);
                result &= range.Children.All(IsHomomorphic);
                stack.Pop();
                return result;
            }
        }

        /// <summary>
        /// The source-code range index as a mapping of the path of a file
        /// onto <see cref="FileRanges"/>. The children of <see cref="FileRanges"/>
        /// are the code ranges for nodes in the graph whose declaration
        /// is contained in that file.
        /// </summary>
        private readonly Dictionary<string, FileRanges> files = new();

        /// <summary>
        /// Returns the innermost <paramref name="node"/> declared in a
        /// file with given full <paramref name="path"/> whose source-code
        /// range encloses the given source <paramref name="line"/>.
        /// If such a node exists, <c>true</c> is returned.
        ///
        /// If no such range exists, <c>false</c> is returned and
        /// <paramref name="node"/> is undefined.
        /// </summary>
        /// <param name="path">full path of the filename in which to search</param>
        /// <param name="line">source line to be searched for</param>
        /// <param name="node">found node or undefined if no such node exists</param>
        /// <returns>true if a node could be found</returns>
        public bool TryGetValue(string path, int line, out Node node)
        {
            if (files.TryGetValue(path, out FileRanges file))
            {
                Range range = file.Find(line);
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
        /// <returns>number of source-code ranges in the index</returns>
        private int NumberOfRanges()
        {
            int count = 0;
            foreach (FileRanges file in files.Values)
            {
                foreach (Range range in file.Children)
                {
                    CountRanges(range, ref count);
                }
            }

            return count;

            static void CountRanges(Range range, ref int count)
            {
                count++;
                foreach (Range child in range.Children)
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
            foreach (var entry in files)
            {
                Debug.Log($"*** {entry.Key} ***\n");
                DumpFile(entry.Value);
            }

            void DumpFile(FileRanges file)
            {
                int i = 1;
                foreach (Range range in file.Children)
                {
                    DumpRange(range, i.ToString());
                    i++;
                }
            }

            void DumpRange(Range range, string prefix)
            {
                Debug.Log($"{prefix} {range}\n");
                int i = 1;
                foreach (Range child in range.Children)
                {
                    DumpRange(child, prefix + "." + i.ToString());
                    i++;
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="root"/> to the index and then
        /// recurses into its descendants to add these to the index, too.
        /// </summary>
        /// <param name="root">root node of the graph</param>
        private void BuildIndex(Node root)
        {
            AddToIndex(root);

            foreach (Node child in root.Children())
            {
                BuildIndex(child);
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
        /// <param name="node">graph node to be added</param>
        private void AddToIndex(Node node)
        {
            // Only nodes with a filename can be added to the index because
            // the index is organized by filenames.
            if (!string.IsNullOrEmpty(node.Filename))
            {
                // Note: path cannot be empty because node.Filename is not empty.
                string path = node.Path();
                // If we do not already have a File for path, we will add one to the index.
                if (!files.TryGetValue(path, out FileRanges file))
                {
                    files.Add(path, file = new FileRanges());
                }
                file.Add(node);
            }
            else
            {
                Debug.LogWarning($"{node.ID} does not have a filename. Will be ignored.\n");
            }
        }
    }
}
