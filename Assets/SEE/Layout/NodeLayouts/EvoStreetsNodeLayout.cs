using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Layout.NodeLayouts.EvoStreets;
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
            offsetBetweenBuildings *= Unit;
            streetHeight *= Unit;
        }

        /// <summary>
        /// The street width of the root node. The width of streets deeper in the hierarchy
        /// will be downscaled by the "depth" relative to this value (<see cref="RelativeStreetWidth"/>).
        /// It will be calculated by <see cref="CalculateStreetWidth(IList{ILayoutNode})"/>.
        /// </summary>
        private float streetWidth = 0.2f;

        /// <summary>
        /// <see cref="CalculateStreetWidth(IList{ILayoutNode})"/> determines a statistical
        /// parameter of the widths and depths of all leaf nodes (the average) and adjusts
        /// this statistical parameter by multiplying it with this factor <see cref="StreetWidthPercentage"/>.
        /// </summary>
        private const float StreetWidthPercentage = 0.3f;

        /// <summary>
        /// The distance between two neighboring leaf-node representations.
        /// The actual value used will be multiplied by leafNodeFactory.Unit.
        /// </summary>
        private readonly float offsetBetweenBuildings = 0.05f;

        /// <summary>
        /// The height (y co-ordinate) of game objects (inner tree nodes) represented by streets.
        /// The actual value used will be multiplied by leafNodeFactory.Unit.
        /// </summary>
        private readonly float streetHeight = 0.0001f;

        /// <summary>
        /// The maximal depth of the ENode tree hierarchy. Set by Layout and used by RelativeStreetWidth.
        /// The depth of a tree with only one node is 1.
        /// </summary>
        private int maximalDepth;

        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> gameNodes)
        {
            IList<ILayoutNode> layoutNodes = gameNodes.ToList();
            if (layoutNodes.Count == 0)
            {
                throw new Exception("No nodes to be laid out.");
            }

            if (layoutNodes.Count == 1)
            {
                ILayoutNode singleNode = layoutNodes.First();
                Dictionary<ILayoutNode, NodeTransform> layoutResult = new Dictionary<ILayoutNode, NodeTransform>
                {
                    [singleNode] = new NodeTransform(Vector3.zero, singleNode.LocalScale)
                };
                return layoutResult;
            }

            roots = LayoutNodes.GetRoots(layoutNodes);
            if (roots.Count == 0)
            {
                throw new Exception("Graph has no root node.");
            }

            if (roots.Count > 1)
            {
                throw new Exception("Graph has multiple roots.");
            }

            {
                streetWidth = CalculateStreetWidth(layoutNodes);
                ILayoutNode root = roots.FirstOrDefault();
                ENode rootNode = GenerateHierarchy(root);
                maximalDepth = MaxDepth(root);
                SetScalePivotsRotation(rootNode);
                CalculationNodeLocation(rootNode, Vector3.zero);
                Dictionary<ILayoutNode, NodeTransform> layoutResult = new Dictionary<ILayoutNode, NodeTransform>();
                ToLayout(rootNode, ref layoutResult);
                return layoutResult;
            }
        }

        /// <summary>
        /// Returns the width of the street for the root as a percentage <see cref="StreetWidthPercentage"/>
        /// of the average of all widths and depths of leaf nodes in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">the nodes to be laid out</param>
        /// <returns>width of street for the root</returns>
        private float CalculateStreetWidth(IList<ILayoutNode> layoutNodes)
        {
            float result = 0;
            int numberOfLeaves = 0;
            foreach (ILayoutNode node in layoutNodes)
            {
                if (node.IsLeaf)
                {
                    numberOfLeaves++;
                    result += node.AbsoluteScale.x > node.AbsoluteScale.z ? node.AbsoluteScale.x : node.AbsoluteScale.z;
                }
            }
            // assert: numberOfLeaves > 0
            result /= numberOfLeaves;
            // result is now the average length over all widths and depths of all leave nodes.
            return result * StreetWidthPercentage;
        }

        /// <summary>
        /// Adds the layout information of the given node and all its descendants to the layout result.
        /// </summary>
        /// <param name="node">root of a subtree to be added to the layout result</param>
        /// <param name="layout_result">layout result</param>
        private void ToLayout(ENode node, ref Dictionary<ILayoutNode, NodeTransform> layout_result)
        {
            if (node.IsLeaf())
            {
                PlaceHouse(node, ref layout_result);
            }
            else
            {
                // Street
                PlaceStreet(node, ref layout_result);
                foreach (ENode child in node.Children)
                {
                    ToLayout(child, ref layout_result);
                }
            }
        }

        /// <summary>
        /// Adds the layout information of the given node assumed to be a leaf to the layout result.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <param name="layout_result">layout result</param>
        private void PlaceHouse(ENode node, ref Dictionary<ILayoutNode, NodeTransform> layout_result)
        {
            layout_result[node.GraphNode] = new NodeTransform(node.Location, node.Scale, 0/*node.Rotation*/);
        }

        /// <summary>
        /// Adds the layout information of the given node assumed to be an inner node to the layout result.
        /// </summary>
        /// <param name="node">inner node</param>
        /// <param name="layout_result">layout result</param>
        private void PlaceStreet(ENode node, ref Dictionary<ILayoutNode, NodeTransform> layout_result)
        {
            layout_result[node.GraphNode]
                = new NodeTransform(node.Location,
                                    new Vector3(node.Scale.x, streetHeight, node.Scale.z),
                                    0/*node.Rotation*/);
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
                result.Children.Add(GenerateHierarchy(result, child));
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
                result.Children.Add(GenerateHierarchy(result, child));
            }
            return result;
        }

        /// <summary>
        /// Determines the location of leaf nodes (houses) and the location and scale
        /// of inner nodes (streets).
        /// </summary>
        /// <param name="node">node whose layout information is to be determined</param>
        /// <param name="newLoc">the position at which to locate the node</param>
        private void CalculationNodeLocation(ENode node, Vector3 newLoc)
        {
            // Note: pivots are 2D vectors where the y component actually represents a value on the z axis in 3D
            Vector2 fromPivot = new Vector2(node.Scale.x / 2, node.Scale.z / 2);
            Vector2 toPivot = fromPivot.GetRotated(node.Rotation);

            if (node.IsLeaf())
            {
                Vector3 toGoal = new Vector3(toPivot.x, groundLevel, toPivot.y);
                node.Location = newLoc + toGoal;
            }
            else
            {
                // street
                float relStreetWidth = RelativeStreetWidth(node);
                Vector2 streetfromPivot = new Vector2(node.Scale.x / 2, node.ZPivot);
                Vector2 streetRotatedfromPivot = streetfromPivot.GetRotated(node.Rotation);
                Vector3 toGoal = new Vector3(streetRotatedfromPivot.x, groundLevel, streetRotatedfromPivot.y);

                node.Location = newLoc + toGoal;
                node.Scale = new Vector3(node.Scale.x, node.Scale.y, relStreetWidth);

                foreach (ENode child in node.Children)
                {
                    Vector2 relChild = new Vector2(child.XPivot, 0.0f);
                    relChild = relChild.GetRotated(node.Rotation);
                    float streetMod = child.Left ? -relStreetWidth / 2 : +relStreetWidth / 2;
                    Vector2 relMy = new Vector2(0.0f, node.ZPivot + streetMod);
                    relMy = relMy.GetRotated(node.Rotation);

                    float nextX = newLoc.x + relChild.x + relMy.x;
                    float nextZ = newLoc.z + relChild.y + relMy.y;

                    CalculationNodeLocation(child, new Vector3(nextX, groundLevel, nextZ));
                }
            }
        }

        /// <summary>
        /// Sets the scale, pivots, and rotation of given <paramref name="node"/> and all its descendants.
        /// Does not yet set the exact positions, however.
        /// </summary>
        /// <param name="node">node whose scale, pivots, and rotation are to be set</param>
        private void SetScalePivotsRotation(ENode node)
        {
            if (node.IsLeaf())
            {
                SetHouseScale(node);
            }
            else
            {
                // node is depicted as a street
                float leftPivotX = offsetBetweenBuildings;
                float RightPivotX = offsetBetweenBuildings;

                foreach (ENode child in node.Children)
                {
                    // child could be a street or house; we will rotate it no matter what; if it is
                    // in fact a leaf, we will rotate it back below
                    child.Rotation = (leftPivotX <= RightPivotX) ? node.Rotation - 90.0f : node.Rotation + 90.0f;
                    child.Rotation = (Mathf.FloorToInt(child.Rotation) + 360) % 360;
                    SetScalePivotsRotation(child);
                    // Pivot setting
                    if (leftPivotX <= RightPivotX)
                    {
                        // child will be put on the left side of the street
                        child.Left = true;
                        if (child.IsLeaf())
                        {
                            // house
                            leftPivotX += child.Scale.x;
                            child.XPivot = leftPivotX;
                            leftPivotX += offsetBetweenBuildings;
                        }
                        else
                        {   // street
                            child.XPivot = leftPivotX;
                            leftPivotX += child.Scale.z;
                            leftPivotX += offsetBetweenBuildings;
                        }
                    }
                    else
                    {
                        // child will be put on the right side of the street
                        child.Left = false;
                        if (child.IsLeaf())
                        {
                            // house
                            child.XPivot = RightPivotX;
                            RightPivotX += child.Scale.x;
                            RightPivotX += offsetBetweenBuildings;
                        }
                        else
                        {
                            // street
                            RightPivotX += child.Scale.z;
                            child.XPivot = RightPivotX;
                            RightPivotX += offsetBetweenBuildings;
                        }
                    }

                    if (child.IsLeaf())
                    {   // a house (leaf) was rotated above; we will rotate it back again
                        child.Rotation = (child.Left) ? node.Rotation - 180.0f : node.Rotation;
                        child.Rotation = (Mathf.FloorToInt(child.Rotation) + 360) % 360;
                    }
                }

                // node is a street, here we calculate its size
                node.Scale = new Vector3(MaxWidthRequired(node, offsetBetweenBuildings),
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
            node.Scale = node.GraphNode.AbsoluteScale;
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
                    if (child.IsLeaf())
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
        /// Returns the depth of the area required for all children of given <paramref name="node"/>.
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
                    if (eNode.IsLeaf())
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
                    if (eNode.IsLeaf())
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
        /// Precondition: node is an inner node represented by a street.
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
        /// <param name="left">if true, only left children are considered, otherwise only
        /// right children</param>
        /// <returns>the width required to place the respective children</returns>
        private float WidthRequired(ENode node, float offset, bool left)
        {
            float sum = offset;

            foreach (ENode child in node.Children)
            {
                if (child.Left == left)
                {
                    if (child.IsLeaf())
                    {
                        sum += child.Scale.x + offset;
                    }
                    else
                    {
                        sum += child.Scale.z + offset;
                    }
                }
            }
            return sum;
        }

        /// <summary>
        /// Returns the width of the street representing an inner node relative to its tree depth.
        /// </summary>
        /// <param name="node">inner node represented as street</param>
        /// <returns>width of the street</returns>
        private float RelativeStreetWidth(ENode node)
        {
            return streetWidth * ((maximalDepth + 1) - node.Depth) / (maximalDepth + 1);
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges, ICollection<SublayoutLayoutNode> sublayouts)
        {
            throw new NotImplementedException();
        }

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false;
        }
    }
}