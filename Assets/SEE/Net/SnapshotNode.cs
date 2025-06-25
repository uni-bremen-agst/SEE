using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Represents a node in the SEE network.
    /// </summary>
    public class SnapshotNode
    {
        /// <summary>
        /// Gets the unique identifier of the node.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// The absolute scale of a node in world co-ordinates.
        ///
        /// Note: This value may be meaningful only if the node is not skewed.
        /// </summary>
        public Vector3 AbsoluteScale { get; set; }

        /// <summary>
        /// See <see cref="IGameNode.CenterPosition"/>.
        /// </summary>
        public Vector3 CenterPosition { get; set; }

        /// <summary>
        /// See <see cref="IGameNode.Rotation"/>.
        /// </summary>
        public float Rotation { get; set; }

    }
}
