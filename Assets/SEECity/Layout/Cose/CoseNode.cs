using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System;

namespace SEE.Layout
{
    public class CoseNode
    {
        /// <summary>
        /// The GraphManager of the current CoseLayout
        /// </summary>
        private CoseGraphManager graphManager;

        /// <summary>
        /// The original node
        /// </summary>
        private Node nodeObject;

        /// <summary>
        /// TODO
        /// </summary>
        private bool isConnected = true;

        /// <summary>
        /// The child graph/ graph neasted inside the node
        /// </summary>
        private CoseGraph child;

        /// <summary>
        /// The graph to with the node belongs to 
        /// </summary>
        private CoseGraph owner;

        /// <summary>
        /// The incoming/ outgoing edges 
        /// </summary>
        private List<CoseEdge> edges = new List<CoseEdge>();

        /// <summary>
        /// the bounding rect 
        /// </summary>
        public Rect rect = new Rect(0, 0, 0, 0);

        /// <summary>
        /// TODO
        /// </summary>
        private CoseNodeSublayoutValues sublayoutValues = new CoseNodeSublayoutValues();
   
        /// <summary>
        /// The estimated size of the node
        /// </summary>
        private float estimatedSize = Int32.MinValue;

        /// <summary>
        /// TODO
        /// </summary>
        private CoseNodeLayoutValues layoutValues = new CoseNodeLayoutValues();

        /// <summary>
        /// A list of nodes that surround the node
        /// </summary>
        private List<CoseNode> surrounding = new List<CoseNode>();

        /// <summary>
        /// the number of children
        /// </summary>
        private int noOfChildren;

       /* public int NoOfChildren { get => noOfChildren; set => noOfChildren = value; }
        public List<CoseNode> Surrounding { get => surrounding; set => surrounding = value; }
        public CoseNodeLayoutValues LayoutValues { get => layoutValues; set => layoutValues = value; }
        public float EstimatedSize { get => estimatedSize; set => estimatedSize = value; }
        public CoseNodeSublayoutValues SublayoutValues { get => subLayoutValues; set => subLayoutValues = value; }
        public List<CoseEdge> Edges { get => edges; set => edges = value; }
        public CoseGraph Owner { get => owner; set => owner = value; }
        public CoseGraph Child { get => child; set => child = value; }
        public bool IsConnected { get => isConnected; set => isConnected = value; }
        public Node NodeObject { get => nodeObject; set => nodeObject = value; }
        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }*/

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node">the original node</param>
        /// <param name="graphManager">the graphmanager</param>
        public CoseNode(Node node, CoseGraphManager graphManager)
        {
            nodeObject = node;
            this.graphManager = graphManager;

            if (graphManager != null && graphManager.Layout.Sublayouts.Count != 0 && node != null && graphManager.Layout.Sublayouts.ContainsKey(node.LinkName))
            {
                sublayoutValues.IsSubLayoutRoot = true;
                sublayoutValues.NodeLayout = graphManager.Layout.Sublayouts[node.LinkName];
            }
        }

        /// <summary>
        /// calculates if this node overlaps with the given node
        /// </summary>
        /// <param name="nodeB">the second node</param>
        /// <param name="overlapAmount">the amount of how much is nodes overlap</param>
        /// <returns></returns>
        public bool CalcOverlap(CoseNode nodeB, double[] overlapAmount)
        {
            Rect rectA = rect;
            Rect rectB = nodeB.rect;

            if (rectA.Overlaps(rectB))
            {
                CoseGeometry.CalcSeparationAmount(rectA, rectB, overlapAmount, CoseLayoutSettings.Edge_Length / 2.0);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// calculates the amout of intersection
        /// </summary>
        /// <param name="nodeB">the second node</param>
        /// <param name="clipPoints">the clip points of the nodes</param>
        /// <returns></returns>
        public double[] CalcIntersection(CoseNode nodeB, double[] clipPoints)
        {
            Tuple<bool, double[]> result = CoseGeometry.GetIntersection(rect, nodeB.rect, clipPoints);
            return result.Item2;
        }

        /// <summary>
        /// Returns a list with this nodes and its children nodes
        /// </summary>
        /// <param name="withOwner"></param>
        /// <returns></returns>
        public List<CoseNode> WithChildren(bool withOwner = true)
        {
            List<CoseNode> withNeighbors = new List<CoseNode>();

            if (withOwner)
            {
                withNeighbors.Add(this);
            }

            if (child != null)
            {
                foreach (CoseNode childN in child.Nodes)
                {
                    withNeighbors.AddRange(childN.WithChildren());
                }
            }

            return withNeighbors;
        }

        /// <summary>
        /// Moves the node according to the forces 
        /// </summary>
        public void Move()
        {
            CoseLayout layout = graphManager.Layout;
            double maxNodeDisplacement = layout.CoseLayoutSettings.CoolingFactor * layout.CoseLayoutSettings.MaxNodeDisplacement;

            layoutValues.DisplacementX = layout.CoseLayoutSettings.CoolingFactor * (layoutValues.SpringForceX + layoutValues.RepulsionForceX + layoutValues.GravitationForceX) / NumberOfChildren();
            layoutValues.DisplacementY = layout.CoseLayoutSettings.CoolingFactor * (layoutValues.SpringForceY + layoutValues.RepulsionForceY + layoutValues.GravitationForceY) / NumberOfChildren();


            if (Math.Abs(layoutValues.DisplacementX) > maxNodeDisplacement)
            {
                layoutValues.DisplacementX = maxNodeDisplacement * Math.Sign(layoutValues.DisplacementX);
            }

            if (Math.Abs(layoutValues.DisplacementY) > maxNodeDisplacement)
            {
                layoutValues.DisplacementY = maxNodeDisplacement * Math.Sign(layoutValues.DisplacementY);
            }

            if (child == null && !sublayoutValues.IsSubLayoutNode) // TODO here maybe
            {
                MoveBy(layoutValues.DisplacementX, layoutValues.DisplacementY);
            }
            else if (child.Nodes.Count == 0 && !sublayoutValues.IsSubLayoutNode) // todo here maybe 
            {
                MoveBy(layoutValues.DisplacementX, layoutValues.DisplacementY);
            }
            else
            {
                if (!sublayoutValues.IsSubLayoutNode && !sublayoutValues.IsSubLayoutRoot)
                {
                    PropogateDisplacementToChildren(layoutValues.DisplacementX, layoutValues.DisplacementY);
                }
                else
                {
                    if (sublayoutValues.IsSubLayoutRoot)
                    {
                        MoveBy(layoutValues.DisplacementX, layoutValues.DisplacementY);
                    }
                }

            }

            layout.CoseLayoutSettings.TotalDisplacement += Math.Abs(LayoutValues.DisplacementX) + Math.Abs(LayoutValues.DisplacementY);
        }

        /// <summary>
        /// Changes to Source/ Target node of the intergraph edges from all child nodes of the sublayout root to sublayout root
        /// </summary>
        public void SetIntergraphEdgesToSublayoutRoot()
        {
            //List<CoseEdge> edges = new List<CoseEdge>();
            List<CoseEdge> allIntergraphEdges = new List<CoseEdge>();
            allIntergraphEdges.AddRange(GraphManager.Edges);

            // here nur sublayout nodes für das jeweilige root 
            List<CoseNode> nodes = SublayoutValues.SublayoutNodes;

            foreach (CoseNode node in nodes)
            {
                if (node != this)
                {
                    foreach (CoseEdge edge in allIntergraphEdges)
                    {
                        // wenn es eine intergraphedge ist, welche aus dem sublayout-graph herausgeht 
                        if (!nodes.Contains(edge.Source) || !nodes.Contains(edge.Target))
                        {
                            if (edge.Source == node)
                            {
                                if (edge.Target != node)
                                {
                                    edge.Source.Edges.Remove(edge);
                                    edge.Source = this;
                                    this.Edges.Add(edge);
                                }
                                else
                                {
                                    allIntergraphEdges.Remove(edge);
                                }

                            }
                            if (edge.Target == node)
                            {
                                if (edge.Source != node)
                                {
                                    edge.Target.Edges.Remove(edge);
                                    edge.Target = this;
                                    this.Edges.Add(edge);
                                }
                                else
                                {
                                    allIntergraphEdges.Remove(edge);
                                }

                            }
                        }
                    }
                }
            }
            GraphManager.AllEdges = null;
        }

        /// <summary>
        /// Propogates the displacement of the sublayout root to the sublayout children
        /// </summary>
        /// <param name="dx">the displacement of the x direction</param>
        /// <param name="dy">the displacement of the y direction</param>
        public void PropogateDisplacementToSublayoutChildren(double dx, double dy)
        {
            MoveBy(dx, dy);

            if (Child != null)
            {
                Child.SetXYDisplacementBoundingRect(dx, dy);

                foreach (CoseNode node in Child.Nodes)
                {
                    node.PropogateDisplacementToSublayoutChildren(dx, dy);
                }
            }
        }

        /// <summary>
        /// Propgates the displacement of this node to its children
        /// </summary>
        /// <param name="dx">the displacement of the x direction</param>
        /// <param name="dy">the displacement of the y direction</param>
        public void PropogateDisplacementToChildren(double dx, double dy)
        {
            foreach (CoseNode node in Child.Nodes)
            {
                if (node.Child == null)
                {
                    node.MoveBy(dx, dy);
                    node.LayoutValues.DisplacementX += dx;
                    node.LayoutValues.DisplacementY += dy;
                }
                else
                {
                    node.PropogateDisplacementToChildren(dx, dy);
                }
            }
        }

        /// <summary>
        /// Changes the position of the nodes according to the given displacement values
        /// </summary>
        /// <param name="dx">the displacement of the x direction</param>
        /// <param name="dy">the displacement of the y direction</param>
        public void MoveBy(double dx, double dy)
        {
            rect.x += (float)dx;
            rect.y += (float)dy;
        }

        /// <summary>
        /// Resets the forces that act on this node
        /// </summary>
        public void Reset()
        {
            LayoutValues.SpringForceX = 0;
            LayoutValues.SpringForceY = 0;
            LayoutValues.RepulsionForceX = 0;
            LayoutValues.RepulsionForceY = 0;
            LayoutValues.GravitationForceX = 0;
            LayoutValues.GravitationForceY = 0;
            LayoutValues.DisplacementX = 0;
            LayoutValues.DisplacementY = 0;
        }

        /// <summary>
        /// Positions the node relative to the given node
        /// </summary>
        /// <param name="origin">the node</param>
        public void SetPositionRelativ(CoseNode origin)
        {
            SublayoutValues.relativeRect.x -= origin.rect.x;
            SublayoutValues.relativeRect.y -= origin.rect.y;
            SublayoutValues.SubLayoutRoot = origin;
        }

        /// <summary>
        /// Sets the position independent to its sublayout root
        /// </summary>
        public void SetOrigin()
        {
            rect.x = SublayoutValues.relativeRect.x + SublayoutValues.SubLayoutRoot.rect.x;
            rect.y = SublayoutValues.relativeRect.y + SublayoutValues.SubLayoutRoot.rect.y;
            SetWidth(SublayoutValues.relativeRect.width);
            SetHeight(SublayoutValues.relativeRect.height);
        }

        /// <summary>
        /// Set the node to the given position and scale
        /// </summary>
        /// <param name="position"> the new position</param>
        /// <param name="scale">the new scale</param>
        public void SetPositionScale(Vector3 position, Vector3 scale)
        {
            SetWidth(scale.x);
            SetHeight(scale.z);

            float left = position.x - (scale.x / 2);
            float right = position.x + (scale.x / 2);
            float top = position.z - (scale.z / 2);
            float bottom = position.z + (scale.z / 2);

            SublayoutValues.UpdateRelativeRect(left, right, top, bottom);
            UpdateRect(left, right, top, bottom);

            if (Child != null)
            {
                Child.Left = left;
                Child.Top = top;
                Child.Right = right;
                Child.Bottom = bottom;
                Child.UpdateBoundingRect();
            }
        }

        /// <summary>
        /// Return wheather the node is a leaf node
        /// </summary>
        /// <returns></returns>
        public bool IsLeaf()
        {
            return Child == null || Child.Nodes.Count == 0;
        }

        /// <summary>
        /// Calculates the number of children
        /// </summary>
        /// <returns></returns>
        public int NumberOfChildren()
        {
            int noOfChildren = 0;

            if (Child == null)
            {
                noOfChildren = 1;
            }
            else
            {
                foreach (CoseNode child in Child.Nodes)
                {
                    noOfChildren += child.NumberOfChildren();
                }
            }

            if (noOfChildren == 0)
            {
                noOfChildren = 1;
            }

            return noOfChildren;
        }


        /// <summary>
        /// Sets the node to the given Location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetLocation(float x, float y)
        {
            rect.x = x;
            rect.y = y;
        }

        /// <summary>
        /// Updates the bounds of this node according to the bounds of its neasted graph
        /// </summary>
        public void UpdateBounds()
        {
            if (Child == null)
            {
                throw new System.Exception("Child Graph is null");
            }

            if (SublayoutValues.IsSubLayoutRoot)
            {
                SetWidth(SublayoutValues.relativeRect.width);
                SetHeight(SublayoutValues.relativeRect.height);
                Child.UpdateBounds(true);
                // rect.x/ rect.y müssen nicht gesetzt werden, da der Knoten seine Größe kennt und nicht abhängig von Child knoten ist
                return;
            }

            if (SublayoutValues.IsSubLayoutNode) // durch das vorherige return werden die root knoten ausgeschlossen
            {
                // diese knoten haben relativ positionen zu subLayoutRoot, d.h. diese müssen wieder auf origin gesetzt werden, 
                // damit das von anderen richtig berechnet werden kann

                // set position to origin by using the relativ values 

                rect.x = SublayoutValues.relativeRect.x + SublayoutValues.SubLayoutRoot.rect.x;
                rect.y = SublayoutValues.relativeRect.y + SublayoutValues.SubLayoutRoot.rect.y;
                SetWidth(SublayoutValues.relativeRect.width);
                SetHeight(SublayoutValues.relativeRect.height);
                Child.UpdateBounds(true);
                return;
            }

            if (Child.Nodes.Count != 0)
            {
                Child.UpdateBounds(true);
                rect.x = Child.Left - CoseLayoutSettings.Compound_Node_Margin;
                rect.y = Child.Top - CoseLayoutSettings.Compound_Node_Margin;

                // float width = childGraph.Right - childGraph.Left / Mathf.Sqrt(2);
                // float height = childGraph.Bottom - childGraph.Top / Mathf.Sqrt(2);

                //float diffWidth = Mathf.Abs(width - rect.width);
                // float diffHeight = Mathf.Abs(height - rect.height);

                // Here add Labelheight etc. 
                if (GraphManager.Layout.InnerNodesAreCircles)
                {
                    var width = Math.Abs(Child.Right - Child.Left);
                    var height = Math.Abs(Child.Bottom - Child.Top);

                    var boundsWidth = (float)((width / Math.Sqrt(2)) - (width / 2));
                    var boundsHeight = (float)((height / Math.Sqrt(2)) - (height / 2));

                    SetWidth(Child.Right - Child.Left + boundsWidth + boundsWidth);
                    SetHeight(Child.Bottom - Child.Top + boundsHeight + boundsHeight);
                }
                else
                {
                    SetWidth(Child.Right - Child.Left + CoseLayoutSettings.Compound_Node_Margin + CoseLayoutSettings.Compound_Node_Margin);//+ (2 * CoseDefaultValues.COMPOUND_NODE_MARGIN)); //+ diffWidth);
                    SetHeight(Child.Bottom - Child.Top + CoseLayoutSettings.Compound_Node_Margin + CoseLayoutSettings.Compound_Node_Margin);// + (2 * CoseDefaultValues.COMPOUND_NODE_MARGIN)); // + diffHeight);
                }
            }
        }

        /// <summary>
        /// Sets the grid start/ end coorinates for this node
        /// </summary>
        /// <param name="_startX"></param>
        /// <param name="_finishX"></param>
        /// <param name="_startY"></param>
        /// <param name="_finishY"></param>
        public void SetGridCoordinates(int startX, int finishX, int startY, int finishY)
        {
            LayoutValues.StartX = startX;
            LayoutValues.FinishX = finishX;
            LayoutValues.StartY = startY;
            LayoutValues.FinishY = finishY;
        }

        /// <summary>
        /// Calculates the estimated size of this node
        /// </summary>
        /// <returns></returns>
        public float CalcEstimatedSize()
        {
            if (Child == null)
            {
                EstimatedSize = (rect.width + rect.height) / 2;
                return EstimatedSize;
            }
            else
            {
                EstimatedSize = Child.CalcEstimatedSize();
                rect.width = EstimatedSize;
                rect.height = EstimatedSize;
                return EstimatedSize;
            }
        }

        /// <summary>
        /// Returns a list with all neighbour nodes
        /// </summary>
        /// <returns>the neighbour nodes</returns>
        public HashSet<CoseNode> GetNeighborsList()
        {
            HashSet<CoseNode> neighbors = new HashSet<CoseNode>();

            foreach (CoseEdge edge in Edges)
            {
                if (edge.Source.Equals(this))
                {
                    neighbors.Add(edge.Target);
                }
                else
                {
                    if (!edge.Target.Equals(this))
                    {
                        throw new System.Exception("Incorrect Incidency");
                    }
                    neighbors.Add(edge.Source);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// updates the bounding rect of the node
        /// </summary>
        /// <param name="left">the left position</param>
        /// <param name="right">the right position</param>
        /// <param name="top">the top position</param>
        /// <param name="bottom">the bottom position</param>
        public void UpdateRect(float left, float right, float top, float bottom)
        {
            rect = new Rect(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// Returns the center of x postion
        /// </summary>
        /// <returns>center x postion</returns>
        public double GetCenterX()
        {
            return rect.x + (rect.width / 2);
        }

        /// <summary>
        /// Returns the center of y postion
        /// </summary>
        /// <returns>center y postion</returns>
        public double GetCenterY()
        {
            return rect.y + (rect.height / 2);
        }

        /// <summary>
        /// Sets the height of the bouding rect
        /// </summary>
        /// <param name="height">the height</param>
        public void SetHeight(float height)
        {
            rect.height = height;
        }

        /// <summary>
        /// Sets the width of the bouding rect
        /// </summary>
        /// <param name="height">the width</param>
        public void SetWidth(float width)
        {
            rect.width = width;
        }

        /// <summary>
        /// Returns the left postion of the bounding rect
        /// </summary>
        /// <returns>the left position</returns>
        public Double GetLeft()
        {
            return rect.x;
        }

        /// <summary>
        /// Returns the right postion of the bounding rect
        /// </summary>
        /// <returns>the right position</returns>
        public Double GetRight()
        {
            return rect.x + rect.width;
        }

        /// <summary>
        /// Returns the top postion of the bounding rect
        /// </summary>
        /// <returns>the top position</returns>
        public Double GetTop()
        {
            return rect.y;
        }

        /// <summary>
        /// Returns the bottom postion of the bounding rect
        /// </summary>
        /// <returns>the bottom position</returns>
        public Double GetBottom()
        {
            return rect.y + rect.height;
        }
    }
}

