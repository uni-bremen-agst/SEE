using System.Collections.Generic;
using UnityEngine;
namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public class TNode
    {
        public TNode(ILayoutNode layoutNode, TNode parent)
        {
            this.representLayoutNode = layoutNode;
            this.parent = parent;
        }

        // do i rly need parent?
        private TNode parent;
        public TNode Parent {get => parent;}

        private IList<TNode> children;
        public IList<TNode> Children {get => children;}
        
        private TRectangle rectangle;
        public TRectangle Rectangle 
        {   get => rectangle; 
            set   {rectangle = value;}
        }

        private float size;
        public float Size 
        {   get => size;
            set {size = value;}}
        
        private ILayoutNode representLayoutNode;
        public ILayoutNode RepresentLayoutNode 
        {   get => representLayoutNode;}
        
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

        public IDictionary<Direction,TSegment> getAllSegments()
        {
            return new Dictionary<Direction,TSegment>{
                 {Direction.Left,  this.leftBoundingSegment},
                 {Direction.Right, this.rightBoundingSegment},
                 {Direction.Lower, this.lowerBoundingSegment},
                 {Direction.Upper, this.upperBoundingSegment}};
        }
    }
}