using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SEE.DataModel;
using SEE.DataModel.DG;
using UnityEngine.Assertions;
using static SEE.Tools.ReflexionAnalysis.ReflexionSubgraph;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This partial class contains methods for the Incremental Reflexion Analysis.
    /// </summary>
    public partial class ReflexionGraph
    {
        /// <summary>
        /// Creates an edge of given <paramref name="type"/> from the given node <paramref name="from"/>
        /// to the given node <paramref name="to"/> and adds it to the implementation graph,
        /// adjusting the reflexion analysis incrementally.
        /// This will propagate and lift the new edge, thereby increasing the counter of the matching specified edge
        /// if it exists.
        /// 
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="from"/> is contained in the implementation graph.</li>
        /// <li><paramref name="to"/> is contained in the implementation graph.</li>
        /// </ul>
        /// 
        /// Postcondition: A new edge from <paramref name="from"/> to <paramref name="to"/>
        ///   is contained in the implementation graph and the reflexion data is updated;
        ///   all observers are informed of the change by an <see cref="EdgeAdded"/> event.
        /// </summary>
        /// <param name="from">source node of the new edge</param>
        /// <param name="to">target node of the new edge</param>
        /// <param name="type">type of the new edge</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="from"/> or <paramref name="to"/> is
        /// not in the implementation graph</exception>
        /// <returns>The newly created edge</returns>
        public Edge AddToImplementation(Node from, Node to, string type)
        {
            AssertOrThrow(ContainsNode(from) && from.IsInImplementation(),
                          () => new NotInSubgraphException(Implementation, from));
            AssertOrThrow(ContainsNode(to) && to.IsInImplementation(),
                          () => new NotInSubgraphException(Implementation, to));
            Edge edge = AddEdge(from, to, type, false);
            PropagateAndLiftDependency(edge);
            return edge;
        }

        /// <summary>
        /// Adds the given <paramref name="edge"/> to the implementation graph,
        /// adjusting the reflexion analysis incrementally.
        /// 
        /// This will propagate and lift the new edge, thereby increasing the counter of the matching specified edge
        /// if it exists.
        /// 
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="edge"/>.Source is contained in the implementation graph.</li>
        /// <li><paramref name="edge"/>.Target is contained in the implementation graph.</li>
        /// </ul>
        /// </summary>
        /// <param name="edge">the edge to add to the reflexion graph</param>
        /// <exception cref="NotInSubgraphException">When the source or target of the <paramref name="edge"/> is
        /// not in the implementation graph</exception>
        public void AddToImplementation(Edge edge)
        {
            AssertOrThrow(ContainsNode(edge.Source) && edge.Source.IsInImplementation(), 
                          () => new NotInSubgraphException(Implementation, edge.Source));
            AssertOrThrow(ContainsNode(edge.Target) && edge.Target.IsInImplementation(), 
                          () => new NotInSubgraphException(Implementation, edge.Target));
            edge.SetInImplementation();
            SetState(edge, State.Undefined);
            base.AddEdge(edge);
            PropagateAndLiftDependency(edge);
        }

        /// <summary>
        /// Removes all edges from <paramref name="from"/> to <paramref name="to"/>
        /// with the given <paramref name="type"/> from the implementation graph,
        /// adjusting the reflexion analysis incrementally.
        /// This will reduce the counter attribute of a corresponding propagated edge, if it exists, and may
        /// cause previously convergent edges to now become absent.
        ///
        /// Precondition: <paramref name="from"/> and <paramref name="to"/> are in the implementation graph and have
        /// at least one edge connecting them.
        /// If the latter isn't true, this method will have no effect.
        /// </summary>
        /// <param name="from">Source node of the edge which shall be deleted</param>
        /// <param name="to">Target node of the edge which shall be deleted</param>
        /// <param name="type">Type of the edge which shall be deleted. If <c>null</c>, will be ignored.</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="to"/> or <paramref name="from"/> is
        /// not present in the implementation graph</exception>
        public void RemoveFromImplementation(Node from, Node to, string type = null)
        {
            AssertOrThrow(ContainsNode(from) && from.IsInImplementation(),
                          () => new NotInSubgraphException(Implementation, from));
            AssertOrThrow(ContainsNode(to) && to.IsInImplementation(),
                          () => new NotInSubgraphException(Implementation, to));
            from.FromTo(to, type).ForEach(RemoveFromImplementation);
        }

        /// <summary>
        /// Removes an edge from the implementation graph, adjusting the reflexion analysis incrementally.
        /// This will reduce the counter attribute of a corresponding propagated edge, if it exists, and may
        /// cause previously convergent edges to now become absent.
        ///
        /// Precondition: <paramref name="edge"/> is an implementation edge contained in the reflexion graph.
        /// </summary>
        /// <param name="edge">The implementation edge to be removed from the graph.</param>
        /// <exception cref="NotInSubgraphException">When the given <paramref name="edge"/> is not in the
        /// implementation graph.</exception>
        public void RemoveFromImplementation(Edge edge)
        {
            AssertOrThrow(edge.IsInImplementation() && ContainsEdge(edge),
                          () => new NotInSubgraphException(Implementation, edge));
            Edge propagated = GetPropagatedDependency(edge);
            if (propagated != null)
            {
                if (propagated.State() != State.Divergent)
                {
                    // A convergence exists that must be handled (i.e. by decreasing the matching specified edge's counter).
                    if (!Lift(propagated.Source, propagated.Target, propagated.Type, -GetImplCounter(edge), out Edge _))
                    {
                        throw new InvalidOperationException($"Since this edge is {propagated.State()} and not "
                                                            + "Divergent, it must have a matching specified edge.");
                    }
                }

                bool implRemoved = propagationTable[propagated.ID].Remove(edge);
                Assert.IsTrue(implRemoved, "Originating edge must be present in propagation table!");
                ChangePropagatedDependency(propagated, -GetImplCounter(edge));
            }

            base.RemoveEdge(edge);
        }

        /// <summary>
        /// Creates an edge of given <paramref name="type"/> from the given node <paramref name="from"/>
        /// to the given node <paramref name="to"/> and adds it to the architecture graph,
        /// adjusting the reflexion analysis incrementally.
        /// This edge will be considered as a specified dependency.
        /// It may not be redundant.
        /// 
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="from"/> is contained in the architecture graph.</li>
        /// <li><paramref name="to"/> is contained in the architecture graph.</li>
        /// <li>The newly created specified edge will not be redundant.</li>
        /// </ul>
        /// Postcondition: A new edge from <paramref name="from"/> to <paramref name="to"/> is contained in the
        ///   architecture graph and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">source node of the new edge</param>
        /// <param name="to">target node of the new edge</param>
        /// <param name="type">type of the new edge</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="from"/> or <paramref name="to"/>
        /// is not contained in the architecture graph.</exception>
        /// <exception cref="RedundantSpecifiedEdgeException">When the edge from <paramref name="from"/> to
        /// <paramref name="to"/> would be redundant to another specified edge.</exception>
        /// <returns>The newly created specified edge</returns>
        public Edge AddToArchitecture(Node from, Node to, string type)
        {
            // Preconditions are checked in AddToArchitecture.
            Edge edge = AddEdge(from, to, type, true, false);
            AddToArchitecture(edge);
            return edge;
        }

        /// <summary>
        /// Adds the given <paramref name="edge"/> to the implementation graph,
        /// adjusting the reflexion analysis incrementally.
        /// 
        /// This will propagate and lift the new edge, thereby increasing the counter of the matching specified edge
        /// if it exists.
        /// 
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="edge"/>.Source is contained in the implementation graph.</li>
        /// <li><paramref name="edge"/>.Target is contained in the implementation graph.</li>
        /// </ul>
        /// </summary>
        /// <param name="edge">the edge to add to the reflexion graph</param>
        /// <exception cref="NotInSubgraphException">When the source or target of the <paramref name="edge"/> is
        /// not in the implementation graph</exception>
        public void AddToArchitecture(Edge edge)
        {
            AssertOrThrow(ContainsNode(edge.Source) && edge.Source.IsInArchitecture(), 
                          () => new NotInSubgraphException(Architecture, edge.Source));
            AssertOrThrow(ContainsNode(edge.Target) && edge.Target.IsInArchitecture(), 
                          () => new NotInSubgraphException(Architecture, edge.Target));
            AssertNotRedundant(edge.Source, edge.Target, edge.Type);
            edge.SetInArchitecture();
            SetState(edge, State.Specified);
            base.AddEdge(edge);

            // We need to handle the propagated dependencies covered by this specified edge.

            // We don't care about order, but O(1) `Contains` is nice to have here, hence the Set
            ISet<Node> targetKids = new HashSet<Node>(edge.Target.PostOrderDescendants());

            // TODO: This should also check the type, but the examples in StreamingAssets need to be updated.
            //       Also, other places in the reflexion analysis should make sure that the type conforms.
            bool IsCoveredEdge(Edge e) => !IsSpecified(e) && targetKids.Contains(e.Target);

            IEnumerable<Edge> coveredEdges = edge.Source.PostOrderDescendants()
                                                 .SelectMany(x => x.Outgoings)
                                                 .Where(IsCoveredEdge);
            bool noneCovered = true;
            foreach (Edge coveredEdge in coveredEdges)
            {
                noneCovered = false;
                ChangeSpecifiedDependency(edge, GetImplCounter(coveredEdge));
                Transition(coveredEdge, coveredEdge.State(), State.Allowed);
            }

            if (noneCovered)
            {
                // In this case, we need to set the state manually because `ChangeSpecifiedDependency` wasn't called.
                Transition(edge, State.Specified, State.Absent);
                SetCounter(edge, 0);
            }
        }

        /// <summary>
        /// Removes all edges from <paramref name="from"/> to <paramref name="to"/>
        /// with the given <paramref name="type"/> from the architecture graph,
        /// adjusting the reflexion analysis incrementally.
        ///
        /// Precondition: <paramref name="from"/> and <paramref name="to"/> are in the architecture graph and have
        /// exactly one specified edge connecting them.
        /// </summary>
        /// <param name="from">Source node of the edge which shall be deleted</param>
        /// <param name="to">Target node of the edge which shall be deleted</param>
        /// <param name="type">Type of the edge which shall be deleted. If <c>null</c>, will be ignored.</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="from"/> or <paramref name="to"/>
        /// is not contained in the architecture graph.</exception>
        public void RemoveFromArchitecture(Node from, Node to, string type = null)
        {
            AssertOrThrow(from.IsInArchitecture(), () => new NotInSubgraphException(Architecture, from));
            AssertOrThrow(to.IsInArchitecture(), () => new NotInSubgraphException(Architecture, to));
            RemoveFromArchitecture(from.FromTo(to, type).Single(IsSpecified));
        }

        /// <summary>
        /// Removes the given specified dependency <paramref name="edge"/> from the architecture.
        /// Precondition: <paramref name="edge"/> must be contained in the architecture graph
        ///   and must represent a specified dependency.
        /// Postcondition: <paramref name="edge"/> is no longer contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the specified dependency edge to be removed from the architecture graph</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="edge"/>
        /// is not contained in the architecture graph.</exception>
        /// <exception cref="ExpectedSpecifiedEdgeException">When the given <paramref name="edge"/> is not
        /// a specified edge.</exception>
        public void RemoveFromArchitecture(Edge edge)
        {
            AssertOrThrow(ContainsEdge(edge) && edge.IsInArchitecture(),
                          () => new NotInSubgraphException(Architecture, edge));
            AssertOrThrow(IsSpecified(edge), () => new ExpectedSpecifiedEdgeException(edge));

            if (edge.State() == State.Convergent)
            {
                // We need to handle the propagated dependencies covered by this specified edge.

                // We don't care about order, but O(1) `Contains` is nice to have here, hence the Set
                ISet<Node> targetKids = new HashSet<Node>(edge.Target.PostOrderDescendants());

                bool IsCoveredEdge(Edge e) => !IsSpecified(e)
                                              && targetKids.Contains(e.Target)
                                              && e.HasSupertypeOf(edge.Type);

                IEnumerable<Edge> coveredEdges = edge.Source.PostOrderDescendants()
                                                     .SelectMany(x => x.Outgoings)
                                                     .Where(IsCoveredEdge);
                foreach (Edge coveredEdge in coveredEdges)
                {
                    Transition(coveredEdge, coveredEdge.State(), State.Divergent);
                }
            }

            base.RemoveEdge(edge);
        }


        /// <summary>
        /// Adds a new Maps_To edge from <paramref name="from"/> to <paramref name="to"/> to the mapping graph and
        /// re-runs the reflexion analysis incrementally.
        /// Preconditions: <paramref name="from"/> is contained in the implementation graph
        /// and <paramref name="to"/> is contained in the architecture graph.
        /// If <paramref name="overrideMapping"/> is false, <paramref name="from"/> must not yet be explicitly mapped.
        /// Postcondition: Created edge is contained in the mapping graph and the reflexion
        ///   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">the source of the Maps_To edge to be added to the mapping graph</param>
        /// <param name="to">the target of the Maps_To edge to be added to the mapping graph</param>
        /// <param name="overrideMapping">whether any existing mapping from <paramref name="from"/> shall be
        /// replaced by this one</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="from"/>
        /// is not contained in the implementation graph or <paramref name="to"/> is not contained in the
        /// architecture graph.</exception>
        /// <exception cref="AlreadyExplicitlyMappedException">When <paramref name="from"/> is already explicitly
        /// mapped to an architecture node and <paramref name="overrideMapping"/> is false.</exception>
        public Edge AddToMapping(Node from, Node to, bool overrideMapping = false)
        {
            AssertOrThrow(from.IsInImplementation(), () => new NotInSubgraphException(Implementation, from));
            AssertOrThrow(to.IsInArchitecture(), () => new NotInSubgraphException(Architecture, to));

            if (IsExplicitlyMapped(from))
            {
                // Existing mapping is only allowed if it can be overridden
                AssertOrThrow(overrideMapping, () => new AlreadyExplicitlyMappedException(from, MapsTo(from)));
                if (MapsTo(from) == to)
                {
                    // We don't need to do anything more, since the node is already mapped to the target.
                    return from.FromTo(MapsTo(from), MapsToType).Single();
                }
                RemoveFromMapping(from);
            }

            // all nodes that should be mapped onto 'to', too, as a consequence of
            // mapping 'from'
            List<Node> subtree = MappedSubtree(from);
            // was 'from' mapped implicitly at all?
            if (implicitMapsToTable.TryGetValue(from.ID, out Node oldTarget))
            {
                // from was actually mapped implicitly onto oldTarget
                Unmap(subtree, oldTarget);
            }

            // FIXME: If below operation fails, we are in an inconsistent state.
            AddToMappingGraph(from, to);
            // adjust explicit mapping
            explicitMapsToTable[from.ID] = to;
            // adjust implicit mapping
            ChangeMap(subtree, to);
            Map(subtree, to);
            return from.FromTo(MapsTo(from), MapsToType).Single();
        }

        /// <summary>
        /// Removes the given Maps_To <paramref name="edge"/> from the mapping graph.
        /// Precondition: <paramref name="edge"/> must be in the mapping graph.
        /// Postcondition: the edge is no longer contained in the mapping graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">The edge that shall be removed from the mapping graph.</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="edge"/>
        /// is not contained in the mapping graph.</exception>
        public void RemoveFromMapping(Edge edge)
        {
            AssertOrThrow(edge.IsInMapping() && ContainsEdge(edge), () => new NotInSubgraphException(Mapping, edge));
            RemoveMapsTo(edge);
        }

        /// <summary>
        /// Removes the Maps_To edge starting from <paramref name="from"/> from the mapping graph.
        /// Precondition: a Maps_To edge between <paramref name="from"/> and <paramref name="to"/>
        /// must be contained in the mapping graph, <paramref name="from"/> is contained in implementation graph
        /// and <paramref name="to"/> is contained in the architecture graph.
        /// Postcondition: the edge is no longer contained in the mapping graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="from">the source (contained in implementation graph) of the Maps_To edge
        /// to be removed from the mapping graph </param>
        /// <param name="ignoreUnmapped">Whether to do nothing when the given node is not explicitly mapped.
        /// If <c>false</c>, this will throw an exception instead.</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="from"/>
        /// is not contained in the implementation graph or <paramref name="to"/> is not contained in the
        /// architecture graph.</exception>
        /// <exception cref="NotExplicitlyMappedException">When <paramref name="ignoreUnmapped"/> is false
        /// and the given node is not explicitly mapped.</exception>
        public void RemoveFromMapping(Node from, bool ignoreUnmapped = false)
        {
            AssertOrThrow(from.IsInImplementation() && ContainsNode(from),
                          () => new NotInSubgraphException(Implementation, from));

            // The mapsTo edge in between from mapFrom to mapTo. There should be exactly one such edge.
            Edge mapsToEdge = from.Outgoings.SingleOrDefault(x => x.IsInMapping());
            if (mapsToEdge == null && !ignoreUnmapped)
            {
                throw new NotExplicitlyMappedException(from);
            }

            // Deletes the unique Maps_To edge.
            RemoveMapsTo(mapsToEdge);
        }

        /// <summary>
        /// Adds given <paramref name="node"/> to architecture graph.
        ///
        /// Precondition: <paramref name="node"/> must not be contained in the reflexion graph.
        /// Postcondition: <paramref name="node"/> is contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the architecture graph</param>
        /// <exception cref="AlreadyContainedException">When the given <paramref name="node"/> is already
        /// present in the reflexion graph.</exception>
        public void AddToArchitecture(Node node)
        {
            AssertOrThrow(!ContainsNode(node), () => new AlreadyContainedException(node));
            node.SetInArchitecture();
            base.AddNode(node);
            // No reflexion data has to be updated, as adding an unmapped and unconnected node has no effect.
        }

        /// <summary>
        /// Removes given <paramref name="node"/> from architecture graph.
        /// Any connected edges are incrementally removed from the graph as well.
        ///
        /// If <paramref name="orphansBecomeRoots"/> is true, the children of <paramref name="node"/>
        /// become root nodes. Otherwise they become children of the parent of <paramref name="node"/>
        /// if there is a parent.
        /// 
        /// Precondition: <paramref name="node"/> must be contained in the architecture graph.
        /// Postcondition: <paramref name="node"/> is no longer contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from the architecture graph</param>
        /// <param name="orphansBecomeRoots">if true, the children of <paramref name="node"/> become root nodes;
        /// otherwise they become children of the parent of <paramref name="node"/> (if any)</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="node"/>
        /// is not contained in the architecture graph.</exception>
        public void RemoveFromArchitecture(Node node, bool orphansBecomeRoots = false)
        {
            AssertOrThrow(node.IsInArchitecture() && ContainsNode(node),
                          () => new NotInSubgraphException(Architecture, node));
            foreach (Edge connected in node.Incomings.Union(node.Outgoings).Where(x => !x.IsInArchitecture() || IsSpecified(x)).ToList())
            {
                RemoveEdge(connected);
            }

            base.RemoveNode(node, orphansBecomeRoots);
        }

        /// <summary>
        /// Adds given <paramref name="node"/> to implementation graph.
        /// Precondition: <paramref name="node"/> must not be contained in the reflexion graph.
        /// Postcondition: <paramref name="node"/> is contained in the implementation graph; all observers are
        /// informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the implementation graph</param>
        /// <exception cref="AlreadyContainedException">When the given <paramref name="node"/> is already
        /// present in the reflexion graph.</exception>
        public void AddToImplementation(Node node)
        {
            AssertOrThrow(!ContainsNode(node), () => new AlreadyContainedException(node));
            node.SetInImplementation();
            base.AddNode(node);
            // No reflexion data has to be updated, as adding an unmapped and unconnected node has no effect.
        }

        /// <summary>
        /// Removes the given <paramref name="node"/> from the implementation graph (and all its incoming and
        /// outgoing edges).
        ///
        /// If <paramref name="orphansBecomeRoots"/> is true, the children of <paramref name="node"/>
        /// become root nodes. Otherwise they become children of the parent of <paramref name="node"/>
        /// if there is a parent.
        /// 
        /// Precondition: <paramref name="node"/> must be contained in the implementation graph.
        /// Postcondition: <paramref name="node"/> is no longer contained in the implementation graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from the implementation graph</param>
        /// <param name="orphansBecomeRoots">if true, the children of <paramref name="node"/> become root nodes;
        /// otherwise they become children of the parent of <paramref name="node"/> (if any)</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="node"/>
        /// is not contained in the implementation graph.</exception>
        public void RemoveFromImplementation(Node node, bool orphansBecomeRoots = false)
        {
            AssertOrThrow(node.IsInImplementation() && ContainsNode(node),
                          () => new NotInSubgraphException(Implementation, node));
            foreach (Edge connected in node.Incomings.Union(node.Outgoings).ToList())
            {
                RemoveEdge(connected);
            }

            base.RemoveNode(node, orphansBecomeRoots);
        }

        /// <summary>
        /// Adds given <paramref name="child"/> as a direct descendant of given <paramref name="parent"/>
        /// in the node hierarchy of the implementation graph.
        /// Precondition: <paramref name="child"/> and <paramref name="parent"/> must be contained in the
        /// implementation graph; <paramref name="child"/> has no current parent.
        /// Postcondition: <paramref name="parent"/> is a parent of <paramref name="child"/> in the
        /// implementation graph and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <param name="parent">parent node</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="child"/> or <paramref name="parent"/>
        /// is not contained in the implementation graph.</exception>
        /// <exception cref="CyclicHierarchyException">When adding <paramref name="child"/> as a child of
        /// <paramref name="parent"/> would result in cycles in the part-of hierarchy.</exception>
        /// <exception cref="NotAnOrphanException">When the given <paramref name="child"/>
        /// already has a parent.</exception>
        public void AddChildInImplementation(Node child, Node parent)
        {
            // TODO: Check for cycles in hierarchy (currently only first level is checked)
            AssertOrThrow(!child.Children().Contains(parent), () => new CyclicHierarchyException());
            AssertOrThrow(child.Parent == null, () => new NotAnOrphanException(child));
            AssertOrThrow(child.IsInImplementation() && ContainsNode(child),
                          () => new NotInSubgraphException(Implementation, child));
            AssertOrThrow(parent.IsInImplementation() && ContainsNode(parent),
                          () => new NotInSubgraphException(Implementation, parent));

            parent.AddChild(child);
            if (!IsExplicitlyMapped(child))
            {
                // An implicit mapping will only be created if child wasn't already explicitly mapped.
                Node target = MapsTo(parent);
                if (target != null)
                {
                    // All non-mapped children of child will now be implicitly mapped onto target.
                    List<Node> subtree = MappedSubtree(child);
                    ChangeMap(subtree, target);
                    // We'll also have to handle the resulting new propagated dependencies, of course.
                    Map(subtree, target);
                }
            }
        }

        /// <summary>
        /// Removes given <paramref name="child"/> from its parent in the node hierarchy of
        /// the implementation graph.
        /// Precondition: <paramref name="child"/> and parent must be contained in the implementation graph;
        ///    child has a parent.
        /// Postcondition: <paramref name="child"/> has no longer a parent in the implementation graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="child"/>
        /// is not contained in the implementation graph.</exception>
        /// <exception cref="IsAnOrphanException">When <paramref name="child"/> has no parent from which it could
        /// be "unparented".</exception>
        public void UnparentInImplementation(Node child)
        {
            Node parent = child.Parent;
            AssertOrThrow(parent != null, () => new IsAnOrphanException(parent));
            AssertOrThrow(child.IsInImplementation() && ContainsNode(child),
                          () => new NotInSubgraphException(Implementation, child));

            Node formerTarget = MapsTo(child);
            child.Reparent(null);
            if (formerTarget != null && !IsExplicitlyMapped(child))
            {
                // If child was implicitly mapped, this was due to parent, which means we now 
                // have to revert that effect on child and its subtree.
                List<Node> subtree = MappedSubtree(child);
                Unmap(subtree, formerTarget);
                ChangeMap(subtree, null);
            }
        }

        /// <summary>
        /// Adds given <paramref name="child"/> as a direct descendant of given <paramref name="parent"/>
        /// in the node hierarchy of the architecture graph.
        /// Preconditions:
        /// - <paramref name="child"/> and <paramref name="parent"/> must be contained in the
        ///   architecture graph; <paramref name="child"/> has no current parent.
        /// - No redundant specified dependencies must come into existence when the subtree is connected.
        /// Postcondition: <paramref name="parent"/> is a parent of <paramref name="child"/> in the
        /// architecture graph and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <param name="parent">parent node</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="child"/> or <paramref name="parent"/>
        /// is not contained in the architecture graph.</exception>
        /// <exception cref="CyclicHierarchyException">When adding <paramref name="child"/> as a child of
        /// <paramref name="parent"/> would result in cycles in the part-of hierarchy.</exception>
        /// <exception cref="NotAnOrphanException">When the given <paramref name="child"/>
        /// already has a parent.</exception>
        /// <exception cref="RedundantSpecifiedEdgeException">When redundant specified dependencies would
        /// come into existence once the subtree was connected.</exception>
        public void AddChildInArchitecture(Node child, Node parent)
        {
            AssertOrThrow(child.IsInArchitecture() && ContainsNode(child),
                          () => new NotInSubgraphException(Architecture, child));
            AssertOrThrow(parent.IsInArchitecture() && ContainsNode(parent),
                          () => new NotInSubgraphException(Architecture, parent));
            AssertOrThrow(child.Parent == null, () => new NotAnOrphanException(child));
            ISet<Node> childDescendants = new HashSet<Node>(child.PostOrderDescendants());
            // TODO: Proper cycle checking?
            AssertOrThrow(!childDescendants.Contains(parent), () => new CyclicHierarchyException());

            // Check that no redundant specified dependencies come into existence when subtree is connected.
            // There are two possibilities for this to happen: Either the subtree rooted by `child` contains
            // an outgoing edge, or an incoming edge, that is made redundant by the `parent` it is attached to
            // (or one of its ascendants).This is why we iterate through the parent's parents,
            // which will also include the parent itself.
            foreach (Node ascendant in parent.Ascendants())
            {
                foreach (Edge outgoing in ascendant.Outgoings)
                {
                    // We needn't check the "supertree", as we are already iterating over ascendants, hence
                    // we are setting that parameter to empty collections. 
                    // Since the only change in this operation will be the additional subtree rooted by `child`,
                    // we only need to compare this outgoing edge by that subtree.
                    AssertNotRedundant(outgoing.Source, outgoing.Target, outgoing.Type,
                                       fromSupertree: new Node[] { }, toSupertree: new HashSet<Node>(),
                                       fromSubtree: childDescendants);
                }

                foreach (Edge incoming in ascendant.Incomings)
                {
                    // We needn't check the "supertree", as we are already iterating over ascendants, hence
                    // we are setting that parameter to empty collections. 
                    // Since the only change in this operation will be the additional subtree rooted by `child`,
                    // we only need to compare this outgoing edge by that subtree.
                    AssertNotRedundant(incoming.Source, incoming.Target, incoming.Type,
                                       fromSupertree: new Node[] { }, toSupertree: new HashSet<Node>(),
                                       toSubtree: childDescendants);
                }
            }

            PartitionedDependencies divergent = DivergentRefsInSubtree(child);
            // New relationship needs to be present for lifting, so we'll add it first
            parent.AddChild(child);

            foreach (Edge edge in divergent.OutgoingCross)
            {
                if (Lift(parent, edge.Target, edge.Type, GetCounter(edge), out _))
                {
                    Transition(edge, edge.State(), State.Allowed);
                }
            }

            foreach (Edge edge in divergent.IncomingCross)
            {
                if (Lift(edge.Source, parent, edge.Type, GetCounter(edge), out _))
                {
                    Transition(edge, edge.State(), State.Allowed);
                }
            }

            foreach (Edge edge in divergent.Inner)
            {
                if (Lift(edge.Source, edge.Target, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, edge.State(), State.Allowed);
                }
            }
        }

        /// <summary>
        /// Removes given <paramref name="child"/> from its parent in the node hierarchy of
        /// the architecture graph.
        /// Precondition: <paramref name="child"/> and parent must be contained in the architecture graph;
        ///    child has a parent.
        /// Postcondition: <paramref name="child"/> has no longer a parent in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <exception cref="NotInSubgraphException">When <paramref name="child"/>
        /// is not contained in the architecture graph.</exception>
        /// <exception cref="IsAnOrphanException">When <paramref name="child"/> has no parent from which it could
        /// be "unparented".</exception>
        public void UnparentInArchitecture(Node child)
        {
            Node parent = child.Parent;
            AssertOrThrow(parent != null, () => new IsAnOrphanException(child));
            AssertOrThrow(child.IsInArchitecture() && ContainsNode(child),
                          () => new NotInSubgraphException(Architecture, child));

            PartitionedDependencies allowed = AllowedRefsInSubtree(child);
            foreach (Edge edge in allowed.OutgoingCross)
            {
                if (Lift(parent, edge.Target, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, edge.State(), State.Divergent);
                }
            }

            foreach (Edge edge in allowed.IncomingCross)
            {
                if (Lift(edge.Source, parent, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, edge.State(), State.Divergent);
                }
            }

            foreach (Edge edge in allowed.Inner)
            {
                if (Lift(parent, edge.Target, edge.Type, -GetCounter(edge), out _)
                    || Lift(edge.Source, parent, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, edge.State(), State.Divergent);
                }
            }

            child.Reparent(null);
        }
        
        #region Helper

        /// <summary>
        /// A set of dependencies partitioned into outgoing cross, incoming cross, and inner dependencies
        /// in reference to a subtree (not contained in this class).
        /// </summary>
        private class PartitionedDependencies
        {
            /// <summary>
            /// Set of dependencies whose source is in the subtree, but whose target is outside of the subtree.
            /// </summary>
            [NotNull]
            public readonly ISet<Edge> OutgoingCross;

            /// <summary>
            /// Set of dependencies whose source is outside of the subtree, but whose target is in the subtree.
            /// </summary>
            [NotNull]
            public readonly ISet<Edge> IncomingCross;

            /// <summary>
            /// Set of dependencies whose source is in the subtree and whose target is also in the subtree.
            /// </summary>
            [NotNull]
            public readonly ISet<Edge> Inner;

            public PartitionedDependencies([NotNull] ISet<Edge> outgoingCross, [NotNull] ISet<Edge> incomingCross, [NotNull] ISet<Edge> inner)
            {
                OutgoingCross = outgoingCross ?? throw new ArgumentNullException(nameof(outgoingCross));
                IncomingCross = incomingCross ?? throw new ArgumentNullException(nameof(incomingCross));
                Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }
        }

        /// <summary>
        /// Returns the propagated dependencies of the subtree rooted by <paramref name="root"/> which fulfill
        /// the given <paramref name="predicate"/> as a partitioned set.
        /// The returned dependencies will be partitioned into three kinds (see <see cref="PartitionedDependencies"/>).
        /// </summary>
        /// <param name="root">Node whose subtree's propagated dependencies shall be returned</param>
        /// <param name="predicate">The predicate a dependency must fulfill to be included</param>
        /// <returns>Propagated dependencies of the subtree rooted by <paramref name="root"/> which fulfill
        /// the given <paramref name="predicate"/></returns>
        private static PartitionedDependencies RefsInSubtree(Node root, Predicate<Edge> predicate)
        {
            AssertOrThrow(root.IsInArchitecture(), () => new NotInSubgraphException(Architecture, root));
            IList<Node> descendants = root.PostOrderDescendants();
            (HashSet<Edge> oc, HashSet<Edge> ic, HashSet<Edge> i) = (new HashSet<Edge>(), new HashSet<Edge>(), new HashSet<Edge>());
            foreach (Node descendant in descendants)
            {
                ILookup<bool, Edge> outgoings = descendant.Outgoings.Where(e => e.IsInArchitecture() && predicate(e) && !IsSpecified(e))
                                                          .ToLookup(e => descendants.Contains(e.Target));
                oc.UnionWith(outgoings[false]);
                i.UnionWith(outgoings[true]);
                ic.UnionWith(descendant.Incomings.Where(e => e.IsInArchitecture() && predicate(e) && !IsSpecified(e) && !descendants.Contains(e.Source)));
            }

            return new PartitionedDependencies(oc, ic, i);
        }
        
        /// <summary>
        /// Returns all allowed propagated dependencies of the subtree rooted by <paramref name="root"/>.
        /// See <see cref="RefsInSubtree"/> for details.
        /// </summary>
        /// <param name="root">Node whose subtree's allowed propagated dependencies shall be returned</param>
        /// <returns>All allowed propagated dependencies of the subtree rooted by <paramref name="root"/>.</returns>
        private static PartitionedDependencies AllowedRefsInSubtree(Node root) => RefsInSubtree(root, e => e.State() == State.Allowed);

        /// <summary>
        /// Returns all divergent propagated dependencies of the subtree rooted by <paramref name="root"/>.
        /// See <see cref="RefsInSubtree"/> for details.
        /// </summary>
        /// <param name="root">Node whose subtree's divergent propagated dependencies shall be returned</param>
        /// <returns>All divergent propagated dependencies of the subtree rooted by <paramref name="root"/>.</returns>
        private static PartitionedDependencies DivergentRefsInSubtree(Node root) => RefsInSubtree(root, e => e.State() == State.Divergent);

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
        private void RemoveMapsTo(Edge mapsTo)
        {
            AssertOrThrow(IsExplicitlyMapped(mapsTo.Source) && explicitMapsToTable.Remove(mapsTo.Source.ID),
                          () => new NotExplicitlyMappedException(mapsTo.Source));

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
                    // However, if newTarget doesn't exist, we should still unmap subtree.
                }
                ChangeMap(subtree, newTarget);

                if (newTarget != null)
                {
                    Map(subtree, newTarget);
                }
            }

            base.RemoveEdge(mapsTo);
        }

        /// <summary>
        /// Returns propagated dependency in architecture graph matching the type of
        /// the <paramref name="implementationEdge"/> exactly if one exists;
        /// returns null if none can be found.
        /// Precondition: <paramref name="implementationEdge"/> is an implementation dependency.
        /// Postcondition: resulting edge is in architecture or null.
        /// </summary>
        /// <param name="implementationEdge">edge in implementation graph whose propagated edge shall be returned
        /// (if it exists)</param>
        /// <returns>the edge in the architecture graph propagated from <paramref name="implementationEdge"/>
        /// with given type; null if there is no such edge</returns>
        public Edge GetPropagatedDependency(Edge implementationEdge)
        {
            AssertOrThrow(implementationEdge.IsInImplementation(),
                          () => new NotInSubgraphException(Implementation, implementationEdge));
            Node archSource = MapsTo(implementationEdge.Source);
            Node archTarget = MapsTo(implementationEdge.Target);
            if (archSource == null || archTarget == null)
            {
                return null;
            }

            return GetPropagatedDependency(archSource, archTarget, implementationEdge.Type);
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
        private void Unmap(IEnumerable<Node> subtree, Node oldTarget)
        {
            AssertOrThrow(oldTarget.IsInArchitecture(), () => new NotInSubgraphException(Architecture, oldTarget));
            HandleMappedSubtree(subtree, oldTarget, DecreaseAndLift);
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
            AssertOrThrow(from.IsInImplementation(), () => new NotInSubgraphException(Implementation, from));
            AssertOrThrow(to.IsInArchitecture(), () => new NotInSubgraphException(Architecture, to));
            // add Maps_To edge to Mapping
            Edge mapsTo = new Edge(from, to, MapsToType);
            base.AddEdge(mapsTo);
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
            AssertOrThrow(target == null || target.IsInArchitecture(),
                          () => new NotInSubgraphException(Architecture, target));
            if (target == null)
            {
                foreach (Node node in subtree)
                {
                    AssertOrThrow(node.IsInImplementation(), () => new NotInSubgraphException(Implementation, node));
                    implicitMapsToTable.Remove(node.ID);
                }
            }
            else
            {
                foreach (Node node in subtree)
                {
                    AssertOrThrow(node.IsInImplementation(), () => new NotInSubgraphException(Implementation, node));
                    implicitMapsToTable[node.ID] = target;
                }
            }
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
        private void Map(IReadOnlyCollection<Node> subtree, Node newTarget)
        {
            AssertOrThrow(newTarget.IsInArchitecture(), () => new NotInSubgraphException(Architecture, newTarget));
            Node notInImpl = subtree.FirstOrDefault(x => !x.IsInImplementation());
            AssertOrThrow(notInImpl == null, () => new NotInSubgraphException(Implementation, notInImpl));
            AssertOrThrow(subtree.All(x => MapsTo(x) == newTarget),
                          () => new CorruptStateException("Mapping failed: " +
                                                          $"nodes in subtree were not all mapped to {newTarget}!"));
            HandleMappedSubtree(subtree, newTarget, IncreaseAndLift);
        }

        /// <summary>
        /// Checks whether a specified dependency between <paramref name="from"/> and <paramref name="to"/>
        /// (which may or may not exist) would be redundant—if so, throws an exception.
        ///
        /// An edge e1 is redundant with another edge e2 if e1 has the same type as e2 and if either e1 is a part of e2
        /// or e2 is a part of e1. This means that we have to check all descendants of
        /// <paramref name="from"/> and look for edges which connect to descendants of <paramref name="to"/>,
        /// as well as do this for all parents of <paramref name="from"/>.
        ///
        /// Pre-conditions:
        /// - <paramref name="from"/> and <paramref name="to"/> are in the architecture graph.
        /// - There are not yet any redundant specified edges in the graph (excluding the supposed edge between
        /// <paramref name="from"/> and <paramref name="to"/>).
        /// </summary>
        /// <param name="from">Source of the supposed edge</param>
        /// <param name="to">Target of the supposed edge</param>
        /// <param name="type">Type of the supposed edge</param>
        /// <param name="fromSubtree">Subtree (descendants) of <paramref name="from"/>—if not given,
        /// we'll simply enumerate all descendants of <paramref name="from"/> ourselves</param>
        /// <param name="fromSupertree">Supertree (ascendants) of <paramref name="from"/>—if not given,
        /// we'll simply enumerate all ascendants of <paramref name="from"/> ourselves</param>
        /// <param name="toSubtree">Subtree (descendants) of <paramref name="to"/>—if not given,
        /// we'll simply enumerate all descendants of <paramref name="to"/> ourselves</param>
        /// <param name="toSupertree">Supertree (ascendants) of <paramref name="to"/>—if not given,
        /// we'll simply enumerate all ascendants of <paramref name="to"/> ourselves</param>
        /// <exception cref="RedundantSpecifiedEdgeException">If the edge between <paramref name="from"/>
        /// and <paramref name="to"/> is redundant in relation to the given sub- and supertrees.</exception>
        private static void AssertNotRedundant(Node from, Node to, string type,
                                               IEnumerable<Node> fromSubtree = null, IEnumerable<Node> fromSupertree = null,
                                               ISet<Node> toSubtree = null, ISet<Node> toSupertree = null)
        {
            fromSubtree ??= from.PostOrderDescendants();
            fromSupertree ??= from.Ascendants();
            // We use HashSets here due to O(1) `contains`, which is the only method we call on these.
            toSupertree ??= new HashSet<Node>(to.Ascendants());
            toSubtree ??= new HashSet<Node>(to.PostOrderDescendants());

            // TODO: Once a true type hierarchy exists, this needs to be updated
            Func<Edge, bool> IsRedundantIn(ICollection<Node> targets) => edge => IsSpecified(edge) && edge.HasSupertypeOf(type) && targets.Contains(edge.Target);

            Edge redundantSuper = fromSupertree.SelectMany(x => x.Outgoings).FirstOrDefault(IsRedundantIn(toSupertree));
            AssertOrThrow(redundantSuper == null,
                          () => new RedundantSpecifiedEdgeException(redundantSuper, new Edge(from, to, type)));
            Edge redundantSub = fromSubtree.SelectMany(x => x.Outgoings).FirstOrDefault(IsRedundantIn(toSubtree));
            AssertOrThrow(redundantSub == null,
                          () => new RedundantSpecifiedEdgeException(redundantSub, new Edge(from, to, type)));
        }

        /// <summary>
        /// Throws the Exception generated by <paramref name="exception"/>
        /// iff <paramref name="condition"/> evaluates to false.
        ///
        /// Note: The JIT compiler will try to inline this, making the cost of calling this method very low.
        /// </summary>
        /// <param name="condition">The condition that shall be evaluated</param>
        /// <param name="exception">Method returning the exception that shall be thrown if the
        /// <paramref name="condition"/> evaluates to false</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertOrThrow(bool condition, Func<Exception> exception)
        {
            if (!condition)
            {
                throw exception();
            }
        }

        #endregion
    }
}