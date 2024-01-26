using System.Collections.Generic;
using UnityEngine;

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
                Traverse(root);
            }
            Dump();
        }

        public bool IsConsistent()
        {
            bool result = true;

            Stack<Range> stack = new();

            foreach (File file in files.Values)
            {
                // Ranges at the same level must not overlap.
                for (int i = 0; i < file.Children.Count - 1; i++)
                {
                    Range pred = file.Children[i];
                    Range succ = file.Children[i + 1];
                    if (pred.End >= succ.Start)
                    {
                        Debug.LogWarning($"The ranges of {pred.Node.ID} (ends at line {pred.End}) and {succ.Node.ID} (starts at line {succ.Start}) overlap.\n");
                        result = false;
                    }
                }

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
        }

        private class File
        {
            public readonly SortedList<int, Range> Children = new();

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

        private class Range : File
        {
            public int? Start;
            public int? End;
            public Node Node;
            public Range(int? start, int? end, Node node)
            {
                Start = start;
                End = end;
                Node = node;
            }

            public override string ToString()
            {
                return $"{Node.ID}@[{Start}, {End}]";
            }
        }

        private readonly Dictionary<string, File> files = new();

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

        private void Traverse(Node node)
        {
            AddToIndex(node);

            foreach (Node child in node.Children())
            {
                Traverse(child);
            }
        }

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
