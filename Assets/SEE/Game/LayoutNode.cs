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
    public class LayoutNode : AbstractLayoutNode
    {
        /// <summary>
        /// Constructor setting the graph <paramref name="node"/> corresponding to this layout node
        /// and the <paramref name="to_layout_node"/> mapping. The mapping maps all graph nodes to be
        /// laid out onto their corresponding layout node and is shared among all layout nodes.
        /// The given <paramref name="node"/> will be added to <paramref name="to_layout_node"/>.
        /// </summary>
        /// <param name="node">graph node corresponding to this layout node</param>
        /// <param name="to_layout_node">the mapping of graph nodes onto LayoutNodes this node should be added to</param>
        public LayoutNode(Node node, Dictionary<Node, ILayoutNode> to_layout_node)
            : base(node, to_layout_node)
        { }

        private Vector3 scale;

        public override Vector3 LocalScale
        {
            get => scale;
            set => scale = value;
        }

        public override Vector3 AbsoluteScale
        {
            get => scale;
        }

        public override void ScaleBy(float factor)
        {
            scale *= factor;
        }

        private Vector3 centerPosition;

        public override Vector3 CenterPosition
        {
            get => centerPosition;
            set => centerPosition = value;
        }

        private float rotation;

        public override float Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        public override Vector3 Roof
        {
            get => centerPosition + Vector3.up * 0.5f * scale.y;
        }

        public override Vector3 Ground
        {
            get => centerPosition - Vector3.up * 0.5f * scale.y;
        }
    }
}