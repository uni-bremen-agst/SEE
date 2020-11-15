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
        /// indicates whether this node is connected/ parent graph is connected
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
        /// the scale of this node
        /// </summary>
        private Vector3 scale;

        /// <summary>
        /// the centerPosition of this node
        /// </summary>
        private Vector3 centerPosition;

        /// <summary>
        /// the extend of this node
        /// </summary>
        public Vector3 Extend
        {
            get => scale / 2;
        }

        /// <summary>
        /// the sublayout values
        /// </summary>
        private CoseNodeSublayoutValues sublayoutValues = new CoseNodeSublayoutValues();

        /// <summary>
        /// The estimated size of the node
        /// </summary>
        private double estimatedSize = Mathf.NegativeInfinity;

        /// <summary>
        /// the layout value
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
        /// the inclusion tree depth
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
            Vector3 center = CenterPosition;
            if (center == nodeB.CenterPosition)
            {
                center = new Vector3(center.x, center.y, center.z + 0.001f);
            }

            centerPosition = center;

            Rect rectA = CoseHelper.NewRect(Scale, centerPosition);
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
        /// <param name="withOwner">if tue the owner nodes are also added</param>
        /// <returns>a list with this nodes and its children nodes</returns>
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

            layoutValues.DisplacementX = layout.CoseLayoutSettings.CoolingFactor * (layoutValues.SpringForceX + layoutValues.RepulsionForceX + layoutValues.GravitationForceX) / noOfChildren;
            layoutValues.DisplacementY = layout.CoseLayoutSettings.CoolingFactor * (layoutValues.SpringForceY + layoutValues.RepulsionForceY + layoutValues.GravitationForceY) / noOfChildren;

            if (Math.Abs(layoutValues.DisplacementX) > maxNodeDisplacement)
            {
                layoutValues.DisplacementX = (float)maxNodeDisplacement * CoseHelper.Sign(layoutValues.DisplacementX);
            }

            if (Math.Abs(layoutValues.DisplacementY) > maxNodeDisplacement)
            {
                layoutValues.DisplacementY = (float)maxNodeDisplacement * CoseHelper.Sign(layoutValues.DisplacementY);
            }

            if (child == null && !sublayoutValues.IsSubLayoutNode)
            {
                MoveBy(layoutValues.DisplacementX, layoutValues.DisplacementY);
            }
            else if (child.Nodes.Count == 0 && !sublayoutValues.IsSubLayoutNode)
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

            layout.CoseLayoutSettings.TotalDisplacement += (decimal)(Math.Abs(layoutValues.DisplacementX) + Math.Abs(layoutValues.DisplacementY));
        }

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

            sublayoutValues.UpdateRelativeBounding(scale: scale, position: position);
            UpdateBounding(position: position, scale: scale);

            if (child != null)
            {
                child.LeftFrontCorner = GetLeftFrontCorner();
                child.RightBackCorner = GetRightBackCorner();
                child.UpdateBounding();
            }
        }

        /// <summary>
        /// Return wheather the node is a leaf node
        /// </summary>
        /// <returns>true if the node is a leaf node</returns>
        public bool IsLeaf()
        {
            return child == null || child.Nodes.Count == 0;
        }

        /// <summary>
        /// Calculates the number of children
        /// </summary>
        /// <returns>the number of children</returns>
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
        /// Sets the node to the given center Location
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="z">z position</param>
        public void SetLocation(float x, float z)
        {
            centerPosition.x = x;
            centerPosition.z = z;
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

                centerPosition.x = child.LeftFrontCorner.x + child.Extend.x;
                centerPosition.z = child.RightBackCorner.y + child.Extend.z;

                SetWidth(child.Scale.x + CoseLayoutSettings.Compound_Node_Margin + CoseLayoutSettings.Compound_Node_Margin);
                SetHeight(child.Scale.z + CoseLayoutSettings.Compound_Node_Margin + CoseLayoutSettings.Compound_Node_Margin);
            }
        }

        /// <summary>
        /// Sets the grid start/ end coorinates for this node
        /// </summary>
        /// <param name="_startX">start x</param>
        /// <param name="_finishX">finish x</param>
        /// <param name="_startY">start y</param>
        /// <param name="_finishY">finish y</param>
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
                scale.x = (float)estimatedSize;
                scale.z = (float)estimatedSize;
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
        /// <param name="position">the center position</param>
        /// <param name="scale">the scale</param>
        public void UpdateBounding(Vector2 position, Vector2 scale)
        {
            this.scale = scale;
            centerPosition = position;
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
            scale.x = (float)width;
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
        /// Returns the left front corner of this node as a 2D Vector
        /// </summary>
        /// <returns>left front corner</returns>
        public Vector2 GetLeftFrontCorner()
        {
            Vector2 leftLowerCorner = new Vector2()
            {
                x = centerPosition.x - Extend.x,
                y = centerPosition.z + Extend.z
            };
            return leftLowerCorner;
        }

        /// <summary>
        /// Returns the right back corner of this node as a 2D Vector
        /// </summary>
        /// <returns>right back corner</returns>
        public Vector2 GetRightBackCorner()
        {
            Vector2 rightBackCorner = new Vector2()
            {
                x = centerPosition.x + Extend.x,
                y = centerPosition.z - Extend.z
            };
            return rightBackCorner;
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

