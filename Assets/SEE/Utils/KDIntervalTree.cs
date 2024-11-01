using System;
using System.Collections.Generic;
using System.Linq;
using Supercluster.KDTree;
using Supercluster.KDTree.Utilities;
using Range = SEE.DataModel.DG.Range;

namespace SEE.Utils
{
    /// <summary>
    /// An interval tree represented as an augmented kd-tree (specifically, a 2D tree),
    /// where the first dimension represents line numbers and the second dimension represents character offsets.
    /// We augment it to an interval tree by storing the maximum end line and character in each node.
    /// The key of a node will be the start line and character of the range.
    /// This allows for stabbing queries, that is, finding all intervals that contain a given interval,
    /// in O(log n) time (on average).
    /// </summary>
    /// <typeparam name="E">The type of elements stored in the tree.</typeparam>
    public class KDIntervalTree<E>
    {
        /// <summary>
        /// A navigator for the kd-tree, allowing it to be used as a conventional binary tree.
        /// </summary>
        private readonly BinaryTreeNavigator<int[], RangeTreeNode> Navigator;

        /// <summary>
        /// Constructs an interval tree from a collection of elements, where each element is associated with a range.
        /// </summary>
        /// <param name="elements">The elements to construct the interval tree from.</param>
        /// <param name="rangeSelector">A function that returns the range of an element.</param>
        public KDIntervalTree(IEnumerable<E> elements, Func<E, Range> rangeSelector)
        {
            RangeTreeNode[] rangeNodes = elements.Select(x => new RangeTreeNode(rangeSelector(x), x)).ToArray();
            int[][] points = rangeNodes.Select(x => new[] { x.Range.StartLine, x.Range.StartCharacter ?? 0 }).ToArray();
            KDTree<int, RangeTreeNode> rangeTree = new(2, points: points, nodes: rangeNodes, metric: Distance,
                                                       searchWindowMinValue: 0, searchWindowMaxValue: int.MaxValue);
            Navigator = rangeTree.Navigator;

            AugmentTree(Navigator);
        }

        /// <summary>
        /// Finds the tightest (i.e., of minimal length) intervals that contain the given interval.
        /// </summary>
        /// <param name="queryRange">The interval to find containing intervals for.</param>
        /// <returns>The tightest intervals that contain the given interval.</returns>
        public IEnumerable<E> Stab(Range queryRange)
        {
            // NOTE: Not a regular stabbing query, we choose the interval with minimal length.
            List<RangeTreeNode> result = new();
            Query(Navigator, queryRange, result);
            return result.Select(x => x.Element);
        }

        /// <summary>
        /// Queries the interval tree accessible by the given <paramref name="navigator"/> for the tightest intervals
        /// that contain the given <paramref name="queryRange"/>.
        /// </summary>
        /// <param name="navigator">The navigator to the interval tree.</param>
        /// <param name="queryRange">The interval to find containing intervals for.</param>
        /// <param name="results">The list to store the results in.</param>
        private static void Query(BinaryTreeNavigator<int[], RangeTreeNode> navigator, Range queryRange, IList<RangeTreeNode> results)
        {
            if (navigator?.Node == null)
            {
                return;
            }

            // Range is to the right of any nodes in this subtree.
            if (queryRange.StartLine > navigator.Node.MaxEndLine ||
                (queryRange.StartLine == navigator.Node.MaxEndLine &&
                    queryRange.StartCharacter >= navigator.Node.MaxEndCharacter)) // >= because end is exclusive.
            {
                return;
            }

            // We want to add only minimal ranges.
            if (navigator.Node.Range.Contains(queryRange))
            {
                // Comparisons between ranges are not transitive,
                // so it does not suffice to check against only one minimum.
                List<int> comparisons = results.Select(x => navigator.Node.Range.CompareTo(x.Range)).ToList();
                if (comparisons.Count(x => x < 0) > 0)
                {
                    // If this range is smaller than some other ranges, we remove those bigger ranges and add this one.
                    results.RemoveWhere(x => navigator.Node.Range.CompareTo(x.Range) < 0);
                    results.Add(navigator.Node);
                }
                else if (comparisons.All(x => x == 0))
                {
                    // Other ranges are equally minimal, so we just add this one.
                    results.Add(navigator.Node);
                }
                // Otherwise, this range is not minimal, so we don't add it.
            }

            // Query the left subtree.
            Query(navigator.Left, queryRange, results);

            // Range is to the left of this interval.
            // Because the key of a node is the start line and character, all nodes in the right subtree
            // will have a start line greater than the query range, so we don't need to search there.
            if (queryRange.EndLine < navigator.Node.Range.StartLine ||
                (queryRange.EndLine == navigator.Node.Range.StartLine
                    && queryRange.EndCharacter <= navigator.Node.Range.StartCharacter)) // <= because end is exclusive.
            {
                return;
            }

            // Query the right subtree.
            Query(navigator.Right, queryRange, results);
        }

        private static double Distance(int[] first, int[] second)
        {
            // Since we are not using nearest-neighbour search, we needn't implement this.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Augments the interval tree with the maximum end line and character in each node.
        /// </summary>
        /// <param name="navigator">The navigator to the interval tree.</param>
        private static void AugmentTree(BinaryTreeNavigator<int[], RangeTreeNode> navigator)
        {
            // To make this a working interval tree, we need to augment it with the maximum end line and character.
            // We do this by traversing the tree in post-order and propagating the maximum end line and character
            // upwards in the tree.

            int maxEndLine = navigator.Node.Range.EndLine;
            int? maxEndCharacter = navigator.Node.Range.EndCharacter;
            if (navigator.Left is { Node: { } left })
            {
                AugmentTree(navigator.Left);
                maxEndLine = Math.Max(maxEndLine, left.MaxEndLine);
                maxEndCharacter = Math.Max(maxEndCharacter ?? 0, left.MaxEndCharacter ?? 0);
            }
            if (navigator.Right is { Node: { } right })
            {
                AugmentTree(navigator.Right);
                maxEndLine = Math.Max(maxEndLine, right.MaxEndLine);
                maxEndCharacter = Math.Max(maxEndCharacter ?? 0, right.MaxEndCharacter ?? 0);
            }

            navigator.Node.MaxEndLine = maxEndLine;
            navigator.Node.MaxEndCharacter = maxEndCharacter;
        }

        /// <summary>
        /// A node in the interval tree, representing a range and an element associated with it.
        /// </summary>
        /// <param name="Range">The range of the node.</param>
        /// <param name="Element">The element associated with the range.</param>
        private record RangeTreeNode(Range Range, E Element)
        {
            /// <summary>
            /// The maximum end line of the range in the subtree rooted at this node.
            /// </summary>
            public int MaxEndLine;

            /// <summary>
            /// The maximum end character of the range in the subtree rooted at this node.
            /// </summary>
            public int? MaxEndCharacter;
        }
    }
}
