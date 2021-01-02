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
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.Cose
{
    public class CoseGraph
    {
        /// <summary>
        /// the parent of the graph
        /// </summary>
        private CoseNode parent;

        /// <summary>
        /// the graphmanager of the current CoseLayout
        /// </summary>
        private CoseGraphManager graphManager;

        /// <summary>
        /// the nodes contained by this graph
        /// </summary>
        private List<CoseNode> nodes = new List<CoseNode>();

        /// <summary>
        /// edges of this graph
        /// </summary>
        private List<CoseEdge> edges = new List<CoseEdge>();

        /// <summary>
        /// the left front corner of the graph
        /// </summary>
        private Vector2 leftFrontCorner;

        /// <summary>
        /// the right back corner of this graph
        /// </summary>
        private Vector2 rightBackCorner;

        /// <summary>
        /// the scale of this graph
        /// </summary>
        private Vector3 scale;

        /// the centerPosition of this graph
        private Vector3 centerPosition;

        /// <summary>
        /// the extend of this graph
        /// </summary>
        public Vector3 Extend
        {
            get => scale / 2;
        }

        /// <summary>
        /// the estimated size of this graph
        /// </summary>
        private double estimatedSize = Mathf.NegativeInfinity;

        /// <summary>
        /// the margin around this graph
        /// </summary>
        private readonly float defaultMargin = CoseLayoutSettings.Graph_Margin;

        /// <summary>
        /// Indicates if the graph is connected
        /// </summary>
        private bool isConnected = false;

        /// <summary>
        /// the original node
        /// </summary>
        private ILayoutNode graphObject;

        /// <summary>
        /// Indicates if this graph is a sublayout
        /// </summary>
        private bool isSubLayout = false;

        public ILayoutNode GraphObject { get => graphObject; set => graphObject = value; }
        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }
        public CoseNode Parent { get => parent; set => parent = value; }
        public List<CoseNode> Nodes { get => nodes; set => nodes = value; }
        public bool IsConnected { get => isConnected; set => isConnected = value; }
        public List<CoseEdge> Edges { get => edges; set => edges = value; }
        public double EstimatedSize { get => estimatedSize; set => estimatedSize = value; }
        public bool IsSubLayout { get => isSubLayout; set => isSubLayout = value; }
        public Vector3 Scale { get => scale; set => scale = value; }
        public Vector3 CenterPosition { get => centerPosition; set => centerPosition = value; }
        public Vector2 LeftFrontCorner { get => leftFrontCorner; set => leftFrontCorner = value; }
        public Vector2 RightBackCorner { get => rightBackCorner; set => rightBackCorner = value; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="parent">the parent node</param>
        /// <param name="graphManager">the graphmanager for this graph</param>
        public CoseGraph(CoseNode parent, CoseGraphManager graphManager)
        {
            this.parent = parent;
            this.graphManager = graphManager;
        }

        /// <summary>
        /// update the bounds of this graph
        /// </summary>
        /// <param name="recursive">if true the update of the bounds is performed recursively</param>
        public void UpdateBounds(bool recursive)
        {
            if (parent.SublayoutValues.IsSubLayoutNode)
            {
                LeftFrontCorner = parent.GetLeftFrontCorner();
                RightBackCorner = parent.GetRightBackCorner();

                UpdateBounding();

                foreach (CoseNode cNode in nodes)
                {
                    if (recursive && cNode.Child != null)
                    {
                        cNode.UpdateBounds();
                    }
                    else
                    {
                        cNode.SetOrigin();
                    }
                }
                return;
            }

            Vector2 leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.NegativeInfinity);
            Vector2 rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.Infinity);

            Vector2 leftLowerCornerNode;
            Vector2 rightUpperCornerNode;

            foreach (CoseNode cNode in nodes)
            {

                if (recursive && cNode.Child != null)
                {
                    cNode.UpdateBounds();
                }

                leftLowerCornerNode = cNode.GetLeftFrontCorner();
                rightUpperCornerNode = cNode.GetRightBackCorner();

                if (leftLowerCorner.x > leftLowerCornerNode.x)
                {
                    leftLowerCorner.x = leftLowerCornerNode.x;
                }

                if (rightUpperCorner.x < rightUpperCornerNode.x)
                {
                    rightUpperCorner.x = rightUpperCornerNode.x;
                }

                if (rightUpperCorner.y > rightUpperCornerNode.y)
                {
                    rightUpperCorner.y = rightUpperCornerNode.y;
                }

                if (leftLowerCorner.y < leftLowerCornerNode.y)
                {
                    leftLowerCorner.y = leftLowerCornerNode.y;
                }
            }

            if (leftLowerCorner.x == Mathf.Infinity)
            {
                leftFrontCorner = parent.GetLeftFrontCorner();
                rightBackCorner = parent.GetRightBackCorner();
            }

            leftFrontCorner.x = leftLowerCorner.x - defaultMargin;
            leftFrontCorner.y = leftLowerCorner.y + defaultMargin;

            rightBackCorner.x = rightUpperCorner.x + defaultMargin;
            rightBackCorner.y = rightUpperCorner.y - defaultMargin;

            UpdateBounding();
        }

        /// <summary>
        /// Adds the given displacment values to the graphs position
        /// </summary>
        /// <param name="dx">displacement x direction</param>
        /// <param name="dz">displacement z direction</param>
        public void SetXZDisplacementBoundingRect(float dx, float dz)
        {
            centerPosition.x += dx;
            centerPosition.z += dz;
        }

        /// <summary>
        /// updates the bounds of the boundingrectanle 
        /// </summary>
        public void UpdateBounding()
        {
            scale.x = RightBackCorner.x - LeftFrontCorner.x;
            scale.z = LeftFrontCorner.y - RightBackCorner.y;
            centerPosition.x = LeftFrontCorner.x + Extend.x;
            centerPosition.z = RightBackCorner.y + Extend.z;
        }

        /// <summary>
        /// Updates and calculates if the graph is connected
        /// </summary>
        public void UpdateConnected()
        {
            if (nodes.Count == 0)
            {
                isConnected = true;
                return;
            }

            HashSet<CoseNode> visited = new HashSet<CoseNode>();

            CoseNode current = nodes[0];

            List<CoseEdge> neighborEdges;
            CoseNode currentNeighbor;

            LinkedList<CoseNode> toBeVisited = new LinkedList<CoseNode>(current.WithChildren());

            while (toBeVisited.Count != 0)
            {
                current = toBeVisited.First.Value;
                toBeVisited.RemoveFirst();
                visited.Add(current);

                neighborEdges = current.Edges;

                foreach (CoseEdge edge in neighborEdges)
                {
                    currentNeighbor = edge.GetOtherEndInGraph(current, this);

                    if (currentNeighbor != null && !visited.Contains(currentNeighbor))
                    {
                        foreach (CoseNode toAdd in currentNeighbor.WithChildren())
                        {
                            toBeVisited.AddLast(toAdd);
                        }
                    }
                }
            }

            isConnected = false;

            if (visited.Count >= nodes.Count)
            {
                int noOfVisitedInThisGraph = 0;

                foreach (CoseNode visitedNode in visited)
                {
                    if (visitedNode.Owner == this)
                    {
                        noOfVisitedInThisGraph++;
                    }
                }

                if (noOfVisitedInThisGraph == nodes.Count)
                {
                    isConnected = true;
                }
            }
        }

        /// <summary>
        /// Adds an edge to this graph
        /// </summary>
        /// <param name="newEdge">the new edge</param>
        /// <param name="sourceNode">the source node</param>
        /// <param name="targetNode">the target node</param>
        /// <returns>the new edge</returns>
        public CoseEdge Add(CoseEdge newEdge, CoseNode sourceNode, CoseNode targetNode)
        {
            if (!nodes.Contains(sourceNode) || !nodes.Contains(targetNode))
            {
                throw new System.Exception("Source or Traget not in Graph");
            }

            if (sourceNode.Owner != targetNode.Owner || sourceNode.Owner != this)
            {
                throw new System.Exception("Both owners must be in this graph");
            }

            newEdge.Source = sourceNode;
            newEdge.Target = targetNode;
            newEdge.IsInterGraph = false;
            edges.Add(newEdge);
            sourceNode.Edges.Add(newEdge);

            if (targetNode != sourceNode)
            {
                targetNode.Edges.Add(newEdge);
            }
            return newEdge;
        }

        /// <summary>
        /// Adds a new node to this graph
        /// </summary>
        /// <param name="node">the new node</param>
        public void AddNode(CoseNode node)
        {
            if (graphManager == null)
            {
                throw new System.Exception("Graph has no graph manager");
            }
            if (nodes.Contains(node))
            {
                throw new System.Exception("Node is already in graph");
            }

            node.Owner = this;
            nodes.Add(node);
        }

        /// <summary>
        /// calculates the estimated size of this graph
        /// </summary>
        /// <returns>the estimated size</returns>
        public double CalcEstimatedSize()
        {
            double size = 0;

            foreach (CoseNode node in nodes)
            {
                size += node.CalcEstimatedSize();
            }

            if (size == 0)
            {
                estimatedSize = CoseLayoutSettings.Empty_Compound_Size;
            }
            else
            {
                estimatedSize = size / Math.Sqrt(nodes.Count);
            }

            return estimatedSize;
        }

        /// <summary>
        /// Calculates the inclusion tree depth of this graph
        /// </summary>
        /// <returns>the inclusion tree depth</returns>
        public int GetInclusionTreeDepth()
        {
            if (this == graphManager.RootGraph)
            {
                return 1;
            }
            else
            {
                return parent.InclusionTreeDepth;
            }
        }

        /// <summary>
        /// Removes a given node from this graph
        /// </summary>
        /// <param name="node">the node to remove</param>
        public void Remove(CoseNode node)
        {
            if (node == null)
            {
                throw new System.Exception("node is null");
            }
            if (node.Owner == null || node.Owner != this)
            {
                throw new System.Exception("owner graph is invalid");
            }
            if (graphManager == null)
            {
                throw new System.Exception("Owner graph manager is invalid");
            }

            List<CoseEdge> edgesToBeRemoved = new List<CoseEdge>();
            edgesToBeRemoved.AddRange(node.Edges);

            foreach (CoseEdge edge in edgesToBeRemoved)
            {
                if (edge.IsInterGraph)
                {
                    graphManager.Remove(edge);
                }
                else
                {
                    edge.Source.Owner.Remove(edge);
                }
            }

            if (!nodes.Contains(node))
            {
                throw new System.Exception("node is not in owner node list");
            }
            nodes.Remove(node);
        }

        /// <summary>
        /// Removes a given edge from this graph
        /// </summary>
        /// <param name="edge">the edge to remove from this graph</param>
        public void Remove(CoseEdge edge)
        {
            if (edge == null)
            {
                throw new System.Exception("Edge is null");
            }
            if (edge.Source == null || edge.Target == null)
            {
                throw new System.Exception("source or target is null");
            }
            if (edge.Source.Owner == null || edge.Target.Owner == null)
            {
                throw new System.Exception("source or target owner is null");
            }
            if (!edge.Source.Edges.Contains(edge) || !edge.Target.Edges.Contains(edge))
            {
                throw new System.Exception("source or target doesnt know this edge");
            }

            edge.Source.Edges.Remove(edge);

            if (edge.Target != edge.Source)
            {
                edge.Target.Edges.Remove(edge);
            }

            if (!edge.Source.Owner.Edges.Contains(edge))
            {
                throw new System.Exception("not in owner graph managers edge list");
            }

            edge.Source.Owner.Edges.Remove(edge);
        }
    }
}

