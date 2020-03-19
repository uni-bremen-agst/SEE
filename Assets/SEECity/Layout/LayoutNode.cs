using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    ///  Defines the methods for all nodes to be laid out.
    /// </summary>
    public interface LayoutNode
    {
        /// <summary>
        /// Yields true if the given node is to be interpreted as a leaf by the layouter.
        /// 
        /// Note: Even leaves may have children. What to do with those is the decision of the
        /// layouter. It may or may not lay them out.
        /// </summary>
        /// <returns>true if the given node is to be interpreted as a leaf by the layouter</returns>
        bool IsLeaf();

        /// <summary>
        /// A unique ID for a node.
        /// </summary>
        /// <returns>unique ID for a node</returns>
        string LinkName();

        /// <summary>
        /// Yields the scale of a node.
        /// </summary>
        /// <returns>scale of node</returns>
        Vector3 GetSize();

        //Vector3 Position();
        //ICollection<LayoutNode> Children();
    }
}