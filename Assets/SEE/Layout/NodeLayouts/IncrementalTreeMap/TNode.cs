using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Node
    {
        public Node(string nodeID)
        {
            this.ID = nodeID;
        }
        
        private Rectangle rectangle;
        public Rectangle Rectangle 
        {   get => rectangle; 
            set => rectangle = value;
        }

        public float Size { get; set; }

        public string ID { get; }

        private Segment leftBoundingSegment;
        private Segment rightBoundingSegment;
        private Segment upperBoundingSegment;
        private Segment lowerBoundingSegment;

        public void RegisterSegment(Segment segment, Direction dir)
        {
            this.DeregisterSegment(dir);    
            if( dir == Direction.Left)
            {
                this.leftBoundingSegment = segment;
                segment.Side2Nodes.Add(this);
            }
            if( dir == Direction.Right)
            {
                this.rightBoundingSegment = segment;
                segment.Side1Nodes.Add(this);
            }
            if( dir == Direction.Lower)
            {
                this.lowerBoundingSegment= segment;
                segment.Side2Nodes.Add(this);
            }
            if( dir == Direction.Upper)
            {
                this.upperBoundingSegment = segment;
                segment.Side1Nodes.Add(this);
            }
        }

        public void DeregisterSegment(Direction dir)
        {
            if( dir == Direction.Left)
            {
                this.leftBoundingSegment?.Side2Nodes.Remove(this);
            } 
            if( dir == Direction.Right)
            {
                this.rightBoundingSegment?.Side1Nodes.Remove(this);
            }
            if( dir == Direction.Lower)
            {
                this.lowerBoundingSegment?.Side2Nodes.Remove(this);
            }
            if( dir == Direction.Upper)
            {
                this.upperBoundingSegment?.Side1Nodes.Remove(this);
            }
        }

        public IDictionary<Direction,Segment> SegmentsDictionary()
        {
            return new Dictionary<Direction,Segment>{
                 {Direction.Left,  this.leftBoundingSegment},
                 {Direction.Right, this.rightBoundingSegment},
                 {Direction.Lower, this.lowerBoundingSegment},
                 {Direction.Upper, this.upperBoundingSegment}};
        }

        private string DebuggerDisplay =>
            string.Format(
                "{0,15} x=[{1,-6}, {2,-6}] y=[{3,-6}, {4,-6}]",
                ID,
                Math.Round(rectangle.x,3),
                Math.Round(rectangle.x + rectangle.width, 3),
                Math.Round(rectangle.z, 3),
                Math.Round(rectangle.z + rectangle.depth, 3));
    }
}