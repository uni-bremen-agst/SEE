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
        /// <param name="gameObject">the game object this layout node represents</param>
        public LayoutGameNode(GameObject gameObject)
            : base(gameObject.GetComponent<NodeRef>().Value)
        {
            GameObject = gameObject;
        }

        /// <summary>
        /// Yields the game object corresponding to this layout node.
        /// </summary>
        /// <returns>game object corresponding to this layout node</returns>
        public GameObject GetGameObject()
        {
            return GameObject;
        }

        /// <summary>
        /// The local scale of this node.
        /// </summary>
        public override Vector3 LocalScale
        {
            get
            {
                return GameObject.transform.localScale;
            }
            set
            {
                GameObject.transform.localScale = value;
            }
        }

        /// <summary>
        /// The absolute scale of a node in world co-ordinates.
        ///
        /// Note: This value may be meaningful only if the node is not skewed.
        /// </summary>
        public override Vector3 AbsoluteScale
        {
            get => GameObject.transform.lossyScale;
        }

        /// <summary>
        /// <see cref="IGameNode.ScaleXZBy(float)"/>.
        /// </summary>
        public override void ScaleXZBy(float factor)
        {
            Vector3 result = LocalScale;
            result.x *= factor;
            result.z *= factor;
            LocalScale = result;
        }

        /// <summary>
        /// The center position of this node in world space.
        /// </summary>
        public override Vector3 CenterPosition
        {
            get
            {
                return GameObject.transform.position;
            }
            set
            {
                GameObject.transform.position = value;
            }
        }

        public override float Rotation
        {
            get => GameObject.transform.eulerAngles.y;
            set => GameObject.transform.Rotate(new Vector3(0, value, 0));
        }


        /// <summary>
        /// X-Z center position of the roof of the node in world space.
        /// </summary>
        public override Vector3 Roof
        {
            get
            {
                return GameObject.GetRoofCenter();
            }
        }

        /// <summary>
        /// X-Z center position of the ground of the node in world space.
        /// </summary>
        public override Vector3 Ground
        {
            get
            {
                return GameObject.GetGroundCenter();
            }
        }

        public override string ToString()
        {
            return "[" + base.ToString() + "]";
        }
    }
}