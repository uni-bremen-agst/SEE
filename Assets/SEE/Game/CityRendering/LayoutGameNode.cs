using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Layout;
using UnityEngine;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Implementation of ILayoutNode. It is simply a wrapper to game objects
    /// created for inner nodes or leaf nodes.
    /// </summary>
    public class LayoutGameNode : AbstractLayoutNode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="toLayoutNode">the mapping of graph nodes onto <see cref="ILayoutNode"/>s
        /// this node should be added to</param>
        /// <param name="gameObject">the game object this layout node represents</param>
        public LayoutGameNode(IDictionary<Node, ILayoutNode> toLayoutNode, GameObject gameObject)
            : base(gameObject.GetComponent<NodeRef>().Value, toLayoutNode)
        {
            this.gameObject = gameObject;
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
        /// The local scale of this node.
        /// </summary>
        public override Vector3 LocalScale
        {
            get
            {
                return gameObject.transform.localScale;
            }
            set
            {
                gameObject.transform.localScale = value;
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
                return gameObject.transform.position;
            }
            set
            {
                gameObject.transform.position = value;
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
                return gameObject.GetRoofCenter();
            }
        }

        /// <summary>
        /// X-Z center position of the ground of the node in world space.
        /// </summary>
        public override Vector3 Ground
        {
            get
            {
                return gameObject.GetGroundCenter();
            }
        }

        public override string ToString()
        {
            return "[" + base.ToString() + "]";
        }
    }
}