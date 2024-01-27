using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// A source-location based node index that allows to search for graph nodes
    /// based on a path and source line.
    /// </summary>
    internal class SourceRangeIndex
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
            Dump();
        }

        /// <summary>
        /// True if the source range index is consistent, that is, if the
        /// following conditions hold:
        ///
        /// (1) the ranges of the siblings at the same level do not overlap
        /// (2) the source-range hierarchy is isomporphic to the node hierarchy
        /// of the underlying graph for the index was calculated.
        /// </summary>
        /// <returns></returns>
        public bool IsConsistent()
        {
            bool result = true;

            Stack<Range> stack = new();

            foreach (File file in files.Values)
            {
                result &= NoOverlap(file);

                // There are two kinds of hierarchies: (1) the node hierarchy in the graph,
                // which is determined syntactically, and (2) the range hierarchy here,
                // which is determined by the nesting of source code ranges.
                // Because nodes in the graph that do not have source-location information,
                // the two hierarchies do not necessarily need to be isomorphic. The second hierarchy
                // needs to be only homomorphic to the first one. That is, if N is a node in
                // the range hierarchy and P its parent in the range hierarchy, then
                // P must be an ancestor of N in the node hierarchy.
                foreach (Range child in file.Children.Values)
                {
                    result &= IsHomomorphic(child);
                }
            }

            return result;

            bool IsHomomorphic(Range range)
            {
                bool result = true;
                if (stack.Count > 0)
                {
                    Range parentRange = stack.Peek();
                    bool isdescendant = range.Node.IsDescendantOf(parentRange.Node);
                    if (!isdescendant)
                    {
                        Debug.LogWarning($"Range {range} is subsumed by {parentRange}, but {range.Node.ID} is "
                            + "not a descendant of {parentRange.Node.ID} in the node hierarchy.\n");
                    }
                    result &= isdescendant;
                }
                stack.Push(range);
                foreach (Range child in range.Children.Values)
                {
                    result &= IsHomomorphic(child);
                }
                stack.Pop();
                return result;
            }

            static bool NoOverlap(File file)
            {
                bool result = true;

                // Ranges at the same level must not overlap.
                for (int i = 0; i < file.Children.Count - 1; i++)
                {
                    try
                    {
                        Range pred = file.Children[i];
                        Range succ = file.Children[i + 1];
                        if (pred.End >= succ.Start)
                        {
                            Debug.LogWarning($"The ranges of {pred.Node.ID} (ends at line {pred.End}) "
                                + "and {succ.Node.ID} (starts at line {succ.Start}) overlap.\n");
                            result = false;
                        }
                    }
                    catch
                    {
                        Dump(file.Children);
                        throw;
                    }
                }

                foreach (Range child in file.Children.Values)
                {
                    result &= NoOverlap(child);
                }

                return result;
            }
        }

        private static void Dump(SortedList<int, Range> children)
        {
            foreach (var item in children)
            {
                Debug.Log($"{item.Key} => {item.Value}\n");
            }
        }

        /// <summary>
        /// A representation of the source-code ranges of a file. This
        /// data structure is used at the top level of the index.
        /// </summary>
        private class File
        {
            /// <summary>
            /// The children sorted by the SourceLine.
            /// </summary>
            public readonly SortedList<int, Range> Children = new();

            /// <summary>
            /// Adds the source-code range of the given <paramref name="node"/>
            /// to the list of children.
            /// </summary>
            /// <param name="node">node whose source-code range is to be added</param>
            public void Add(Node node)
            {
                int? sourceLine = node.SourceLine;
                if (sourceLine.HasValue)
                {
                    Range descendant = Find(sourceLine.Value);
                    if (descendant == null)
                    {
                        Children.Add(sourceLine.Value, new Range(sourceLine, node.EndLine(), node));
                    }
                    else
                    {
                        descendant.Add(node);
                    }
                }
                else
                {
                    Debug.LogWarning($"{node.ID} does not have a source line. Will be ignored.\n");
                }
            }

            /// <summary>
            /// Returns the innermost source-code range enclosing the given
            /// source <paramref name="line"/>. If no such range exists,
            /// <c>null</c> is returned.
            /// </summary>
            /// <param name="line">source line to be searched for</param>
            /// <returns>innermost source-code range or <c>null</c></returns>
            public Range Find(int line)
            {
                // FIXME: Use binary search instead.
                foreach (Range range in Children.Values)
                {
                    if (range.Start <=  line && line <= range.End)
                    {
                        Range child = range.Find(line);
                        return child ?? range;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// A representation of a source-code range in the index.
        /// </summary>
        private class Range : File
        {
            /// <summary>
            /// Start line of the range.
            /// </summary>
            public int? Start;
            /// <summary>
            /// End line of the range.
            /// </summary>
            public int? End;
            /// <summary>
            /// The node whose source-code range is represented.
            /// </summary>
            public Node Node;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="start">Start line of the range.</param>
            /// <param name="end">End line of the range.</param>
            /// <param name="node">The node whose source-code range is represented.</param>
            public Range(int? start, int? end, Node node)
            {
                Assert.IsNotNull(node);
                Assert.IsTrue(start.HasValue);
                Assert.IsTrue(end.HasValue);
                Assert.IsTrue(start <= end);

                Start = start;
                End = end;
                Node = node;
            }

            public override string ToString()
            {
                return $"{Node.ID}@[{Start}, {End}]";
            }
        }

        /// <summary>
        /// The source-code range index as a mapping of the path of a file
        /// onto <see cref="File"/>. The children of <see cref="File"/>
        /// are the code ranges for nodes in the graph whose declaration
        /// is contained in that file.
        /// </summary>
        private readonly Dictionary<string, File> files = new();

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
            if (files.TryGetValue(path, out File file))
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
            foreach (File file in files.Values)
            {
                foreach (Range range in file.Children.Values)
                {
                    CountRanges(range, ref count);
                }
            }

            return count;

            static void CountRanges(Range range, ref int count)
            {
                count++;
                foreach (Range child in range.Children.Values)
                {
                    CountRanges(child, ref count);
                }
            }
        }

        /// <summary>
        /// Dumps the index for debugging.
        /// </summary>
        private void Dump()
        {
            foreach (var entry in files)
            {
                Debug.Log($"*** {entry.Key} ***\n");
                DumpFile(entry.Value);
            }

            void DumpFile(File file)
            {
                int i = 1;
                foreach (Range range in file.Children.Values)
                {
                    DumpRange(range, i.ToString());
                    i++;
                }
            }

            void DumpRange(Range range, string prefix)
            {
                Debug.Log($"{prefix} {range}\n");
                int i = 1;
                foreach (Range child in range.Children.Values)
                {
                    DumpRange(child, prefix + "." + i.ToString());
                    i++;
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="root"/> to the index and then
        /// recurses into its descendants.
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
        /// a <see cref="File"/> under this filename will be added at top level.
        ///
        /// Let F be the <see cref="File"/> representing the file with the node's filename (full
        /// path). Then the <paramref name="node"/> is added via <see cref="File.Add(Node))"/>
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
                if (!files.TryGetValue(path, out File file))
                {
                    files.Add(path, file = new File());
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
