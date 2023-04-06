using System.Collections.Generic;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public class TNode
    {
        public void Rectangle(ILayoutNode layoutNode, float size)
        {
            this.size = size;
            this.representLayoutNode = layoutNode;
        }

        public TRectangle rectangle;

        private float size;

        public float Size {get;}
        private ILayoutNode representLayoutNode;
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
                this.leftBoundingSegment.Side2Nodes.Remove(this);
            }
            if( dir == Direction.Right)
            {
                this.rightBoundingSegment.Side1Nodes.Remove(this);
            }
            if( dir == Direction.Lower)
            {
                this.lowerBoundingSegment.Side2Nodes.Remove(this);
            }
            if( dir == Direction.Upper)
            {
                this.upperBoundingSegment.Side1Nodes.Remove(this);
            }
        }
    }
}