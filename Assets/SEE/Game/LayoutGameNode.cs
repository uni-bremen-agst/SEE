using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.GO.NodeFactories;
using SEE.Layout;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Implementation of ILayoutNode. It is simply a wrapper to game objects
    /// created for inner nodes or leaf nodes.
    /// </summary>
    public class LayoutGameNode : AbstractLayoutNode
    {
        /// <summary>
        /// The node factory that created the game object. Required to obtain scaling information.
        /// </summary>
        private readonly NodeFactory nodeFactory;

        /// <summary>
        /// Constructor in cases where <paramref name="gameObject"/> is not a leaf and was created by
        /// a <paramref name="nodeFactory"/>.
        /// </summary>
        /// <param name="to_layout_node">the mapping of graph nodes onto LayoutNodes this node should be added to</param>
        /// <param name="gameObject">the game object this layout node represents</param>
        /// <param name="nodeFactory">the node factory that created <paramref name="gameObject"/>;
        /// may be null for inner nodes</param>
        public LayoutGameNode(Dictionary<Node, ILayoutNode> to_layout_node, GameObject gameObject, NodeFactory nodeFactory)
            : base(gameObject.GetComponent<NodeRef>().Value, to_layout_node)
        {
            this.gameObject = gameObject;
            this.nodeFactory = nodeFactory;
        }

        /// <summary>
        /// Yields the game object corresponding to this layout node.
        /// </summary>
        /// <returns>game object corresponding to this layout node</returns>
        public GameObject GetGameObject()
        {
            return gameObject;
        }

        /// <summary>
        /// The scale of this node.
        /// </summary>
        public override Vector3 LocalScale
        {
            get
            {
                return nodeFactory.GetSize(gameObject);
            }
            set
            {
                nodeFactory.SetSize(gameObject, value);
            }
        }

        /// <summary>
        /// The absolute scale of a node in world co-ordinates.
        ///
        /// Note: This value may be meaningful only if the node is not skewed.
        /// </summary>
        public override Vector3 AbsoluteScale
        {
            get => gameObject.transform.lossyScale;
        }

        /// <summary>
        /// Scales this node by the given <paramref name="factor"/>: its current
        /// Scale is multiplied by <paramref name="factor"/>. If the object
        /// contains a line, the line width is multiplied by <paramref name="factor"/>, too.
        /// </summary>
        /// <param name="factor">factor by which to mulitply the scale</param>
        public override void ScaleBy(float factor)
        {
            LineRenderer renderer = gameObject.GetComponent<LineRenderer>();
            if (renderer != null)
            {
                // This object is drawn by a line. The width of the line must
                // be adjusted.
                renderer.startWidth *= factor;
                renderer.endWidth *= factor;
            }
            LocalScale *= factor;
        }

        /// <summary>
        /// The center position of this node in world space.
        /// </summary>
        public override Vector3 CenterPosition
        {
            get
            {
                return nodeFactory.GetCenterPosition(gameObject);
            }
            set
            {
                Vector3 groundPosition = value;
                Vector3 extent = nodeFactory.GetSize(gameObject) / 2.0f;
                groundPosition.y -= extent.y;
                nodeFactory.SetGroundPosition(gameObject, groundPosition);
            }
        }

        public override float Rotation
        {
            get => gameObject.transform.eulerAngles.y;
            set => gameObject.transform.Rotate(new Vector3(0, value, 0));
        }


        /// <summary>
        /// X-Z center position of the roof of the node in world space.
        /// </summary>
        public override Vector3 Roof
        {
            get
            {
                return nodeFactory.Roof(gameObject);
            }
        }

        /// <summary>
        /// X-Z center position of the ground of the node in world space.
        /// </summary>
        public override Vector3 Ground
        {
            get
            {
                return nodeFactory.Ground(gameObject);
            }
        }

        public override string ToString()
        {
            return "[" + base.ToString() + "]";
        }
    }
}