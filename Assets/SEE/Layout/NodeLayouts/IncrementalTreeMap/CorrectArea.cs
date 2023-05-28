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
            HashSet<TSegment> segments = new HashSet<TSegment>();
            foreach(var node in nodes)
            {
                segments.UnionWith(node.getAllSegments().Values);
            }
            segments.RemoveWhere(s => s.IsConst);
            int i = 0;
            Dictionary<TSegment,int> mapSegmentIndex 
                = segments.ToDictionary(s => s, s => i++);

            double distance = 0;
            for(int j = 0; j < 50; j++)
            {
                distance = CalculateOneStep(nodes, mapSegmentIndex);
                if(distance <= 0.00001) break;
            }
            if(distance <= 0.00001)
            {
                Debug.LogWarning("++++ layout correction .00001");
            }
            else if(distance <= 0.0001)
            {
                Debug.LogWarning("-+++ layout correction .0001");
            }
            else if(distance <= 0.001)
            {
                Debug.LogWarning("--++ layout correction .001");
            }
            else if(distance <= 0.01)
            {
                Debug.LogWarning("---+ layout correction .01");
            }
            else if(distance <= 0.1)
            {
                Debug.LogWarning("---- layout correction .1");
            }
            else
            {
                Debug.LogWarning("<<>> layout correction >.1");
            }
            bool cons = CheckCons(nodes);
            if(!cons)
            {
                Debug.LogWarning("layout correction failed negative rec");
            }
            //Debug.LogWarning("layout correction bad result ["+distance.ToString()+"]");
            return (cons && distance <= 0.001);
        }

        private static Matrix<float> JacobianMatrix(
            IList<TNode> nodes, 
            Dictionary<TSegment, int> mapSegmentIndex)
        {
            int n = nodes.Count;
            var matrix = Matrix<float>.Build.Sparse(n,n-1);
            foreach(var node in nodes)
            {
                var segments = node.getAllSegments();
                int index_node = nodes.IndexOf(node);    
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var segment = segments[dir];
                    if(!segment.IsConst)
                    {
                        float value = 0;
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
            Matrix<float> matrix = JacobianMatrix(nodes,mapSegmentIndex);
            
            Vector<float> nodes_sizes_wanted = 
                Vector<float>.Build.DenseOfArray(nodes.Select(node => node.Size).ToArray());
            Vector<float> nodes_sizes_current = 
                Vector<float>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
            var diff = nodes_sizes_wanted - nodes_sizes_current;
            Matrix<float> pinv;
            try
            {
                pinv = matrix.PseudoInverse();
            }
            catch
            {
                try
                {
                    Matrix<float> bias = Matrix<float>.Build.Random(nodes.Count,mapSegmentIndex.Count, new ContinuousUniform(-.1,0.1));
                    pinv = (matrix + bias).PseudoInverse();
                    Debug.LogWarning("layout correction needs bias");
                }
                catch
                {
                    Debug.LogWarning("layout correction failed");
                    pinv = Matrix<float>.Build.Dense(mapSegmentIndex.Count,nodes.Count,0);
                }
            }

            Vector<float> segmentShift = pinv * diff;

            ApplyShift(segmentShift, nodes, mapSegmentIndex);
            
            Vector<float> nodes_sizes_afterStep = 
                Vector<float>.Build.DenseOfArray(nodes.Select(node => node.Rectangle.Area()).ToArray());
            return (nodes_sizes_afterStep - nodes_sizes_wanted).Norm(2);
        }


        private static void ApplyShift(
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