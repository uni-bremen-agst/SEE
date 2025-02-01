using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Defines properties and methods for all nodes to be laid out regarding rendering.
    /// </summary>
    public interface IGameNode
    {
        /// <summary>
        /// A unique ID for a node.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// The local scale of a node (i.e., scale relative to its parent).
        /// </summary>
        Vector3 LocalScale { get; set; }

        /// <summary>
        /// The absolute scale of a node in world co-ordinates.
        ///
        /// Note: This value may be meaningful only if the node is not skewed.
        /// </summary>
        Vector3 AbsoluteScale { get; }

        /// <summary>
        /// Scales the node by the given <paramref name="factor"/>.
        /// </summary>
        /// <param name="factor">factory by which to scale the node</param>
        void ScaleBy(float factor);

        /// <summary>
        /// Center position of a node in world space.
        /// </summary>
        Vector3 CenterPosition { get; set; }

        /// <summary>
        /// Rotation around the y axis in degrees.
        /// </summary>
        float Rotation { get; set; }

        /// <summary>
        /// X-Z center position of the roof of the node in world space.
        /// </summary>
        Vector3 Roof { get; }

        /// <summary>
        /// X-Z center position of the ground of the node in world space.
        /// </summary>
        Vector3 Ground { get; }
    }
}
