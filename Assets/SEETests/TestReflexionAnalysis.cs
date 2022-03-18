using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace SEE.Tools.Architecture
{
    /// <summary>
    /// Shared code of unit tests for ReflexionAnalysis.
    /// </summary>
    internal class TestReflexionAnalysis : Observer
    {
        // TODO: Test types as well
        protected const string call = "call";
        protected Graph fullGraph;
        protected Reflexion reflexion;
        protected SEELogger logger = new SEELogger();

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        protected HashSet<string> HierarchicalEdges;

        protected const string Enclosing = "Enclosing";

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        protected static HashSet<string> HierarchicalEdgeTypes()
        {
            HashSet<string> result = new HashSet<string>
            {
                Enclosing,
                "Belongs_To",
                "Part_Of",
                "Defined_In"
            };
            return result;
        }

        /// <summary>
        /// List of edges changed for a single reflexion-analysis run.
        /// </summary>
        protected List<EdgeChange> edgeChanges = new List<EdgeChange>();

        /// <summary>
        /// List of edges propagated from the implementation onto the architecture for
        /// a single reflexion-analysis run.
        /// </summary>
        protected List<PropagatedEdgeAdded> propagatedEdgesAdded = new List<PropagatedEdgeAdded>();
        /// <summary>
        /// List of propagated dependency edges removed from the reflexion result.
        /// </summary>
        protected List<PropagatedEdgeRemoved> propagatedEdgesRemoved = new List<PropagatedEdgeRemoved>();

        /// <summary>
        /// List of Maps_To edges added to the mapping.
        /// </summary>
        protected List<MapsToEdgeAdded> mapsToEdgesAdded = new List<MapsToEdgeAdded>();
        /// <summary>
        /// List of Maps_To edges removed from the mapping.
        /// </summary>
        protected List<MapsToEdgeRemoved> mapsToEdgesRemoved = new List<MapsToEdgeRemoved>();
        
        /// <summary>
        /// List of Implementation edges added to the mapping.
        /// </summary>
        protected List<ImplementationEdgeAdded> implementationEdgesAdded = new List<ImplementationEdgeAdded>();
        /// <summary>
        /// List of Implementation edges removed from the mapping.
        /// </summary>
        protected List<ImplementationEdgeRemoved> implementationEdgesRemoved = new List<ImplementationEdgeRemoved>();

        /// <summary>
        /// Re-sets the event caches edgeChanges, propagatedEdges, and removedEdges to
        /// to their initial value (empty).
        /// </summary>
        protected void ResetEvents()
        {
            edgeChanges = new List<EdgeChange>();
            propagatedEdgesAdded = new List<PropagatedEdgeAdded>();
            propagatedEdgesRemoved = new List<PropagatedEdgeRemoved>();
            mapsToEdgesAdded = new List<MapsToEdgeAdded>();
            mapsToEdgesRemoved = new List<MapsToEdgeRemoved>();
            implementationEdgesAdded = new List<ImplementationEdgeAdded>();
            implementationEdgesRemoved = new List<ImplementationEdgeRemoved>();
        }

        /// <summary>
        /// True if edgeChanges has an edge from source to target with given edgeType whose new state is the
        /// given state.
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private static bool HasNewState(IEnumerable<EdgeChange> edgeChanges, Node source, Node target, string edgeType, State state)
        {
            return edgeChanges.Any(e => e.Edge.Source == source && e.Edge.Target == target && e.Edge.Type == edgeType && e.NewState == state);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.convergent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        protected static bool IsConvergent(IEnumerable<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.Convergent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.allowed).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        protected static bool IsAllowed(IEnumerable<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.Allowed);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.absent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        protected static bool IsAbsent(IEnumerable<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.Absent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.implicitly_allowed).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        protected static bool IsImplicitlyAllowed(IEnumerable<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.ImplicitlyAllowed);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.allowed_absent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        protected static bool IsAllowedAbsent(IEnumerable<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.AllowedAbsent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.divergent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        protected static bool IsDivergent(IEnumerable<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.Divergent);
        }

        /// <summary>
        /// Returns true if an edge from <paramref name="from"/> to <paramref name="to"/> with given <paramref name="edgeType"/>
        /// has been propagated to the architecture graph.
        /// </summary>
        /// <param name="from">source of the propagated edge</param>
        /// <param name="to">target of the propagated edge</param>
        /// <param name="edgeType">type of the propagated edge</param>
        /// <returns>true if such an edge is contained in propagatedEdgesAdded</returns>
        protected bool IsPropagated(Node from, Node to, string edgeType)
        {
            return IsContained(from, to, edgeType, propagatedEdgesAdded);
        }

        /// <summary>
        /// Returns true if an edge from <paramref name="from"/> to <paramref name="to"/> with given <paramref name="edgeType"/>
        /// has been unpropagated from the architecture graph.
        /// </summary>
        /// <param name="from">source of the unpropagated edge</param>
        /// <param name="to">target of the unpropagated edge</param>
        /// <param name="edgeType">type of the unpropagated edge</param>
        /// <returns>true if such an edge is contained in propagatedEdgesRemoved</returns>
        protected bool IsUnpropagated(Node from, Node to, string edgeType)
        {
            return IsContained(from, to, edgeType, propagatedEdgesRemoved);
        }

        /// <summary>
        /// Returns true if an edge from <paramref name="from"/> to <paramref name="to"/> with given <paramref name="edgeType"/>
        /// is contained in <paramref name="propagatedEdges"/>.
        /// </summary>
        /// <param name="from">source of the propagated edge</param>
        /// <param name="to">target of the propagated edge</param>
        /// <param name="edgeType">type of the propagated edge</param>
        /// <returns>true if such an edge is contained in <paramref name="propagatedEdges"/></returns>
        protected static bool IsContained(Node from, Node to, string edgeType, IEnumerable<PropagatedEdge> propagatedEdges)
        {
            return propagatedEdges.Any(edge => from.ID == edge.ThePropagatedEdge.Source.ID &&
                                               to.ID == edge.ThePropagatedEdge.Target.ID &&
                                               edgeType == edge.ThePropagatedEdge.Type);
        }
        
        protected static bool IsNotContained(Node from, Node to, string edgeType, IEnumerable<EdgeChange> edgeChanges)
        {
            return !edgeChanges.Any(edge => from.ID == edge.Edge.Source.ID && 
                                               to.ID == edge.Edge.Target.ID && 
                                               edgeType == edge.Edge.Type);
        }

        /// <summary>
        /// Dumps the results collected in mapsToEdges, edgeChanges, propagatedEdges, and removedEdges
        /// to standard output.
        /// </summary>
        protected void DumpEvents()
        {
            Debug.Log("MAPS_TO EDGES ADDED TO MAPPING\n");
            foreach (MapsToEdgeAdded e in mapsToEdgesAdded)
            {
                Debug.LogFormat("maps_to {0}\n", e.TheMapsToEdge);
            }
            Debug.Log("MAPS_TO EDGES REMOVED FROM MAPPING\n");
            foreach (MapsToEdgeRemoved e in mapsToEdgesRemoved)
            {
                Debug.LogFormat("maps_to {0}\n", e.TheMapsToEdge);
            }

            Debug.Log("DEPENDENCIES PROPAGATED TO ARCHITECTURE\n");
            foreach (PropagatedEdgeAdded e in propagatedEdgesAdded)
            {
                Debug.LogFormat("propagated {0}\n", e.ThePropagatedEdge);
            }
            Debug.Log("PROPAGATED DEPENDENCIES REMOVED FROM ARCHITECTURE\n");
            foreach (PropagatedEdgeRemoved e in propagatedEdgesRemoved)
            {
                Debug.LogFormat("removed {0}\n", e.ThePropagatedEdge);
            }

            Debug.Log("DEPENDENCIES CHANGED IN ARCHITECTURE\n");
            foreach (EdgeChange e in edgeChanges)
            {
                Debug.LogFormat("changed {0} from {1} to {2}\n", e.Edge, e.OldState, e.NewState);
            }
            
            Debug.Log("DEPENDENCIES ADDED IN IMPLEMENTATION");
            foreach (ImplementationEdgeAdded e in implementationEdgesAdded)
            {
                Debug.Log($"added {e}");
            }
            
            Debug.Log("DEPENDENCIES REMOVED IN IMPLEMENTATION");
            foreach (ImplementationEdgeRemoved e in implementationEdgesRemoved)
            {
                Debug.Log($"removed {e}");
            }
        }

        /// <summary>
        /// Saves the given graphs to disk in GXL format.
        /// </summary>
        /// <param name="impl">implementation graph</param>
        /// <param name="arch">architecture graph</param>
        /// <param name="map">mapping graph</param>
        protected static void Save(Graph impl, Graph arch, Graph map)
        {
            GraphWriter.Save("implementation.gxl", impl, Enclosing);
            GraphWriter.Save("architecture.gxl", arch, Enclosing);
            GraphWriter.Save("mapping.gxl", map, Enclosing);
        }

        /// <summary>
        /// Creates HierarchicalEdges and re-sets all event caches.
        /// </summary>
        [SetUp]
        protected virtual void Setup()
        {
            HierarchicalEdges = HierarchicalEdgeTypes();
            fullGraph = new Graph("DUMMYBASEPATH");
            ResetEvents();
        }

        /// <summary>
        /// Resets all attributes to null.
        /// </summary>
        [TearDown]
        protected virtual void Teardown()
        {
            fullGraph = null;
            reflexion = null;
            HierarchicalEdges = null;
            logger = null;
            edgeChanges = null;
            propagatedEdgesAdded = null;
            mapsToEdgesAdded = null;
            mapsToEdgesRemoved = null;
            propagatedEdgesRemoved = null;
            implementationEdgesAdded = null;
            implementationEdgesRemoved = null;
        }

        protected Node NewNode(bool inArchitecture, string linkname, string type = "Routine")
        {
            Node result = new Node
            {
                ID = linkname,
                SourceName = linkname,
                Type = type
            };
            if (inArchitecture) {
                result.SetInArchitecture();
            }
            else
            {
                result.SetInImplementation();
            }
            fullGraph.AddNode(result);
            return result;
        }

        protected Edge NewEdge(Node from, Node to, string type)
        {
            Edge result = new Edge(from, to, type);
            if (type == ReflexionGraphTools.MapsToType)
            {
                Assert.IsTrue(from.IsInImplementation());
                Assert.IsTrue(to.IsInArchitecture());
            }
            else if (from.IsInImplementation())
            {
                Assert.IsTrue(to.IsInImplementation());
                result.SetInImplementation();
            }
            else if (from.IsInArchitecture())
            {
                Assert.IsTrue(to.IsInArchitecture());
                result.SetInArchitecture();
            }

            fullGraph.AddEdge(result);
            return result;
        }

        /// <summary>
        /// Callback of reflexion analysis. Will be called by reflexion analysis on every
        /// state change. Collects the events in the respective change-event lists
        /// edgeChanges, propagatedEdges, removedEdges.
        /// </summary>
        /// <param name="changeEvent">the event that occurred</param>
        public virtual void Update(ChangeEvent changeEvent)
        {
            switch (changeEvent)
            {
                case EdgeChange @event: edgeChanges.Add(@event);
                    break;
                case PropagatedEdgeAdded added: propagatedEdgesAdded.Add(added);
                    break;
                case PropagatedEdgeRemoved @event: propagatedEdgesRemoved.Add(@event);
                    break;
                case MapsToEdgeAdded added: mapsToEdgesAdded.Add(added);
                    break;
                case MapsToEdgeRemoved @event: mapsToEdgesRemoved.Add(@event);
                    break;
                case ImplementationEdgeAdded @event: implementationEdgesAdded.Add(@event);
                    break;
                case ImplementationEdgeRemoved @event: implementationEdgesRemoved.Add(@event);
                    break;
                default: Debug.LogError($"UNHANDLED CALLBACK: {changeEvent}\n");
                    break;
            }
        }
    }
}