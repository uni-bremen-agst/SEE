// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.Cose
{
    public class CoseEdge
    {
        /// <summary>
        /// Indicates whether the edge is an intergraph edge
        /// </summary>
        private bool isInterGraph;

        /// <summary>
        /// the length of the edge
        /// </summary>
        private float length = 0.0f;

        /// <summary>
        /// the length of the edge in x direction
        /// </summary>
        private float lengthX = 0.0f;

        /// <summary>
        /// the length of the edge in y direction
        /// </summary>
        private float lengthY = 0.0f;

        /// <summary>
        /// Indicates whether source and target node are overlapping
        /// </summary>
        private bool isOverlappingSourceAndTarget;

        /// <summary>
        /// the lowest common ancestor of the traget and source node
        /// </summary>
        private CoseGraph lowestCommonAncestor;

        /// <summary>
        /// the lowest common ancestor of the source node
        /// </summary>
        private CoseNode sourceInLca;

        /// <summary>
        /// the lowest common ancestor of the target node
        /// </summary>
        private CoseNode targetInLca;

        /// <summary>
        /// The source node
        /// </summary>
        private CoseNode source;

        /// <summary>
        /// the target Node
        /// </summary>
        private CoseNode target;

        /// <summary>
        /// the ideal edge length of the edge
        /// </summary>
        private float idealEdgeLength = CoseLayoutSettings.Edge_Length;

        public bool IsInterGraph { get => isInterGraph; set => isInterGraph = value; }
        public float LengthX { get => lengthX; set => lengthX = value; }
        public float LengthY { get => lengthY; set => lengthY = value; }
        public bool IsOverlappingSourceAndTarget { get => isOverlappingSourceAndTarget; set => isOverlappingSourceAndTarget = value; }
        public CoseGraph LowestCommonAncestor { get => lowestCommonAncestor; set => lowestCommonAncestor = value; }
        public CoseNode SourceInLca { get => sourceInLca; set => sourceInLca = value; }
        public CoseNode TargetInLca { get => targetInLca; set => targetInLca = value; }
        public CoseNode Source { get => source; set => source = value; }
        public CoseNode Target { get => target; set => target = value; }
        public float IdealEdgeLength { get => idealEdgeLength; set => idealEdgeLength = value; }
        public float Length { get => length; set => length = value; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">the source node of the edge</param>
        /// <param name="target">the target node of the edge</param>
        public CoseEdge(CoseNode source, CoseNode target)
        {
            this.source = source;
            this.target = target;
        }

        /// <summary>
        /// Returns the other end of the edge if in this graph
        /// </summary>
        /// <param name="node">the node on the one end</param>
        /// <param name="graph">the corresponding graph</param>
        /// <returns></returns>
        public CoseNode GetOtherEndInGraph(CoseNode node, CoseGraph graph)
        {
            CoseNode otherEnd = GetOtherEnd(node);
            CoseGraph root = graph.GraphManager.RootGraph;

            while (true)
            {
                if (otherEnd.Owner == graph)
                {
                    return otherEnd;
                }

                if (otherEnd.Owner == root)
                {
                    break;
                }

                otherEnd = otherEnd.Owner.Parent;
            }
            return null;
        }

        /// <summary>
        /// Returns the other end node of the edge 
        /// </summary>
        /// <param name="node"></param>
        /// <returns>the other end node of this edge</returns>
        public CoseNode GetOtherEnd(CoseNode node)
        {
            if (node.Equals(source))
            {
                return target;
            }
            else if (node.Equals(target))
            {
                return source;
            }
            else
            {
                throw new System.Exception("Node is not incident");
            }
        }

        /// <summary>
        /// Calculates and sets the current length of the edge if all nodes have the same size
        /// </summary>
        public void UpdateLengthSimple()
        {
            lengthX = target.GetCenterX() - source.GetCenterX();
            lengthY = target.GetCenterY() - source.GetCenterY();

            if (Mathf.Abs(lengthX) < 1.0)
            {
                lengthX = CoseHelper.Sign(lengthX);
            }

            if (Mathf.Abs(lengthY) < 1.0)
            {
                lengthY = CoseHelper.Sign(lengthY);
            }

            length = Mathf.Sqrt(lengthX * lengthX + lengthY * lengthY);
        }

        /// <summary>
        /// updates the legth of the edge (different node sizes)
        /// </summary>
        public void UpdateLenght()
        {
            double[] clipPointCoordinates = new double[4];
            Tuple<bool, double[]> result = CoseGeometry.GetIntersection(CoseHelper.NewRect(target.Scale, target.CenterPosition), CoseHelper.NewRect(source.Scale, source.CenterPosition), clipPointCoordinates);
            isOverlappingSourceAndTarget = result.Item1;
            clipPointCoordinates = result.Item2;

            if (!isOverlappingSourceAndTarget)
            {
                lengthX = (float)clipPointCoordinates[0] - (float)clipPointCoordinates[2];
                lengthY = (float)clipPointCoordinates[1] - (float)clipPointCoordinates[3];

                if (Mathf.Abs(lengthX) < 1.0)
                {
                    lengthX = CoseHelper.Sign(lengthX);
                }

                if (Mathf.Abs(lengthY) < 1.0)
                {
                    lengthY = CoseHelper.Sign(lengthY);
                }

                length = Mathf.Sqrt(lengthX * lengthX + lengthY * lengthY);
            }
        }
    }
}
