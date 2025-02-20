using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A simple implementation of ILayoutNode.
    /// </summary>
    public class LayoutVertex : ILayoutNode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initialSize">the size of the node</param>
        /// <param name="index">the unique ID of the node (a number to be converted into a string)</param>
        public LayoutVertex(Vector3 initialSize, int index)
        {
            AbsoluteScale = initialSize;
            this.id = index.ToString();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">unique ID of the node</param>
        public LayoutVertex(string id)
        {
            AbsoluteScale = Vector3.zero;
            this.id = id;
        }

        /// <summary>
        /// Unique ID of the node.
        /// </summary>
        private readonly string id;

        /// <summary>
        /// <see cref="IGameNode.ID"/>.
        /// </summary>
        public override string ID => id;

        #region IGameNode

        /// <summary>
        /// Implementation of <see cref="ILayoutNode.HasType(string)"/>.
        /// </summary>
        /// <param name="typeName">ignored</param>
        /// <returns>always false</returns>
        public override bool HasType(string typeName)
        {
            return false;
        }

        /// <summary>
        /// <see cref="IGameNode.AbsoluteScale"/>.
        /// </summary>
        public override Vector3 AbsoluteScale { set; get; }

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
        /// The center position of the node.
        /// </summary>
        public override Vector3 CenterPosition { set; get; }

        /// <summary>
        /// The X-Z center position of the node's roof.
        /// </summary>
        public override Vector3 Roof
        {
            get => CenterPosition + 0.5f * AbsoluteScale.y * Vector3.up;
        }

        /// <summary>
        /// The X-Z center position of the node's ground.
        /// </summary>
        public override Vector3 Ground
        {
            get => CenterPosition - 0.5f * AbsoluteScale.y * Vector3.up;
        }

        /// <summary>
        /// The rotation of the node along the y axis in degrees.
        /// </summary>
        public override float Rotation { set; get; }

        #endregion IGameNode
    }
}
