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
    public class Node
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">the id</param>
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
        public float Size { get; set; }

        /// <summary>
        /// the ID
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// the adjacent segment on the <see cref="Direction.Left"/> side of the node
        /// </summary>
        private Segment _leftBoundingSegment;
        
        /// <summary>
        ///the adjacent segment on the <see cref="Direction.Right"/> side of the node
        /// </summary>
        private Segment _rightBoundingSegment;
        
        /// <summary>
        ///the adjacent segment on the <see cref="Direction.Upper"/> side of the node
        /// </summary>
        private Segment _upperBoundingSegment;
        
        /// <summary>
        /// the adjacent segment on the <see cref="Direction.Lower"/> side of the node
        /// </summary>
        private Segment _lowerBoundingSegment;

        /// <summary>
        /// Register the node to a new segment, deregister from the old one on this side.
        /// The node knows about new segment and the segment knows about new node.
        /// </summary>
        /// <param name="segment">the new adjacent segment</param>
        /// <param name="dir">the side of the new adjacent segment</param>
        public void RegisterSegment(Segment segment, Direction dir)
        {
            DeregisterSegment(dir);    
            switch (dir)
            {
                case Left:
                    _leftBoundingSegment = segment;
                    segment.Side2Nodes.Add(this);
                    break;
                case Right:
                    _rightBoundingSegment = segment;
                    segment.Side1Nodes.Add(this);
                    break;
                case Lower:
                    _lowerBoundingSegment= segment;
                    segment.Side2Nodes.Add(this);
                    break;
                case Upper:
                    _upperBoundingSegment = segment;
                    segment.Side1Nodes.Add(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, "We should never arrive here");
            }
        }

        /// <summary>
        /// Remove node from a current adjacent segment
        /// </summary>
        /// <param name="dir">The side of the adjacent segment</param>
        public void DeregisterSegment(Direction dir)
        {
            switch (dir)
            {
                case Left:
                    _leftBoundingSegment?.Side2Nodes.Remove(this);
                    break;
                case Right:
                    _rightBoundingSegment?.Side1Nodes.Remove(this);
                    break;
                case Lower:
                    _lowerBoundingSegment?.Side2Nodes.Remove(this);
                    break;
                case Upper:
                    _upperBoundingSegment?.Side1Nodes.Remove(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, "We should never arrive here");
            }
        }

        /// <summary>
        /// Get all adjacent segments
        /// </summary>
        /// <returns>dictionary direction -> segment in this direction</returns>
        public IDictionary<Direction,Segment> SegmentsDictionary()
        {
            return new Dictionary<Direction,Segment>{
                 {Left,  _leftBoundingSegment},
                 {Right, _rightBoundingSegment},
                 {Lower, _lowerBoundingSegment},
                 {Upper, _upperBoundingSegment}};
        }
        
        /// <summary>
        /// Method for easy overview in debugger
        /// </summary>
        private string DebuggerDisplay =>
            string.Format(
                "{0,15} x=[{1,-6}, {2,-6}] y=[{3,-6}, {4,-6}]",
                ID,
                Math.Round(Rectangle.X,3),
                Math.Round(Rectangle.X + Rectangle.Width, 3),
                Math.Round(Rectangle.Z, 3),
                Math.Round(Rectangle.Z + Rectangle.Depth, 3));
    }
}