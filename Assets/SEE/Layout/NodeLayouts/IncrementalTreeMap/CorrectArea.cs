using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using MathNet.Numerics.LinearAlgebra;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    static class CorrectAreas
    {
        public static void Correct(IList<TNode> nodes)
        {
            HashSet<TSegment> segments = new HashSet<TSegment>();
            foreach(var node in nodes)
            {
                segments.UnionWith(node.getAllSegments().Values);
            }
            segments.RemoveWhere(s => s.IsConst);
            int i = 0;
            Dictionary<TSegment,int> mapSegmentIndex 
                = segments.ToDictionary(s => s, s => i++);


            for(int j = 0; j < 10; j++)
            {
                var distance = CalculateOneStep(nodes, mapSegmentIndex);
                //TODO
                Debug.Log("Distance["+j.ToString()+"] : " + distance.ToString());
            }
        }

        private static Matrix<float> JacobianMatrix(
            IList<TNode> nodes, 
            Dictionary<TSegment, int> mapSegmentIndex)
        {
            int n = nodes.Count;
            var matrix = Matrix<float>.Build.Dense(n,n-1);
            foreach(var node in nodes)
            {
                var segments = node.getAllSegments();
                int index_node = nodes.IndexOf(node);    
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var segment = segments[dir];
                    if(!segment.IsConst)
                    {
                        int index_segment = mapSegmentIndex[segment];
                        float value;
                        if(dir == Direction.Left || dir == Direction.Right)
                        {
                            value = node.Rectangle.depth;
                        }
                        else
                        {
                            value = node.Rectangle.width;
                        }
                        if(dir == Direction.Left || dir == Direction.Lower)
                        {
                            value *= -1;
                        }
                        matrix[index_node,index_segment] = value;
                    }
                }                   
            }
            return matrix;
        }

        private static double CalculateOneStep(
            IList<TNode> nodes, 
            Dictionary<TSegment, int> mapSegmentIndex)
        {
            Matrix<float> matrix = JacobianMatrix(nodes,mapSegmentIndex);
            Vector<float> nodes_sizes_wanted = 
                Vector<float>.Build.DenseOfArray(nodes.Select(node => node.Size).ToArray());
            Vector<float> segmentShift = matrix.PseudoInverse() * nodes_sizes_wanted;
            applyShift(segmentShift, nodes, mapSegmentIndex);
            Vector<float> nodes_sizes_afterStep = 
                Vector<float>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.area()).ToArray());

            return (nodes_sizes_afterStep - nodes_sizes_wanted).Norm(2);
        }


        private static void applyShift(
            Vector<float> shift,
            IList<TNode> nodes,
            Dictionary<TSegment, int> mapSegmentIndex)
        {
            foreach(var node in nodes)
            {
                var segments = node.getAllSegments();
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var segment = segments[dir];
                    if(segment.IsConst) continue;
                    var value = shift[mapSegmentIndex[segment]];
                    if(dir == Direction.Left) node.Rectangle.x += value;
                    if(dir == Direction.Right) node.Rectangle.width += value;
                    if(dir == Direction.Lower) node.Rectangle.z += value;
                    if(dir == Direction.Upper) node.Rectangle.depth += value;
                }
            }
        }
    }
}