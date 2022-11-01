using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
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
    [Explicit("Reflexion tests currently don't work, tracked in #481")]
    internal class TestReflexionAnalysis : IObserver<ChangeEvent>
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
        /// Equivalent to: HasNewState(source, target, edgeType, State.Unmapped).
        /// </summary>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <returns>true if such an edge exists</returns>
        protected bool IsUnmapped(Node source, Node target, string edgeType)
        {
            return HasNewState(source, target, edgeType, State.Unmapped);
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

        /// <summary>
        /// Asserts that the number of events of type <typeparamref name="T"/> corresponds to
        /// <paramref name="expected"/>.
        ///
        /// <see cref="EdgeChange"/> events for propagated edges are included by default, but this can be controlled
        /// by setting <paramref name="ignoreVirtual"/>.
        /// </summary>
        /// <param name="expected">Expected number of events of type <typeparamref name="T"/></param>
        /// <param name="change">If given, only counts events with this change type</param>
        /// <param name="affectedGraph">If given, only counts events for this subgraph</param>
        /// <param name="ignoreVirtual">Whether to ignore <see cref="EdgeChange"/> events
        /// for virtual (e.g., propagated) edges</param>
        /// <typeparam name="T">Type of events that should be counted</typeparam>
        protected void AssertEventCountEquals<T>(int expected, ChangeType? change = null, ReflexionSubgraph? affectedGraph = null, bool ignoreVirtual = false) where T : ChangeEvent
        {
            Assert.AreEqual(expected, changes.OfType<T>().Count(EventIncluded));

            // Returns whether the given event shall be included in the count of events or not.
            // See method documentation for details.
            bool EventIncluded(T @event)
            {
                return (change == null || @event.Change == change) 
                       && (affectedGraph == null || @event.Affected == affectedGraph) 
                       && !(ignoreVirtual && @event is EdgeChange edgeChange && edgeChange.Edge.HasToggle(Edge.IsVirtualToggle));
            }
        }

        /// <summary>
        /// Dumps the results collected in mapsToEdges, edgeChanges, propagatedEdges, and removedEdges
        /// to standard output.
        /// </summary>
        protected void DumpEvents(bool orderByCategory = true)
        {
            Debug.Log($"CHANGES IN REFLEXION{(orderByCategory ? " (ordered by category)" : " (ordered by time)")}\n\n");
            foreach (ChangeEvent e in orderByCategory ? changes.OrderBy(x => x.GetType().Name) : changes.AsEnumerable())
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
            ResultState result = TestContext.CurrentContext.Result.Outcome;
            if (result == ResultState.Failure || result == ResultState.Error)
            {
                // In case the tests failed, it helps to see the events:
                DumpEvents();
            }
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
        public virtual void OnNext(ChangeEvent changeEvent)
        {
            changes = changes.Incorporate(changeEvent).ToList();
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnCompleted()
        {
            // Should never be called.
            throw new NotImplementedException();
        }
    }
}