using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System;
using System.Linq;

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
        private ILayoutNode nodeObject;

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
        //public Rect rect = new Rect(0, 0, 0, 0);

        /// TODO
        private Vector3 scale;

        /// TODO
        private Vector3 centerPosition; 

        /// <summary>
        /// TODO
        /// </summary>
        private CoseNodeSublayoutValues sublayoutValues = new CoseNodeSublayoutValues();

        /// <summary>
        /// The estimated size of the node
        /// </summary>
        private double estimatedSize = Mathf.NegativeInfinity;

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

        /// <summary>
        /// TODO
        /// </summary>
        private int inclusionTreeDepth = int.MaxValue;

        public int NoOfChildren { get => noOfChildren; set => noOfChildren = value; }
        public List<CoseNode> Surrounding { get => surrounding; set => surrounding = value; }
        public CoseNodeLayoutValues LayoutValues { get => layoutValues; set => layoutValues = value; }
        public double EstimatedSize { get => estimatedSize; set => estimatedSize = value; }
        public CoseNodeSublayoutValues SublayoutValues { get => sublayoutValues; set => sublayoutValues = value; }
        public List<CoseEdge> Edges { get => edges; set => edges = value; }
        public CoseGraph Owner { get => owner; set => owner = value; }
        public CoseGraph Child { get => child; set => child = value; }
        public bool IsConnected { get => isConnected; set => isConnected = value; }
        public ILayoutNode NodeObject { get => nodeObject; set => nodeObject = value; }
        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }
        public int InclusionTreeDepth { get => inclusionTreeDepth; set => inclusionTreeDepth = value; }
        public Vector3 Scale { get => scale; set => scale = value; }
        public Vector3 CenterPosition { get => centerPosition; set => centerPosition = value; }




        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node">the original node</param>
        /// <param name="graphManager">the graphmanager</param>
        public CoseNode(ILayoutNode node, CoseGraphManager graphManager)
        {
            nodeObject = node;
            this.graphManager = graphManager;
        }

        /// <summary>
        /// calculates if this node overlaps with the given node
        /// </summary>
        /// <param name="nodeB">the second node</param>
        /// <param name="overlapAmount">the amount of how much is nodes overlap</param>
        /// <returns></returns>
        public bool CalcOverlap(CoseNode nodeB, double[] overlapAmount)
        {
            Rect rectA = CoseHelper.NewRect(Scale, CenterPosition);
            Rect rectB = CoseHelper.NewRect(nodeB.Scale, nodeB.CenterPosition);

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
            Tuple<bool, double[]> result = CoseGeometry.GetIntersection(CoseHelper.NewRect(scale, centerPosition), CoseHelper.NewRect(nodeB.scale, nodeB.centerPosition), clipPoints);
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

            layoutValues.DisplacementX = layout.CoseLayoutSettings.CoolingFactor * (float)(layoutValues.SpringForceX + layoutValues.RepulsionForceX + layoutValues.GravitationForceX) / noOfChildren;
            layoutValues.DisplacementY = layout.CoseLayoutSettings.CoolingFactor * (float)(layoutValues.SpringForceY + layoutValues.RepulsionForceY + layoutValues.GravitationForceY) / noOfChildren;


            if (Math.Abs(layoutValues.DisplacementX) > maxNodeDisplacement)
            {
                layoutValues.DisplacementX = (float) maxNodeDisplacement * Mathf.Sign(layoutValues.DisplacementX);
            }

            if (Math.Abs(layoutValues.DisplacementY) > maxNodeDisplacement)
            {
                layoutValues.DisplacementY = (float) maxNodeDisplacement * Mathf.Sign(layoutValues.DisplacementY);
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

            layout.CoseLayoutSettings.TotalDisplacement += Math.Abs(layoutValues.DisplacementX) + Math.Abs(layoutValues.DisplacementY);
        }

        /*/// <summary>
        /// Changes to Source/ Target node of the intergraph edges from all child nodes of the sublayout root to sublayout root
        /// </summary>
        public void SetIntergraphEdgesToSublayoutRoot()
        {
            //List<CoseEdge> edges = new List<CoseEdge>();
            List<CoseEdge> allIntergraphEdges = new List<CoseEdge>();
            allIntergraphEdges.AddRange(graphManager.Edges);

            // here nur sublayout nodes für das jeweilige root 

            List<CoseNode> nodes = new List<CoseNode>();//cNSubLValues.Sublayout.NodeMapSublayout.Keys.ToList();

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
                                    this.edges.Add(edge);
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
                                    this.edges.Add(edge);
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
            graphManager.AllEdges = null;
        }*/

        /// <summary>
        /// Propogates the displacement of the sublayout root to the sublayout children
        /// </summary>
        /// <param name="dx">the displacement of the x direction</param>
        /// <param name="dy">the displacement of the y direction</param>
        public void PropogateDisplacementToSublayoutChildren(float dx, float dy)
        {
            MoveBy(dx, dy);

            if (child != null)
            {
                child.SetXZDisplacementBoundingRect(dx, dy);

                foreach (CoseNode node in child.Nodes)
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
        public void PropogateDisplacementToChildren(float dx, float dy)
        {
            foreach (CoseNode node in child.Nodes)
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
        /// <param name="dz">the displacement of the z direction</param>
        public void MoveBy(double dx, double dz)
        {
            centerPosition.x += (float)dx;
            centerPosition.z += (float)dz;
        }

        /// <summary>
        /// Resets the forces that act on this node
        /// </summary>
        public void Reset()
        {
            layoutValues.SpringForceX = 0;
            layoutValues.SpringForceY = 0;
            layoutValues.RepulsionForceX = 0;
            layoutValues.RepulsionForceY = 0;
            layoutValues.GravitationForceX = 0;
            layoutValues.GravitationForceY = 0;
            layoutValues.DisplacementX = 0;
            layoutValues.DisplacementY = 0;
        }

        /// <summary>
        /// Positions the node relative to the given node
        /// </summary>
        /// <param name="origin">the node</param>
        public void SetPositionRelativ(CoseNode origin)
        {
            sublayoutValues.SetLocationRelative(sublayoutValues.RelativeCenterPosition.x - origin.centerPosition.x, sublayoutValues.RelativeCenterPosition.z - origin.centerPosition.z);
            sublayoutValues.SubLayoutRoot = origin;
        }

        /// <summary>
        /// Sets the position independent to its sublayout root
        /// </summary>
        public void SetOrigin()
        {
            centerPosition.x = sublayoutValues.RelativeCenterPosition.x + sublayoutValues.SubLayoutRoot.centerPosition.x;
            centerPosition.z = sublayoutValues.RelativeCenterPosition.z + sublayoutValues.SubLayoutRoot.centerPosition.z;
            SetWidth(sublayoutValues.RelativeScale.x);
            SetHeight(sublayoutValues.RelativeScale.z);
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

            sublayoutValues.UpdateRelativeBounding(left, right, top, bottom);
            UpdateBounding(left, right, top, bottom);

            if (child != null)
            {
                child.Left = left;
                child.Top = top;
                child.Right = right;
                child.Bottom = bottom;
                child.UpdateBoundingRect();
            }
        }

        /// <summary>
        /// Return wheather the node is a leaf node
        /// </summary>
        /// <returns></returns>
        public bool IsLeaf()
        {
            return child == null || child.Nodes.Count == 0;
        }

        /// <summary>
        /// Calculates the number of children
        /// </summary>
        /// <returns></returns>
        public int CalcNumberOfChildren()
        {
            int noOfChildren = 0;

            if (child == null)
            {
                noOfChildren = 1;
            }
            else
            {
                foreach (CoseNode child in child.Nodes)
                {
                    noOfChildren += child.CalcNumberOfChildren();
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
            centerPosition.x = x;
            centerPosition.z = y;
            //rect.position = new Vector2(x, y);
        }

        /// <summary>
        /// Updates the bounds of this node according to the bounds of its neasted graph
        /// </summary>
        public void UpdateBounds()
        {
            if (child == null)
            {
                throw new System.Exception("Child Graph is null");
            }

            if (sublayoutValues.IsSubLayoutRoot)
            {
                SetWidth(sublayoutValues.RelativeScale.x);
                SetHeight(sublayoutValues.RelativeScale.z);
                child.UpdateBounds(true);
                // rect.x/ rect.y müssen nicht gesetzt werden, da der Knoten seine Größe kennt und nicht abhängig von Child knoten ist
                return;
            }

            if (sublayoutValues.IsSubLayoutNode) // durch das vorherige return werden die root knoten ausgeschlossen
            {
                // diese knoten haben relativ positionen zu subLayoutRoot, d.h. diese müssen wieder auf origin gesetzt werden, 
                // damit das von anderen richtig berechnet werden kann

                // set position to origin by using the relativ values 
                centerPosition.x = sublayoutValues.RelativeCenterPosition.x + sublayoutValues.SubLayoutRoot.centerPosition.x;
                centerPosition.z = sublayoutValues.RelativeCenterPosition.z + sublayoutValues.SubLayoutRoot.centerPosition.z;
                SetWidth(sublayoutValues.RelativeScale.x);
                SetHeight(sublayoutValues.RelativeScale.z);
                child.UpdateBounds(true);
                return;
            }

            if (child.Nodes.Count != 0)
            {
                child.UpdateBounds(true);
               

                centerPosition.x = (float)(child.Left + ((child.Right - child.Left) / 2));
                centerPosition.z = (float)(child.Top + ((child.Bottom - child.Top) / 2));

                SetWidth(child.Right - child.Left + CoseLayoutSettings.Compound_Node_Margin + CoseLayoutSettings.Compound_Node_Margin);//+ (2 * CoseDefaultValues.COMPOUND_NODE_MARGIN)); //+ diffWidth);
                SetHeight(child.Bottom - child.Top + CoseLayoutSettings.Compound_Node_Margin + CoseLayoutSettings.Compound_Node_Margin);// + (2 * CoseDefaultValues.COMPOUND_NODE_MARGIN)); // + diffHeight);

                //centerPosition.x = child.Left.x - CoseLayoutSettings.Compound_Node_Margin;
                //centerPosition.z = child.CenterPosition.z - CoseLayoutSettings.Compound_Node_Margin;

                // float width = childGraph.Right - childGraph.Left / Mathf.Sqrt(2);
                // float height = childGraph.Bottom - childGraph.Top / Mathf.Sqrt(2);

                //float diffWidth = Mathf.Abs(width - rect.width);
                // float diffHeight = Mathf.Abs(height - rect.height);

                // Here add Labelheight etc. 




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
            layoutValues.StartX = startX;
            layoutValues.FinishX = finishX;
            layoutValues.StartY = startY;
            layoutValues.FinishY = finishY;
        }

        /// <summary>
        /// Calculates the estimated size of this node
        /// </summary>
        /// <returns></returns>
        public double CalcEstimatedSize()
        {
            if (child == null)
            {
                estimatedSize = (scale.x + scale.z) / 2;
                return estimatedSize;
            }
            else
            {
                estimatedSize = child.CalcEstimatedSize();
                scale.x = (float) estimatedSize;
                scale.z = (float) estimatedSize;
                return estimatedSize;
            }
        }

        /// <summary>
        /// Returns a list with all neighbour nodes
        /// </summary>
        /// <returns>the neighbour nodes</returns>
        public HashSet<CoseNode> GetNeighborsList()
        {
            HashSet<CoseNode> neighbors = new HashSet<CoseNode>();

            foreach (CoseEdge edge in edges)
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
        public void UpdateBounding(float left, float right, float top, float bottom)
        {
            scale.x = right - left;
            scale.z = bottom - top;
            centerPosition.x = left + scale.x / 2;
            centerPosition.z = top + scale.z / 2;
        }

        /// <summary>
        /// Returns the center of x postion
        /// </summary>
        /// <returns>center x postion</returns>
        public float GetCenterX()
        {
            return centerPosition.x;
        }

        /// <summary>
        /// Returns the center of y postion
        /// </summary>
        /// <returns>center y postion</returns>
        public float GetCenterY()
        {
            return centerPosition.z;
        }

        /// <summary>
        /// Sets the height of the bouding rect
        /// </summary>
        /// <param name="height">the height</param>
        public void SetHeight(double height)
        {
            scale.z = (float)height;
        }

        /// <summary>
        /// Sets the width of the bouding rect
        /// </summary>
        /// <param name="height">the width</param>
        public void SetWidth(double width)
        {
            scale.x = (float) width;
        }

        /// <summary>
        /// Returns the left postion of the bounding rect
        /// </summary>
        /// <returns>the left position</returns>
        public float GetLeft()
        {
            return centerPosition.x - Scale.x / 2;
        }

        /// <summary>
        /// Returns the right postion of the bounding rect
        /// </summary>
        /// <returns>the right position</returns>
        public float GetRight()
        {
            return centerPosition.x + Scale.x / 2;
        }

        /// <summary>
        /// Returns the top postion of the bounding rect
        /// </summary>
        /// <returns>the top position</returns>
        public float GetTop()
        {
            return centerPosition.z - Scale.z / 2;
        }

        /// <summary>
        /// Returns the bottom postion of the bounding rect
        /// </summary>
        /// <returns>the bottom position</returns>
        public float GetBottom()
        {
            return centerPosition.z + Scale.z / 2;
        }
    }
}

