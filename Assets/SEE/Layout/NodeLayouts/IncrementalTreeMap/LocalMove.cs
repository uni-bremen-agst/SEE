using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public abstract class LocalMove
    {
        protected Node node1;
        public Node Node1 => this.node1;
        protected Node node2;

        public Node Node2 => this.node2;
        public abstract void Apply();

        public abstract LocalMove Clone(IDictionary<string,Node> mapOriginalClone);
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class FlipMove : LocalMove
    {
        private bool clockwise;
        public FlipMove(Node node1, Node node2, bool clockwise)
        {
            this.node1 = node1; this.node2 = node2; this.clockwise = clockwise;
        }
        
        override 
        public void Apply()
        {
            var segmentsNode1 = node1.SegmentsDictionary();
            var segmentsNode2 = node2.SegmentsDictionary();

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

        override
        public LocalMove Clone(IDictionary<string,Node> mapOriginalClone)
        {
            return new FlipMove(mapOriginalClone[node1.ID],mapOriginalClone[node2.ID],clockwise);
        }

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
            rightNode.Rectangle.Depth *= (1-ratio);
            if(clockwise)
            {
                leftNode.Rectangle.Z = rightNode.Rectangle.Z + rightNode.Rectangle.Depth;
            }
            else
            {
                rightNode.Rectangle.Z = leftNode.Rectangle.Z + leftNode.Rectangle.Depth;
            }

            // switch segments
            Segment newRightSegmentForLeftNode = rightNode.SegmentsDictionary()[Direction.Right];
            Segment newLeftSegmentForRightNode  = leftNode.SegmentsDictionary()[Direction.Left];
            Segment middle = leftNode.SegmentsDictionary()[Direction.Right];
            middle.IsVertical = false;

            leftNode.RegisterSegment(newRightSegmentForLeftNode, Direction.Right);
            rightNode.RegisterSegment(newLeftSegmentForRightNode, Direction.Left);

            if(clockwise)
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
            upperNode.Rectangle.Width *= (1-ratio);
            if(clockwise)
            {
                upperNode.Rectangle.X = lowerNode.Rectangle.X  + lowerNode.Rectangle.Width;
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

            if(clockwise)
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
        
        private string DebuggerDisplay => "flip "+node1.ID+" "+node2.ID+" {clockwise}";
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class StretchMove : LocalMove
    {

        public StretchMove(Node node1, Node node2)
        {
            this.node1 = node1; this.node2 = node2;
        }

        override 
        public void Apply()
        {
            var segmentsNode1 = node1.SegmentsDictionary();
            var segmentsNode2 = node2.SegmentsDictionary();
            if(    segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left]
                || segmentsNode1[Direction.Left] == segmentsNode2[Direction.Right])
            {
                var (leftNode, rightNode) = (segmentsNode1[Direction.Right] == segmentsNode2[Direction.Left]) ? (node1,node2) : (node2, node1);
                if(leftNode.Rectangle.Depth >= rightNode.Rectangle.Depth)
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
                if(lowerNode.Rectangle.Width >= upperNode.Rectangle.Width)
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
        public LocalMove Clone(IDictionary<string,Node> mapOriginalClone)
        {
            return new StretchMove(mapOriginalClone[node1.ID],mapOriginalClone[node2.ID]);
        }

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
            if(alongLowerSegment)
            {
                rightNode.Rectangle.Z = leftNode.Rectangle.Z + leftNode.Rectangle.Depth;
            }
            // switch segments
            leftNode.RegisterSegment(segmentsRightNode[Direction.Right], Direction.Right);
            if(alongLowerSegment)
            {
                rightNode.RegisterSegment(segmentsLeftNode[Direction.Upper], Direction.Lower);
            }
            else
            {
                rightNode.RegisterSegment(segmentsLeftNode[Direction.Lower],Direction.Upper);
            }
        }
        
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
            if(alongLowerSegment)
            {
                leftNode.Rectangle.Z = rightNode.Rectangle.Z + rightNode.Rectangle.Depth;
            } 
            // switch segments
            rightNode.RegisterSegment(segmentsLeftNode[Direction.Left], Direction.Left);
            if(alongLowerSegment)
            {
                leftNode.RegisterSegment(segmentsRightNode[Direction.Upper], Direction.Lower);
            }
            else
            {
                leftNode.RegisterSegment(segmentsRightNode[Direction.Lower],Direction.Upper);
            }
        }

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
            if(alongLeftSegment)
            {
                upperNode.Rectangle.X = lowerNode.Rectangle.X + lowerNode.Rectangle.Width;
            }
            // switch segments
            lowerNode.RegisterSegment(segmentsUpperNode[Direction.Upper], Direction.Upper);
            if(alongLeftSegment)
            {
                upperNode.RegisterSegment(segmentsLowerNode[Direction.Right], Direction.Left);
            }
            else
            {
                upperNode.RegisterSegment(segmentsLowerNode[Direction.Left],Direction.Right);
            }
        }
        
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
            if(alongLeftSegment)
            {
                lowerNode.Rectangle.X = upperNode.Rectangle.X + upperNode.Rectangle.Width;
            }
            // switch segments
            upperNode.RegisterSegment(segmentsLowerNode[Direction.Lower], Direction.Lower);
            if(alongLeftSegment)
            {
                lowerNode.RegisterSegment(segmentsUpperNode[Direction.Right], Direction.Left);
            }
            else
            {
                lowerNode.RegisterSegment(segmentsUpperNode[Direction.Left],Direction.Right);
            }
        }
        
        private string DebuggerDisplay => "flip "+node1.ID+" "+node2.ID;
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