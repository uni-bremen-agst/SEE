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
        /// See <see cref="IGameNode.AbsoluteScale"/>.
        /// </summary>
        public override Vector3 AbsoluteScale
        {
            get
            {
                return GameObject.transform.lossyScale;
            }
            set
            {
                Transform parent = GameObject.transform.parent;
                GameObject.transform.SetParent(null);
                GameObject.transform.localScale = value;
                GameObject.transform.SetParent(parent);
            }
        }

        /// <summary>
        /// <see cref="IGameNode.ScaleXZBy(float)"/>.
        /// </summary>
        public override void ScaleXZBy(float factor)
        {
            Vector3 result = AbsoluteScale;
            result.x *= factor;
            result.z *= factor;
            AbsoluteScale = result;
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

        /// <summary>
        /// See <see cref="IGameNode.Rotation"/>.
        /// </summary>
        public override float Rotation
        {
            get => GameObject.transform.eulerAngles.y;
            set => GameObject.transform.Rotate(new Vector3(0, value, 0));
        }


        /// <summary>
        /// See <see cref="IGameNode.Roof"/>.
        /// </summary>
        public override Vector3 Roof
        {
            get
            {
                return GameObject.GetRoofCenter();
            }
        }

        /// <summary>
        /// See <see cref="IGameNode.Ground"/>.
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
