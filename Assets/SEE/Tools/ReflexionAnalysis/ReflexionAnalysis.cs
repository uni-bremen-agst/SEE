/*
Copyright (C) Axivion GmbH, 2011-2020

@author Rainer Koschke, Falko Galperin
Initially written in C++ on Jul 24, 2011.
Rewritten in C# on Jan 14, 2020.
Refactored to work on a single graph on Feb 17, 2022.

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

Open issues: implementation of incremental analysis is missing.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using UnityEngine;
using UnityEngine.Assertions;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;

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
        Specified = 7
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
    public class Reflexion : Observable
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
        public Reflexion(Graph implementation,
                         Graph architecture,
                         Graph mapping,
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
        /// Returns the state of an architecture dependency.
        /// Precondition: edge must be in the architecture graph.
        /// </summary>
        /// <param name="edge">a dependency in the architecture</param>
        /// <returns>the state of <paramref name="edge"/> in the architecture</returns>
        public static State GetState(Edge edge)
        {
            if (edge.TryGetInt(StateAttribute, out int value))
            {
                return (State)value;
            }
            else
            {
                return State.Undefined;
            }
        }

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
            Assert.IsTrue(edge.IsInArchitecture());
            State state = GetState(edge);
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
            Assert.IsTrue(edge.IsInArchitecture());
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
            Assert.IsTrue(edge.IsInArchitecture());
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
        public static int GetCounter(Edge edge) => edge.TryGetInt(CounterAttribute, out int value) ? value : 0;

        /// <summary>
        /// Increases counter of <paramref name="edge"/> by <paramref name="value"/> (may be negative).
        /// If its counter drops to zero, the edge is removed.
        /// Notifies if edge's state changes.
        /// Precondition: <paramref name="edge"/> is a dependency in architecture graph that
        /// was propagated from the implementation graph (i.e., !IsSpecified(<paramref name="edge"/>)).
        /// </summary>
        /// <param name="edge">propagated dependency in architecture graph</param>
        /// <param name="value">value to be added</param>
        private void AddToImplRef(Edge edge, int value)
        {
            Assert.IsTrue(edge.IsInArchitecture() && !IsSpecified(edge));
            int oldValue = GetCounter(edge);
            int newValue = oldValue + value;
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
                Notify(new PropagatedEdgeRemoved(edge));
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
        private int GetImplCounter(Edge edge = null)
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

        #region Node


        /// <summary>
        /// Adds given node to the mapping graph.
        /// Precondition: node must not be contained in the mapping graph.
        /// Postcondition: node is contained in the mapping graph and the architecture
        //   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the mapping graph</param>
        //public void AddToMapping(Node node)
        //{
        //    throw new NotImplementedException(); // FIXME
        //}

        /// <summary>
        /// Removes given node from mapping graph.
        /// Precondition: node must be contained in the mapping graph.
        /// Postcondition: node is no longer contained in the mapping graph and the architecture
        ///   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">node to be removed from the mapping</param>
        //public void DeleteFromMapping(Node node)
        //{
        //    throw new NotImplementedException(); // FIXME
        //}

        /// <summary>
        /// Adds given <paramref name="node"/> to architecture graph.
        ///
        /// Precondition: <paramref name="node"/> must not be contained in the architecture graph.
        /// Postcondition: <paramref name="node"/> is contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the architecture graph</param>
        public void AddToArchitecture(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes given <paramref name="node"/> from architecture graph.
        /// Precondition: <paramref name="node"/> must be contained in the architecture graph.
        /// Postcondition: <paramref name="node"/> is no longer contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from the architecture graph</param>
        public void DeleteFromArchitecture(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Adds given <paramref name="node"/> to implementation graph.
        /// Precondition: <paramref name="node"/> must not be contained in the implementation graph.
        /// Postcondition: <paramref name="node"/> is contained in the implementation graph; all observers are
        /// informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the implementation graph</param>
        public void AddToImplementation(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes the given <paramref name="node"/> from the implementation graph (and all its incoming and
        /// outgoing edges).
        /// Precondition: <paramref name="node"/> must be contained in the implementation graph.
        /// Postcondition: <paramref name="node"/> is no longer contained in the implementation graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from the implementation graph</param>
        public void DeleteFromImplementation(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        #endregion

        #region Edge

        /// <summary>
        /// Adds a new Maps_To edge from <paramref name="from"/> to <paramref name="to"/> to the mapping graph and
        /// re-runs the reflexion analysis incrementally.
        /// Preconditions: <paramref name="from"/> is contained in the implementation graph and not yet mapped
        /// explicitly and <paramref name="to"/> is contained in the architecture graph.
        /// Postcondition: Created edge is contained in the mapping graph and the reflexion
        ///   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">the source of the Maps_To edge to be added to the mapping graph</param>
        /// <param name="to">the target of the Maps_To edge to be added to the mapping graph</param>
        public void AddToMapping(Node from, Node to)
        {
            Assert.IsTrue(from.IsInImplementation());
            Assert.IsTrue(to.IsInArchitecture());
            if (IsExplicitlyMapped(from))
            {
                throw new ArgumentException($"Node {from.ID} is already mapped explicitly.");
            }
            else
            {
                // all nodes that should be mapped onto 'to', too, as a consequence of
                // mapping 'from'
                List<Node> subtree = MappedSubtree(from);
                // was 'from' mapped implicitly at all?
                if (implicitMapsToTable.TryGetValue(from.ID, out Node oldTarget))
                {
                    // from was actually mapped implicitly onto oldTarget
                    Unmap(subtree, oldTarget);
                }
                AddToMappingGraph(from, to);
                // adjust explicit mapping
                explicitMapsToTable[from.ID] = to;
                // adjust implicit mapping
                ChangeMap(subtree, to);
                Map(subtree, to);
            }
        }

        /// <summary>
        /// Adds an edge of type <see cref="MapsToType"/> between <paramref name="from"/> and <paramref name="to"/>.
        ///
        /// Precondition: <paramref name="from"/> is contained in the implementation graph and <paramref name="to"/> is
        /// contained in the architecture graph.
        /// Postcondition: there is a Maps_To edge from <paramref name="from"/> to <paramref name="to"/> in the mapping.
        /// graph.
        /// </summary>
        /// <param name="from">source of the Maps_To edge</param>
        /// <param name="to">target of the Maps_To edge</param>
        private void AddToMappingGraph(Node from, Node to)
        {
            Assert.IsTrue(from.IsInImplementation());
            Assert.IsTrue(to.IsInArchitecture());
            // add Maps_To edge to Mapping
            Edge mapsTo = new Edge(from, to, MapsToType);
            FullGraph.AddEdge(mapsTo);
            Notify(new MapsToEdgeAdded(mapsTo));
        }

        /// <summary>
        /// All nodes in given <paramref name="subtree"/> are implicitly mapped onto given <paramref name="target"/>
        /// architecture node if <paramref name="target"/> != null. If <paramref name="target"/> == null,
        /// all nodes in given subtree are removed from implicitMapsToTable.
        ///
        /// Precondition: <paramref name="target"/> is either null, or is in the architecture graph and all nodes in
        /// <paramref name="subtree"/> are in the implementation graph.
        /// </summary>
        /// <param name="subtree">list of nodes to be mapped onto <paramref name="target"/></param>
        /// <param name="target">architecture node onto which to map all nodes in <paramref name="subtree"/></param>
        private void ChangeMap(List<Node> subtree, Node target)
        {
            Assert.IsTrue(target == null || target.IsInArchitecture());
            if (target == null)
            {
                foreach (Node node in subtree)
                {
                    Assert.IsTrue(node.IsInImplementation());
                    implicitMapsToTable.Remove(node.ID);
                }
            }
            else
            {
                foreach (Node node in subtree)
                {
                    Assert.IsTrue(node.IsInImplementation());
                    implicitMapsToTable[node.ID] = target;
                }
            }
        }

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
        private void HandleMappedSubtree(List<Node> subtree, Node archNode, HandleMappingChange handler)
        {
            Assert.IsTrue(archNode.IsInArchitecture());
            // An inner dependency may occur twice in the iteration below, once in the set
            // of outgoing edges and once in the set of incoming edges of any nodes in the subtree.
            // We may call the handler only once for these; that is why we need to keep a log of
            // inner edges already handled.
            ISet<Edge> innerEdgesAlreadyHandled = new HashSet<Edge>();
            foreach (Node implNode in subtree)
            {
                Assert.IsTrue(implNode.IsInImplementation());
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
                    Assert.IsTrue(incoming.IsInImplementation());
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
        /// Reverts the effect of the mapping of every node in the given <paramref name="subtree"/> onto the
        /// reflexion data. That is, every non-dangling incoming and outgoing dependency of every
        /// node in the subtree will be "unpropagated" and "unlifted".
        /// Precondition: given <paramref name="oldTarget"/> is non-null and contained in the architecture graph
        /// and all nodes in <paramref name="subtree"/> are in the implementation graph.
        /// All nodes in <paramref name="subtree"/> were originally mapped onto <paramref name="oldTarget"/>.
        /// </summary>
        /// <param name="subtree">implementation nodes whose mapping is to be reverted</param>
        /// <param name="oldTarget">architecture node onto which the nodes in <paramref name="subtree"/>
        /// were mapped originally</param>
        private void Unmap(List<Node> subtree, Node oldTarget)
        {
            Assert.IsTrue(oldTarget.IsInArchitecture());
            HandleMappedSubtree(subtree, oldTarget, DecreaseAndLift);
        }

        /// <summary>
        /// Maps every node in the given subtree onto <paramref name="newTarget"/>. That is, every non-dangling incoming
        /// and outgoing dependency of every node in the <paramref name="subtree"/> will be propagated and lifted.
        /// Precondition: given <paramref name="newTarget"/> is non-null and contained in the architecture graph
        /// and all nodes in <paramref name="subtree"/> are in the implementation graph.
        /// All nodes in <paramref name="subtree"/> are to be mapped onto <paramref name="newTarget"/>.
        /// </summary>
        /// <param name="subtree">implementation nodes whose mapping is to be put into effect</param>
        /// <param name="newTarget">architecture node onto which the nodes in subtree are to be mapped</param>
        private void Map(List<Node> subtree, Node newTarget)
        {
            Assert.IsTrue(newTarget.IsInArchitecture());
            HandleMappedSubtree(subtree, newTarget, IncreaseAndLift);
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
                Assert.IsTrue(implementationDependency.IsInImplementation());
                Assert.IsTrue(from.IsInArchitecture());
                Assert.IsTrue(to.IsInArchitecture());
                Edge propagatedEdge = GetPropagatedDependency(from, to, implementationDependency.Type);
                // It can happen that one of the edges end is not mapped, hence, no edge was propagated.
                if (propagatedEdge != null)
                {
                    int counter = -GetImplCounter(implementationDependency);
                    if (Lift(propagatedEdge.Source, propagatedEdge.Target, propagatedEdge.Type, counter, out Edge _))
                    {
                        // matching specified architecture dependency found; no state change
                    }
                    AddToImplRef(propagatedEdge, counter);
                }
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
        /// dependency in the architecture graph is to be decreased and lifted</param>
        /// <param name="from">architecture node = MapsTo(implementationDependency.Source)</param>
        /// <param name="to">architecture node = MapsTo(implementationDependency.Target)</param>
        /// <remarks>
        /// <paramref name="from"/> and <paramref name="to"/> are actually ignored (intentionally).
        /// </remarks>
        private void IncreaseAndLift(Edge implementationDependency, Node from, Node to)
        {
            Assert.IsTrue(implementationDependency.IsInImplementation());
            Assert.IsTrue(from.IsInArchitecture());
            Assert.IsTrue(to.IsInArchitecture());
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
            Assert.IsTrue(node.IsInImplementation());
            return node.Children().Where(x => !IsExplicitlyMapped(x))
                       .SelectMany(MappedSubtree).Prepend(node).ToList();
        }

        /// <summary>
        /// Removes the given Maps_To <paramref name="edge"/> from the mapping graph.
        /// Precondition: <paramref name="edge"/> must be contained in the mapping graph and must have type Maps_To.
        /// Postcondition: <paramref name="edge"/> is no longer contained in the mapping graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the Maps_To edge to be removed from the mapping graph</param>
        public void DeleteFromMapping(Edge edge)
        {
            Assert.IsTrue(edge.IsInMapping());
            // The mapping target in the architecture graph.
            if (!edge.Target.IsInArchitecture())
            {
                throw new ArgumentException($"Mapping target node {edge.Target.ID} is not in the architecture.");
            }
            // The mapping source in the implementation graph.
            if (!edge.Source.IsInImplementation())
            {
                throw new ArgumentException($"Mapping source node {edge.Source.ID} is not in the implementation.");
            }
            DeleteMapsTo(edge);
        }

        /// <summary>
        /// Reverts the effect of the mapping indicated by <paramref name="mapsTo"/>.
        /// Precondition: <paramref name="mapsTo"/>.Source is in the implementation graph and is a mapper,
        /// <paramref name="mapsTo"/>.Target is in the architecture graph, and mapsTo is in the mapping graph.
        /// Postconditions:
        /// (1) <paramref name="mapsTo"/> is removed from the graph.
        /// (2) <paramref name="mapsTo"/>.Source is removed from <see cref="explicitMapsToTable"/>
        /// (3) all nodes in the mapped subtree rooted by <paramref name="mapsTo"/>.Source are first unmapped
        /// and then -- if <paramref name="mapsTo"/>.Source has a mapped parent -- mapped onto the same target
        /// as the mapped parent of <paramref name="mapsTo"/>.Source; <see cref="implicitMapsToTable"/>
        /// is adjusted accordingly
        /// (4) all other reflexion data is adjusted and all observers are notified
        /// </summary>
        /// <param name="mapsTo">the mapping which shall be reverted</param>
        private void DeleteMapsTo(Edge mapsTo)
        {
            if (!IsExplicitlyMapped(mapsTo.Source))
            {
                throw new ArgumentException($"Implementation node {mapsTo.Source} is not mapped explicitly.");
            }
            else if (!explicitMapsToTable.Remove(mapsTo.Source.ID))
            {
                throw new ArgumentException($"Implementation node {mapsTo.Source} is not mapped explicitly.");
            }

            // All nodes in the subtree rooted by mapsTo.Source and mapped onto the same target as mapsTo.Source.
            List<Node> subtree = MappedSubtree(mapsTo.Source);
            Unmap(subtree, mapsTo.Target);
            Node implSourceParent = mapsTo.Source.Parent;
            if (implSourceParent == null)
            {
                // If mapsTo.Source has no parent, all nodes in subtree are not mapped at all any longer.
                ChangeMap(subtree, null);
            }
            else
            {
                // If mapsTo.Source has a parent, all nodes in subtree should be mapped onto
                // the architecture node onto which the parent is mapped -- if the parent
                // is mapped at all (implicitly or explicitly).
                if (implicitMapsToTable.TryGetValue(implSourceParent.ID, out Node newTarget))
                {
                    // newTarget is the architecture node onto which the parent of mapsTo.Source is mapped.
                    ChangeMap(subtree, newTarget);
                }
                if (newTarget != null)
                {
                    Map(subtree, newTarget);
                }
            }
            // First notify before we delete the mapsTo edge for good.
            Notify(new MapsToEdgeRemoved(mapsTo));
            FullGraph.RemoveEdge(mapsTo);
        }

        /// <summary>
        /// Removes the Maps_To edge between <paramref name="from"/> and <paramref name="to"/>
        /// from the mapping graph.
        /// Precondition: a Maps_To edge between <paramref name="from"/> and <paramref name="to"/>
        /// must be contained in the mapping graph, <paramref name="from"/> is contained in implementation graph
        /// and <paramref name="to"/> is contained in the architecture graph.
        /// Postcondition: the edge is no longer contained in the mapping graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">the source (contained in implementation graph) of the Maps_To edge
        /// to be removed from the mapping graph </param>
        /// <param name="to">the target (contained in the architecture graph) of the Maps_To edge
        /// to be removed from the mapping graph </param>
        public void DeleteFromMapping(Node from, Node to)
        {
            Assert.IsTrue(from.IsInImplementation());
            Assert.IsTrue(to.IsInArchitecture());
            if (!FullGraph.ContainsNode(from))
            {
                throw new ArgumentException($"Node {from} is not in the graph.");
            }
            if (!FullGraph.ContainsNode(to))
            {
                throw new ArgumentException($"Node {to} is not in the graph.");
            }

            // The mapsTo edge in between from mapFrom to mapTo. There should be exactly one such edge.
            Edge mapsToEdge = from.FromTo(to, MapsToType).SingleOrDefault();
            if (mapsToEdge == null)
            {
                throw new InvalidOperationException($"There must be exactly one mapping in between {from} and {to}.");
            }

            // Deletes the unique Maps_To edge.
            DeleteMapsTo(mapsToEdge);
        }

        /// <summary>
        /// Adds the given dependency <paramref name="edge"/> to the architecture graph. This edge will
        /// be considered as a specified dependency.
        /// Precondition: <paramref name="edge"/> must not be contained in the architecture graph and must
        /// represent a dependency.
        /// Postcondition: <paramref name="edge"/> is contained in the architecture graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the dependency edge to be added to the architecture graph</param>
        public void AddToArchitecture(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes the given specified dependency <paramref name="edge"/> from the architecture.
        /// Precondition: <paramref name="edge"/> must be contained in the architecture graph
        ///   and must represent a specified dependency.
        /// Postcondition: <paramref name="edge"/> is no longer contained in the architecture graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the specified dependency edge to be removed from the architecture graph</param>
        public void DeleteFromArchitecture(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Adds given dependency <paramref name="edge"/> to the implementation graph.
        /// Precondition: <paramref name="edge"/> must not be contained in the implementation graph
        /// Postcondition: <paramref name="edge"/> is contained in the implementation graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the dependency edge to be added to the implementation graph</param>
        public void AddToDependencies(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes the given dependency <paramref name="edge"/> from the implementation graph.
        /// Precondition: <paramref name="edge"/> must be contained in the implementation graph
        ///   and edge must be a dependency.
        /// Postcondition: <paramref name="edge"/> is no longer contained in the implementation graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the dependency edge to be removed from the implementation graph</param>
        private void DeleteFromDependencies(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Adds given <paramref name="child"/> as a direct descendant of given <paramref name="parent"/>
        /// in the node hierarchy of the implementation graph.
        /// Precondition: <paramref name="child"/> and <paramref name="parent"/> must be contained in the
        /// hierarchy graph; <paramref name="child"/> has no current parent.
        /// Postcondition: <paramref name="parent"/> is a parent of <paramref name="child"/> in the
        /// implementation graph and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <param name="parent">parent node</param>
        public void AddChildInImplementation(Node child, Node parent)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes given <paramref name="child"/> from its parent in the node hierarchy of
        /// the implementation graph.
        /// Precondition: <paramref name="child"/> and parent must be contained in the hierarchy graph;
        ///    child has a parent.
        /// Postcondition: <paramref name="child"/> has no longer a parent in the implementation graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        public void UnparentInImplementation(Node child)
        {
            throw new NotImplementedException(); // FIXME
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
                summary[(int)GetState(edge)] += GetCounter(edge);
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
                Assert.IsTrue(source.IsInImplementation());
                Assert.IsTrue(target.IsInArchitecture());
                explicitMapsToTable[source.ID] = target;
                //explicitMapsToTable[InImplementation[source.ID]] = InArchitecture[target.ID];
            }

            implicitMapsToTable = new Dictionary<string, Node>();

            foreach (Edge mapsTo in FullGraph.Edges().Where(x => x.IsInMapping()))
            {
                Node source = mapsTo.Source;
                Node target = mapsTo.Target;
                Assert.IsTrue(source.IsInImplementation());
                Assert.IsTrue(target.IsInArchitecture());
                AddSubtreeToImplicitMap(source, target);
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
            Assert.IsTrue(root.IsInImplementation());
            Assert.IsTrue(target.IsInArchitecture());
            List<Node> children = root.Children();
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
        /// Adds value to counter of <paramref name="edge"/> and transforms its state.
        /// Notifies if edge state changes.
        /// Precondition: <paramref name="edge"/> is in architecture graph.
        /// </summary>
        /// <param name="edge">architecture dependency to be changed</param>
        /// <param name="value">the value to be added to the edge's counter</param>
        private void ChangeArchitectureDependency(Edge edge, int value)
        {
            Assert.IsTrue(edge.IsInArchitecture());
            int oldValue = GetCounter(edge);
            int newValue = oldValue + value;
            State state = GetState(edge);

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
        /// Resets architecture markings.
        /// </summary>
        private void Reset()
        {
            ResetArchitecture();
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
                State state = GetState(edge);
                switch (state)
                {
                    case State.Undefined:
                    case State.Specified:
                        SetCounter(edge, 0); // Note: architecture edges have a counter
                        SetState(edge, State.Specified); // initial state must be State.specified
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
                        Notify(new PropagatedEdgeRemoved(edge));
                        toBeRemoved.Add(edge);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown state '{state}' encountered!");
                }
            }

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
                Assert.IsTrue(sourceNode.IsInImplementation());
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
                State state = GetState(edge);
                if (IsSpecified(edge) && state != State.Convergent)
                {
                    Transition(edge, state,
                               edge.HasToggle("Architecture.Is_Optional") ? State.AllowedAbsent : State.Absent);
                }
            }
        }

        /// <summary>
        /// Returns propagated dependency in architecture graph matching the type of
        /// of the implementation dependency Edge exactly if one exists;
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
            Assert.IsTrue(source.IsInArchitecture());
            Assert.IsTrue(target.IsInArchitecture());
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
            Assert.IsTrue(implementationDependency.IsInImplementation());
            Node implSource = implementationDependency.Source;
            Node implTarget = implementationDependency.Target;
            Assert.IsTrue(implSource.IsInImplementation());
            Assert.IsTrue(implTarget.IsInImplementation());
            string implType = implementationDependency.Type;

            Node archSource = MapsTo(implSource);
            Node archTarget = MapsTo(implTarget);
            Assert.IsTrue(archSource == null || archSource.IsInArchitecture());
            Assert.IsTrue(archTarget == null || archTarget.IsInArchitecture());

            if (archSource == null || archTarget == null)
            {
                // source or target are not mapped; so we cannot do anything
                return;
            }
            Edge architectureDependency = GetPropagatedDependency(archSource, archTarget, implType);
            Assert.IsTrue(architectureDependency == null || architectureDependency.IsInArchitecture());
            Edge allowingEdge = null;
            if (architectureDependency == null)
            {
                // a propagated dependency has not existed yet; we need to create one
                architectureDependency = NewImplDepInArchitecture(archSource, archTarget, implType, out allowingEdge);
                Assert.IsTrue(architectureDependency.IsInArchitecture());
            }
            else
            {
                // a propagated dependency exists already
                int implCounter = GetImplCounter(implementationDependency);
                Assert.IsTrue(architectureDependency.Source.IsInArchitecture());
                Assert.IsTrue(architectureDependency.Target.IsInArchitecture());
                // Assert: architectureDependency.Source and architectureDependency.Target are in architecture.
                Lift(architectureDependency.Source, architectureDependency.Target,
                     implType, implCounter, out allowingEdge);
                AddToImplRef(architectureDependency, implCounter);
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
            Assert.IsTrue(node.IsInImplementation());
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
            Assert.IsTrue(node.IsInImplementation());
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
            Assert.IsTrue(edge.IsInImplementation());
            Node mappedSource = MapsTo(edge.Source);
            Node mappedTarget = MapsTo(edge.Target);

            if (mappedSource != null && mappedTarget != null)
            {
                Assert.IsTrue(mappedSource.IsInArchitecture());
                Assert.IsTrue(mappedTarget.IsInArchitecture());
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
        private Edge Add(Node from, Node to, string itsType)
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
                Assert.IsTrue(to.IsInImplementation());
                result.SetInImplementation();
            } 
            else if (from.IsInArchitecture())
            {
                Assert.IsTrue(to.IsInArchitecture());
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
        /// Postcondition: the newly created and returned dependency is contained in
        /// the architecture graph and marked as propagated.
        /// </summary>
        /// <param name="archSource">architecture node that is the source of the propagated edge</param>
        /// <param name="archTarget">architecture node that is the target of the propagated edge</param>
        /// <param name="edgeType">type of the propagated implementation edge</param>
        /// <param name="allowingEdgeOut">the specified architecture dependency allowing the implementation
        /// dependency if there is one; otherwise null; allowingEdgeOut is also null if the implementation
        /// dependency form a self-loop (archSource == archTarget); self-dependencies are implicitly
        /// allowed, but do not necessarily have a specified architecture dependency</param>
        /// <returns>a new propagated dependency in the architecture graph</returns>
        private Edge NewImplDepInArchitecture(Node archSource, Node archTarget, string edgeType, out Edge allowingEdgeOut)
        {
            const int counter = 1;
            Edge propagatedArchitectureDep = Add(archSource, archTarget, edgeType);
            // propagatedArchitectureDep is a propagated dependency in the architecture graph
            SetCounter(propagatedArchitectureDep, counter);

            // TODO: Mark propagatedArchitecturDep as propagated. Or maybe that is not necessary at all
            // because we have the edge state from which we can derive whether an edge is specified
            // or propagated.

            // propagatedArchitecturDep is a dependency propagated from the implementation onto the architecture;
            // it was just created and, hence, has no state yet (which means it is State.undefined);
            // because it has just come into existence, we need to let our observers know about it
            Notify(new PropagatedEdgeAdded(propagatedArchitectureDep));

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
        /// If no such matching/ edge is found, false is returned and <paramref name="allowingEdgeOut"/> is null.
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
            Assert.IsTrue(from.IsInArchitecture());
            Assert.IsTrue(to.IsInArchitecture());
            List<Node> parents = to.Ascendants();
            Assert.IsTrue(parents.All(x => x.IsInArchitecture()));
            Node cursor = from;
            Assert.IsTrue(cursor.IsInArchitecture());
            while (cursor != null)
            {
                IEnumerable<Edge> outs = cursor.Outgoings.Where(x => x.IsInArchitecture());
                foreach (Edge edge in outs)
                {
                    Assert.IsTrue(edge.IsInArchitecture());
                    // Assert: edge is in architecture; edgeType is the type of edge
                    // being propagated and lifted; it may be more concrete than the
                    // type of the specified architecture dependency.
                    if (IsSpecified(edge)
                        // && edge.HasSupertypeOf(edgeType) FIXME: We consider that edges match independent of their types
                        && parents.Contains(edge.Target))
                    {   // matching architecture dependency found
                        ChangeArchitectureDependency(edge, counter);
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
                Debug.Log($"{AsClause(edge)} {GetCounter(edge)} {GetState(edge)}\n");
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
