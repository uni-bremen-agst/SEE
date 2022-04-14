using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using UnityEngine;
using static SEE.Tools.ReflexionAnalysis.ReflexionSubgraph;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// Type of a reflexion subgraph.
    /// </summary>
    public enum ReflexionSubgraph
    {
        /// <summary>
        /// The implementation graph.
        /// </summary>
        Implementation,
        
        /// <summary>
        /// The architecture graph.
        /// </summary>
        Architecture,
        
        /// <summary>
        /// The mapping graph.
        /// </summary>
        Mapping,
        
        /// <summary>
        /// The full reflexion graph.
        /// </summary>
        FullReflexion
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
                Mapping => null, // identified by edges' nodes
                ReflexionSubgraph.FullReflexion => null, // simply the whole graph
                _ => throw new ArgumentOutOfRangeException(nameof(subgraph), subgraph, "Unknown subgraph type.")
            };
        }
        
        /// <summary>
        /// The edge type maps-to for edges mapping implementation entities onto architecture entities.
        /// </summary>
        public const string MapsToType = "Maps_To";

        /// <summary>
        /// Returns true if this <paramref name="element"/> is in the given <paramref name="subgraph"/> type.
        /// </summary>
        /// <param name="subgraph">Subgraph whose containment of this <paramref name="element"/> will be checked</param>
        /// <returns>
        /// Whether this <paramref name="element"/> is contained in the given <paramref name="subgraph"/> type.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool IsIn(this GraphElement element, ReflexionSubgraph subgraph)
        {
            switch (subgraph)
            {
                case Implementation:
                case Architecture: 
                    return element.HasToggle(subgraph.GetLabel());
                case Mapping:
                    // Either a "Maps_To" edge or a node connected to such an edge
                    return element is Edge edge && edge.HasSupertypeOf(MapsToType) 
                           || element is Node node && node.Incomings.Concat(node.Outgoings).Any(IsInMapping);
                case ReflexionSubgraph.FullReflexion: 
                    return element.IsInImplementation() || element.IsInArchitecture() || element.IsInMapping();
                default: throw new ArgumentOutOfRangeException(nameof(subgraph), subgraph, "Unknown subgraph type.");
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
                case ReflexionSubgraph.FullReflexion: throw new InvalidOperationException("Can't explicitly assign graph element to " 
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
                                                           .Where(node => markRootNode || node.Type != GraphRenderer.RootType)
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
        public static Graph Assemble(Graph ArchitectureGraph, Graph ImplementationGraph, Graph MappingGraph, string Name)
        {
            if (ImplementationGraph == null || ArchitectureGraph == null || MappingGraph == null)
            {
                throw new ArgumentException("All three sub-graphs must be loaded before generating "
                                            + "the full graph.");
            }

            // MappingGraph needn't be labeled, as any remaining/new edge (which must be Maps_To)
            // automatically belongs to it
            ArchitectureGraph.MarkGraphNodesIn(Architecture);
            ImplementationGraph.MarkGraphNodesIn(Implementation);

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