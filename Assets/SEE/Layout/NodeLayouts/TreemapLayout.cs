using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
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
    public class TreemapLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor. The width and depth are assumed to be in Unity units.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="width">width of the rectangle in which to place all nodes in Unity units</param>
        /// <param name="depth">width of the rectangle in which to place all nodes in Unity units</param>
        public TreemapLayout(float groundLevel,
                             float width,
                             float depth)
        : base(groundLevel)
        {
            name = "Treemap";
            this.width = width;
            this.depth = depth;
        }

        /// <summary>
        /// The width of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float width;

        /// <summary>
        /// The depth of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float depth;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private Dictionary<ILayoutNode, NodeTransform> layout_result;

        /// <summary>
        /// Return a treemap layout where are all nodes are fit into a given rectangle
        /// with the width and depth passed to the constructor. The width and depth of
        /// the original layout nodes will be scaled to fit into the rectangle, but
        /// the height will remain the same as in the input.
        /// </summary>
        /// <param name="layoutNodes">nodes to be laid out</param>
        /// <returns>treemap layout scaled in x and z axes</returns>
        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes)
        {
            layout_result = new Dictionary<ILayoutNode, NodeTransform>();

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
                        UnityEngine.Assertions.Assert.AreEqual(gameNode.AbsoluteScale, gameNode.LocalScale);
                        layout_result[gameNode] = new NodeTransform(Vector3.zero,
                            new Vector3(width, gameNode.LocalScale.y, depth));
                    }
                    else
                    {
                        Assert.IsTrue(false, "We should never arrive here.\n");
                    }

                    break;
                }
                default:
                    roots = LayoutNodes.GetRoots(layoutNodeList);
                    CalculateSize();
                    CalculateLayout();
                    break;
            }

            return layout_result;
        }

        /// <summary>
        /// Adds positioning and scales to <see cref="layout_result"/> for all root nodes (nodes with no parent)
        /// within a rectangle whose center position is Vector3.zero and whose width and depth is
        /// as specified by the constructor call. This function is then called recursively for the
        /// children of each root (until leaves are reached).
        /// </summary>
        private void CalculateLayout()
        {
            /// Our "logical" rectangle in which to put the whole treemap is assumed to have its
            /// center at Vector3.zero here. <see cref="CalculateLayout(ICollection{ILayoutNode}, float, float, float, float)"/>
            /// assumes the rectangle's location be specified by its left front corner.
            /// Hence, we need to transform the center of the "logical" rectangle to the left front
            /// corner of the rectangle by -width/2 and -depth/2, respectively.
            if (roots.Count == 1)
            {
                ILayoutNode root = roots[0];
                UnityEngine.Assertions.Assert.AreEqual(root.AbsoluteScale, root.LocalScale);
                layout_result[root] = new NodeTransform(Vector3.zero,
                                                        new Vector3(width, root.LocalScale.y, depth));
                CalculateLayout(root.Children(), x: -width / 2.0f, z: -depth / 2.0f, width, depth);
            }
            else
            {
                CalculateLayout(roots, x: -width / 2.0f, z: -depth / 2.0f, width, depth);
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
            float padding = Mathf.Min(width, depth) * 0.01f;
            List<RectangleTiling.Rectangle> rects = RectangleTiling.Squarified_Layout_With_Padding(sizes, x, z, width, depth, padding);
            Add_To_Layout(sizes, rects);

            foreach (ILayoutNode node in siblings)
            {
                ICollection<ILayoutNode> children = node.Children();
                if (children.Count > 0)
                {
                    // Note: nodeTransform.position is the center position, while
                    // CalculateLayout assumes co-ordinates x and z as the left front corner
                    UnityEngine.Assertions.Assert.AreEqual(node.AbsoluteScale, node.LocalScale);
                    NodeTransform nodeTransform = layout_result[node];
                    CalculateLayout(children,
                                    nodeTransform.position.x - nodeTransform.scale.x / 2.0f,
                                    nodeTransform.position.z - nodeTransform.scale.z / 2.0f,
                                    nodeTransform.scale.x,
                                    nodeTransform.scale.z);
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
            float total_size = 0.0f;
            foreach (ILayoutNode root in roots)
            {
                total_size += CalculateSize(root);
            }
            return total_size;
        }

        /// <summary>
        /// The size metric of each node. The area of the rectangle is proportional to a node's size.
        /// </summary>
        private readonly Dictionary<ILayoutNode, RectangleTiling.NodeSize> sizes
            = new Dictionary<ILayoutNode, RectangleTiling.NodeSize>();

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
                float total_size = 0.0f;
                foreach (ILayoutNode child in node.Children())
                {
                    total_size += CalculateSize(child);
                }
                sizes[node] = new RectangleTiling.NodeSize(node, total_size);
                return total_size;
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
            List<RectangleTiling.NodeSize> result = new List<RectangleTiling.NodeSize>();
            foreach (ILayoutNode node in nodes)
            {
                result.Add(sizes[node]);
            }
            return result;
        }

        /// <summary>
        /// Adds the transforms (position, scale) of the game objects (nodes) according to their
        /// corresponding rectangle in rects to <see cref="layout_result"/>.
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
        private void Add_To_Layout
           (List<RectangleTiling.NodeSize> nodes,
            List<RectangleTiling.Rectangle> rects)
        {
            int i = 0;
            foreach (RectangleTiling.Rectangle rect in rects)
            {
                ILayoutNode o = nodes[i].gameNode;
                Vector3 position = new Vector3(rect.x + rect.width / 2.0f, groundLevel, rect.z + rect.depth / 2.0f);
                Vector3 scale = new Vector3(rect.width, o.LocalScale.y, rect.depth);
                UnityEngine.Assertions.Assert.AreEqual(o.AbsoluteScale, o.LocalScale);
                layout_result[o] = new NodeTransform(position, scale);
                i++;
            }
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout
            (ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges,
             ICollection<SublayoutLayoutNode> sublayouts)
        {
            throw new NotImplementedException();
        }

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false;
        }
    }
}
