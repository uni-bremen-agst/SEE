using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using UnityEngine;
using UnityEngine.Assertions;
using static SEE.Tools.ReflexionAnalysis.ReflexionSubgraphs;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// Part of the reflexion class managing the inheritance to <see cref="Graph"/>, e.g., overriding relevant methods.
    /// </summary>
    public partial class ReflexionGraph
    {
        /// <summary>
        /// Root node of the architecture.
        /// Note that this isn't a root node of the whole graph, it only roots all architecture nodes.
        /// </summary>
        private Node architectureRoot;

        /// <summary>
        /// Property for the architecture root.
        /// If the attribute has not been set,
        /// the corresponding node is searched for,
        /// and the attribute is assigned.
        /// Assignment to the attribute is only possible once.
        /// </summary>
        public Node ArchitectureRoot
        {
            get {
                return architectureRoot ??= Nodes().FirstOrDefault(node => node.IsArchitectureRoot());
            }
            set {
                architectureRoot ??= value;
            }
        }

        /// <summary>
        /// Name of the artificial node type used for the artificial architecture root nodes added
        /// when we do not have a real node type derived from the input graph.
        /// </summary>
        public const string ArchitectureType = "ARCHITECTURE";

        /// <summary>
        /// Root node of the implementation.
        /// Note that this isn't a root node of the whole graph, it only roots all implementation nodes.
        /// </summary>
        private Node implementationRoot;

        /// <summary>
        /// Property for the implementation root.
        /// If the attribute has not been set,
        /// the corresponding node is searched for,
        /// and the attribute is assigned.
        /// Assignment to the attribute is only possible once.
        /// </summary>
        public Node ImplementationRoot
        {
            get
            {
                return implementationRoot ??= Nodes().FirstOrDefault(node => node.IsImplementationRoot());
            }
            set
            {
                implementationRoot ??= value;
            }
        }

        /// <summary>
        /// Name of the artificial node type used for artificial implementation root nodes added
        /// when we do not have a real node type derived from the input graph.
        /// </summary>
        public const string ImplementationType = "IMPLEMENTATION";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="basePath">the base path of this graph; it will be prepended to
        /// <see cref="GraphElement.AbsolutePlatformPath()"/> for every graph element of this graph</param>
        /// <param name="name">name of the graph</param>
        /// <param name="allowDependenciesToParents">whether descendants may access their ancestors</param>
        public ReflexionGraph(string basePath, string name = "", bool allowDependenciesToParents = true) : base(basePath, name)
        {
            this.allowDependenciesToParents = allowDependenciesToParents;
        }

        /// <summary>
        /// Constructor for setting up the reflexion analysis.
        /// NOTE: The three given graphs will be copied and assembled into <see cref="FullGraph"/>.
        /// Any further modifications to those three graphs will not be taken into account.
        /// </summary>
        /// <param name="implementation">the implementation graph</param>
        /// <param name="architecture">the architecture model</param>
        /// <param name="mapping">the mapping of implementation nodes onto architecture nodes</param>
        /// <param name="allowDependenciesToParents">whether descendants may access their ancestors</param>
        /// <remarks>
        /// This does not really run the reflexion analysis. Use method Run() to start the analysis.
        /// </remarks>
        public ReflexionGraph(Graph implementation, Graph architecture, Graph mapping, string name = null,
                         bool allowDependenciesToParents = true) :
            base(Assemble(architecture, implementation, mapping, name ?? "Reflexion Graph", out Node aRoot, out Node iRoot))
        {
            // FIXME: This constructor has really bad performance, due to all the copying around in Assemble().
            ArchitectureRoot = aRoot;
            ImplementationRoot = iRoot;
            this.allowDependenciesToParents = allowDependenciesToParents;
        }

        /// <summary>
        /// Constructor for setting up the reflexion analysis by copying from <paramref name="fullGraph"/>.
        /// </summary>
        /// <param name="fullGraph">
        /// The graph containing nodes and edges for implementation, architecture and mapping, labeled
        /// using the toggle attributes <see cref="ReflexionGraphTools.ImplementationLabel"/> and
        /// <see cref="ReflexionGraphTools.ArchitectureLabel"/>.
        /// Its attributes will be copied into this graph.
        /// </param>
        /// <param name="allowDependenciesToParents">whether descendants may access their ancestors</param>
        /// <remarks>
        /// This does not really run the reflexion analysis. Use <see cref="RunAnalysis"/> to start the analysis.
        /// </remarks>
        public ReflexionGraph(Graph fullGraph, bool allowDependenciesToParents = true) : base(fullGraph)
        {
            this.allowDependenciesToParents = allowDependenciesToParents;
            (Graph implementation, Graph architecture, _) = Disassemble();
            ArchitectureRoot = architecture.GetRoots().FirstOrDefault();
            ImplementationRoot = implementation.GetRoots().FirstOrDefault();
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
        private static ReflexionGraph Assemble(Graph architectureGraph, Graph implementationGraph, Graph mappingGraph,
            string name, out Node architectureRoot, out Node implementationRoot)
        {
            if (implementationGraph == null || architectureGraph == null || mappingGraph == null)
            {
                throw new ArgumentException("All three sub-graphs must be loaded before generating "
                                            + "the full graph.");
            }

            // Add artificial roots if graph has more than one root node, to physically differentiate the two.
            architectureGraph.AddSingleRoot(out architectureRoot, type: ArchitectureType);
            implementationGraph.AddSingleRoot(out implementationRoot, type: ImplementationType);

            // MappingGraph needn't be labeled, as any remaining/new edge (which must be Maps_To)
            // automatically belongs to it
            architectureGraph.MarkGraphNodesIn(Architecture);
            implementationGraph.MarkGraphNodesIn(Implementation);

            // We need to set all Maps_To edges as virtual so they don't get drawn.
            // (Mapping is indicated by moving the implementation node, not by a separate edge.)
            foreach (Edge mapsTo in mappingGraph.Edges())
            {
                Assert.IsTrue(mapsTo.HasSupertypeOf(MapsToType));
                mapsTo.SetToggle(GraphElement.IsVirtualToggle);
            }

            // We set the name for the implementation graph, because its name will be used for the merged graph.
            implementationGraph.Name = name;

            // We merge architecture and implementation first.
            // Duplicate node IDs between architecture and implementation are not allowed.
            // Any duplicate nodes in the mapping graph are merged into the full graph.
            // If there are duplicate edge IDs, try to remedy this by appending a suffix to the edge ID.
            List<string> nodesOverlap = NodeIntersection(implementationGraph, architectureGraph).ToList();
            List<string> edgesOverlap = EdgeIntersection(implementationGraph, architectureGraph).ToList();
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

            if (implementationGraph is not ReflexionGraph)
            {
                implementationGraph = new ReflexionGraph(implementationGraph);
            }
            ReflexionGraph mergedGraph = implementationGraph.MergeWith<ReflexionGraph>(architectureGraph, edgeIdSuffix: suffix);

            // Then we add the mappings, again checking if any IDs overlap, though node IDs overlapping is fine here.
            edgesOverlap = EdgeIntersection(mergedGraph, mappingGraph).ToList();
            suffix = null;
            if (edgesOverlap.Count > 0)
            {
                suffix = "-M";
                Debug.LogWarning($"Overlapping edge IDs found, will append '{suffix}' suffix."
                                 + $"Offending elements: {string.Join(", ", edgesOverlap)}");
            }

            mergedGraph = mergedGraph.MergeWith<ReflexionGraph>(mappingGraph, suffix);
            mergedGraph.AddSingleRoot(out Node _);
            return mergedGraph;

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
        /// The three resulting graphs are new graphs, that is, the nodes and edges are copies; they are not
        /// shared with the disassembled graph. Graph elements in the resulting subgraphs can be mapped onto their
        /// original corresponding graph elements in this graph by way of their ID.
        ///
        /// Pre-condition: The given graph must have been assembled by <see cref="Assemble"/>.
        /// </summary>
        /// <returns>3-tuple consisting of (implementation, architecture, mapping) graph</returns>
        public (Graph implementation, Graph architecture, Graph mapping) Disassemble()
        {
            return (implementation: SubgraphBy(ReflexionGraphTools.IsInImplementation),
                    architecture: SubgraphBy(ReflexionGraphTools.IsInArchitecture),
                    mapping: SubgraphBy(ReflexionGraphTools.IsInMapping));
        }

        #region Overridden Methods

        // NOTE: For all overridden methods below, we check whether the reflexion graph has been initialized yet.
        //       If it hasn't, we simply always call the base method, so that we can act as a simple graph
        //       to callers who may not expect this to be a "special" reflexion graph.

        /// <summary>
        /// Removes the given <paramref name="edge"/> from the reflexion graph.
        /// Precondition: <paramref name="edge"/> must be contained in the reflexion graph.
        /// If it belongs to the architecture graph, it must represent a specified dependency.
        /// Postcondition: <paramref name="edge"/> is no longer contained in the reflexion graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the specified edge to be removed from the reflexion graph</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="edge"/>
        /// is not contained in the reflexion graph.</exception>
        /// <exception cref="ExpectedSpecifiedEdgeException">When the given <paramref name="edge"/> is not
        /// a specified edge.</exception>
        /// <exception cref="NotSupportedException">When the given <paramref name="edge"/> is neither in
        /// the architecture, implementation, nor mapping graph.</exception>
        public override void RemoveEdge(Edge edge)
        {
            if (!AnalysisInitialized)
            {
                base.RemoveEdge(edge);
                return;
            }
            switch (edge.GetSubgraph())
            {
                case Architecture:
                    RemoveFromArchitecture(edge);
                    break;
                case Implementation:
                    RemoveFromImplementation(edge);
                    break;
                case Mapping:
                    RemoveFromMapping(edge);
                    break;
                default: throw new NotSupportedException("Given edge must be in reflexion graph!");
            }
        }

        /// <summary>
        /// Adds the given <paramref name="node"/> to the reflexion graph.
        ///
        /// Pre-condition: <paramref name="node"/> must be either in the architecture or implementation graph.
        ///                Alternatively, one of <paramref name="node"/>'s parents must be in one of them.
        /// </summary>
        /// <param name="node">the node to add to the graph</param>
        /// <exception cref="NotSupportedException">If the precondition of this method is not met</exception>
        public override void AddNode(Node node)
        {
            if (!AnalysisInitialized)
            {
                base.AddNode(node);
                return;
            }
            switch (DetermineSubgraph(node))
            {
                case Architecture:
                    AddToArchitecture(node);
                    break;
                case Implementation:
                    AddToImplementation(node);
                    break;
                default:
                    throw new NotSupportedException($"Given node {node.ID} must be in architecture or implementation graph!");
            }
        }

        /// <summary>
        /// Removes given <paramref name="node"/> from its graph, which is either the architecture or the
        /// implementation graph.
        ///
        /// If <paramref name="orphansBecomeRoots"/> is true, the children of <paramref name="node"/>
        /// become root nodes. Otherwise they become children of the parent of <paramref name="node"/>
        /// if there is a parent.
        ///
        /// Pre-condition: <paramref name="node"/> is contained in either the architecture or the implementation graph.
        ///                Alternatively, one of <paramref name="node"/>'s parents must be in one of them.
        /// Postcondition: <paramref name="node"/> is no longer contained in its graph and the reflexion data
        ///                is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from its graph</param>
        /// <param name="orphansBecomeRoots">if true, the children of <paramref name="node"/> become root nodes;
        /// otherwise they become children of the parent of <paramref name="node"/> (if any)</param>
        /// <exception cref="NotSupportedException">When <paramref name="node"/> is neither in the architecture
        /// nor the implementation graph.</exception>
        public override void RemoveNode(Node node, bool orphansBecomeRoots = false)
        {
            if (!AnalysisInitialized)
            {
                base.RemoveNode(node, orphansBecomeRoots);
                return;
            }
            if (node.IsInArchitecture())
            {
                RemoveFromArchitecture(node, orphansBecomeRoots);
            }
            else if (node.IsInImplementation())
            {
                RemoveFromImplementation(node, orphansBecomeRoots);
            }
            else
            {
                throw new NotSupportedException("Given node must be either in architecture or implementation graph!");
            }
        }

        /// <summary>
        /// Adds the given <paramref name="edge"/> to the reflexion graph,
        /// adjusting the reflexion analysis incrementally.
        ///
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="edge"/> is either contained in the architecture or implementation graph.</li>
        /// <li>If added to the architecture graph, the newly created specified edge will not be redundant.</li>
        /// </ul>
        /// Postcondition: The given <paramref name="edge"/> is contained in the reflexion graph
        /// and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the edge to add to the graph</param>
        /// <exception cref="NotSupportedException">When <paramref name="edge"/>
        /// is not contained in the architecture or implementation graph.</exception>
        /// <exception cref="RedundantSpecifiedEdgeException">When the <paramref name="edge"/>
        /// would be redundant to another specified edge.</exception>
        public override void AddEdge(Edge edge)
        {
            if (!AnalysisInitialized)
            {
                base.AddEdge(edge);
                return;
            }
            switch (DetermineSubgraph(edge))
            {
                case Architecture:
                    AddToArchitecture(edge);
                    break;
                case Implementation:
                    AddToImplementation(edge);
                    break;
                case Mapping: throw new NotSupportedException("Call `AddToMapping` if you wish to create a MapsTo edge.");
                default: throw new NotSupportedException("Given edge must either be in architecture or implementation graph!");
            }
        }

        /// <summary>
        /// Adds a new mapping edge from <paramref name="from"/> to <paramref name="to"/>.
        /// Convenience wrapper around <see cref="AddEdge(SEE.DataModel.DG.Node, SEE.DataModel.DG.Node, string)"/>.
        /// </summary>
        public Edge AddEdge(Node from, Node to) => AddEdge(from, to, MapsToType);

        /// <summary>
        /// Creates an edge of given <paramref name="type"/> (or Maps_To if no type was given)
        /// from the given node <paramref name="from"/>
        /// to the given node <paramref name="to"/> and adds it to a fitting subgraph of the reflexion graph,
        /// adjusting the reflexion analysis incrementally.
        ///
        /// If both <paramref name="from"/> and <paramref name="to"/> belong to the architecture graph, the new edge
        /// will be added to the architecture graph as well (vice versa for the implementation graph.)
        /// If <paramref name="from"/> belongs to the implementation graph and <paramref name="to"/> belongs to the
        /// architecture graph, the new edge will be added as a Mapping edge (<paramref name="type"/>
        /// must be <c>null</c> in such a case!).
        /// Any other case will result in a <see cref="NotSupportedException"/>.
        ///
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="from"/> is contained in the reflexion graph.</li>
        /// <li><paramref name="to"/> is contained in the reflexion graph.</li>
        /// <li>If added to the architecture graph, the newly created specified edge will not be redundant.</li>
        /// <li>If added to the mapping graph, <paramref name="from"/> must not already be explicitly mapped.</li>
        /// </ul>
        /// Postcondition: A new edge from <paramref name="from"/> to <paramref name="to"/> is contained in the
        ///   reflexion graph and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">source node of the new edge</param>
        /// <param name="to">target node of the new edge</param>
        /// <param name="type">type of the new edge</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="from"/> or <paramref name="to"/>
        /// is not contained in the reflexion graph.</exception>
        /// <exception cref="RedundantSpecifiedEdgeException">When the edge from <paramref name="from"/> to
        /// <paramref name="to"/> would be redundant to another specified edge.</exception>
        /// <exception cref="AlreadyExplicitlyMappedException">When <paramref name="from"/> is already explicitly
        /// mapped to an architecture node and <paramref name="to"/> is in the architecture graph.</exception>
        public override Edge AddEdge(Node from, Node to, string type)
        {
            if (!AnalysisInitialized)
            {
                return base.AddEdge(from, to, type);
            }
            ReflexionSubgraphs fromSub = DetermineSubgraph(from);
            ReflexionSubgraphs toSub = DetermineSubgraph(to);
            switch (fromSub, toSub)
            {
                case (Architecture, Architecture):
                    AssertOrThrow(type != null,
                                  () => new ArgumentException($"{nameof(type)} must be set when adding an architecture edge!"));
                    return AddToArchitecture(from, to, type);
                case (Implementation, Implementation):
                    AssertOrThrow(type != null,
                                  () => new ArgumentException($"{nameof(type)} must be set when adding an implementation edge!"));
                    return AddToImplementation(from, to, type);
                case (Implementation, Architecture):
                    AssertOrThrow(type == MapsToType,
                                  () => new ArgumentException($"{nameof(type)} must not be set when adding a mapping!"));
                    return AddToMapping(from, to);
                default: throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Unsupported method. Do not call this on <see cref="ReflexionGraph"/> once it was
        /// initialized, i.e., when <see cref="AnalysisInitialized"/> is true.
        ///
        /// If <see cref="AnalysisInitialized"/> is false, this method is equivalent to
        /// <see cref="Graph.AddSingleRoot(out Node, string, string)"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">thrown in case the </exception>
        public override bool AddSingleRoot(out Node root, string name = null, string type = null)
        {
            if (!AnalysisInitialized)
            {
                return base.AddSingleRoot(out root, name, type);
            }
            else
            {
                throw new NotSupportedException("Cannot change the virtual root of a reflexion graph!");
            }
        }

        #endregion

        /// <summary>
        /// Unparents the given <paramref name="child"/> from its parent.
        /// Refer to <see cref="UnparentInArchitecture"/> or <see cref="UnparentInImplementation"/> for details.
        /// </summary>
        /// <param name="child">The node to unparent</param>
        public void Unparent(Node child)
        {
            if (!AnalysisInitialized)
            {
                child.Reparent(null);
                return;
            }

            switch (DetermineSubgraph(child))
            {
                case Architecture:
                    UnparentInArchitecture(child);
                    break;
                case Implementation:
                    UnparentInImplementation(child);
                    break;
                default:
                    throw new NotSupportedException("Given node must be in architecture or implementation graph!");
            }
        }

        /// <summary>
        /// Adds given <paramref name="child"/> as a direct descendant of given <paramref name="parent"/>
        /// in the node hierarchy of the reflexion graph.
        /// Precondition: <paramref name="child"/> and <paramref name="parent"/> must be contained in the
        /// same subgraph (i.e., implementation or architecture); <paramref name="child"/> has no current parent.
        /// Postcondition: <paramref name="parent"/> is a parent of <paramref name="child"/> in the
        /// reflexion graph and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <param name="parent">parent node</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="child"/> or <paramref name="parent"/>
        /// is not contained in the implementation graph.</exception>
        /// <exception cref="CyclicHierarchyException">When adding <paramref name="child"/> as a child of
        /// <paramref name="parent"/> would result in cycles in the part-of hierarchy.</exception>
        /// <exception cref="NotAnOrphanException">When the given <paramref name="child"/>
        /// already has a parent.</exception>
        public void AddChild(Node child, Node parent)
        {
            if (!AnalysisInitialized)
            {
                parent.AddChild(child);
                return;
            }

            ReflexionSubgraphs parentSubgraph = DetermineSubgraph(parent);
            ReflexionSubgraphs childSubgraph = DetermineSubgraph(child);
            if (parentSubgraph != childSubgraph)
            {
                throw new NotSupportedException("Child and parent must be in the same graph!");
            }
            switch (parentSubgraph)
            {
                case Architecture:
                    AddChildInArchitecture(child, parent);
                    break;
                case Implementation:
                    AddChildInImplementation(child, parent);
                    break;
                default:
                    throw new NotSupportedException("Given parent must be in architecture or implementation graph!");
            }
        }

        /// <summary>
        /// Determines the subgraph this <see cref="element"/> was likely intentioned to have.
        /// Not all callers of AddNode/AddEdge are "reflexion-aware", so we need to
        /// check if we can determine a fitting subgraph type.
        ///
        /// Pre-condition: <paramref name="element"/> is not null.
        /// Post-condition: Returned subgraph is either Architecture, Implementation, None,
        ///                 or (only if <paramref name="element"/> is an edge) Mapping.
        /// </summary>
        /// <param name="element">Non-null node or edge whose subgraph shall be determined</param>
        /// <returns></returns>
        private static ReflexionSubgraphs DetermineSubgraph(GraphElement element)
        {
            ReflexionSubgraphs subgraph = element.GetSubgraph();
            if (subgraph != Architecture && subgraph != Implementation && (subgraph != Mapping || element is Node))
            {
                // No subgraph has been explicitly assigned to this element.
                // We try to determine it from its parent, or if this is an edge, from the nodes it is connected to.
                switch (element)
                {
                    // Node with a parent:
                    case Node { Parent: not null } node:
                        subgraph = DetermineSubgraph(node.Parent);
                        break;
                    case Edge edge:
                        {
                            subgraph = DetermineSubgraph(edge.Source);
                            ReflexionSubgraphs target = DetermineSubgraph(edge.Target);
                            if (subgraph == Implementation && target == Architecture)
                            {
                                // If edge had the MapsTo type, its subgraph would already reflect that.
                                throw new NotSupportedException("Mapping edge must have type MapsTo!\n"
                                                                + $"(Offending edge: {edge.ToShortString()})");
                            }
                            if (subgraph != target)
                            {
                                throw new NotSupportedException("Edge must be connected to nodes within the same graph!\n"
                                                                + $"(Offending edge: {edge.ToShortString()})");
                            }
                            break;
                        }
                    default:
                        subgraph = None;
                        break;
                }
            }
            return subgraph;
        }
    }
}
