using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.Assertions;

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
                apply_flipOnVerticalSegment(leftNode: node1, rightNode: node2);
            }
            else if(segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                // [Node2][Node1]
                apply_flipOnVerticalSegment(leftNode: node2, rightNode: node1);
            }
            else if(segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower])
            {
                // [Node2]
                // [Node1]
                apply_flipOnHorizontalSegment(lowerNode: node1, upperNode: node2);
            }
            else if(segmentsNode1[Direction.Lower] == segmentsNode2[Direction.Upper])
            {
                // [Node1]
                // [Node2]
                apply_flipOnHorizontalSegment(lowerNode: node2, upperNode: node1);
            }
            else
            {
                    throw new ArgumentException("Cant apply flip move");
            }
        }

        private void apply_flipOnVerticalSegment(TNode leftNode, TNode rightNode)
        {
            // clockwise            anticlockwise
            // [l][r] -> [lll]      [l][r] -> [rrr]
            // [l][r]    [rrr]      [l][r]    [lll]

            // adjust rectangles
            float width = leftNode.Rectangle.width + rightNode.Rectangle.width;
            float ratio = leftNode.Rectangle.area() / (leftNode.Rectangle.area() + rightNode.Rectangle.area());
            
            leftNode.Rectangle.width = width;
            rightNode.Rectangle.width = width;
            rightNode.Rectangle.x = leftNode.Rectangle.x;            
            
            leftNode.Rectangle.depth *= ratio;
            rightNode.Rectangle.depth *= (1-ratio);
            if(clockwise)
            {
                leftNode.Rectangle.z = rightNode.Rectangle.z + rightNode.Rectangle.depth;
            }
            else
            {
                rightNode.Rectangle.z = leftNode.Rectangle.z + leftNode.Rectangle.depth;
            }

            // switch segments
            TSegment newRightSegmentForLeftNode = rightNode.getAllSegments()[Direction.Right];
            TSegment newLeftSegmentForRightNode  = leftNode.getAllSegments()[Direction.Left];
            TSegment middle = leftNode.getAllSegments()[Direction.Right];
            middle.IsVertical = false;

            leftNode.registerSegment(newRightSegmentForLeftNode, Direction.Right);
            rightNode.registerSegment(newLeftSegmentForRightNode, Direction.Left);

            if(clockwise)
            {
                leftNode.registerSegment(middle, Direction.Lower);
                rightNode.registerSegment(middle, Direction.Upper);
            }
            else
            {
                leftNode.registerSegment(middle, Direction.Upper);
                rightNode.registerSegment(middle, Direction.Lower);
            }   
        }

        private void apply_flipOnHorizontalSegment(TNode lowerNode, TNode upperNode)
        {
            // clockwise                anticlockwise
            // [uuu]  ->  [l][u]        [uuu]  ->  [u][l]
            // [lll]      [l][u]        [lll]      [u][l]

            // adjust rectangles
            float depth = lowerNode.Rectangle.depth + upperNode.Rectangle.depth;
            float ratio = lowerNode.Rectangle.area() / (lowerNode.Rectangle.area() + upperNode.Rectangle.area());
            
            lowerNode.Rectangle.depth = depth;
            upperNode.Rectangle.depth = depth;
            upperNode.Rectangle.z = lowerNode.Rectangle.z;         
            
            lowerNode.Rectangle.width *= ratio;
            lowerNode.Rectangle.width *= (1-ratio);
            if(clockwise)
            {
                upperNode.Rectangle.x = lowerNode.Rectangle.x  + lowerNode.Rectangle.width;
            }
            else
            {
                lowerNode.Rectangle.x = upperNode.Rectangle.x + upperNode.Rectangle.width;
            }

            // switch segments
            TSegment newUpperSegmentForLowerNode = upperNode.getAllSegments()[Direction.Upper];
            TSegment newLowerSegmentForUpperNode = lowerNode.getAllSegments()[Direction.Lower];
            TSegment middle = lowerNode.getAllSegments()[Direction.Upper];
            middle.IsVertical = true;

            lowerNode.registerSegment(newUpperSegmentForLowerNode, Direction.Upper);
            upperNode.registerSegment(newLowerSegmentForUpperNode, Direction.Lower);

            if(clockwise)
            {
                lowerNode.registerSegment(middle, Direction.Right);
                upperNode.registerSegment(middle, Direction.Left);
            }
            else
            {
                lowerNode.registerSegment(middle, Direction.Left);
                upperNode.registerSegment(middle, Direction.Right);
            }
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
            var segmentsNode1 = node1.getAllSegments();
            var segmentsNode2 = node2.getAllSegments();
            if(    segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left]
                || segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                var (leftNode, rightNode) = (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left]) ? (node1,node2) : (node2, node1);
                if(leftNode.Rectangle.width >= rightNode.Rectangle.width)
                {
                    apply_StretchRightOverVertical(leftNode: leftNode, rightNode: rightNode);
                }
                else
                {
                    apply_StretchLeftOverVertical(leftNode: leftNode, rightNode: rightNode);
                }
            }
            else if (   segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower]
                     || segmentsNode1[Direction.Lower] == segmentsNode2[Direction.Upper])
            {
                var (lowerNode, upperNode) = (segmentsNode1[Direction.Upper] == segmentsNode2[Direction.Lower]) ? (node1,node2) : (node2, node1);
                if(lowerNode.Rectangle.depth >= upperNode.Rectangle.depth)
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
                throw new ArgumentException("Cant apply flip move");
            }
        }

        private void apply_StretchLeftOverVertical(TNode leftNode, TNode rightNode){}
        private void apply_StretchRightOverVertical(TNode leftNode, TNode rightNode){}

        private void apply_StretchLowerOverHorizontal(TNode lowerNode, TNode upperNode){}
        private void apply_StretchUpperOverHorizontal(TNode lowerNode, TNode upperNode){}

        
    }
}

/*
 over vertical:
    along lower:
        stretch left:
               [r]         [r]
            [l][r]  ->  [llll]
        stretch right:
            [l]         [l]
            [l][r]  ->  [rrrr]
    along upper:
        stretch left:
            [l][r]  ->  [llll]
               [r]         [r]
        stretch right:
            [l][r]  ->  [rrrr] 
            [l]         [l]
 over horizontal:
    along left:
        stretch lower:
            [uuuu]  ->  [l][u]
            [l]         [l]
        stretch upper:
            [u]     ->  [u] 
            [llll]      [u][l]
    along right:
        stretch lower:
            [uuuu]  ->  [u][l]
               [l]         [l]
        stretch upper:
               [u]  ->     [u]
            [llll]      [l][u]
*/