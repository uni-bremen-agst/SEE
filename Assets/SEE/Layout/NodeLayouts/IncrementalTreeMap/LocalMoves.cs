using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class LocalMoves
    {
        private static IList<LocalMove> FindLocalMoves(Segment segment)
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
                Node upperNode1 = Utils.ArgMax(segment.Side1Nodes, x => x.Rectangle.z);
                Node upperNode2 = Utils.ArgMax(segment.Side2Nodes, x => x.Rectangle.z);
                Assert.IsTrue(upperNode1.SegmentsDictionary()[Direction.Upper] == upperNode2.SegmentsDictionary()[Direction.Upper]);

                Node lowerNode1 = Utils.ArgMin(segment.Side1Nodes, x => x.Rectangle.z);
                Node lowerNode2 = Utils.ArgMin(segment.Side2Nodes, x => x.Rectangle.z);
                Assert.IsTrue(lowerNode1.SegmentsDictionary()[Direction.Lower] == lowerNode2.SegmentsDictionary()[Direction.Lower]);

                result.Add(new StretchMove(upperNode1,upperNode2));
                result.Add(new StretchMove(lowerNode1,lowerNode2));
                return result;
            }
            Node rightNode1 = Utils.ArgMax(segment.Side1Nodes, x => x.Rectangle.x);
            Node rightNode2 = Utils.ArgMax(segment.Side2Nodes, x => x.Rectangle.x);
            Assert.IsTrue(rightNode1.SegmentsDictionary()[Direction.Right] == rightNode2.SegmentsDictionary()[Direction.Right]);

            Node leftNode1 = Utils.ArgMin(segment.Side1Nodes, x => x.Rectangle.x);
            Node leftNode2 = Utils.ArgMin(segment.Side2Nodes, x => x.Rectangle.x);
            Assert.IsTrue(leftNode1.SegmentsDictionary()[Direction.Left] == leftNode2.SegmentsDictionary()[Direction.Left]);

            result.Add(new StretchMove(rightNode1,rightNode2));
            result.Add(new StretchMove(leftNode1,leftNode2));
            return result;
        }

        public static void AddNode(IList<Node> nodes,Node newNode)
        {
            // ArgMax is shit in c#
            // node with rectangle with highest aspect ratio
            Node bestNode = Utils.ArgMax(nodes, x => x.Rectangle.AspectRatio());

            newNode.Rectangle = new Rectangle(x: bestNode.Rectangle.x, z: bestNode.Rectangle.z,
                                               width: bestNode.Rectangle.width, depth: bestNode.Rectangle.depth);
            IDictionary<Direction,Segment> segments = bestNode.SegmentsDictionary();
            foreach(Direction dir in Enum.GetValues(typeof(Direction)))
            {
                newNode.RegisterSegment(segments[dir],dir);
            }
            if(bestNode.Rectangle.width >= bestNode.Rectangle.depth)
            {
                // [bestNode]|[newNode]
                Segment newSegment = new Segment(isConst: false, isVertical: true);
                newNode.RegisterSegment(newSegment, Direction.Left);
                bestNode.RegisterSegment(newSegment, Direction.Right);
                bestNode.Rectangle.width *= 0.5f;
                newNode.Rectangle.width *= 0.5f;
                newNode.Rectangle.x = bestNode.Rectangle.x + bestNode.Rectangle.width;
            }
            else
            {
                // [newNode]
                // ---------
                // [bestNode]
                Segment newSegment = new Segment(isConst: false, isVertical: false);
                newNode.RegisterSegment(newSegment, Direction.Lower);
                bestNode.RegisterSegment(newSegment, Direction.Upper);
                bestNode.Rectangle.depth *= 0.5f;
                newNode.Rectangle.depth *= 0.5f;
                newNode.Rectangle.z = bestNode.Rectangle.z + bestNode.Rectangle.depth;
            }
        }

        public static void DeleteNode(Node obsoleteNode)
        {
            // check whether node is grounded
            var segments = obsoleteNode.SegmentsDictionary();
            bool isGrounded = false;
            if(segments[Direction.Left].Side2Nodes.Count == 1 && !segments[Direction.Left].IsConst)
            {
                isGrounded = true;
                //[E][O]
                var expandingNodes = segments[Direction.Left].Side1Nodes.ToArray();
                foreach(var node in expandingNodes)
                {
                    node.Rectangle.width += obsoleteNode.Rectangle.width;
                    node.RegisterSegment(segments[Direction.Right], Direction.Right);
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
                    node.RegisterSegment(segments[Direction.Left], Direction.Left);
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
                    node.RegisterSegment(segments[Direction.Upper], Direction.Upper);
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
                    node.RegisterSegment(segments[Direction.Lower], Direction.Lower);
                }
            }
            if(isGrounded)
            {
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    obsoleteNode.DeregisterSegment(dir);
                }
            }
            else
            {
                Segment bestSegment = Utils.ArgMin(segments.Values, x => x.Side1Nodes.Count + x.Side2Nodes.Count);
                
                var moves = FindLocalMoves(bestSegment);
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
        
        public static void MakeLocalMoves(List<Node> nodes, int amount)
        {
            var allResults = RecursiveMakeMoves(nodes,amount);
            allResults.Add(new Tuple<List<Node>, double>(nodes,AspectRatiosPNorm(nodes)));
            var bestResult = Utils.ArgMin(allResults, x => x.Item2).Item1;
            foreach(var node in nodes)
            {
                var resultNode = bestResult.Find(n => n.ID == node.ID);
                node.Rectangle = resultNode.Rectangle;
            }
            HashSet<Segment> resultSegments = new HashSet<Segment>();
            foreach(var resultNode in bestResult)
            {
                resultSegments.UnionWith(resultNode.SegmentsDictionary().Values);
            }
            foreach(var resultSegment in resultSegments)
            {
                var newSegment = new Segment(resultSegment.IsConst,resultSegment.IsVertical);
                foreach(var resultNode in resultSegment.Side1Nodes.ToArray())
                {
                    var node = nodes.Find(n => n.ID == resultNode.ID);
                    node.RegisterSegment(newSegment, 
                        newSegment.IsVertical ? Direction.Right : Direction.Upper);
                }
                foreach(var resultNode in resultSegment.Side2Nodes.ToArray())
                {
                    var node = nodes.Find(n => n.ID == resultNode.ID);
                    node.RegisterSegment(newSegment, 
                        newSegment.IsVertical ? Direction.Left : Direction.Lower);
                }
            }
        }

        private static List<Tuple<List<Node>,double>> RecursiveMakeMoves(
            List<Node> nodes,
            int amount)
        {
            var resultThisRecursion = new List<Tuple<List<Node>,double>>();
            
            if(amount <= 0) return resultThisRecursion;

            HashSet<Segment> segments = new HashSet<Segment>();
            foreach(var node in nodes)
            {
                segments.UnionWith(node.SegmentsDictionary().Values);
            }
            List<LocalMove> possibleMoves = new List<LocalMove>();
            foreach(var segment in segments)
            {
                possibleMoves.AddRange(FindLocalMoves(segment));
            }

            foreach(var move in possibleMoves)
            {
                var nodeClones = CloneGraph(nodes,segments);
                var moveClone = move.Clone(nodeClones);
                moveClone.Apply();
                var works = CorrectAreas.Correct(nodeClones.Values.ToList());
                if(!works) continue;
                resultThisRecursion.Add(
                    new Tuple<List<Node>, double>(nodeClones.Values.ToList(),AspectRatiosPNorm(nodeClones.Values.ToList())));
            }
            resultThisRecursion.Sort((x,y) => x.Item2.CompareTo(y.Item2));
            while(resultThisRecursion.Count > Parameters.RecursionBoundToBestSelection)
            {
                resultThisRecursion.RemoveAt(Parameters.RecursionBoundToBestSelection);
            }

            var resultsNextRecursions = new List<Tuple<List<Node>,double>>(); 
            foreach(var result in resultThisRecursion)
            {
                resultsNextRecursions.AddRange(RecursiveMakeMoves(result.Item1, amount-1));
            }
            return resultThisRecursion.Concat(resultsNextRecursions).ToList();
        }

        private static double AspectRatiosPNorm(IList<Node> nodes)
        {
            Vector<double> aspectRatios = Vector<double>.Build.DenseOfEnumerable(nodes.Select(n => n.Rectangle.AspectRatio()));
            return aspectRatios.Norm(Parameters.PNorm);
        }

        private static Dictionary<string,Node> CloneGraph(IList<Node> nodes, HashSet<Segment> segments)
        {
            Dictionary<string,Node> mapOriginalClone = 
            nodes.ToDictionary(
                node => node.ID,
                node => {
                            var nodeClone = new Node(node.ID)
                            {
                                Rectangle = (Rectangle) node.Rectangle.Clone(),
                                Size = node.Size
                            };
                            return nodeClone;
                        });

            foreach(var segment in segments)
            {
                var segmentClone = new Segment(segment.IsConst, segment.IsVertical);
                foreach(var node in segment.Side1Nodes.ToArray())
                {
                    mapOriginalClone[node.ID].RegisterSegment(segmentClone, 
                        segmentClone.IsVertical ? Direction.Right : Direction.Upper);
                }
                foreach(var node in segment.Side2Nodes.ToArray())
                {
                    mapOriginalClone[node.ID].RegisterSegment(segmentClone, 
                        segmentClone.IsVertical ? Direction.Left : Direction.Lower);
                }
            }
            return mapOriginalClone;
        }
    }
}