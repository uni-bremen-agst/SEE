using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.Assertions;
using MathNet.Numerics.LinearAlgebra;
using Unity.Jobs;
using Unity.Collections;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class LocalMoves
    {
        static double pNorm = double.PositiveInfinity;
        private static T ArgMaxJ<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Max(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }
        
        private static T ArgMinJ<T>(ICollection<T> collection, Func<T, IComparable> eval)
        {
            var bestVal = collection.Min(eval);
            return collection.First(x => eval(x).CompareTo(bestVal) == 0);
        }

        private static IList<LocalMove> findLocalMoves(TSegment segment)
        {
            List<LocalMove> result = new List<LocalMove>();
            if(segment.IsConst)
            {
                return result;
            }
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
            TNode bestNode = ArgMaxJ<TNode>(nodes, x => x.Rectangle.AspectRatio());

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
                TSegment newSegment = new TSegment(isConst: false, isVertical: true);
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
                TSegment newSegment = new TSegment(isConst: false, isVertical: false);
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
            if(segments[Direction.Left].Side2Nodes.Count == 1 && !segments[Direction.Left].IsConst)
            {
                isGrounded = true;
                //[E][O]
                var expandingNodes = segments[Direction.Left].Side1Nodes.ToArray();
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.width += obsoleteNode.Rectangle.width;
                    node.registerSegment(segments[Direction.Right], Direction.Right);
                }
            }
            else if(segments[Direction.Right].Side1Nodes.Count == 1 && !segments[Direction.Right].IsConst)
            {
                isGrounded = true;
                //[O][E]
                var expandingNodes = segments[Direction.Right].Side2Nodes.ToArray();
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.x = obsoleteNode.Rectangle.x;
                    node.Rectangle.width += obsoleteNode.Rectangle.width;
                    node.registerSegment(segments[Direction.Left], Direction.Left);
                }
            }
            else if(segments[Direction.Lower].Side2Nodes.Count == 1 && !segments[Direction.Lower].IsConst)
            {
                isGrounded = true;
                //[O]
                //[E]
                var expandingNodes = segments[Direction.Lower].Side1Nodes.ToArray();
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.depth += obsoleteNode.Rectangle.depth;
                    node.registerSegment(segments[Direction.Upper], Direction.Upper);
                }
            }
            else if(segments[Direction.Upper].Side1Nodes.Count == 1 && !segments[Direction.Upper].IsConst)
            {  
                isGrounded = true;
                //[E]
                //[O]
                var expandingNodes = segments[Direction.Upper].Side2Nodes.ToArray();
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
                        move.Apply();
                        DeleteNode(obsoleteNode);
                        return;
                    }
                }
                // We should never arrive here
                Assert.IsFalse(true);
            }
        }
        
        public static void MakeLocalMoves(IList<TNode> nodes, int amount)
        {
            //var startNodes = nodes.ToDictionary(node => node.RepresentLayoutNode.ID, node => node);
            //HashSet<TSegment> segments = new HashSet<TSegment>();
            //foreach(var node in nodes)
            //{
            //    segments.UnionWith(node.getAllSegments().Values);
            //}
            //List<LocalMove> possibleMoves = new List<LocalMove>();
            //foreach(var segment in segments)
            //{
            //    possibleMoves.AddRange(findLocalMoves(segment));
            //}
            //var nextIteration = new ParallelMovesJob();
            //nextIteration.amount = amount;
            //nextIteration.nodes = startNodes.Values.ToList();
            //nextIteration.moves = possibleMoves;
            //nextIteration.Schedule(possibleMoves.Count,32);
            //var allResults = nextIteration.result;

            var startNodes = nodes.ToDictionary(node => node.ID, node => node);
            var allResults = RecursiveMakeMoves(startNodes,amount);
            allResults.Add(new Tuple<Dictionary<string, TNode>, double>(startNodes,AspectRatiosPNorm(nodes)));
            var bestResult = ArgMinJ(allResults, x => x.Item2).Item1;
            foreach(var node in nodes)
            {
                var resultNode = bestResult[node.ID];
                node.Rectangle = resultNode.Rectangle;
            }
            HashSet<TSegment> resultSegments = new HashSet<TSegment>();
            foreach(var resultNode in bestResult.Values)
            {
                resultSegments.UnionWith(resultNode.getAllSegments().Values);
            }
            foreach(var resultSegment in resultSegments)
            {
                var newSegment = new TSegment(resultSegment.IsConst,resultSegment.IsVertical);
                foreach(var resultNode in resultSegment.Side1Nodes.ToArray())
                {
                    startNodes[resultNode.ID].registerSegment(newSegment, 
                        newSegment.IsVertical ? Direction.Right : Direction.Upper);
                }
                foreach(var resultNode in resultSegment.Side2Nodes.ToArray())
                {
                    startNodes[resultNode.ID].registerSegment(newSegment, 
                        newSegment.IsVertical ? Direction.Left : Direction.Lower);
                }
            }
        }

        private static List<Tuple<Dictionary<string,TNode>,double>> RecursiveMakeMoves(
            Dictionary<string,TNode> nodesMap,
            int amount)
        {
            List<TNode> nodes = nodesMap.Values.ToList();
            //nodes.Sort((x,y) => y.Rectangle.AspectRatio().CompareTo(x.Rectangle.AspectRatio()));
            List<Tuple<Dictionary<string,TNode>,double>> result = new List<Tuple<Dictionary<string,TNode>,double>>();
            if(amount <= 0)
            {   
                var x = new Tuple<Dictionary<string,TNode>,double> (nodesMap, AspectRatiosPNorm(nodes));
                result.Add(x);
                return result;
            }
            amount--;
            HashSet<TSegment> segments = new HashSet<TSegment>();
            HashSet<TSegment> segmentsForMoves = new HashSet<TSegment>();
            int i = 5;
            foreach(var node in nodes)
            {
                segments.UnionWith(node.getAllSegments().Values);
                //if(i <= 0) continue;
                segmentsForMoves.UnionWith(node.getAllSegments().Values);
                i--;
            }
            List<LocalMove> possibleMoves = new List<LocalMove>();
            foreach(var segment in segmentsForMoves)
            {
                possibleMoves.AddRange(findLocalMoves(segment));
            }

            var possibleResults = new List<Tuple<Dictionary<string,TNode>,double>>();
            foreach(var move in possibleMoves)
            {
                var nodeCloneMap = CloneGraph(nodes,segments);
                var moveClone = move.Clone(nodeCloneMap);
                moveClone.Apply();
                var works = CorrectAreas.Correct(nodeCloneMap.Values.ToList());
                if(!works) continue;
                possibleResults.Add(
                    new Tuple<Dictionary<string, TNode>, double>(nodeCloneMap,AspectRatiosPNorm(nodeCloneMap.Values.ToList())));
            }
            possibleResults.Sort((x,y) => x.Item2.CompareTo(y.Item2));
            i = 3;
            foreach(var possibleResult in possibleResults)
            {
                if(i <= 0) break;
                i--;
                result.Add(possibleResult);
                var outcome = RecursiveMakeMoves(possibleResult.Item1, amount-1);
                result.AddRange(outcome);
            }
            return result;
        }

        private static double AspectRatiosPNorm(IList<TNode> nodes)
        {
            Vector<float> aspectRatios = Vector<float>.Build.DenseOfEnumerable(nodes.Select(n => n.Rectangle.AspectRatio()));
            return aspectRatios.Norm(pNorm);
        }

        private static Dictionary<string,TNode> CloneGraph(IList<TNode> nodes, HashSet<TSegment> segments)
        {
            Dictionary<string,TNode> mapOriginalClone = 
            nodes.ToDictionary(
                node => node.ID,
                node => {
                            var nodeClone = new TNode(node.ID);
                            nodeClone.Rectangle = (TRectangle) node.Rectangle.Clone();
                            nodeClone.Size = node.Size;
                            return nodeClone;
                        });

            foreach(var segment in segments)
            {
                var segmentClone = new TSegment(segment.IsConst, segment.IsVertical);
                foreach(var node in segment.Side1Nodes.ToArray())
                {
                    mapOriginalClone[node.ID].registerSegment(segmentClone, 
                        segmentClone.IsVertical ? Direction.Right : Direction.Upper);
                }
                foreach(var node in segment.Side2Nodes.ToArray())
                {
                    mapOriginalClone[node.ID].registerSegment(segmentClone, 
                        segmentClone.IsVertical ? Direction.Left : Direction.Lower);
                }
            }
            return mapOriginalClone;
        }
    }
}