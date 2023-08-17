using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using SEE.Game.City;
using Sirenix.Utilities;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// Provides an algorithm to recalibrate the layout, so that the areas of the <see cref="Rectangle"/>
    /// of <see cref="Node"/> match the wanted <see cref="Node.Size"/> of the node.
    /// </summary>
    internal static class CorrectAreas
    {
        /// <summary>
        /// Recalibrate the layout, so that the areas of the <see cref="Rectangle"/>
        /// of <see cref="Node"/> match the wanted <see cref="Node.Size"/> of the node.
        /// </summary>
        /// <param name="nodes">nodes with layout</param>
        /// <param name="settings">the settings</param>
        /// <returns></returns>
        public static bool Correct(IList<Node> nodes, IncrementalTreeMapSetting settings)
        {
            if (nodes.Count == 1) return true;
            if (IsSliceAble(nodes, out Segment slicingSegment))
            {
                Split(nodes, slicingSegment,
                    out IList<Node> partition1,
                    out IList<Node> partition2);
                Assert.AreEqual(partition1.Count + partition2.Count, nodes.Count);
                AdjustSliced(partition1, partition2, slicingSegment);
                slicingSegment.IsConst = true;
                bool works1 = Correct(partition1, settings);
                bool works2 = Correct(partition2, settings);
                slicingSegment.IsConst = false;
                return works1 && works2;
            }
            else
            {
                return GradientDecent(nodes, settings);
            }
        }

        /// <summary>
        /// Checks the layout of <paramref name="nodes"/> can be divided into two disjoint sub layouts.
        /// </summary>
        /// <param name="nodes">nodes with layout</param>
        /// <param name="slicingSegment">a segment that would separate the sub layouts</param>
        /// <returns>true if nodes are slice able else false</returns>
        private static bool IsSliceAble(IList<Node> nodes, out Segment slicingSegment)
        {
            slicingSegment = null;
            var segments = nodes.SelectMany(n => n.SegmentsDictionary().Values).Distinct();
            foreach (var segment in segments)
            {
                slicingSegment = segment;
                if (segment.IsConst) continue;
                if (segment.IsVertical)
                {
                    var nodeLowerEnd = Utils.ArgMin(segment.Side1Nodes, node => node.Rectangle.Z);
                    var nodeUpperEnd = Utils.ArgMax(segment.Side1Nodes, node => node.Rectangle.Z);
                    if (nodeLowerEnd.SegmentsDictionary()[Direction.Lower].IsConst &&
                        nodeUpperEnd.SegmentsDictionary()[Direction.Upper].IsConst) return true;
                }
                else
                {
                    var nodeLeftEnd = Utils.ArgMin(segment.Side1Nodes, node => node.Rectangle.X);
                    var nodeRightEnd = Utils.ArgMax(segment.Side1Nodes,node => node.Rectangle.X);
                    if (nodeLeftEnd.SegmentsDictionary()[Direction.Left].IsConst &&
                        nodeRightEnd.SegmentsDictionary()[Direction.Right].IsConst) return true;
                }
            }

            slicingSegment = null;
            return false;
        }

        /// <summary>
        /// Splits the layout of <paramref name="nodes"/> into two disjoint layouts <paramref name="partition1"/>
        /// and <paramref name="partition2"/>. 
        /// </summary>
        /// <param name="nodes">nodes with layout</param>
        /// <param name="slicingSegment"> the segment that divides both layouts</param>
        /// <param name="partition1">the <see cref="Direction.Lower"/>/<see cref="Direction.Left"/> sub layout</param>
        /// <param name="partition2">the <see cref="Direction.Upper"/>/<see cref="Direction.Right"/> sub layout</param>
        private static void Split(IList<Node> nodes, Segment slicingSegment,
            out IList<Node> partition1, out IList<Node> partition2)
        {
            partition1 = new List<Node>();
            partition2 = new List<Node>();
            if (slicingSegment.IsVertical)
            {
                double xPosSegment = slicingSegment.Side2Nodes.First().Rectangle.X;
                foreach (var node in nodes)
                {
                    if (slicingSegment.Side1Nodes.Contains(node))
                    {
                        partition1.Add(node);
                        continue;
                    }
                    if (slicingSegment.Side2Nodes.Contains(node))
                    {
                        partition2.Add(node);
                        continue;
                    }
                    
                    if (node.Rectangle.X + .5 * node.Rectangle.Width < xPosSegment)
                    {
                        partition1.Add(node);
                    }
                    else
                    {
                        partition2.Add(node);
                    }
                }
            }
            else
            {
                double zPosSegment = slicingSegment.Side2Nodes.First().Rectangle.Z;
                foreach (var node in nodes)
                {
                    if (slicingSegment.Side1Nodes.Contains(node))
                    {
                        partition1.Add(node);
                        continue;
                    }
                    if (slicingSegment.Side2Nodes.Contains(node))
                    {
                        partition2.Add(node);
                        continue;
                    }
                    
                    if (node.Rectangle.Z + .5 * node.Rectangle.Depth < zPosSegment)
                    {
                        partition1.Add(node);
                    }
                    else
                    {
                        partition2.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// Adjust the layout so that the size of <paramref name="partition1"/> and <paramref name="partition2"/> 
        /// are consistent with the sizes of their nodes.
        /// </summary>
        /// <param name="partition1">the <see cref="Direction.Lower"/>/<see cref="Direction.Left"/> sub layout</param>
        /// <param name="partition2">the <see cref="Direction.Upper"/>/<see cref="Direction.Right"/> sub layout</param>
        /// <param name="slicingSegment"></param>
        private static void AdjustSliced(IList<Node> partition1,
            IList<Node> partition2,
            Segment slicingSegment)
        {
            Rectangle rectangle1Old = Utils.CreateParentRectangle(partition1);
            Rectangle rectangle2Old = Utils.CreateParentRectangle(partition2);
            double size1 = partition1.Sum(n => n.Size);
            double size2 = partition2.Sum(n => n.Size);
            Rectangle rectangle1New = rectangle1Old.Clone();
            Rectangle rectangle2New = rectangle2Old.Clone();

            if (slicingSegment.IsVertical)
            {
                rectangle1New.Width = size1 / rectangle1New.Depth;
                rectangle2New.Width = size2 / rectangle2New.Depth;
                rectangle2New.X = rectangle1New.X + rectangle1New.Width;
            }
            else
            {
                rectangle1New.Depth = size1 / rectangle1New.Width;
                rectangle2New.Depth = size2 / rectangle2New.Width;
                rectangle2New.Z = rectangle1New.Z + rectangle1New.Depth;
            }
            Utils.TransformRectangles(partition1, newRectangle: rectangle1New, oldRectangle: rectangle1Old);
            Utils.TransformRectangles(partition2, newRectangle: rectangle2New, oldRectangle: rectangle2Old);
        }

        /// <summary>
        /// A gradient decent approach to recalibrate the layout
        /// </summary>
        /// <param name="nodes">nodes with layout</param>
        /// <param name="settings">the setting</param>
        /// <returns></returns>
        private static bool GradientDecent(IList<Node> nodes, IncrementalTreeMapSetting settings)
        {
            var segments = nodes.SelectMany(n => n.SegmentsDictionary().Values).ToHashSet();
            segments.RemoveWhere(s => s.IsConst);
            int i = 0;
            Dictionary<Segment, int> mapSegmentIndex
                = segments.ToDictionary(s => s, _ => i++);

            double distance = 0;
            double maximalError = Math.Pow(10, settings.gradientDescentPrecisionExponent);
            for (int j = 0; j < 50; j++)
            {
                distance = CalculateOneStep(nodes, mapSegmentIndex);
                if (distance <= maximalError) break;
            }

            if (distance > maximalError)
            {
                Debug.LogWarning($" layout correction > {maximalError}");
            }

            bool cons = CheckNegativeLength(nodes);
            if (!cons)
            {
                Debug.LogWarning("layout correction failed negative rec");
            }

            return (cons && distance < maximalError);
        }

        /// <summary>
        /// Calculates the Jacobian Matrix. A derivative for the function that
        /// the position of the segments to the area of each node (its rectangle)
        /// </summary>
        /// <param name="nodes">the nodes of the layout</param>
        /// <param name="mapSegmentIndex">the segments as dictionary with their index in the function</param>
        /// <returns></returns>
        private static Matrix<double> JacobianMatrix(
            IList<Node> nodes,
            Dictionary<Segment, int> mapSegmentIndex)
        {
            int n = nodes.Count;
            var matrix = Matrix<double>.Build.Sparse(n, n - 1);
            foreach (var node in nodes)
            {
                var segments = node.SegmentsDictionary();
                int indexNode = nodes.IndexOf(node);
                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var segment = segments[dir];
                    if (!segment.IsConst)
                    {
                        double value = dir switch
                        {
                            Direction.Left => -node.Rectangle.Depth,
                            Direction.Right => node.Rectangle.Depth,
                            Direction.Lower => -node.Rectangle.Width,
                            Direction.Upper => node.Rectangle.Width,
                            _ => 0
                        };

                        matrix[indexNode, mapSegmentIndex[segment]] = value;
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// Moves the segments 'one step' the gradient direction.
        /// </summary>
        /// <param name="nodes">the nodes of the layout</param>
        /// <param name="mapSegmentIndex">the segments as dictionary with their index in the function</param>
        /// <returns>the error between the current state and the wanted state</returns>
        private static double CalculateOneStep(
            IList<Node> nodes,
            Dictionary<Segment, int> mapSegmentIndex)
        {
            Matrix<double> matrix = JacobianMatrix(nodes, mapSegmentIndex);

            Vector<double> nodesSizesWanted =
                Vector<double>.Build.DenseOfArray(nodes.Select(node => (double)node.Size).ToArray());
            Vector<double> nodesSizesCurrent =
                Vector<double>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
            var diff = nodesSizesWanted - nodesSizesCurrent;
            Matrix<double> pseudoInverse = matrix.PseudoInverse();
            Vector<double> segmentShift = pseudoInverse * diff;
            ApplyShift(segmentShift, nodes, mapSegmentIndex);

            Vector<double> nodesSizesAfterStep =
                Vector<double>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
            return (nodesSizesAfterStep - nodesSizesWanted).Norm(1);
        }

        /// <summary>
        /// Applies the calculated shift of the segments to the nodes (their rectangles)
        /// </summary>
        /// <param name="shift">the calculated shift</param>
        /// <param name="nodes">the nodes of the layout</param>
        /// <param name="mapSegmentIndex">the segments as dictionary with their index in the function</param>
        private static void ApplyShift(
            Vector<double> shift,
            IList<Node> nodes,
            Dictionary<Segment, int> mapSegmentIndex)
        {
            foreach (var node in nodes)
            {
                var segments = node.SegmentsDictionary();
                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var segment = segments[dir];
                    if (segment.IsConst) continue;
                    var value = shift[mapSegmentIndex[segment]];
                    switch (dir)
                    {
                        case Direction.Left:
                            node.Rectangle.X += value;
                            node.Rectangle.Width -= value;
                            break;
                        case Direction.Right:
                            node.Rectangle.Width += value;
                            break;
                        case Direction.Lower:
                            node.Rectangle.Z += value;
                            node.Rectangle.Depth -= value;
                            break;
                        case Direction.Upper:
                            node.Rectangle.Depth += value;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// A checker that verifies that the result has no rectangles with negative width or depth 
        /// </summary>
        /// <param name="nodes">nodes of the layout</param>
        /// <returns>true if all rectangles are fine, else false</returns>
        private static bool CheckNegativeLength(IList<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Rectangle.Width <= 0 || node.Rectangle.Depth <= 0) return false;
            }

            return true;
        }
    }
}