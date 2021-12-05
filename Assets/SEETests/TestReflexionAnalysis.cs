using NUnit.Framework;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Tools.Architecture
{
    /// <summary>
    /// Shared code of unit tests for ReflexionAnalysis.
    /// </summary>
    internal class TestReflexionAnalysis : Observer
    {
        protected const string call = "call";
        protected Graph impl;
        protected Graph arch;
        protected Graph mapping;
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
        protected static HashSet<string> Hierarchical_Edge_Types()
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
        protected List<PropagatedEdge> propagatedEdgesAdded = new List<PropagatedEdge>();
        /// <summary>
        /// List of propagated dependency edges removed from the reflexion result.
        /// </summary>
        protected List<PropagatedEdge> propagatedEdgesRemoved = new List<PropagatedEdge>();

        /// <summary>
        /// List of Maps_To edges added to the mapping.
        /// </summary>
        protected List<MapsToEdgeAdded> mapsToEdgesAdded = new List<MapsToEdgeAdded>();
        /// <summary>
        /// List of Maps_To edges removed from the mapping.
        /// </summary>
        protected List<MapsToEdgeRemoved> mapsToEdgesRemoved = new List<MapsToEdgeRemoved>();

        /// <summary>
        /// Re-sets the event chaches edgeChanges, propagatedEdges, and removedEdges to
        /// to their initial value (empty).
        /// </summary>
        protected void ResetEvents()
        {
            edgeChanges = new List<EdgeChange>();
            propagatedEdgesAdded = new List<PropagatedEdge>();
            propagatedEdgesRemoved = new List<PropagatedEdge>();
            mapsToEdgesAdded = new List<MapsToEdgeAdded>();
            mapsToEdgesRemoved = new List<MapsToEdgeRemoved>();
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
        private bool HasNewState(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType, State state)
        {
            foreach (EdgeChange e in edgeChanges)
            {
                if (e.edge.Source == source && e.edge.Target == target && e.edge.Type == edgeType && e.newState == state)
                {
                    return true;
                }
            }
            return false;
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
        protected bool IsConvergent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.convergent);
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
        protected bool IsAllowed(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.allowed);
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
        protected bool IsAbsent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.absent);
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
        protected bool IsImplicitlyAllowed(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.implicitly_allowed);
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
        protected bool IsAllowedAbsent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.allowed_absent);
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
        protected bool IsDivergent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.divergent);
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
        protected bool IsContained(Node from, Node to, string edgeType, List<PropagatedEdge> propagatedEdges)
        {
            foreach (PropagatedEdge edge in propagatedEdges)
            {
                if (from.ID == edge.propagatedEdge.Source.ID
                    && to.ID == edge.propagatedEdge.Target.ID
                    && edgeType == edge.propagatedEdge.Type)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Dumps the results collected in mapsToEdges, edgeChanges, propagatedEdges, and removedEdges
        /// to standard output.
        /// </summary>
        protected void DumpEvents()
        {
            Debug.Log("MAPS_TO EDGES ADDED TO MAPPING\n");
            foreach (MapsToEdge e in mapsToEdgesAdded)
            {
                Debug.LogFormat("maps_to {0}\n", e.mapsToEdge.ToString());
            }
            Debug.Log("MAPS_TO EDGES REMOVED FROM MAPPING\n");
            foreach (MapsToEdge e in mapsToEdgesRemoved)
            {
                Debug.LogFormat("maps_to {0}\n", e.mapsToEdge.ToString());
            }

            Debug.Log("DEPENDENCIES PROPAGATED TO ARCHITECTURE\n");
            foreach (PropagatedEdgeAdded e in propagatedEdgesAdded)
            {
                Debug.LogFormat("propagated {0}\n", e.propagatedEdge.ToString());
            }
            Debug.Log("PROPAGATED DEPENDENCIES REMOVED FROM ARCHITECTURE\n");
            foreach (PropagatedEdgeRemoved e in propagatedEdgesRemoved)
            {
                Debug.LogFormat("removed {0}\n", e.propagatedEdge.ToString());
            }

            Debug.Log("DEPENDENCIES CHANGED IN ARCHITECTURE\n");
            foreach (EdgeChange e in edgeChanges)
            {
                Debug.LogFormat("changed {0} from {1} to {2}\n", e.edge.ToString(), e.oldState, e.newState);
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
            HierarchicalEdges = Hierarchical_Edge_Types();
            ResetEvents();
        }

        /// <summary>
        /// Resets all attributes to null.
        /// </summary>
        [TearDown]
        protected virtual void Teardown()
        {
            impl = null;
            arch = null;
            mapping = null;
            reflexion = null;
            HierarchicalEdges = null;
            logger = null;
            edgeChanges = null;
            propagatedEdgesAdded = null;
            mapsToEdgesAdded = null;
            mapsToEdgesRemoved = null;
            propagatedEdgesRemoved = null;
        }

        protected static Node NewNode(Graph graph, string linkname, string type = "Routine")
        {
            Node result = new Node();
            result.ID = linkname;
            result.SourceName = linkname;
            result.Type = type;
            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Unique ID for edges.
        /// </summary>
        private static int edgeID = 1;

        protected static Edge NewEdge(Graph graph, Node from, Node to, string type)
        {
            edgeID++;
            Edge result = new Edge(from, to, type);
            graph.AddEdge(result);
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
            if (changeEvent is EdgeChange)
            {
                edgeChanges.Add(changeEvent as EdgeChange);
            }
            else if (changeEvent is PropagatedEdgeAdded)
            {
                propagatedEdgesAdded.Add(changeEvent as PropagatedEdgeAdded);
            }
            else if (changeEvent is PropagatedEdgeRemoved)
            {
                propagatedEdgesRemoved.Add(changeEvent as PropagatedEdgeRemoved);
            }
            else if (changeEvent is MapsToEdgeAdded)
            {
                mapsToEdgesAdded.Add(changeEvent as MapsToEdgeAdded);
            }
            else if (changeEvent is MapsToEdgeRemoved)
            {
                mapsToEdgesRemoved.Add(changeEvent as MapsToEdgeRemoved);
            }
            else
            {
                Debug.LogErrorFormat("UNHANDLED CALLBACK: {0}\n", changeEvent.ToString());
            }
        }
    }
}