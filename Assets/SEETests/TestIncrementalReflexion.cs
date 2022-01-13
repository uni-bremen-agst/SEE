using NUnit.Framework;
using SEE.DataModel.DG;
using System.Collections.Generic;
using SEE.Tools.ReflexionAnalysis;

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
        /// The architecture nodes in the architecture graph: a[j] where 1 <= j <= 8.
        ///
        /// Note: a[0] does not exist.
        /// </summary>
        private Dictionary<int, Node> a;

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
            impl = NewImplementation();
            arch = NewArchitecture();
            mapping = new Graph("mapping");
            SetupReflexion();
        }

        private void SetupReflexion()
        {
            reflexion = new Reflexion(impl, arch, mapping);
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
            Save(impl, arch, mapping);
        }

        private static Edge AddToGraph(Graph graph, string edgeType, Node from, Node to)
        {
            Edge edge = new Edge(from, to, edgeType);
            graph.AddEdge(edge);
            return edge;
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
        ///   s1: a3 -> a7
        ///   s2: a1 -> a3
        ///   s3: a8 -> a8
        ///   s4: a2 -> a4
        /// </summary>
        /// <returns></returns>
        private Graph NewArchitecture()
        {
            Graph arch = new Graph("architecture");
            a = new Dictionary<int, Node>();
            for (int j = 1; j <= 9; j++)
            {
                a[j] = NewNode(arch, "a" + j, "Component");
            }
            a[7].AddChild(a[6]);
            a[7].AddChild(a[5]);

            a[8].AddChild(a[1]);
            a[8].AddChild(a[2]);

            Dictionary<int, Edge> s = new Dictionary<int, Edge>();
            s[1] = AddToGraph(arch, call, a[3], a[7]);
            s[2] = AddToGraph(arch, call, a[1], a[3]);
            s[3] = AddToGraph(arch, call, a[8], a[8]);
            s[4] = AddToGraph(arch, call, a[2], a[4]);

            return arch;
        }

        private Graph NewImplementation()
        {
            Graph impl = new Graph("implementation");
            i = new Dictionary<int, Node>();
            for (int j = 1; j <= 17; j++)
            {
                i[j] = NewNode(impl, "i" + j);
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

            Dictionary<int, Edge> e = new Dictionary<int, Edge>();
            e[1] = AddToGraph(impl, call, i[3], i[15]);
            e[2]= AddToGraph(impl, call, i[4], i[16]);
            e[3] = AddToGraph(impl, call, i[5], i[17]);
            e[4] = AddToGraph(impl, call, i[8], i[6]);
            e[5] = AddToGraph(impl, call, i[9], i[8]);
            e[6] = AddToGraph(impl, call, i[9], i[10]);
            e[7] = AddToGraph(impl, call, i[12], i[10]);
            e[8] = AddToGraph(impl, call, i[12], i[9]);
            e[9] = AddToGraph(impl, call, i[14], i[13]);

            return impl;
        }

        //--------------------
        // Incremental mapping
        //--------------------

        private void AssertMapped(Node implNode, Node archNode)
        {
            Assert.AreEqual(1, mapsToEdgesAdded.Count);
            Assert.AreEqual(implNode.ID, mapsToEdgesAdded[0].mapsToEdge.Source.ID);
            Assert.AreEqual(archNode.ID, mapsToEdgesAdded[0].mapsToEdge.Target.ID);
        }

        private void AssertUnmapped(Node implNode, Node archNode)
        {
            Assert.AreEqual(1, mapsToEdgesRemoved.Count);
            Assert.AreEqual(implNode.ID, mapsToEdgesRemoved[0].mapsToEdge.Source.ID);
            Assert.AreEqual(archNode.ID, mapsToEdgesRemoved[0].mapsToEdge.Target.ID);
        }

        /// <summary>
        /// Mapping i1 -> a1 and i2 -> a1.
        /// Expected result: a propagated edge from a1 to a1 that is implicitly allowed.
        /// </summary>
        [Test]
        public void TestMappingToSameComponentA()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            impl = new Graph("implementation");
            arch = new Graph("architecture");
            mapping = new Graph("mapping");
            SetupReflexion();
            ResetEvents();

            Node a1 = NewNode(arch, "a1", "Component");
            Node a2 = NewNode(arch, "a2", "Component");

            Node i1 = NewNode(impl, "i1", "Routine");
            Node i2 = NewNode(impl, "i2", "Routine");
            Edge e = NewEdge(impl, i1, i2, call);

            // i1 -> a1
            reflexion.Add_To_Mapping(i1, a1);
            AssertMapped(i1, a1);
            ResetEvents();

            // i2 -> a1
            reflexion.Add_To_Mapping(i2, a1);
            AssertMapped(i2, a1);
            Assert.AreEqual(1, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a1, a1, call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsImplicitlyAllowed(edgeChanges, a1, a1, call));
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);
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
            impl = new Graph("implementation");
            arch = new Graph("architecture");
            mapping = new Graph("mapping");
            SetupReflexion();
            ResetEvents();

            Node a1 = NewNode(arch, "a1", "Component");
            Node a2 = NewNode(arch, "a2", "Component");

            Node i1 = NewNode(impl, "i1", "Routine");
            Node i2 = NewNode(impl, "i2", "Routine");
            Edge e = NewEdge(impl, i1, i2, call);

            // i2 -> a1
            reflexion.Add_To_Mapping(i2, a1);
            AssertMapped(i2, a1);
            ResetEvents();

            // i1 -> a1
            reflexion.Add_To_Mapping(i1, a1);
            AssertMapped(i1, a1);
            Assert.AreEqual(1, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a1, a1, call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsImplicitlyAllowed(edgeChanges, a1, a1, call));
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);
            ResetEvents();
        }

        [Test]
        public void TestRemapping()
        {
            // We want to start with fresh empty graphs (Setup creates filled ones)
            impl = new Graph("implementation");
            arch = new Graph("architecture");
            mapping = new Graph("mapping");
            SetupReflexion();
            ResetEvents();

            Node a1 = NewNode(arch, "a1", "Component");
            Node a2 = NewNode(arch, "a2", "Component");

            Node i1 = NewNode(impl, "i1", "Routine");
            Node i2 = NewNode(impl, "i2", "Routine");
            Edge e = NewEdge(impl, i1, i2, call);

            // i1 -> a1
            reflexion.Add_To_Mapping(i1, a1);
            AssertMapped(i1, a1);
            ResetEvents();

            // i2 -> a2
            reflexion.Add_To_Mapping(i2, a2);
            AssertMapped(i2, a2);
            Assert.AreEqual(1, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a1, a2, call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsDivergent(edgeChanges, a1, a2, call));
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);
            ResetEvents();

            // unmap i1
            reflexion.Delete_From_Mapping(i1, a1);
            AssertUnmapped(i1, a1);
            Assert.AreEqual(1, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a1, a2, call));
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(0, edgeChanges.Count);
            ResetEvents();

            // i1 -> a2
            reflexion.Add_To_Mapping(i1, a2);
            AssertMapped(i1, a2);
            Assert.AreEqual(1, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a2, a2, call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsImplicitlyAllowed(edgeChanges, a2, a2, call));
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);
        }

        [Test]
        public void TestIncrementalMapping()
        {
            //--------------------
            // initial state
            //--------------------
            Assert.That(IsAbsent(edgeChanges, a[3], a[7], call));
            Assert.That(IsAbsent(edgeChanges, a[1], a[3], call));
            Assert.That(IsAbsent(edgeChanges, a[8], a[8], call));
            Assert.That(IsAbsent(edgeChanges, a[2], a[4], call));
            Assert.AreEqual(0, mapsToEdgesAdded.Count);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            //--------------------
            // incremental mapping
            //--------------------

            ResetEvents();
            reflexion.Add_To_Mapping(i[17], a[6]);
            Assert.AreEqual(0, edgeChanges.Count);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            AssertMapped(i[17], a[6]);
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[16], a[6]);
            Assert.AreEqual(0, edgeChanges.Count);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            AssertMapped(i[16], a[6]);
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[3], a[3]);
            Assert.AreEqual(2, edgeChanges.Count);
            Assert.That(IsConvergent(edgeChanges, a[3], a[7], call));
            Assert.That(IsAllowed(edgeChanges, a[3], a[6], call));
            Assert.AreEqual(1, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a[3], a[6], call));
            AssertMapped(i[3], a[3]);
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[15], a[5]);
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsAllowed(edgeChanges, a[3], a[5], call));
            Assert.AreEqual(1, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a[3], a[5], call));
            AssertMapped(i[15], a[5]);
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[1], a[1]);
            AssertMapped(i[1], a[1]);
            Assert.AreEqual(2, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a[1], a[3], call));
            Assert.That(IsPropagated(a[1], a[1], call));
            Assert.AreEqual(4, edgeChanges.Count);
            Assert.That(IsAllowed(edgeChanges, a[1], a[3], call));
            Assert.That(IsConvergent(edgeChanges, a[1], a[3], call));
            Assert.That(IsAllowed(edgeChanges, a[1], a[1], call));
            Assert.That(IsConvergent(edgeChanges, a[8], a[8], call));
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[14], a[4]);
            AssertMapped(i[14], a[4]);
            Assert.AreEqual(1, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a[4], a[1], call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsDivergent(edgeChanges, a[4], a[1], call));
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[2], a[9]);
            AssertMapped(i[2], a[9]);
            Assert.AreEqual(3, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a[9], a[3], call));
            Assert.That(IsPropagated(a[1], a[9], call));
            Assert.That(IsPropagated(a[9], a[9], call));
            Assert.AreEqual(2, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a[1], a[3], call));
            Assert.That(IsUnpropagated(a[1], a[1], call));
            Assert.AreEqual(5, edgeChanges.Count);
            Assert.That(IsDivergent(edgeChanges, a[9], a[3], call));
            Assert.That(IsDivergent(edgeChanges, a[1], a[9], call));
            Assert.That(IsImplicitlyAllowed(edgeChanges, a[9], a[9], call));
            Assert.That(IsAbsent(edgeChanges, a[1], a[3], call));
            Assert.That(IsAbsent(edgeChanges, a[8], a[8], call));

            ResetEvents();
            reflexion.Add_To_Mapping(i[10], a[2]);
            AssertMapped(i[10], a[2]);
            Assert.AreEqual(2, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a[1], a[2], call));
            Assert.That(IsPropagated(a[9], a[2], call));
            Assert.AreEqual(3, edgeChanges.Count);
            Assert.That(IsAllowed(edgeChanges, a[1], a[2], call));
            Assert.That(IsDivergent(edgeChanges, a[9], a[2], call));
            Assert.That(IsConvergent(edgeChanges, a[8], a[8], call));
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);

            //----------------------
            // incremental unmapping
            //----------------------
            ResetEvents();
            reflexion.Delete_From_Mapping(i[10], a[2]);
            AssertUnmapped(i[10], a[2]);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(2, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a[1], a[2], call));
            Assert.That(IsUnpropagated(a[9], a[2], call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsAbsent(edgeChanges, a[8], a[8], call));

            ResetEvents();
            reflexion.Delete_From_Mapping(i[2], a[9]);
            AssertUnmapped(i[2], a[9]);
            Assert.AreEqual(2, propagatedEdgesAdded.Count);
            Assert.That(IsPropagated(a[1], a[3], call));
            Assert.That(IsPropagated(a[1], a[1], call));
            Assert.AreEqual(3, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a[9], a[3], call));
            Assert.That(IsUnpropagated(a[1], a[9], call));
            Assert.That(IsUnpropagated(a[9], a[9], call));
            Assert.AreEqual(4, edgeChanges.Count);
            Assert.That(IsConvergent(edgeChanges, a[1], a[3], call));
            Assert.That(IsConvergent(edgeChanges, a[8], a[8], call));
            Assert.That(IsAllowed(edgeChanges, a[1], a[3], call));
            Assert.That(IsAllowed(edgeChanges, a[1], a[1], call));

            ResetEvents();
            reflexion.Delete_From_Mapping(i[14], a[4]);
            AssertUnmapped(i[14], a[4]);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(1, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a[4], a[1], call));
            Assert.AreEqual(0, edgeChanges.Count);

            ResetEvents();
            reflexion.Delete_From_Mapping(i[1], a[1]);
            AssertUnmapped(i[1], a[1]);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(2, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a[1], a[3], call));
            Assert.That(IsUnpropagated(a[1], a[1], call));
            Assert.AreEqual(2, edgeChanges.Count);
            Assert.That(IsAbsent(edgeChanges, a[8], a[8], call));
            Assert.That(IsAbsent(edgeChanges, a[1], a[3], call));

            ResetEvents();
            reflexion.Delete_From_Mapping(i[15], a[5]);
            AssertUnmapped(i[15], a[5]);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(1, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a[3], a[5], call));
            Assert.AreEqual(0, edgeChanges.Count);

            ResetEvents();
            reflexion.Delete_From_Mapping(i[3], a[3]);
            AssertUnmapped(i[3], a[3]);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(1, propagatedEdgesRemoved.Count);
            Assert.That(IsUnpropagated(a[3], a[6], call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsAbsent(edgeChanges, a[3], a[7], call));

            ResetEvents();
            reflexion.Delete_From_Mapping(i[16], a[6]);
            AssertUnmapped(i[16], a[6]);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);
            Assert.AreEqual(0, edgeChanges.Count);

            ResetEvents();
            reflexion.Delete_From_Mapping(i[17], a[6]);
            AssertUnmapped(i[17], a[6]);
            Assert.AreEqual(0, propagatedEdgesAdded.Count);
            Assert.AreEqual(0, propagatedEdgesRemoved.Count);
            Assert.AreEqual(0, edgeChanges.Count);
        }
    }
}
