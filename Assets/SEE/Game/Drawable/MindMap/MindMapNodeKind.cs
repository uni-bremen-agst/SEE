using System;

namespace SEE.Game.Drawable.MindMap
{
    /// <summary>
    /// Represents the kind of a Mind Map node.
    /// </summary>
    [Serializable]
    public enum MindMapNodeKind
    {
        /// <summary>
        /// A root node of a Mind Map.
        /// A theme does not have a parent.
        /// </summary>
        Theme,

        /// <summary>
        /// An intermediate Mind Map node that can have a parent and child nodes.
        /// </summary>
        Subtheme,

        /// <summary>
        /// A terminal Mind Map node that cannot act as a parent for other nodes.
        /// </summary>
        Leaf
    }
}
