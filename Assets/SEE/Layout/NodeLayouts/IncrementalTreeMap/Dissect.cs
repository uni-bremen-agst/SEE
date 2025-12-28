using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// Provides an algorithm to create a new layout.
    /// </summary>
    internal static class Dissect
    {
        /// <summary>
        /// Calculates new layout for <paramref name="nodes"/>.
        /// Assigns rectangles and segments to each node in <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">Nodes to be laid out.</param>
        /// <param name="rectangle">Rectangle in which the nodes should be placed.</param>
        public static void Apply(IEnumerable<Node> nodes, Rectangle rectangle)
        {
            Node[] nodesArray = nodes.ToArray();
            Array.Sort(nodesArray, (x, y) => (x.DesiredSize.CompareTo(y.DesiredSize)));

            if (Math.Abs(nodesArray.Sum(x => x.DesiredSize) - rectangle.Area()) >= rectangle.Area() * Math.Pow(10, -3)
                && nodesArray.Length > 1)
            {
                Debug.LogWarning("Dissect: nodes don't fit in rectangle");
            }

            Apply(rectangle,
                nodesArray,
                leftBound: new Segment(true, true),
                rightBound: new Segment(true, true),
                upperBound: new Segment(true, false),
                lowerBound: new Segment(true, false));
        }

        /// <summary>
        /// Calculates the layout by slicing the <paramref name="rectangle"/> recursively.
        /// </summary>
        /// <param name="rectangle">The rectangle to be sliced.</param>
        /// <param name="nodes">Nodes to be placed in rectangle.</param>
        /// <param name="leftBound">The <see cref="Direction.Left"/> segment of the rectangle.</param>
        /// <param name="rightBound">The <see cref="Direction.Right"/> segment of the rectangle.</param>
        /// <param name="upperBound">The <see cref="Direction.Upper"/> segment of the rectangle.</param>
        /// <param name="lowerBound">The <see cref="Direction.Lower"/> segment of the rectangle.</param>
        private static void Apply(Rectangle rectangle,
            Node[] nodes,
            Segment leftBound,
            Segment rightBound,
            Segment upperBound,
            Segment lowerBound)
        {
            if (nodes.Length == 1)
            {
                Node node = nodes[0];
                node.Rectangle = rectangle;
                node.RegisterSegment(leftBound, Direction.Left);
                node.RegisterSegment(rightBound, Direction.Right);
                node.RegisterSegment(lowerBound, Direction.Lower);
                node.RegisterSegment(upperBound, Direction.Upper);
            }
            else
            {
                int splitIndex = GetSplitIndex(nodes);
                Node[] nodes1 = nodes[..splitIndex];
                Node[] nodes2 = nodes[splitIndex..];

                float ratio = nodes1.Sum(x => x.DesiredSize) / nodes.Sum(x => x.DesiredSize);

                Rectangle rectangle1 = rectangle.Clone();
                Rectangle rectangle2 = rectangle.Clone();
                if (rectangle.Width >= rectangle.Depth)
                {
                    rectangle1.Width *= ratio;
                    rectangle2.Width *= (1 - ratio);
                    rectangle2.X = rectangle1.X + rectangle1.Width;
                    Segment newSegment = new Segment(false, true);

                    Dissect.Apply(rectangle1, nodes1,
                        leftBound: leftBound,
                        rightBound: newSegment,
                        upperBound: upperBound,
                        lowerBound: lowerBound);

                    Dissect.Apply(rectangle2, nodes2,
                        leftBound: newSegment,
                        rightBound: rightBound,
                        upperBound: upperBound,
                        lowerBound: lowerBound);
                }
                else
                {
                    rectangle1.Depth *= ratio;
                    rectangle2.Depth *= (1 - ratio);
                    rectangle2.Z = rectangle1.Z + rectangle1.Depth;
                    Segment newSegment = new Segment(false, false);

                    Dissect.Apply(rectangle1, nodes1,
                        leftBound: leftBound,
                        rightBound: rightBound,
                        upperBound: newSegment,
                        lowerBound: lowerBound);

                    Dissect.Apply(rectangle2, nodes2,
                        leftBound: leftBound,
                        rightBound: rightBound,
                        upperBound: upperBound,
                        lowerBound: newSegment);
                }
            }
        }

        /// <summary>
        /// Calculates the index that separates the <paramref name="nodes"/> array into two
        /// partitions. The specific split should result in good visual quality.
        /// </summary>
        /// <param name="nodes">Sorted list.</param>
        /// <returns>Index.</returns>
        private static int GetSplitIndex(Node[] nodes)
        {
            float totalSize = nodes.Sum(node => node.DesiredSize);
            if (totalSize <= nodes.Last().DesiredSize * 3)
            {
                return nodes.Length - 1;
            }
            else
            {
                for (int i = 1; i <= nodes.Length + 1; i++)
                {
                    if (nodes[..i].Sum(x => x.DesiredSize) * 3 >= totalSize)
                    {
                        return i;
                    }
                }
            }

            throw new ArgumentException("We should never arrive here.\n");
        }
    }
}
