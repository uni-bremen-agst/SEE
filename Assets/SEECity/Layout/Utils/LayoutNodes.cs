using System.Collections.Generic;

namespace SEE.Layout
{
    public static class LayoutNodes
    {
        /// <summary>
        /// Sets the level of each node (node.Level) in <paramref name="layoutNodes"/>. A root has level 1,
        /// for every other node the level is its distance to its root.
        /// </summary>
        /// <param name="layoutNodes">nodes whose level is to be set</param>
        public static void SetLevels(ICollection<LayoutNode> layoutNodes)
        {
            foreach (LayoutNode root in GetRoots(layoutNodes))
            {
                root.Level = 0;
                foreach (LayoutNode child in root.Children())
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
        public static void SetLevels(LayoutNode node, int level)
        {
            node.Level = level;
            foreach (LayoutNode child in node.Children())
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
        public static IList<LayoutNode> GetRoots(ICollection<LayoutNode> layoutNodes)
        {
            IList<LayoutNode> roots = new List<LayoutNode>();
            foreach (LayoutNode layoutNode in layoutNodes)
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