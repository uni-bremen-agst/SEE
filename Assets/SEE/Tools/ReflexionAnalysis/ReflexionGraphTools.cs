using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using UnityEngine;
using UnityEngine.Assertions;
using static SEE.Tools.ReflexionAnalysis.ReflexionSubgraph;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// Type of a reflexion subgraph.
    /// </summary>
    [Flags]
    public enum ReflexionSubgraph
    {
        /// <summary>
        /// No reflexion subgraph.
        /// </summary>
        None = 0,

        /// <summary>
        /// The implementation graph.
        /// </summary>
        Implementation = 1 << 0,

        /// <summary>
        /// The architecture graph.
        /// </summary>
        Architecture = 1 << 1,

        /// <summary>
        /// The mapping graph.
        /// </summary>
        Mapping = 1 << 2,

        /// <summary>
        /// The full reflexion graph.
        /// </summary>
        FullReflexion = 1 << 3
    }

    /// <summary>
    /// Reflexion graph consisting of architecture, implementation, and mapping nodes and edges.
    /// Nodes and edges are marked with toggles to differentiate these types.
    /// In the future, when the type system is more advanced, this should be handled via the graph elements' types.
    /// </summary>
    public static class ReflexionGraphTools
    {
        /// <summary>
        /// Returns the label of this <paramref name="subgraph"/>, or <c>null</c> if this subgraph is not identified
        /// by a label (such as <see cref="ReflexionSubgraph.Mapping"/>).
        /// </summary>
        /// <param name="subgraph">Subgraph type for which the label shall be returned</param>
        /// <returns>Label of this subgraph type</returns>
        public static string GetLabel(this ReflexionSubgraph subgraph)
        {
            return subgraph switch
            {
                Implementation => "Implementation",
                Architecture => "Architecture",
                None => null,
                Mapping => null, // identified by edges' nodes
                FullReflexion => null, // simply the whole graph
                _ => throw new ArgumentOutOfRangeException(nameof(subgraph), subgraph, "Unknown subgraph type.")
            };
        }

        /// <summary>
        /// Returns a short, human-readable string representation of this <paramref name="subgraph"/>.
        /// </summary>
        /// <param name="subgraph">Subgraph type for which a short human-readable string shall be returned</param>
        /// <returns>Short, human-readable string for this subgraph type</returns>
        public static string ToShortString(this ReflexionSubgraph subgraph)
        {
            return subgraph switch
            {
                Implementation => "Implementation",
                Architecture => "Architecture",
                Mapping => "Mapping",
                FullReflexion => "Reflexion Graph",
                None => "Graph", // we aren't in a reflexion context
                _ => throw new ArgumentOutOfRangeException(nameof(subgraph), subgraph, "Unknown subgraph type.")
            };
        }

        /// <summary>
        /// "Incorporates" the given <paramref name="newEvent"/> into <paramref name="events"/>.
        ///
        /// For all event types except <see cref="EdgeChange"/> (see below), this means that if
        /// <paramref name="events"/> contains a redundant event
        /// (which must be the "inverse" of the given <paramref name="newEvent"/>),
        /// it will be removed and <paramref name="newEvent"/> will not be added to <paramref name="events"/>
        /// (because the two events cancel each other out).
        /// If no such redundant event exists, <paramref name="newEvent"/> will simply be added to the
        /// <paramref name="events"/>.
        ///
        /// For <see cref="EdgeChange"/> events, all such events which have the same edge as <paramref name="newEvent"/>
        /// will be removed from <paramref name="events"/> and a modified version of
        /// <paramref name="newEvent"/> will be added to it in which the OldState is equal to the OldState of the first
        /// EdgeChange event in <paramref name="events"/> if one exists (otherwise the event will not be modified).
        /// This has the effect of reducing a chain of EdgeEvents like this (written as [old state, new state]):
        /// <code>[x1, x2] -> [x2, x3] -> [x3, x4] -> [x4, x5]</code>
        /// to this:
        /// <code>[x1, x5]</code>
        ///
        /// This method can be used if you perform multiple incremental reflexion analysis operations in a row and only
        /// care about the total changes to the graph, but not each individual step.
        ///
        /// Pre-conditions:
        /// - <paramref name="newEvent"/> is not yet in <paramref name="events"/>.
        /// - <paramref name="events"/> is a list of non-"redundant" events, which is true if all events in
        ///   <paramref name="events"/> have been added using this method.
        /// </summary>
        /// <param name="events">List of events into which <paramref name="newEvent"/> shall be incorporated.
        /// May be modified in the course of this method.</param>
        /// <param name="newEvent">The event to incorporate into <paramref name="events"/>.</param>
        /// <returns>A modified version of <paramref name="events"/> into which <paramref name="newEvent"/>
        /// was added.</returns>
        public static IList<ChangeEvent> Incorporate(this IList<ChangeEvent> events, ChangeEvent newEvent) =>
            newEvent switch
            {
                EdgeChange edgeChange => events.Incorporate(edgeChange),
                EdgeEvent edgeEvent => events.Incorporate(edgeEvent, e => e.Edge == edgeEvent.Edge),
                HierarchyEvent hierarchyChangeEvent =>
                    events.Incorporate(hierarchyChangeEvent, e => e.Child == hierarchyChangeEvent.Child
                                                                  && e.Parent == hierarchyChangeEvent.Parent),
                NodeEvent nodeChangeEvent => events.Incorporate(nodeChangeEvent, e => e.Node == nodeChangeEvent.Node),
                PropagatedEdgeEvent propagatedEdgeEvent =>
                    events.Incorporate(propagatedEdgeEvent, e => e.PropagatedEdge == propagatedEdgeEvent.PropagatedEdge),
                _ => throw new ArgumentOutOfRangeException(nameof(newEvent))
            };

        /// <summary>
        /// Removes all EdgeChange events with the same edge as <paramref name="edgeChange"/>
        /// from <paramref name="events"/> and adds a modified version of
        /// <paramref name="edgeChange"/> to it in which the OldState is equal to the OldState of the first
        /// EdgeChange event in <paramref name="events"/> if one exists (otherwise the event will not be modified).
        /// </summary>
        /// <param name="events">The events into which <paramref name="edgeChange"/> shall be incorporated</param>
        /// <param name="edgeChange">The event which shall be incorporated into <paramref name="events"/></param>
        /// <returns>A modified list of <paramref name="events"/> into which <paramref name="edgeChange"/>
        /// has been incorporated.</returns>
        private static IList<ChangeEvent> Incorporate(this IList<ChangeEvent> events, EdgeChange edgeChange)
        {
            // We only care about the most recent NewState of an edge.
            // However, we also care about the first OldState of an edge, so we find it first.
            State? oldState = events.OfType<EdgeChange>().FirstOrDefault(x => x.Edge == edgeChange.Edge)?.OldState;
            EdgeChange newEvent = new EdgeChange(edgeChange.Edge, oldState ?? edgeChange.OldState, edgeChange.NewState);
            // Now we just have to filter out previous EdgeChange events and add the new one.
            return events.Where(x => !(x is EdgeChange e && e.Edge == edgeChange.Edge)).Append(newEvent).ToList();
        }

        /// <summary>
        /// Adds <paramref name="newEvent"/> into <paramref name="events"/> and returns the result iff no element
        /// in <paramref name="events"/> is redundant as specified by <paramref name="isRedundant"/>.
        /// If, on the other hand, an element in <paramref name="events"/> is redundant, that element will be removed
        /// from <paramref name="events"/> and <paramref name="newEvent"/> will be ignored.
        ///
        /// Pre-condition: There is at most one redundant event in <paramref name="events"/> and
        /// <paramref name="newEvent"/> is not yet in <paramref name="events"/>.
        /// </summary>
        /// <param name="events">List of ChangeEvents</param>
        /// <param name="newEvent">The new event to be incorporated into <paramref name="events"/></param>
        /// <param name="isRedundant">Function which returns true iff an element is redundant to
        /// <paramref name="newEvent"/></param>
        /// <typeparam name="T">Type of the new event</typeparam>
        /// <returns>A version of <paramref name="events"/> into which <paramref name="newEvent"/> was
        /// incorporated</returns>
        private static IList<ChangeEvent> Incorporate<T>(this IList<ChangeEvent> events, T newEvent,
                                                         Func<T, bool> isRedundant) where T : ChangeEvent
        {
            // Due to the precondition, there can be at most one redundant event of this type in `events`.
            ChangeEvent redundant = events.SingleOrDefault(x => x is T e && isRedundant(e));
            if (redundant != null)
            {
                // Since the same thing (edge/child/...) can't be removed or added twice, it must be the inverse
                // operation of newEvent. This means we can simply remove that one and ignore the newEvent.
                events.Remove(redundant);
                return events;
            }
            else
            {
                // Otherwise, the new event must be non-redundant.
                events.Add(newEvent);
                return events;
            }
        }

        /// <summary>
        /// Name of the edge attribute for the state of a dependency.
        /// </summary>
        private const string StateAttribute = "Reflexion.State";

        /// <summary>
        /// Returns the state of a dependency.
        /// If no state has been set, <see cref="ReflexionAnalysis.State.Undefined"/> will be returned.
        /// </summary>
        /// <param name="edge">a dependency</param>
        /// <returns>the state of <paramref name="edge"/></returns>
        public static State State(this Edge edge)
        {
            if (edge.TryGetInt(StateAttribute, out int value))
            {
                return (State)value;
            }
            else
            {
                return ReflexionAnalysis.State.Undefined;
            }
        }

        /// <summary>
        /// The edge type maps-to for edges mapping implementation entities onto architecture entities.
        /// </summary>
        public const string MapsToType = "Maps_To";

        /// <summary>
        /// Returns all subgraphs this <paramref name="element"/> is in.
        /// Use <c>Enum.HasFlag</c> to check the resulting value.
        /// </summary>
        public static ReflexionSubgraph GetSubgraph(this GraphElement element) =>
            Enum.GetValues(typeof(ReflexionSubgraph))
                .Cast<ReflexionSubgraph>()
                .Where(element.IsIn)
                .Aggregate(None, (acc, x) => acc & x);

        /// <summary>
        /// Returns true if this <paramref name="element"/> is in the given <paramref name="subgraph"/> type.
        /// </summary>
        /// <param name="subgraph">Subgraph whose containment of this <paramref name="element"/> will be checked</param>
        /// <returns>
        /// Whether this <paramref name="element"/> is contained in the given <paramref name="subgraph"/> type.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the given <paramref name="subgraph"/> is unknown
        /// </exception>
        public static bool IsIn(this GraphElement element, ReflexionSubgraph subgraph)
        {
            if (element == null)
            {
                return false;
            }
            switch (subgraph)
            {
                case Implementation:
                case Architecture:
                    return element.HasToggle(subgraph.GetLabel());
                case Mapping:
                    // Either a "Maps_To" edge or a node connected to such an edge
                    return (element is Edge edge && edge.HasSupertypeOf(MapsToType))
                           || (element is Node node && node.Incomings.Concat(node.Outgoings).Any(IsInMapping));
                case FullReflexion:
                    return element.IsInImplementation() || element.IsInArchitecture() || element.IsInMapping();
                case None:
                    return !element.IsInReflexion();
                default:
                    throw new ArgumentOutOfRangeException(nameof(subgraph), subgraph, "Unknown subgraph type.");
            }
        }

        /// <summary>
        /// Marks this <paramref name="element"/> as being in the given <paramref name="subgraph"/>.
        /// This will also remove it from other subgraphs, if applicable.
        /// </summary>
        /// <param name="subgraph">The subgraph this <paramref name="element"/> shall get assigned to.</param>
        /// <exception cref="InvalidOperationException">If <paramref name="subgraph"/> is
        /// <see cref="Mapping"/> or <see cref="ReflexionSubgraph.FullReflexion"/>.</exception>
        public static void SetIn(this GraphElement element, ReflexionSubgraph subgraph)
        {
            switch (subgraph)
            {
                case Implementation:
                    element.UnsetToggle(Architecture.GetLabel());
                    element.SetToggle(Implementation.GetLabel());
                    break;
                case Architecture:
                    element.UnsetToggle(Implementation.GetLabel());
                    element.SetToggle(Architecture.GetLabel());
                    break;
                case Mapping:
                case None:
                case FullReflexion:
                    throw new InvalidOperationException("Can't explicitly assign graph element to "
                                                        + $"'{subgraph}' (only implicitly)!");
                default: throw new ArgumentOutOfRangeException(nameof(subgraph), subgraph, "Unknown subgraph type.");
            }
        }

        /// <summary>
        /// Returns true if <paramref name="element"/> is in the architecture graph.
        /// </summary>
        public static bool IsInArchitecture(this GraphElement element) => element.IsIn(Architecture);

        /// <summary>
        /// Returns true if <paramref name="element"/> is in the implementation graph.
        /// </summary>
        public static bool IsInImplementation(this GraphElement element) => element.IsIn(Implementation);

        /// <summary>
        /// Returns true if <paramref name="edge"/> is in the mapping graph.
        /// </summary>
        public static bool IsInMapping(this GraphElement element) => element.IsIn(Mapping);

        /// <summary>
        /// Returns true if <paramref name="edge"/> is in the reflexion graph.
        /// </summary>
        public static bool IsInReflexion(this GraphElement element) => element.IsIn(FullReflexion);

        /// <summary>
        /// Marks the <paramref name="element"/> as being in the architecture graph.
        /// This will also remove it from the implementation graph, if applicable.
        /// </summary>
        public static void SetInArchitecture(this GraphElement element) => element.SetIn(Architecture);

        /// <summary>
        /// Marks the <paramref name="element"/> as being in the implementation graph.
        /// This will also remove it from the architecture graph, if applicable.
        /// </summary>
        public static void SetInImplementation(this GraphElement element) => element.SetIn(Implementation);

        /// <summary>
        /// Marks each node and edge of the given <paramref name="graph"/> as being contained in the given
        /// <paramref name="subgraph"/>.
        /// </summary>
        /// <param name="graph">The graph whose nodes and edges shall be marked</param>
        /// <param name="subgraph">The subgraph the nodes and edges will be marked with</param>
        /// <param name="markRootNode">Whether to mark the root node of the <paramref name="graph"/>, too</param>
        public static void MarkGraphNodesIn(this Graph graph, ReflexionSubgraph subgraph, bool markRootNode = true)
        {
            IEnumerable<GraphElement> graphElements = graph.Nodes()
                                                           .Where(node => markRootNode || node.HasToggle(GraphRenderer.RootToggle))
                                                           .Concat<GraphElement>(graph.Edges());
            foreach (GraphElement graphElement in graphElements)
            {
                graphElement.SetIn(subgraph);
            }
        }

        /// <summary>
        /// Generates the full graph from the three sub-graphs <see cref="ImplementationGraph"/>,
        /// <see cref="ArchitectureGraph"/> and <see cref="MappingGraph"/> by combining them into one, returning
        /// the result. Note that the name of the three graphs may be modified.
        ///
        /// Pre-condition: <see cref="ImplementationGraph"/>, <see cref="ArchitectureGraph"/> and
        /// <see cref="MappingGraph"/> are not <c>null</c> (i.e. have been loaded).
        /// </summary>
        /// <returns>Full graph obtained by combining architecture, implementation and mapping</returns>
        public static Graph Assemble(Graph ArchitectureGraph, Graph ImplementationGraph, Graph MappingGraph, string Name,
                                     out Node ArchitectureRoot, out Node ImplementationRoot)
        {
            if (ImplementationGraph == null || ArchitectureGraph == null || MappingGraph == null)
            {
                throw new ArgumentException("All three sub-graphs must be loaded before generating "
                                            + "the full graph.");
            }

            // Add artificial roots if graph has more than one root node, to physically differentiate the two.
            ArchitectureRoot = GraphRenderer.AddGraphRootNodeIfNecessary(ArchitectureGraph) ?? ArchitectureGraph.GetRoots().FirstOrDefault();
            ImplementationRoot = GraphRenderer.AddGraphRootNodeIfNecessary(ImplementationGraph) ?? ImplementationGraph.GetRoots().FirstOrDefault();

            // MappingGraph needn't be labeled, as any remaining/new edge (which must be Maps_To)
            // automatically belongs to it
            ArchitectureGraph.MarkGraphNodesIn(Architecture);
            ImplementationGraph.MarkGraphNodesIn(Implementation);

            // We need to set all Maps_To edges as virtual so they don't get drawn.
            // (Mapping is indicated by moving the implementation node, not by a separate edge.)
            foreach (Edge mapsTo in MappingGraph.Edges())
            {
                Assert.IsTrue(mapsTo.HasSupertypeOf(MapsToType));
                mapsTo.SetToggle(Edge.IsVirtualToggle);
            }

            // We set the name for the implementation graph, because its name will be used for the merged graph.
            ImplementationGraph.Name = Name;

            // We merge architecture and implementation first.
            // Duplicate node IDs between architecture and implementation are not allowed.
            // Any duplicate nodes in the mapping graph are merged into the full graph.
            // If there are duplicate edge IDs, try to remedy this by appending a suffix to the edge ID.
            List<string> nodesOverlap = NodeIntersection(ImplementationGraph, ArchitectureGraph).ToList();
            List<string> edgesOverlap = EdgeIntersection(ImplementationGraph, ArchitectureGraph).ToList();
            string suffix = null;
            if (nodesOverlap.Count > 0)
            {
                throw new ArgumentException($"Overlapping node IDs found: {string.Join(", ", nodesOverlap)}");
            }

            if (edgesOverlap.Count > 0)
            {
                suffix = "-A";
                Debug.LogWarning($"Overlapping edge IDs found, will append '{suffix}' suffix."
                                 + $"Offending elements: {string.Join(", ", edgesOverlap)}");
            }

            Graph mergedGraph = ImplementationGraph.MergeWith(ArchitectureGraph, edgeIdSuffix: suffix);

            // Then we add the mappings, again checking if any IDs overlap, though node IDs overlapping is fine here.
            edgesOverlap = EdgeIntersection(mergedGraph, MappingGraph).ToList();
            suffix = null;
            if (edgesOverlap.Count > 0)
            {
                suffix = "-M";
                Debug.LogWarning($"Overlapping edge IDs found, will append '{suffix}' suffix."
                                 + $"Offending elements: {string.Join(", ", edgesOverlap)}");
            }

            return mergedGraph.MergeWith(MappingGraph, suffix);

            #region Local Functions

            // Returns the intersection of the node IDs of the two graphs.
            IEnumerable<string> NodeIntersection(Graph aGraph, Graph anotherGraph)
                => aGraph.Nodes().Select(x => x.ID).Intersect(anotherGraph.Nodes().Select(x => x.ID));

            // Returns the intersection of the edge IDs of the two graphs.
            IEnumerable<string> EdgeIntersection(Graph aGraph, Graph anotherGraph)
                => aGraph.Edges().Select(x => x.ID).Intersect(anotherGraph.Edges().Select(x => x.ID));

            #endregion
        }

        /// <summary>
        /// Disassembles the given <paramref name="FullGraph"/> into implementation, architecture, and mapping graphs,
        /// and returns them in this order.
        ///
        /// Pre-condition: The given graph must have been assembled by <see cref="Assemble"/>.
        /// </summary>
        /// <param name="FullGraph">Graph generated by <see cref="Assemble"/></param>
        /// <returns>3-tuple consisting of (implementation, architecture, mapping) graph</returns>
        public static (Graph implementation, Graph architecture, Graph mapping) Disassemble(this Graph FullGraph)
        {
            Graph ImplementationGraph = FullGraph.SubgraphBy(IsInImplementation);
            Graph ArchitectureGraph = FullGraph.SubgraphBy(IsInArchitecture);
            Graph MappingGraph = FullGraph.SubgraphBy(IsInMapping);
            return (ImplementationGraph, ArchitectureGraph, MappingGraph);
        }
    }
}