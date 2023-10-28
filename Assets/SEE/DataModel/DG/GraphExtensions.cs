using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Provides extensions to <see cref="Graph"/>.
    /// </summary>
    public static class GraphExtensions
    {
        /// <summary>
        /// Yields the differences of the graph elements of type <typeparamref name="T"/>
        /// in this <paramref name="newGraph"/> relative to <paramref name="oldGraph"/>.
        /// </summary>
        /// <typeparam name="T">the type of graph elements (nodes or edges)</typeparam>
        /// <param name="newGraph">the new graph to be compared against <paramref name="oldGraph"/></param>
        /// <param name="oldGraph">the previous graph as a baseline</param>
        /// <param name="getElements">yields all graph elements of <paramref name="newGraph"/> and
        /// <paramref name="oldGraph"/> to be compared</param>
        /// <param name="getElement">yields a particular graph element for a given ID</param>
        /// <param name="diff">yields true if two graph elements have different attributes</param>
        /// <param name="comparer">yields true if two graph elements are to be considered identical</param>
        /// <param name="added">the nodes that are only in <paramref name="newGraph"/></param>
        /// <param name="removed">the nodes that are only in <paramref name="oldGraph"/></param>
        /// <param name="changed">the elements in both graphs that have differences according to <paramref name="diff"/></param>
        /// <param name="equal">the elements in both graphs that have no differences according to <paramref name="diff"/></param>
        public static void Diff<T>
           (this Graph newGraph,
            Graph oldGraph,
            Func<Graph, IEnumerable<T>> getElements,
            Func<Graph, string, T> getElement,
            IGraphElementDiff diff,
            GraphElementEqualityComparer<T> comparer,
            out ISet<T> added,
            out ISet<T> removed,
            out ISet<T> changed,
            out ISet<T> equal)
           where T : GraphElement
        {
            IEnumerable<T> oldElements = oldGraph != null ? getElements(oldGraph).ToList() : null;
            IEnumerable<T> newElements = newGraph != null ? getElements(newGraph).ToList() : null;

            if (oldElements == null || !oldElements.Any())
            {
                removed = new HashSet<T>();
                changed = new HashSet<T>();
                equal = new HashSet<T>();

                if (newElements == null || !newElements.Any())
                {
                    // Both are empty. There are no differences.
                    added = new HashSet<T>();
                }
                else
                {
                    // oldElements is empty, but newElements is not
                    added = new HashSet<T>(newElements);
                }
            }
            else if (newElements == null || !newElements.Any())
            {
                // oldElements is non-empty, but newElements is
                added = new HashSet<T>();
                removed = new HashSet<T>(oldElements);
                changed = new HashSet<T>();
                equal = new HashSet<T>();
            }
            else
            {
                // Note: The comparison is based on the IDs of the nodes/edges because nodes/edges between
                // two graphs must be different even if they denote the "logically same" node/edge.
                ISet<T> oldGraphElements = new HashSet<T>(oldElements, comparer);
                ISet<T> newGraphElements = new HashSet<T>(newElements, comparer);

                added = new HashSet<T>(newGraphElements, comparer);
                added.ExceptWith(oldGraphElements);

                removed = new HashSet<T>(oldGraphElements, comparer);
                removed.ExceptWith(newGraphElements);

                // The nodes in both graphs; must be further partitioned into changedNodes and equalNodes.
                ISet<T> sharedElements = new HashSet<T>(oldGraphElements, comparer);
                sharedElements.IntersectWith(newGraphElements);

                changed = new HashSet<T>();
                equal = new HashSet<T>();

                foreach (T sharedFromOldGraph in sharedElements)
                {
                    T sharedFromNewGraph = getElement(newGraph, sharedFromOldGraph.ID);
                    if (diff.AreDifferent(sharedFromOldGraph, sharedFromNewGraph))
                    {
                        changed.Add(sharedFromNewGraph);
                    }
                    else
                    {
                        equal.Add(sharedFromNewGraph);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="IGraphElementDiff"/> that considers all
        /// node and edge attributes contained in any of the given <paramref name="graphs"/>.
        /// </summary>
        /// <param name="graphs">list of graphs</param>
        /// <returns>a <see cref="IGraphElementDiff"/> for all types of node and edge attributes</returns>
        public static IGraphElementDiff AttributeDiff(params Graph[] graphs)
        {
            ISet<string> floatAttributes = new HashSet<string>();
            ISet<string> intAttributes = new HashSet<string>();
            ISet<string> stringAttributes = new HashSet<string>();
            ISet<string> toggleAttributes = new HashSet<string>();
            graphs.ToList().ForEach(graph =>
            {
                if (graph != null)
                {
                    floatAttributes.UnionWith(graph.AllFloatGraphElementAttributes());
                    intAttributes.UnionWith(graph.AllIntGraphElementAttributes());
                    stringAttributes.UnionWith(graph.AllStringGraphElementAttributes());
                    toggleAttributes.UnionWith(graph.AllToggleGraphElementAttributes());
                }
            });
            return new AttributeDiff(floatAttributes, intAttributes, stringAttributes, toggleAttributes);
        }
    }
}
