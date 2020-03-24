using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;
using SEE.GO;
using SEE.Layout;

namespace SEE.Game
{
    /// <summary>
    /// Implementation of ILayoutNode. It is simply a wrapper to game objects
    /// created for inner nodes or leaf nodes.
    /// </summary>
    public class GameNode : AbstractLayoutNode
    {
        /// <summary>
        /// The game object his layout node encapsulates.
        /// </summary>
        private readonly GameObject gameObject;
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
        public GameNode(Dictionary<Node, ILayoutNode> to_layout_node, GameObject gameObject, NodeFactory nodeFactory)
            : base(gameObject.GetComponent<NodeRef>().node, to_layout_node)
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
        public override Vector3 Scale
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
    }
}