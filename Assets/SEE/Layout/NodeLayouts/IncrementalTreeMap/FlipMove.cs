using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A flip move is a kind of <see cref="LocalMove"/>.
    /// It rotates two nodes by 90 degrees.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class FlipMove : LocalMove
    {
        /// <summary>
        /// Whether the rotation is clockwise.
        /// </summary>
        private readonly bool clockwise;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node1">One affected node.</param>
        /// <param name="node2">Other affected node.</param>
        /// <param name="clockwise">Whether the rotation is clockwise.</param>
        public FlipMove(Node node1, Node node2, bool clockwise)
        {
            this.Node1 = node1;
            this.Node2 = node2;
            this.clockwise = clockwise;
        }

        override public void Apply()
        {
            IDictionary<Direction, Segment> segmentsNode1 = Node1.SegmentsDictionary();
            IDictionary<Direction, Segment> segmentsNode2 = Node2.SegmentsDictionary();

            if (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left])
            {
                // [Node1][Node2]
                FlipOnVerticalSegment(leftNode: Node1, rightNode: Node2);
            }
            else if (segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                // [Node2][Node1]
                FlipOnVerticalSegment(leftNode: Node2, rightNode: Node1);
            }
            else if (segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower])
            {
                // [Node2]
                // [Node1]
                FlipOnHorizontalSegment(lowerNode: Node1, upperNode: Node2);
            }
            else if (segmentsNode1[Direction.Lower] == segmentsNode2[Direction.Upper])
            {
                // [Node1]
                // [Node2]
                FlipOnHorizontalSegment(lowerNode: Node2, upperNode: Node1);
            }
            else
            {
                throw new ArgumentException("Can't apply flip move.");
            }
        }

        override public LocalMove Clone(IDictionary<string, Node> cloneMap)
        {
            return new FlipMove(cloneMap[Node1.ID], cloneMap[Node2.ID], clockwise);
        }

        /// <summary>
        /// Applies the local move for the case that the nodes are separated vertically.
        /// </summary>
        /// <param name="leftNode">The node on the <see cref="Direction.Left"/> side.</param>
        /// <param name="rightNode">The node on the <see cref="Direction.Right"/> side.</param>
        private void FlipOnVerticalSegment(Node leftNode, Node rightNode)
        {
            // clockwise            anticlockwise
            // [l][r] -> [lll]      [l][r] -> [rrr]
            // [l][r]    [rrr]      [l][r]    [lll]

            // adjust rectangles
            double width = leftNode.Rectangle.Width + rightNode.Rectangle.Width;
            double ratio = leftNode.Rectangle.Area() / (leftNode.Rectangle.Area() + rightNode.Rectangle.Area());

            leftNode.Rectangle.Width = width;
            rightNode.Rectangle.Width = width;
            rightNode.Rectangle.X = leftNode.Rectangle.X;

            leftNode.Rectangle.Depth *= ratio;
            rightNode.Rectangle.Depth *= (1 - ratio);
            if (clockwise)
            {
                leftNode.Rectangle.Z = rightNode.Rectangle.Z + rightNode.Rectangle.Depth;
            }
            else
            {
                rightNode.Rectangle.Z = leftNode.Rectangle.Z + leftNode.Rectangle.Depth;
            }

            // switch segments
            Segment newRightSegmentForLeftNode = rightNode.SegmentsDictionary()[Direction.Right];
            Segment newLeftSegmentForRightNode = leftNode.SegmentsDictionary()[Direction.Left];
            Segment middle = leftNode.SegmentsDictionary()[Direction.Right];
            middle.IsVertical = false;

            leftNode.RegisterSegment(newRightSegmentForLeftNode, Direction.Right);
            rightNode.RegisterSegment(newLeftSegmentForRightNode, Direction.Left);

            if (clockwise)
            {
                leftNode.RegisterSegment(middle, Direction.Lower);
                rightNode.RegisterSegment(middle, Direction.Upper);
            }
            else
            {
                leftNode.RegisterSegment(middle, Direction.Upper);
                rightNode.RegisterSegment(middle, Direction.Lower);
            }
        }

        /// <summary>
        /// Applies the local move for the case that the nodes are separated horizontally.
        /// </summary>
        /// <param name="lowerNode">The node on the <see cref="Direction.Lower"/> side.</param>
        /// <param name="upperNode">The node on the <see cref="Direction.Upper"/> side.</param>
        private void FlipOnHorizontalSegment(Node lowerNode, Node upperNode)
        {
            // clockwise                anticlockwise
            // [uuu]  ->  [l][u]        [uuu]  ->  [u][l]
            // [lll]      [l][u]        [lll]      [u][l]

            // adjust rectangles
            double depth = lowerNode.Rectangle.Depth + upperNode.Rectangle.Depth;
            double ratio = lowerNode.Rectangle.Area() / (lowerNode.Rectangle.Area() + upperNode.Rectangle.Area());

            lowerNode.Rectangle.Depth = depth;
            upperNode.Rectangle.Depth = depth;
            upperNode.Rectangle.Z = lowerNode.Rectangle.Z;

            lowerNode.Rectangle.Width *= ratio;
            upperNode.Rectangle.Width *= (1 - ratio);
            if (clockwise)
            {
                upperNode.Rectangle.X = lowerNode.Rectangle.X + lowerNode.Rectangle.Width;
            }
            else
            {
                lowerNode.Rectangle.X = upperNode.Rectangle.X + upperNode.Rectangle.Width;
            }

            // switch segments
            Segment newUpperSegmentForLowerNode = upperNode.SegmentsDictionary()[Direction.Upper];
            Segment newLowerSegmentForUpperNode = lowerNode.SegmentsDictionary()[Direction.Lower];
            Segment middle = lowerNode.SegmentsDictionary()[Direction.Upper];
            middle.IsVertical = true;

            lowerNode.RegisterSegment(newUpperSegmentForLowerNode, Direction.Upper);
            upperNode.RegisterSegment(newLowerSegmentForUpperNode, Direction.Lower);

            if (clockwise)
            {
                lowerNode.RegisterSegment(middle, Direction.Right);
                upperNode.RegisterSegment(middle, Direction.Left);
            }
            else
            {
                lowerNode.RegisterSegment(middle, Direction.Left);
                upperNode.RegisterSegment(middle, Direction.Right);
            }
        }

        /// <summary>
        /// Method for better overview in debugger.
        /// </summary>
        private string DebuggerDisplay => "flip " + Node1.ID + " " + Node2.ID + " {clockwise}";
    }
}
