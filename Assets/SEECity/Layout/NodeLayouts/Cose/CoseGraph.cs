using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SEE.DataModel;

namespace SEE.Layout
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
        /// top position of the graph
        /// </summary>
        private double top;

        /// <summary>
        /// left position of the graph
        /// </summary>
        private double left;

        /// <summary>
        /// bottom position of the graph
        /// </summary>
        private double bottom;

        /// <summary>
        /// right position of the graph
        /// </summary>
        private double right;

        /// <summary>
        /// the bounding rect of this graph
        /// </summary>
        public Rect boudingRect = new Rect();

        /// <summary>
        /// the esitmated size of this graph
        /// </summary>
        private double estimatedSize = Mathf.NegativeInfinity;

        /// <summary>
        /// the margin around this graph
        /// </summary>
        private double defaultMargin = CoseLayoutSettings.Graph_Margin;

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
        public double Left { get => left; set => left = value; }
        public double Top { get => top; set => top = value; }
        public double Bottom { get => bottom; set => bottom = value; }
        public double Right { get => right; set => right = value; }
        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }
        public CoseNode Parent { get => parent; set => parent = value; }
        public List<CoseNode> Nodes { get => nodes; set => nodes = value; }
        public Rect BoudingRect { get => boudingRect; set => boudingRect = value; }
        public bool IsConnected { get => isConnected; set => isConnected = value; }
        public List<CoseEdge> Edges { get => edges; set => edges = value; }
        public double EstimatedSize { get => estimatedSize; set => estimatedSize = value; }
        public bool IsSubLayout { get => isSubLayout; set => isSubLayout = value; }

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
        /// Updates the bounds of the graph, depends of the position/ size of the nodes insite the graph
        /// </summary>
        /// <param name="recursive">Indicates if the bounds should be calculated recursively</param>
        public void UpdateBounds(bool recursive)
        {
            if (parent.SublayoutValues.IsSubLayoutNode) 
            {
                this.left = parent.GetLeft();
                this.right = parent.GetRight();
                this.top = parent.GetTop();
                this.bottom = parent.GetBottom();
                UpdateBoundingRect();

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

            double left = Mathf.Infinity;
            double right = Mathf.NegativeInfinity;
            double top = Mathf.Infinity;
            double bottom = Mathf.NegativeInfinity;
            double nodeLeft;
            double nodeRight;
            double nodeTop;
            double nodeBottom;

            foreach (CoseNode cNode in nodes)
            {

                if (recursive && cNode.Child != null)
                {
                    cNode.UpdateBounds();
                }

                nodeLeft = cNode.GetLeft();
                nodeRight = cNode.GetRight();
                nodeTop = cNode.GetTop();
                nodeBottom = cNode.GetBottom();

                if (left > nodeLeft)
                {
                    left = nodeLeft;
                }

                if (right < nodeRight)
                {
                    right = nodeRight;
                }

                if (top > nodeTop)
                {
                    top = nodeTop;
                }

                if (bottom < nodeBottom)
                {
                    bottom = nodeBottom;
                }
            }

            Rect boundingRect = new Rect((float)left, (float)top, (float)(right - left), (float)(bottom - top));

            if (left == Mathf.Infinity)
            {
                this.left = parent.GetLeft();
                this.right = parent.GetRight();
                this.top = parent.GetTop();
                this.bottom = parent.GetBottom();
            }

            if (graphManager.Layout.InnerNodesAreCircles)
            {
                var width = Math.Abs(this.right - this.left);
                var height = Math.Abs(this.bottom - this.top);

                var boundsWidth = (width / Math.Sqrt(2)) - (width / 2);
                var boundsHeight = (height / Math.Sqrt(2)) - (height / 2);

                defaultMargin = Math.Max(boundsWidth, defaultMargin);
                defaultMargin = Math.Max(boundsHeight, defaultMargin);
            }

            this.left = boundingRect.x - defaultMargin;
            this.right = boundingRect.x + boundingRect.width + defaultMargin;
            this.top = boundingRect.y - defaultMargin;
            this.bottom = boundingRect.y + boundingRect.height + defaultMargin;

            UpdateBoundingRect();
        }

        /// <summary>
        /// Adds the given displacment values to the graphs position
        /// </summary>
        /// <param name="dx">displacement x direction</param>
        /// <param name="dy">displacement y direction</param>
        public void SetXYDisplacementBoundingRect(double dx, double dy)
        {
            boudingRect.x += (float)dx;
            boudingRect.y += (float)dy;
        }

        /// <summary>
        /// updates the bounds of the boundingrectanle 
        /// </summary>
        public void UpdateBoundingRect()
        {
            this.boudingRect = new Rect((float)left, (float)top, (float)right - (float)left, (float)bottom - (float)top);
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
        /// <returns></returns>
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
        /// <returns></returns>
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
                // TODO auf level umgestellt, schauen was herauskommt
                return parent.NodeObject.Level;
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
        /// <param name="edge"></param>
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

        /*/// <summary>
        /// Calculates all nodes needed for a sublayout with this graphs parent node as the sublayouts root node
        /// </summary>
        /// <param name="onlyLeaves"></param>
        /// <returns></returns>
        public List<CoseNode> CalculateNodesForSublayout(bool onlyLeaves)
        {
            List<CoseNode> nodesForLayout = new List<CoseNode>();
            foreach (CoseNode node in nodes)
            {
                if (onlyLeaves)
                {
                    if (node.IsLeaf())
                    {
                        nodesForLayout.Add(node);
                        node.SublayoutValues.IsSubLayoutNode = true;
                    }
                }
                else
                {
                    nodesForLayout.Add(node);
                    node.SublayoutValues.IsSubLayoutNode = true;
                }
            }
        }*/
    }
}

