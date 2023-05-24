using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class Dissect{
        static public void dissect(TRectangle rectangle, IEnumerable<TNode> nodes)
        {
            TNode[] nodesArray = nodes.ToArray();
            Array.Sort(nodesArray,(x,y) => (x.Size.CompareTo(y.Size)));
            dissect(rectangle,
                    nodesArray,
                    leftBound : new TSegment(true, true),
                    rightBound: new TSegment(true, true), 
                    upperBound: new TSegment(true, false),
                    lowerBound: new TSegment(true, false));
        }

        static private void dissect( TRectangle rectangle, 
                                TNode[] nodes,
                                TSegment leftBound,
                                TSegment rightBound,
                                TSegment upperBound, 
                                TSegment lowerBound)
        {
            if(nodes.Length == 1)
            {
                TNode node = nodes[0];
                node.Rectangle = rectangle;
                node.registerSegment(leftBound, Direction.Left);
                node.registerSegment(rightBound,Direction.Right);
                node.registerSegment(lowerBound,Direction.Lower);
                node.registerSegment(upperBound,Direction.Upper);
                return;
            }
            else
            {
                int splitIndex = getSplitIndex(nodes);
                TNode[] nodes_1 = nodes[..splitIndex];
                TNode[] nodes_2 = nodes[splitIndex..];
                
                float ratio = nodes_1.Sum(x => x.Size) / nodes.Sum(x => x.Size);

                TRectangle rectangle_1 = new TRectangle(x : rectangle.x, z : rectangle.z,
                    width : rectangle.width, depth : rectangle.depth);
                TRectangle rectangle_2 = new TRectangle(x : rectangle.x, z : rectangle.z,
                    width : rectangle.width, depth : rectangle.depth);
                if(rectangle.width >= rectangle.depth)
                {
                    rectangle_1.width *= ratio;
                    rectangle_2.width *= (1 - ratio);
                    rectangle_2.x = rectangle_1.x + rectangle_1.width;
                    TSegment newSegment = new TSegment(false,false);

                    Dissect.dissect(rectangle_1, nodes_1,
                                    leftBound : leftBound,
                                    rightBound: newSegment,
                                    upperBound: upperBound,
                                    lowerBound: lowerBound);

                    Dissect.dissect(rectangle_2, nodes_2,
                                    leftBound :  newSegment,
                                    rightBound: rightBound,
                                    upperBound: upperBound,
                                    lowerBound: lowerBound);
                }
                else
                {
                    rectangle_1.depth *= ratio;
                    rectangle_2.depth *= (1 - ratio);
                    rectangle_2.z = rectangle_1.z + rectangle_1.depth;
                    TSegment newSegment = new TSegment(false,true);

                    Dissect.dissect(rectangle_1, nodes_1,
                                    leftBound : leftBound,
                                    rightBound: rightBound,
                                    upperBound: newSegment,
                                    lowerBound: lowerBound);

                    Dissect.dissect(rectangle_2, nodes_2,
                                    leftBound :  leftBound,
                                    rightBound: rightBound,
                                    upperBound: upperBound,
                                    lowerBound: newSegment);
                }
            }
        }

        internal static int getSplitIndex(TNode[] nodes)
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