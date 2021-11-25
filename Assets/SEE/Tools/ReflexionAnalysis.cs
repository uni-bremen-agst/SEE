/*
Copyright (C) Axivion GmbH, 2011-2020

@author Rainer Koschke
Initially written in C++ on Jul 24, 2011.
Rewritten in C# on Jan 14, 2020.

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

Open issues: implementation of incremental analysis is missing.

*/

using System;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
using UnityEngine;

namespace SEE.Tools
{
    /// <summary>
    /// Super class for all exceptions thrown by the architecture analysis.
    /// </summary>
    public class DG_Exception : Exception { }

    /// <summary>
    /// Thrown if the hierarchy is not a tree structure.
    /// </summary>
    public class Hierarchy_Is_Not_A_Tree : DG_Exception { }

    public class Corrupt_State : DG_Exception { }

    /// <summary>
    /// State of a dependency in the architecture or implementation within the
    /// reflexion model.
    /// </summary>
    public enum State
    {
        undefined = 0,          // initial undefined state
        allowed = 1,            // allowed propagated dependency towards a convergence; only for implementation dependencies
        divergent = 2,          // disallowed propagated dependency (divergence); only for implementation dependencies
        absent = 3,             // specified architecture dependency without corresponding implementation dependency (absence); only for architecture dependencies
        convergent = 4,         // specified architecture dependency with corresponding implementation dependency (convergence); only for architecture dependencies
        implicitly_allowed = 5, // self-usage is always implicitly allowed; only for implementation dependencies
        allowed_absent = 6,     // absence, but Architecture.Is_Optional attribute set
        specified = 7           // tags an architecture edge that was created by the architect,
                                // i.e., is a specified edge; this is the initial state of a specified
                                // architecture dependency; only for architecture dependencies
    };

    /// <summary>
    /// A change event fired when the state of an edge changed.
    /// </summary>
    public class EdgeChange : ChangeEvent
    {
        /// <summary>
        /// The edge being changed.
        /// </summary>
        public Edge edge;
        /// <summary>
        /// The previous state of the edge before the change.
        /// </summary>
        public State oldState;
        /// <summary>
        /// The new state of the edge after the change.
        /// </summary>
        public State newState;

        /// <summary>
        /// Constructor for a change of an edge event.
        /// </summary>
        /// <param name="edge">edge being changed</param>
        /// <param name="old_state">the old state of the edge</param>
        /// <param name="new_state">the new state of the edge after the change</param>
        public EdgeChange(Edge edge, State oldState, State newState)
        {
            this.edge = edge;
            this.oldState = oldState;
            this.newState = newState;
        }

        public override string ToString()
        {
            return base.ToString() + ": " + edge.ToString() + " changed from " + oldState + " to " + newState;
        }
    }

    /// <summary>
    /// A change event fired when an implementation dependency was either propagated to
    /// the architecture or unpropagated from the architecture.
    /// </summary>
    public abstract class PropagatedEdge : ChangeEvent
    {
        /// <summary>
        /// The implementation dependency propagated from the implementation to the architecture.
        /// </summary>
        public Edge propagatedEdge;

        /// <summary>
        /// Constructor preserving the implementation dependency that is or was
        /// propagated to the architecture graph.
        /// </summary>
        /// <param name="propagatedEdge">the propagated edge</param>
        public PropagatedEdge(Edge propagatedEdge)
        {
            this.propagatedEdge = propagatedEdge;
        }
    }

    /// <summary>
    /// A change event fired when an implementation dependency was propagated to
    /// the architecture.
    ///
    /// Note: This event is fired only once for the very first time a corresponding
    /// new propagated edge was created in the architecture. If there is already such
    /// a propagated edge in the architecture, this existing edge is re-used and only
    /// its counter is updated.
    /// </summary>
    public class PropagatedEdgeAdded : PropagatedEdge
    {
        /// <summary>
        /// Constructor preserving the implementation dependency propagated from the
        /// implementation to the architecture.
        /// </summary>
        /// <param name="propagatedEdge">the propagated edge</param>
        public PropagatedEdgeAdded(Edge propagatedEdge) : base(propagatedEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": new propagated edge " + propagatedEdge.ToString();
        }
    }

    /// <summary>
    /// A change event fired when a propagated dependency edge was removed from the architecture.
    /// </summary>
    public class PropagatedEdgeRemoved : PropagatedEdge
    {
        /// <summary>
        /// Constructor passing on the edge to be removed.
        /// </summary>
        /// <param name="propagatedEdge">edge to be removed</param>
        public PropagatedEdgeRemoved(Edge propagatedEdge) : base(propagatedEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": unpropagated edge " + propagatedEdge.ToString();
        }
    }

    /// <summary>
    /// A change event fired when a Maps_To edge was added to the mapping or removed from it.
    /// </summary>
    public abstract class MapsToEdge : ChangeEvent
    {
        /// <summary>
        /// The Maps_To edge added to the mapping or removed from it.
        /// </summary>
        public Edge mapsToEdge;

        /// <summary>
        /// Constructor preserving the Maps_To edge added to the mapping or removed from it.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being added or removed</param>
        public MapsToEdge(Edge mapsToEdge)
        {
            this.mapsToEdge = mapsToEdge;
        }
    }

    /// <summary>
    /// A change event fired when a Maps_To edge was added to the mapping.
    /// </summary>
    public class MapsToEdgeAdded : MapsToEdge
    {
        /// <summary>
        /// Constructor preserving the Maps_To edge added to the mapping.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being added</param>
        public MapsToEdgeAdded(Edge mapsToEdge) : base(mapsToEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": new Maps_To edge " + mapsToEdge.ToString();
        }
    }

    /// <summary>
    /// A change event fired when a Maps_To edge was removed from the mapping.
    /// </summary>
    public class MapsToEdgeRemoved : MapsToEdge
    {
        /// <summary>
        /// Constructor preserving the Maps_To edge removed from the mapping.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being removed</param>
        public MapsToEdgeRemoved(Edge mapsToEdge) : base(mapsToEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": removed Maps_To edge " + mapsToEdge.ToString();
        }
    }

    /// <summary>
    /// Implements the reflexion analysis, which compares an implementation against an expected
    /// architecture based on a mapping between the two.
    /// </summary>
    public class Reflexion : Observable
    {
        /// <summary>
        /// Constructor for setting up and running the reflexion analysis.
        /// Note: This does not really run the reflexion analysis. Use
        /// method Run() to start the analysis.
        /// </summary>
        /// <param name="implementation">the implementation graph</param>
        /// <param name="architecture">the architecture model</param>
        /// <param name="mapping">the mapping of implementation nodes onto architecture nodes</param>
        /// <param name="allow_dependencies_to_parents">whether descendants may access their ancestors</param>
        public Reflexion(Graph implementation,
                         Graph architecture,
                         Graph mapping,
                         bool allow_dependencies_to_parents = true)
        {
            _implementation = implementation;
            _architecture = architecture;
            _mapping = mapping;
            _allow_dependencies_to_parents = allow_dependencies_to_parents;
        }

        /// <summary>
        /// Runs the reflexion analysis. If an observer has registered before,
        /// the observer will receive the results via the callback Update(ChangeEvent).
        /// </summary>
        public void Run()
        {
            RegisterNodes();
            Add_Transitive_Mapping();
            From_Scratch();
            //DumpResults();
        }

        // --------------------------------------------------------------------
        // State edge attribute
        // --------------------------------------------------------------------

        /// <summary>
        /// Name of the edge attribute for the state of a dependency.
        /// </summary>
        private const string state_attribute = "Reflexion.State";

        /// <summary>
        /// Returns the state of an architecture dependency.
        /// Precondition: edge must be in the architecture graph.
        /// </summary>
        /// <param name="edge">a dependency in the architecture</param>
        /// <returns>the state of 'edge' in the architecture</returns>
        public static State Get_State(Edge edge)
        {
            if (edge.TryGetInt(state_attribute, out int value))
            {
                return (State)value;
            }
            else
            {
                return State.undefined;
            }
        }

        /// <summary>
        /// Sets the initial state of edge to state.
        /// Precondition: edge has no state attribute yet.
        /// </summary>
        /// <param name="edge">edge whose initial state is to be set</param>
        /// <param name="initial_state">the initial state to be set</param>
        private static void Set_Initial(Edge edge, State initial_state)
        {
            edge.SetInt(state_attribute, (int)initial_state);
        }

        /// <summary>
        /// Sets the state of edge to new state.
        /// Precondition: edge has a state attribute.
        /// </summary>
        /// <param name="edge">edge whose state is to be set</param>
        /// <param name="new_state">the state to be set</param>
        private static void Set_State(Edge edge, State new_state)
        {
            edge.SetInt(state_attribute, (int)new_state);
        }

        /// <summary>
        /// Transfers edge from its old_state to new_state; notifies all observers
        /// if old_state and new_state actually differ.
        /// </summary>
        /// <param name="edge">edge being changed</param>
        /// <param name="old_state">the old state of the edge</param>
        /// <param name="new_state">the new state of the edge after the change</param>
        private void Transition(Edge edge, State old_state, State new_state)
        {
            if (old_state != new_state)
            {
                Set_State(edge, new_state);
                Notify(new EdgeChange(edge, old_state, new_state));
            }
        }

        /// <summary>
        /// Returns true if edge is a specified edge in the architecture (has one of the
        /// following states: specified, convergent, absent).
        /// Precondition: edge must be in the architecture graph.
        /// </summary>
        /// <param name="edge">architecture dependency</param>
        /// <returns>true if edge is a specified architecture dependency</returns>
        private bool Is_Specified(Edge edge)
        {
            State state = Get_State(edge);
            return state == State.specified || state == State.convergent || state == State.absent;
        }

        // --------------------------------------------------------------------
        // Edge counter attribute
        // --------------------------------------------------------------------

        /// <summary>
        /// Name of the edge attribute for the counter of a dependency.
        /// </summary>
        private const string counter_attribute = "Reflexion.Counter";

        /// <summary>
        /// Sets counter of given architecture dependency to given value.
        /// Precondition: edge is in the architecture graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be set</param>
        /// <param name="value">value to be set</param>
        private void Set_Counter(Edge edge, int value)
        {
            edge.SetInt(counter_attribute, value);
        }

        /// <summary>
        /// Adds value to the counter attribute of given edge. The value may be negative.
        /// Precondition: edge is in the architecture graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be changed</param>
        /// <param name="value">value to be added</param>
        private void Change_Counter(Edge edge, int value)
        {
            if (edge.TryGetInt(counter_attribute, out int oldValue))
            {
                edge.SetInt(counter_attribute, oldValue + value);
            }
            else
            {
                edge.SetInt(counter_attribute, value);
            }
        }

        /// <summary>
        /// Returns the the counter of given architecture dependency 'edge'.
        /// Precondition: edge is in the architecture graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be retrieved</param>
        /// <returns>the counter of 'edge'</returns>
        public static int Get_Counter(Edge edge)
        {
            if (edge.TryGetInt(counter_attribute, out int value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Increases counter of edge by value (may be negative).
        /// If its counter drops to zero, the edge is removed.
        /// Notifies if edge's state changes.
        /// Precondition: edge is a dependency in architecture graph that
        /// was propagated from the implementation graph (i.e., !is_specified(edge)).
        /// </summary>
        /// <param name="edge">propagated dependency in architecture graph</param>
        /// <param name="value">value to be added</param>
        private void Change_Impl_Ref(Edge edge, int value)
        {
            int old_value = Get_Counter(edge);
            int new_value = old_value + value;
            //Debug.LogFormat("Change_Impl_Ref: changed counter from {0} to {1} for edge {2}\n", old_value, new_value, edge);
            if (new_value <= 0)
            {
                /*
                if (Get_State(edge) == State.divergent)
                {
                    Transition(edge, State.divergent, State.undefined);
                }
                */
                // We can drop this edge; it is no longer needed. Because the edge is
                // dropped and all observers are informed about the removal of this
                // edge, we do not need to inform them about its state change from
                // divergent/allowed/implicitly_allowed to undefined.
                Set_Counter(edge, 0);
                Notify(new PropagatedEdgeRemoved(edge));
                _architecture.RemoveEdge(edge);
            }
            else
            {
                Set_Counter(edge, new_value);
            }
        }

        /// <summary>
        /// Returns the value of the counter attribute of an implementation dependency.
        /// Currently, always 1 is returned.
        /// Precondition: edge is in the implementation graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be retrieved</param>
        /// <returns>value of the counter attribute of given implementation dependency</returns>
        private int Get_Impl_Counter(Edge edge = null)
        {
            // returns the value of the counter attribute of edge
            // at present, dependencies extracted from the source
            // do not have an edge counter; therefore, we return just 1
            return 1;
        }

        // --------------------------------------------------------------------
        //                      context information
        // --------------------------------------------------------------------

        /// <summary>
        /// Returns the implementation graph for which the reflexion data are calculated.
        /// </summary>
        /// <returns>implementation graph</returns>
        public Graph Get_Implementation()
        {
            return _implementation;
        }

        /// <summary>
        /// Returns the architecture graph for which the reflexion data are calculated.
        /// </summary>
        /// <returns>architecture graph</returns>
        public Graph Get_Architecture()
        {
            return _architecture;
        }

        /// <summary>
        /// Returns the mapping graph for which the reflexion data are calculated.
        /// </summary>
        /// <returns>mapping graph</returns>
        public Graph Get_Mapping()
        {
            return _mapping;
        }

        // --------------------------------------------------------------------
        //                             modifiers
        // --------------------------------------------------------------------
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
        // NODE section

        /// <summary>
        /// Adds given node to the mapping graph.
        /// Precondition: node must not be contained in the mapping graph.
        /// Postcondition: node is contained in the mapping graph and the architecture
        //   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the mapping graph</param>
        //public void Add_To_Mapping(Node node)
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
        //public void Delete_From_Mapping(Node node)
        //{
        //    throw new NotImplementedException(); // FIXME
        //}

        /// <summary>
        /// Adds given node to architecture graph.
        ///
        /// Precondition: node must not be contained in the architecture graph.
        /// Postcondition: node is contained in the architecture graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the architecture graph</param>
        public void Add_To_Architecture(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes given node from architecture graph.
        /// Precondition: node must be contained in the architecture graph.
        /// Postcondition: node is no longer contained in the architecture graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from the architecture graph</param>
        public void Delete_From_Architecture(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Adds given node to implementation graph.
        /// Precondition: node must not be contained in the implementation graph.
        /// Postcondition: node is contained in the implementation graph; all observers are
        /// informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the implementation graph</param>
        public void Add_To_Implementation(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes the given node from the implementation graph (and all its incoming and
        /// outgoing edges).
        /// Precondition: node must be contained in the implementation graph.
        /// Postcondition: node is no longer contained in the implementation graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from the implementation graph</param>
        public void Delete_From_Implementation(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        // EDGE section

        /// <summary>
        /// Adds a new Maps_To edge from 'from' to 'to' to the mapping graph and
        /// re-runs the reflexion analysis incrementally.
        /// Preconditions: from is contained in the implementation graph and not yet mapped explicitly
        /// and to is contained in the architecture graph.
        /// Postcondition: edge is contained in the mapping graph and the reflexion
        ///   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">the source of the maps_to edge to be added to the mapping graph</param>
        /// <param name="to">the target of the maps_to edge to be added to the mapping graph</param>
        public void Add_To_Mapping(Node from, Node to)
        {
            if (Is_Explicitly_Mapped(from))
            {
                throw new Exception("node " + from.ID + " is already mapped explicitly.");
            }
            else
            {
                // all nodes that should be mapped onto 'to', too, as a consequence of
                // mapping 'from'
                List<Node> subtree = Mapped_Subtree(from);
                // was 'to' mapped implicitly at all?
                if (_implicit_maps_to_table.TryGetValue(from.ID, out Node oldTarget))
                {
                    // from was actually mapped implicitly onto oldTarget
                    Unmap(subtree, oldTarget);
                }
                Add_To_Mapping_Graph(from, to);
                // adjust explicit mapping
                _explicit_maps_to_table[from.ID] = to;
                // adjust implicit mapping
                Change_Map(subtree, to);
                Map(subtree, to);
            }
        }

        /// <summary>
        /// The edge type maps-to edges mapping implementation entities onto architecture entities.
        /// </summary>
        private const string MapsToType = "Maps_To";

        /// <summary>
        /// Adds a clone of 'from' and a clone of 'to' to the mapping graph if
        /// they do not have one already and adds a Maps_To edge in between.
        ///
        /// Precondition: from is contained in the implementation graph and to is
        /// contained in the architecture graph.
        /// Postcondition: a clone F of from and a clone T of to exist in the
        /// mapping graph and there is a maps_to edge from F to T in the mapping
        /// graph.
        /// </summary>
        /// <param name="from">source of the maps-to edge</param>
        /// <param name="to">target of the maps-to edge</param>
        private void Add_To_Mapping_Graph(Node from, Node to)
        {
            Node from_clone = CloneInMapping(from);
            Node to_clone = CloneInMapping(to);
            // add maps_to edge to _mapping
            Edge mapsTo = new Edge(from_clone, to_clone, MapsToType);
            _mapping.AddEdge(mapsTo);
            Notify(new MapsToEdgeAdded(mapsTo));
        }

        /// <summary>
        /// Returns the node with the same ID as given node contained
        /// in _mapping. If no such node exists, a clone of the given node is
        /// created, added to _mapping, and returned.
        /// </summary>
        /// <param name="node">node whose clone in _mapping is needed</param>
        /// <returns>clone of node in _mapping</returns>
        private Node CloneInMapping(Node node)
        {
            Node clone = _mapping.GetNode(node.ID);
            if (clone == null)
            {
                clone = (Node)node.Clone();
                _mapping.AddNode(clone);
            }
            return clone;
        }

        /// <summary>
        /// All nodes in given subtree are implicitly mapped onto given target architecture node
        /// if target != null. If target == null, all nodes in given subtree are removed from
        /// _implicit_maps_to_table.
        ///
        /// Precondition: target is in the architecture graph and all nodes in subtree are in
        /// the implementation graph.
        /// </summary>
        /// <param name="subtree">list of nodes to be mapped onto target</param>
        /// <param name="target">architecture node onto which to map all nodes in subtree</param>
        private void Change_Map(List<Node> subtree, Node target)
        {
            if (target == null)
            {
                foreach (Node node in subtree)
                {
                    _implicit_maps_to_table.Remove(node.ID);
                }
            }
            else
            {
                foreach (Node node in subtree)
                {
                    _implicit_maps_to_table[node.ID] = target;
                }
            }
        }

        /// <summary>
        /// A function delegate that can be used to handle changes of the mapping by Handle_Mapped_Subtree.
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private delegate void Handle_Mapping_Change(Edge edge, Node from, Node to);

        /// <summary>
        /// Handles every dependency edges (incoming as well as outgoing) of every node in given subtree
        /// as follows:
        ///
        /// Let e = (i1, i2) be a dependency edge where either i1 or i2 or both are contained in subtree.
        /// Then e falls into one of the following categories:
        ///  (1) inner dependency: it is in between two entities, i1 and i2, mapped onto the same entity:
        ///      maps_to(i1) != null and maps_to(i2) != null and maps_to(i1) = maps_to(i2)
        ///  (2) cross dependency: it is in between two entities, i1 and i2, mapped onto different entities:
        ///      maps_to(i1) != null and maps_to(i2) != null and maps_to(i1) != maps_to(i2)
        ///  (3) dangling dependency: it is in between two entities, i1 and i2, not yet both mapped:
        ///      maps_to(i1) = null or maps_to(i2) = null
        ///
        /// Dangling dependencies will be ignored. For every inner or cross dependency, e, the given handler
        /// will be applied with the following arguments:
        ///
        ///   if e is an outgoing cross dependency, i.e., e.Source is contained in subtree:
        ///     handler(e, arch_node, maps_to(e.Target))
        ///   if e is an incoming cross dependency, i.e., e.Target is contained in subtree:
        ///     handler(e, maps_to(e.Source), arch_node)
        ///   if e is an inner dependency:
        ///     handler(e, arch_node, arch_node)
        ///
        /// Precondition: given arch_node is in the architecture graph and all nodes in subtree are in the
        /// implementation graph.
        /// </summary>
        /// <param name="subtree">implementation nodes whose mapping is to be adjusted</param>
        /// <param name="arch_node">architecture node related to the nodes in subtree (to be mapped or unmapped);
        /// this may either be the architecture node onto which the nodes in subtree were mapped originally
        /// when this function is called to unmap a subtree or architecture node onto which the nodes in subtree
        /// are to be mapped as new</param>
        /// <param name="handler">delegate handling the necessary adjustment</param>
        private void Handle_Mapped_Subtree(List<Node> subtree, Node arch_node, Handle_Mapping_Change handler)
        {
            // An inner dependency may occur twice in the iteration below, once it the set
            // of outgoing edges and once in the set of incoming edges of any nodes in the subtree.
            // We may call the handler only once for these that is why we need to keep a log of
            // inner edges already handled.
            HashSet<Edge> innerEdgesAlreadyHandled = new HashSet<Edge>();
            foreach (Node impl_node in subtree)
            {
                // assert: impl_node is in implementation graph
                foreach (Edge outgoing in impl_node.Outgoings)
                {
                    // assert: outgoing is in implementation graph
                    if (_implicit_maps_to_table.TryGetValue(outgoing.Target.ID, out Node oldTarget))
                    {
                        // outgoing is not dangling; it is either an inner or cross dependency
                        if (oldTarget == arch_node)
                        {
                            // outgoing is an inner dependency
                            if (innerEdgesAlreadyHandled.Add(outgoing))
                            {
                                // Note: HashSet.Add(e) yields true if e has not been contained in the set so far.
                                // That is, outgoing has not been processed yet.
                                handler(outgoing, arch_node, arch_node);
                            }
                        }
                        else
                        {
                            // outgoing is an outgoing cross dependency
                            handler(outgoing, arch_node, oldTarget);
                        }
                    }
                }
                foreach (Edge incoming in impl_node.Incomings)
                {
                    // assert: incoming is in implementation graph
                    if (_implicit_maps_to_table.TryGetValue(incoming.Source.ID, out Node oldTarget))
                    {
                        // incoming is not dangling; it is either an incoming cross or inner dependency
                        if (oldTarget == arch_node)
                        {
                            // outgoing is an inner dependency
                            if (innerEdgesAlreadyHandled.Add(incoming))
                            {
                                // Note: HashSet.Add(e) yields true if e has not been contained in the set so far.
                                // That is, incoming has not been processed yet.
                                handler(incoming, arch_node, arch_node);
                            }
                        }
                        else
                        {
                            handler(incoming, oldTarget, arch_node);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reverts the effect of the mapping of every node in the given subtree onto the
        /// reflexion data. That is, every non-dangling incoming and outgoing dependency of every
        /// node in the subtree will be "unpropagated" and "unlifted".
        /// Precondition: given oldTarget is non-null and contained in the architecture graph and all nodes
        /// in subtree are in the implementation graph. All nodes in subtree were originally mapped onto oldTarget.
        /// </summary>
        /// <param name="subtree">implementation nodes whose mapping is to be reverted</param>
        /// <param name="oldTarget">architecture node onto which the nodes in subtree were mapped originally</param>
        private void Unmap(List<Node> subtree, Node oldTarget)
        {
            Handle_Mapped_Subtree(subtree, oldTarget, Decrease_And_Lift);
        }

        /// <summary>
        /// Maps every node in the given subtree onto newTarget. That is, every non-dangling incoming
        /// and outgoing dependency of every node in the subtree will be propagated and lifted.
        /// Precondition: given newTarget is non-null and contained in the architecture graph and all nodes
        /// in subtree are in the implementation graph. All nodes in subtree are to be mapped onto newTarget.
        /// </summary>
        /// <param name="subtree">implementation nodes whose mapping is to be put into effect</param>
        /// <param name="newTarget">architecture node onto which the nodes in subtree are to be mapped</param>
        private void Map(List<Node> subtree, Node newTarget)
        {
            Handle_Mapped_Subtree(subtree, newTarget, Increase_And_Lift);
        }

        /// <summary>
        /// If both 'from' and 'to' are not null, the propagated architecture dependency corresponding
        /// to given 'implementation_dependency' is lifted where the counter of the matching specified
        /// architecture dependency and the counter of this propagated architecture dependency are decreased
        /// by the absolute value of implementation_dependency's counter.
        /// Otherwise, nothing is done.
        /// Precondition: 'implementation_dependency' is a dependency edge contained in implementation graph
        /// and 'to' and 'from' are contained in the architecture graph.
        /// </summary>
        /// <param name="implementation_dependency">an implementation dependency whose corresponding propagated dependency
        /// in the architecture graph is to be decreased and lifted</param>
        /// <param name="from">architecture node = Maps_To(implementation_dependency.Source)</param>
        /// <param name="to">architecture node = Maps_To(implementation_dependency.Target)</param>
        private void Decrease_And_Lift(Edge implementation_dependency, Node from, Node to)
        {
            if (from != null && to != null)
            {
                Edge propagated_edge = Get_Propagated_Dependency(from, to, implementation_dependency.Type);
                // It can happen that one of the edges end is not mapped, hence, no edge was propagated.
                if (propagated_edge != null)
                {
                    int counter = -Get_Impl_Counter(implementation_dependency);
                    if (Lift(propagated_edge.Source, propagated_edge.Target, propagated_edge.Type, counter, out Edge allowing_edge))
                    {
                        // matching specified architecture dependency found; no state change
                    }
                    //Debug.LogFormat("Decreasing counter of edge {0} by {1}\n", propagated_edge.ToString(), counter);
                    Change_Impl_Ref(propagated_edge, counter);
                }
            }
        }

        /// <summary>
        /// Propagates and lifts given implementation_dependency.
        /// Precondition: 'implementation_dependency' is a dependency edge contained in implementation graph
        /// and 'to' and 'from' are contained in the architecture graph.
        ///
        /// Note: from and to are actually ignored (intentionally).
        /// </summary>
        /// <param name="implementation_dependency">an implementation dependency whose corresponding propagated dependency
        /// in the architecture graph is to be decreased and lifted</param>
        /// <param name="from">architecture node = Maps_To(implementation_dependency.Source)</param>
        /// <param name="to">architecture node = Maps_To(implementation_dependency.Target)</param>
        private void Increase_And_Lift(Edge implementation_dependency, Node from, Node to)
        {
            // safely ignore from and to
            Propagate_And_Lift_Dependency(implementation_dependency);
        }

        /// <summary>
        /// Returns the list of nodes in the subtree rooted by given node (including this
        /// node itself) excluding those descendants in nested subtrees rooted by a mapper node,
        /// that is, are mapped elsewhere.
        /// Precondition: node is contained in implementation graph and not Is_Mapper(node).
        /// Postcondition: all nodes in the result are in the implementation graph and mapped
        /// onto the same architecture node as the given node; the given node is included
        /// in the result.
        /// </summary>
        /// <param name="node">root node of the subtree</param>
        /// <returns></returns>
        private List<Node> Mapped_Subtree(Node node)
        {
            List<Node> result = new List<Node> { node };
            foreach (Node child in node.Children())
            {
                if (!Is_Explicitly_Mapped(child))
                {
                    result.AddRange(Mapped_Subtree(child));
                }
            }
            return result;
        }

        /// <summary>
        /// Removes the given Maps_To edge from the mapping graph.
        /// Precondition: edge must be contained in the mapping graph and must have type Maps_To.
        /// Postcondition: edge is no longer contained in the mapping graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the Maps_To edge to be removed from the mapping graph</param>
        public void Delete_From_Mapping(Edge edge)
        {
            // The mapping target in the architecture graph.
            Node arch_target = _architecture.GetNode(edge.Target.ID);
            if (arch_target == null)
            {
                throw new Exception("Mapping target node " + edge.Target.ID + " is not in the architecture.");
            }
            else
            {
                // The mapping source in the implementation graph.
                Node impl_source = _implementation.GetNode(edge.Source.ID);
                if (impl_source == null)
                {
                    throw new Exception("Mapping source node " + edge.Source.ID + " is not in the implementation.");
                }
                else
                {
                    Delete_Maps_To(impl_source, arch_target, edge);
                }
            }
        }

        /// <summary>
        /// Reverts the effect of mapping from impl_source onto arch_target.
        /// Precondition: impl_source is in the implementation graph and is a mapper, arch_target is in the architecture
        /// graph, and maps_to is in the mapping graph, where maps_to.Source.ID == impl_source.ID
        /// and maps_to.Target.ID = arch_target.ID.
        /// Postconditions:
        /// (1) <paramref name="maps_to"/> is removed from _mapping
        /// (2) <paramref name="impl_source"/> is removed from _explicit_mapping
        /// (3) all nodes in the mapped subtree rooted by <paramref name="impl_source"/> are first unmapped
        /// and then -- if <paramref name="impl_source"/> has a mapped parent -- mapped onto the same target
        /// as the mapped parent of <paramref name="impl_source"/>; _implicit_mapping is adjusted accordingly
        /// (4) all other reflexion data are adjusted and all observers are notified
        /// </summary>
        /// <param name="impl_source">source of the mapping contained in _implementation</param>
        /// <param name="arch_target">target of the mapping contained in _architecture</param>
        /// <param name="maps_to">the mapping of impl_source onto arch_target as represented in _mapping</param>
        private void Delete_Maps_To(Node impl_source, Node arch_target, Edge maps_to)
        {
            if (!Is_Explicitly_Mapped(impl_source))
            {
                throw new Exception("Implementation node " + impl_source + " is not mapped explicitly.");
            }
            else if (!_explicit_maps_to_table.Remove(impl_source.ID))
            {
                throw new Exception("Implementation node " + impl_source + " is not mapped explicitly.");
            }
            else
            {
                // All nodes in the subtree rooted by impl_source and mapped onto the same target as impl_source.
                List<Node> subtree = Mapped_Subtree(impl_source);
                Unmap(subtree, arch_target);
                Node impl_source_parent = impl_source.Parent;
                if (impl_source_parent == null)
                {
                    // If impl_source has no parent, all nodes in subtree are not mapped at all any longer.
                    Change_Map(subtree, null);
                }
                else
                {
                    // If impl_source has a parent, all nodes in subtree should be mapped onto
                    // the architecture node onto which the parent is mapped -- if the parent
                    // is mapped at all (implicitly or explicitly).
                    if (_implicit_maps_to_table.TryGetValue(impl_source_parent.ID, out Node new_target))
                    {
                        // new_target is the architecture node onto which the parent of impl_source is mapped.
                        Change_Map(subtree, new_target);
                    }
                    if (new_target != null)
                    {
                        Map(subtree, new_target);
                    }
                }
                // First notify before we delete the maps_to edge for good.
                Notify(new MapsToEdgeRemoved(maps_to));
                // When an edge is removed from the graph, its source and target and graph containment are
                // deleted.
                _mapping.RemoveEdge(maps_to);
                _mapping.RemoveNode(maps_to.Source);
                if (maps_to.Target.Incomings.Count == 0)
                {
                    _mapping.RemoveNode(maps_to.Target);
                }
            }
        }

        /// <summary>
        /// Removes the Maps_To edge between 'from' and 'to' from the mapping graph (more precisely,
        /// the nodes corresponding to <paramref name="from"/> and <paramref name="to"/> in the
        /// mapping graph; where two nodes correspond if they have the same ID).
        /// Precondition: a Maps_To edge between 'from' and 'to' must be contained in the mapping graph,
        /// 'from' is contained in implementation graph and 'to' is contained in the architecture graph.
        /// Postcondition: the edge is no longer contained in the mapping graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">the source (contained in implementation graph) of the Maps_To edge
        /// to be removed from the mapping graph </param>
        /// <param name="to">the target (contained in the architecture graph) of the Maps_To edge
        /// to be removed from the mapping graph </param>
        public void Delete_From_Mapping(Node from, Node to)
        {
            // The node corresponding to 'from' in the mapping.
            Node map_from = _mapping.GetNode(from.ID);
            if (map_from == null)
            {
                throw new Exception("Node " + from + " is not mapped.");
            }
            else
            {
                // The node corresponding to 'to' in the mapping.
                Node map_to = _mapping.GetNode(to.ID);
                if (map_to == null)
                {
                    throw new Exception("Node " + to + " is no mapping target.");
                }
                else
                {
                    // The maps_to edges in between from map_from to map_to. There should be
                    // exactly one such edge.
                    List<Edge> maps_to_edges = map_from.From_To(map_to, "Maps_To");
                    if (maps_to_edges.Count == 0)
                    {
                        throw new Exception("There is no mapping from " + from + " onto " + to + ".");
                    }
                    else if (maps_to_edges.Count > 1)
                    {
                        throw new Exception("There are multiple mappings in between " + from + " and " + to + ".");
                    }
                    else
                    {
                        // Deletes the unique Maps_To edge from map_from to map_to in mapping graph
                        Delete_Maps_To(from, to, maps_to_edges[0]);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the given dependency edge to the architecture graph. This edge will
        /// be considered as a specified dependency.
        /// Precondition: edge must not be contained in the architecture graph and must
        /// represent a dependency.
        /// Postcondition: edge is contained in the architecture graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the dependency edge to be added to the architecture graph</param>
        public void Add_To_Architecture(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes the given specified dependency edge from the architecture.
        /// Precondition: edge must be contained in the architecture graph
        ///   and edge must represent a specified dependency.
        /// Postcondition: edge is no longer contained in the architecture graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the specified dependency edge to be removed from the architecture graph</param>
        public void Delete_From_Architecture(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Adds given dependency edge to the implementation graph.
        /// Precondition: edge must not be contained in the implementation graph
        /// Postcondition: edge is contained in the implementation graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the dependency edge to be added to the implementation graph</param>
        public void Add_To_Dependencies(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes the given dependency edge from the implementation graph.
        /// Precondition: edge must be contained in the implementation graph
        ///   and edge must be a dependency.
        /// Postcondition: edge is no longer contained in the implementation graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the dependency edge to be removed from the implementation graph</param>
        private void Delete_From_Dependencies(Edge edge)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Adds given child as a direct descendant of given parent in the node hierarchy of
        /// the implementation graph.
        /// Precondition: child and parent must be contained in the hierarchy graph;
        ///    child has no current parent.
        /// Postcondition: parent is a parent of child in the implementation graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <param name="parent">parent node</param>
        public void Add_Child_In_Implementation(Node child, Node parent)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes given child from its parent in the node hierarchy of
        /// the implementation graph.
        /// Precondition: child and parent must be contained in the hierarchy graph;
        ///    child has a parent.
        /// Postcondition: child has no longer a parent in the implementation graph and the reflexion
        ///   data are updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        public void Unparent_In_Implementation(Node child)
        {
            throw new NotImplementedException(); // FIXME
        }

        // --------------------------------------------------------------------
        // Summaries
        // --------------------------------------------------------------------

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
        /// <param name="summary"></param>
        public void PrintSummary(int[] summary)
        {
            string[] stateNames = Enum.GetNames(typeof(State));
            foreach (int s in Enum.GetValues(typeof(State)))
            {
                Debug.LogFormat("number of edges in state {0} = {1}\n", stateNames[s], summary[s]);
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
            Graph _architecture = Get_Architecture();
            string[] stateNames = Enum.GetNames(typeof(State));
            int[] summary = new int[stateNames.Length];

            foreach (Edge edge in _architecture.Edges())
            {
                summary[(int)Get_State(edge)] += Get_Counter(edge);
            }
            return summary;
        }

        /// <summary>
        /// Whether descendants may implicitly access their ancestors.
        /// </summary>
        private readonly bool _allow_dependencies_to_parents;

        // *****************************************
        // involved graphs
        // *****************************************

        /// <summary>
        /// The graph representing the implementation.
        /// </summary>
        private readonly Graph _implementation;

        /// <summary>
        /// The graph representing the specified architecture model.
        /// </summary>
        private readonly Graph _architecture;

        /// <summary>
        /// The graph describing the mapping of implementation entities onto architecture entities.
        /// </summary>
        private readonly Graph _mapping;

        // ******************************************************************
        // node mappings from node IDs onto nodes in the various graphs
        // ******************************************************************

        /// <summary>
        /// Mapping of IDs onto nodes in the implementation graph.
        /// </summary>
        private Dictionary<string, Node> InImplementation;
        /// <summary>
        /// Mapping of IDs onto nodes in the architecture graph.
        /// </summary>
        private Dictionary<string, Node> InArchitecture;
        /// <summary>
        /// Mapping of IDs onto nodes in the mapping graph.
        /// </summary>
        private Dictionary<string, Node> InMapping;

        // ********************************************************************************
        // predicates for nodes and edges from implementation relevant for reflexion analysis
        // ********************************************************************************

        /// <summary>
        /// Returns false for given node if it should be ignored in the reflexion analysis.
        /// For instance, artificial nodes, template instances, and nodes with ambiguous definitions
        /// are to be ignored.
        /// Precondition: node is a node in the implementation graph.
        /// </summary>
        /// <param name="node">implementation node</param>
        /// <returns>true if node should be considered in the reflexion analysis</returns>
        private bool Is_Relevant(Node node)
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
        private bool Is_Relevant(Edge edge)
        {
            return Is_Relevant(edge.Source) && Is_Relevant(edge.Target);
            // FIXME: For the time being, we consider every edge to be relevant as long as their
            // source and target are relevant.
        }

        // *****************************************
        // mapping
        // *****************************************

        /// <summary>
        /// The implicit mapping as derived from _explicit_maps_to_table.
        /// Note 1: the mapping key is the ID of a node in implementation and the mapping value a node in architecture
        /// Note 2: not every node in implementation is a key in this dictionary; node in the implementation
        /// neither mapped explicitly nor implicitly will not be contained.
        /// </summary>
        private Dictionary<string, Node> _implicit_maps_to_table;

        /// <summary>
        /// The explicit mapping from implementation node ID onto architecture nodes
        /// as derived from the mappings graph. This is equivalent to the content
        /// of the mapping graph where the corresponding nodes of the implementation
        /// (source of a mapping) and architecture (target of a mapping) are used-
        /// The correspondence of nodes between these three graphs is established
        /// by way of the unique ID attribute.
        /// Note: key is a node in the implementation and target a node in the
        /// architecture graph.
        /// </summary>
        private Dictionary<string, Node> _explicit_maps_to_table;

        /// <summary>
        /// Creates the transitive closure for the mapping so that we
        /// know immediately where an implementation entity is mapped to.
        /// The result is stored in _explicit_maps_to_table and _implicit_maps_to_table.
        /// </summary>
        private void Add_Transitive_Mapping()
        {
            // Because add_subtree_to_implicit_map() will check whether a node is a
            // mapper, which is done by consulting _explicit_maps_to_table, we need
            // to first create the explicit mapping and can only then map the otherwise
            // unmapped children
            _explicit_maps_to_table = new Dictionary<string, Node>();
            foreach (Edge mapsto in _mapping.Edges())
            {
                Node source = mapsto.Source;
                Node target = mapsto.Target;
                Debug.Assert(!String.IsNullOrEmpty(source.ID));
                Debug.Assert(!String.IsNullOrEmpty(target.ID));
                //Debug.Log(source.ID + "\n");
                Debug.Assert(InImplementation[source.ID] != null);
                //Debug.Log(target.ID + "\n");
                Debug.Assert(InArchitecture[target.ID] != null);
                _explicit_maps_to_table[source.ID] = InArchitecture[target.ID];
                //_explicit_maps_to_table[InImplementation[source.ID]] = InArchitecture[target.ID];
            }

            _implicit_maps_to_table = new Dictionary<string, Node>();
            foreach (Edge mapsto in _mapping.Edges())
            {
                Node source = mapsto.Source;
                Node target = mapsto.Target;
                Add_Subtree_To_Implicit_Map(InImplementation[source.ID], InArchitecture[target.ID]);
            }

#if DEBUG
            /*
            Debug.Log("\nexplicit mapping\n");
            dump_table(_explicit_maps_to_table);

            Debug.Log("\nimplicit mapping\n");
            dump_table(_implicit_maps_to_table);
            */
#endif
        }

        /// <summary>
        /// Returns true if node is explicitly mapped, that is, contained in _explicit_maps_to_table
        /// as a key.
        /// Precondition: node is a node of the implementation graph
        /// </summary>
        /// <param name="node">implementation node</param>
        /// <returns>true if node is explicitly mapped</returns>
        public bool Is_Explicitly_Mapped(Node node)
        {
            return _explicit_maps_to_table.ContainsKey(node.ID);
        }

        /// <summary>
        /// Adds all descendants of 'root' in the implementation that are implicitly
        /// mapped onto the same target as 'root' to the implicit mapping table
        /// _implicit_maps_to_table and maps them onto 'target'. This function recurses
        /// into all subtrees unless the root of a subtree is an explicitly mapped node.
        /// Preconditions:
        /// (1) root is a node in implementation
        /// (2) target is a node in architecture
        /// </summary>
        /// <param name="root">implementation node that is the root of a subtree to be mapped implicitly</param>
        /// <param name="target">architecture node that is the target of the implicit mapping</param>
        private void Add_Subtree_To_Implicit_Map(Node root, Node target)
        {
            List<Node> children = root.Children();
#if DEBUG
            // Debug.LogFormat("node {0} has {1} children\n", root.ID, children.Count);
#endif
            foreach (Node child in children)
            {
#if DEBUG
                //Debug.LogFormat("mapping child {0} of {1}\n", child.ID, root.ID);
#endif
                // child is contained in implementation
                if (!Is_Explicitly_Mapped(child))
                {
                    Add_Subtree_To_Implicit_Map(child, target);
                }
                else
                {
#if DEBUG
                    //Debug.LogFormat("child {0} of {1} is a mapper\n", child.ID, root.ID);
#endif
                }
            }
            _implicit_maps_to_table[root.ID] = target;
        }

        // *****************************************
        // DG utilities
        // *****************************************

        /// <summary>
        /// Adds value to counter of edge and transforms its state.
        /// Notifies if edge state changes.
        /// Precondition: edge is in architecture graph.
        /// </summary>
        /// <param name="edge">architecture dependency to be changed</param>
        /// <param name="value">the value to be added to the edge's counter</param>
        private void Change_Architecture_Dependency(Edge edge, int value)
        {
            int old_value = Get_Counter(edge);
            int new_value = old_value + value;
            State state = Get_State(edge);

            if (old_value == 0)
            {
                Transition(edge, state, State.convergent);
            }
            else if (new_value == 0)
            {
                Transition(edge, state, State.absent);
            }
            Set_Counter(edge, new_value);
        }

        // *****************************************
        // analysis steps
        // *****************************************

        /// <summary>
        /// Runs reflexion analysis from scratch, i.e., non-incrementally.
        /// </summary>
        private void From_Scratch()
        {
            Reset();
            RegisterNodes();
            Calculate_Convergences_And_Divergences();
            Calculate_Absences();
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

            foreach (Edge edge in _architecture.Edges())
            {
                State state = Get_State(edge);
                switch (state)
                {
                    case State.undefined:
                    case State.specified:
                        Set_Counter(edge, 0); // Note: architecture edges have a counter
                        Set_Initial(edge, State.specified); // initial state must be State.specified
                        break;
                    case State.absent:
                    case State.convergent:
                        // The initial state of an architecture dependency that was not propagated is specified.
                        Transition(edge, state, State.specified);
                        Set_Counter(edge, 0); // Note: architecture edges have a counter
                        break;
                    default:
                        // The edge is a left-over from a previous analysis and should be
                        // removed. Before we actually do that, we need to notify all observers.
                        Notify(new PropagatedEdgeRemoved(edge));
                        toBeRemoved.Add(edge);
                        break;
                }
            }
            // Removal of edges from _architecture must be done outside of the loop
            // because the loop iterates on _architecture.Edges().
            foreach (Edge edge in toBeRemoved)
            {
                _architecture.RemoveEdge(edge);
            }
        }

        /// <summary>
        /// Registers all nodes of all graphs under their ID in the respective mappings.
        /// </summary>
        private void RegisterNodes()
        {
            // Mapping of IDs onto nodes in the implementation graph.
            InImplementation = new SerializableDictionary<string, Node>();
            foreach (Node n in _implementation.Nodes())
            {
                InImplementation[n.ID] = n;
            }

            // Mapping of IDs onto nodes in the architecture graph.
            InArchitecture = new SerializableDictionary<string, Node>();
            foreach (Node n in _architecture.Nodes())
            {
                InArchitecture[n.ID] = n;
            }

            // Mapping of IDs onto nodes in the mapping graph.
            InMapping = new SerializableDictionary<string, Node>();
            foreach (Node n in _mapping.Nodes())
            {
                InMapping[n.ID] = n;
            }
        }

        /// <summary>
        /// Calculates convergences and divergences non-incrementally.
        /// </summary>
        private void Calculate_Convergences_And_Divergences()
        {
            // Iterate on all nodes in the domain of implicit_maps_to_table
            // (N.B.: these are nodes that are in 'implementation'), and
            // propagate and lift their dependencies in the architecture
            foreach (KeyValuePair<string, Node> mapsto in _implicit_maps_to_table)
            {
                // source_node is in implementation
                Node source_node = InImplementation[mapsto.Key];
                System.Diagnostics.Debug.Assert(source_node.ItsGraph == _implementation);
                if (Is_Relevant(source_node))
                {
                    // Node is contained in implementation graph and _implicit_maps_to_table
                    Propagate_And_Lift_Outgoing_Dependencies(source_node);
                }
            }
        }

        /// <summary>
        /// Calculates absences non-incrementally.
        /// </summary>
        private void Calculate_Absences()
        {
            // after calculate_convergences_and_divergences() all
            // architectural dependencies not marked as 'convergent'
            // are 'absent' (unless the architecture edge is marked 'optional'
            foreach (Edge edge in _architecture.Edges())
            {
                State state = Get_State(edge);
                if (Is_Specified(edge) && state != State.convergent)
                {
                    if (edge.HasToggle("Architecture.Is_Optional"))
                    {
                        Transition(edge, state, State.allowed_absent);
                    }
                    else
                    {
                        Transition(edge, state, State.absent);
                    }
                }
            }
        }

        /// <summary>
        /// Returns propagated dependency in architecture graph matching the type of
        /// of the implementation dependency Edge exactly if one exists;
        /// returns null if none can be found.
        /// Precondition: From and To are two architecture entities in architecture
        /// graph onto which an implementation dependency of type Its_Type was
        /// possibly propagated.
        /// Postcondition: resulting edge is in architecture or null
        /// </summary>
        /// <param name="source">source node of propagated dependency in architecture</param>
        /// <param name="target">target node of propagated dependency in architecture</param>
        /// <param name="its_type">the edge type of the propagated dependency</param>
        /// <returns>the propagated edge in the architecture graph between source and target
        /// with given type; null if there is no such edge</returns>
        private Edge Get_Propagated_Dependency(
            Node source, // source of edge; must be in architecture
            Node target, // target of edge; must be in architecture
            string its_type) // the edge type that must match exactly
        {
            List<Edge> connectings = source.From_To(target, its_type);

            foreach (Edge edge in connectings)
            {
                // There may be multiple (more precisely, two or less) edges from source to target with its_type,
                // but at most one that was specified by the user in the architecture model (we assume that
                // the architecture graph does not have redundant specified dependencies).
                // All others (more precisely, at most one) are dependencies that were propagated from the
                // implementation graph to the architecture graph.
                if (!Is_Specified(edge))
                {
                    return edge;
                }
            }
            return null;
        }

        /// <summary>
        /// Propagates and lifts dependency edge from implementation to architecture graph.
        ///
        /// Precondition: implementation_dep is in implementation graph.
        /// </summary>
        /// <param name="implementation_dep">the implementation edge to be propagated</param>
        private void Propagate_And_Lift_Dependency(Edge implementation_dep)
        {
#if DEBUG

            //Debug.LogFormat("propagate_and_lift_dependency: propagated implementation_dep = {0}\n",
            //                Edge_Name(implementation_dep, true));
#endif
            System.Diagnostics.Debug.Assert(implementation_dep.ItsGraph == _implementation);
            Node impl_source = implementation_dep.Source;
            Node impl_target = implementation_dep.Target;
            // Assert: impl_source and impl_target are in implementation
            string impl_type = implementation_dep.Type;

            Node arch_source = Maps_To(impl_source);
            Node arch_target = Maps_To(impl_target);
            // Assert: arch_source and arch_target are in architecture or null

            if (arch_source == null || arch_target == null)
            {
                // source or target are not mapped; so we cannot do anything
#if DEBUG
                //Debug.Log("source or target are not mapped; bailing out\n");
#endif
                return;
            }
            Edge propagated_architecture_dep = Get_Propagated_Dependency(arch_source, arch_target, impl_type);
            // Assert: architecture_dep is in architecture graph or null.
            System.Diagnostics.Debug.Assert(propagated_architecture_dep == null || propagated_architecture_dep.ItsGraph == _architecture);
            Edge allowing_edge = null;
            if (propagated_architecture_dep == null)
            {   // a propagated dependency has not existed yet; we need to create one
                propagated_architecture_dep
                  = New_Impl_Dep_In_Architecture
                      (arch_source, arch_target, impl_type, ref allowing_edge);
                // Assert: architecture_dep is in architecture graph (it is propagated; not specified)
#if DEBUG
                //Debug.LogFormat("new propagated dependency in architecture created: {0}\n", propagated_architecture_dep);
#endif
            }
            else
            {
                // a propagated dependency exists already
#if DEBUG
                //Debug.Log("a propagated dependency exists already\n");
#endif
                int impl_counter = Get_Impl_Counter(implementation_dep);
                // Assert: architecture_dep.Source and architecture_dep.Target are in architecture.
                Lift(propagated_architecture_dep.Source,
                     propagated_architecture_dep.Target,
                     impl_type,
                     impl_counter,
                     out allowing_edge);
                Change_Impl_Ref(propagated_architecture_dep, impl_counter);
            }
            // keep a trace of dependency propagation
            //causing.insert(std::pair<Edge*, Edge*>
            // (allowing_edge ? allowing_edge : architecture_dep, implementation_dep));
        }

        /// <summary>
        /// Propagates the outgoing dependencies of node from implementation to architecture
        /// graph and lifts them in architecture (if and only if an outgoing dependency is
        /// relevant).
        ///
        /// Precondition: node is in implementation graph.
        /// </summary>
        /// <param name="node">implementation node whose outgoings are to be propagated and lifted</param>
        private void Propagate_And_Lift_Outgoing_Dependencies(Node node)
        {
            System.Diagnostics.Debug.Assert(node.ItsGraph == _implementation);
            foreach (Edge edge in node.Outgoings)
            {
                // edge is in implementation
                // only relevant dependencies may be propagated and lifted
                if (Is_Relevant(edge))
                {
                    Propagate_And_Lift_Dependency(edge);
                }
            }
        }

        /// <summary>
        /// Returns the architecture node upon which 'node' is mapped;
        /// if 'node' is not mapped, null is returned.
        /// Precondition: node is in implementation.
        /// Postcondition: either result is null or result is in architecture
        /// </summary>
        /// <param name="node"></param>
        /// <returns>the architecture node upon which node is mapped or null</returns>
        private Node Maps_To(Node node)
        {
            System.Diagnostics.Debug.Assert(node.ItsGraph == _implementation);
            if (_implicit_maps_to_table.TryGetValue(node.ID, out Node target))
            {
                return target;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if this causing implementation edge is a dependency from child to
        /// parent in the sense of the "allow dependencies to parents" option.
        ///
        /// Precondition: edge is in implementation graph.
        /// </summary>
        /// <param name="edge">dependency edge to be checked</param>
        /// <returns>true if this causing edge is a dependency from child to
        /// parent</returns>
        private bool Is_Dependency_To_Parent(Edge edge)
        {
            Node mapped_source = Maps_To(edge.Source);
            Node mapped_target = Maps_To(edge.Target);
            // Assert: mapped_source and mapped_target are in architecture
            if (mapped_source != null && mapped_target != null)
            {
                return Is_Descendant_Of(mapped_source, mapped_target);
            }
            return false;
        }

        /// <summary>
        /// Returns true if 'descendant' is a descendant of 'ancestor' in the node hierarchy.
        ///
        /// Precondition: descendant and ancestor are in the same graph.
        /// </summary>
        /// <param name="descendant">source node</param>
        /// <param name="ancestor">target node</param>
        /// <returns>true if 'descendant' is a descendant of 'ancestor'</returns>
        private bool Is_Descendant_Of(Node descendant, Node ancestor)
        {
            Node cursor = descendant.Parent;
            while (cursor != null && cursor != ancestor)
            {
                cursor = cursor.Parent;
            }
            return cursor == ancestor;
        }

        /// <summary>
        /// Creates and returns a new edge of type its_type from 'from' to 'to' in given 'graph'.
        /// Use this function for source dependencies.
        /// Source dependencies are a special case because there may be two
        /// equivalent source dependencies between the same node pair: one
        /// specified and one propagated.
        ///
        /// Precondition: from and to are already in the graph.
        /// </summary>
        /// <param name="from">the source of the edge</param>
        /// <param name="to">the target of the edge</param>
        /// <param name="its_type">the type of the edge</param>
        /// <param name="graph">the graph the new edge should be added to</param>
        /// <returns>the new edge</returns>
        private Edge Add(Node from, Node to, string its_type, Graph graph)
        {
            // Note: there may be a specified as well as a propagated edge between the
            // same two architectural entities; hence, we may have multiple edges
            // in between
            Edge result = new Edge(from, to, its_type);
            graph.AddEdge(result);
            return result;
        }

        /// <summary>
        /// Adds a propagated dependency to the architecture graph
        /// from arch_source to arch_target with given edge type. This edge is lifted
        /// to an allowing specified architecture dependency if there is one (if that
        /// is the case the specified architecture dependency allowing this implementation
        /// dependency is returned in the output parameter allowing_edge_out. The state
        /// of the allowing specified architecture dependency is set to convergent if
        /// such an edge exists. Likewise, the state of the propagated dependency is
        /// set to either allowed, implicitly_allowed, or divergent.
        ///
        /// Preconditions:
        /// (1) there is no propagated edge from arch_source to arch_target with the given edge_type yet
        /// (2) arch_source and arch_target are in the architecture graph
        /// Postcondition: the newly created and returned dependency is contained in
        /// the architecture graph and marked as propagated.
        /// </summary>
        /// <param name="arch_source">architecture node that is the source of the propagated edge</param>
        /// <param name="arch_target">architecture node that is the target of the propagated edge</param>
        /// <param name="edge_type">type of the propagated implementation edge</param>
        /// <param name="allowing_edge_out">the specified architecture dependency allowing the implementation
        /// dependency if there is one; otherwise null; allowing_edge_out is also null if the implementation
        /// dependency form a self-loop (arch_source == arch_target); self-dependencies are implicitly
        /// allowed, but do not necessarily have a specified architecture dependency</param>
        /// <returns>a new propagated dependency in the architecture graph</returns>
        private Edge New_Impl_Dep_In_Architecture(Node arch_source,
                                                  Node arch_target,
                                                  string edge_type,
                                                  ref Edge allowing_edge_out)
        {
            int counter = 1;
            Edge propagated_architecture_dep = Add(arch_source, arch_target, edge_type, _architecture);
            // architecture_dep is a propagated dependency in the architecture graph
            Set_Counter(propagated_architecture_dep, counter);

            // TODO: Mark architecture_dep as propagated. Or maybe that is not necessary at all
            // because we have the edge state from which we can derive whether an edge is specified
            // or propagated.

            // architecture_dep is a dependency propagated from the implementation onto the architecture;
            // it was just created and, hence, has no state yet (which means it is State.undefined);
            // because it has just come into existence, we need to let our observers know about it
            Notify(new PropagatedEdgeAdded(propagated_architecture_dep));

            if (Lift(arch_source, arch_target, edge_type, counter, out allowing_edge_out))
            {
                // found a matching specified architecture dependency allowing propagated_architecture_dep
                Transition(propagated_architecture_dep, State.undefined, State.allowed);
            }
            else if (arch_source == arch_target)
            {
                // by default, every entity may use itself
                Transition(propagated_architecture_dep, State.undefined, State.implicitly_allowed);
                // Note: there is no specified architecture dependency that allows this implementation
                // dependency. Self dependencies are implicitly allowed.
                allowing_edge_out = null;
            }
            else if (_allow_dependencies_to_parents
                     && Is_Descendant_Of(propagated_architecture_dep.Source, propagated_architecture_dep.Target))
            {
                Transition(propagated_architecture_dep, State.undefined, State.implicitly_allowed);
                // Note: there is no specified architecture dependency that allows this implementation
                // dependency. Dependencies from descendants to ancestors are implicitly allowed if
                // _allow_dependencies_to_parents is true.
                allowing_edge_out = null;
            }
            else
            {
                Transition(propagated_architecture_dep, State.undefined, State.divergent);
                allowing_edge_out = null;
            }
            return propagated_architecture_dep;
        }

        /// <summary>
        /// Returns true if a matching architecture dependency is found, also
        /// sets allowing_edge_out to that edge in that case. If no such matchitng
        /// edge is found, false is returned and allowing_edge_out is null.
        /// Precondition: from and to are in the architecture graph.
        /// Postcondition: allowing_edge_out is a specified dependency in architecture graph or null.
        /// </summary>
        /// <param name="from">source of the edge</param>
        /// <param name="to">target of the edge</param>
        /// <param name="edge_type">type of the edge</param>
        /// <param name="counter">the multiplicity of the edge, i.e. the number of other
        /// edges covered by it</param>
        /// <param name="allowing_edge_out">the specified architecture dependency allowing the implementation
        /// dependency if there is any; otherwise null</param>
        /// <returns></returns>
        private bool Lift(Node from,
                          Node to,
                          string edge_type,
                          int counter,
                          out Edge allowing_edge_out)

        {
            List<Node> parents = to.Ascendants();
            // Assert: all parents are in architecture
            Node cursor = from;
            // Assert: cursor is in architecture
#if DEBUG
            //Debug.Log("lift: lift an edge from "
            //    + Qualified_Node_Name(from, true)
            //    + " to "
            //    + Qualified_Node_Name(to, true)
            //    + " of type "
            //    + edge_type
            //    + " and counter value "
            //    + counter
            //    + "\n");
            //dump_node_set(parents, "parents(to)");
#endif

            while (cursor != null)
            {
#if DEBUG
                //Debug.Log("cursor: " + qualified_node_name(cursor, false) + "\n");
#endif
                List<Edge> outs = cursor.Outgoings;
                // Assert: all edges in outs are in architecture.
                foreach (Edge edge in outs)
                {
                    // Assert: edge is in architecture; edge_type is the type of edge
                    // being propagated and lifted; it may be more concrete than the
                    // type of the specified architecture dependency.
                    if (Is_Specified(edge)
                        // && edge.Has_Supertype_Of(edge_type) FIXME: We consider that edges match independent of their types
                        && parents.Contains(edge.Target))
                    {   // matching architecture dependency found
                        Change_Architecture_Dependency(edge, counter);
                        allowing_edge_out = edge;
                        return true;
                    }
                }
                cursor = cursor.Parent;
            }
            // no matching architecture dependency found
#if DEBUG
            //Debug.Log("lift: no matching architecture dependency found" + "\n");
#endif
            allowing_edge_out = null;
            return false;
        }

        //------------------------------------------------------------------
        // Helper methods for debugging
        //------------------------------------------------------------------

        /// <summary>
        /// Returns the Source.File attribute of the given attributable if it exists, otherwise
        /// the empty string.
        /// </summary>
        /// <param name="graphElement">attributable element</param>
        /// <returns>Source.File attribute or empty string</returns>
        private static string Get_Filename(GraphElement graphElement)
        {
            string result = graphElement.Filename();
            return result == null ? string.Empty : result;
        }

        /// <summary>
        /// Returns the Source.Line attribute as a string if it exists; otherwise the empty string.
        /// </summary>
        /// <param name="graphElement">attributable element</param>
        /// <returns>Source.Line attribute or empty string</returns>
        private static string Get_Source_Line(GraphElement graphElement)
        {
            int? result = graphElement.SourceLine();
            return result.HasValue ? result.ToString() : string.Empty;
        }

        /// <summary>
        /// Returns a human-readable identifier for the given edge.
        /// Note: this identifier is not necessarily unique.
        /// </summary>
        /// <param name="edge">edge whose identifier is required</param>
        /// <param name="be_verbose">currently ignored</param>
        /// <returns>an identifier for the given edge</returns>
        private static string Edge_Name(Edge edge, bool be_verbose = false)
        {
            return edge.ToString();
        }

        /// <summary>
        /// Returns a human-readable identifier for the given node.
        /// Note: this identifier is not necessarily unique.
        /// </summary>
        /// <param name="node">node whose identifier is required</param>
        /// <param name="be_verbose">currently ignored</param>
        /// <returns>an identifier for the given node</returns>
        private static string Node_Name(Node node, bool be_verbose = false)
        {
            string name = node.SourceName;

            if (be_verbose)
            {
                return name
                + " (" + node.ID + ") "
                + node.GetType().Name;
            }
            else
            {
                return name;
            }
        }

        /// <summary>
        /// Returns a human-readable identifier for the given node further qualified with
        /// its source location if available.
        /// Note: this identifier is not necessarily unique.
        /// </summary>
        /// <param name="node">node whose identifier is required</param>
        /// <param name="be_verbose">currently ignored</param>
        /// <returns>an identifier for the given node</returns>
        // returns node name
        private static string Qualified_Node_Name(Node node, bool be_verbose = false)
        {
            string filename = Get_Filename(node);
            string loc = Get_Source_Line(node);
            return Node_Name(node, be_verbose) + "@" + filename + ":" + loc;
        }

        /// <summary>
        /// Returns the edge as a clause "type(from, to)".
        /// </summary>
        /// <param name="edge">edge whose clause is expected</param>
        /// <returns>the edge as a clause</returns>
        private static string As_Clause(Edge edge)
        {
            return edge.GetType().Name + "(" + Node_Name(edge.Source, false) + ", "
                                             + Node_Name(edge.Target, false) + ")";
        }

        /// <summary>
        /// Returns the edge as a qualified clause "type(from@loc, to@loc)@loc",
        /// </summary>
        /// <param name="edge">edge whose qualified clause is expected</param>
        /// <returns>qualified clause</returns>
        private static string As_Qualified_Clause(Edge edge)
        {
            return edge.GetType().Name + "(" + Qualified_Node_Name(edge.Source, false) + ", "
                                 + Qualified_Node_Name(edge.Target, false) + ")"
                                 + "@" + Get_Filename(edge) + ":" + Get_Source_Line(edge);
        }

        /// <summary>
        /// Dumps given node_set after given message was dumped.
        /// </summary>
        /// <param name="node_set">list of nodes whose qualified name is to be dumped</param>
        /// <param name="message">message to be emitted before the nodes</param>
        private static void Dump_Node_Set(List<Node> node_set, string message)
        {
            Debug.Log(message + "\n");
            foreach (Node node in node_set)
            {
                Debug.Log(Qualified_Node_Name(node, true) + "\n");
            }
        }

        /// <summary>
        /// Dumps given edge_set after given message was dumped.
        /// </summary>
        /// <param name="edge_set">list of edges whose qualified name is to be dumped</param>
        /// <param name="message">message to be emitted before the edges</param>
        private static void Dump_Edge_Set(List<Edge> edge_set)
        {
            foreach (Edge edge in edge_set)
            {
                Debug.Log(As_Qualified_Clause(edge) + "\n");
            }
        }

        /// <summary>
        /// Dumps the nodes and edges of the architecture graph to Unity's debug console.
        /// Intended for debugging.
        /// </summary>
        public void DumpArchitecture()
        {
            DumpGraph(_architecture);
            //Debug.LogFormat("REFLEXION RESULT ({0} nodes and {1} edges): \n", _architecture.NodeCount, _architecture.EdgeCount);
            //Debug.Log("NODES\n");
            //foreach (Node node in _architecture.Nodes())
            //{
            //    Debug.Log(node.ToString());
            //}
            //Debug.Log("EDGES\n");
            //foreach (Edge edge in _architecture.Edges())
            //{
            //    // edge counter state
            //    Debug.LogFormat("{0} {1} {2}\n", As_Clause(edge), Get_Counter(edge), Get_State(edge));
            //}
        }

        /// <summary>
        /// Dumps the nodes and edges of the <paramref name="graph"/> to Unity's debug console.
        /// Intended for debugging.
        /// </summary>
        public static void DumpGraph(Graph graph)
        {
            Debug.LogFormat("Graph {0} with {1} nodes and {2} edges: \n", graph.Name, graph.NodeCount, graph.EdgeCount);
            Debug.Log("NODES\n");
            foreach (Node node in graph.Nodes())
            {
                Debug.Log(node.ToString());
            }
            Debug.Log("EDGES\n");
            foreach (Edge edge in graph.Edges())
            {
                // edge counter state
                Debug.LogFormat("{0} {1} {2}\n", As_Clause(edge), Get_Counter(edge), Get_State(edge));
            }
        }

        public void DumpMapping()
        {
            Debug.Log("EXPLICITLY MAPPED NODES\n");
            DumpTable(_explicit_maps_to_table);
            Debug.Log("IMPLICITLY MAPPED NODES\n");
            DumpTable(_implicit_maps_to_table);
            Debug.Log("MAPPING GRAPH\n");
            DumpGraph(_mapping);
        }

        public static void DumpTable(Dictionary<string, Node> table)
        {
            foreach (KeyValuePair<string, Node> entry in table)
            {
                Debug.LogFormat("  {0} -> {1}\n", entry.Key, entry.Value.ID);
            }
        }

    } // ReflexionAnalysis
} // namespace
