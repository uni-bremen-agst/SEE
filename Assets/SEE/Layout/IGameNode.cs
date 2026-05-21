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
        /// True if this node has a type with the given <paramref name="typeName"/>.
        /// </summary>
        /// <param name="typeName">Name of a node type.</param>
        /// <returns>True if this node has a type with the given <paramref name="typeName"/>.</returns>
        bool HasType(string typeName);

        /// <summary>
        /// The absolute scale of a node in world co-ordinates.
        ///
        /// Note: This value may be meaningful only if the node is not skewed.
        /// </summary>
        Vector3 AbsoluteScale { get; set; }

        /// <summary>
        /// Scales the width (x) and depth (z) of the node by the given <paramref name="factor"/>.
        /// The height will be maintained.
        /// </summary>
        /// <param name="factor">Factory by which to scale the width and depth of the node.</param>
        void ScaleXZBy(float factor);

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
