using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Layout.NodeLayouts.EvoStreets;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    public class EvoStreetsNodeLayout : NodeLayout
    {
        static EvoStreetsNodeLayout()
        {
            Name = "EvoStreets";
        }

        /// <summary>
        /// <see cref="CalculateStreetWidth(IList{ILayoutNode})"/> determines a statistical
        /// parameter of the widths and depths of all leaf nodes (the average) and adjusts
        /// this statistical parameter by multiplying it with this factor <see cref="streetWidthPercentage"/>.
        /// </summary>
        private const float streetWidthPercentage = 0.3f;

        /// <summary>
        /// Is used to calculate the offset between buildings as this factor multiplied by
        /// the absolute street width for the root node.
        /// </summary>
        private const float offsetBetweenBuildingsPercentage = 0.3f;

        /// <summary>
        /// The height (y co-ordinate) of game objects (inner tree nodes) represented by streets.
        /// The actual value used will be multiplied by leafNodeFactory.Unit.
        /// </summary>
        private readonly float streetHeight = 0.0001f;

        protected override Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> gameNodes,
            Vector3 centerPosition,
            Vector2 rectangle)
        {
            IList<ILayoutNode> layoutNodes = gameNodes.ToList();
            if (layoutNodes.Count == 0)
            {
                throw new Exception("No nodes to be laid out.");
            }

            if (layoutNodes.Count == 1)
            {
                ILayoutNode singleNode = layoutNodes.First();
                Dictionary<ILayoutNode, NodeTransform> layoutResult = new()
                {
                    [singleNode] = new NodeTransform(0, 0, singleNode.AbsoluteScale)
                };
                return layoutResult;
            }

            Roots = LayoutNodes.GetRoots(layoutNodes);
            if (Roots.Count == 0)
            {
                throw new Exception("Graph has no root node.");
            }

            if (Roots.Count > 1)
            {
                throw new Exception("Graph has multiple roots.");
            }

            {
                LayoutDescriptor treeDescriptor;
                treeDescriptor.StreetWidth = CalculateStreetWidth(layoutNodes);
                treeDescriptor.OffsetBetweenBuildings = treeDescriptor.StreetWidth * offsetBetweenBuildingsPercentage;
                ILayoutNode root = Roots.FirstOrDefault();
                ENode rootNode = GenerateHierarchy(root);
                treeDescriptor.MaximalDepth = MaxDepth(root);

                rootNode.SetSize(Orientation.East, treeDescriptor);
                rootNode.SetLocation(Orientation.East, new Location(0, 0));

                Dictionary<ILayoutNode, NodeTransform> layoutResult = new();
                rootNode.ToLayout(ref layoutResult, groundLevel, streetHeight);
                return layoutResult;
            }
        }

        /// <summary>
        /// Returns the width of the street for the root as a percentage <see cref="streetWidthPercentage"/>
        /// of the average of all widths and depths of leaf nodes in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">the nodes to be laid out</param>
        /// <returns>width of street for the root</returns>
        private static float CalculateStreetWidth(IList<ILayoutNode> layoutNodes)
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
            UnityEngine.Assertions.Assert.IsTrue(numberOfLeaves > 0);
            result /= numberOfLeaves;
            // result is now the average length over all widths and depths of all leaf nodes.
            return result * streetWidthPercentage;
        }

        /// <summary>
        /// Creates the ENode tree hierarchy starting at given root node. The root has
        /// depth 0.
        /// </summary>
        /// <param name="root">root of the hierarchy</param>
        /// <returns>root ENode</returns>
        private static ENode GenerateHierarchy(ILayoutNode root, int depth = 0)
        {
            ENode result = ENodeFactory.Create(root);
            result.TreeDepth = depth;
            if (result is EInner inner)
            {
                foreach (ILayoutNode child in root.Children())
                {
                    inner.AddChild(GenerateHierarchy(child, depth + 1));
                }
            }
            return result;
        }
    }
}