using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.DataModel.DG.GraphSearch;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Window.TreeWindow
{
    /// <summary>
    /// A group of graph elements in the tree window.
    /// </summary>
    /// <param name="Text">The text to display for this group.</param>
    /// <param name="IconGlyph">The icon to display for this group.</param>
    /// <param name="StartColor">The start color of the gradient to use for this group.</param>
    /// <param name="EndColor">The end color of the gradient to use for this group.</param>
    public record TreeWindowGroup(string Text, char IconGlyph, Color StartColor, Color EndColor)
    {
        /// <summary>
        /// Returns the color gradient to use for this group.
        /// </summary>
        public Color[] Gradient => new[] { StartColor, EndColor };
    }

    /// <summary>
    /// An assignment of graph elements to <see cref="TreeWindowGroup"/>s.
    /// </summary>
    public interface ITreeWindowGroupAssigment
    {
        /// <summary>
        /// Returns the group the given element belongs to.
        /// </summary>
        /// <param name="element">The element whose group shall be returned.</param>
        /// <returns>The group the given element belongs to.</returns>
        public TreeWindowGroup GroupFor(GraphElement element);

        /// <summary>
        /// Returns all groups that are available, ordered by the number of members (descending).
        /// </summary>
        public IEnumerable<TreeWindowGroup> AllGroups
        {
            get;
        }

        /// <summary>
        /// A dummy group assignment that always returns null.
        /// </summary>
        /// <returns>A dummy group assignment that always returns null.</returns>
        public static ITreeWindowGroupAssigment Dummy()
        {
            return new TreeWindowGroupAssigment<object>(new Dictionary<object, TreeWindowGroup>(), _ => null);
        }
    }

    /// <summary>
    /// A group assignment that is based on a mapping from group identifiers to the corresponding groups.
    /// </summary>
    /// <param name="Groups">A mapping from group identifiers to the corresponding groups.
    /// This mapping does not need to be injective.</param>
    /// <param name="DetermineGroup">A function that returns the group identifier for a given element.</param>
    /// <typeparam name="T">The type of the group identifiers.</typeparam>
    public record TreeWindowGroupAssigment<T>(
        IDictionary<T, TreeWindowGroup> Groups,
        Func<GraphElement, T> DetermineGroup) : ITreeWindowGroupAssigment
    {
        public IEnumerable<TreeWindowGroup> AllGroups => Groups.Values.Distinct();

        public TreeWindowGroup GroupFor(GraphElement element)
        {
            T itsGroup = DetermineGroup(element);
            if (itsGroup == null)
            {
                return null;
            }
            return Groups.TryGetValue(DetermineGroup(element), out TreeWindowGroup group) ? group : null;
        }
    }

    /// <summary>
    /// Manages the grouping of graph elements in the tree window.
    /// </summary>
    public class TreeWindowGrouper
    {
        /// <summary>
        /// The assignment of graph elements to groups.
        /// Note that you will need to invoke <see cref="RebuildCounts"/> after changing this.
        /// </summary>
        public ITreeWindowGroupAssigment Assignment
        {
            private get;
            set;
        }

        /// <summary>
        /// The filter to use for determining which elements are included in the tree window.
        /// </summary>
        private readonly GraphFilter filter;

        /// <summary>
        /// The graph on which the tree window is based.
        /// </summary>
        private readonly Graph graph;

        /// <summary>
        /// A mapping from node IDs and groups to the number of descendants of that node
        /// that are included in the tree window and belong to that group.
        /// </summary>
        private readonly DefaultDictionary<(string nodeId, TreeWindowGroup group), int> descendantCounts;

        /// <summary>
        /// Creates a new <see cref="TreeWindowGrouper"/> with the given parameters.
        /// </summary>
        /// <param name="filter">The filter to use for determining which elements are included in the tree window.</param>
        /// <param name="graph">The graph on which the tree window is based.</param>
        public TreeWindowGrouper(GraphFilter filter, Graph graph)
        {
            this.filter = filter;
            this.graph = graph;
            descendantCounts = new DefaultDictionary<(string, TreeWindowGroup), int>();
            Reset();
        }

        /// <summary>
        /// Resets the grouping of graph elements in the tree window.
        /// </summary>
        public void Reset()
        {
            Assignment = ITreeWindowGroupAssigment.Dummy();
            descendantCounts.Clear();
        }

        /// <summary>
        /// Returns all groups that are available, ordered by the number of members (descending).
        /// </summary>
        public IOrderedEnumerable<TreeWindowGroup> AllGroups => Assignment.AllGroups.OrderByDescending(MembersInGroup);

        /// <summary>
        /// Whether there is at least one group.
        /// </summary>
        public bool IsActive => AllGroups.Any();

        /// <summary>
        /// Returns the group for the given element.
        /// </summary>
        /// <param name="element">The element whose group shall be returned.</param>
        /// <returns>The group for the given element.</returns>
        public TreeWindowGroup GetGroupFor(GraphElement element)
        {
            return Assignment.GroupFor(element);
        }

        /// <summary>
        /// Returns the number of descendants of the given <paramref name="node"/> that are included in the tree window
        /// and belong to the given <paramref name="group"/>.
        /// </summary>
        /// <param name="node">The node whose descendants shall be counted.</param>
        /// <param name="group">The group to which the descendants shall belong.</param>
        /// <returns>The number of descendants of the given <paramref name="node"/> that are included in the tree window
        /// and belong to the given <paramref name="group"/>.</returns>
        public int DescendantsInGroup(Node node, TreeWindowGroup group) => descendantCounts[(node.ID, group)];

        /// <summary>
        /// Returns the number of members of the given <paramref name="group"/>.
        /// </summary>
        /// <param name="group">The group whose members shall be counted.</param>
        /// <returns>The number of members of the given <paramref name="group"/>.</returns>
        public int MembersInGroup(TreeWindowGroup group) => graph.GetRoots().Sum(x => DescendantsInGroup(x, group));

        /// <summary>
        /// Rebuilds the descendant counts for all nodes.
        /// </summary>
        /// <remarks>
        /// This method should be called whenever the graph or its filter changes.
        /// </remarks>
        public void RebuildCounts()
        {
            descendantCounts.Clear();
            foreach (Node root in graph.GetRoots())
            {
                BuildDescendantCounts(root);
            }
        }

        /// <summary>
        /// Returns all direct children of the given <paramref name="node"/> that are included in the tree window
        /// and belong to the given <paramref name="group"/>, including any connected edges.
        /// </summary>
        /// <param name="node">The node whose children and edges shall be returned.</param>
        /// <param name="group">The group to which the children and edges shall belong.</param>
        /// <returns>All direct children of the given <paramref name="node"/> that are included in the tree window
        /// and belong to the given <paramref name="group"/>.</returns>
        public IEnumerable<GraphElement> ChildrenInGroup(Node node, TreeWindowGroup group)
        {
            return node.Children().Concat<GraphElement>(node.Edges).Where(x => filter.Includes(x) && GetGroupFor(x) == group);
        }

        /// <summary>
        /// Returns whether the given <paramref name="element"/> is relevant for the given <paramref name="group"/>.
        /// Specifically, this is the case if the element is included in the tree window and belongs to the group,
        /// or if it is an ascendant of such an element.
        /// </summary>
        /// <param name="element">The element whose relevance shall be checked.</param>
        /// <param name="group">The group for which the relevance shall be checked.</param>
        /// <returns>Whether the given <paramref name="element"/> is relevant for the given <paramref name="group"/>.</returns>
        public bool IsRelevantFor(GraphElement element, TreeWindowGroup group)
        {
            return descendantCounts[(element.ID, group)] > 0 || (filter.Includes(element) && GetGroupFor(element) == group);
        }

        /// <summary>
        /// Computes the descendant counts for the given <paramref name="node"/>
        /// and stores them in <see cref="descendantCounts"/>.
        /// </summary>
        /// <param name="node">The node whose descendant counts shall be computed.</param>
        private void BuildDescendantCounts(Node node)
        {
            foreach (Node descendant in node.PostOrderDescendants())
            {
                // Due to post-order traversal, we can assume that the counts for all children have already been computed.
                foreach (TreeWindowGroup group in AllGroups)
                {
                    // We add the number of relevant children to the sum of the counts of *all* children.
                    // The latter uses all children, not just the relevant ones, because we want to include
                    // relevant descendants of irrelevant children as well.
                    descendantCounts[(descendant.ID, group)] = descendant.Children().Sum(x => descendantCounts[(x.ID, group)]) + ChildrenInGroup(descendant, group).Count();
                }
            }
        }
    }
}
