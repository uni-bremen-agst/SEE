using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A local moves a is small transformation of a layout that only affects two nodes.
    /// There are two types of local moves: <see cref="FlipMove"/> and <see cref="StretchMove"/>.
    /// </summary>
    internal abstract class LocalMove
    {
        /// <summary>
        /// one affected node
        /// </summary>
        public Node Node1;

        /// <summary>
        /// the other affected node
        /// </summary>
        public Node Node2;

        /// <summary>
        /// Execute the local move transformation on the layout of <see cref="Node1"/> and <see cref="Node2"/>.
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// Creates a new local move, that can be applied on the layout of the node clones.
        /// </summary>
        /// <param name="cloneMap"> dictionary that maps id to node, assuming that the cloneMap represents a clone
        /// of the node layout of of <see cref="Node1"/></param>/ <see cref="Node2"/>.
        /// <returns>a new local move</returns>
        public abstract LocalMove Clone(IDictionary<string, Node> cloneMap);
    }

    /// <summary>
    /// A flip move is a kind of <see cref="LocalMove"/>.
    /// It rotates two nodes 90 degrees.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class FlipMove : LocalMove
    {
        /// <summary>
        /// if the rotation is clockwise
        /// </summary>
        private readonly bool _clockwise;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node1">one affected node</param>
        /// <param name="node2">other affected node</param>
        /// <param name="clockwise">if the rotation is clockwise </param>
        public FlipMove(Node node1, Node node2, bool clockwise)
        {
            this.Node1 = node1;
            this.Node2 = node2;
            this._clockwise = clockwise;
        }

        override
            public void Apply()
        {
            var segmentsNode1 = Node1.SegmentsDictionary();
            var segmentsNode2 = Node2.SegmentsDictionary();

            if (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left])
            {
                // [Node1][Node2]
                apply_flipOnVerticalSegment(leftNode: Node1, rightNode: Node2);
            }
            else if (segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                // [Node2][Node1]
                apply_flipOnVerticalSegment(leftNode: Node2, rightNode: Node1);
            }
            else if (segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower])
            {
                // [Node2]
                // [Node1]
                apply_flipOnHorizontalSegment(lowerNode: Node1, upperNode: Node2);
            }
            else if (segmentsNode1[Direction.Lower] == segmentsNode2[Direction.Upper])
            {
                // [Node1]
                // [Node2]
                apply_flipOnHorizontalSegment(lowerNode: Node2, upperNode: Node1);
            }
            else
            {
                throw new ArgumentException("Cant apply flip move");
            }
        }

        override
            public LocalMove Clone(IDictionary<string, Node> cloneMap)
        {
            return new FlipMove(cloneMap[Node1.ID], cloneMap[Node2.ID], _clockwise);
        }

        /// <summary>
        /// applies the local move for the case that the nodes are separated vertical
        /// </summary>
        /// <param name="leftNode">the node on the <see cref="Direction.Left"/> side</param>
        /// <param name="rightNode">the node on the <see cref="Direction.Right"/> side</param>
        private void apply_flipOnVerticalSegment(Node leftNode, Node rightNode)
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
            if (_clockwise)
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

            if (_clockwise)
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
        /// applies the local move for the case that the nodes are separated horizontal
        /// </summary>
        /// <param name="lowerNode">the node on the <see cref="Direction.Lower"/> side</param>
        /// <param name="upperNode">the node on the <see cref="Direction.Upper"/> side</param>
        private void apply_flipOnHorizontalSegment(Node lowerNode, Node upperNode)
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
            if (_clockwise)
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

            if (_clockwise)
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
        /// Method for better overview in debugger
        /// </summary>
        private string DebuggerDisplay => "flip " + Node1.ID + " " + Node2.ID + " {clockwise}";
    }

    /// <summary>
    /// A stretch move is a kind of <see cref="LocalMove"/>.
    /// It expands a nodes over another.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class StretchMove : LocalMove
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node1">one affected node</param>
        /// <param name="node2">the other affected node</param>
        public StretchMove(Node node1, Node node2)
        {
            Node1 = node1;
            Node2 = node2;
        }

        override
            public void Apply()
        {
            var segmentsNode1 = Node1.SegmentsDictionary();
            var segmentsNode2 = Node2.SegmentsDictionary();
            if (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left]
                || segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                var (leftNode, rightNode) = (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left])
                    ? (Node1, Node2)
                    : (Node2, Node1);
                if (leftNode.Rectangle.Depth >= rightNode.Rectangle.Depth)
                {
                    apply_StretchRightOverVertical(leftNode: leftNode, rightNode: rightNode);
                }
                else
                {
                    apply_StretchLeftOverVertical(leftNode: leftNode, rightNode: rightNode);
                }
            }
            else if (segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower]
                     || segmentsNode1[Direction.Lower] == segmentsNode2[Direction.Upper])
            {
                var (lowerNode, upperNode) = (segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower])
                    ? (Node1, Node2)
                    : (Node2, Node1);
                if (lowerNode.Rectangle.Width >= upperNode.Rectangle.Width)
                {
                    apply_StretchUpperOverHorizontal(lowerNode: lowerNode, upperNode: upperNode);
                }
                else
                {
                    apply_StretchLowerOverHorizontal(lowerNode: lowerNode, upperNode: upperNode);
                }
            }
            else
            {
                throw new ArgumentException("Cant apply stretch move");
            }
        }

        override
            public LocalMove Clone(IDictionary<string, Node> cloneMap)
        {
            return new StretchMove(cloneMap[Node1.ID], cloneMap[Node2.ID]);
        }

        /// <summary>
        /// Apply for the case that:
        /// - the nodes are seperated vertical
        /// - the left node is the expanding node
        /// </summary>
        /// <param name="leftNode">node one the <see cref="Direction.Left"/> side</param>
        /// <param name="rightNode">node one the <see cref="Direction.Right"/> side</param>
        private void apply_StretchLeftOverVertical(Node leftNode, Node rightNode)
        {
            // along lower           along upper
            //    [r]         [r]    [l][r]  ->  [llll]
            // [l][r]  ->  [llll]       [r]         [r]
            var segmentsLeftNode = leftNode.SegmentsDictionary();
            var segmentsRightNode = rightNode.SegmentsDictionary();
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
        /// - the nodes are seperated vertical
        /// - the right node is the expanding node
        /// </summary>
        /// <param name="leftNode">node one the <see cref="Direction.Left"/> side</param>
        /// <param name="rightNode">node one the <see cref="Direction.Right"/> side</param>
        private void apply_StretchRightOverVertical(Node leftNode, Node rightNode)
        {
            // along lower           along upper
            // [l]         [l]       [l][r]  ->  [r][r]
            // [l][r]  ->  [r][r]    [l]         [l]
            var segmentsLeftNode = leftNode.SegmentsDictionary();
            var segmentsRightNode = rightNode.SegmentsDictionary();
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
        /// - the nodes are seperated horizontal
        /// - the lower node is the expanding node
        /// </summary>
        /// <param name="lowerNode">node one the <see cref="Direction.Lower"/> side</param>
        /// <param name="upperNode">node one the <see cref="Direction.Upper"/> side</param>
        private void apply_StretchLowerOverHorizontal(Node lowerNode, Node upperNode)
        {
            // along left           along right
            // [uuuu] ->  [l][u]    [uuuu]  ->  [u][l]
            // [l]        [l]          [l]         [l]
            var segmentsLowerNode = lowerNode.SegmentsDictionary();
            var segmentsUpperNode = upperNode.SegmentsDictionary();
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
        /// - the nodes are seperated horizontal
        /// - the upper node is the expanding node
        /// </summary>
        /// <param name="lowerNode">node one the <see cref="Direction.Lower"/> side</param>
        /// <param name="upperNode">node one the <see cref="Direction.Upper"/> side</param>
        private void apply_StretchUpperOverHorizontal(Node lowerNode, Node upperNode)
        {
            // along left           along right
            // [u]    ->  [u]          [u]  ->      [u]
            // [llll]     [u][l]    [llll]       [l][u]
            var segmentsLowerNode = lowerNode.SegmentsDictionary();
            var segmentsUpperNode = upperNode.SegmentsDictionary();
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
        /// Method for better overview in debugger
        /// </summary>
        private string DebuggerDisplay => "stretch " + Node1.ID + " " + Node2.ID;
    }
}
