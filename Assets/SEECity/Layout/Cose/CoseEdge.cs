using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CoseEdge : MonoBehaviour
    {
        /// <summary>
        /// Indicates whether the edge is an intergraph edge
        /// </summary>
        private bool isInterGraph;

        /// <summary>
        /// the length of the edge
        /// </summary>
        private double length = 0.0;

        /// <summary>
        /// the length of the edge in x direction
        /// </summary>
        private double lengthX = 0.0;

        /// <summary>
        /// the length of the edge in y direction
        /// </summary>
        private double lengthY = 0.0;

        /// <summary>
        /// Indicates whether source and target node are overlapping
        /// </summary>
        private bool isOverlappingSourceAndTarget;

        /// <summary>
        /// the lowest common ancestor of the traget and source node
        /// </summary>
        private CoseGraph lowestCommonAncestor;

        /// <summary>
        /// the lowest common anchestor of the source node
        /// </summary>
        private CoseNode sourceInLca;

        /// <summary>
        /// the lowest common anchestor of the target node
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
        private double idealEdgeLength = CoseLayoutSettings.Edge_Length;

        public bool IsInterGraph { get => isInterGraph; set => isInterGraph = value; }
        public double LengthX { get => lengthX; set => lengthX = value; }
        public double LengthY { get => lengthY; set => lengthY = value; }
        public bool IsOverlappingSourceAndTarget { get => isOverlappingSourceAndTarget; set => isOverlappingSourceAndTarget = value; }
        public CoseGraph LowestCommonAncestor { get => lowestCommonAncestor; set => lowestCommonAncestor = value; }
        public CoseNode SourceInLca { get => sourceInLca; set => sourceInLca = value; }
        public CoseNode TargetInLca { get => targetInLca; set => targetInLca = value; }
        public CoseNode Source { get => source; set => source = value; }
        public CoseNode Target { get => target; set => target = value; }
        public double IdealEdgeLength { get => idealEdgeLength; set => idealEdgeLength = value; }
        public double Length { get => length; set => length = value; }

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
        /// <returns></returns>
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
            lengthX = Target.GetCenterX() - Source.GetCenterX();
            lengthY = Target.GetCenterY() - Source.GetCenterY();

            if (Math.Abs(lengthX) < 1.0)
            {
                lengthX = Math.Sign(lengthX);
            }

            if (Math.Abs(lengthY) < 1.0)
            {
                lengthY = Math.Sign(lengthY);
            }

            Length = Math.Sqrt(lengthX * lengthX + lengthY * lengthY);
        }

        /// <summary>
        /// updates the legth of the edge (diffrent node sizes)
        /// </summary>
        public void UpdateLenght()
        {
            double[] clipPointCoordinates = new double[4];
            Tuple<bool, double[]> result = CoseGeometry.GetIntersection(Target.rect, Source.rect, clipPointCoordinates);
            IsOverlappingSourceAndTarget = result.Item1;
            clipPointCoordinates = result.Item2;

            if (!IsOverlappingSourceAndTarget)
            {
                lengthX = clipPointCoordinates[0] - clipPointCoordinates[2];
                lengthY = clipPointCoordinates[1] - clipPointCoordinates[3];

                if (Math.Abs(lengthX) < 1.0)
                {
                    LengthX = Math.Sign(LengthX);
                }

                if (Math.Abs(lengthY) < 1.0)
                {
                    LengthY = Math.Sign(LengthY);
                }

                Length = Math.Sqrt(lengthX * lengthX + lengthY * lengthY);
            }
        }
    }
}
