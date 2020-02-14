using NUnit.Framework;
using System.Collections.Generic;

namespace SEE.DataModel 
{
    /// <summary>
    /// Tests for the incremental reflexion analysis.
    /// 
    /// These test cases follows the scenarios described in the paper
    /// "Incremental Reflexion Analysis", Rainer Koschke, Journal on Software Maintenance
    /// and Evolution, 2011, DOI 10.1002 / smr.542 in Figure 8.
    /// </summary>
    class TestIncrementalReflexion : TestReflexionAnalysis
    {
        /// <summary>
        /// The implementation nodes in the implementation graph: i[j] where 1 <= j <= 17.
        /// 
        /// Note: i[0] does not exist.
        /// </summary>
        Dictionary<int, Node> i;

        /// <summary>
        /// The architecture nodes in the architecture graph: a[j] where 1 <= j <= 8.
        /// 
        /// Note: a[0] does not exist.
        /// </summary>
        Dictionary<int, Node> a;

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
            mapping = new Graph();
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

        private static void AddToGraph(Graph graph, Edge edge, Node from, Node to)
        {
            edge.Source = from;
            edge.Target = to;
            graph.AddEdge(edge);
        }

        private Graph NewArchitecture()
        {
            Graph arch = new Graph();
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
            for (int j = 1; j <= 4; j++)
            {
                s[j] = new Edge();
                s[j].Type = call;
            }

            AddToGraph(arch, s[1], a[3], a[7]);
            AddToGraph(arch, s[2], a[1], a[3]);
            AddToGraph(arch, s[3], a[8], a[8]);
            AddToGraph(arch, s[4], a[2], a[4]);

            return arch;
        }

        private Graph NewImplementation()
        {
            Graph impl = new Graph();
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
            for (int j = 1; j <= 9; j++)
            {
                Edge edge = new Edge();
                edge.Type = call;
                e[j] = edge;
            }

            AddToGraph(impl, e[1], i[ 3], i[15]);
            AddToGraph(impl, e[2], i[ 4], i[16]);
            AddToGraph(impl, e[3], i[ 5], i[17]);
            AddToGraph(impl, e[4], i[ 8], i[ 6]);
            AddToGraph(impl, e[5], i[ 9], i[ 8]);
            AddToGraph(impl, e[6], i[ 9], i[10]);
            AddToGraph(impl, e[7], i[12], i[10]);
            AddToGraph(impl, e[8], i[12], i[ 9]);
            AddToGraph(impl, e[9], i[14], i[13]);

            return impl;
        }

        //--------------------
        // Incremental mapping 
        //--------------------

        private void AssertMapped(Node implNode, Node archNode)
        {
            Assert.AreEqual(1, mapsToEdgesAdded.Count);
            Assert.AreEqual(implNode.LinkName, mapsToEdgesAdded[0].mapsToEdge.Source.LinkName);
            Assert.AreEqual(archNode.LinkName, mapsToEdgesAdded[0].mapsToEdge.Target.LinkName);
        }

        private void AssertUnmapped(Node implNode, Node archNode)
        {
            Assert.AreEqual(1, mapsToEdgesRemoved.Count);
            Assert.AreEqual(implNode.LinkName, mapsToEdgesRemoved[0].mapsToEdge.Source.LinkName);
            Assert.AreEqual(archNode.LinkName, mapsToEdgesRemoved[0].mapsToEdge.Target.LinkName);
        }

        private bool IsPropagated(Node from, Node to, string edgeType)
        {
            foreach (PropagatedEdge edge in propagatedEdges)
            {
                if (from.LinkName == edge.propagatedEdge.Source.LinkName
                    && to.LinkName == edge.propagatedEdge.Target.LinkName
                    && edgeType == edge.propagatedEdge.Type)
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void TestIncrementalMapping()
        {
            Assert.That(IsAbsent(edgeChanges, a[3], a[7], call));
            Assert.That(IsAbsent(edgeChanges, a[1], a[3], call));
            Assert.That(IsAbsent(edgeChanges, a[8], a[8], call));
            Assert.That(IsAbsent(edgeChanges, a[2], a[4], call));
            Assert.AreEqual(0, mapsToEdgesAdded.Count);
            Assert.AreEqual(0, propagatedEdges.Count);
            Assert.AreEqual(0, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[17], a[6]);
            Assert.AreEqual(0, edgeChanges.Count);
            Assert.AreEqual(0, propagatedEdges.Count);
            AssertMapped(i[17], a[6]);
            Assert.AreEqual(0, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[16], a[6]);
            Assert.AreEqual(0, edgeChanges.Count);
            Assert.AreEqual(0, propagatedEdges.Count);
            AssertMapped(i[16], a[6]);
            Assert.AreEqual(0, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[3], a[3]);
            Assert.AreEqual(2, edgeChanges.Count);
            Assert.That(IsConvergent(edgeChanges, a[3], a[7], call));
            Assert.That(IsAllowed(edgeChanges, a[3], a[6], call));
            Assert.AreEqual(1, propagatedEdges.Count);
            Assert.That(IsPropagated(a[3], a[6], call));
            AssertMapped(i[3], a[3]);
            Assert.AreEqual(0, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[15], a[5]);
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsAllowed(edgeChanges, a[3], a[5], call));
            Assert.AreEqual(1, propagatedEdges.Count);
            Assert.That(IsPropagated(a[3], a[5], call));
            AssertMapped(i[15], a[5]);
            Assert.AreEqual(0, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[1], a[1]);
            AssertMapped(i[1], a[1]);
            Assert.AreEqual(2, propagatedEdges.Count);
            Assert.That(IsPropagated(a[1], a[3], call));
            Assert.That(IsPropagated(a[1], a[1], call));
            Assert.AreEqual(4, edgeChanges.Count);
            Assert.That(IsAllowed   (edgeChanges, a[1], a[3], call));
            Assert.That(IsConvergent(edgeChanges, a[1], a[3], call));
            Assert.That(IsAllowed   (edgeChanges, a[1], a[1], call));
            Assert.That(IsConvergent(edgeChanges, a[8], a[8], call));
            Assert.AreEqual(0, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[14], a[4]);
            AssertMapped(i[14], a[4]);
            Assert.AreEqual(1, propagatedEdges.Count);
            Assert.That(IsPropagated(a[4], a[1], call));
            Assert.AreEqual(1, edgeChanges.Count);
            Assert.That(IsDivergent(edgeChanges, a[4], a[1], call));
            Assert.AreEqual(0, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[2], a[9]);
            AssertMapped(i[2], a[9]);
            Assert.AreEqual(3, propagatedEdges.Count);
            Assert.That(IsPropagated(a[9], a[3], call));
            Assert.That(IsPropagated(a[1], a[9], call));
            Assert.That(IsPropagated(a[9], a[9], call));
            Assert.AreEqual(5, edgeChanges.Count);
            Assert.That(IsDivergent(edgeChanges, a[9], a[3], call));
            Assert.That(IsDivergent(edgeChanges, a[1], a[9], call));
            Assert.That(IsImplicitlyAllowed(edgeChanges, a[9], a[9], call));
            Assert.That(IsAbsent(edgeChanges, a[1], a[3], call));
            Assert.That(IsAbsent(edgeChanges, a[8], a[8], call));
            Assert.AreEqual(2, removedEdges.Count);

            ResetEvents();
            reflexion.Add_To_Mapping(i[10], a[2]);
            AssertMapped(i[10], a[2]);
            Assert.AreEqual(2, propagatedEdges.Count);
            Assert.That(IsPropagated(a[1], a[2], call));
            Assert.That(IsPropagated(a[9], a[2], call));
            Assert.AreEqual(3, edgeChanges.Count);
            Assert.That(IsAllowed(edgeChanges, a[1], a[2], call));
            Assert.That(IsDivergent(edgeChanges, a[9], a[2], call));
            Assert.That(IsConvergent(edgeChanges, a[8], a[8], call));
            Assert.AreEqual(0, removedEdges.Count);
        }
    }
}
