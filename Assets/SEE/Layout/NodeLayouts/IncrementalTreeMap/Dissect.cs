using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class Dissect{
        public static void Apply(IEnumerable<Node> nodes, Rectangle rectangle)
        {
            Node[] nodesArray = nodes.ToArray();
            Array.Sort(nodesArray,(x,y) => (x.Size.CompareTo(y.Size)));

            if(Math.Abs(nodesArray.Sum(x => x.Size) - rectangle.Area()) >= rectangle.Area() * Math.Pow(10, -3)
                && nodesArray.Length > 1)
            {
                Debug.LogWarning("Dissect: nodes doesnt fit in rectangle");
            }

            Apply(rectangle,
                    nodesArray,
                    leftBound : new Segment(true, true),
                    rightBound: new Segment(true, true), 
                    upperBound: new Segment(true, false),
                    lowerBound: new Segment(true, false));
        }

        private static void Apply( Rectangle rectangle, 
                                Node[] nodes,
                                Segment leftBound,
                                Segment rightBound,
                                Segment upperBound, 
                                Segment lowerBound)
        {
            if(nodes.Length == 1)
            {
                Node node = nodes[0];
                node.Rectangle = rectangle;
                node.RegisterSegment(leftBound, Direction.Left);
                node.RegisterSegment(rightBound,Direction.Right);
                node.RegisterSegment(lowerBound,Direction.Lower);
                node.RegisterSegment(upperBound,Direction.Upper);
            }
            else
            {
                int splitIndex = GetSplitIndex(nodes);
                Node[] nodes1 = nodes[..splitIndex];
                Node[] nodes2 = nodes[splitIndex..];
                
                float ratio = nodes1.Sum(x => x.Size) / nodes.Sum(x => x.Size);

                Rectangle rectangle1 = rectangle.Clone();
                Rectangle rectangle2 = rectangle.Clone();
                if(rectangle.Width >= rectangle.Depth)
                {
                    rectangle1.Width *= ratio;
                    rectangle2.Width *= (1 - ratio);
                    rectangle2.X = rectangle1.X + rectangle1.Width;
                    Segment newSegment = new Segment(false,true);

                    Dissect.Apply(rectangle1, nodes1,
                                    leftBound : leftBound,
                                    rightBound: newSegment,
                                    upperBound: upperBound,
                                    lowerBound: lowerBound);

                    Dissect.Apply(rectangle2, nodes2,
                                    leftBound :  newSegment,
                                    rightBound: rightBound,
                                    upperBound: upperBound,
                                    lowerBound: lowerBound);
                }
                else
                {
                    rectangle1.Depth *= ratio;
                    rectangle2.Depth *= (1 - ratio);
                    rectangle2.Z = rectangle1.Z + rectangle1.Depth;
                    Segment newSegment = new Segment(false,false);

                    Dissect.Apply(rectangle1, nodes1,
                                    leftBound : leftBound,
                                    rightBound: rightBound,
                                    upperBound: newSegment,
                                    lowerBound: lowerBound);

                    Dissect.Apply(rectangle2, nodes2,
                                    leftBound :  leftBound,
                                    rightBound: rightBound,
                                    upperBound: upperBound,
                                    lowerBound: newSegment);
                }
            }
        }

        private static int GetSplitIndex(Node[] nodes)
        {
            if(nodes.Sum( x => x.Size)  <=  nodes.Last().Size * 3)
            {
                return nodes.Length -1;
            }
            else
            {
                for (int i = 1; i <= nodes.Length+1; i++)
                {
                    if( nodes[..i].Sum(x => x.Size) * 3 >= nodes.Sum(x => x.Size))
                    {
                        return i;
                    }
                }
            }
            throw new ArgumentException("We should never arrive here.\n");
        }
    }
}