using NUnit.Framework;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.Tools.ReflexionAnalysis;
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
            reflexion.Register(this);
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
            MapsToEdgeEvent eventAdded = changes.OfType<MapsToEdgeEvent>().Single(x => x.Change == ChangeType.Addition);
            Assert.AreEqual(implNode.ID, eventAdded.MapsToEdge.Source.ID);
            Assert.AreEqual(archNode.ID, eventAdded.MapsToEdge.Target.ID);
        }

        private void AssertUnmapped(Node implNode, Node archNode)
        {
            MapsToEdgeEvent eventRemoved = changes.OfType<MapsToEdgeEvent>().Single(x => x.Change == ChangeType.Removal);
            Assert.AreEqual(implNode.ID, eventRemoved.MapsToEdge.Source.ID);
            Assert.AreEqual(archNode.ID, eventRemoved.MapsToEdge.Target.ID);
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
            reflexion.AddToMapping(i1, a1);
            AssertMapped(i1, a1);
            ResetEvents();

            // i2 -> a1
            reflexion.AddToMapping(i2, a1);
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
            reflexion.AddToMapping(i2, a1);
            AssertMapped(i2, a1);
            ResetEvents();

            // i1 -> a1
            reflexion.AddToMapping(i1, a1);
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
            reflexion.AddToMapping(i1, a1);
            AssertMapped(i1, a1);
            ResetEvents();

            // i2 -> a2
            reflexion.AddToMapping(i2, a2);
            AssertMapped(i2, a2);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a1, a2, call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsDivergent(a1, a2, call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            ResetEvents();

            // unmap i1
            reflexion.DeleteFromMapping(i1, a1);
            AssertUnmapped(i1, a1);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a1, a2, call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<EdgeChange>(0);
            ResetEvents();

            // i1 -> a2
            reflexion.AddToMapping(i1, a2);
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
            reflexion.AddToMapping(i_1, a1);
            reflexion.AddToMapping(i_2, a2);
            reflexion.AddToMapping(i1, a1);
            reflexion.AddToMapping(i2, a2);

            // Test addition

            ResetEvents();
            Edge ei12 = new Edge(i1, i2, call);
            reflexion.AddToImplementation(ei12);
            Assert.That(IsConvergent(a_1, a_2, call));
            Assert.That(IsPropagated(a1, a2, call));
            Assert.That(IsAllowed(a1, a2, call));

            ResetEvents();
            Edge ei_12 = new Edge(i_1, i_2, call);
            reflexion.AddToImplementation(ei_12);
            Assert.That(IsNotContained(a_1, a_2, call));
            Assert.That(IsNotContained(a1, a2, call));

            // Test deletion

            ResetEvents();
            reflexion.DeleteFromImplementation(ei_12);
            Assert.That(IsNotContained(a_1, a_2, call));
            Assert.That(IsNotContained(a1, a2, call));

            ResetEvents();
            reflexion.DeleteFromImplementation(ei12);
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
            reflexion.AddToMapping(i1, a_1);
            reflexion.AddToMapping(i2, a_2);
            Assert.That(IsAbsent(a1, a2, call));
            AssertEventCountEquals<EdgeChange>(1);
            AssertEventCountEquals<MapsToEdgeEvent>(2, ChangeType.Addition);
            Assert.AreEqual(3, changes.Count);

            ResetEvents();
            reflexion.DeleteFromArchitecture(ea12);
            AssertEventCountEquals<EdgeChange>(0); // no matching propagated edge exists
            Assert.AreEqual(1, changes.Count);
            Assert.IsTrue(changes.OfType<ArchitectureEdgeEvent>().Single(x => x.Change == ChangeType.Removal).ArchitectureEdge.Equals(ea12));

            // We will now check the "left side" scenario of the figure.
            // We will restore the "left side" state by using the incremental operations.
            ResetEvents();
            ea12 = new Edge(a1, a2, call);
            reflexion.AddToArchitecture(ea12);
            Assert.That(IsAbsent(a1, a2, call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.IsTrue(changes.OfType<ArchitectureEdgeEvent>().Single(x => x.Change == ChangeType.Addition).ArchitectureEdge.Equals(ea12));
            Assert.AreEqual(2, changes.Count);

            ResetEvents();
            Edge ei12 = new Edge(i1, i2, call);
            reflexion.AddToImplementation(ei12);
            Assert.That(IsConvergent(a1, a2, call));
            Assert.That(IsAllowed(a_1, a_2, call));
            AssertEventCountEquals<EdgeChange>(2);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            AssertEventCountEquals<ImplementationEdgeEvent>(1, ChangeType.Addition);
            Assert.AreEqual(4, changes.Count);

            // Now we can check what happens once we remove ea12 (an allowed edge should become divergent).
            ResetEvents();
            reflexion.DeleteFromArchitecture(ea12);
            Assert.That(IsDivergent(a_1, a_2, call));
            AssertEventCountEquals<EdgeChange>(1);
            AssertEventCountEquals<ArchitectureEdgeEvent>(1, ChangeType.Removal);
            Assert.AreEqual(2, changes.Count);

            // And one last time, we add it back to check the `AddToArchitecture` operation.
            ResetEvents();
            ea12 = new Edge(a1, a2, call);
            reflexion.AddToArchitecture(ea12);
            Assert.That(IsConvergent(a1, a2, call));
            Assert.That(IsAllowed(a_1, a_2, call));
            AssertEventCountEquals<EdgeChange>(2);
            AssertEventCountEquals<ArchitectureEdgeEvent>(1, ChangeType.Addition);
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
                          .OfType<ImplementationHierarchyChangeEvent>()
                          .Count(x => x.Child == i[7] && x.Parent == i[2] && x.Change == ChangeType.Removal) == 1);
            Assert.That(IsUnpropagated(a[9], a[2], call));
            Assert.That(IsUnpropagated(a[9], a[9], call));
            Assert.That(IsUnpropagated(a[1], a[9], call));
            Assert.That(IsUnpropagated(a[9], a[3], call));
            Assert.AreEqual(changes.Count, 5);

            // Now, we add the relationship back.
            ResetEvents();
            reflexion.AddChildInImplementation(i[7], i[2]);
            Assert.IsTrue(changes
                          .OfType<ImplementationHierarchyChangeEvent>()
                          .Count(x => x.Child == i[7] && x.Parent == i[2] && x.Change == ChangeType.Addition) == 1);
            Assert.That(IsPropagated(a[9], a[2], call));
            Assert.That(IsPropagated(a[9], a[9], call));
            Assert.That(IsPropagated(a[1], a[9], call));
            Assert.That(IsPropagated(a[9], a[3], call));
            Assert.That(IsDivergent(a[9], a[2], call));
            Assert.That(IsImplicitlyAllowed(a[9], a[9], call));
            Assert.That(IsDivergent(a[1], a[9], call));
            Assert.That(IsDivergent(a[9], a[3], call));
            Assert.AreEqual(changes.Count, 9);
        }

        /// <summary>
        /// Sets up the reflexion graph's mapping as in Figure 8 of "Incremental Reflexion Analysis" (Koschke, 2011).
        /// </summary>
        public void MapIncrementally()
        {
            ResetEvents();
            reflexion.AddToMapping(i[17], a[6]);
            AssertEventCountEquals<EdgeChange>(0);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertMapped(i[17], a[6]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.AddToMapping(i[16], a[6]);
            AssertEventCountEquals<EdgeChange>(0);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertMapped(i[16], a[6]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.AddToMapping(i[3], a[3]);
            AssertEventCountEquals<EdgeChange>(2);
            Assert.That(IsConvergent(a[3], a[7], call));
            Assert.That(IsAllowed(a[3], a[6], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a[3], a[6], call));
            AssertMapped(i[3], a[3]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.AddToMapping(i[15], a[5]);
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsAllowed(a[3], a[5], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a[3], a[5], call));
            AssertMapped(i[15], a[5]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.AddToMapping(i[1], a[1]);
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
            reflexion.AddToMapping(i[14], a[4]);
            AssertMapped(i[14], a[4]);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Addition);
            Assert.That(IsPropagated(a[4], a[1], call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsDivergent(a[4], a[1], call));
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.AddToMapping(i[2], a[9]);
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
            reflexion.AddToMapping(i[10], a[2]);
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
            AssertEventCountEquals<MapsToEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            AssertEventCountEquals<ImplementationEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<ImplementationEdgeEvent>(0, ChangeType.Removal);

            MapIncrementally();

            //----------------------
            // incremental unmapping
            //----------------------
            ResetEvents();
            reflexion.DeleteFromMapping(i[10], a[2]);
            AssertUnmapped(i[10], a[2]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[1], a[2], call));
            Assert.That(IsUnpropagated(a[9], a[2], call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsAbsent(a[8], a[8], call));

            ResetEvents();
            reflexion.DeleteFromMapping(i[2], a[9]);
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
            reflexion.DeleteFromMapping(i[14], a[4]);
            AssertUnmapped(i[14], a[4]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[4], a[1], call));
            AssertEventCountEquals<EdgeChange>(0);

            ResetEvents();
            reflexion.DeleteFromMapping(i[1], a[1]);
            AssertUnmapped(i[1], a[1]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(2, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[1], a[3], call));
            Assert.That(IsUnpropagated(a[1], a[1], call));
            AssertEventCountEquals<EdgeChange>(2);
            Assert.That(IsAbsent(a[8], a[8], call));
            Assert.That(IsAbsent(a[1], a[3], call));

            ResetEvents();
            reflexion.DeleteFromMapping(i[15], a[5]);
            AssertUnmapped(i[15], a[5]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[3], a[5], call));
            AssertEventCountEquals<EdgeChange>(0);

            ResetEvents();
            reflexion.DeleteFromMapping(i[3], a[3]);
            AssertUnmapped(i[3], a[3]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(1, ChangeType.Removal);
            Assert.That(IsUnpropagated(a[3], a[6], call));
            AssertEventCountEquals<EdgeChange>(1);
            Assert.That(IsAbsent(a[3], a[7], call));

            ResetEvents();
            reflexion.DeleteFromMapping(i[16], a[6]);
            AssertUnmapped(i[16], a[6]);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            AssertEventCountEquals<EdgeChange>(0);

            ResetEvents();
            reflexion.DeleteFromMapping(i[17], a[6]);
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
            AssertEventCountEquals<MapsToEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<PropagatedEdgeEvent>(0, ChangeType.Removal);
            AssertEventCountEquals<ImplementationEdgeEvent>(0, ChangeType.Addition);
            AssertEventCountEquals<ImplementationEdgeEvent>(0, ChangeType.Removal);

            ResetEvents();
            reflexion.DeleteFromImplementation(ie[(14, 13)]);
            AssertEventCountEquals<ImplementationEdgeEvent>(1, ChangeType.Removal);
            Assert.AreEqual(1, changes.Count);
        }
    }
}