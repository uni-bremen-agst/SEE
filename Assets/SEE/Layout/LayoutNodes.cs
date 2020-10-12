using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// Utilities for collections of ILayoutNodes (levels and roots).
    /// </summary>
    public static class LayoutNodes
    {
        /// <summary>
        /// Sets the level of each node (node.Level) in <paramref name="layoutNodes"/>. A root has level 0,
        /// for every other node the level is its distance to its root.
        /// </summary>
        /// <param name="layoutNodes">nodes whose level is to be set</param>
        public static void SetLevels(ICollection<ILayoutNode> layoutNodes)
        {
            foreach (ILayoutNode root in GetRoots(layoutNodes))
            {
                root.Level = 0;
                foreach (ILayoutNode child in root.Children())
                {
                    SetLevels(child, 1);
                }
            }
        }

        /// <summary>
        /// Sets the level of the given <paramref name="node"/> to the given <paramref name="level"/>
        /// and recurses to its children with the <paramref name="level"/> increased by one.
        /// </summary>
        /// <param name="node">node whose level is to be set (node.Level)</param>
        /// <param name="level">level to set</param>
        public static void SetLevels(ILayoutNode node, int level)
        {
            node.Level = level;
            foreach (ILayoutNode child in node.Children())
            {
                SetLevels(child, level + 1);
            }
        }

        /// <summary>
        /// Returns all root nodes in <paramref name="layoutNodes"/>. A node is a root node
        /// if its Parent is null.
        /// </summary>
        /// <param name="layoutNodes">layout nodes for which to collect all roots</param>
        /// <returns>list of root nodes</returns>
        public static IList<ILayoutNode> GetRoots(ICollection<ILayoutNode> layoutNodes)
        {
            IList<ILayoutNode> roots = new List<ILayoutNode>();
            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                if (layoutNode.Parent == null)
                {
                    roots.Add(layoutNode);
                }
            }
            return roots;
        }
    }
}