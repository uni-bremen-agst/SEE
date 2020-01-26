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

The reflexion analysis calculates the reflexion view describing the convergences,
absences, divergences between an architecture and its implementation. The following
views are needed to calculate the reflexion view:

- architecture defines the expected architectural entities and their dependencies
(architectural entities may be hierarchical modeled by the part-of relation)
- the implementation model is represented in two separated views:
hierarchy view describes the nesting of implementation entities
dependency view describes the dependencies among the implementation entities
- mapping view describes the partial mapping of implementation entities onto
architectural entities.

Open issues: implementation of incremental analysis is missing.

*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Super class for all exceptions thrown by the architecture analysis.
    /// </summary>
    public class DG_Exception : Exception { }

    /// <summary>
    /// Thrown if the hierarchy is not a tree structure.
    /// </summary>
    public class Hierarchy_Is_Not_A_Tree : DG_Exception {}

    public class Corrupt_State : DG_Exception {}

    /// <summary>
    /// State of a dependency in the architecture or implementation within the 
    /// reflexion model.
    /// </summary>
    public enum State
    {
        undefined = 0,          // initial undefined state
        allowed = 1,            // propagated edge towards a convergence
        divergent = 2,          // divergence
        absent = 3,             // absence
        convergent = 4,         // convergence
        implicitly_allowed = 5, // self-usage is always implicitly allowed
        allowed_absent = 6,     // absence, but is_optional attribute set
        specified = 7           // tags an architecture edge that was created by the architect, i.e., is a specified edge
    };

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
    }

    /// <summary>
    /// Implements the reflexion analysis, which compares an implementation against an expected
    /// architecture based on a mapping between the two.
    /// </summary>
    public class Reflexion : Observable
    {
        /// <summary>
        /// Constructor for setting up and running the reflexion analysis.
        /// </summary>
        /// <param name="implementation">the implementation graph</param>
        /// <param name="architecture">the architecture model</param>
        /// <param name="mapping">the mapping of implementation nodes onto architecture nodes</param>
        public Reflexion(Graph implementation,
                         Graph architecture,
                         Graph mapping)
        {
            _implementation = implementation;
            _architecture = architecture;
            _mapping = mapping;
            RegisterNodes();
            add_transitive_mapping();
            from_scratch();
        }

        // Cleans up architecture view by removing the reflexion-specific
        // attributes Bauhaus::DG::Causing_Edge_Count_Attribute and
        // Bauhaus::DG::Reflexion_Edge_State_Attribute in architecture
        // view that are no longer needed after the analysis has finished.
        // Precondition: underlying DG and architecture must still exist.
        // Postcondition: reflexion is undefined afterwards; can no longer
        //  be used.
        public void clean_up()
        { }

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
        public State get_state(Edge edge)
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
        private void set_initial(Edge edge, State initial_state)
        {
            edge.SetInt(state_attribute, (int)initial_state);
        }

        /// <summary>
        /// Sets the state of edge to new state.
        /// Precondition: edge has a state attribute.
        /// </summary>
        /// <param name="edge">edge whose state is to be set</param>
        /// <param name="new_state">the state to be set</param>
        private void set_state(Edge edge, State new_state)
        {
            edge.SetInt(state_attribute, (int)new_state);
        }

        /// <summary>
        /// Transfers edge from its old_state to new_state; notifies all observers.
        /// </summary>
        /// <param name="edge">edge being changed</param>
        /// <param name="old_state">the old state of the edge</param>
        /// <param name="new_state">the new state of the edge after the change</param>
        private void transition(Edge edge, State old_state, State new_state)
        {
            set_state(edge, new_state);
            Notify(new EdgeChange(edge, old_state, new_state));
        }

        /// <summary>
        /// Returns true if edge is a specified edge in the architecture (has state 'specified').
        /// Precondition: edge must be in the architecture graph.
        /// </summary>
        /// <param name="edge">architecture dependency</param>
        /// <returns>true if edge is a specified architecture dependency</returns>
        private bool is_specified(Edge edge)
        {
            return get_state(edge) == State.specified;
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
        private void set_counter(Edge edge, int value)
        {
            edge.SetInt(counter_attribute, value);
        }

        /// <summary>
        /// Adds value to the counter attribute of given edge. The value may be negative.
        /// Precondition: edge is in the architecture graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be changed</param>
        /// <param name="value">value to be added</param>
        private void change_counter(Edge edge, int value)
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
        public int get_counter(Edge edge)
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
        private void change_impl_ref(Edge edge, int value)
        {
            int old_value = get_counter(edge);
            int new_value = old_value + value;

            if (new_value == 0)
            {
                if (get_state(edge) == State.divergent)
                {
                    transition(edge, State.divergent, State.undefined);
                }
                // we can drop this edge; it is no longer needed
                _architecture.RemoveEdge(edge);
            }
            else
            {
                set_counter(edge, new_value);
            }
        }

        /// <summary>
        /// Returns the value of the counter attribute of an implementation dependency.
        /// Currently, always 1 is returned.
        /// Precondition: edge is in the implementation graph.
        /// </summary>
        /// <param name="edge">an architecture dependency whose counter is to be retrieved</param>
        /// <returns>value of the counter attribute of given implementation dependency</returns>
        private int get_impl_counter(Edge edge = null)
        {
            // returns the value of the counter attribute of edge
            // at present, dependencies extracted from the source
            // do not have an edge counter; therefore, we return just 1
            return 1;
        }

        // --------------------------------------------------------------------
        //                      context information
        // --------------------------------------------------------------------
        //
        // @return the RFG for which the reflexion view is calculated
        //
        public Graph get_graph()
        {
            return _implementation;
        }
        //
        // @return the architecture view
        //
        public Graph get_architecture()
        {
            return _architecture;
        }
        
        //
        // @return the mapping view
        //
        public Graph get_mapping()
        {
            return _mapping;
        }

        // --------------------------------------------------------------------
        // reflexion results
        // --------------------------------------------------------------------

        // adds a browsable mapping to 'result_view' by adding mapped elements in
        // the hierarchy view as children to their mapping targets, replicating
        // the remaining hierarchy for nodes that are not mapped; populates
        // 'parents' with a mapping from inserted nodes to their parents
        // precondition: none of the nodes in the hierarchy view are visible in
        //               'result_view', all targets of mapping edges in the
        //               mapping view are visible in 'result_view'
        //public void add_browsable_mapping(View result_view,
        //                                  SerializableDictionary<Node, Node> parents)
        //{
        //    // FIXME
        //}

        // adds a single node and its hierarchy to 'result_view', stops
        // going up the hierarchy when it encounters an outgoing mapping edge
        // and adds a hierarchical edge to 'result_view' in its place; updates
        // 'parents' with any newly added hierarchy edges
        // precondition: all targets of mapping edges in the mapping view are
        //               visible in 'result_view'
        /*
        void add_causing_endpoint(Node endpoint,
                          View result_view,
                          Edge_Type* hierarchy_edge_type,
                                  const Edge_Type* mapping_edge_type,
                          std::map<Node, Node>& parents) const;
                          */

        // @return list of causes for architecture discrepancies consisting
        // of edge pairs (R, D) as follows:
        //   R in reflexion_view and
        //   type-of(R) = divergence => D in get_dependencies() and D is not
        //      allowed according to the architecture, that is, D causes R
        //   type-of(R) = absence    => D in get_architecture() and D is a specified
        //      architectural dependency for which no implementation dependency
        //      was found; that is, D causes R

        // Precondition: reflexion_view is the result of get_reflexion_view().
        //public List<Tuple<Edge, Edge>> get_caused(View reflexion_view)
        //{
        //    return null; // FIXME
        //}

        // As get_caused, but returns justifications for the convergence edges.
        // Precondition: reflexion_view is the result of get_reflexion_view().
        //public List<Tuple<Edge, Edge>> get_conforming(View reflexion_view)
        //{
        //    return null; // FIXME
        //}

        // --------------------------------------------------------------------
        //                             modifiers
        // --------------------------------------------------------------------
        // The following operations manipulate the relevant views of the
        // context and trigger the incremental update of the reflexion view;
        // if anything in the reflexion view changes, all listeners are informed
        // by the update message.
        // Never modify the underlying views directly; always use the following
        // methods. Otherwise the reflexion view may be in an inconsistent state.
        // Implementation detail: in case of a state change, Notify(ChangeEvent arg)
        // will be called where arg describes the type of change; such change information
        // consists of the object changed (either a single edge or node) and the kind
        // of change, namely, addition/removal or a counter increment/decrement.
        // Note that any of the following modifiers may result in a sequence of updates
        // where each single change is reported.
        // Note also that a removal of a node implies that all its incoming and outgoing
        // edges (hierarchical as well as dependencies) will be removed, too.
        // Note also that an addition of an edge will imply an implicit addition of
        // its source and target node if there are not yet contained in the target view.
        // NODE section
        //
        // @param node  the node to be added to the mapping view
        // precondition: node must not be contained in the mapping view
        // postcondition: node is contained in the mapping view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_mapping(Node node)
        {
            // FIXME
        }
        //
        // @param node  the node to be removed from the mapping view
        // precondition: node must be contained in the mapping view
        // postcondition: node is no longer contained in the mapping view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void delete_from_mapping(Node node)
        {
            // FIXME
        }
        //
        // @param node  the node to be added to the architecture view
        // precondition: node must not be contained in the architecture view
        // postcondition: node is contained in the architecture view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_architecture(Node node)
        {
            // FIXME
        }
        //
        // @param node  the node to be removed from the architecture view
        // precondition: node must be contained in the architecture view
        // postcondition: node is no longer contained in the architecture view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void delete_from_architecture(Node node)
        {
            // FIXME
        }
        //
        // @param node  the node to be added to the dependency view
        // precondition: node must not be contained in the dependency view
        // postcondition: node is contained in the dependency view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_dependencies(Node node)
        {
            // FIXME
        }
        //
        // @param node  the node to be removed from the dependency view
        // precondition: node must be contained in the dependency view
        // postcondition: node is no longer contained in the dependency view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void delete_from_dependencies(Node node)
        {
            // FIXME
        }

        //
        // @param node  the node to be added to the hierarchy view
        // precondition: node must not be contained in the hierarchy view
        // postcondition: node is contained in the hierarchy view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_hierarchy(Node node)
        {
            // FIXME
        }
        //
        // @param node  the node to be removed from the hierarchy view
        // precondition: node must be contained in the hierarchy view
        // postcondition: node is no longer contained in the hierarchy view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void delete_from_hierarchy(Node node)
        {
            // FIXME
        }

        // EDGE section

        //
        // @param edge  the edge to be added to the mapping view
        // precondition: edge must not be contained in the mapping view
        //   and edge must be a maps_to edge
        // postcondition: edge is contained in the mapping view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_mapping(Edge edge)
        {
            // FIXME
        }
        //
        // @param edge  the edge to be removed from the mapping view
        // precondition: edge must be contained in the mapping view
        // postcondition: edge is no longer contained in the mapping view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void delete_from_mapping(Edge edge)
        {
            // FIXME
        }

        //
        // @param edge  the edge to be added to the architecture view
        // precondition: edge must not be contained in the architecture view
        // postcondition: edge is contained in the architecture view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_architecture(Edge edge)
        {
            // FIXME
        }
        //
        // @param edge  the edge to be removed from the architecture view
        // precondition: edge must be contained in the architecture view
        //   and edge must be either a hierarchical or a dependency
        // postcondition: edge is no longer contained in the architecture view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void delete_from_architecture(Edge edge)
        {
            // FIXME
        }

        //
        // @param edge  the edge to be added to the dependency view
        // precondition: edge must not be contained in the dependency view
        // postcondition: edge is contained in the dependency view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_dependencies(Edge edge)
        {
            // FIXME
        }

        /*
        //
        // @param edge  the edge to be removed from the dependency view
        // precondition: edge must be contained in the dependency view
        //   and edge must be a dependency
        // postcondition: edge is no longer contained in the dependency view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        void delete_from_dependencies(Edge edge);
        */

        //
        // @param edge  the edge to be added to the hierarchy view
        // precondition: edge must not be contained in the hierarchy view
        //  and edge must be a hierarchical edge
        // postcondition: edge is contained in the hierarchy view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void add_to_hierarchy(Edge edge)
        {
            // FIXME
        }
        //
        // @param edge  the edge to be removed from the hierarchy view
        // precondition: edge must be contained in the hierarchy view
        //    and edge must be hierarchical
        // postcondition: edge is no longer contained in the hierarchy view and the reflexion
        //   view is updated; all listeners are informed of the change.
        //
        public void delete_from_hierarchy(Edge edge)
        {
            // FIXME
        }

        // --------------------------------------------------------------------
        // debugging
        // --------------------------------------------------------------------

        // dumps the convergences, absences, divergences to the logging channel
        /*
        public void dump_results()
        {
            Debug.LogFormat("REFLEXION RESULT ({0} nodes and {1} edges): \n", _reflexion.NodeCount, _reflexion.EdgeCount);
            foreach (Edge edge in _reflexion.Edges())
            {
                // edge counter state
                Debug.LogFormat("{0} {1} {2}\n", as_clause(edge), get_counter(edge), get_state(edge));

            }
        }
        */

        // *****************************************
        // involved graphs
        // *****************************************

        /// <summary>
        /// The graph representing the implementation.
        /// </summary>
        private Graph _implementation;

        /// <summary>
        /// The graph representing the specified architecture model.
        /// </summary>
        private Graph _architecture;

        /// <summary>
        /// The graph describing the mapping of implementation entities onto architecture entities.
        /// </summary>
        private Graph _mapping;

        // ******************************************************************
        // node mappings from node linknames onto nodes in the various graphs
        // ******************************************************************

        /// <summary>
        /// Mapping of linknames onto nodes in the implementation graph.
        /// </summary>
        private SerializableDictionary<string, Node> InImplementation;
        /// <summary>
        /// Mapping of linknames onto nodes in the architecture graph.
        /// </summary>
        private SerializableDictionary<string, Node> InArchitecture;
        /// <summary>
        /// Mapping of linknames onto nodes in the mapping graph.
        /// </summary>
        private SerializableDictionary<string, Node> InMapping;

        // *****************************************
        // Node hierarchy
        // *****************************************

        /// <summary>
        /// Yields the parent of node in hierarchy or null if it has no parent.
        /// Works for all nodes in all graphs. The parent returned is in the same
        /// graph where the given node is contained.
        /// </summary>
        /// <param name="node">node whose parent is to be retrieved</param>
        /// <returns>parent node or null</returns>
        private Node get_parent(Node node)
        {
            return node.Parent;
        }

        /// <summary>
        /// Yields the set of all transitive parents of node in hierarchy
        /// including node itself
        /// </summary>
        /// <param name="node">node whose ascendants are to be retrieved</param>
        /// <returns>ascendants of node in the hierarchy including node itself</returns>
        private List<Node> ascendants(Node node)
        {
            List<Node> result = new List<Node>();
            Node cursor = node;
            while (cursor != null)
            {
                result.Add(cursor);
                cursor = cursor.Parent;
            }
            return result;
        }

        // ********************************************************************************
        // predicates for nodes and edges from implementation relevant for reflexion analysis
        // ********************************************************************************

        /// <summary>
        /// Returns false for given node if it should be ignored in the reflexion analysis.
        /// For instance, artificial nodes, template instances, and nodes with ambiguous definitions
        /// are to be ignored.
        /// Precondition: source_node is node in implementation graph.
        /// </summary>
        /// <param name="source_node">implementation node</param>
        /// <returns>true if node should be considered in the reflexion analysis</returns>
        private bool is_relevant(Node source_node)
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
        // are to be ignored.
        /// Precondition: source_edge is node in implementation graph.
        /// </summary>
        /// <param name="source_edge">implementation dependency</param>
        /// <returns>true if edge should be considered in the reflexion analysis</returns>
        private bool is_relevant(Edge source_edge)
        {
            return is_relevant(source_edge.Source) && is_relevant(source_edge.Target);
            // FIXME: For the time being, we consider every edge to be relevant as long as their
            // source and target are relevant.
        }

        // *****************************************
        // mapping
        // *****************************************

        /// <summary>
        /// The implicit mapping as derived from _explicit_maps_to_table.
        /// Note: the mapping key is a node in implementation and the mapping value a node in architecture
        /// </summary>
        private SerializableDictionary<Node, Node> _implicit_maps_to_table;

        /// <summary>
        /// The explicit mapping from implementation nodes onto architecture nodes
        /// as derived from the mappings graph. This is equivalent to the content
        /// of the mapping graph where the corresponding nodes of the implementation
        /// (source of a mapping) and architecture (target of a mapping) are used-
        /// The correspondence of nodes between these three graphs is established
        /// by way of the unique Linkage.Name attribute.
        /// Note: key is a node in the implementation and target a node in the 
        /// architecture graph.
        /// </summary>
        private SerializableDictionary<Node, Node> _explicit_maps_to_table;

        /// <summary>
        /// Creates the transitive closure for the mapping so that we
        /// know immediately where an implementation entity is mapped to.
        /// The result is stored in _explicit_maps_to_table and _implicit_maps_to_table.
        /// </summary>
        private void add_transitive_mapping()
        {
            // Because add_subtree_to_implicit_map() will check whether a node is a 
            // mapper, which is done by consulting _explicit_maps_to_table, we need
            // to first create the explicit mapping and can only then map the otherwise
            // unmapped children
            _explicit_maps_to_table = new SerializableDictionary<Node, Node>();
            foreach (Edge mapsto in _mapping.Edges())
            {
                Node source = mapsto.Source;
                Node target = mapsto.Target;
                Debug.Assert(!String.IsNullOrEmpty(source.LinkName));
                Debug.Assert(!String.IsNullOrEmpty(target.LinkName));
                //Debug.Log(source.LinkName + "\n");
                Debug.Assert(InImplementation[source.LinkName] != null);
                //Debug.Log(target.LinkName + "\n");
                Debug.Assert(InArchitecture[target.LinkName] != null);
                _explicit_maps_to_table[InImplementation[source.LinkName]] = InArchitecture[target.LinkName];
            }

            _implicit_maps_to_table = new SerializableDictionary<Node, Node>();
            foreach (Edge mapsto in _mapping.Edges())
            {
                Node source = mapsto.Source;
                Node target = mapsto.Target;
                add_subtree_to_implicit_map(InImplementation[source.LinkName], InArchitecture[target.LinkName]);
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
        private bool is_mapper(Node node)
        {
            return _explicit_maps_to_table.ContainsKey(node);
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
        private void add_subtree_to_implicit_map(Node root, Node target)
        {
            List<Node> children = root.Children();
#if DEBUG
            // Debug.LogFormat("node {0} has {1} children\n", root.LinkName, children.Count);
#endif            
            foreach (Node child in children)
            {
#if DEBUG
                //Debug.LogFormat("mapping child {0} of {1}\n", child.LinkName, root.LinkName);
#endif
                // child is contained in implementation
                if (!is_mapper(child))
                {
                    add_subtree_to_implicit_map(child, target);
                }
                else
                {
#if DEBUG
                    //Debug.LogFormat("child {0} of {1} is a mapper\n", child.LinkName, root.LinkName);
#endif
                }
            }
            _implicit_maps_to_table[root] = target;
        }

        private class multimap<T1, T2> { }

        // ***********************************************************************************
        // Traceability between dependencies propagated from dependency view into architecture
        // ***********************************************************************************
        private multimap<Edge, Edge> _causing;
        // map: propagated edge in architecture -> set of dependencies in dependency;
        // _causing[p] := { d | d in dependency and d was propagated onto p }
        // where p is a dependency edges in architecture not specified by the user;
        // invariant: get_counter(p) == |_causing[p]|

        // TODO: currently, we add only to this map; if we implement an incremental
        // reflexion analysis, we need to update this map in case implementation
        // dependencies are removed.

        // This map helps us to create the result list explaining divergences and absences.
        // The result list (see the Ada implementation) contains all pairs of divergences
        // along with the dependencies causing the divergence as well as all pairs of
        // absences with the specified architecture dependency classified as absent.
        private List<Tuple<Edge, Edge>> _result_list;
        // This list explains the convergence edges: It contains all pairs of
        // convergence edges along with the dependencies implementing them.
        private List<Tuple<Edge, Edge>> _convergence_list;

        // *****************************************
        // DG utilities
        // *****************************************

        // creates and returns a new of type its_type from 'from' to 'to' in 'view'
        //private Edge add(Node from, Node to, string its_type, View view)
        //{
        //    return null; // FIXME
        //}

        // removes edge from view and notifies all observers
        //private void remove(Edge edge, View view)
        //{
        //    // FIXME
        //}

        // Adds value to counter of edge and transforms its state.
        // Notifies if edge state changes.
        // Assumption: edge is in Architecture view.
        private void change_architecture_dependency(Edge edge, int value)
        {
            // FIXME
        }

        // Helper struct storing an allowing edge and a propagated
        // architecture dependency
        private class Candidate_Edges
        {
            public Edge allowing_edge;
            public Edge architecture_dep;

            public Candidate_Edges()
            {
                allowing_edge = null;
                architecture_dep = null;
            }

            public Candidate_Edges(Edge allowing_edge, Edge architecture_dep)
            {
                this.allowing_edge = allowing_edge;
                this.architecture_dep = architecture_dep;
            }
        }

        // Choose from a map of candidate edge instances the (key) node n
        // with the smallest Linkage name, but prefer the node "preferred_node"
        // if possible. Set witness_node to n.
        private void select_divergence_representative(SerializableDictionary<Node, Candidate_Edges> instances,
                                                      Node first_preferred_node,
                                                      Node second_preferred_node,
                                                      Node witness_node,
                                                      Candidate_Edges witness_edges)
        {
            // FIXME
        }

        // *****************************************
        // analysis steps
        // *****************************************

        /// <summary>
        /// Runs reflexion analysis from scratch, i.e., non-incrementally.
        /// </summary>
        private void from_scratch()
        {
            // _reflexion = (Graph)_architecture.Clone();
            Reset();
            RegisterNodes();
            calculate_convergences_and_divergences();
            calculate_absences();
        }

        /// <summary>
        /// Resets architecture and implementation markings.
        /// </summary>
        private void Reset()
        {
            ResetArchitecture();
            ResetImplementation();
        }

        /// <summary>
        /// The state of all architectural dependencies will be set to 'undefined'
        /// and their counters be set to zero again. Propagated dependencies are
        /// removed.
        /// </summary>
        private void ResetArchitecture()
        {
            foreach (Edge edge in _architecture.Edges())
            {
                // TODO/FIXME: We should also remove all propagated dependencies.
                set_state(edge, State.undefined);
                set_counter(edge, 0); // FIXME: do architecture edges have counters at all?
            }
        }

        /// <summary>
        /// The state of all implementation dependencies will be set to 'undefined'.
        /// </summary>
        private void ResetImplementation()
        {
            foreach (Edge edge in _implementation.Edges())
            {
                set_state(edge, State.undefined);
                //set_counter(edge, 0); // FIXME: do implementation edges have counters at all?
            }
        }

        /// <summary>
        /// Registers all nodes of all graphs under their linkname in the respective mappings.
        /// </summary>
        private void RegisterNodes()
        {
            // Mapping of linknames onto nodes in the implementation graph.
            InImplementation = new SerializableDictionary<string, Node>();
            foreach (Node n in _implementation.Nodes())
            {
                InImplementation[n.LinkName] = n;
            }

            // Mapping of linknames onto nodes in the architecture graph.
            InArchitecture = new SerializableDictionary<string, Node>();
            foreach (Node n in _architecture.Nodes())
            {
                InArchitecture[n.LinkName] = n;
            }

            // Mapping of linknames onto nodes in the mapping graph.
            InMapping = new SerializableDictionary<string, Node>();
            foreach (Node n in _mapping.Nodes())
            {
                InMapping[n.LinkName] = n;
            }
        }

        /// <summary>
        /// Calculates convergences and divergences non-incrementally.
        /// </summary>
        private void calculate_convergences_and_divergences()
        {
            // Iterate on all nodes in the domain of implicit_maps_to_table
            // (N.B.: these are nodes that are in 'implementation'), and
            // propagate and lift their dependencies in the architecture
            foreach (var mapsto in _implicit_maps_to_table)
            {
                // source_node is in implementation
                Node source_node = mapsto.Key;
                System.Diagnostics.Debug.Assert(source_node.ItsGraph == _implementation);
                if (is_relevant(source_node))
                {
                    // Node is visible in dependencies and explicit_maps_to_table
                    propagate_and_lift_outgoing_dependencies(source_node);
                }
            }
        }

        // calculates absences non-incrementally
        private void calculate_absences()
        {
            // FIXME
        }

        /// <summary>
        /// Returns propagated dependency in architecture graph matching the type of
        /// of the implementation dependency Edge exactly;
        /// returns null if none can be found.
        /// Precondition: From and To are two architecture entities in architecture
        /// view onto which the implementation dependency of type Its_Type is
        /// to be propagated.
        /// Postcondition: resulting edge is in architecture or null
        /// </summary>
        /// <param name="source">source node of propagated dependency in architecture</param>
        /// <param name="target">target node of propagated dependency in architecture</param>
        /// <param name="its_type">the edge type of the propagated dependency</param>
        /// <returns>the propagated edge in the architecture graph between source and target
        /// with given type; null if there is no such edge</returns>
        private Edge get_propagated_dependency(
            Node source, // source of edge; must be in architecture
            Node target, // target of edge; must be in architecture
            string its_type) // the edge type that must match exactly
        {
            List<Edge> connectings = source.From_To(target, its_type);

            foreach (Edge edge in connectings)
            {
                if (!is_specified(edge))
                {
                    return edge;
                }
            }
            return null;
        }

        /// <summary>
        /// Propagates and lifts dependency edge from implementation to architecture graph.
        /// 
        /// Precondition: implementation_dep is in implementation.
        /// </summary>
        /// <param name="implementation_dep">the implementation edge to be propagated</param>
        private void propagate_and_lift_dependency(Edge implementation_dep)
        {
#if DEBUG
            /*
            Debug.LogFormat("propagate_and_lift_dependency: propagated implementation_dep = {0}\n",
                            edge_name(implementation_dep, true));
            */
#endif
            System.Diagnostics.Debug.Assert(implementation_dep.ItsGraph == _implementation);
            Node impl_source = implementation_dep.Source;
            Node impl_target = implementation_dep.Target;
            // Assert: impl_source and impl_target are in implementation
            string impl_type = implementation_dep.Type;

            Node arch_source = maps_to(impl_source);
            Node arch_target = maps_to(impl_target);
            // Assert: arch_source and arch_target are in architecture or null

            if (arch_source == null || arch_target == null)
            {
                // source or target are not mapped; so we cannot do anything
                return; 
            }
            Edge architecture_dep = get_propagated_dependency(arch_source, arch_target, impl_type);
            // Assert: architecture_dep is in architecture graph or null.
            System.Diagnostics.Debug.Assert(architecture_dep == null ||architecture_dep.ItsGraph == _architecture);
            Edge allowing_edge = null;
            if (architecture_dep == null)
            {   // has not existed yet
                architecture_dep
                  = new_impl_dep_in_architecture
                      (arch_source, arch_target, impl_type, ref allowing_edge);
                // Assert: architecture_dep is in architecture graph
            }
            else
            {
                int impl_counter = get_impl_counter(implementation_dep);
                // Assert: architecture_dep.Source and architecture_dep.Target are in architecture.
                lift(architecture_dep.Source,
                     architecture_dep.Target,
                     impl_type,
                     impl_counter, ref allowing_edge);
                change_impl_ref(architecture_dep, impl_counter);
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
        private void propagate_and_lift_outgoing_dependencies(Node node)
        {
            System.Diagnostics.Debug.Assert(node.ItsGraph == _implementation);
            foreach (Edge edge in node.Outgoings)
            {
                // edge is in implementation
                // only relevant dependencies may be propagated and lifted
                if (is_relevant(edge))
                {
                    propagate_and_lift_dependency(edge);
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
        private Node maps_to(Node node)
        {
            System.Diagnostics.Debug.Assert(node.ItsGraph == _implementation);
            if (_explicit_maps_to_table.TryGetValue(node, out Node target))
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
        private bool is_dependency_to_parent(Edge edge) 
        {
            Node mapped_source = maps_to(edge.Source);
            Node mapped_target = maps_to(edge.Target);
            // Assert: mapped_source and mapped_target are in architecture
            if (mapped_source != null && mapped_target != null)
            {
                return is_descendant_of(mapped_source, mapped_target);
            }
            return false;
        }

        /// <summary>
        /// Returns true if 'child' is a descendant of 'ancestor' in the hierarchy.
        /// 
        /// Precondition: child and ancestor are in the same graph.
        /// </summary>
        /// <param name="child">source node</param>
        /// <param name="ancestor">target node</param>
        /// <returns>true if 'source' is a descendant of 'target'</returns>
        private bool is_descendant_of(Node child, Node ancestor)
        {
            Node cursor = get_parent(child);
            while (cursor != null && cursor != ancestor)
            {
                cursor = get_parent(cursor);
            }
            return cursor == ancestor;
        }

        /// <summary>
        /// Creates and returns a new edge of type its_type from 'from' to 'to' in give 'graph'.
        /// Use this function for source dependencies; otherwise use Insert below.
        /// Source dependencies are a special case because there may be two
        /// equivalent source dependencies between the same node pair: one
        /// specified and one propagated.
        /// </summary>
        /// <param name="from">the source of the edge</param>
        /// <param name="to">the target of the edge</param>
        /// <param name="its_type">the type of the edge</param>
        /// <param name="graph">the graph in which to add the new edge</param>
        /// <returns>the new edge</returns>
        Edge add(Node from, Node to, string its_type, Graph graph)
        {
            // Note: there may be a specified as well as a propagated edge between the
            // same two architectural entities; hence, we may have multiple edges
            // in between

            // FIXME: We are assuming that from and to are already in the graph.
            // Is that true?
            Debug.Assert(graph.Nodes().Contains(from));
            Debug.Assert(graph.Nodes().Contains(to));
            Edge result = new Edge
            {
                Type = its_type,
                Source = from,
                Target = to
            };
            graph.AddEdge(result);
            return result;
        }

        /// <summary>
        /// Adds a propagated dependency to the architecture graph corresponding to the
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
        /// (2) the corresponding entities in the architecture exist
        /// (the maps-to targets); once the new implementation_dep is added,
        /// it is lifted.
        /// (3) arch_source and arch_target are in architecture.
        /// Postcondition: the newly created and returned dependency is contained in
        /// the architecture graph and marked as propagated.
        /// </summary>
        /// <param name="arch_source">architecture node that is the source of the propagated edge</param>
        /// <param name="arch_target">architecture node that is the target of the propagated edge</param>
        /// <param name="edge_type">type of the propagated implementation edge</param>
        /// <param name="allowing_edge_out">the specified architecture dependency allowing the implementation
        /// dependency if there is one; otherwise null</param>
        /// <returns>a new propagated dependency in the architecture graph</returns>
        private Edge new_impl_dep_in_architecture(Node arch_source,
                                                  Node arch_target,
                                                  string edge_type,
                                                  ref Edge allowing_edge_out)
        {
            int new_counter = get_impl_counter();
            Edge architecture_dep = add(arch_source, arch_target, edge_type, _architecture);
            set_counter(architecture_dep, new_counter);
            // TODO: Mark architecture_dep as propagated. Or maybe that is not necessary at all
            // because we have the edge state from which we can derive whether an edge is specified
            // or propagated.

            if (lift(arch_source, arch_target, edge_type, new_counter, ref allowing_edge_out))
            {
                set_initial(architecture_dep, State.allowed);
            }
            else if (arch_source == arch_target)
            {
                // by default, every entity may use itself
                set_initial(architecture_dep, State.implicitly_allowed);
            }
            else
            {
                set_initial(architecture_dep, State.divergent);
            }
            return architecture_dep;
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
        private bool lift(Node from,
                          Node to,
                          string edge_type,
                          int counter,
                          ref Edge allowing_edge_out)

        {
            List<Node> parents = ascendants(to);
            // Assert: all parents are in architecture
            Node cursor = from;
            // Assert: cursor is in architecture
#if DEBUG
            Debug.Log("lift: lift an edge from "
                + qualified_node_name(from, true)
                + " to "
                + qualified_node_name(to, true)
                + " of type "
                + edge_type
                + " and counter value "
                + counter
                + "\n");

            Debug.Log("parents(to):\n");
            dump_node_set(parents);
#endif

            while (cursor != null)
            {
#if DEBUG
                Debug.Log("cursor: " + qualified_node_name(cursor, false) + "\n");
#endif
                List<Edge> outs = cursor.Outgoings;
                // Assert: all edges in outs are in architecture.
                foreach (Edge edge in outs)
                { 
                    // Assert: edge is in architecture
                    if (is_specified(edge)
                        && edge.Has_Supertype_Of(edge_type)
                        && parents.Contains(edge.Target))
                    {   // matching architecture dependency found
                        change_architecture_dependency(edge, counter);
                        allowing_edge_out = edge;
                        return true;
                    }
                }
                cursor = get_parent(cursor);
            }
            // no matching architecture dependency found
#if DEBUG
            Debug.Log("lift: no matching architecture dependency found" + "\n");
#endif
            allowing_edge_out = null;
            return false;
        }

        // adds all children of 'mapped_node' to 'result_view' and adds the
        // hierarchical edges to their parent and does the same recursively for
        // each added child - completely ignores children that have outgoing
        // mapping edges; populates 'parents' with the hierarchical edges
        // precondition: 'mapped_node' is visible in 'result_view'
        private void add_unmapped_descendants(Node mapped_node,
                              string hierarchy_edge_type,
                              string mapping_edge_type,
                              //View result_view,
                              SerializableDictionary<Node, Node> parents) 
        {
            // FIXME
        }

        //------------------------------------------------------------------
        // Static helper methods for debugging
        //------------------------------------------------------------------

        private const string File_Name_Attribute   = "Source.File";
        private const string Line_Number_Attribute = "Source.Line";
        private const string Object_Name_Attribute = "Source.Name";
        
        private static string GetStringAttribute(Attributable attributable, string attribute)
        {
            if (attributable.TryGetString(attribute, out string result))
            {
                return result;
            }
            else
            {
                return "";
            }
        }

        // returns identifier for edge
        private static string edge_name(Edge edge, bool be_verbose = false)
        {
            return edge.ToString();
        }

        // returns identifier for node
        private static string node_name(Node node, bool be_verbose = false)
        {
            string name = node.SourceName;

            if (be_verbose)
            {
                return name
                + " (" + node.LinkName + ") "
                + node.GetType().Name;
            }
            else
            {
                return name;
            }
        }

        // returns Source.File attribute if it exists; otherwise ""
        private static string get_filename(Attributable attributable)
        {
            return GetStringAttribute(attributable, File_Name_Attribute);
        }

        // returns node name further qualified with source location if available
        private static string qualified_node_name(Node node, bool be_verbose = false)
        {
            string filename = get_filename(node);
            string loc = get_loc(node);
            return node_name(node, be_verbose) + "@" + filename + ":" + loc;
        }

        // returns the edge as a clause "type(from, to)"
        private static string as_clause(Edge edge)
        {
            return edge.GetType().Name + "(" + node_name(edge.Source, false) + ", "
                                             + node_name(edge.Target, false) + ")";
        }

        // returns Source.Line attribute as a string if it exists; otherwise ""
        private static string get_loc(Attributable attributable)
        {
            if (attributable.TryGetInt(Line_Number_Attribute, out int result))
            {
                return result.ToString();
            }
            else
            {
                return "";
            }
        }

        // returns the edge as a qualified clause "type(from@loc, to@loc)@loc"
        private static string as_qualified_clause(Edge edge)
        {
            return edge.GetType().Name + "(" + qualified_node_name(edge.Source, false) + ", "
                                 + qualified_node_name(edge.Target, false) + ")"
                                 + "@" + get_filename(edge) + ":" + get_loc(edge);
        }

        // dumps node_set
        static void dump_node_set(List<Node> node_set)
        {
            foreach (Node node in node_set)
            {
                Debug.Log(qualified_node_name(node, true) + "\n");
            }
        }

        // dumps edge_set
        static void dump_edge_set(List<Edge> edge_set)
        {
            foreach (Edge edge in edge_set)
            {
                Debug.Log(as_qualified_clause(edge) + "\n");
            }
        }

        // dumps table (mapping)
        static void dump_table(SerializableDictionary<Node, Node> table)
        {
            foreach(var entry in table)
            {
                Debug.LogFormat("{0} --maps_to--> {1}\n",
                                qualified_node_name(entry.Key, true),
                                qualified_node_name(entry.Value, true));
            }
        }
    } // namespace SEE
} // namespace DataModel
