using System.Collections.Generic;
using System.Linq;
using System;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public enum MoveKind
    {Flip, Stretch} 

    abstract public class LocalMove
    {
        protected TNode node1;
        protected TNode node2;

        abstract public void apply();
        // 
    }

    public class FlipMove : LocalMove
    {
        bool clockwise;
        public FlipMove(TNode node1, TNode node2, bool clockwise)
        {
            this.node1 = node1; this.node2 = node2; this.clockwise = clockwise;
        }
        
        override 
        public void apply()
        {
            var segmentsNode1 = node1.getAllSegments();
            var segmentsNode2 = node2.getAllSegments();

            if(segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left])
            {
                // [Node1][Node2]
                if(clockwise)
                {
                    clockwise_onVerticalSegment(leftNode: node1, rightNode: node2);
                }
                else
                {
                    anticlockwise_onVerticalSegment(leftNode: node1, rightNode: node2);
                }
            }
            else if(segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                // [Node2][Node1]
                if(clockwise)
                {
                    clockwise_onVerticalSegment(leftNode: node2, rightNode: node1);
                }
                else
                {
                    anticlockwise_onVerticalSegment(leftNode: node2, rightNode: node1);
                }
            }
            else if(segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower])
            {
                // [Node2]
                // [Node1]
                if(clockwise)
                {
                    clockwise_onHorizontalSegment(lowerNode: node1, upperNode: node2);
                }
                else
                {
                    anticlockwise_onHorizontalSegment(lowerNode: node1, upperNode: node2);
                }
            }
            else if(segmentsNode1[Direction.Lower] == segmentsNode2[Direction.Upper])
            {
                // [Node1]
                // [Node2]
                if(clockwise)
                {
                    clockwise_onHorizontalSegment(lowerNode: node2, upperNode: node1);
                }
                else
                {
                    anticlockwise_onHorizontalSegment(lowerNode: node2, upperNode: node1);
                }
            }
            else
            {
                    throw new ArgumentException("Cant apply flip move");
            }

        }


        // [l][r] -> [lll]
        // [l][r]    [rrr]
        private void clockwise_onVerticalSegment(TNode leftNode, TNode rightNode)
        {
            // adjust rectangles
            float width = leftNode.Rectangle.width + rightNode.Rectangle.width;
            float ratio = leftNode.Rectangle.area() / (leftNode.Rectangle.area() + rightNode.Rectangle.area());
            
            leftNode.Rectangle.width = width;
            rightNode.Rectangle.x = leftNode.Rectangle.x;
            rightNode.Rectangle.width = width;

            rightNode.Rectangle.depth *= (1-ratio);
            leftNode.Rectangle.z = rightNode.Rectangle.z + rightNode.Rectangle.depth;
            leftNode.Rectangle.depth *= ratio;

            // switch segments
            TSegment newRightSegmentForLeftNode = rightNode.getAllSegments()[Direction.Right];
            TSegment newLeftSegmentForRightNode  = leftNode.getAllSegments()[Direction.Left];
            TSegment middle = leftNode.getAllSegments()[Direction.Right];

            leftNode.registerSegment(newRightSegmentForLeftNode, Direction.Right);
            leftNode.registerSegment(middle, Direction.Lower);
            rightNode.registerSegment(newLeftSegmentForRightNode, Direction.Left);
            rightNode.registerSegment(middle, Direction.Upper);

            // TODO middle.IsVertical = false
        }


        // [l][r] -> [rrr]
        // [l][r]    [lll]
        private void anticlockwise_onVerticalSegment(TNode leftNode, TNode rightNode)
        {

        }

        // [uuu]  ->  [l][u]
        // [lll]      [l][u]
        private void clockwise_onHorizontalSegment(TNode lowerNode, TNode upperNode)
        {

        }

        // [uuu]  ->  [u][l]
        // [lll]      [u][l]
        private void anticlockwise_onHorizontalSegment(TNode lowerNode, TNode upperNode)
        {

        }
    }

    public class StretchMove : LocalMove
    {

        public StretchMove(TNode node1, TNode node2)
        {
            this.node1 = node1; this.node2 = node2;
        }

        override 
        public void apply()
        {

        }
    }
}