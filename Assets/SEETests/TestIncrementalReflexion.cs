using System;
using NUnit.Framework;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SEE.DataModel;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;

namespace SEE.Tools.Architecture
{
    /// <summary>
    /// Tests for the incremental reflexion analysis.
    ///
    /// These test cases follows the scenarios described in the paper
    /// "Incremental Reflexion Analysis", Rainer Koschke, Journal on Software Maintenance
    /// and Evolution, 2011, DOI 10.1002 / smr.542 in Figure 8.
    /// </summary>
    [Explicit("Reflexion tests currently don't work, tracked in #481")]
    internal class TestIncrementalReflexion : TestReflexionAnalysis
    {
        /// <summary>
        /// The implementation nodes in the implementation graph: i[j] where 1 <= j <= 17.
        ///
        /// Note: i[0] does not exist.
        /// </summary>
        private Dictionary<int, Node> i;

        /// <summary>
        /// The implementation edges in the implementation graph: (x, y) for an edge from ix to iy.
        /// </summary>
        private Dictionary<(int, int), Edge> ie;

        /// <summary>
        /// The architecture nodes in the architecture graph: a[j] where 1 <= j <= 8.
        ///
        /// Note: a[0] does not exist.
        /// </summary>
        private Dictionary<int, Node> a;

        /// <summary>
        /// The architecture edges in the architecture graph: (x, y) for an edge from ax to ay.
        /// </summary>
        private Dictionary<(int, int), Edge> ae;

        /// <summary>
        /// Sets up all three graphs (implementation, architecture,
        /// mapping) and registers itself at the reflexion analysis
        /// to obtain the results via the callback Update(ChangeEvent).
        /// Does not really run the analysis, however.
        /// </summary>
        [SetUp]
        protected override void Setup()
        {
            base.Setup();
            AddImplementation();
            AddArchitecture();
            SetupReflexion();
        }

        private void SetupReflexion()
        {
            reflexion = new Reflexion(fullGraph);
            reflexion.Subscribe(this);
            // An initial run is necessary to set up the necessary data structures.
            reflexion.Run();
        }

        [TearDown]
        protected override void Teardown()
        {
            base.Teardown();
            i = null;
            a = null;
        }

        /// <summary>
        /// Saves iml, arch, and mapping to separate GXL files. Useful for debugging.
        /// </summary>
        protected void SaveGraphs()
        {
            (Graph implementation, Graph architecture, Graph mapping) = fullGraph.Disassemble();
            Save(implementation, architecture, mapping);
        }

        private Edge AddToGraph(string edgeType, Node from, Node to)
        {
            return NewEdge(from, to, edgeType);
        }

        /// <summary>
        /// Creates an architecture as follows:
        ///
        /// Node hierarchy (child => parent):
        ///   a1 => a8
        ///   a2 => a8
        ///   a3
        ///   a4
        ///   a5 => a7
        ///   a6 => a7
        ///   a7
        ///   a8
        /// Dependency edges (n1 depends on n2: n1 -> n2):
        ///   a3 -> a7
        ///   a1 -> a3
        ///   a8 -> a8
        ///   a2 -> a4
        /// </summary>
        /// <returns></returns>
        private void AddArchitecture()
        {
            a = new Dictionary<int, Node>();
            for (int j = 1; j <= 9; j++)
            {
                a[j] = NewNode(true, "a" + j, "Component");
            }

            a[7].AddChild(a[6]);
            a[7].AddChild(a[5]);

            a[8].AddChild(a[1]);
            a[8].AddChild(a[2]);

            (int, int)[] edgesFromTo =
            {
                (3, 7), (1, 3), (8, 8), (2, 4)
            };
            ae = CreateEdgesDictionary(edgesFromTo, a);
        }

        /// <summary>
        /// Returns a new dictionary mapping from (source id, target id) to a newly created edge which maps from
        /// <paramref name="nodes"/>[source id] to <paramref name="nodes"/>[target id].
        /// </summary>
        private Dictionary<(int, int), Edge> CreateEdgesDictionary(IEnumerable<(int, int)> edges,
                                                                   IDictionary<int, Node> nodes)
        {
            return edges.ToDictionary(x => x, x => AddToGraph(call, nodes[x.Item1], nodes[x.Item2]));
        }

        /// <summary>
        /// Creates an implementation as follows:
        ///
        /// Node hierarchy (child => parent):
        ///   i1
        ///   i2  => i1
        ///   i3  => i2
        ///   i4  => i3
        ///   i5  => i3
        ///   i6  => i3
        ///   i7  => i2
        ///   i8  => i7
        ///   i9  => i7
        ///   i10 => i7
        ///   i11 => i1
        ///   i12 => i11
        ///   i13 => i11
        ///   i14
        ///   i15
        ///   i16
        ///   i17
        ///
        /// Dependency edges (n1 depends on n2: n1 -> n2):
        ///   1: i3 -> i15
        ///   2: i4 -> i16
        ///   3: i5 -> i17
        ///   4: i8 -> i6
        ///   5: i9 -> i8
        ///   6: i9 -> i10
        ///   7: i12 -> i10
        ///   8: i12 -> i9
        ///   9: i14 -> i13
        /// </summary>
        private void AddImplementation()
        {
            i = new Dictionary<int, Node>();
            for (int j = 1; j <= 17; j++)
            {
                i[j] = NewNode(false, "i" + j);
            }

            i[1].AddChild(i[2]);
            i[1].AddChild(i[11]);

            i[2].AddChild(i[3]);
            i[2].AddChild(i[7]);

            i[3].AddChild(i[4]);
            i[3].AddChild(i[5]);
            i[3].AddChild(i[6]);

            i[7].AddChild(i[8]);
            i[7].AddChild(i[9]);
            i[7].AddChild(i[10]);

            i[11].AddChild(i[12]);
            i[11].AddChild(i[13]);

            (int, int)[] edgesFromTo =
            {
                (3, 15), (4, 16), (5, 17), (8, 6), (9, 8), (9, 10), (12, 10), (12, 9), (14, 13)
            };
            ie = CreateEdgesDictionary(edgesFromTo, i);
        }

        //--------------------
        // Incremental mapping
        //--------------------

        private void AssertMapped(Node implNode, Node archNode)
        {
            Assert.GreaterOrEqual(changes.OfType<EdgeEvent>().Count(x => x.Change == ChangeType.Addition && x.Affected == ReflexionSubgraph.Mapping &&
                                                                         x.Edge.Source.ID == implNode.ID && x.Edge.Target.ID == archNode.ID), 1);
        }

        private void AssertUnmapped(Node implNode, Node archNode)
        {
            Assert.GreaterOrEqual(changes.OfType<EdgeEvent>().Count(x => x.Change == ChangeType.Removal && x.Affected == ReflexionSubgraph.Mapping &&
                                                                         x.Edge.Source.ID == implNode.ID && x.Edge.Target.ID == archNode.ID), 1);
        }

        /// <summary>
        /// Mapping i1 -> a1 and i2 -> a1.
        /// Expected result: a propagated edge from a1 to a1 that is implicitly allowed.
        /// </summary>
        [Test]
        public void TestMappingToSameComponentA()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            fullGraph = new Graph("DUMMYBASEPATH");
            SetupReflexion();
            ResetEvents();

            Node a1 = NewNode(true, "a1", "Component");
            Node a2 = NewNode(true, "a2", "Component");

            Node i1 = NewNode(false, "i1", "Routine");
            Node i2 = NewNode(false, "i2", "Routine");
            Edge e = NewEdge(i1, i2, call);

            // i1 -> a1
            reflexion.Add(i1, a1);
            AssertMapped(i1, a1);
            ResetEvents();

            // i2 -> a1
            reflexion.Add(i2, a1);
            AssertMapped(i2, a1);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a1, a1, call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsImplicitlyAllowed(a1, a1, call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            ResetEvents();
        }

        /// <summary>
        /// Mapping i2 -> a1 and i1 -> a1 (analogous to TestMappingToSameComponentA, but
        /// the order of the mapping is swapped).
        /// Expected result: a propagated edge from a1 to a1 that is implicitly allowed.
        /// </summary>
        [Test]
        public void TestMappingToSameComponentB()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            fullGraph = new Graph("DUMMYBASEPATH");
            SetupReflexion();
            ResetEvents();

            Node a1 = NewNode(true, "a1", "Component");
            Node a2 = NewNode(true, "a2", "Component");

            Node i1 = NewNode(false, "i1", "Routine");
            Node i2 = NewNode(false, "i2", "Routine");
            Edge e = NewEdge(i1, i2, call);

            // i2 -> a1
            reflexion.Add(i2, a1);
            AssertMapped(i2, a1);
            ResetEvents();

            // i1 -> a1
            reflexion.Add(i1, a1);
            AssertMapped(i1, a1);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a1, a1, call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsImplicitlyAllowed(a1, a1, call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            ResetEvents();
        }

        [Test]
        public void TestRemapping()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            fullGraph = new Graph("DUMMYBASEPATH");
            SetupReflexion();
            ResetEvents();

            Node a1 = NewNode(true, "a1", "Component");
            Node a2 = NewNode(true, "a2", "Component");

            Node i1 = NewNode(false, "i1", "Routine");
            Node i2 = NewNode(false, "i2", "Routine");
            Edge e = NewEdge(i1, i2, call);

            // i1 -> a1
            reflexion.Add(i1, a1);
            AssertMapped(i1, a1);
            ResetEvents();

            // i2 -> a2
            reflexion.Add(i2, a2);
            AssertMapped(i2, a2);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a1, a2, call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsDivergent(a1, a2, call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            ResetEvents();

            // unmap i1
            reflexion.DeleteFromMapping(i1);
            AssertUnmapped(i1, a1);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a1, a2, call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<EdgeChange>(0);
            ResetEvents();

            // i1 -> a2
            reflexion.Add(i1, a2);
            AssertMapped(i1, a2);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a2, a2, call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsImplicitlyAllowed(a2, a2, call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
        }

        [Test]
        public void TestSimpleImplEdgeChange()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            fullGraph = new Graph("");
            SetupReflexion();
            ResetEvents();

            // Example taken from Figure 4 of "Incremental Reflexion Analysis" (Koschke, 2011)
            Node a_1 = NewNode(true, "a'1");
            Node a_2 = NewNode(true, "a'2");
            Node a1 = NewNode(true, "a1");
            Node a2 = NewNode(true, "a2");
            Node i_1 = NewNode(false, "i'1");
            Node i_2 = NewNode(false, "i'2");
            Node i1 = NewNode(false, "i1");
            Node i2 = NewNode(false, "i2");
            a_1.AddChild(a1);
            a_2.AddChild(a2);

            // We will now incrementally add the edges, starting with the architecture (creating an absence).
            Edge ea12 = AddToGraph(call, a_1, a_2);
            ResetEvents();
            SetupReflexion();
            Assert.That(IsAbsent(a_1, a_2, call));

            ResetEvents();
            reflexion.Add(i_1, a1);
            reflexion.Add(i_2, a2);
            reflexion.Add(i1, a1);
            reflexion.Add(i2, a2);

            // Test addition

            ResetEvents();
            reflexion.Add(i1, i2, call);
            Assert.That(IsConvergent(a_1, a_2, call));
            Assert.That(IsPropagated(a1, a2, call));
            Assert.That(IsAllowed(a1, a2, call));

            ResetEvents();
            reflexion.Add(i_1, i_2, call);
            Assert.That(IsNotContained(a_1, a_2, call));
            Assert.That(IsNotContained(a1, a2, call));

            // Test deletion

            ResetEvents();
            reflexion.DeleteFromImplementation(i_1, i_2, call);
            Assert.That(IsNotContained(a_1, a_2, call));
            Assert.That(IsNotContained(a1, a2, call));

            ResetEvents();
            reflexion.DeleteFromImplementation(i1, i2, call);
            Assert.That(IsAbsent(a_1, a_2, call));
            Assert.That(IsUnpropagated(a1, a2, call));
        }

        [Test]
        public void TestSimpleIncrementalArchEdgeChange()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            fullGraph = new Graph("");
            SetupReflexion();

            // Example taken from Figure 7 of "Incremental Reflexion Analysis" (Koschke, 2011).
            // We start by checking the "right side" scenario of the figure.
            Node a_1 = NewNode(true, "a'1");
            Node a_2 = NewNode(true, "a'2");
            Node a1 = NewNode(true, "a1");
            Node a2 = NewNode(true, "a2");
            Node i1 = NewNode(false, "i1");
            Node i2 = NewNode(false, "i2");
            a1.AddChild(a_1);
            a2.AddChild(a_2);

            // We expect an absence before and after the mapping, because no implementation dependency exists.
            Edge ea12 = AddToGraph(call, a1, a2);
            ResetEvents();
            SetupReflexion();
            reflexion.Add(i1, a_1);
            reflexion.Add(i2, a_2);
            Assert.That(IsAbsent(a1, a2, call));
            AssertEventCountEquals<EdgeChange>(1);
            AssertEventCountEquals<EdgeEvent>(2, ChangeType.Addition, ReflexionSubgraph.Mapping);
            Assert.AreEqual(3, changes.Count);

            ResetEvents();
            reflexion.Delete(ea12);
            AssertEventCountEquals<EdgeChange>(0); // no matching propagated edge exists
            Assert.AreEqual(1, changes.Count);
            Assert.IsTrue(changes.OfType<EdgeEvent>().Single(x => x.Change == ChangeType.Removal && x.Affected == ReflexionSubgraph.Architecture).Edge.Equals(ea12));

            // We will now check the "left side" scenario of the figure.
            // We will restore the "left side" state by using the incremental operations.
            ResetEvents();
            reflexion.Add(a1, a2, call);
            Assert.That(IsAbsent(a1, a2, call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.IsTrue(changes.OfType<EdgeEvent>().Single(x => x.Change == ChangeType.Addition && x.Affected == ReflexionSubgraph.Architecture).Edge.Equals(ea12));
            Assert.AreEqual(2, changes.Count);

            ResetEvents();
            reflexion.Add(i1, i2, call);
            Assert.That(IsConvergent(a1, a2, call));
            Assert.That(IsAllowed(a_1, a_2, call));
            AssertEventCountEquals<EdgeChange>(2);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Implementation);
            Assert.AreEqual(4, changes.Count);

            // Now we can check what happens once we remove ea12 (an allowed edge should become divergent).
            ResetEvents();
            reflexion.DeleteFromArchitecture(a1, a2, call);
            Assert.That(IsDivergent(a_1, a_2, call));
            AssertEventCountEquals<EdgeChange>(1);
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Removal, ReflexionSubgraph.Architecture);
            Assert.AreEqual(2, changes.Count);

            // And one last time, we add it back to check the `Add` operation.
            ResetEvents();
            reflexion.Add(a1, a2, call);
            Assert.That(IsConvergent(a1, a2, call));
            Assert.That(IsAllowed(a_1, a_2, call));
            AssertEventCountEquals<EdgeChange>(2);
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture);
            Assert.AreEqual(3, changes.Count);
        }

        [Test]
        public void TestIncrementalImplHierarchyChange()
        {
            // We set up the reflexion graph as described in Figure 9 of the paper.
            MapIncrementally();
            ResetEvents();
            reflexion.UnparentInImplementation(i[7]);
            Assert.IsTrue(changes
                          .OfType<HierarchyEvent>()
                          .Count(x => x.Child == i[7] && x.Parent == i[2] && x.Change == ChangeType.Removal && x.Affected == ReflexionSubgraph.Implementation) == 1);
            Assert.That(IsUnpropagated(a[9], a[2], call));
            Assert.That(IsUnpropagated(a[9], a[9], call));
            Assert.That(IsUnpropagated(a[1], a[9], call));
            Assert.That(IsUnpropagated(a[9], a[3], call));
            Assert.AreEqual(changes.Count, 5);

            // Now, we add the relationship back.
            ResetEvents();
            reflexion.AddChildInImplementation(i[7], i[2]);
            Assert.IsTrue(changes
                          .OfType<HierarchyEvent>()
                          .Count(x => x.Child == i[7] && x.Parent == i[2] && x.Change == ChangeType.Addition && x.Affected == ReflexionSubgraph.Implementation) == 1);
            Assert.That(IsPropagated(a[9], a[2], call));
            Assert.That(IsPropagated(a[9], a[9], call));
            Assert.That(IsPropagated(a[1], a[9], call));
            Assert.That(IsPropagated(a[9], a[3], call));
            Assert.That(IsDivergent(a[9], a[2], call));
            Assert.That(IsImplicitlyAllowed(a[9], a[9], call));
            Assert.That(IsDivergent(a[1], a[9], call));
            Assert.That(IsDivergent(a[9], a[3], call));
            Assert.AreEqual(changes.Count, 9);
            Assert.Throws<NotAnOrphanException>(() => reflexion.AddChildInImplementation(i[7], i[2]));
            Assert.Throws<CyclicHierarchyException>(() => reflexion.AddChildInImplementation(i[2], i[7]));
        }

        [Test]
        public void TestIncrementalArchHierarchyChange()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            fullGraph = new Graph("");
            SetupReflexion();

            // Example taken from Figure 10 of "Incremental Reflexion Analysis" (Koschke, 2011).
            // We set up nodes first:
            Node a1 = NewNode(true, "a1");
            Node a2 = NewNode(true, "a2");
            Node a3 = NewNode(true, "a3");
            Node a_1 = NewNode(true, "a'1");
            Node a_2 = NewNode(true, "a'2");
            Node a_3 = NewNode(true, "a'3");
            Node a_4 = NewNode(true, "a'4");
            Node i1 = NewNode(false, "i1");
            Node i2 = NewNode(false, "i2");
            Node i3 = NewNode(false, "i3");
            Node i4 = NewNode(false, "i4");

            // Then we add the hierarchy:
            a1.AddChild(a_1);
            a2.AddChild(a_2);
            a3.AddChild(a_3);
            a3.AddChild(a_4);

            // Now we setup references:
            AddToGraph(call, a2, a1);
            AddToGraph(call, a2, a3);
            AddToGraph(call, i1, i2);
            AddToGraph(call, i2, i3);
            AddToGraph(call, i2, i4);

            // And finally, we setup the mapping:
            AddToGraph(MapsToType, i1, a_1);
            AddToGraph(MapsToType, i2, a_2);
            AddToGraph(MapsToType, i3, a_3);
            AddToGraph(MapsToType, i4, a_4);

            ResetEvents();
            reflexion.Run();
            Assert.That(IsAbsent(a2, a1, call));
            Assert.That(IsConvergent(a2, a3, call));
            Assert.That(IsDivergent(a_1, a_2, call));
            Assert.That(IsAllowed(a_2, a_3, call));
            Assert.That(IsAllowed(a_2, a_4, call));
            AssertEventCountEquals<EdgeChange>(5);
            AssertEventCountEquals<PropagatedEdgeEvent>(3, ChangeType.Addition);
            Assert.AreEqual(8, changes.Count);

            // Now we start testing what we actually want to check: Incremental changes to the arch hierarchy.
            ResetEvents();
            reflexion.UnparentInArchitecture(a_2);
            Assert.That(IsAbsent(a2, a3, call));
            Assert.That(IsDivergent(a_2, a_3, call));
            Assert.That(IsDivergent(a_2, a_4, call));
            AssertEventCountEquals<HierarchyEvent>(1, ChangeType.Removal, ReflexionSubgraph.Architecture);
            Assert.AreEqual(4, changes.Count);

            // Quick diversion: Adding an edge from a'2 to a'3 should work, but then adding a'2 as a child to a2 should
            // result in a redundant specified edge.
            ResetEvents();
            reflexion.Add(a_2, a_3, call);
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture);
            Assert.Throws<RedundantSpecifiedEdgeException>(() => reflexion.AddChildInArchitecture(a_2, a2));
            ResetEvents();
            reflexion.DeleteFromArchitecture(a_2, a_3, call);
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Removal, ReflexionSubgraph.Architecture);

            ResetEvents();
            reflexion.AddChildInArchitecture(a_2, a2);
            Assert.That(IsConvergent(a2, a3, call));
            Assert.That(IsAllowed(a_2, a_3, call));
            Assert.That(IsAllowed(a_2, a_4, call));
            AssertEventCountEquals<HierarchyEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture);
            Assert.AreEqual(4, changes.Count);

            // Now we test some additional error cases.
            Assert.Throws<NotAnOrphanException>(() => reflexion.AddChildInArchitecture(a_2, a2));
            Assert.Throws<CyclicHierarchyException>(() => reflexion.AddChildInArchitecture(a2, a_2));
            Assert.Throws<NotInSubgraphException>(() => reflexion.AddChildInArchitecture(a2, i2));
            Assert.Throws<RedundantSpecifiedEdgeException>(() => reflexion.Add(a_2, a_3, call));
        }

        /// <summary>
        /// Sets up the reflexion graph's mapping as in Figure 8 of "Incremental Reflexion Analysis" (Koschke, 2011).
        /// </summary>
        public void MapIncrementally()
        {
            ResetEvents();
            reflexion.Add(i[17], a[6]);
            AssertEventCountEquals<EdgeChange>(0);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertMapped(i[17], a[6]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.Add(i[16], a[6]);
            AssertEventCountEquals<EdgeChange>(0);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertMapped(i[16], a[6]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.Add(i[3], a[3]);
            AssertEventCountEquals<EdgeChange>(2);
            Assert.That(IsConvergent(a[3], a[7], call));
            Assert.That(IsAllowed(a[3], a[6], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a[3], a[6], call));
            AssertMapped(i[3], a[3]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.Add(i[15], a[5]);
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsAllowed(a[3], a[5], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a[3], a[5], call));
            AssertMapped(i[15], a[5]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.Add(i[1], a[1]);
            AssertMapped(i[1], a[1]);
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Addition);
            Assert.That(IsPropagated(a[1], a[3], call));
            Assert.That(IsPropagated(a[1], a[1], call));
            AssertEventCountEquals<EdgeChange>(4);
            Assert.That(IsAllowed(a[1], a[3], call));
            Assert.That(IsConvergent(a[1], a[3], call));
            Assert.That(IsAllowed(a[1], a[1], call));
            Assert.That(IsConvergent(a[8], a[8], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.Add(i[14], a[4]);
            AssertMapped(i[14], a[4]);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a[4], a[1], call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsDivergent(a[4], a[1], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.Add(i[2], a[9]);
            AssertMapped(i[2], a[9]);
            AssertEventCountEquals<PropagatedEdgeEvent>(3, ChangeType.Addition);
            Assert.That(IsPropagated(a[9], a[3], call));
            Assert.That(IsPropagated(a[1], a[9], call));
            Assert.That(IsPropagated(a[9], a[9], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[1], a[3], call));
            Assert.That(IsUnpropagated(a[1], a[1], call));
            AssertEventCountEquals<EdgeChange>(5);
            Assert.That(IsDivergent(a[9], a[3], call));
            Assert.That(IsDivergent(a[1], a[9], call));
            Assert.That(IsImplicitlyAllowed(a[9], a[9], call));
            Assert.That(IsAbsent(a[1], a[3], call));
            Assert.That(IsAbsent(a[8], a[8], call));

            ResetEvents();
            reflexion.Add(i[10], a[2]);
            AssertMapped(i[10], a[2]);
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Addition);
            Assert.That(IsPropagated(a[1], a[2], call));
            Assert.That(IsPropagated(a[9], a[2], call));
            AssertEventCountEquals<EdgeChange>(3);
            Assert.That(IsAllowed(a[1], a[2], call));
            Assert.That(IsDivergent(a[9], a[2], call));
            Assert.That(IsConvergent(a[8], a[8], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
        }

        [Test]
        public void TestIncrementalMapping()
        {
            //--------------------
            // initial state
            //--------------------
            Assert.That(IsAbsent(a[3], a[7], call));
            Assert.That(IsAbsent(a[1], a[3], call));
            Assert.That(IsAbsent(a[8], a[8], call));
            Assert.That(IsAbsent(a[2], a[4], call));
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Addition, ReflexionSubgraph.Mapping);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Addition, ReflexionSubgraph.Implementation);
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Implementation);

            MapIncrementally();

            //----------------------
            // incremental unmapping
            //----------------------
            ResetEvents();
            reflexion.DeleteFromMapping(i[10]);
            AssertUnmapped(i[10], a[2]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[1], a[2], call));
            Assert.That(IsUnpropagated(a[9], a[2], call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsAbsent(a[8], a[8], call));

            ResetEvents();
            reflexion.DeleteFromMapping(i[2]);
            AssertUnmapped(i[2], a[9]);
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Addition);
            Assert.That(IsPropagated(a[1], a[3], call));
            Assert.That(IsPropagated(a[1], a[1], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(3, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[9], a[3], call));
            Assert.That(IsUnpropagated(a[1], a[9], call));
            Assert.That(IsUnpropagated(a[9], a[9], call));
            AssertEventCountEquals<EdgeChange>(4);
            Assert.That(IsConvergent(a[1], a[3], call));
            Assert.That(IsConvergent(a[8], a[8], call));
            Assert.That(IsAllowed(a[1], a[3], call));
            Assert.That(IsAllowed(a[1], a[1], call));

            ResetEvents();
            reflexion.DeleteFromMapping(i[14]);
            AssertUnmapped(i[14], a[4]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[4], a[1], call));
            AssertEventCountEquals<EdgeChange>(0);

            ResetEvents();
            reflexion.DeleteFromMapping(i[1]);
            AssertUnmapped(i[1], a[1]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[1], a[3], call));
            Assert.That(IsUnpropagated(a[1], a[1], call));
            AssertEventCountEquals<EdgeChange>(2);
            Assert.That(IsAbsent(a[8], a[8], call));
            Assert.That(IsAbsent(a[1], a[3], call));

            ResetEvents();
            reflexion.DeleteFromMapping(i[15]);
            AssertUnmapped(i[15], a[5]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[3], a[5], call));
            AssertEventCountEquals<EdgeChange>(0);

            ResetEvents();
            reflexion.DeleteFromMapping(i[3]);
            AssertUnmapped(i[3], a[3]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[3], a[6], call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsAbsent(a[3], a[7], call));

            ResetEvents();
            reflexion.DeleteFromMapping(i[16]);
            AssertUnmapped(i[16], a[6]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            AssertEventCountEquals<EdgeChange>(0);

            ResetEvents();
            reflexion.DeleteFromMapping(i[17]);
            AssertUnmapped(i[17], a[6]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            AssertEventCountEquals<EdgeChange>(0);
        }

        [Test]
        public void TestIncrementalImplRefChange()
        {
            //--------------------
            // initial state
            //--------------------

            Assert.That(IsAbsent(a[3], a[7], call));
            Assert.That(IsAbsent(a[1], a[3], call));
            Assert.That(IsAbsent(a[8], a[8], call));
            Assert.That(IsAbsent(a[2], a[4], call));
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Addition, ReflexionSubgraph.Mapping);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Addition, ReflexionSubgraph.Implementation);
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Implementation);

            ResetEvents();
            reflexion.DeleteFromImplementation(ie[(14, 13)]);
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Removal, ReflexionSubgraph.Implementation);
            Assert.AreEqual(1, changes.Count);
        }

        private static IEnumerable<IList<int>> BigIncrementalOrderings()
        {
            IEnumerable<IList<int>> orderings = new[]
            {
                new List<int> { 0, 1, 2, 3, 4, 5, 6 },
                new List<int> { 0, 2, 1, 3, 5, 4, 6 },
                new List<int> { 3, 4, 5, 0, 1, 2, 6 },
                new List<int> { 3, 5, 4, 0, 2, 1, 6 },
                new List<int> { 3, 0, 4, 5, 2, 1, 6 },
            };

            // If 0 and 3 are at the start, the rest can be in any order. Thus, we generate all permutations.
            return orderings.Concat(new[] { 1, 2, 4, 5, 6 }.Permutations().Select(x => new[] { 0, 3 }.Concat(x).ToList()));
        }


        [Test]
        [TestCaseSource(nameof(BigIncrementalOrderings))]
        public void TestBigIncremental(IList<int> order)
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            fullGraph = new Graph("");
            SetupReflexion();

            // Now we recreate Figure 3 from the paper, but we do it fully incrementally.
            // We want to verify that the order of operations here does not matter, so we'll try multiple
            // orderings. The test is parameterized over the order of the operations for this purpose.

            List<Action> incrementalMethods = new List<Action>
            {
                AddArchNodes, // 0
                AddArchHierarchy, // 1, depends on 0
                AddArchEdges, // 2, depends on 0
                AddImplNodes, // 3
                AddImplHierarchy, // 4, depends on 3
                AddImplEdges, // 5, depends on 3
                AddMapping // 6, depends on 0 and 3
            };
            foreach (int o in order)
            {
                incrementalMethods[o]();
            }

            // Verifying that everything went as it should:
            Debug.Log($"Tested Order: {string.Join(",", order)}");
            DumpEvents();
            AssertMapped(i[17], a[6]);
            AssertMapped(i[16], a[6]);
            AssertMapped(i[3], a[3]);
            AssertMapped(i[15], a[5]);
            AssertMapped(i[1], a[1]);
            AssertMapped(i[14], a[4]);
            // Only in Figure 8: AssertMapped(i[2], a[9]);
            AssertMapped(i[10], a[2]);
            Assert.That(IsConvergent(a[3], a[7], call));
            Assert.That(IsConvergent(a[1], a[3], call));
            Assert.That(IsConvergent(a[8], a[8], call));
            Assert.That(IsAbsent(a[2], a[4], call));
            Assert.That(IsDivergent(a[4], a[1], call));
            Assert.That(IsPropagated(a[3], a[5], call));
            Assert.That(IsPropagated(a[3], a[6], call));
            Assert.That(IsPropagated(a[1], a[3], call));
            Assert.That(IsPropagated(a[1], a[1], call));
            Assert.That(IsPropagated(a[1], a[2], call));
            Assert.That(IsPropagated(a[4], a[1], call));
            // Technically 7 propagated edges, but it's actually 6 where one has a counter of 2
            AssertEventCountEquals<PropagatedEdgeEvent>(6, ChangeType.Addition);
            AssertEventCountEquals<HierarchyEvent>(4, ChangeType.Addition, ReflexionSubgraph.Architecture);
            AssertEventCountEquals<HierarchyEvent>(12, ChangeType.Addition, ReflexionSubgraph.Implementation);
            AssertEventCountEquals<NodeEvent>(9, ChangeType.Addition, ReflexionSubgraph.Architecture);
            AssertEventCountEquals<NodeEvent>(17, ChangeType.Addition, ReflexionSubgraph.Implementation);
            AssertEventCountEquals<EdgeEvent>(4, ChangeType.Addition, ReflexionSubgraph.Architecture);
            AssertEventCountEquals<EdgeEvent>(9, ChangeType.Addition, ReflexionSubgraph.Implementation);
        }

        #region Recreating Figure 3

        private void AddMapping()
        {
            reflexion.Add(i[17], a[6]);
            reflexion.Add(i[16], a[6]);
            reflexion.Add(i[3], a[3]);
            reflexion.Add(i[15], a[5]);
            reflexion.Add(i[1], a[1]);
            reflexion.Add(i[14], a[4]);
            // Only in Figure 8: reflexion.Add(i[2], a[9]);
            reflexion.Add(i[10], a[2]);
        }

        private void AddImplEdges()
        {
            (int, int)[] implEdgesFromTo =
            {
                (3, 15), (4, 16), (5, 17), (8, 6), (9, 8), (9, 10), (12, 10), (12, 9), (14, 13)
            };
            ie = implEdgesFromTo.ToDictionary(x => x, x => reflexion.AddToImplementation(i[x.Item1], i[x.Item2], call));
        }

        private void AddImplHierarchy()
        {
            reflexion.AddChildInImplementation(i[2], i[1]);
            reflexion.AddChildInImplementation(i[11], i[1]);

            reflexion.AddChildInImplementation(i[3], i[2]);
            reflexion.AddChildInImplementation(i[7], i[2]);

            reflexion.AddChildInImplementation(i[4], i[3]);
            reflexion.AddChildInImplementation(i[5], i[3]);
            reflexion.AddChildInImplementation(i[6], i[3]);

            reflexion.AddChildInImplementation(i[8], i[7]);
            reflexion.AddChildInImplementation(i[9], i[7]);
            reflexion.AddChildInImplementation(i[10], i[7]);

            reflexion.AddChildInImplementation(i[12], i[11]);
            reflexion.AddChildInImplementation(i[13], i[11]);
        }

        private void AddImplNodes()
        {
            i = new Dictionary<int, Node>();
            for (int j = 1; j <= 17; j++)
            {
                i[j] = new Node
                {
                    ID = "i" + j,
                    SourceName = "i" + j,
                    Type = "Component"
                };
                reflexion.AddToImplementation(i[j]);
            }
        }

        private void AddArchEdges()
        {
            (int, int)[] archEdgesFromTo =
            {
                (3, 7), (1, 3), (8, 8), (2, 4)
            };
            ae = archEdgesFromTo.ToDictionary(x => x, x => reflexion.AddToArchitecture(a[x.Item1], a[x.Item2], call));
        }

        private void AddArchHierarchy()
        {
            reflexion.AddChildInArchitecture(a[6], a[7]);
            reflexion.AddChildInArchitecture(a[5], a[7]);
            reflexion.AddChildInArchitecture(a[1], a[8]);
            reflexion.AddChildInArchitecture(a[2], a[8]);
        }

        private void AddArchNodes()
        {
            a = new Dictionary<int, Node>();
            for (int j = 1; j <= 9; j++)
            {
                a[j] = new Node
                {
                    ID = "a" + j,
                    SourceName = "a" + j,
                    Type = "Component"
                };
                reflexion.AddToArchitecture(a[j]);
            }
        }

        #endregion
    }

    // TODO: Test redundant specified edge in Add and AddChildInArchitecture (incoming and outgoing!)
    // TODO: More complex UnparentInArchitecture tests, with IncomingCross and Inner
    // TODO: Test {AddTo,DeleteFrom}{Architecture,Implementation}(Node)
    // TODO: Test Delete(Edge)
    // TODO: Check error cases
}