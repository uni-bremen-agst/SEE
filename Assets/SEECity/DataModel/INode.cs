using System.Collections.Generic;

namespace SEE
{
    public interface INode : IGraphElement
    {
        /// <summary>
        /// The unique identifier of a node.
        /// </summary>
        string LinkName { get; set; }

        /// <summary>
        /// The name of the node (which is not necessarily unique).
        /// </summary>
        string SourceName { get; set; }

        /// <summary>
        /// The ancestor of the node in the hierarchy. May be null if the node
        /// is a root.
        /// </summary>
        INode Parent { get; set; }

        /// <summary>
        /// The number of descendants of this node.
        /// </summary>
        /// <returns>number of descendants</returns>
        int NumberOfChildren();

        /// <summary>
        /// The descendants of the node. 
        /// Note: This is not a copy. Do not modify the result.
        /// </summary>
        /// <returns>descendants of the node</returns>
        List<INode> Children();

        /// <summary>
        /// Add given node as a descendant of the node in the hierarchy.
        /// The same node must not be added more than once.
        /// </summary>
        /// <param name="child">descendant to be added to node</param>
        void AddChild(INode child);
    }
}