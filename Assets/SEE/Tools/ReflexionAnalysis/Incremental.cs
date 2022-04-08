using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SEE.DataModel;
using SEE.DataModel.DG;
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
            Edge mapsToEdge = from.FromTo(to, MapsToType).SingleOrDefault(x => x.IsInMapping());
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
            Assert.IsTrue(!FullGraph.ContainsNode(node));
            node.SetInArchitecture();
            FullGraph.AddNode(node);
            Notify(new NodeChangeEvent(node, ChangeType.Addition, AffectedGraph.Architecture));
            // No reflexion data has to be updated, as adding an unmapped and unconnected node has no effect.
        }

        /// <summary>
        /// Removes given <paramref name="node"/> from architecture graph.
        /// Any connected edges are incrementally removed from the graph as well.
        /// Precondition: <paramref name="node"/> must be contained in the architecture graph.
        /// Postcondition: <paramref name="node"/> is no longer contained in the architecture graph and the reflexion
        ///   data is updated; all observers are informed of the change.
        /// </summary>
        /// <param name="node">the node to be removed from the architecture graph</param>
        public void DeleteFromArchitecture(Node node)
        {
            Assert.IsTrue(node.IsInArchitecture());
            Assert.IsTrue(FullGraph.ContainsNode(node));
            foreach (Edge connected in node.Incomings.Union(node.Outgoings))
            {
                Delete(connected);
            }
            Notify(new NodeChangeEvent(node, ChangeType.Removal, AffectedGraph.Architecture));
            // TODO: Should children become orphans or attached to parents?
            FullGraph.RemoveNode(node);
        }

        /// <summary>
        /// Adds given <paramref name="node"/> to implementation graph.
        /// Precondition: <paramref name="node"/> must not be contained in the reflexion graph.
        /// Postcondition: <paramref name="node"/> is contained in the implementation graph; all observers are
        /// informed of the change.
        /// </summary>
        /// <param name="node">the node to be added to the implementation graph</param>
        public void AddToImplementation(Node node)
        {
            Assert.IsTrue(!FullGraph.ContainsNode(node));
            node.SetInImplementation();
            FullGraph.AddNode(node);
            Notify(new NodeChangeEvent(node, ChangeType.Addition, AffectedGraph.Implementation));
            // No reflexion data has to be updated, as adding an unmapped and unconnected node has no effect.
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
            Assert.IsTrue(node.IsInImplementation());
            Assert.IsTrue(FullGraph.ContainsNode(node));
            foreach (Edge connected in node.Incomings.Union(node.Outgoings))
            {
                Delete(connected);
            }
            Notify(new NodeChangeEvent(node, ChangeType.Removal, AffectedGraph.Implementation));
            // TODO: Should children become orphans or attached to parents?
            FullGraph.RemoveNode(node);
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
            
            // TODO: Check that no redundant specified dependencies come into existence when subtree is connected
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

        #region Aggregator Methods

        public void Add(Node node)
        {
            Assert.IsFalse(FullGraph.ContainsNode(node));
            if (node.IsInArchitecture())
            {
                AddToArchitecture(node);
            } 
            else if (node.IsInImplementation())
            {
                AddToImplementation(node);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public void Delete(Node node)
        {
            Assert.IsTrue(FullGraph.ContainsNode(node));
            if (node.IsInArchitecture())
            {
                DeleteFromArchitecture(node);
            } 
            else if (node.IsInImplementation())
            {
                DeleteFromImplementation(node);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public void Add(Edge edge)
        {
            Assert.IsFalse(FullGraph.ContainsEdge(edge));
            throw new NotImplementedException();
            // TODO: Consistently accept (from, to) or edges directly
        }

        public void Delete(Edge edge)
        {
            Assert.IsTrue(FullGraph.ContainsEdge(edge));
            if (edge.IsInArchitecture())
            {
                DeleteFromArchitecture(edge);
            }
            else if (edge.IsInImplementation())
            {
                DeleteFromImplementation(edge);
            }
            else if (edge.IsInMapping())
            { 
                DeleteFromMapping(edge.Source, edge.Target);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region Helper

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
            Notify(new EdgeEvent(mapsTo, ChangeType.Removal, AffectedGraph.Mapping));
            FullGraph.RemoveEdge(mapsTo);
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
        private Edge GetPropagatedDependency(Edge implementationEdge)
        {
            Assert.IsTrue(implementationEdge.IsInImplementation());
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
        private void Unmap(List<Node> subtree, Node oldTarget)
        {
            Assert.IsTrue(oldTarget.IsInArchitecture());
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
            Assert.IsTrue(from.IsInImplementation());
            Assert.IsTrue(to.IsInArchitecture());
            // add Maps_To edge to Mapping
            Edge mapsTo = new Edge(from, to, MapsToType);
            FullGraph.AddEdge(mapsTo);
            Notify(new EdgeEvent(mapsTo, ChangeType.Addition, AffectedGraph.Mapping));
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
            Assert.IsTrue(subtree.All(x => x.IsInImplementation() && MapsTo(x) == newTarget));
            HandleMappedSubtree(subtree, newTarget, IncreaseAndLift);
        }

        #endregion
    }
}