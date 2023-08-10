using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Distributions;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class CorrectAreas
    {
        public static bool Correct(IList<TNode> nodes)
        {
            if (nodes.Count == 1) return true;
            if(IsSliceable( nodes, out TSegment slicingSegment))
            {
                Split(nodes, slicingSegment, 
                    out IList<TNode> partition1, 
                    out IList<TNode> partition2);
                AdjustSliced(partition1,partition2,slicingSegment);
                slicingSegment.IsConst = true;
                bool works1 = Correct(partition1);
                bool works2 = Correct(partition2);
                slicingSegment.IsConst = false;
                return works1 && works2;
            }
            else
            {
                return GradientDecrease(nodes);
            }

        }
        
        private static bool IsSliceable(IList<TNode> nodes, out TSegment slicingSegment)
        {
            slicingSegment = null;
            var segments = nodes.SelectMany(n => n.SegmentsDictionary().Values).ToHashSet();
            foreach (var segment in segments)
            {
                slicingSegment = segment;
                if (segment.IsConst) return false;
                if (segment.IsVertical)
                {
                    var nodeLowerEnd = Utils.ArgMin(segment.Side1Nodes, node => node.Rectangle.z);
                    var nodeUpperEnd = Utils.ArgMax(segment.Side1Nodes,
                        node => node.Rectangle.z + node.Rectangle.depth);
                    return (nodeLowerEnd.SegmentsDictionary()[Direction.Lower].IsConst &&
                            nodeUpperEnd.SegmentsDictionary()[Direction.Upper].IsConst);
                }
                else
                {
                    var nodeLeftEnd = Utils.ArgMin(segment.Side1Nodes, node => node.Rectangle.x);
                    var nodeRightEnd = Utils.ArgMax(segment.Side1Nodes,
                        node => node.Rectangle.x + node.Rectangle.width);
                    return (nodeLeftEnd.SegmentsDictionary()[Direction.Left].IsConst &&
                            nodeRightEnd.SegmentsDictionary()[Direction.Right].IsConst);
                }
            }

            return false;
        }

        private static void Split(IList<TNode> nodes, TSegment slicingSegment,
            out IList<TNode> partition1, out IList<TNode> partition2)
        {
            partition1 = new List<TNode>();
            partition2 = new List<TNode>();
            if (slicingSegment.IsVertical)
            {
                double xPosSegment = slicingSegment.Side2Nodes.First().Rectangle.x;
                foreach (var node in nodes)
                {
                    if (node.Rectangle.x < xPosSegment)
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
                double zPosSegment = slicingSegment.Side2Nodes.First().Rectangle.z;
                foreach (var node in nodes)
                {
                    if (node.Rectangle.z < zPosSegment)
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

        private static void AdjustSliced(IList<TNode> partition1, 
            IList<TNode> partition2,
            TSegment slicingSegment)
        {
            TRectangle rectangle1Old = Utils.CreateParentRectangle(partition1);
            TRectangle rectangle2Old = Utils.CreateParentRectangle(partition2);
            double size1 = partition1.Sum(n => n.Size);
            double size2 = partition2.Sum(n => n.Size);
            TRectangle rectangle1New = (TRectangle) rectangle1Old.Clone();
            TRectangle rectangle2New = (TRectangle) rectangle2Old.Clone();

            if (slicingSegment.IsVertical)
            {
                rectangle1New.width = size1 / rectangle1New.depth;
                rectangle2New.width = size2 / rectangle2New.depth;
                rectangle2New.x = rectangle1New.x + rectangle1New.width;
            }
            else
            {
                rectangle1New.depth = size1 / rectangle1New.width;
                rectangle2New.depth = size2 / rectangle2New.width;
                rectangle2New.z = rectangle1New.z + rectangle1New.depth;
            }
            Utils.TransformRectangles(partition1, newRectangle: rectangle1New, oldRectangle: rectangle1Old);
            Utils.TransformRectangles(partition2, newRectangle: rectangle2New, oldRectangle: rectangle2Old);
        }
        
        
        private static bool GradientDecrease(IList<TNode> nodes)
        {
            var segments = nodes.SelectMany(n => n.SegmentsDictionary().Values).ToHashSet();
            segments.RemoveWhere(s => s.IsConst);
            int i = 0;
            Dictionary<TSegment,int> mapSegmentIndex 
                = segments.ToDictionary(s => s, s => i++);

            double distance = 0;
            for(int j = 0; j < 50; j++)
            {
                distance = CalculateOneStep(nodes, mapSegmentIndex);
                if(distance <= Parameters.Precision) break;
            }
            if(distance > Parameters.Precision)
            {
                Debug.LogWarning($" layout correction > {Parameters.Precision}");
            }
            bool cons = CheckCons(nodes);
            if(!cons)
            {
                Debug.LogWarning("layout correction failed negative rec");
            }
            return (cons && distance < Parameters.Precision);
        }

        private static Matrix<double> JacobianMatrix(
            IList<TNode> nodes, 
            Dictionary<TSegment, int> mapSegmentIndex)
        {
            int n = nodes.Count;
            var matrix = Matrix<double>.Build.Sparse(n,n-1);
            foreach(var node in nodes)
            {
                var segments = node.SegmentsDictionary();
                int index_node = nodes.IndexOf(node);    
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var segment = segments[dir];
                    if(!segment.IsConst)
                    {
                        double value = 0;
                        switch(dir)
                        {
                            case Direction.Left:
                                value = - node.Rectangle.depth;
                                break;
                            case Direction.Right:
                                value = node.Rectangle.depth;
                                break;
                            case Direction.Lower:
                                value = - node.Rectangle.width;
                                break;
                            case Direction.Upper:
                                value = node.Rectangle.width;
                                break;
                        }
                        matrix[index_node,mapSegmentIndex[segment]] = value;
                    }
                }                   
            }
            return matrix;
        }

        private static double CalculateOneStep(
            IList<TNode> nodes, 
            Dictionary<TSegment, int> mapSegmentIndex)
        {
            Matrix<double> matrix = JacobianMatrix(nodes,mapSegmentIndex);
            
            Vector<double> nodes_sizes_wanted = 
                Vector<double>.Build.DenseOfArray(nodes.Select(node => (double) node.Size).ToArray());
            Vector<double> nodes_sizes_current = 
                Vector<double>.Build.DenseOfArray(nodes.Select(node => (double) node.Rectangle.Area()).ToArray());
            var diff = nodes_sizes_wanted - nodes_sizes_current;
            Matrix<double> pinv;
            try
            {
                pinv = matrix.PseudoInverse();
            }
            catch
            {
                try
                {
                    Matrix<double> bias = Matrix<double>.Build.Random(nodes.Count,mapSegmentIndex.Count, new ContinuousUniform(-.1,0.1));
                    pinv = (matrix + bias).PseudoInverse();
                    Debug.LogWarning("layout correction needs bias");
                }
                catch
                {
                    Debug.LogWarning("layout correction failed");
                    pinv = Matrix<double>.Build.Dense(mapSegmentIndex.Count,nodes.Count,0);
                }
            }

            Vector<double> segmentShift = pinv * diff;

            ApplyShift(segmentShift, nodes, mapSegmentIndex);
            
            Vector<double> nodes_sizes_afterStep = 
                Vector<double>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
            return (nodes_sizes_afterStep - nodes_sizes_wanted).Norm(2);
        }


        private static void ApplyShift(
            Vector<double> shift,
            IList<TNode> nodes,
            Dictionary<TSegment, int> mapSegmentIndex)
        {
            foreach(var node in nodes)
            {
                var segments = node.SegmentsDictionary();
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var segment = segments[dir];
                    if(segment.IsConst) continue;
                    var value = shift[mapSegmentIndex[segment]];
                    switch(dir)
                    {
                        case Direction.Left:
                            node.Rectangle.x += value;
                            node.Rectangle.width -= value;
                            break;
                        case Direction.Right:
                            node.Rectangle.width += value;
                            break;
                        case Direction.Lower:
                            node.Rectangle.z += value;
                            node.Rectangle.depth -= value;
                            break;
                        case Direction.Upper:
                            node.Rectangle.depth += value;
                            break;
                    }
                }
            }
        }

        private static bool CheckCons(IList<TNode> nodes)
        {
            foreach(var node in nodes)
            {
                if(node.Rectangle.width <= 0 || node.Rectangle.depth <= 0) return false;
            }
            return true;
        }

    }
}