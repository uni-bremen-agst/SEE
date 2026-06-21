using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Representation of a leaf node for the EvoStreets.
    /// </summary>
    internal class ELeaf : ENode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">The leaf graph node represented by this <see cref="ENode"/>.</param>
        public ELeaf(ILayoutNode node) : base(node)
        {
        }

        /// <summary>
        /// Sets <see cref="Rectangle.Width"/> and <see cref="Rectangle.Depth"/> of this node
        /// to the absolute scale of its underlying <see cref="GraphNode"/>.
        /// For reasons of uniformity (unambiguous interpretation), the orientation
        /// of a leaf is always towards East/West, that is, its width metric is
        /// depicted uniformly along the x axis in world space.
        /// </summary>
        /// <param name="orientation">Will be ignored.</param>
        /// <param name="treeDescriptor">Will be ignored.</param>
        /// <param name="lastLayout">Will be ignored.</param>
        /// <param name="newNodes">Will be ignored.</param>
        /// <param name="existingNodes">Will be ignored.</param>
        public override void SetSizeAndDistribute
            (Orientation orientation,
            LayoutDescriptor treeDescriptor,
            Dictionary<ILayoutNode, NodeTransform> lastLayout,
            HashSet<ILayoutNode> newNodes,
            HashSet<ILayoutNode> existingNodes)
        {
            Rectangle.Width = GraphNode.AbsoluteScale.x;
            Rectangle.Depth = GraphNode.AbsoluteScale.z;
        }

        /// <summary>
        /// Sets <see cref="Center"/> to <paramref name="centerLocation"/>.
        /// </summary>
        /// <param name="orientation">Will be ignored.</param>
        /// <param name="centerLocation">The center location to be set.</param>
        public override void SetLocation(Orientation orientation, Location centerLocation)
        {
            Rectangle.Center = centerLocation;
        }

        /// <summary>
        /// Adds the layout information of this <see cref="ELeaf"/> to the <paramref name="layoutResult"/>.
        /// <seealso cref="ENode.ToLayout(ref Dictionary{ILayoutNode, NodeTransform}, float, float)"/>.
        /// </summary>
        /// <param name="layoutResult">Layout where to add the layout information.</param>
        /// <param name="streetHeight">Will be ignored.</param>
        public override void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layoutResult, float streetHeight)
        {
            /// A leaf will keep its original height (the one by <see cref="GraphNode"/>).
            /// We only adjust the width and depth according to the calculated layout.
            layoutResult[GraphNode] = new NodeTransform(Rectangle.Center.X, Rectangle.Center.Y,
                                                        new Vector3 (Rectangle.Width, GraphNode.AbsoluteScale.y, Rectangle.Depth),
                                                        0);
        }
    }
}
