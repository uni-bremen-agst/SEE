using System.Collections.Generic;
using System;
using Sirenix.Utilities;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Merges the differences between an old and a new graph into the new graph.
    /// </summary>
    internal static class MergeDiffGraphExtension
    {
        /// <summary>
        /// This postfix will be added at the end of the name of an attribute whose
        /// value has changed or was deleted from the old graph to the new graph.
        /// </summary>
        public const string AttributeOldValuePostfix = ".Old";

        /// <summary>
        /// Merges the changes of nodes and edges of <paramref name="newGraph"/> with
        /// respect to <paramref name="oldGraph"/>.
        ///
        /// More precisely, graph elements in <paramref name="newGraph"/>, but not in
        /// <paramref name="oldGraph"/> are marked by toggle attribute
        /// <see cref="ChangeMarkers.IsNew"/>.
        ///
        /// Graph elements in <paramref name="oldGraph"/>, but not in <paramref name="newGraph"/>
        /// are added to <paramref name="newGraph"/> and marked by toggle attribute
        /// <see cref="ChangeMarkers.IsDeleted"/>.
        ///
        /// If a graph element is in both <paramref name="newGraph"/> and
        /// <paramref name="oldGraph"/> and has a difference in any of its attributes,
        /// the graph element in <paramref name="newGraph"/> will be marked by toggle
        /// attribute <see cref="ChangeMarkers.IsChanged"/>. Let N be the graph
        /// element in <paramref name="newGraph"/> corresponding to the graph element O
        /// of <paramref name="oldGraph"/>. For every attribute, A of O,
        /// either not present in N or present in N but with a different value, an attribute
        /// named A appended by the <see cref="AttributeOldValuePostfix"/>
        /// will be added to N with the value of O.A.
        ///
        /// Whether two attribute values are equal/different is determined
        /// by <see cref="Object.Equals(object, object)"/>.
        ///
        /// Whether two graph elements are the same in the two graphs is determined by
        /// <see cref="GraphElement.ID"/>.
        ///
        /// Only <paramref name="newGraph"/> can be effected by this method.
        /// </summary>
        /// <param name="newGraph">The newer graph in which to merge the differences.</param>
        /// <param name="oldGraph">The old graph whose difference to <paramref name="newGraph"/>
        /// should be merged into <paramref name="newGraph"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown in case <paramref name="newGraph"/> is null.</exception>
        public static void MergeDiff(this Graph newGraph, Graph oldGraph)
        {
            if (newGraph == null)
            {
                throw new ArgumentNullException(nameof(newGraph));
            }
            if (oldGraph == null)
            {
                return;
            }
            else
            {
                MergeDiffNodes(newGraph, oldGraph);
                MergeDiffEdges(newGraph, oldGraph);
            }
        }

        /// <summary>
        /// Merges the differences between <paramref name="newGraph"/> and <paramref name="oldGraph"/>
        /// with respect to nodes into <paramref name="newGraph"/>.
        /// </summary>
        /// <param name="newGraph">The newer graph.</param>
        /// <param name="oldGraph">The older graph.</param>
        private static void MergeDiffNodes(Graph newGraph, Graph oldGraph)
        {
            newGraph.Diff(oldGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(newGraph, oldGraph),
                          new NodeEqualityComparer(),
                          out ISet<Node> addedNodes,
                          out ISet<Node> removedNodes,
                          out ISet<Node> changedNodes,
                          out ISet<Node> equalNodes);

            MergeGraphElements(addedNodes, removedNodes, changedNodes, newGraph.AddNode, oldGraph);
        }

        /// <summary>
        /// Merges the differences between <paramref name="newGraph"/> and <paramref name="oldGraph"/>
        /// with respect to edges into <paramref name="newGraph"/>.
        /// </summary>
        /// <param name="newGraph">The newer graph.</param>
        /// <param name="oldGraph">The older graph.</param>
        private static void MergeDiffEdges(Graph newGraph, Graph oldGraph)
        {
            newGraph.Diff(oldGraph,
                        g => g.Edges(),
                        (g, id) => g.GetEdge(id),
                        GraphExtensions.AttributeDiff(newGraph, oldGraph),
                        new EdgeEqualityComparer(),
                        out ISet<Edge> addedEdges,
                        out ISet<Edge> removedEdges,
                        out ISet<Edge> changedEdges,
                        out ISet<Edge> equalEdges);

            MergeGraphElements(addedEdges, removedEdges, changedEdges, AddEdge, oldGraph);

            // Adds edge to LoadedGraph. Note: edge is assumed to be cloned
            // from an edge belonging to the baseline graph that has no
            // corresponding edge in LoadedGraph, thus, was deleted.
            void AddEdge(Edge edge)
            {
                // edge is cloned from a baseline edge, but after the cloning its source
                // and target are nodes in the baseline graph. edge will be added to the
                // LoadedGraph, hence, we need to adjust its source and target to the
                // corresponding nodes in LoadedGraph.
                edge.Source = newGraph.GetNode(edge.Source.ID);
                edge.Target = newGraph.GetNode(edge.Target.ID);
                // edge was clone from a baseline edge, but the cloned edge does
                // not belong to any graph (its graph is null). As a consequence,
                // we do not need to reset ItsGraph.
                newGraph.AddEdge(edge);
            }
        }

        /// <summary>
        /// Marks all <paramref name="added"/> as <see cref="ChangeMarkers.IsNew"/>.
        /// Marks all <paramref name="changed"/> as <see cref="ChangeMarkers.IsChanged"/>.
        /// Adds all <paramref name="removed"/> to the newer graph using <paramref name="addToGraph"/>
        /// and marks them as <see cref="ChangeMarkers.IsDeleted"/>.
        ///
        /// Assumption: <paramref name="added"/>, <paramref name="changed"/>, and
        /// <paramref name="removed"/> are mutually exclusive.
        /// </summary>
        /// <typeparam name="T">type of <see cref="GraphElement"/></typeparam>
        /// <param name="added">Added graph elements (these are in the newer graph).</param>
        /// <param name="removed">Removed graph elements (these are in <paramref name="baseline"/>).</param>
        /// <param name="changed">Changed graph elements (these are in the newer graph).</param>
        /// <param name="addToGraph">Adds its argument to the newer graph and
        /// will be called for all elements in <paramref name="removed"/>.</param>
        private static void MergeGraphElements<T>(ISet<T> added, ISet<T> removed, ISet<T> changed, Action<T> addToGraph, Graph baseline)
            where T : GraphElement
        {
            // here graphElement stems from the newer graph
            added.ForEach(graphElement => graphElement.SetToggle(ChangeMarkers.IsNew));
            // here graphElement stems from the newer graph
            changed.ForEach(UpdateChanged);
            // here graphElement stems from the baseline graph
            removed.ForEach(MergeRemoved);

            // Adds graphElement to newer graph and marks it as deleted.
            // This graphElement is assumed to be in the baseline graph.
            void MergeRemoved(T graphElement)
            {
                T removedGraphElement = graphElement.Clone() as T;
                addToGraph(removedGraphElement);
                removedGraphElement.SetToggle(ChangeMarkers.IsDeleted);
            }

            // Marks given graph element as changed.
            // Note: graphElement is from newer graph.
            void UpdateChanged(T graphElement)
            {
                graphElement.SetToggle(ChangeMarkers.IsChanged);
                // Calculates the diff between the corresponding metrics. Diff = new - old

                // The corresponding graph element from the baseline.
                T correspondingElementInBaseline = graphElement is Node ? baseline.GetNode(graphElement.ID) as T
                                                                        : baseline.GetEdge(graphElement.ID) as T;

                MergeAttributes<T, int>(graphElement, correspondingElementInBaseline,
                                        ge => ge.AllIntAttributeNames(),
                                        (ge, m) => ge.GetInt(m),
                                        (T ge, string m, out int v) => ge.TryGetInt(m, out v),
                                        (ge, m, v) => ge.SetInt(m, v));

                MergeAttributes<T, float>(graphElement, correspondingElementInBaseline,
                        ge => ge.AllFloatAttributeNames(),
                        (ge, m) => ge.GetFloat(m),
                        (T ge, string m, out float v) => ge.TryGetFloat(m, out v),
                        (ge, m, v) => ge.SetFloat(m, v));

                MergeAttributes<T, string>(graphElement, correspondingElementInBaseline,
                        ge => ge.AllStringAttributeNames(),
                        (ge, m) => ge.GetString(m),
                        (T ge, string m, out string v) => ge.TryGetString(m, out v),
                        (ge, m, v) => ge.SetString(m, v));

                MergeAttributes<T, bool>(graphElement, correspondingElementInBaseline,
                        ge => ge.AllToggleAttributeNames(),
                        (ge, m) => ge.HasToggle(m),
                        (T ge, string m, out bool v) => v = ge.HasToggle(m),
                        (ge, m, v) => ge.SetToggle(m));
            }
        }

        /// <summary>
        /// Retrieves the value of attribute <paramref name="attributeName"/> from
        /// <paramref name="graphElement"/>. If <paramref name="graphElement"/> has
        /// this attribute, true is returned and the attributes value can be found
        /// in <paramref name="value"/>. If <paramref name="graphElement"/> does
        /// not have this attribute, false is returned and <paramref name="value"/>
        /// is undefined.
        /// </summary>
        /// <typeparam name="T">type of graph element</typeparam>
        /// <typeparam name="V">value type of a graph-element attribute</typeparam>
        /// <param name="graphElement">Graph element whose attribute value is requested.</param>
        /// <param name="attributeName">The name of the requested attribute.</param>
        /// <param name="value">The value of the attribute or undefined.</param>
        /// <returns>True if <paramref name="graphElement"/> has an attribute
        /// with given <paramref name="attributeName"/>.</returns>
        private delegate bool TryGet<T, V>(T graphElement, string attributeName, out V value);

        /// <summary>
        /// Merges the attributes of <paramref name="graphElementInOld"/> into <paramref name="graphElementInNew"/>.
        ///
        /// Let A be an attribute of <paramref name="graphElementInOld"/>. If <paramref name="graphElementInNew"/>
        /// does not have A or has A but with a different value, a new attribute with the name of A
        /// appended by <see cref="AttributeOldValuePostfix"/> will be added to <paramref name="graphElementInNew"/>
        /// whose value will the original value of A.
        /// </summary>
        /// <typeparam name="T">type of graph element</typeparam>
        /// <typeparam name="V">value type of a graph-element attribute</typeparam>
        /// <param name="graphElementInNew">The graph element in the new graph.</param>
        /// <param name="graphElementInOld">The graph element in the old graph corresponding to <paramref name="graphElementInNew"/>.</param>
        /// <param name="allAttributeNames">Yields the names of the attributes to be merged.</param>
        /// <param name="get">Yields the value of an attribute of a given name for a graph element.</param>
        /// <param name="tryGet">See <see cref="TryGet{T, V}"/>.</param>
        /// <param name="set">Sets the value of an attribute of a given name for a graph element.</param>
        private static void MergeAttributes<T, V>(T graphElementInNew, T graphElementInOld,
            Func<T, ICollection<string>> allAttributeNames,
            Func<T, string, V> get,
            TryGet<T, V> tryGet,
            Action<T, string, V> set) where T : GraphElement
        {
            foreach (string attribute in allAttributeNames(graphElementInOld))
            {
                V correspondingValueInBaseline = get(graphElementInOld, attribute);
                if (!tryGet(graphElementInNew, attribute, out V valueInTarget)
                    || !correspondingValueInBaseline.Equals(valueInTarget))
                {
                    // attribute is either not present in graphElementInTarget or present but
                    // with a different value
                    set(graphElementInNew,
                        attribute + AttributeOldValuePostfix,
                        correspondingValueInBaseline);
                }
            }
        }
    }
}
