using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using UnityEngine;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// Reflexion graph consisting of architecture, implementation, and mapping nodes and edges.
    /// Nodes and edges are marked with toggles to differentiate these types.
    /// In the future, when the type system is more advanced, this should be handled via the graph elements' types.
    /// </summary>
    public static class ReflexionGraphTools
    {
        /// <summary>
        /// Label for the architecture toggle added to each graph element of the architecture city.
        /// </summary>
        private const string ArchitectureLabel = "Architecture";

        /// <summary>
        /// Label for the implementation toggle added to each graph element of the implementation city.
        /// </summary>
        private const string ImplementationLabel = "Implementation";

        /// <summary>
        /// The edge type maps-to for edges mapping implementation entities onto architecture entities.
        /// </summary>
        public const string MapsToType = "Maps_To";

        /// <summary>
        /// Returns true if <paramref name="element"/> is in the architecture graph.
        /// </summary>
        public static bool IsInArchitecture(this GraphElement element) => element.HasToggle(ArchitectureLabel);

        /// <summary>
        /// Returns true if <paramref name="element"/> is in the implementation graph.
        /// </summary>
        public static bool IsInImplementation(this GraphElement element) => element.HasToggle(ImplementationLabel);

        /// <summary>
        /// Returns true if <paramref name="edge"/> is in the mapping graph.
        /// </summary>
        public static bool IsInMapping(this Edge edge) => edge.HasSupertypeOf(MapsToType);

        /// <summary>
        /// Returns true if <paramref name="node"/> is in the mapping graph.
        /// </summary>
        public static bool IsInMapping(this Node node) => node.Incomings.Concat(node.Outgoings).Any(IsInMapping);

        /// <summary>
        /// Marks the <paramref name="element"/> as being in the architecture graph.
        /// This will also remove it from the implementation graph, if applicable.
        /// </summary>
        public static void SetInArchitecture(this GraphElement element)
        {
            element.UnsetToggle(ImplementationLabel);
            element.SetToggle(ArchitectureLabel);
        }

        /// <summary>
        /// Marks the <paramref name="element"/> as being in the implementation graph.
        /// This will also remove it from the architecture graph, if applicable.
        /// </summary>
        public static void SetInImplementation(this GraphElement element)
        {
            element.UnsetToggle(ArchitectureLabel);
            element.SetToggle(ImplementationLabel);
        }

        /// <summary>
        /// Adds a toggle attribute <paramref name="label"/> to each node and edge of
        /// the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph whose nodes and edges shall be marked with a toggle attribute</param>
        /// <param name="label">The value of the toggle attribute</param>
        /// <param name="labelRootNode">Whether to label the root node of the <paramref name="graph"/>, too</param>
        public static void MarkGraphNodes(this Graph graph, string label, bool labelRootNode = true)
        {
            IEnumerable<GraphElement> graphElements = graph.Nodes()
                                                           .Where(node => labelRootNode || node.Type != GraphRenderer.RootType)
                                                           .Concat<GraphElement>(graph.Edges());
            foreach (GraphElement graphElement in graphElements)
            {
                graphElement.SetToggle(label);
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
        public static Graph Assemble(Graph ArchitectureGraph, Graph ImplementationGraph, Graph MappingGraph, string Name)
        {
            if (ImplementationGraph == null || ArchitectureGraph == null || MappingGraph == null)
            {
                throw new ArgumentException("All three sub-graphs must be loaded before generating "
                                            + "the full graph.");
            }

            // MappingGraph needn't be labeled, as any remaining/new edge automatically belongs to it
            ArchitectureGraph.MarkGraphNodes(ArchitectureLabel);
            ImplementationGraph.MarkGraphNodes(ImplementationLabel);

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
            Graph ImplementationGraph = FullGraph.SubgraphByToggleAttributes(new[] { ImplementationLabel });
            Graph ArchitectureGraph = FullGraph.SubgraphByToggleAttributes(new[] { ArchitectureLabel });
            // Mapping graph's edges will have neither architecture nor implementation label and will only contain
            // nodes connected to those edges.
            Graph MappingGraph = FullGraph.SubgraphByEdges(x => !x.HasToggle(ImplementationLabel)
                                                                && !x.HasToggle(ArchitectureLabel));
            return (ImplementationGraph, ArchitectureGraph, MappingGraph);
        }
    }
}