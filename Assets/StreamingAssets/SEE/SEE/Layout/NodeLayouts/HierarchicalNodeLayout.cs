using System.Collections.Generic;

namespace SEE.Layout.NodeLayouts
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
        public HierarchicalNodeLayout(float groundLevel)
            : base(groundLevel)
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
        protected IList<ILayoutNode> roots;

        /// <summary>
        /// Returns the maximal depth of the forest with the given root nodes.
        /// If roots.Count == 0, 0 is the maximal depth. If there is at least
        /// one root, the minimum value of the maximal depth is 1.
        /// </summary>
        /// <param name="roots">set of root tree nodes of the forest</param>
        /// <returns>maximal depth of the forest</returns>
        protected static int MaxDepth(List<ILayoutNode> roots)
        {
            int result = 0;
            foreach (ILayoutNode root in roots)
            {
                int depth = MaxDepth(root);
                if (depth > result)
                {
                    result = depth;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the maximal depth of the tree rooted by given node. The depth of
        /// a tree with only one node is 1.
        /// </summary>
        /// <param name="node">root node of the tree</param>
        /// <returns>maximal depth of the tree</returns>
        protected static int MaxDepth(ILayoutNode node)
        {
            int result = 0;
            foreach (ILayoutNode child in node.Children())
            {
                int depth = MaxDepth(child);
                if (depth > result)
                {
                    result = depth;
                }
            }
            return result + 1;
        }
    }
}
