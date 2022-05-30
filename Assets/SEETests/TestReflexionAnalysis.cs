using System;
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
        /// List of all changes for a single reflexion-analysis run or incremental change.
        /// Since <see cref="ChangeEvent"/> is abstract, this will contain concrete subtypes.
        /// </summary>
        protected List<ChangeEvent> changes = new List<ChangeEvent>();

        /// <summary>
        /// Re-sets the event cache in <see cref="changes"/> to its initial value (empty).
        /// </summary>
        protected void ResetEvents()
        {
            changes = new List<ChangeEvent>();
        }

        /// <summary>
        /// True if changes has an edge from source to target with given edgeType whose new state is the
        /// given state.
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool HasNewState(Node source, Node target, string edgeType, State state)
        {
            return changes.OfType<EdgeChange>().Any(e => e.Edge.Source == source && e.Edge.Target == target && e.Edge.Type == edgeType && e.NewState == state);
        }

        /// <summary>
        /// Equivalent to: HasNewState(source, target, edgeType, State.Convergent).
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <returns>true if such an edge exists</returns>
        protected bool IsConvergent(Node source, Node target, string edgeType)
        {
            return HasNewState(source, target, edgeType, State.Convergent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(source, target, edgeType, State.Allowed).
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <returns>true if such an edge exists</returns>
        protected bool IsAllowed(Node source, Node target, string edgeType)
        {
            return HasNewState(source, target, edgeType, State.Allowed);
        }

        /// <summary>
        /// Equivalent to: HasNewState(source, target, edgeType, State.Absent).
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <returns>true if such an edge exists</returns>
        protected bool IsAbsent(Node source, Node target, string edgeType)
        {
            return HasNewState(source, target, edgeType, State.Absent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(source, target, edgeType, State.ImplicitlyAllowed).
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <returns>true if such an edge exists</returns>
        protected bool IsImplicitlyAllowed(Node source, Node target, string edgeType)
        {
            return HasNewState(source, target, edgeType, State.ImplicitlyAllowed);
        }

        /// <summary>
        /// Equivalent to: HasNewState(source, target, edgeType, State.AllowedAbsent).
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <returns>true if such an edge exists</returns>
        protected bool IsAllowedAbsent(Node source, Node target, string edgeType)
        {
            return HasNewState(source, target, edgeType, State.AllowedAbsent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(source, target, edgeType, State.Divergent).
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <returns>true if such an edge exists</returns>
        protected bool IsDivergent(Node source, Node target, string edgeType)
        {
            return HasNewState(source, target, edgeType, State.Divergent);
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
            return IsContained(from, to, edgeType, ChangeType.Addition);
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
            return IsContained(from, to, edgeType, ChangeType.Removal);
        }

        /// <summary>
        /// Returns true if an edge from <paramref name="from"/> to <paramref name="to"/> with given <paramref name="edgeType"/>
        /// is contained in <paramref name="propagatedEdges"/>.
        /// </summary>
        /// <param name="from">source of the propagated edge</param>
        /// <param name="to">target of the propagated edge</param>
        /// <param name="edgeType">type of the propagated edge</param>
        /// <param name="change">change type for the propagated edge</param>
        /// <returns>true if such an edge is contained in <paramref name="propagatedEdges"/></returns>
        protected bool IsContained(Node from, Node to, string edgeType, ChangeType change)
        {
            return changes.OfType<PropagatedEdgeEvent>().Any(edge => from.ID == edge.PropagatedEdge.Source.ID &&
                                                                     to.ID == edge.PropagatedEdge.Target.ID &&
                                                                     edgeType == edge.PropagatedEdge.Type && change == edge.Change);
        }

        protected bool IsNotContained(Node from, Node to, string edgeType)
        {
            return !changes.OfType<EdgeChange>().Any(edge => from.ID == edge.Edge.Source.ID &&
                                                             to.ID == edge.Edge.Target.ID &&
                                                             edgeType == edge.Edge.Type);
        }

        protected void AssertEventCountEquals<T>(int expected, ChangeType? change = null, ReflexionSubgraph? affectedGraph = null) where T : ChangeEvent
        {
            Assert.AreEqual(expected, changes.OfType<T>().Count(x => (change == null || x.Change == change) && (affectedGraph == null || x.Affected == affectedGraph)));
        }

        /// <summary>
        /// Dumps the results collected in mapsToEdges, edgeChanges, propagatedEdges, and removedEdges
        /// to standard output.
        /// </summary>
        protected void DumpEvents()
        {
            Debug.Log("CHANGES IN REFLEXION\n\n");
            foreach (ChangeEvent e in changes)
            {
                Debug.Log(e);
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
            changes = null;
        }

        protected Node NewNode(bool inArchitecture, string linkname, string type = "Routine")
        {
            Node result = new Node
            {
                ID = linkname,
                SourceName = linkname,
                Type = type
            };
            if (inArchitecture)
            {
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
        /// state change. Collects the events in the change-event list.
        /// </summary>
        /// <param name="changeEvent">the event that occurred</param>
        public virtual void HandleChange(ChangeEvent changeEvent)
        {
            changes = changes.Incorporate(changeEvent).ToList();
        }
    }
}