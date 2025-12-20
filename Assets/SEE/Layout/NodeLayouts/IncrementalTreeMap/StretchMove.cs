using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A stretch move is a kind of <see cref="LocalMove"/>.
    /// It expands a node over another.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class StretchMove : LocalMove
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node1">One affected node.</param>
        /// <param name="node2">The other affected node.</param>
        public StretchMove(Node node1, Node node2)
        {
            Node1 = node1;
            Node2 = node2;
        }

        override public void Apply()
        {
            IDictionary<Direction, Segment> segmentsNode1 = Node1.SegmentsDictionary();
            IDictionary<Direction, Segment> segmentsNode2 = Node2.SegmentsDictionary();
            if (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left]
                || segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                (Node leftNode, Node rightNode) = (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left])
                    ? (Node1, Node2)
                    : (Node2, Node1);
                if (leftNode.Rectangle.Depth >= rightNode.Rectangle.Depth)
                {
                    StretchRightOverVertical(leftNode: leftNode, rightNode: rightNode);
                }
                else
                {
                    StretchLeftOverVertical(leftNode: leftNode, rightNode: rightNode);
                }
            }
            else if (segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower]
                     || segmentsNode1[Direction.Lower] == segmentsNode2[Direction.Upper])
            {
                (Node lowerNode, Node upperNode) = (segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower])
                    ? (Node1, Node2)
                    : (Node2, Node1);
                if (lowerNode.Rectangle.Width >= upperNode.Rectangle.Width)
                {
                    StretchUpperOverHorizontal(lowerNode: lowerNode, upperNode: upperNode);
                }
                else
                {
                    StretchLowerOverHorizontal(lowerNode: lowerNode, upperNode: upperNode);
                }
            }
            else
            {
                throw new ArgumentException("Can't apply stretch move");
            }
        }

        override public LocalMove Clone(IDictionary<string, Node> cloneMap)
        {
            return new StretchMove(cloneMap[Node1.ID], cloneMap[Node2.ID]);
        }

        /// <summary>
        /// Apply for the case that:
        /// - the nodes are seperated vertically
        /// - the left node is the expanding node
        /// </summary>
        /// <param name="leftNode">Node on the <see cref="Direction.Left"/> side.</param>
        /// <param name="rightNode">Node on the <see cref="Direction.Right"/> side.</param>
        private static void StretchLeftOverVertical(Node leftNode, Node rightNode)
        {
            // along lower           along upper
            //    [r]         [r]    [l][r]  ->  [llll]
            // [l][r]  ->  [llll]       [r]         [r]
            IDictionary<Direction, Segment> segmentsLeftNode = leftNode.SegmentsDictionary();
            IDictionary<Direction, Segment> segmentsRightNode = rightNode.SegmentsDictionary();
            bool alongLowerSegment = segmentsLeftNode[Direction.Lower] == segmentsRightNode[Direction.Lower];
            // adjust rectangles
            leftNode.Rectangle.Width += rightNode.Rectangle.Width;
            rightNode.Rectangle.Depth -= leftNode.Rectangle.Depth;
            if (alongLowerSegment)
            {
                rightNode.Rectangle.Z = leftNode.Rectangle.Z + leftNode.Rectangle.Depth;
            }

            // switch segments
            leftNode.RegisterSegment(segmentsRightNode[Direction.Right], Direction.Right);
            if (alongLowerSegment)
            {
                rightNode.RegisterSegment(segmentsLeftNode[Direction.Upper], Direction.Lower);
            }
            else
            {
                rightNode.RegisterSegment(segmentsLeftNode[Direction.Lower], Direction.Upper);
            }
        }

        /// <summary>
        /// Apply for the case that:
        /// - the nodes are seperated vertically
        /// - the right node is the expanding node
        /// </summary>
        /// <param name="leftNode">Node on the <see cref="Direction.Left"/> side.</param>
        /// <param name="rightNode">Node on the <see cref="Direction.Right"/> side.</param>
        private static void StretchRightOverVertical(Node leftNode, Node rightNode)
        {
            // along lower           along upper
            // [l]         [l]       [l][r]  ->  [r][r]
            // [l][r]  ->  [r][r]    [l]         [l]
            IDictionary<Direction, Segment> segmentsLeftNode = leftNode.SegmentsDictionary();
            IDictionary<Direction, Segment> segmentsRightNode = rightNode.SegmentsDictionary();
            bool alongLowerSegment = segmentsLeftNode[Direction.Lower] == segmentsRightNode[Direction.Lower];
            // adjust rectangles
            rightNode.Rectangle.Width += leftNode.Rectangle.Width;
            rightNode.Rectangle.X = leftNode.Rectangle.X;
            leftNode.Rectangle.Depth -= rightNode.Rectangle.Depth;
            if (alongLowerSegment)
            {
                leftNode.Rectangle.Z = rightNode.Rectangle.Z + rightNode.Rectangle.Depth;
            }

            // switch segments
            rightNode.RegisterSegment(segmentsLeftNode[Direction.Left], Direction.Left);
            if (alongLowerSegment)
            {
                leftNode.RegisterSegment(segmentsRightNode[Direction.Upper], Direction.Lower);
            }
            else
            {
                leftNode.RegisterSegment(segmentsRightNode[Direction.Lower], Direction.Upper);
            }
        }

        /// <summary>
        /// Apply for the case that:
        /// - the nodes are seperated horizontally
        /// - the lower node is the expanding node
        /// </summary>
        /// <param name="lowerNode">Node on the <see cref="Direction.Lower"/> side.</param>
        /// <param name="upperNode">Node on the <see cref="Direction.Upper"/> side.</param>
        private static void StretchLowerOverHorizontal(Node lowerNode, Node upperNode)
        {
            // along left           along right
            // [uuuu] ->  [l][u]    [uuuu]  ->  [u][l]
            // [l]        [l]          [l]         [l]
            IDictionary<Direction, Segment> segmentsLowerNode = lowerNode.SegmentsDictionary();
            IDictionary<Direction, Segment> segmentsUpperNode = upperNode.SegmentsDictionary();
            bool alongLeftSegment = segmentsLowerNode[Direction.Left] == segmentsUpperNode[Direction.Left];
            // adjust rectangles
            lowerNode.Rectangle.Depth += upperNode.Rectangle.Depth;
            upperNode.Rectangle.Width -= lowerNode.Rectangle.Width;
            if (alongLeftSegment)
            {
                upperNode.Rectangle.X = lowerNode.Rectangle.X + lowerNode.Rectangle.Width;
            }

            // switch segments
            lowerNode.RegisterSegment(segmentsUpperNode[Direction.Upper], Direction.Upper);
            if (alongLeftSegment)
            {
                upperNode.RegisterSegment(segmentsLowerNode[Direction.Right], Direction.Left);
            }
            else
            {
                upperNode.RegisterSegment(segmentsLowerNode[Direction.Left], Direction.Right);
            }
        }

        /// <summary>
        /// Apply for the case that:
        /// - the nodes are seperated horizontally
        /// - the upper node is the expanding node
        /// </summary>
        /// <param name="lowerNode">Node on the <see cref="Direction.Lower"/> side.</param>
        /// <param name="upperNode">Node on the <see cref="Direction.Upper"/> side.</param>
        private static void StretchUpperOverHorizontal(Node lowerNode, Node upperNode)
        {
            // along left           along right
            // [u]    ->  [u]          [u]  ->      [u]
            // [llll]     [u][l]    [llll]       [l][u]
            IDictionary<Direction, Segment> segmentsLowerNode = lowerNode.SegmentsDictionary();
            IDictionary<Direction, Segment> segmentsUpperNode = upperNode.SegmentsDictionary();
            bool alongLeftSegment = segmentsLowerNode[Direction.Left] == segmentsUpperNode[Direction.Left];
            // adjust rectangles
            upperNode.Rectangle.Depth += lowerNode.Rectangle.Depth;
            upperNode.Rectangle.Z = lowerNode.Rectangle.Z;

            lowerNode.Rectangle.Width -= upperNode.Rectangle.Width;
            if (alongLeftSegment)
            {
                lowerNode.Rectangle.X = upperNode.Rectangle.X + upperNode.Rectangle.Width;
            }

            // switch segments
            upperNode.RegisterSegment(segmentsLowerNode[Direction.Lower], Direction.Lower);
            if (alongLeftSegment)
            {
                lowerNode.RegisterSegment(segmentsUpperNode[Direction.Right], Direction.Left);
            }
            else
            {
                lowerNode.RegisterSegment(segmentsUpperNode[Direction.Left], Direction.Right);
            }
        }

        /// <summary>
        /// Method for better overview in debugger.
        /// </summary>
        private string DebuggerDisplay => "stretch " + Node1.ID + " " + Node2.ID;
    }
}
