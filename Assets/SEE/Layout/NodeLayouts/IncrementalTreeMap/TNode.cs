using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TNode
    {
        public TNode(string nodeID)
        {
            this.nodeID = nodeID;
        }
        
        private TRectangle rectangle;
        public TRectangle Rectangle 
        {   get => rectangle; 
            set   {rectangle = value;}
        }

        private float size;
        public float Size 
        {   get => size;
            set {size = value;}}

        private string nodeID;
        public string ID 
        {   get => nodeID;}

        private TSegment leftBoundingSegment;
        private TSegment rightBoundingSegment;
        private TSegment upperBoundingSegment;
        private TSegment lowerBoundingSegment;

        public void registerSegment(TSegment segment, Direction dir)
        {
            this.deregisterSegment(dir);    
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

        public void deregisterSegment(Direction dir)
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

        public IDictionary<Direction,TSegment> SegmentsDictionary()
        {
            return new Dictionary<Direction,TSegment>{
                 {Direction.Left,  this.leftBoundingSegment},
                 {Direction.Right, this.rightBoundingSegment},
                 {Direction.Lower, this.lowerBoundingSegment},
                 {Direction.Upper, this.upperBoundingSegment}};
        }

        private string DebuggerDisplay
        {
            get {
                return string.Format(
                    "{0,15} x=[{1,-6}, {2,-6}] y=[{3,-6}, {4,-6}]",
                    ID,
                    Math.Round(rectangle.x,3),
                    Math.Round(rectangle.x + rectangle.width, 3),
                    Math.Round(rectangle.z, 3),
                    Math.Round(rectangle.z + rectangle.depth, 3));
            }
        }


    }
}