using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.CirclePacking;
using SEE.Layout.NodeLayouts.Cose;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// This layout packs circles closely together as a set of nested circles to decrease 
    /// the total area of city.
    /// </summary>
    public class CirclePackingNodeLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        public CirclePackingNodeLayout(float groundLevel)
            : base(groundLevel)
        {
            name = "Circle Packing";
        }

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private Dictionary<ILayoutNode, NodeTransform> layoutResult;

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes)
        {
            layoutResult = new Dictionary<ILayoutNode, NodeTransform>();

            ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root node.");
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }
            else
            {
                // exactly one root
                ILayoutNode root = roots.FirstOrDefault();
                float outRadius = PlaceNodes(root);
                Vector3 position = new Vector3(0.0f, groundLevel, 0.0f);
                layoutResult[root] = new NodeTransform(position,
                                                        GetScale(root, outRadius));
                MakeGlobal(layoutResult, position, root.Children());
                return layoutResult;
            }
        }

        /// <summary>
        /// "Globalizes" the layout. Initially, the position of children are assumed to be 
        /// relative to their parent, where the parent has position Vector3.zero. This 
        /// function adjusts the co-ordinates of all nodes to the world's co-ordinates.
        /// We also adjust the ground level of each inner node by its level lift.
        /// </summary>
        /// <param name="layoutResult">the layout to be adjusted</param>
        /// <param name="position">the position of the parent of all children</param>
        /// <param name="children">the children to be laid out</param>
        private static void MakeGlobal
            (Dictionary<ILayoutNode, NodeTransform> layoutResult,
             Vector3 position,
             ICollection<ILayoutNode> children)
        {
            foreach (ILayoutNode child in children)
            {
                NodeTransform childTransform = layoutResult[child];
                if (!child.IsLeaf)
                {
                    // The inner nodes will be slightly lifted along the y axis according to their
                    // tree depth so that they can be stacked visually (level 0 is at the bottom).
                    position.y += LevelLift(child);
                }
                childTransform.position += position;
                layoutResult[child] = childTransform;
                MakeGlobal(layoutResult, childTransform.position, child.Children());
            }
        }

        /// <summary>
        /// Places all children of the given parent node (recursively for all descendants
        /// of the given parent).
        /// </summary>
        /// <param name="parent">node whose descendants are to be placed</param>
        /// <returns>the radius required for a circle represent parent</returns>
        private float PlaceNodes(ILayoutNode parent)
        {
            ICollection<ILayoutNode> children = parent.Children();

            if (children.Count == 0)
            {
                // No scaling for leaves because they are already scaled.
                // Position Vector3.zero because they are located relative to their parent.
                // This position may be overridden later in the context of parent's parent.
                return LeafRadius(parent);
            }
            else
            {
                List<Circle> circles = new List<Circle>(children.Count);

                int i = 0;
                foreach (ILayoutNode child in children)
                {
                    float radius = child.IsLeaf ? LeafRadius(child) : PlaceNodes(child);
                    // Position the children on a circle as required by CirclePacker.Pack.
                    float radians = (i / (float)children.Count) * (2.0f * Mathf.PI);
                    circles.Add(new Circle(child, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
                    i++;
                }

                // The co-ordinates returned in circles are local, that is, relative to the zero center.
                // The resulting center and outOuterRadius relate to the circle object comprising
                // the children we just packed. By necessity, the objects whose children we are
                // currently processing is a composed object represented by a circle, otherwise
                // we would not have any children here.
                CirclePacker.Pack(0.1f, circles, out float outOuterRadius);

                if (children.Count == 1 && !children.ElementAt(0).IsLeaf)
                {
                    outOuterRadius *= 1.2f;
                }

                foreach (Circle circle in circles)
                {
                    // Note: The position of the transform is currently only local, relative to the zero center
                    // within the parent node. The co-ordinates will later be adjusted to the world scope.
                    layoutResult[circle.gameObject]
                         = new NodeTransform(new Vector3(circle.center.x, groundLevel, circle.center.y),
                                             GetScale(circle.gameObject, circle.radius));
                }
                return outOuterRadius;
            }
        }

        /// <summary>
        /// Returns the scaling vector for given node. If the node is a leaf, it will be the node's original size
        /// because leaves are not scaled at all. We do not want to change their size. Its predetermined
        /// by the client of this class. If the node is not a leaf, it will be represented by a circle
        /// whose scaling in x and y axes is twice the given radius (we do not scale along the y axis,
        /// hence, co-ordinate y of the resulting vector is always circleHeight.
        /// </summary>
        /// <param name="node">game node whose size is to be determined</param>
        /// <param name="radius">the radius for the game node if it is an inner node</param>
        /// <returns>the scale of <paramref name="node"/></returns>
        private Vector3 GetScale(ILayoutNode node, float radius)
        {
            return node.IsLeaf ? node.LocalScale
                               : new Vector3(2 * radius, innerNodeHeight, 2 * radius);
        }

        /// <summary>
        /// Yields the radius of the minimal circle containing the given block.
        /// 
        /// Precondition: node must be a leaf node, a block generated by blockFactory.
        /// </summary>
        /// <param name="block">block whose radius is required</param>
        /// <returns>radius of the minimal circle containing the given block</returns>
        private float LeafRadius(ILayoutNode block)
        {
            Vector3 extent = block.LocalScale / 2.0f;
            return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
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
