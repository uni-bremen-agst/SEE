using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Net;
using UnityEngine.Assertions;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This partial class contains methods for the Incremental Reflexion Analysis.
    /// </summary>
    public partial class Reflexion
    {
        /// <summary>
        /// Adds an the given <paramref name="edge"/> to the implementation graph,
        /// adjusting the reflexion analysis incrementally.
        /// This will propagate and lift the new edge, thereby increasing the counter of the matching specified edge
        /// if it exists.
        /// 
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="edge"/> is not yet in the reflexion graph.</li>
        /// <li><paramref name="edge"/>'s connected nodes are contained in the implementation graph.</li>
        /// </ul>
        /// 
        /// Postcondition: <paramref name="edge"/> is contained in the implementation graph and the reflexion
        ///   data is updated; all observers are informed of the change by an <see cref="ImplementationEdgeAdded"/>
        ///   event.
        /// </summary>
        /// <param name="edge">the new edge</param>
        public void AddToImplementation(Edge edge)
        {
            Assert.IsTrue(!FullGraph.ContainsEdge(edge));
            Assert.IsTrue(edge.Source.IsInImplementation() && edge.Target.IsInImplementation());
            edge.SetInImplementation();
            FullGraph.AddEdge(edge);
            Notify(new EdgeEvent(edge, ChangeType.Addition, AffectedGraph.Implementation));
            PropagateAndLiftDependency(edge);
        }

        /// <summary>
        /// Removes an edge from the implementation graph, adjusting the reflexion analysis incrementally.
        /// This will reduce the counter attribute of a corresponding propagated edge, if it exists, and may
        /// cause previously convergent edges to now become absent.
        ///
        /// Precondition: <paramref name="edge"/> is an implementation edge contained in the reflexion graph.
        /// </summary>
        /// <param name="edge">The implementation edge to be removed from the graph.</param>
        public void DeleteFromImplementation(Edge edge)
        {
            Assert.IsTrue(edge.IsInImplementation());
            Assert.IsTrue(FullGraph.ContainsEdge(edge));
            Edge propagated = GetPropagatedDependency(edge);
            if (propagated != null)
            {
                if (GetState(propagated) != State.Divergent)
                {
                    // A convergence exists that must be handled (i.e. by decreasing the matching specified edge's counter).
                    if (!Lift(propagated.Source, propagated.Target, propagated.Type, -GetImplCounter(edge), out Edge _))
                    {
                        throw new InvalidOperationException($"Since this edge is {GetState(propagated)} and not "
                                                            + "Divergent, it must have a matching specified edge.");
                    }
                }

                ChangePropagatedDependency(propagated, -GetImplCounter(edge));
            }

            Notify(new EdgeEvent(edge, ChangeType.Removal, AffectedGraph.Implementation));
            FullGraph.RemoveEdge(edge);
        }

        /// <summary>
        /// Adds the given dependency <paramref name="edge"/> to the architecture graph. This edge will
        /// be considered as a specified dependency.
        /// Preconditions:
        /// <ul>
        /// <li><paramref name="edge"/> is not yet in the reflexion graph.</li>
        /// <li><paramref name="edge"/>'s connected nodes are contained in the architecture graph.</li>
        /// <li><paramref name="edge"/> represents a dependency.</li>
        /// </ul>
        /// Postcondition: <paramref name="edge"/> is contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the dependency edge to be added to the architecture graph</param>
        public void AddToArchitecture(Edge edge)
        {
            Assert.IsTrue(!FullGraph.ContainsEdge(edge));
            Assert.IsTrue(edge.Source.IsInArchitecture() && edge.Target.IsInArchitecture());
            edge.SetInArchitecture();
            FullGraph.AddEdge(edge);
            SetState(edge, State.Specified);
            Notify(new EdgeEvent(edge, ChangeType.Addition, AffectedGraph.Architecture));

            // We need to handle the propagated dependencies covered by this specified edge.

            // We don't care about order, but O(1) `Contains` is nice to have here, hence the Set
            ISet<Node> targetKids = new HashSet<Node>(edge.Target.PostOrderDescendants());

            bool IsCoveredEdge(Edge e) => !IsSpecified(e)
                                          && targetKids.Contains(e.Target)
                                          && e.HasSupertypeOf(edge.Type);

            IEnumerable<Edge> coveredEdges = edge.Source.PostOrderDescendants()
                                                 .SelectMany(x => x.Outgoings)
                                                 .Where(IsCoveredEdge);
            bool noneCovered = true;
            foreach (Edge coveredEdge in coveredEdges)
            {
                noneCovered = false;
                ChangeSpecifiedDependency(edge, GetImplCounter(coveredEdge));
                Transition(coveredEdge, GetState(coveredEdge), State.Allowed);
            }

            if (noneCovered)
            {
                // In this case, we need to set the state manually because `ChangeSpecifiedDependency` wasn't called.
                Transition(edge, State.Specified, State.Absent);
                SetCounter(edge, 0);
            }
        }

        /// <summary>
        /// Removes the given specified dependency <paramref name="edge"/> from the architecture.
        /// Precondition: <paramref name="edge"/> must be contained in the architecture graph
        ///   and must represent a specified dependency.
        /// Postcondition: <paramref name="edge"/> is no longer contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="edge">the specified dependency edge to be removed from the architecture graph</param>
        public void DeleteFromArchitecture(Edge edge)
        {
            Assert.IsTrue(FullGraph.ContainsEdge(edge));
            Assert.IsTrue(edge.IsInArchitecture() && IsSpecified(edge));

            if (GetState(edge) == State.Convergent)
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
                    Transition(coveredEdge, GetState(coveredEdge), State.Divergent);
                }
            }

            Notify(new EdgeEvent(edge, ChangeType.Removal, AffectedGraph.Architecture));
            FullGraph.RemoveEdge(edge);
        }


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
        /// Adds given <paramref name="node"/> to architecture graph.
        ///
        /// Precondition: <paramref name="node"/> must not be contained in the reflexion graph.
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

        /// <summary>
        /// Adds given node to the mapping graph.
        /// Precondition: node must not be contained in the mapping graph.
        /// Postcondition: node is contained in the mapping graph and the architecture
        //   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the mapping graph</param>
        public void AddToMapping(Node node)
        {
            throw new NotImplementedException(); // FIXME
        }

        /// <summary>
        /// Removes given node from mapping graph.
        /// Precondition: node must be contained in the mapping graph.
        /// Postcondition: node is no longer contained in the mapping graph and the architecture
        ///   graph is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">node to be removed from the mapping</param>
        public void DeleteFromMapping(Node node)
        {
            throw new NotImplementedException(); // FIXME
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
        public void AddChildInImplementation(Node child, Node parent)
        {
            // TODO: What if parent is a child of child?
            Assert.IsNull(child.Parent);
            Assert.IsTrue(child.IsInImplementation() && parent.IsInImplementation());
            Assert.IsTrue(FullGraph.ContainsNode(child));
            Assert.IsTrue(FullGraph.ContainsNode(parent));

            parent.AddChild(child);
            Notify(new HierarchyChangeEvent(parent, child, ChangeType.Addition, AffectedGraph.Implementation));
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
        public void UnparentInImplementation(Node child)
        {
            Node parent = child.Parent;
            Assert.IsNotNull(parent);
            Assert.IsTrue(child.IsInImplementation());
            Assert.IsTrue(FullGraph.ContainsNode(child));

            Node formerTarget = MapsTo(child);
            Notify(new HierarchyChangeEvent(parent, child, ChangeType.Removal, AffectedGraph.Implementation));
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
        /// Precondition: <paramref name="child"/> and <paramref name="parent"/> must be contained in the
        /// architecture graph; <paramref name="child"/> has no current parent.
        /// Postcondition: <paramref name="parent"/> is a parent of <paramref name="child"/> in the
        /// architecture graph and the reflexion data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="child">child node</param>
        /// <param name="parent">parent node</param>
        public void AddChildInArchitecture(Node child, Node parent)
        {
            Assert.IsTrue(child.IsInArchitecture());
            Assert.IsTrue(parent.IsInArchitecture());
            Assert.IsNull(child.Parent);
            Assert.IsTrue(FullGraph.ContainsNode(child));
            Assert.IsTrue(FullGraph.ContainsNode(parent));
            
            // TODO: Check that no redundant-specified dependencies come into existence when subtree is connected
            PartitionedDependencies divergent = DivergentRefsInSubtree(child);
            // New relationship needs to be present for lifting, so we'll add it first
            parent.AddChild(child);
            Notify(new HierarchyChangeEvent(parent, child, ChangeType.Addition, AffectedGraph.Architecture));
            
            foreach (Edge edge in divergent.OutgoingCross)
            {
                if (Lift(parent, edge.Target, edge.Type, GetCounter(edge), out _))
                {
                    Transition(edge, GetState(edge), State.Allowed);
                }
            }

            foreach (Edge edge in divergent.IncomingCross)
            {
                if (Lift(edge.Source, parent, edge.Type, GetCounter(edge), out _))
                {
                    Transition(edge, GetState(edge), State.Allowed);
                }
            }

            foreach (Edge edge in divergent.Inner)
            {
                if (Lift(edge.Source, edge.Target, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, GetState(edge), State.Allowed);
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
        public void UnparentInArchitecture(Node child)
        {
            Node parent = child.Parent;
            Assert.IsNotNull(parent);
            Assert.IsTrue(child.IsInArchitecture());
            Assert.IsTrue(FullGraph.ContainsNode(child));

            PartitionedDependencies allowed = AllowedRefsInSubtree(child);
            foreach (Edge edge in allowed.OutgoingCross)
            {
                if (Lift(parent, edge.Target, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, GetState(edge), State.Divergent);
                }
            }

            foreach (Edge edge in allowed.IncomingCross)
            {
                if (Lift(edge.Source, parent, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, GetState(edge), State.Divergent);
                }
            }

            foreach (Edge edge in allowed.Inner)
            {
                if (Lift(parent, edge.Target, edge.Type, -GetCounter(edge), out _) 
                    || Lift(edge.Source, parent, edge.Type, -GetCounter(edge), out _))
                {
                    Transition(edge, GetState(edge), State.Divergent);
                }
            }
            
            Notify(new HierarchyChangeEvent(parent, child, ChangeType.Removal, AffectedGraph.Architecture));
            child.Reparent(null);
        }

        #region Helper

        // TODO: Move other helper methods here

        private class PartitionedDependencies
        {
            [NotNull]
            public readonly ISet<Edge> OutgoingCross;

            [NotNull]
            public readonly ISet<Edge> IncomingCross;

            [NotNull]
            public readonly ISet<Edge> Inner;

            public PartitionedDependencies([NotNull] ISet<Edge> outgoingCross, [NotNull] ISet<Edge> incomingCross, [NotNull] ISet<Edge> inner)
            {
                OutgoingCross = outgoingCross ?? throw new ArgumentNullException(nameof(outgoingCross));
                IncomingCross = incomingCross ?? throw new ArgumentNullException(nameof(incomingCross));
                Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }
        }

        private static PartitionedDependencies RefsInSubtree(Node root, Predicate<Edge> predicate)
        {
            IList<Node> descendants = root.PostOrderDescendants();
            (HashSet<Edge> oc, HashSet<Edge> ic, HashSet<Edge> i) = (new HashSet<Edge>(), new HashSet<Edge>(), new HashSet<Edge>());
            foreach (Node descendant in descendants)
            {
                ILookup<bool, Edge> outgoings = descendant.Outgoings.Where(e => e.IsInArchitecture() && predicate(e) && !IsSpecified(e))
                                                          .ToLookup(e => descendants.Contains(e.Target));
                oc.UnionWith(outgoings[false]);
                i.UnionWith(outgoings[true]);
                ic.UnionWith(descendant.Incomings.Where(e => e.IsInArchitecture() && predicate(e) && !IsSpecified(e) && descendants.Contains(e.Source)));
            }

            return new PartitionedDependencies(oc, ic, i);
        }

        // TODO: What about ImplicitlyAllowed and AllowedAbsence?
        private static PartitionedDependencies AllowedRefsInSubtree(Node root) => RefsInSubtree(root, e => GetState(e) == State.Allowed);
        
        private static PartitionedDependencies DivergentRefsInSubtree(Node root) => RefsInSubtree(root, e => GetState(e) == State.Divergent);

        #endregion
    }
}