using SEE.GO;
using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// The abstract super class of all hierarchical node layouts.
    /// </summary>
    public abstract class HierarchicalNodeLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        public HierarchicalNodeLayout
            (float groundLevel,
             NodeFactory leafNodeFactory)
            : base(groundLevel, leafNodeFactory)
        {
        }

        /// <summary>
        /// Always true because hierarchical layouts can handle both inner nodes and leaves.
        /// </summary>
        /// <returns>always true</returns>
        public override bool IsHierarchical()
        {
            return true;
        }

        /// <summary>
        /// The roots of the subtrees of the original graph that are to be laid out.
        /// A node is considered a root if it has either no parent in the original
        /// graph or its parent is not contained in the set of nodes to be laid out.
        /// </summary>
        protected IList<LayoutNode> roots;
    }
}
