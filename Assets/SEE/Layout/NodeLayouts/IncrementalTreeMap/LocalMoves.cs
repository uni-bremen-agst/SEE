using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class LocalMoves
    {
        private static T ArgMaxJ<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Max(eval);
            return collection.First(x => eval(x) == bestVal);
        }
        
        private static T ArgMinJ<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Min(eval);
            return collection.First(x => eval(x) == bestVal);
        }

        private static IList<LocalMove> findLocalMoves(TSegment segment)
        {
            List<LocalMove> result = new List<LocalMove>();
            if(segment.Side1Nodes.Count == 1 && segment.Side2Nodes.Count == 1)
            {
                result.Add(new FlipMove(segment.Side1Nodes.First(),segment.Side2Nodes.First(), true));
                result.Add(new FlipMove(segment.Side1Nodes.First(),segment.Side2Nodes.First(), false));
                return result;
            }
            if(segment.IsVertical)
            {
                TNode upperNode1 = ArgMaxJ<TNode>(segment.Side1Nodes, x => x.Rectangle.z);
                TNode upperNode2 = ArgMaxJ<TNode>(segment.Side2Nodes, x => x.Rectangle.z);
                Assert.IsTrue(upperNode1.getAllSegments()[Direction.Upper] == upperNode2.getAllSegments()[Direction.Upper]);

                TNode lowerNode1 = ArgMinJ<TNode>(segment.Side1Nodes, x => x.Rectangle.z);
                TNode lowerNode2 = ArgMinJ<TNode>(segment.Side2Nodes, x => x.Rectangle.z);
                Assert.IsTrue(lowerNode1.getAllSegments()[Direction.Lower] == lowerNode2.getAllSegments()[Direction.Lower]);

                result.Add(new StretchMove(upperNode1,upperNode2));
                result.Add(new StretchMove(lowerNode1,lowerNode2));
                return result;
            }
            TNode rightNode1 = ArgMaxJ<TNode>(segment.Side1Nodes, x => x.Rectangle.x);
            TNode rightNode2 = ArgMaxJ<TNode>(segment.Side2Nodes, x => x.Rectangle.x);
            Assert.IsTrue(rightNode1.getAllSegments()[Direction.Right] == rightNode2.getAllSegments()[Direction.Right]);

            TNode leftNode1 = ArgMinJ<TNode>(segment.Side1Nodes, x => x.Rectangle.x);
            TNode leftNode2 = ArgMinJ<TNode>(segment.Side2Nodes, x => x.Rectangle.x);
            Assert.IsTrue(leftNode1.getAllSegments()[Direction.Left] == leftNode2.getAllSegments()[Direction.Left]);

            result.Add(new StretchMove(rightNode1,rightNode2));
            result.Add(new StretchMove(leftNode1,leftNode2));
            return result;
        }

        public static void AddNode(IList<TNode> nodes,TNode newNode)
        {
            // ArgMax is shit in c#
            // node with rectangle with highest aspect ratio
            TNode bestNode = ArgMaxJ<TNode>(nodes, x => x.Rectangle.aspect_ratio());

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
        {
            // check wether node is grounded
            var segments = obsoleteNode.getAllSegments();
            bool isGrounded = false;
            if(segments[Direction.Left].Side2Nodes.Count == 1)
            {
                isGrounded = true;
                //[E][O]
                var expandingNodes = segments[Direction.Left].Side1Nodes;
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.width += obsoleteNode.Rectangle.width;
                    node.registerSegment(segments[Direction.Right], Direction.Right);
                }
            }
            else if(segments[Direction.Right].Side1Nodes.Count == 1)
            {
                isGrounded = true;
                //[O][E]
                var expandingNodes = segments[Direction.Right].Side2Nodes;
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.x = obsoleteNode.Rectangle.x;
                    node.Rectangle.width += obsoleteNode.Rectangle.width;
                    node.registerSegment(segments[Direction.Left], Direction.Left);
                }
            }
            else if(segments[Direction.Lower].Side2Nodes.Count == 1)
            {
                isGrounded = true;
                //[O]
                //[E]
                var expandingNodes = segments[Direction.Lower].Side1Nodes;
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.depth += obsoleteNode.Rectangle.depth;
                    node.registerSegment(segments[Direction.Upper], Direction.Upper);
                }
            }
            else if(segments[Direction.Upper].Side1Nodes.Count == 1)
            {  
                isGrounded = true;
                //[E]
                //[O]
                var expandingNodes = segments[Direction.Upper].Side2Nodes;
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.z = obsoleteNode.Rectangle.z;
                    node.Rectangle.depth += obsoleteNode.Rectangle.depth;
                    node.registerSegment(segments[Direction.Lower], Direction.Lower);
                }
            }
            if(isGrounded)
            {
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    obsoleteNode.deregisterSegment(dir);
                }
            }
            else
            {
                TSegment bestSegment = ArgMaxJ<TSegment>(segments.Values, x => x.Side1Nodes.Count + x.Side2Nodes.Count);
                
                var moves = findLocalMoves(bestSegment);
                Assert.IsTrue(moves.All(x => x is (StretchMove)));
                foreach(var move in moves)
                {
                    if(move.Node1 != obsoleteNode && move.Node2 != obsoleteNode)
                    {
                        move.apply();
                        DeleteNode(obsoleteNode);
                    }
                }
            }
        }
        
        public static void MakeLocalMoves(IList<TNode> nodes)
        {
        }
    }
}