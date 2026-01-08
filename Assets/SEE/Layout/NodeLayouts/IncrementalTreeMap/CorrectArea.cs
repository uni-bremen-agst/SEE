using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using MathNet.Numerics.LinearAlgebra;
using SEE.Game.City;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// Provides an algorithm to recalibrate the layout, so that the areas of the <see cref="Rectangle"/>
    /// of <see cref="Node"/> match the <see cref="Node.DesiredSize"/> of the node.
    /// </summary>
    internal static class CorrectAreas
    {
        /// <summary>
        /// Recalibrate the layout, so that the areas of the <see cref="Rectangle"/>
        /// of <see cref="Node"/> match the <see cref="Node.DesiredSize"/> of the node.
        /// </summary>
        /// <param name="nodes">Nodes with layout.</param>
        /// <param name="settings">The settings of the incremental tree map layout.</param>
        /// <returns>True if correction was successful, else false.</returns>
        public static bool Correct(IList<Node> nodes, IncrementalTreeMapAttributes settings)
        {
            if (nodes.Count == 1)
            {
                return true;
            }

            if (IsSliceAble(nodes, out Segment slicingSegment))
            {
                Split(nodes, slicingSegment,
                    out IList<Node> partition1,
                    out IList<Node> partition2);

                // adjust the position of slicingSegments
                AdjustSliced(nodes, partition1, partition2, slicingSegment);
                // recursively adjust the two sublayouts
                // since both sublayouts are temporally independent from each other
                // the segment that separates these must be considered as a border (IsConst = true)
                slicingSegment.IsConst = true;
                Correct(partition1, settings);
                Correct(partition2, settings);
                slicingSegment.IsConst = false;

                Vector<double> nodesSizesWanted =
                    Vector<double>.Build.DenseOfArray(nodes.Select(node => (double)node.DesiredSize).ToArray());
                Vector<double> nodesSizesCurrent =
                    Vector<double>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
                double error = (nodesSizesWanted - nodesSizesCurrent).Norm(p: 1);

                return error <= Math.Pow(10, settings.GradientDescentPrecision) && CheckPositiveLength(nodes);
            }
            else
            {
                return GradientDecent(nodes, settings);
            }
        }

        /// <summary>
        /// Checks if the layout of <paramref name="nodes"/> can be divided into two disjoint sublayouts.
        /// </summary>
        /// <param name="nodes">Nodes with layout.</param>
        /// <param name="slicingSegment">A segment that would separate the sublayouts.</param>
        /// <returns>True if nodes are sliceable, else false.</returns>
        private static bool IsSliceAble(IList<Node> nodes, out Segment slicingSegment)
        {
            slicingSegment = null;
            IEnumerable<Segment> segments = nodes.SelectMany(n => n.SegmentsDictionary().Values).Distinct();
            foreach (Segment segment in segments)
            {
                slicingSegment = segment;
                if (segment.IsConst)
                {
                    continue;
                }
                if (segment.IsVertical)
                {
                    Node nodeLowerEnd = Utils.ArgMin(segment.Side1Nodes, node => node.Rectangle.Z);
                    Node nodeUpperEnd = Utils.ArgMax(segment.Side1Nodes, node => node.Rectangle.Z);
                    if (nodeLowerEnd.SegmentsDictionary()[Direction.Lower].IsConst &&
                        nodeUpperEnd.SegmentsDictionary()[Direction.Upper].IsConst)
                    {
                        return true;
                    }
                }
                else
                {
                    Node nodeLeftEnd = Utils.ArgMin(segment.Side1Nodes, node => node.Rectangle.X);
                    Node nodeRightEnd = Utils.ArgMax(segment.Side1Nodes, node => node.Rectangle.X);
                    if (nodeLeftEnd.SegmentsDictionary()[Direction.Left].IsConst &&
                        nodeRightEnd.SegmentsDictionary()[Direction.Right].IsConst)
                    {
                        return true;
                    }
                }
            }

            slicingSegment = null;
            return false;
        }

        /// <summary>
        /// Splits the layout of <paramref name="nodes"/> into two disjoint layouts <paramref name="partition1"/>
        /// and <paramref name="partition2"/>.
        /// </summary>
        /// <param name="nodes">Nodes with layout.</param>
        /// <param name="slicingSegment"> The segment that divides both layouts.</param>
        /// <param name="partition1">The <see cref="Direction.Lower"/>/<see cref="Direction.Left"/> sublayout.</param>
        /// <param name="partition2">The <see cref="Direction.Upper"/>/<see cref="Direction.Right"/> sublayout.</param>
        private static void Split(IList<Node> nodes, Segment slicingSegment,
            out IList<Node> partition1, out IList<Node> partition2)
        {
            partition1 = new List<Node>();
            partition2 = new List<Node>();
            if (slicingSegment.IsVertical)
            {
                double xPosSegment = slicingSegment.Side2Nodes.First().Rectangle.X;
                foreach (Node node in nodes)
                {
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
                foreach (Node node in nodes)
                {
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
        /// This method recalibrates a layout that is sliced in two sublayouts,
        /// so the sublayouts get the size they should have.
        /// A sublayout can still have internal wrong node sizes.
        /// </summary>
        /// <param name="nodes">Nodes of a sliceable layout.</param>
        /// <param name="partition1">Partition of <paramref name="nodes"/>,
        /// the <see cref="Direction.Lower"/>/<see cref="Direction.Left"/> sublayout.</param>
        /// <param name="partition2">Partition of <paramref name="nodes"/>,
        /// the <see cref="Direction.Upper"/>/<see cref="Direction.Right"/> sublayout.</param>
        /// <param name="slicingSegment">The segment that slices the layout.</param>
        private static void AdjustSliced(
            IList<Node> nodes,
            IList<Node> partition1,
            IList<Node> partition2,
            Segment slicingSegment)
        {

            // creating four rectangles - two for each sublayout
            // old rectangle -> current parent rectangle
            // new rectangle -> parent rectangle how it should be after adjustment
            Rectangle rectangle1Old = Utils.CreateParentRectangle(nodes);
            Rectangle rectangle2Old = rectangle1Old.Clone();
            Rectangle rectangle1New = rectangle1Old.Clone();
            Rectangle rectangle2New = rectangle1Old.Clone();

            // set the correct sizes for the four rectangles
            if (slicingSegment.IsVertical)
            {
                double segmentXPosition = slicingSegment.Side2Nodes.First().Rectangle.X;
                rectangle1Old.Width = segmentXPosition - rectangle1Old.X;
                rectangle2Old.Width -= rectangle1Old.Width;
                rectangle2Old.X = rectangle1Old.X + rectangle1Old.Width;

                float ratio = partition1.Sum(n => n.DesiredSize) / nodes.Sum(n => n.DesiredSize);
                rectangle1New.Width *= ratio;
                rectangle2New.Width *= (1 - ratio);
                rectangle2New.X = rectangle1New.X + rectangle1New.Width;
            }
            else
            {
                double segmentZPosition = slicingSegment.Side2Nodes.First().Rectangle.Z;
                rectangle1Old.Depth = segmentZPosition - rectangle1Old.Z;
                rectangle2Old.Depth -= rectangle1Old.Depth;
                rectangle2Old.Z = rectangle1Old.Z + rectangle1Old.Depth;

                float ratio = partition1.Sum(n => n.DesiredSize) / nodes.Sum(n => n.DesiredSize);
                rectangle1New.Depth *= ratio;
                rectangle2New.Depth *= (1 - ratio);
                rectangle2New.Z = rectangle1New.Z + rectangle1New.Depth;
            }

            Utils.TransformRectangles(partition1, newRectangle: rectangle1New, oldRectangle: rectangle1Old);
            Utils.TransformRectangles(partition2, newRectangle: rectangle2New, oldRectangle: rectangle2Old);
        }

        /// <summary>
        /// A gradient decent approach to recalibrate the layout.
        /// This approach 'moves' the segment in several steps, until the desired sizes are realized.
        /// In one step it calculates a shift for all segments at once
        /// by setting up a function that maps the position of the segments to the area of each node,
        /// getting the derivative of this function
        /// and calculating a shift for the segment vector in the direction of the desired size vector.
        /// </summary>
        /// <param name="nodes">Nodes with layout.</param>
        /// <param name="settings">The settings of the incremental tree map layout,
        /// especially including the maximal error between the desired layout and the result.</param>
        /// <returns>True if correction was successful, else false.</returns>
        private static bool GradientDecent(IList<Node> nodes, IncrementalTreeMapAttributes settings)
        {
            HashSet<Segment> segments = nodes.SelectMany(n => n.SegmentsDictionary().Values).ToHashSet();
            segments.RemoveWhere(s => s.IsConst);
            int i = 0;
            Dictionary<Segment, int> mapSegmentIndex
                = segments.ToDictionary(s => s, _ => i++);

            double distance = 0;
            double maximalError = Math.Pow(10, settings.GradientDescentPrecision);
            const int numberOfIterations = 50;
            for (int j = 0; j < numberOfIterations; j++)
            {
                distance = CalculateOneStep(nodes, mapSegmentIndex);
                if (distance <= maximalError)
                {
                    break;
                }
            }

            return CheckPositiveLength(nodes) && distance < maximalError;
        }

        /// <summary>
        /// Calculates the Jacobian matrix, a derivative for the function that maps
        /// the position of the segments to the area of each node (its rectangle).
        /// </summary>
        /// <param name="nodes">The nodes of the layout.</param>
        /// <param name="mapSegmentIndex">The segments as a dictionary with their index in the function.</param>
        /// <returns>The matrix.</returns>
        private static Matrix<double> JacobianMatrix(
            IList<Node> nodes,
            Dictionary<Segment, int> mapSegmentIndex)
        {
            int n = nodes.Count;
            Matrix<double> matrix = Matrix<double>.Build.Sparse(n, n - 1);
            for (int indexNode = 0; indexNode < nodes.Count; indexNode++)
            {
                Node node = nodes[indexNode];
                IDictionary<Direction, Segment> segments = node.SegmentsDictionary();
                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    Segment segment = segments[dir];
                    if (!segment.IsConst)
                    {
                        double value = dir switch
                        {
                            Direction.Left => -node.Rectangle.Depth,
                            Direction.Right => node.Rectangle.Depth,
                            Direction.Lower => -node.Rectangle.Width,
                            Direction.Upper => node.Rectangle.Width,
                            _ => throw new InvalidEnumArgumentException("Unrecognized Direction value.")
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
        /// <param name="nodes">The nodes of the layout.</param>
        /// <param name="mapSegmentIndex">The segments as dictionary with their index in the function.</param>
        /// <returns>The error between the current state and the wanted state.</returns>
        private static double CalculateOneStep(
            IList<Node> nodes,
            Dictionary<Segment, int> mapSegmentIndex)
        {
            Matrix<double> matrix = JacobianMatrix(nodes, mapSegmentIndex);

            Vector<double> desiredNodeSizes =
                Vector<double>.Build.DenseOfArray(nodes.Select(node => (double)node.DesiredSize).ToArray());
            Vector<double> currentNodeSizes =
                Vector<double>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
            Vector<double> diff = desiredNodeSizes - currentNodeSizes;
            Matrix<double> pseudoInverse = matrix.PseudoInverse();
            Vector<double> segmentShift = pseudoInverse * diff;
            ApplyShift(segmentShift, nodes, mapSegmentIndex);

            Vector<double> nodesSizesAfterStep =
                Vector<double>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
            return (nodesSizesAfterStep - desiredNodeSizes).Norm(1);
        }

        /// <summary>
        /// Applies the calculated shift of the segments to the nodes (their rectangles).
        /// </summary>
        /// <param name="shift">The calculated shift.</param>
        /// <param name="nodes">The nodes of the layout.</param>
        /// <param name="mapSegmentIndex">The segments as dictionary with their index in the function.</param>
        private static void ApplyShift(
            Vector<double> shift,
            IList<Node> nodes,
            Dictionary<Segment, int> mapSegmentIndex)
        {
            foreach (Node node in nodes)
            {
                IDictionary<Direction, Segment> segments = node.SegmentsDictionary();
                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    Segment segment = segments[dir];
                    if (segment.IsConst)
                    {
                        continue;
                    }
                    double value = shift[mapSegmentIndex[segment]];
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
        /// Verifies that the result has no rectangles with non-positive width or depth.
        /// </summary>
        /// <param name="nodes">Nodes of the layout.</param>
        /// <returns>True if all rectangles have positive lengths, else false.</returns>
        private static bool CheckPositiveLength(IList<Node> nodes)
        {
            return nodes.All(node => node.Rectangle.Width > 0 || node.Rectangle.Depth > 0);
        }
    }
}
