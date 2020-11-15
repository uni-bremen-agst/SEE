using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Layout.NodeLayouts.EvoStreets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    public class EvoStreetsNodeLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        public EvoStreetsNodeLayout(float groundLevel, float Unit)
        : base(groundLevel)
        {
            name = "EvoStreets";
            OffsetBetweenBuildings *= Unit;
            StreetWidth *= Unit;
            StreetHeight *= Unit;
        }

        /// <summary>
        /// The distance between two neighboring leaf-node representations.
        /// The actual value used will be multiplied by leafNodeFactory.Unit.
        /// </summary>
        private readonly float OffsetBetweenBuildings = 1.0f;

        /// <summary>
        /// The base street width that will be adjusted by the "depth" of the street
        /// (<see cref="RelativeStreetWidth"/>).
        /// The actual value used will be multiplied by leafNodeFactory.Unit.
        /// </summary>
        private readonly float StreetWidth = 10.0f;

        /// <summary>
        /// The height (y co-ordinate) of game objects (inner tree nodes) represented by streets.
        /// The actual value used will be multiplied by UnleafNodeFactory.Unit.
        /// </summary>
        private readonly float StreetHeight = 0.05f;

        /// <summary>
        /// The maximal depth of the ENode tree hierarchy. Set by Layout and used by RelativeStreetWidth.
        /// </summary>
        private int maximalDepth;

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> gameNodes)
        {
            if (gameNodes.Count == 0)
            {
                throw new System.Exception("No nodes to be laid out.");
            }
            else if (gameNodes.Count == 1)
            {
                ILayoutNode singleNode = gameNodes.FirstOrDefault();
                Dictionary<ILayoutNode, NodeTransform> layout_result = new Dictionary<ILayoutNode, NodeTransform>
                {
                    [singleNode] = new NodeTransform(Vector3.zero, singleNode.LocalScale)
                };
                return layout_result;
            }
            else
            {
                roots = LayoutNodes.GetRoots(gameNodes);
                if (roots.Count == 0)
                {
                    throw new System.Exception("Graph has no root node.");
                }
                else if (roots.Count > 1)
                {
                    throw new System.Exception("Graph has multiple roots.");
                }
                else
                {
                    ILayoutNode root = roots.FirstOrDefault();
                    ENode rootNode = GenerateHierarchy(root);
                    maximalDepth = MaxDepth(root);
                    SetScalePivotsRotation(rootNode);
                    CalculationNodeLocation(rootNode, Vector3.zero);
                    Dictionary<ILayoutNode, NodeTransform> layout_result = new Dictionary<ILayoutNode, NodeTransform>();
                    To_Layout(rootNode, ref layout_result);
                    return layout_result;
                }
            }
        }

        /// <summary>
        /// Adds the layout information of the given node and all its descendants to the layout result.
        /// </summary>
        /// <param name="node">root of a subtree to be added to the layout result</param>
        /// <param name="layout_result">layout result</param>
        private void To_Layout(ENode node, ref Dictionary<ILayoutNode, NodeTransform> layout_result)
        {
            if (node.IsHouse())
            {
                Place_House(node, ref layout_result);
            }
            else
            {
                // Street
                Place_Street(node, ref layout_result);
                foreach (ENode child in node.Children)
                {
                    To_Layout(child, ref layout_result);
                }
            }
        }

        /// <summary>
        /// Adds the layout information of the given node assumed to be a leaf to the layout result.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <param name="layout_result">layout result</param>
        private void Place_House(ENode node, ref Dictionary<ILayoutNode, NodeTransform> layout_result)
        {
            layout_result[node.GraphNode] = new NodeTransform(node.Location, node.Scale, node.Rotation);
        }

        /// <summary>
        /// Adds the layout information of the given node assumed to be an inner node to the layout result.
        /// </summary>
        /// <param name="node">inner node</param>
        /// <param name="layout_result">layout result</param>
        private void Place_Street(ENode node, ref Dictionary<ILayoutNode, NodeTransform> layout_result)
        {
            layout_result[node.GraphNode]
                = new NodeTransform(node.Location,
                                    new Vector3(node.Scale.x, StreetHeight, node.Scale.z),
                                    node.Rotation);
        }

        /// <summary>
        /// Creates the ENode tree hierarchy starting at given root node. The root has
        /// depth 0.
        /// </summary>
        /// <param name="root">root of the hierarchy</param>
        /// <returns>root ENode</returns>
        private ENode GenerateHierarchy(ILayoutNode root)
        {
            ENode result = new ENode(root)
            {
                Depth = 0
            };
            foreach (ILayoutNode child in root.Children())
            {
                ENode kid = GenerateHierarchy(result, child);
                result.Children.Add(kid);
            }
            return result;
        }

        /// <summary>
        /// Creates the ENode tree hierarchy starting at given node.
        /// </summary>
        /// <param name="parent">parent of node in the ENode tree hierarchy</param>
        /// <param name="node">root of a subtree in the original graph</param>
        /// <returns>ENode representing node</returns>
        private ENode GenerateHierarchy(ENode parent, ILayoutNode node)
        {
            ENode result = new ENode(node)
            {
                Depth = parent.Depth + 1,
                ParentNode = parent
            };
            foreach (ILayoutNode child in node.Children())
            {
                ENode kid = GenerateHierarchy(result, child);
                result.Children.Add(kid);
            }
            return result;
        }

        /// <summary>
        /// Determines the location and rotation of leaf nodes (houses) and the location, rotation and scaling
        /// of inner nodes (streets).
        /// </summary>
        /// <param name="node">node whose layout information is to be determined</param>
        /// <param name="newLoc">the position at which to locate the node</param>
        private void CalculationNodeLocation(ENode node, Vector3 newLoc)
        {
            float nextX;
            float nextZ;

            // Note: pivots are 2D vectors where the y component actually represents a value on the z axis in 3D
            Vector2 fromPivot = new Vector2(node.Scale.x / 2, node.Scale.z / 2);
            Vector2 rotatedfromPivot = fromPivot.GetRotated(node.Rotation);
            Vector2 toPivot = rotatedfromPivot;

            if (node.IsHouse())
            {
                Vector3 toGoal = new Vector3(toPivot.x, groundLevel, toPivot.y);
                node.Location = newLoc + toGoal;
            }
            else
            {
                // street
                Vector2 StreetfromPivot = new Vector2(node.Scale.x / 2, node.ZPivot);
                Vector2 StreetRotatedfromPivot = StreetfromPivot.GetRotated(node.Rotation);
                float relStreetWidth = RelativeStreetWidth(node);
                Vector3 StreetToGoal = new Vector3(StreetRotatedfromPivot.x, groundLevel, StreetRotatedfromPivot.y);

                node.Location = newLoc + StreetToGoal;
                node.Scale = new Vector3(node.Scale.x, node.Scale.y, relStreetWidth);

                foreach (ENode eNode in node.Children)
                {
                    float streetMod = eNode.Left ? -relStreetWidth / 2 : +relStreetWidth / 2;
                    Vector2 relChild = new Vector2(eNode.XPivot, 0.0f);
                    relChild = relChild.GetRotated(node.Rotation);
                    Vector2 relMy = new Vector2(0.0f, node.ZPivot + streetMod);
                    relMy = relMy.GetRotated(node.Rotation);

                    nextX = newLoc.x + relChild.x + relMy.x;
                    nextZ = newLoc.z + relChild.y + relMy.y;

                    CalculationNodeLocation(eNode, new Vector3(nextX, groundLevel, nextZ));
                }
            }
        }

        /// <summary>
        /// Sets the scale, pivots, and rotation of given node and all its descendants.
        /// Does not yet set the exact positions, however.
        /// </summary>
        /// <param name="node">node whose scale, pivots, and rotation are to be set</param>
        private void SetScalePivotsRotation(ENode node)
        {
            if (node.GraphNode.IsLeaf)
            {
                SetHouseScale(node);
            }
            else
            {
                // street
                float leftPivotX = OffsetBetweenBuildings;
                float RightPivotX = OffsetBetweenBuildings;

                foreach (ENode newChildNode in node.Children)
                {
                    newChildNode.Rotation =
                        (leftPivotX <= RightPivotX) ? node.Rotation - 90.0f : node.Rotation + 90.0f; // could be a street
                    newChildNode.Rotation = (Mathf.FloorToInt(newChildNode.Rotation) + 360) % 360;
                    SetScalePivotsRotation(newChildNode);
                    // Pivot setting
                    if (leftPivotX <= RightPivotX)
                    {
                        // left
                        newChildNode.Left = true; // is default value
                        if (newChildNode.GraphNode.IsLeaf)
                        {
                            // house
                            leftPivotX += newChildNode.Scale.x;
                            newChildNode.XPivot = leftPivotX;
                            leftPivotX += OffsetBetweenBuildings;
                        }
                        else
                        {   // street
                            newChildNode.XPivot = leftPivotX;
                            leftPivotX += newChildNode.Scale.z;
                            leftPivotX += OffsetBetweenBuildings;
                        }
                    }
                    else
                    {
                        // right
                        newChildNode.Left = false;
                        if (newChildNode.GraphNode.IsLeaf)
                        {
                            // house
                            newChildNode.XPivot = RightPivotX;
                            RightPivotX += newChildNode.Scale.x;
                            RightPivotX += OffsetBetweenBuildings;
                        }
                        else
                        {
                            // street
                            RightPivotX += newChildNode.Scale.z;
                            newChildNode.XPivot = RightPivotX;
                            RightPivotX += OffsetBetweenBuildings;
                        }
                    }

                    if (newChildNode.GraphNode.IsLeaf)
                    {   // house
                        newChildNode.Rotation =
                            (newChildNode.Left) ? node.Rotation - 180.0f : node.Rotation; //is not a street
                        newChildNode.Rotation = (Mathf.FloorToInt(newChildNode.Rotation) + 360) % 360;
                    }
                }
                //for InParentNode is a street calculate its size

                node.Scale = new Vector3(MaxWidthRequired(node, OffsetBetweenBuildings),
                                         node.MaxChildZ,
                                         DepthRequired(node));
                node.ZPivot = MaxLeftZ(node);
            }
        }

        /// <summary>
        /// Sets the scale of the given leaf ENode according to the scale of the graph
        /// node it represents. The original scale of the graph node is maintained.
        /// 
        /// Precondition: node is a leaf
        /// </summary>
        /// <param name="node">ENode whose scale is to be set</param>
        private void SetHouseScale(ENode node)
        {
            // Scaled metric values for the dimensions.
            Vector3 size = node.GraphNode.LocalScale;
            node.Scale = new Vector3(size.x, size.y, size.z);
        }

        /// <summary>
        /// Returns the maximal depth (if child is a leaf) or width (if child is an inner node)
        /// for all left children of given node.
        /// </summary>
        /// <param name="node">node whose maximum is to be determined</param>
        /// <returns>maximum depth of width</returns>
        private float MaxLeftZ(ENode node)
        {
            float max = 0.0f;
            foreach (ENode child in node.Children)
            {
                //Left children only             
                if (child.Left)
                {
                    if (child.IsHouse())
                    {
                        if (child.Scale.z > max)
                        {
                            max = child.Scale.z;
                        }
                    }
                    else
                    {
                        if (child.Scale.x > max)
                        {
                            max = child.Scale.x;
                        }
                    }
                }
            }
            return max;
        }

        /// <summary>
        /// Returns the depth of the area required for all children of given node.
        /// </summary>
        /// <param name="node">node whose required depth is to be determined</param>
        /// <returns>depth required</returns>
        private float DepthRequired(ENode node)
        {
            float leftMax = 0.0f;
            float rightMax = 0.0f;

            foreach (ENode eNode in node.Children)
            {
                if (eNode.Left)
                {
                    if (eNode.IsHouse())
                    {
                        if (eNode.Scale.z > leftMax)
                        {
                            leftMax = eNode.Scale.z;
                        }
                    }
                    else
                    {
                        if (eNode.Scale.x > leftMax)
                        {
                            leftMax = eNode.Scale.x;
                        }
                    }
                }
                else
                {
                    if (eNode.IsHouse())
                    {
                        if (eNode.Scale.z > rightMax)
                        {
                            rightMax = eNode.Scale.z;
                        }
                    }
                    else
                    {
                        if (eNode.Scale.x > rightMax)
                        {
                            rightMax = eNode.Scale.x;
                        }
                    }
                }
            }
            return leftMax + rightMax + RelativeStreetWidth(node);
        }

        /// <summary>
        /// Returns the maximum of the width required for left and right children.
        /// 
        /// Precondition: node is an inner node represented by a street
        /// </summary>
        /// <param name="node">inner node</param>
        /// <param name="offset">offset between neighboring children</param>
        /// <returns>maximum width</returns>
        private float MaxWidthRequired(ENode node, float offset)
        {
            float left = WidthRequired(node, offset, true);
            float right = WidthRequired(node, offset, false);
            return left < right ? right : left;
        }

        /// <summary>
        /// Returns the sum of the widths of all children on the left (if left is
        /// true) or on the right (if left is false) including the given offset between
        /// those and the relative width of the street representing the given inner node.
        /// 
        /// Precondition: node is an inner node represented by a street
        /// </summary>
        /// <param name="node">inner node</param>
        /// <param name="offset">offset between neighboring children</param>
        /// <param name="left">if true, only left children are consider, otherwise only
        /// right children</param>
        /// <returns>the width required to place the respective children</returns>
        private float WidthRequired(ENode node, float offset, bool left)
        {
            float sum = offset;

            foreach (ENode eNode in node.Children)
            {
                if (eNode.Left == left)
                {
                    if (eNode.IsHouse())
                    {
                        sum += eNode.Scale.x + offset;
                    }
                    else
                    {
                        sum += eNode.Scale.z + offset;
                    }
                }
            }
            return sum + RelativeStreetWidth(node);
        }

        /// <summary>
        /// Returns the width of the street representing an inner node relative to its tree depth.
        /// </summary>
        /// <param name="node">inner node represented as street</param>
        /// <returns>width of the street</returns>
        private float RelativeStreetWidth(ENode node)
        {
            return StreetWidth * ((maximalDepth + 1) - node.Depth) / (maximalDepth + 1);
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges, ICollection<SublayoutLayoutNode> sublayouts)
        {
            throw new System.NotImplementedException();
        }

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false;
        }
    }
}