using System;
using System.Collections.Generic;
using System.Linq;
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
            Notify(new ImplementationEdgeAdded(edge));
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
            Notify(new ImplementationEdgeRemoved(edge));
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
            Notify(new ArchitectureEdgeAdded(edge));
            
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
            
            Notify(new ArchitectureEdgeRemoved(edge));
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
    }
}