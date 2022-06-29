/*
Copyright (C) Axivion GmbH, 2011-2020

@author Rainer Koschke, Falko Galperin
Initially written in C++ on Jul 24, 2011.
Rewritten in C# on Jan 14, 2020.
Refactored to work on a single graph on Feb 17, 2022.
Incremental Reflexion Analysis operations implemented in April 2022.

Purpose:
Implements incremental reflexion analysis. For detailed documentation refer to
the article:
"Incremental Reflexion Analysis", Rainer Koschke, Journal on Software Maintenance
and Evolution, 2011, DOI 10.1002/smr.542

The reflexion analysis calculates the reflexion graph describing the convergences,
absences, divergences between an architecture and its implementation. The following
graphs are needed to calculate the reflexion graph:

- architecture defines the expected architectural entities and their dependencies
(architectural entities may be hierarchical modeled by the part-of relation)
- the implementation model is represented in two separated graphs:
hierarchy graph describes the nesting of implementation entities
dependency graph describes the dependencies among the implementation entities
- mapping graph describes the partial mapping of implementation entities onto
architectural entities.

Internally, the reflexion analysis operates on a single graph, distinguishing between
architecture and implementation by checking a corresponding attribute, whereas the
mapping graph simply consists of all Maps_To edges and their connected nodes.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using UnityEngine;
using static SEE.Game.GraphRenderer;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;
using static SEE.Tools.ReflexionAnalysis.ReflexionSubgraph;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// State of a dependency in the architecture or implementation within the reflexion model.
    /// </summary>
    public enum State
    {
        /// <summary>
        /// Initial undefined state.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Allowed propagated dependency towards a convergence; only for implementation dependencies.
        /// </summary>
        Allowed = 1,

        /// <summary>
        /// Disallowed propagated dependency (divergence); only for implementation dependencies.
        /// </summary>
        Divergent = 2,

        /// <summary>
        /// Specified architecture dependency without corresponding implementation dependency (absence);
        /// only for architecture dependencies.
        /// </summary>
        Absent = 3,

        /// <summary>
        /// Specified architecture dependency with corresponding implementation dependency (convergence);
        /// only for architecture dependencies.
        /// </summary>
        Convergent = 4,

        /// <summary>
        /// Self-usage is always implicitly allowed; only for implementation dependencies.
        /// </summary>
        ImplicitlyAllowed = 5,

        /// <summary>
        /// Absence, but "Architecture.Is_Optional" attribute set.
        /// </summary>
        AllowedAbsent = 6,

        /// <summary>
        /// Tags an architecture edge that was created by the architect,
        /// i.e., is a specified edge; this is the initial state of a specified
        /// architecture dependency; only for architecture dependencies
        /// </summary>
        Specified = 7,
        
        /// <summary>
        /// Tags an implementation edge that has not yet been mapped; only for implementation dependencies.
        /// </summary>
        Unmapped = 8
    }

    /// <summary>
    /// Implements the reflexion analysis, which compares an implementation against an expected
    /// architecture based on a mapping between the two.
    /// </summary>
    /// <remarks>
    /// Whenever we talk about the "implementation graph", "architecture graph", or "mapping graph",
    /// we are referring to a subgraph of the full reflexion graph. Namely:
    /// <ul>
    /// <li>
    /// "implementation graph" == all graph elements with the <see cref="ReflexionGraphTools.ImplementationLabel"/>
    /// </li>
    /// <li>
    /// "architecture graph" == all graph elements with the <see cref="ReflexionGraphTools.ArchitectureLabel"/>
    /// </li>
    /// <li>
    /// "mapping graph" == all edges of type <see cref="MapsToType"/> and all nodes connected to those edges
    /// </li>
    /// </ul>
    /// </remarks>
    public partial class Reflexion : Observable
    {
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
        /// This does not really run the reflexion analysis. Use
        /// method Run() to start the analysis.
        /// </remarks>
        public Reflexion(Graph implementation, Graph architecture, Graph mapping,
                         bool allowDependenciesToParents = true)
        {
            FullGraph = Assemble(architecture, implementation, mapping, "Reflexion Graph");
            AllowDependenciesToParents = allowDependenciesToParents;
        }

        /// <summary>
        /// Constructor for setting up the reflexion analysis.
        /// </summary>
        /// <param name="fullGraph">
        /// The graph containing nodes and edges for implementation, architecture and mapping, labeled
        /// using the toggle attributes <see cref="ReflexionGraphTools.ImplementationLabel"/> and
        /// <see cref="ReflexionGraphTools.ArchitectureLabel"/>.
        /// </param>
        /// <param name="allowDependenciesToParents">whether descendants may access their ancestors</param>
        /// <remarks>
        /// This does not really run the reflexion analysis. Use <see cref="Run"/> to start the analysis.
        /// </remarks>
        public Reflexion(Graph fullGraph, bool allowDependenciesToParents = true)
        {
            FullGraph = fullGraph;
            AllowDependenciesToParents = allowDependenciesToParents;
        }

        /// <summary>
        /// Runs the reflexion analysis. If an observer has registered before,
        /// the observer will receive the results via the callback Update(ChangeEvent).
        /// </summary>
        public void Run()
        {
            ConstructTransitiveMapping();
            FromScratch();
            //DumpResults();
        }

        #region Graphs

        /// <summary>
        /// The reflexion graph for which the reflexion data is calculated.
        /// Implementation and architecture nodes and edges are distinguished by the toggle attributes
        /// <see cref="ReflexionGraphTools.ImplementationLabel"/> and
        /// <see cref="ReflexionGraphTools.ArchitectureLabel"/>, whereas mapping edges have none of these attributes.
        /// </summary>
        public Graph FullGraph { get; }

        #endregion


        #region State Edge Attribute

        /// <summary>
        /// Name of the edge attribute for the state of a dependency.
        /// </summary>
        private const string StateAttribute = "Reflexion.State";

        /// <summary>
        /// Sets the state of <paramref name="edge"/> to <paramref name="newState"/>.
        /// Precondition: <paramref name="edge"/> has a state attribute.
        /// </summary>
        /// <param name="edge">edge whose state is to be set</param>
        /// <param name="newState">the state to be set</param>
        private static void SetState(Edge edge, State newState)
        {
            edge.SetInt(StateAttribute, (int)newState);
        }

        /// <summary>
        /// Transfers edge from its <paramref name="oldState"/> to <paramref name="newState"/>;
        /// notifies all observers if <paramref name="oldState"/> and <paramref name="newState"/> actually differ.
        /// </summary>
        /// <param name="edge">edge being changed</param>
        /// <param name="oldState">the old state of the edge</param>
        /// <param name="newState">the new state of the edge after the change</param>
        private void Transition(Edge edge, State oldState, State newState)
        {
            if (oldState != newState)
            {
                SetState(edge, newState);
                Notify(new EdgeChange(edge, oldState, newState));
            }
        }

        /// <summary>
        /// Returns true if <paramref name="edge"/> is a specified edge in the architecture (has one of the
        /// following states: specified, convergent, absent).
        /// Precondition: <paramref name="edge"/> must be in the architecture graph.
        /// </summary>
        /// <param name="edge">architecture dependency</param>
        /// <returns>true if edge is a specified architecture dependency</returns>
        private static bool IsSpecified(Edge edge)
        {
            AssertOrThrow(edge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, edge));
            State state = edge.State();
            return state == State.Specified || state == State.Convergent || state == State.Absent;
        }

        #endregion

        #region Edge counter attribute

        /// <summary>
        /// Name of the edge attribute for the counter of a dependency.
        /// </summary>
        /// <remarks>
        /// The counter counts the number of propagations.
        /// </remarks>
        private const string CounterAttribute = "Reflexion.Counter";

        /// <summary>
        /// Sets counter of given architecture dependency <paramref name="edge"/> to given <paramref name="value"/>.
        /// Precondition: <paramref name="edge"/> is in the architecture graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be set</param>
        /// <param name="value">value to be set</param>
        private static void SetCounter(Edge edge, int value)
        {
            AssertOrThrow(edge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, edge));
            edge.SetInt(CounterAttribute, value);
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the counter attribute of given <paramref name="edge"/>.
        /// The value may be negative.
        /// Precondition: <paramref name="edge"/> is in the architecture graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be changed</param>
        /// <param name="value">value to be added</param>
        private void AddToCounter(Edge edge, int value)
        {
            AssertOrThrow(edge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, edge));
            if (edge.TryGetInt(CounterAttribute, out int oldValue))
            {
                edge.SetInt(CounterAttribute, oldValue + value);
            }
            else
            {
                edge.SetInt(CounterAttribute, value);
            }
        }

        /// <summary>
        /// Returns the the counter of given architecture dependency <paramref name="edge"/>.
        /// Precondition: <paramref name="edge"/> is in the architecture graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be retrieved</param>
        /// <returns>the counter of <paramref name="edge"/></returns>
        public static int GetArchCounter(Edge edge)
        {
            AssertOrThrow(edge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, edge));
            return edge.TryGetInt(CounterAttribute, out int value) ? value : 0;
        }

        /// <summary>
        /// Returns the the counter of given dependency <paramref name="edge"/>.
        /// Precondition: <paramref name="edge"/> is either in the architecture or the implementation graph.
        /// </summary>
        /// <param name="edge">an architecture or implementation dependency whose counter is to be retrieved</param>
        /// <returns>the counter of <paramref name="edge"/></returns>
        public static int GetCounter(Edge edge)
        {
            if (edge.IsInArchitecture())
            {
                return GetArchCounter(edge);
            }
            else if (edge.IsInImplementation())
            {
                return GetImplCounter(edge);
            }
            else
            {
                throw new NotSupportedException($"Can't retrieve counter of edge '{edge.ToShortString()}' "
                                                + "because it's neither in implementation nor in architecture!");
            }
        }

        /// <summary>
        /// Increases counter of <paramref name="edge"/> by <paramref name="value"/> (may be negative).
        /// If its counter drops to zero, the edge is removed.
        /// Notifies if edge's state changes.
        /// Precondition: <paramref name="edge"/> is a dependency in architecture graph that
        /// was propagated from the implementation graph (i.e., !IsSpecified(<paramref name="edge"/>)).
        /// </summary>
        /// <param name="edge">propagated dependency in architecture graph</param>
        /// <param name="value">value to be added</param>
        private void ChangePropagatedDependency(Edge edge, int value)
        {
            AssertOrThrow(edge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, edge));
            AssertOrThrow(!IsSpecified(edge), () => new ExpectedPropagatedEdgeException(edge));
            int oldValue = GetArchCounter(edge);
            int newValue = oldValue + value;
            // TODO(falko17): Figure 5.b on page 10 also describes a state change to 'allowed'.
            if (newValue <= 0)
            {
                // TODO(koschke): Why was this needed — do we still need this code fragment?
                /*
                if (GetState(edge) == State.divergent)
                {
                    Transition(edge, State.divergent, State.undefined);
                }
                */
                // We can drop this edge; it is no longer needed. Because the edge is
                // dropped and all observers are informed about the removal of this
                // edge, we do not need to inform them about its state change from
                // divergent/allowed/implicitlyAllowed to undefined.
                SetCounter(edge, 0);
                Notify(new PropagatedEdgeEvent(edge, ChangeType.Removal));
                propagationTable.Remove(edge.ID);
                FullGraph.RemoveEdge(edge);
            }
            else
            {
                SetCounter(edge, newValue);
            }
        }

        /// <summary>
        /// Returns the value of the counter attribute of an implementation dependency.
        /// Currently, 1 is always returned.
        /// Precondition: <paramref name="edge"/> is in the implementation graph.
        /// </summary>
        /// <param name="edge">an implementation dependency whose counter is to be retrieved</param>
        /// <returns>value of the counter attribute of given implementation dependency</returns>
        private static int GetImplCounter(Edge edge = null)
        {
            // returns the value of the counter attribute of edge
            // at present, dependencies extracted from the source
            // do not have an edge counter; therefore, we return just 1
            return 1;
        }

        #endregion

        #region Modifiers

        // The following operations manipulate the relevant graphs of the
        // context and trigger the incremental update of the reflexion results;
        // if anything in the reflexion results changes, all observers are informed
        // by the update message.
        // Never modify the underlying graphs directly; always use the following
        // methods. Otherwise the reflexion result may be in an inconsistent state.
        //
        // Implementation detail: in case of a state change, Notify(ChangeEvent arg)
        // will be called where arg describes the type of change; such change information
        // consists of the object changed (either a single edge or node) and the kind
        // of change, namely, addition/removal or a counter increment/decrement.
        // Note that any of the following modifiers may result in a sequence of updates
        // where each single change is reported.
        // Note also that a removal of a node implies that all its incoming and outgoing
        // edges (hierarchical as well as dependencies) will be removed, too.
        // Note also that an addition of an edge will imply an implicit addition of
        // its source and target node if there are not yet contained in the target graph.

        #region Edge

        // TODO(falko17): Add parameter documentation
        /// <summary>
        /// A function delegate that can be used to handle changes of the mapping by HandleMappedSubtree.
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private delegate void HandleMappingChange(Edge edge, Node from, Node to);

        // Meaning of "propagate": copy edge from implementation to architecture
        // Meaning of "lift": move upwards through hierarchy and check convergence

        /// <summary>
        /// Handles every dependency edge (incoming as well as outgoing) of every node in given
        /// <paramref name="subtree"/> as follows:
        ///
        /// Let e = (i1, i2) be a dependency edge where either i1 or i2 or both are contained in
        /// <paramref name="subtree"/>. Then e falls into one of the following categories:
        ///  (1) inner dependency: it is in between two entities, i1 and i2, mapped onto the same entity:
        ///      mapsTo(i1) != null and mapsTo(i2) != null and mapsTo(i1) = mapsTo(i2)
        ///  (2) cross dependency: it is in between two entities, i1 and i2, mapped onto different entities:
        ///      mapsTo(i1) != null and mapsTo(i2) != null and mapsTo(i1) != mapsTo(i2)
        ///  (3) dangling dependency: it is in between two entities, i1 and i2, not yet both mapped:
        ///      mapsTo(i1) = null or mapsTo(i2) = null
        ///
        /// Dangling dependencies will be ignored. For every inner or cross dependency, e, the given
        /// <paramref name="handler"/> will be applied with the following arguments:
        ///
        ///   if e is an outgoing cross dependency, i.e., e.Source is contained in subtree:
        ///     handler(e, archNode, mapsTo(e.Target))
        ///   if e is an incoming cross dependency, i.e., e.Target is contained in subtree:
        ///     handler(e, mapsTo(e.Source), archNode)
        ///   if e is an inner dependency:
        ///     handler(e, archNode, archNode)
        ///
        /// Precondition: given <paramref name="archNode"/> is in the architecture graph and
        /// all nodes in <paramref name="subtree"/> are in the implementation graph.
        /// </summary>
        /// <param name="subtree">implementation nodes whose mapping is to be adjusted</param>
        /// <param name="archNode">architecture node related to the nodes in subtree (to be mapped or unmapped);
        /// this may either be the architecture node onto which the nodes in subtree were mapped originally
        /// when this function is called to unmap a subtree or architecture node onto which the nodes in subtree
        /// are to be mapped as new</param>
        /// <param name="handler">delegate handling the necessary adjustment</param>
        private void HandleMappedSubtree(IEnumerable<Node> subtree, Node archNode, HandleMappingChange handler)
        {
            AssertOrThrow(archNode.IsInArchitecture(), () => new NotInSubgraphException(Architecture, archNode));
            // An inner dependency may occur twice in the iteration below, once in the set
            // of outgoing edges and once in the set of incoming edges of any nodes in the subtree.
            // We may call the handler only once for these; that is why we need to keep a log of
            // inner edges already handled.
            ISet<Edge> innerEdgesAlreadyHandled = new HashSet<Edge>();
            foreach (Node implNode in subtree)
            {
                AssertOrThrow(implNode.IsInImplementation(), () => new NotInSubgraphException(Implementation, implNode));
                foreach (Edge outgoing in implNode.Outgoings.Where(x => x.IsInImplementation()))
                {
                    if (implicitMapsToTable.TryGetValue(outgoing.Target.ID, out Node oldTarget))
                    {
                        // outgoing is not dangling; it is either an inner or cross dependency
                        if (oldTarget == archNode)
                        {
                            // outgoing is an inner dependency
                            if (innerEdgesAlreadyHandled.Add(outgoing))
                            {
                                // Note: ISet.Add(e) yields true if e has not been contained in the set so far.
                                // That is, outgoing has not been processed yet.
                                handler(outgoing, archNode, archNode);
                            }
                        }
                        else
                        {
                            // outgoing is an outgoing cross dependency
                            handler(outgoing, archNode, oldTarget);
                        }
                    }
                }

                foreach (Edge incoming in implNode.Incomings.Where(x => x.IsInImplementation()))
                {
                    AssertOrThrow(incoming.IsInImplementation(), () => new NotInSubgraphException(Implementation, incoming));
                    if (implicitMapsToTable.TryGetValue(incoming.Source.ID, out Node oldTarget))
                    {
                        // incoming is not dangling; it is either an incoming cross or inner dependency
                        if (oldTarget == archNode)
                        {
                            // outgoing is an inner dependency
                            if (innerEdgesAlreadyHandled.Add(incoming))
                            {
                                // Note: ISet.Add(e) yields true if e has not been contained in the set so far.
                                // That is, incoming has not been processed yet.
                                handler(incoming, archNode, archNode);
                            }
                        }
                        else
                        {
                            handler(incoming, oldTarget, archNode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// If both <paramref name="from"/> and <paramref name="to"/> are not null,
        /// the propagated architecture dependency corresponding
        /// to given <paramref name="implementationDependency"/> is lifted where the counter of the matching specified
        /// architecture dependency and the counter of this propagated architecture dependency are decreased
        /// by the absolute value of <paramref name="implementationDependency"/>'s counter.
        /// Otherwise, nothing is done.
        /// Precondition: <paramref name="implementationDependency"/> is a dependency edge contained
        /// in implementation graph and <paramref name="to"/> and <paramref name="from"/> are
        /// contained in the architecture graph.
        /// </summary>
        /// <param name="implementationDependency">an implementation dependency whose corresponding
        /// propagated dependency in the architecture graph is to be decreased and lifted</param>
        /// <param name="from">architecture node = Maps_To(implementationDependency.Source)</param>
        /// <param name="to">architecture node = Maps_To(implementationDependency.Target)</param>
        private void DecreaseAndLift(Edge implementationDependency, Node from, Node to)
        {
            if (from != null && to != null)
            {
                AssertOrThrow(implementationDependency.IsInImplementation(), () => new NotInSubgraphException(Implementation, implementationDependency));
                AssertOrThrow(from.IsInArchitecture(), () => new NotInSubgraphException(Architecture, from));
                AssertOrThrow(to.IsInArchitecture(), () => new NotInSubgraphException(Architecture, to));
                Edge propagatedEdge = GetPropagatedDependency(from, to, implementationDependency.Type);
                // It can happen that one of the edges end is not mapped, hence, no edge was propagated.
                if (propagatedEdge != null)
                {
                    int counter = -GetImplCounter(implementationDependency);
                    if (Lift(propagatedEdge.Source, propagatedEdge.Target, propagatedEdge.Type, counter, out Edge _))
                    {
                        // matching specified architecture dependency found; no state change
                    }

                    ChangePropagatedDependency(propagatedEdge, counter);
                }
                Transition(propagatedEdge, propagatedEdge.State(), State.Unmapped);
            }
        }

        /// <summary>
        /// Propagates and lifts given <paramref name="implementationDependency"/>.
        /// Precondition: <paramref name="implementationDependency"/> is a dependency edge contained
        /// in implementation graph and <paramref name="to"/> and <paramref name="from"/>
        /// are contained in the architecture graph.
        ///
        /// </summary>
        /// <param name="implementationDependency">an implementation dependency whose corresponding propagated
        /// dependency in the architecture graph is to be increased and lifted</param>
        /// <param name="from">architecture node = MapsTo(implementationDependency.Source)</param>
        /// <param name="to">architecture node = MapsTo(implementationDependency.Target)</param>
        /// <remarks>
        /// <paramref name="from"/> and <paramref name="to"/> are actually ignored (intentionally).
        /// </remarks>
        private void IncreaseAndLift(Edge implementationDependency, Node from, Node to)
        {
            AssertOrThrow(implementationDependency.IsInImplementation(), () => new NotInSubgraphException(Implementation, implementationDependency));
            AssertOrThrow(from.IsInArchitecture(), () => new NotInSubgraphException(Architecture, from));
            AssertOrThrow(to.IsInArchitecture(), () => new NotInSubgraphException(Architecture, to));
            // safely ignore from and to
            PropagateAndLiftDependency(implementationDependency);
        }

        /// <summary>
        /// Returns the list of nodes in the subtree rooted by given <paramref name="node"/> (including this
        /// node itself) excluding those descendants in nested subtrees rooted by a mapper node,
        /// that is, are mapped elsewhere.
        /// Precondition: <paramref name="node"/> is contained in implementation graph and not Is_Mapper(node).
        /// Postcondition: all nodes in the result are in the implementation graph and mapped
        /// onto the same architecture node as the given node; the given node is included
        /// in the result.
        /// </summary>
        /// <param name="node">root node of the subtree</param>
        private List<Node> MappedSubtree(Node node)
        {
            AssertOrThrow(node.IsInImplementation(), () => new NotInSubgraphException(Implementation, node));
            return node.Children().Where(x => !IsExplicitlyMapped(x))
                       .SelectMany(MappedSubtree).Prepend(node).ToList();
        }

        #endregion

        #endregion

        #region Summaries

        /// <summary>
        /// Prints a summary of the number of edges in each state using Unity's standard logger.
        /// Is equivalent to PrintSummary(Summary()).
        /// </summary>
        public void PrintSummary()
        {
            PrintSummary(Summary());
        }

        /// <summary>
        /// Prints the given <paramref name="summary"/> of the number of edges in each state
        /// using Unity's standard logger. The argument <paramref name="summary"/> can be
        /// computed by Summary(). It is assumed to have as many entries as there are different
        /// State values. When indexed by a State value, it yields the number of edges in the
        /// architecture that are in this state.
        /// </summary>
        /// <param name="summary">Number of edges in the architecture indexed by state value</param>
        public static void PrintSummary(int[] summary)
        {
            string[] stateNames = Enum.GetNames(typeof(State));
            foreach (int stateValue in Enum.GetValues(typeof(State)))
            {
                Debug.Log($"number of edges in state {stateNames[stateValue]} = {summary[stateValue]}\n");
            }
        }

        /// <summary>
        /// Yields a summary of the number of edges in the architecture for each respective state.
        /// The result has as many entries as there are different State values. When indexed by a
        /// State value, it yields the number of edges in the architecture that are in this state.
        /// For instance, Summary()[(int)State.divergent] gives the number of architecture edges
        /// that are in state divergent.
        /// </summary>
        /// <returns>summary of the number of edges in the architecture for each respective state</returns>
        public int[] Summary()
        {
            string[] stateNames = Enum.GetNames(typeof(State));
            int[] summary = new int[stateNames.Length];

            foreach (Edge edge in FullGraph.Edges().Where(x => x.IsInArchitecture()))
            {
                summary[(int)edge.State()] += GetArchCounter(edge);
            }

            return summary;
        }

        /// <summary>
        /// Whether descendants may implicitly access their ancestors.
        /// </summary>
        private readonly bool AllowDependenciesToParents;

        #endregion

        #region Node and Edge predicates

        /// <summary>
        /// Returns false for given node if it should be ignored in the reflexion analysis.
        /// For instance, artificial nodes, template instances, and nodes with ambiguous definitions
        /// are to be ignored.
        /// Precondition: node is a node in the implementation graph.
        /// </summary>
        /// <param name="node">implementation node</param>
        /// <returns>true if node should be considered in the reflexion analysis</returns>
        private bool IsRelevant(Node node)
        {
            return true;
            // FIXME: For the time being, we consider every node to be relevant.

            //if (source_node->has_value(_node_is_artificial_attribute)
            //    && !source_node->has_value(_node_is_inherited_attribute))
            //{
            //    return false;
            //}
            //else if (source_node->has_value(_node_is_template_instance_attribute))
            //{
            //    return false;
            //}
            //else if (source_node->has_value(_node_has_ambiguous_definition_attribute))
            //{
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
        }

        /// <summary>
        /// Returns false for given edge if it should be ignored in the reflexion analysis.
        /// For instance, artificial edges and edges for which at least one end is an irrelevant node
        /// are to be ignored.
        /// Precondition: edge is an edge in the implementation graph.
        /// </summary>
        /// <param name="edge">implementation dependency</param>
        /// <returns>true if edge should be considered in the reflexion analysis</returns>
        private bool IsRelevant(Edge edge)
        {
            return IsRelevant(edge.Source) && IsRelevant(edge.Target);
            // FIXME: For the time being, we consider every edge to be relevant as long as their
            // source and target are relevant.
        }

        /// <summary>
        /// Returns true if the given <paramref name="edge"/> has been propagated.
        ///
        /// Pre-condition: <paramref name="edge"/> is contained in the implementation.
        /// </summary>
        /// <param name="edge">The edge which should be checked</param>
        /// <returns>True iff <paramref name="edge"/> has been propagated</returns>
        private bool IsPropagated(Edge edge)
        {
            AssertOrThrow(edge.IsInImplementation(), () => new NotInSubgraphException(Implementation, edge));
            return MapsTo(edge.Source) != null && MapsTo(edge.Target) != null;
        }

        #endregion

        #region Mapping

        /// <summary>
        /// The implicit mapping as derived from explicitMapsToTable.
        /// Note 1: the mapping key is the ID of a node in implementation and the mapping value a node in architecture
        /// Note 2: not every node in implementation is a key in this dictionary; node in the implementation
        /// neither mapped explicitly nor implicitly will not be contained.
        /// </summary>
        private Dictionary<string, Node> implicitMapsToTable;

        /// <summary>
        /// The explicit mapping from implementation node ID onto architecture nodes
        /// as derived from the mappings graph. This is equivalent to the content
        /// of the mapping graph where the corresponding nodes of the implementation
        /// (source of a mapping) and architecture (target of a mapping) are used.
        /// The correspondence of nodes between these three graphs is established
        /// by way of the unique ID attribute.
        /// Note: key is a node in the implementation and target a node in the
        /// architecture graph.
        /// </summary>
        private Dictionary<string, Node> explicitMapsToTable;

        /// <summary>
        /// Creates the transitive closure for the mapping so that we
        /// know immediately where an implementation entity is mapped to.
        /// The result is stored in explicitMapsToTable and implicitMapsToTable.
        /// </summary>
        private void ConstructTransitiveMapping()
        {
            // Because AddSubtreeToImplicitMap() will check whether a node is a
            // mapper, which is done by consulting explicitMapsToTable, we need
            // to first create the explicit mapping and can only then map the otherwise
            // unmapped children
            explicitMapsToTable = new Dictionary<string, Node>();
            foreach (Edge mapsTo in FullGraph.Edges().Where(x => x.IsInMapping()))
            {
                Node source = mapsTo.Source;
                Node target = mapsTo.Target;
                AssertOrThrow(source.IsInImplementation(), () => new NotInSubgraphException(Implementation, source));
                AssertOrThrow(target.IsInArchitecture(), () => new NotInSubgraphException(Architecture, target));
                explicitMapsToTable[source.ID] = target;
                //explicitMapsToTable[InImplementation[source.ID]] = InArchitecture[target.ID];
            }

            implicitMapsToTable = new Dictionary<string, Node>();

            foreach (Edge mapsTo in FullGraph.Edges().Where(x => x.IsInMapping()))
            {
                Node source = mapsTo.Source;
                Node target = mapsTo.Target;
                AssertOrThrow(source.IsInImplementation(), () => new NotInSubgraphException(Implementation, source));
                AssertOrThrow(target.IsInArchitecture(), () => new NotInSubgraphException(Architecture, target));
                AddSubtreeToImplicitMap(source, target);
                
                // We'll now also notify our observer's that a "new" mapping edge exists.
                Notify(new EdgeEvent(mapsTo, ChangeType.Addition, Mapping));
            }
        }

        /// <summary>
        /// Returns true if <paramref name="node"/> is explicitly mapped, that is,
        /// contained in <see cref="explicitMapsToTable"/>  as a key.
        /// Precondition: <paramref name="node"/> is a node of the implementation graph
        /// </summary>
        /// <param name="node">implementation node</param>
        /// <returns>true if node is explicitly mapped</returns>
        public bool IsExplicitlyMapped(Node node) => explicitMapsToTable.ContainsKey(node.ID);

        /// <summary>
        /// Adds all descendants of <paramref name="root"/> in the implementation that are implicitly
        /// mapped onto the same target as <paramref name="root"/> to the implicit mapping table
        /// <see cref="implicitMapsToTable"/> and maps them onto <paramref name="target"/>. This function recurses
        /// into all subtrees unless the root of a subtree is an explicitly mapped node.
        /// Preconditions:
        /// (1) <paramref name="root"/> is a node in implementation
        /// (2) <paramref name="target"/> is a node in architecture
        /// </summary>
        /// <param name="root">implementation node that is the root of a subtree to be mapped implicitly</param>
        /// <param name="target">architecture node that is the target of the implicit mapping</param>
        private void AddSubtreeToImplicitMap(Node root, Node target)
        {
            AssertOrThrow(root.IsInImplementation(), () => new NotInSubgraphException(Implementation, root));
            AssertOrThrow(target.IsInArchitecture(), () => new NotInSubgraphException(Architecture, target));
            IList<Node> children = root.Children();
            foreach (Node child in children)
            {
                // child is contained in implementation
                if (!IsExplicitlyMapped(child))
                {
                    AddSubtreeToImplicitMap(child, target);
                }
            }

            implicitMapsToTable[root.ID] = target;
        }

        #endregion


        #region DG Utilities

        /// <summary>
        /// Map from IDs of propagated edges to their originating edge, i.e., the edge they were propagated from.
        /// </summary>
        private readonly IDictionary<string, Edge> propagationTable = new Dictionary<string, Edge>();

        /// <summary>
        /// Returns the edge from which the given <paramref name="propagatedEdge"/> was propagated or <c>null</c>
        /// if no such edge exists.
        ///
        /// Pre-conditions:
        /// - Given <paramref name="propagatedEdge"/> is in the architecture graph.
        ///
        /// Post-conditions:
        /// - Returned edge is in the implementation graph or null.
        /// </summary>
        /// <param name="propagatedEdge">Propagated edge whose originating edge shall be returned</param>
        /// <returns>Edge from which <paramref name="propagatedEdge"/> was propagated or null</returns>
        public Edge GetOriginatingEdge(Edge propagatedEdge)
        {
            AssertOrThrow(propagatedEdge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, propagatedEdge));
            return propagationTable.TryGetValue(propagatedEdge.ID, out Edge edge) ? edge : null;
        }

        /// <summary>
        /// Adds value to counter of <paramref name="edge"/> and transforms its state.
        /// Notifies if edge state changes.
        /// Precondition: <paramref name="edge"/> is in architecture graph and specified.
        /// </summary>
        /// <param name="edge">architecture dependency to be changed</param>
        /// <param name="value">the value to be added to the edge's counter</param>
        private void ChangeSpecifiedDependency(Edge edge, int value)
        {
            AssertOrThrow(edge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, edge));
            AssertOrThrow(IsSpecified(edge), () => new ExpectedSpecifiedEdgeException(edge));
            int oldValue = GetArchCounter(edge);
            int newValue = oldValue + value;
            State state = edge.State();

            if (oldValue == 0)
            {
                Transition(edge, state, State.Convergent);
            }
            else if (newValue == 0)
            {
                Transition(edge, state, State.Absent);
            }

            SetCounter(edge, newValue);
        }

        #endregion

        #region Analysis Steps

        /// <summary>
        /// Runs reflexion analysis from scratch, i.e., non-incrementally.
        /// </summary>
        private void FromScratch()
        {
            Reset();
            CalculateConvergencesAndDivergences();
            CalculateAbsences();
        }

        /// <summary>
        /// Resets architecture markings and implementation states.
        /// </summary>
        private void Reset()
        {
            ResetArchitecture();
            ResetImplementation();
        }

        /// <summary>
        /// Sets the state of all implementation dependencies to 'unmapped'.
        /// </summary>
        private void ResetImplementation()
        {
            foreach (Edge edge in FullGraph.Edges().Where(x => x.IsInImplementation()))
            {
                Transition(edge, edge.State(), State.Unmapped);
            }
        }

        /// <summary>
        /// The state of all architectural dependencies will be set to 'undefined'
        /// and their counters be set to zero again. Propagated dependencies are
        /// removed.
        /// </summary>
        private void ResetArchitecture()
        {
            List<Edge> toBeRemoved = new List<Edge>();

            foreach (Edge edge in FullGraph.Edges().Where(x => x.IsInArchitecture()))
            {
                State state = edge.State();
                switch (state)
                {
                    case State.Undefined:
                    case State.Specified:
                        SetCounter(edge, 0); // Note: architecture edges have a counter
                        Transition(edge, state, State.Specified); // initial state must be State.specified
                        break;
                    case State.Absent:
                    case State.Convergent:
                        // The initial state of an architecture dependency that was not propagated is specified.
                        Transition(edge, state, State.Specified);
                        SetCounter(edge, 0); // Note: architecture edges have a counter
                        break;
                    case State.Allowed:
                    case State.Divergent:
                    case State.ImplicitlyAllowed:
                    case State.AllowedAbsent:
                        // The edge is a left-over from a previous analysis and should be
                        // removed. Before we actually do that, we need to notify all observers.
                        Notify(new PropagatedEdgeEvent(edge, ChangeType.Removal));
                        // No need to delete from propagationTable, as we clear that at the end anyway.
                        toBeRemoved.Add(edge);
                        break;
                    default:
                        // Also need to clear here, otherwise we're in a bad state.
                        propagationTable.Clear();
                        throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown state encountered!");
                }
            }

            propagationTable.Clear();

            // Removal of edges from architecture must be done outside of the loop
            // because the loop iterates on architecture.Edges().
            foreach (Edge edge in toBeRemoved)
            {
                FullGraph.RemoveEdge(edge);
            }
        }

        /// <summary>
        /// Calculates convergences and divergences non-incrementally.
        /// </summary>
        private void CalculateConvergencesAndDivergences()
        {
            // Iterate on all nodes in the domain of implicitMapsToTable
            // (N.B.: these are nodes that are in 'implementation'), and
            // propagate and lift their dependencies in the architecture
            foreach (KeyValuePair<string, Node> mapsTo in implicitMapsToTable)
            {
                Node sourceNode = FullGraph.GetNode(mapsTo.Key);
                AssertOrThrow(sourceNode.IsInImplementation(), () => new NotInSubgraphException(Implementation, sourceNode));
                if (IsRelevant(sourceNode))
                {
                    // Node is contained in implementation graph and implicitMapsToTable
                    PropagateAndLiftOutgoingDependencies(sourceNode);
                }
            }
        }

        /// <summary>
        /// Calculates absences non-incrementally.
        /// Pre-condition: <see cref="CalculateConvergencesAndDivergences"/> has been called previously.
        /// </summary>
        private void CalculateAbsences()
        {
            // after CalculateConvergesAndDivergences() all
            // architectural dependencies not marked as 'convergent'
            // are 'absent' (unless the architecture edge is marked 'optional')
            foreach (Edge edge in FullGraph.Edges().Where(x => x.IsInArchitecture()))
            {
                State state = edge.State();
                if (IsSpecified(edge) && state != State.Convergent)
                {
                    Transition(edge, state,
                               edge.HasToggle("Architecture.Is_Optional") ? State.AllowedAbsent : State.Absent);
                }
            }
        }

        /// <summary>
        /// Returns propagated dependency in architecture graph matching the type of
        /// the implementation dependency Edge exactly if one exists;
        /// returns null if none can be found.
        /// Precondition: <paramref name="source"/> and <paramref name="target"/>
        /// are two architecture entities in architecture
        /// graph onto which an implementation dependency of type <paramref name="itsType"/> was
        /// possibly propagated.
        /// Postcondition: resulting edge is in architecture or null
        /// </summary>
        /// <param name="source">source node of propagated dependency in architecture</param>
        /// <param name="target">target node of propagated dependency in architecture</param>
        /// <param name="itsType">the edge type of the propagated dependency</param>
        /// <returns>the propagated edge in the architecture graph between source and target
        /// with given type; null if there is no such edge</returns>
        private static Edge GetPropagatedDependency(
            Node source, // source of edge; must be in architecture
            Node target, // target of edge; must be in architecture
            string itsType) // the edge type that must match exactly
        {
            AssertOrThrow(source.IsInArchitecture(), () => new NotInSubgraphException(Architecture, source));
            AssertOrThrow(target.IsInArchitecture(), () => new NotInSubgraphException(Architecture, target));
            // There may be multiple (more precisely, two or less) edges from source to target with itsType,
            // but at most one that was specified by the user in the architecture model (we assume that
            // the architecture graph does not have redundant specified dependencies).
            // All others (more precisely, at most one) are dependencies that were propagated from the
            // implementation graph to the architecture graph.
            return source.FromTo(target, itsType).SingleOrDefault(edge => !IsSpecified(edge));
        }

        /// <summary>
        /// Propagates and lifts dependency edge from implementation to architecture graph.
        ///
        /// Precondition: <paramref name="implementationDependency"/> is in implementation graph.
        /// </summary>
        /// <param name="implementationDependency">the implementation edge to be propagated</param>
        private void PropagateAndLiftDependency(Edge implementationDependency)
        {
            AssertOrThrow(implementationDependency.IsInImplementation(), () => new NotInSubgraphException(Implementation, implementationDependency));
            Node implSource = implementationDependency.Source;
            Node implTarget = implementationDependency.Target;
            AssertOrThrow(implSource.IsInImplementation(), () => new NotInSubgraphException(Implementation, implSource));
            AssertOrThrow(implTarget.IsInImplementation(), () => new NotInSubgraphException(Implementation, implTarget));
            string implType = implementationDependency.Type;

            Node archSource = MapsTo(implSource);
            Node archTarget = MapsTo(implTarget);

            if (archSource == null || archTarget == null)
            {
                // source or target are not mapped; so we cannot do anything
                return;
            }

            AssertOrThrow(archSource.IsInArchitecture(), () => new NotInSubgraphException(Architecture, archSource));
            AssertOrThrow(archTarget.IsInArchitecture(), () => new NotInSubgraphException(Architecture, archTarget));
            Edge architectureDependency = GetPropagatedDependency(archSource, archTarget, implType);
            AssertOrThrow(architectureDependency == null || architectureDependency.IsInArchitecture(),
                          () => new NotInSubgraphException(Architecture, architectureDependency));
            Edge allowingEdge;
            if (architectureDependency == null)
            {
                // a propagated dependency has not existed yet; we need to create one
                architectureDependency = NewImplDepInArchitecture(archSource, archTarget, implType, implementationDependency, out allowingEdge);
                AssertOrThrow(architectureDependency.IsInArchitecture(), () => new NotInSubgraphException(Architecture, architectureDependency));
            }
            else
            {
                // a propagated dependency exists already
                int implCounter = GetImplCounter(implementationDependency);
                AssertOrThrow(architectureDependency.Source.IsInArchitecture(),
                              () => new NotInSubgraphException(Architecture, architectureDependency.Source));
                AssertOrThrow(architectureDependency.Target.IsInArchitecture(),
                              () => new NotInSubgraphException(Architecture, architectureDependency.Target));
                Lift(architectureDependency.Source, architectureDependency.Target,
                     implType, implCounter, out allowingEdge);
                ChangePropagatedDependency(architectureDependency, implCounter);
            }
            // TODO: keep a trace of dependency propagation
            //causing.insert(std::pair<Edge*, Edge*>
            // (allowing_edge ? allowing_edge : architecture_dep, implementationDependency));
        }

        /// <summary>
        /// Propagates the outgoing dependencies of <paramref name="node"/> from implementation to architecture
        /// graph and lifts them in architecture (if and only if an outgoing dependency is
        /// relevant).
        ///
        /// Precondition: <paramref name="node"/> is in implementation graph.
        /// </summary>
        /// <param name="node">implementation node whose outgoings are to be propagated and lifted</param>
        private void PropagateAndLiftOutgoingDependencies(Node node)
        {
            AssertOrThrow(node.IsInImplementation(), () => new NotInSubgraphException(Implementation, node));
            foreach (Edge edge in node.Outgoings.Where(x => x.IsInImplementation()))
            {
                // edge is in implementation
                // only relevant dependencies may be propagated and lifted
                if (IsRelevant(edge))
                {
                    PropagateAndLiftDependency(edge);
                }
            }
        }

        /// <summary>
        /// Returns the architecture node upon which <paramref name="node"/> is mapped;
        /// if <paramref name="node"/> is not mapped, null is returned.
        /// Precondition: <paramref name="node"/> is in implementation.
        /// Postcondition: either result is null or result is in architecture
        /// </summary>
        /// <param name="node"></param>
        /// <returns>the architecture node upon which node is mapped or null</returns>
        private Node MapsTo(Node node)
        {
            AssertOrThrow(node.IsInImplementation(), () => new NotInSubgraphException(Implementation, node));
            return implicitMapsToTable.TryGetValue(node.ID, out Node target) ? target : null;
        }

        /// <summary>
        /// Returns true if this causing implementation edge is a dependency from child to
        /// parent in the sense of the <see cref="AllowDependenciesToParents"/> option.
        ///
        /// Precondition: <paramref name="edge"/> is in implementation graph.
        /// </summary>
        /// <param name="edge">dependency edge to be checked</param>
        /// <returns>true if this causing edge is a dependency from child to parent</returns>
        private bool IsDependencyToParent(Edge edge)
        {
            AssertOrThrow(edge.IsInImplementation(), () => new NotInSubgraphException(Implementation, edge));
            Node mappedSource = MapsTo(edge.Source);
            Node mappedTarget = MapsTo(edge.Target);

            if (mappedSource != null && mappedTarget != null)
            {
                AssertOrThrow(mappedSource.IsInArchitecture(), () => new NotInSubgraphException(Architecture, mappedSource));
                AssertOrThrow(mappedTarget.IsInArchitecture(), () => new NotInSubgraphException(Architecture, mappedTarget));
                return IsDescendantOf(mappedSource, mappedTarget);
            }

            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="descendant"/> is a descendant of <paramref name="ancestor"/>
        /// in the node hierarchy.
        ///
        /// Precondition: <paramref name="descendant"/> and <paramref name="ancestor"/> are in the same graph.
        /// </summary>
        /// <param name="descendant">source node</param>
        /// <param name="ancestor">target node</param>
        /// <returns>true if <paramref name="descendant"/> is a descendant of <paramref name="ancestor"/></returns>
        private static bool IsDescendantOf(Node descendant, Node ancestor)
        {
            Node cursor = descendant.Parent;
            while (cursor != null && cursor != ancestor)
            {
                cursor = cursor.Parent;
            }

            return cursor == ancestor;
        }

        /// <summary>
        /// Creates and returns a new edge of type <paramref name="itsType"/> from <paramref name="from"/> to
        /// <paramref name="to"/> with given <paramref name="label"/>
        /// Use this function for source dependencies.
        /// Source dependencies are a special case because there may be two
        /// equivalent source dependencies between the same node pair: one
        /// specified and one propagated.
        ///
        /// Precondition: <paramref name="from"/> and <paramref name="to"/> are already in the graph.
        /// </summary>
        /// <param name="from">the source of the edge</param>
        /// <param name="to">the target of the edge</param>
        /// <param name="itsType">the type of the edge</param>
        /// <returns>the new edge</returns>
        private Edge AddEdge(Node from, Node to, string itsType)
        {
            // Note: a propagated edge between the same two architectural entities may be specified as well;
            // hence, we may have multiple edges in between.
            // Because of that and because the edge's ID is generated based on its source, target, and type,
            // we need to set the ID ourselves to make sure it is unique.
            Edge result = new Edge(from, to, itsType)
            {
                // The edge ID must be changed before the edge is added to the graph.
                ID = Guid.NewGuid().ToString()
            };
            if (from.IsInImplementation())
            {
                AssertOrThrow(to.IsInImplementation(), () => new NotInSubgraphException(Implementation, to));
                result.SetInImplementation();
            }
            else if (from.IsInArchitecture())
            {
                AssertOrThrow(to.IsInArchitecture(), () => new NotInSubgraphException(Architecture, to));
                result.SetInArchitecture();
            }

            FullGraph.AddEdge(result);
            return result;
        }

        /// <summary>
        /// Adds a propagated dependency to the architecture graph
        /// from <paramref name="archSource"/> to <paramref name="archTarget"/> with given <paramref name="edgeType"/>.
        /// This edge is lifted to an allowing specified architecture dependency if there is one
        /// (if that is the case the specified architecture dependency allowing this implementation
        /// dependency is returned in the output parameter allowingEdgeOut). The state
        /// of the allowing specified architecture dependency is set to convergent if
        /// such an edge exists. Likewise, the state of the propagated dependency is
        /// set to either allowed, implicitlyAllowed, or divergent.
        ///
        /// Preconditions:
        /// (1) there is no propagated edge from <paramref name="archSource"/> to <paramref name="archTarget"/>
        /// with the given <paramref name="edgeType"/> yet
        /// (2) <paramref name="archSource"/> and <paramref name="archTarget"/> are in the architecture graph
        /// (3) <paramref name="originatingEdge"/> is in the implementation graph.
        /// Postcondition: the newly created and returned dependency is contained in
        /// the architecture graph and marked as propagated.
        /// </summary>
        /// <param name="archSource">architecture node that is the source of the propagated edge</param>
        /// <param name="archTarget">architecture node that is the target of the propagated edge</param>
        /// <param name="edgeType">type of the propagated implementation edge</param>
        /// <param name="originatingEdge">Implementation edge from which the newly created edge will originate</param>
        /// <param name="allowingEdgeOut">the specified architecture dependency allowing the implementation
        /// dependency if there is one; otherwise null; allowingEdgeOut is also null if the implementation
        /// dependency form a self-loop (archSource == archTarget); self-dependencies are implicitly
        /// allowed, but do not necessarily have a specified architecture dependency</param>
        /// <returns>a new propagated dependency in the architecture graph</returns>
        private Edge NewImplDepInArchitecture(Node archSource, Node archTarget, string edgeType, Edge originatingEdge, out Edge allowingEdgeOut)
        {
            AssertOrThrow(archSource.IsInArchitecture(), () => new NotInSubgraphException(Architecture, archSource));
            AssertOrThrow(archTarget.IsInArchitecture(), () => new NotInSubgraphException(Architecture, archTarget));
            AssertOrThrow(originatingEdge.IsInImplementation(), () => new NotInSubgraphException(Implementation, originatingEdge));
            Edge alreadyPropagated = FullGraph.Edges().FirstOrDefault(x => x.Source == archSource
                                                                           && x.Target == archTarget
                                                                           && x.Type == edgeType
                                                                           && x.IsInArchitecture() && !IsSpecified(x));
            AssertOrThrow(alreadyPropagated == null, () => new AlreadyPropagatedException(alreadyPropagated, originatingEdge));

            const int counter = 1;
            Edge propagatedArchitectureDep = AddEdge(archSource, archTarget, edgeType);
            // propagatedArchitectureDep is a propagated dependency in the architecture graph
            SetCounter(propagatedArchitectureDep, counter);
            propagationTable[propagatedArchitectureDep.ID] = originatingEdge;

            // propagatedArchitectureDep is a dependency propagated from the implementation onto the architecture;
            // it was just created and, hence, has no state yet (which means it is State.undefined);
            // because it has just come into existence, we need to let our observers know about it
            Notify(new PropagatedEdgeEvent(propagatedArchitectureDep, ChangeType.Addition));

            if (Lift(archSource, archTarget, edgeType, counter, out allowingEdgeOut))
            {
                // found a matching specified architecture dependency allowing propagatedArchitectureDep
                Transition(propagatedArchitectureDep, State.Undefined, State.Allowed);
            }
            else if (archSource == archTarget)
            {
                // by default, every entity may use itself
                Transition(propagatedArchitectureDep, State.Undefined, State.ImplicitlyAllowed);
                // Note: there is no specified architecture dependency that allows this implementation
                // dependency. Self dependencies are implicitly allowed.
                allowingEdgeOut = null;
            }
            else if (AllowDependenciesToParents
                     && IsDescendantOf(propagatedArchitectureDep.Source, propagatedArchitectureDep.Target))
            {
                Transition(propagatedArchitectureDep, State.Undefined, State.ImplicitlyAllowed);
                // Note: there is no specified architecture dependency that allows this implementation
                // dependency. Dependencies from descendants to ancestors are implicitly allowed if
                // AllowDependenciesToParents is true.
                allowingEdgeOut = null;
            }
            else
            {
                Transition(propagatedArchitectureDep, State.Undefined, State.Divergent);
                allowingEdgeOut = null;
            }

            return propagatedArchitectureDep;
        }

        /// <summary>
        /// Returns true if a matching architecture dependency is found, also
        /// sets <paramref name="allowingEdgeOut"/> to that edge in that case.
        /// If no such matching edge is found, false is returned and <paramref name="allowingEdgeOut"/> is null.
        /// Precondition: <paramref name="from"/> and <paramref name="to"/> are in the architecture graph.
        /// Postcondition: <paramref name="allowingEdgeOut"/> is a specified dependency in architecture graph or null.
        /// </summary>
        /// <param name="from">source of the edge</param>
        /// <param name="to">target of the edge</param>
        /// <param name="edgeType">type of the edge</param>
        /// <param name="counter">the multiplicity of the edge, i.e. the number of other
        /// edges covered by it</param>
        /// <param name="allowingEdgeOut">the specified architecture dependency allowing the implementation
        /// dependency if there is any; otherwise null</param>
        /// <returns>True if a matching architecture dependency is found</returns>
        private bool Lift(Node from, Node to, string edgeType, int counter, out Edge allowingEdgeOut)
        {
            AssertOrThrow(from.IsInArchitecture(), () => new NotInSubgraphException(Architecture, from));
            AssertOrThrow(to.IsInArchitecture(), () => new NotInSubgraphException(Architecture, to));
            IList<Node> parents = to.Ascendants();
            Node notInArch = parents.FirstOrDefault(x => !x.IsInArchitecture() && !x.HasToggle(RootToggle));
            AssertOrThrow(notInArch == null, () => new NotInSubgraphException(Architecture, notInArch));
            Node cursor = from;
            AssertOrThrow(cursor.IsInArchitecture(), () => new NotInSubgraphException(Architecture, cursor));
            while (cursor != null)
            {
                IEnumerable<Edge> outs = cursor.Outgoings.Where(x => x.IsInArchitecture());
                foreach (Edge edge in outs)
                {
                    AssertOrThrow(edge.IsInArchitecture(), () => new NotInSubgraphException(Architecture, edge));
                    // Assert: edge is in architecture; edgeType is the type of edge
                    // being propagated and lifted; it may be more concrete than the
                    // type of the specified architecture dependency.
                    if (IsSpecified(edge)
                        // && edge.HasSupertypeOf(edgeType) FIXME: We consider that edges match independent of their types
                        && parents.Contains(edge.Target))
                    {
                        // matching architecture dependency found
                        ChangeSpecifiedDependency(edge, counter);
                        allowingEdgeOut = edge;
                        return true;
                    }
                }

                cursor = cursor.Parent;
            }

            // no matching architecture dependency found
            allowingEdgeOut = null;
            return false;
        }

        #endregion

        #region Helper methods for debugging

        /// <summary>
        /// Returns the Source.File attribute of the given <paramref name="graphElement"/> if it exists, otherwise
        /// the empty string.
        /// </summary>
        /// <param name="graphElement">attributable element</param>
        /// <returns>Source.File attribute or empty string</returns>
        private static string GetFilename(GraphElement graphElement)
        {
            return graphElement.Filename() ?? string.Empty;
        }

        /// <summary>
        /// Returns the Source.Line attribute as a string if it exists; otherwise the empty string.
        /// </summary>
        /// <param name="graphElement">attributable element</param>
        /// <returns>Source.Line attribute or empty string</returns>
        private static string GetSourceLine(GraphElement graphElement)
        {
            int? result = graphElement.SourceLine();
            return result.HasValue ? result.ToString() : string.Empty;
        }

        /// <summary>
        /// Returns a human-readable identifier for the given node.
        /// Note: this identifier is not necessarily unique.
        /// </summary>
        /// <param name="node">node whose identifier is required</param>
        /// <param name="beVerbose">whether to provide verbose output</param>
        /// <returns>an identifier for the given node</returns>
        private static string NodeName(Node node, bool beVerbose = false)
        {
            string name = node.SourceName;
            return beVerbose ? $"{name} ({node.ID}) {node.GetType().Name}" : name;
        }

        /// <summary>
        /// Returns a human-readable identifier for the given node further qualified with
        /// its source location if available.
        /// Note: this identifier is not necessarily unique.
        /// </summary>
        /// <param name="node">node whose identifier is required</param>
        /// <param name="beVerbose">whether to provide verbose output</param>
        /// <returns>an identifier for the given node</returns>
        // returns node name
        private static string QualifiedNodeName(Node node, bool beVerbose = false)
        {
            string filename = GetFilename(node);
            string loc = GetSourceLine(node);
            return $"{NodeName(node, beVerbose)}@{filename}:{loc}";
        }

        /// <summary>
        /// Returns the edge as a clause "type(from, to)".
        /// </summary>
        /// <param name="edge">edge whose clause is expected</param>
        /// <returns>the edge as a clause</returns>
        private static string AsClause(Edge edge)
        {
            return $"{edge.GetType().Name}({NodeName(edge.Source)}, {NodeName(edge.Target)})";
        }

        /// <summary>
        /// Returns the edge as a qualified clause "type(from@loc, to@loc)@loc",
        /// </summary>
        /// <param name="edge">edge whose qualified clause is expected</param>
        /// <returns>qualified clause</returns>
        private static string AsQualifiedClause(Edge edge)
        {
            return $"{edge.GetType().Name}({QualifiedNodeName(edge.Source)}, "
                   + $"{QualifiedNodeName(edge.Target)})@{GetFilename(edge)}:{GetSourceLine(edge)}";
        }

        /// <summary>
        /// Dumps given <paramref name="nodeSet"/> after given message was dumped.
        /// </summary>
        /// <param name="nodeSet">list of nodes whose qualified name is to be dumped</param>
        /// <param name="message">message to be emitted before the nodes</param>
        private static void DumpNodeSet(List<Node> nodeSet, string message)
        {
            Debug.Log(message + "\n");
            foreach (Node node in nodeSet)
            {
                Debug.Log(QualifiedNodeName(node, true) + "\n");
            }
        }

        /// <summary>
        /// Dumps given <paramref name="edgeSet"/> after given message was dumped.
        /// </summary>
        /// <param name="edgeSet">list of edges whose qualified name is to be dumped</param>
        private static void DumpEdgeSet(List<Edge> edgeSet)
        {
            foreach (Edge edge in edgeSet)
            {
                Debug.Log(AsQualifiedClause(edge) + "\n");
            }
        }

        /// <summary>
        /// Dumps the nodes and edges of the <paramref name="graph"/> to Unity's debug console.
        /// Intended for debugging.
        /// </summary>
        public static void DumpGraph(Graph graph)
        {
            Debug.Log($"Graph {graph.Name} with {graph.NodeCount} nodes and {graph.EdgeCount} edges: \n");
            Debug.Log("NODES\n");
            foreach (Node node in graph.Nodes())
            {
                Debug.Log(node.ToString());
            }

            Debug.Log("EDGES\n");
            foreach (Edge edge in graph.Edges())
            {
                // edge counter state
                Debug.Log($"{AsClause(edge)} {GetArchCounter(edge)} {edge.State()}\n");
            }
        }

        public void DumpMapping()
        {
            Debug.Log("EXPLICITLY MAPPED NODES\n");
            DumpTable(explicitMapsToTable);
            Debug.Log("IMPLICITLY MAPPED NODES\n");
            DumpTable(implicitMapsToTable);
        }

        public static void DumpTable(Dictionary<string, Node> table)
        {
            foreach (KeyValuePair<string, Node> entry in table)
            {
                Debug.Log($"  {entry.Key} -> {entry.Value.ID}\n");
            }
        }

        #endregion
    }
}