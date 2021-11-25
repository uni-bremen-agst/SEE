using SEE.DataModel.DG;
using SEE.Layout;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Implementation of ILayoutNode wrapping a graph node and its layout
    /// information. It is used by the EvolutionRenderer in the upfront calculation
    /// of layouts when the layout information should not be applied immediately to
    /// game objects.
    /// </summary>
    public class LayoutGraphNode : AbstractLayoutNode
    {
        /// <summary>
        /// Constructor setting the graph <paramref name="node"/> corresponding to this layout node
        /// and the <paramref name="to_layout_node"/> mapping. The mapping maps all graph nodes to be
        /// laid out onto their corresponding layout node and is shared among all layout nodes.
        /// The given <paramref name="node"/> will be added to <paramref name="to_layout_node"/>.
        /// </summary>
        /// <param name="node">graph node corresponding to this layout node</param>
        /// <param name="to_layout_node">the mapping of graph nodes onto LayoutNodes this node should be added to</param>
        public LayoutGraphNode(Node node, Dictionary<Node, ILayoutNode> to_layout_node)
            : base(node, to_layout_node)
        { }

        /// <summary>
        /// The scale of the node (both local and absolute scale; there is
        /// no distinction here).
        /// </summary>
        private Vector3 scale;

        /// <summary>
        /// <see cref="AbstractLayoutNode.LocalScale"/>.
        /// </summary>
        public override Vector3 LocalScale
        {
            get => scale;
            set => scale = value;
        }

        /// <summary>
        /// <see cref="AbstractLayoutNode.AbsoluteScale"/>.
        /// </summary>
        public override Vector3 AbsoluteScale
        {
            get => scale;
        }

        /// <summary>
        /// <see cref="AbstractLayoutNode.ScaleBy"/>.
        /// </summary>
        /// <param name="factor">factor by which to scale the node</param>
        public override void ScaleBy(float factor)
        {
            scale *= factor;
        }

        /// <summary>
        /// The position of the center of the node.
        /// </summary>
        private Vector3 centerPosition;

        /// <summary>
        /// <see cref="AbstractLayoutNode.CenterPosition"/>.
        /// </summary>
        public override Vector3 CenterPosition
        {
            get => centerPosition;
            set => centerPosition = value;
        }

        /// <summary>
        /// The rotation of the node along the y axis.
        /// </summary>
        private float rotation;

        /// <summary>
        /// <see cref="AbstractLayoutNode.Rotation"/>.
        /// </summary>
        public override float Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        /// <summary>
        /// <see cref="AbstractLayoutNode.Roof"/>.
        /// </summary>
        public override Vector3 Roof
        {
            get => centerPosition + Vector3.up * 0.5f * scale.y;
        }

        /// <summary>
        /// <see cref="AbstractLayoutNode.Ground"/>.
        /// </summary>
        public override Vector3 Ground
        {
            get => centerPosition - Vector3.up * 0.5f * scale.y;
        }
    }
}