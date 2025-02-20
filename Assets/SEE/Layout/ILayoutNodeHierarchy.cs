using System.Collections.Generic;
using System.Linq;

namespace SEE.Layout
{
    /// <summary>
    /// Utility methods for <see cref="ILayoutNode"/> hierarchies.
    /// </summary>
    public static class ILayoutNodeHierarchy
    {
        /// <summary>
        /// Returns all nodes in <paramref name="layoutNodes"/> that do not have a parent.
        /// </summary>
        /// <param name="layoutNodes">nodes to be queried</param>
        /// <returns>all root nodes in <paramref name="layoutNodes"/></returns>
        public static ICollection<ILayoutNode> Roots(ICollection<ILayoutNode> layoutNodes)
        {
            return layoutNodes.Where(node => node.Parent == null).ToList();
        }

        /// <summary>
        /// Returns all transitive descendants of <paramref name="parent"/> including <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">the root of the hierarchy</param>
        /// <returns>all transitive descendants of <paramref name="parent"/> including <paramref name="parent"/></returns>
        public static ICollection<ILayoutNode> DescendantsOf(ILayoutNode parent)
        {
            List<ILayoutNode> descendants = new();

            DFS(parent);

            return descendants;

            void DFS(ILayoutNode node)
            {
                descendants.Add(node);
                foreach (ILayoutNode child in node.Children())
                {
                    DFS(child);
                }
            }
        }
    }
}
