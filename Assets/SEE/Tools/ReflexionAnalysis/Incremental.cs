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
        /// Removes an edge from the implementation graph, adjusting the reflexion analysis incrementally.
        /// This will reduce the counter attribute of a corresponding propagated edge, if it exists, and may
        /// cause previously convergent edges to now become absent.
        ///
        /// Pre-condition: <paramref name="edge"/> is an implementation edge.
        /// </summary>
        /// <param name="edge">The implementation edge to be removed from the graph.</param>
        public void DeleteImplementationEdge(Edge edge)
        {
            Assert.IsTrue(edge.IsInImplementation());
            Edge propagated = GetPropagatedDependency(edge.Source, edge.Target, edge.Type);
            if (propagated != null)
            {
                if (GetState(propagated) != State.Divergent)
                {
                    // A convergence exists that must be handled (i.e. by decreasing the matching specified edge's counter).
                    if (!Lift(propagated.Source, propagated.Target, propagated.Type, -GetCounter(edge), out Edge _))
                    {
                        throw new InvalidOperationException($"Since this edge is {GetState(propagated)} and not "
                                                            + "Divergent, it must have a matching specified edge.");
                    }
                }
                ChangePropagatedDependency(propagated, -GetCounter(edge));
            }
            Notify(new ImplementationEdgeRemoved(edge));
            FullGraph.RemoveEdge(edge);
        }

        /// <summary>
        /// Adds an edge from <paramref name="from"/> to <paramref name="to"/> of type <paramref name="type"/>
        /// to the implementation graph, returning the new edge and adjusting the reflexion analysis incrementally.
        /// This will propagate and lift the new edge, thereby increasing the counter of the matching specified edge
        /// if it exists.
        /// 
        /// Pre-condition: <paramref name="from"/> and <paramref name="to"/> are contained in the implementation graph.
        /// </summary>
        /// <param name="from">Source of the new edge</param>
        /// <param name="to">Target of the new edge</param>
        /// <param name="type">Type of the new edge</param>
        public void AddImplementationEdge(Node from, Node to, string type)  // TODO: Support counter in the future
        {
            Assert.IsTrue(from.IsInImplementation() && to.IsInImplementation());
            Edge edge = new Edge(from, to, type);
            FullGraph.AddEdge(edge);
            Notify(new ImplementationEdgeAdded(edge));
            PropagateAndLiftDependency(edge);
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
    }
}