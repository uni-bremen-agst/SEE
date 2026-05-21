using System;
using System.Collections.Generic;
using System.Diagnostics;
using static SEE.Layout.NodeLayouts.IncrementalTreeMap.Direction;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A core element in the Layout, each entity that should be laid out
    /// has a corresponding node.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class Node
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">The ID of the new node.</param>
        public Node(string id)
        {
            ID = id;
        }

        /// <summary>
        /// The <see cref="Rectangle"/> represent the position and size of the node in the layout.
        /// </summary>
        public Rectangle Rectangle { get; set; }

        /// <summary>
        /// The size that the node SHOULD occupy, may differ from the actual size/area of its rectangle.
        /// </summary>
        public float DesiredSize { get; set; }

        /// <summary>
        /// The ID.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// The adjacent segment on the <see cref="Direction.Left"/> side of the node.
        /// </summary>
        private Segment leftBoundingSegment;

        /// <summary>
        /// The adjacent segment on the <see cref="Direction.Right"/> side of the node.
        /// </summary>
        private Segment rightBoundingSegment;

        /// <summary>
        /// The adjacent segment on the <see cref="Direction.Upper"/> side of the node.
        /// </summary>
        private Segment upperBoundingSegment;

        /// <summary>
        /// The adjacent segment on the <see cref="Direction.Lower"/> side of the node.
        /// </summary>
        private Segment lowerBoundingSegment;

        /// <summary>
        /// Registers the node with a new segment,
        /// so the node and the segment get the information about the new adjacent.
        /// </summary>
        /// <param name="segment">The new adjacent segment.</param>
        /// <param name="dir">The side of the new adjacent segment.</param>
        public void RegisterSegment(Segment segment, Direction dir)
        {
            DeregisterSegment(dir);
            switch (dir)
            {
                case Left:
                    leftBoundingSegment = segment;
                    segment.Side2Nodes.Add(this);
                    break;
                case Right:
                    rightBoundingSegment = segment;
                    segment.Side1Nodes.Add(this);
                    break;
                case Lower:
                    lowerBoundingSegment = segment;
                    segment.Side2Nodes.Add(this);
                    break;
                case Upper:
                    upperBoundingSegment = segment;
                    segment.Side1Nodes.Add(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, "We should never arrive here.");
            }
        }

        /// <summary>
        /// Removes the node from a current adjacent segment.
        /// </summary>
        /// <param name="dir">The side of the adjacent segment.</param>
        public void DeregisterSegment(Direction dir)
        {
            switch (dir)
            {
                case Left:
                    leftBoundingSegment?.Side2Nodes.Remove(this);
                    break;
                case Right:
                    rightBoundingSegment?.Side1Nodes.Remove(this);
                    break;
                case Lower:
                    lowerBoundingSegment?.Side2Nodes.Remove(this);
                    break;
                case Upper:
                    upperBoundingSegment?.Side1Nodes.Remove(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, "We should never arrive here");
            }
        }

        /// <summary>
        /// Returns all adjacent segments.
        /// </summary>
        /// <returns>Dictionary where the direction maps the segment in this direction.</returns>
        public IDictionary<Direction, Segment> SegmentsDictionary()
        {
            return new Dictionary<Direction, Segment>
            {
                { Left, leftBoundingSegment },
                { Right, rightBoundingSegment },
                { Lower, lowerBoundingSegment },
                { Upper, upperBoundingSegment }
            };
        }

        /// <summary>
        /// Method for better overview in debugger.
        /// </summary>
        private string DebuggerDisplay =>
            string.Format(
                "{0,15} x=[{1,-6}, {2,-6}] y=[{3,-6}, {4,-6}]",
                ID,
                Math.Round(Rectangle.X, 3),
                Math.Round(Rectangle.X + Rectangle.Width, 3),
                Math.Round(Rectangle.Z, 3),
                Math.Round(Rectangle.Z + Rectangle.Depth, 3));
    }
}
