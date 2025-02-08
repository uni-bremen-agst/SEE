using SEE.Layout.NodeLayouts.TreeMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// Yields a squarified treemap node layout according to the algorithm
    /// described by Bruls, Huizing, van Wijk, "Squarified Treemaps".
    /// pp. 33-42, Eurographics / IEEE VGTC Symposium on Visualization, 2000.
    /// </summary>
    public class TreemapLayout : NodeLayout
    {
        static TreemapLayout()
        {
            Name = "Treemap";
        }

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private Dictionary<ILayoutNode, NodeTransform> layoutResult;

        protected override Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> layoutNodes,
             Vector3 centerPosition,
             Vector2 rectangle)
        {
            layoutResult = new Dictionary<ILayoutNode, NodeTransform>();

            IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
            switch (layoutNodeList.Count)
            {
                case 0:
                    throw new ArgumentException("No nodes to be laid out.");
                case 1:
                {
                    using IEnumerator<ILayoutNode> enumerator = layoutNodeList.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        // MoveNext() must be called before we can call Current.
                        ILayoutNode gameNode = enumerator.Current;
                        Assert.AreEqual(gameNode.AbsoluteScale, gameNode.LocalScale);
                        layoutResult[gameNode] = new NodeTransform(0, 0,
                                                                   new Vector3(rectangle.x, gameNode.AbsoluteScale.y, rectangle.y));
                    }
                    else
                    {
                        Assert.IsTrue(false, "We should never arrive here.\n");
                    }

                    break;
                }
                default:
                    Roots = LayoutNodes.GetRoots(layoutNodeList);
                    CalculateSize();
                    CalculateLayout(rectangle);
                    break;
            }

            return layoutResult;
        }

        /// <summary>
        /// Adds positioning and scales to <see cref="layoutResult"/> for all root nodes (nodes with no parent)
        /// within a rectangle whose center position is Vector3.zero and whose width and depth is
        /// as specified by the constructor call. This function is then called recursively for the
        /// children of each root (until leaves are reached).
        /// </summary>
        private void CalculateLayout(Vector2 rectangle)
        {
            /// Our "logical" rectangle in which to put the whole treemap is assumed to have its
            /// center at Vector3.zero here. <see cref="CalculateLayout(ICollection{ILayoutNode}, float, float, float, float)"/>
            /// assumes the rectangle's location be specified by its left front corner.
            /// Hence, we need to transform the center of the "logical" rectangle to the left front
            /// corner of the rectangle by -width/2 and -depth/2, respectively.
            if (Roots.Count == 1)
            {
                ILayoutNode root = Roots[0];
                Assert.AreEqual(root.AbsoluteScale, root.LocalScale);
                layoutResult[root] = new NodeTransform(0, 0,
                                                       new Vector3(rectangle.x, root.AbsoluteScale.y, rectangle.y));
                CalculateLayout(root.Children(), x: -rectangle.x / 2.0f, z: -rectangle.y / 2.0f, rectangle.x, rectangle.y);
            }
            else
            {
                CalculateLayout(Roots, x: -rectangle.x / 2.0f, z: -rectangle.y / 2.0f, rectangle.x, rectangle.y);
            }
        }

        /// <summary>
        /// Adds positioning and scaling to layout_result for all given siblings (children of the same
        /// immediate parent in the node tree) within a rectangle with left front corner (x, z) and
        /// given width and depth. This function is then called recursively for the children of the
        /// given siblings.
        /// </summary>
        /// <param name="siblings">children of the same immediate parent in the node tree</param>
        /// <param name="x">x co-ordinate of the left front corner of the rectangle in which to place the nodes</param>
        /// <param name="z">z co-ordinate of the left front corner of the rectangle</param>
        /// <param name="width">width of the rectangle in which to fit the nodes</param>
        /// <param name="depth">depth of the rectangle in which to fit the nodes</param>
        private void CalculateLayout(ICollection<ILayoutNode> siblings, float x, float z, float width, float depth)
        {
            List<RectangleTiling.NodeSize> sizes = GetSizes(siblings);
            float padding = Padding(width, depth);
            List<RectangleTiling.Rectangle> rects = RectangleTiling.SquarifiedLayoutWithPadding(sizes, x, z, width, depth, padding);
            AddToLayout(sizes, rects);

            foreach (ILayoutNode node in siblings)
            {
                ICollection<ILayoutNode> children = node.Children();
                if (children.Count > 0)
                {
                    // Note: nodeTransform.position is the center position, while
                    // CalculateLayout assumes co-ordinates x and z as the left front corner
                    Assert.AreEqual(node.AbsoluteScale, node.LocalScale);
                    NodeTransform nodeTransform = layoutResult[node];
                    CalculateLayout(children,
                                    nodeTransform.X - nodeTransform.Scale.x / 2.0f,
                                    nodeTransform.Z - nodeTransform.Scale.z / 2.0f,
                                    nodeTransform.Scale.x,
                                    nodeTransform.Scale.z);
                }
            }
        }

        /// <summary>
        /// Calculates the size of all nodes. The size of a leaf is the maximum of
        /// its width and depth. The size of an inner node is the sum of the sizes
        /// of all its children.
        ///
        /// The sizes of all <see cref="roots"/> and all their descendants are
        /// stored in <see cref="sizes"/>.
        /// </summary>
        /// <returns>total size of all node</returns>
        private float CalculateSize()
        {
            float totalSize = 0.0f;
            foreach (ILayoutNode root in Roots)
            {
                totalSize += CalculateSize(root);
            }
            return totalSize;
        }

        /// <summary>
        /// The size metric of each node. The area of the rectangle is proportional to a node's size.
        /// </summary>
        private readonly Dictionary<ILayoutNode, RectangleTiling.NodeSize> sizes = new();

        /// <summary>
        /// Calculates the size of node and all its descendants. The size of a leaf
        /// is the maximum of its width and depth. The size of an inner node is the
        /// sum of the sizes of all its children.
        ///
        /// The size of <see cref="node"/> and all its descendants is stored in <see cref="sizes"/>.
        /// </summary>
        /// <param name="node">node whose size it to be determined</param>
        /// <returns>size of <see cref="node"/></returns>
        private float CalculateSize(ILayoutNode node)
        {
            if (node.IsLeaf)
            {
                // a leaf
                Vector3 size = node.LocalScale;
                // x and z lengths may differ; we need to consider the larger value
                float result = Mathf.Max(size.x, size.z);
                sizes[node] = new RectangleTiling.NodeSize(node, result);
                return result;
            }
            else
            {
                float totalSize = 0.0f;
                foreach (ILayoutNode child in node.Children())
                {
                    totalSize += CalculateSize(child);
                }
                sizes[node] = new RectangleTiling.NodeSize(node, totalSize);
                return totalSize;
            }
        }

        /// <summary>
        /// Returns the list of node area sizes; one for each node in nodes as
        /// defined in sizes.
        /// </summary>
        /// <param name="nodes">list of nodes whose sizes are to be determined</param>
        /// <returns>list of node area sizes</returns>
        private List<RectangleTiling.NodeSize> GetSizes(ICollection<ILayoutNode> nodes)
        {
            List<RectangleTiling.NodeSize> result = new();
            foreach (ILayoutNode node in nodes)
            {
                result.Add(sizes[node]);
            }
            return result;
        }

        /// <summary>
        /// Adds the transforms (position, scale) of the game objects (nodes) according to their
        /// corresponding rectangle in rects to <see cref="layoutResult"/>.
        ///
        /// The x and z co-ordinates for the resulting <see cref="NodeTransform"/> are determined
        /// by the rectangles, but the y co-ordinate is the original value of the input
        /// <see cref="ILayoutNode"/> (local scale).
        ///
        /// Precondition: For every i in the range of nodes: rects[i] is the transform
        /// corresponding to nodes[i].
        /// </summary>
        /// <param name="nodes">the game nodes</param>
        /// <param name="rects">their corresponding rectangle</param>
        private void AddToLayout
           (List<RectangleTiling.NodeSize> nodes,
            List<RectangleTiling.Rectangle> rects)
        {
            int i = 0;
            foreach (RectangleTiling.Rectangle rect in rects)
            {
                ILayoutNode o = nodes[i].GameNode;
                Vector3 scale = new(rect.Width, o.LocalScale.y, rect.Depth);
                Assert.AreEqual(o.AbsoluteScale, o.LocalScale, $"{o.ID}: {o.AbsoluteScale} != {o.LocalScale}");
                layoutResult[o] = new NodeTransform(rect.X + rect.Width / 2.0f, rect.Z + rect.Depth / 2.0f, scale);
                i++;
            }
        }
    }
}
