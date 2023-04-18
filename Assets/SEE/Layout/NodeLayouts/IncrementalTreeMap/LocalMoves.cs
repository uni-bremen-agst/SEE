using System.Collections.Generic;
using System.Linq;
using System;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    internal enum MoveKind
    {Flip, Stretch} 

    abstract class LocalMove
    {
        protected TNode node1;
        protected TNode node2;

        abstract public void apply();
        // 
        private static void findLocalMoves()
        {}

        // -------------------------------------------------------------------
        // public methods all static 

        public static void CorretNodes(IList<TNode> nodes)
        {}
        public static void AddNode(IList<TNode> nodes,TNode newNode)
        {
            // ArgMax is shit in c#
            // node with rectangle with highest aspect ratio
            TNode bestNode = ((List<TNode>) nodes).Find(x => x.Rectangle.aspect_ratio() == ((List<TNode>) nodes).Max( y => y.Rectangle.aspect_ratio()));
            newNode.Rectangle = new TRectangle(x: bestNode.Rectangle.x, z: bestNode.Rectangle.z,
                                               width: bestNode.Rectangle.width, depth: bestNode.Rectangle.depth);
            IDictionary<Direction,TSegment> segments = bestNode.getAllSegments();
            foreach(Direction dir in Enum.GetValues(typeof(Direction)))
            {
                newNode.registerSegment(segments[dir],dir);
            }
            if(bestNode.Rectangle.width >= bestNode.Rectangle.depth)
            {
                // [bestNode]|[newNode]
                TSegment newSegment = new TSegment(isConst: false, isVertical: false);
                newNode.registerSegment(newSegment, Direction.Left);
                bestNode.registerSegment(newSegment, Direction.Right);
                bestNode.Rectangle.width *= 0.5f;
                newNode.Rectangle.width *= 0.5f;
                newNode.Rectangle.x = bestNode.Rectangle.x + bestNode.Rectangle.width;
            }
            else
            {
                // [newNode]
                // ---------
                // [bestNode]
                TSegment newSegment = new TSegment(isConst: false, isVertical: true);
                newNode.registerSegment(newSegment, Direction.Lower);
                bestNode.registerSegment(newSegment, Direction.Upper);
                bestNode.Rectangle.depth *= 0.5f;
                newNode.Rectangle.depth *= 0.5f;
                newNode.Rectangle.z = bestNode.Rectangle.z + bestNode.Rectangle.depth;
            }
        }

        public static void DeleteNode(TNode obsoleteNode)
        {}
        public static void MakeLocalMoves(IList<TNode> nodes)
        {}
    }

    // TODO own file own class etc
    internal class FlipMove : LocalMove
    {
        bool clockwise;
        internal FlipMove(TNode node1, TNode node2, bool clockwise)
        {
            this.node1 = node1; this.node2 = node2; this.clockwise = clockwise;
        }
        
        override 
        public void apply()
        {

        }

    }

    internal class StretchMove : LocalMove
    {

        internal StretchMove(TNode node1, TNode node2)
        {
            this.node1 = node1; this.node2 = node2;
        }

        override 
        public void apply()
        {

        }
    }
}