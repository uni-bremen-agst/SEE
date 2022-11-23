using NUnit.Framework;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraph;

namespace SEE.Tools.Architecture
{
    /// <summary>
    /// Test cases for the non-incremental reflexion analysis.
    /// </summary>
    internal class TestNonIncrementalReflexionAnalysis : TestReflexionAnalysis
    {
        //----------------------------
        // implementation nodes
        //----------------------------
        /// <summary>
        /// Node "n1" in implementation.
        /// </summary>
        private Node n1;
        /// <summary>
        /// Node "n1_c1" in implementation.
        /// </summary>
        private Node n1_c1;
        /// <summary>
        /// Node "n1_c1_c2" in implementation.
        /// </summary>
        private Node n1_c1_c1;
        /// <summary>
        /// Node "n1_c1_c1" in implementation.
        /// </summary>
        private Node n1_c1_c2;
        /// <summary>
        /// Node "n1_c1_c2" in implementation.
        /// </summary>
        private Node n1_c2;
        /// <summary>
        /// Node "n2" in implementation.
        /// </summary>
        private Node n2;
        /// <summary>
        /// Node "n2_c1" in implementation.
        /// </summary>
        private Node n2_c1;
        /// <summary>
        /// Node "n3" in implementation.
        /// </summary>
        private Node n3;

        //----------------------------
        // architecture nodes
        //----------------------------
        /// <summary>
        /// Node "N1" in architecture.
        /// </summary>
        private Node N1;
        /// <summary>
        /// Node "N1_C1" in architecture.
        /// </summary>
        private Node N1_C1;
        /// <summary>
        /// Node "N1_C2" in architecture.
        /// </summary>
        private Node N1_C2;
        /// <summary>
        /// Node "N2" in architecture.
        /// </summary>
        private Node N2;
        /// <summary>
        /// Node "N2_C1" in architecture.
        /// </summary>
        private Node N2_C1;
        /// <summary>
        /// Node "N3" in architecture.
        /// </summary>
        private Node N3;

        /// <summary>
        /// Sets the implementation nodes (retrieved from impl graph) and architecture
        /// nodes (retrieved from arch graph).
        /// </summary>
        private void SetNodes()
        {
            // implementation nodes
            n1 = graph.GetNode("n1");
            n1_c1 = graph.GetNode("n1_c1");
            n1_c2 = graph.GetNode("n1_c2");
            n1_c1_c1 = graph.GetNode("n1_c1_c1");
            n1_c1_c2 = graph.GetNode("n1_c1_c2");
            n2 = graph.GetNode("n2");
            n2_c1 = graph.GetNode("n2_c1");
            n3 = graph.GetNode("n3");
            // architecture nodes
            N1 = graph.GetNode("N1");
            N1_C1 = graph.GetNode("N1_C1");
            N1_C2 = graph.GetNode("N1_C2");
            N2 = graph.GetNode("N2");
            N2_C1 = graph.GetNode("N2_C1");
            N3 = graph.GetNode("N3");
        }

        /// <summary>
        /// Resets the implementation nodes (retrieved from impl graph) and architecture
        /// nodes (retrieved from arch graph) to null.
        /// </summary>
        private void ClearNodes()
        {
            // implementation nodes
            n1 = null;
            n1_c1 = null;
            n1_c2 = null;
            n1_c1_c1 = null;
            n1_c1_c2 = null;
            n2 = null;
            n2_c1 = null;
            n3 = null;
            // architecture nodes
            N1 = null;
            N1_C1 = null;
            N1_C2 = null;
            N2 = null;
            N2_C1 = null;
            N3 = null;
        }

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
            AddImplementationNodeHierarchy();
            AddArchitecture();
            AddMapping();
            graph.Subscribe(this);
            SetNodes();
        }

        [TearDown]
        protected override void Teardown()
        {
            base.Teardown();
            ClearNodes();
        }

        /// <summary>
        /// Returns new architecture graph with the following node hierarchy:
        ///
        ///  N1
        ///   -- N1_C1
        ///   -- N1_C2
        ///  N2
        ///   -- N2_C1
        ///  N3
        ///
        /// edges:
        ///
        /// call(N1_C1, N2_C1)
        /// call(N2, N1)
        /// call(N3, N1_C2)
        /// call(N3, N2_C1) marked as Architecture.Is_Optional
        /// </summary>
        /// <returns>new architecture graph</returns>
        private void AddArchitecture()
        {
            // Root nodes
            N1 = NewNode(true, "N1", "Component");
            N2 = NewNode(true, "N2", "Component");
            N3 = NewNode(true, "N3", "Component");

            // Second level
            N1_C1 = NewNode(true, "N1_C1", "Component");
            N1_C2 = NewNode(true, "N1_C2", "Component");
            N1.AddChild(N1_C1);
            N1.AddChild(N1_C2);
            N2_C1 = NewNode(true, "N2_C1", "Component");
            N2.AddChild(N2_C1);

            NewEdge(N1_C1, N2_C1, call);
            NewEdge(N2, N1, call);
            Edge edge = NewEdge(N3, N2_C1, call);
            edge.SetToggle("Architecture.Is_Optional");
            NewEdge(N3, N1_C2, call);
        }

        /// <summary>
        /// Creates a graph with the following node hierarchy:
        ///  n1
        ///   -- n1_c1
        ///         -- n1_c1_c1
        ///         -- n1_c1_c2
        ///   -- n1_c2
        ///  n2
        ///   -- n2_c1
        ///  n3
        ///
        /// </summary>
        /// <returns>new implementation graph with a hierarchy of nodes</returns>
        private void AddImplementationNodeHierarchy()
        {
            // Root nodes
            n1 = NewNode(false, "n1");
            n2 = NewNode(false, "n2");
            n3 = NewNode(false, "n3");

            // Second level
            n1_c1 = NewNode(false, "n1_c1");
            n1_c2 = NewNode(false, "n1_c2");
            n1.AddChild(n1_c1);
            n1.AddChild(n1_c2);

            n2_c1 = NewNode(false, "n2_c1");
            n2.AddChild(n2_c1);

            // Third level
            n1_c1_c1 = NewNode(false, "n1_c1_c1");
            n1_c1_c2 = NewNode(false, "n1_c1_c2");
            n1_c1.AddChild(n1_c1_c1);
            n1_c1.AddChild(n1_c1_c2);
        }

        /// <summary>
        /// Returns a new mapping between implementation and architecture as follows:
        ///
        /// n1    -Maps_To-> N1
        /// n2    -Maps_To-> N2
        /// n3    -Maps_To-> N3
        /// n1_c1 -Maps_To-> N1_C1
        /// n1_c2 -Maps_To-> N1_C2
        /// n2_c1 -Maps_To-> N2_C1
        /// </summary>
        /// <returns>mapping from implementation onto architecture</returns>
        private void AddMapping()
        {
            NewEdge(n1, N1, MapsToType);
            NewEdge(n2, N2, MapsToType);
            NewEdge(n3, N3, MapsToType);
            NewEdge(n1_c1, N1_C1, MapsToType);
            NewEdge(n1_c2, N1_C2, MapsToType);
            NewEdge(n2_c1, N2_C1, MapsToType);
        }

        /// <summary>
        /// Returns the implementation graph created by AddImplementationNodeHierarchy()
        /// with the following additional edges:
        ///   call(n1, n2)
        ///   call(n2, n3)
        ///   call(n2, n3)
        /// </summary>
        /// <returns>implementation graph</returns>
        private void AddCallGraph()
        {
            AddImplementationNodeHierarchy();
            n1 = graph.GetNode("n1");
            n2 = graph.GetNode("n2");
            n3 = graph.GetNode("n3");
            NewEdge(n1, n2, call);
            NewEdge(n2, n3, call);
            NewEdge(n2, n3, call);
        }

        //-------------------
        // Implicitly allowed
        //-------------------

        private void CommonImplicitlyAllowed(Node fromImpl, Node toImpl)
        {
            // 1 propagated edge
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture, ignorePropagated: false);

            // 1 implicitly allowed propagated dependency
            Assert.That(IsImplicitlyAllowed(fromImpl, toImpl, call));
            // 4 absences
            Assert.That(IsAbsent(N2, N1, call));
            Assert.That(IsAbsent(N1_C1, N2_C1, call));
            Assert.That(IsAbsent(N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(N3, N2_C1, call));
            // 0 divergences
            AssertEventCountEquals<EdgeChange>(5);

            // 0 removed edges
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Architecture, ignorePropagated: false);
        }

        [Test]
        public void TestImplicitlyAllowed1()
        {
            // dependency between siblings not mapped
            NewEdge(n1_c1_c1, n1_c1_c2, call);
            graph.RunAnalysis();

            CommonImplicitlyAllowed(n1_c1_c1, n1_c1_c2);
        }

        [Test]
        public void TestImplicitlyAllowed2()
        {
            // self dependency for node not mapped
            NewEdge(n1_c1_c1, n1_c1_c1, call);
            graph.RunAnalysis();

            CommonImplicitlyAllowed(n1_c1_c1, n1_c1_c1);
        }

        [Test]
        public void TestImplicitlyAllowed3()
        {
            // self dependency for node mapped
            NewEdge(n1_c1, n1_c1, call);
            graph.RunAnalysis();

            CommonImplicitlyAllowed(n1_c1, n1_c1);
        }

        [Test]
        public void TestImplicitlyAllowed4()
        {
            // dependency to parent where source is not mapped and target is mapped
            NewEdge(n1_c1_c1, n1_c1, call);
            graph.RunAnalysis();

            CommonImplicitlyAllowed(n1_c1_c1, n1_c1);
        }

        //---------------------------
        // Access along the hierarchy
        //---------------------------

        private void CommonHierarchyAccess()
        {
            // 1 propagated edges
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture, ignorePropagated: false);

            // 4 absences
            Assert.That(IsAbsent(N2, N1, call));
            Assert.That(IsAbsent(N1_C1, N2_C1, call));
            Assert.That(IsAbsent(N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(N3, N2_C1, call));
            // 0 divergences
            AssertEventCountEquals<EdgeChange>(5);

            // 0 removed edges
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Architecture, ignorePropagated: false);
        }

        [Test]
        public void TestAllowedParentAccess1()
        {
            // dependency to parent where source and target are mapped explicitly
            NewEdge(n1_c1, n1, call);
            graph.RunAnalysis();

            // 1 implicitly allowed propagated dependencies
            Assert.That(IsImplicitlyAllowed(n1_c1, n1, call));
            CommonHierarchyAccess();
        }

        [Test]
        public void TestAllowedParentAccess2()
        {
            // dependency to parent where source is not mapped explicitly but
            // target is mapped explicitly
            NewEdge(n1_c1_c1, n1, call);
            graph.RunAnalysis();

            // 1 implicitly allowed propagated dependencies
            Assert.That(IsImplicitlyAllowed(n1_c1_c1, n1, call));
            CommonHierarchyAccess();
        }

        [Test]
        public void TestAllowedChildAccess1()
        {
            // dependency from parent to child where both are mapped explicitly
            NewEdge(n1_c1, n1_c1_c1, call);
            graph.RunAnalysis();

            // 1 implicitly allowed propagated dependencies
            Assert.That(IsImplicitlyAllowed(n1_c1, n1_c1_c1, call));
            CommonHierarchyAccess();
        }

        [Test]
        public void TestDisallowedChildAccess()
        {
            // dependency from parent to child where both are mapped explicitly
            NewEdge(n1, n1_c1, call);
            graph.RunAnalysis();

            // 1 disallowed propagated dependencies
            Assert.That(IsDivergent(n1, n1_c1, call));
            CommonHierarchyAccess();
        }

        //-------------------
        // Convergences
        //-------------------

        [Test]
        public void TestConvergences1()
        {
            // Note: only one propagated edge for the following two implementation dependencies
            // will be created that covers both.
            NewEdge(n1_c1_c1, n2_c1, call);
            NewEdge(n1_c1_c2, n2_c1, call);
            NewEdge(n2, n1_c2, call);
            NewEdge(n3, n2_c1, call);
            NewEdge(n3, n1_c2, call);
            graph.RunAnalysis();

            // 4 propagated edges
            AssertEventCountEquals<EdgeEvent>(4, ChangeType.Addition, ReflexionSubgraph.Architecture, ignorePropagated: false);

            // 4 convergences
            Assert.That(IsConvergent(N2, N1, call));
            Assert.That(IsConvergent(N1_C1, N2_C1, call));
            Assert.That(IsConvergent(N3, N1_C2, call));
            Assert.That(IsConvergent(N3, N2_C1, call));

            // 5 allowed propagated dependencies
            Assert.That(IsAllowed(n1_c1_c1, n2_c1, call));
            Assert.That(IsAllowed(n1_c1_c2, n2_c1, call));
            Assert.That(IsAllowed(n2, n1_c2, call));
            Assert.That(IsAllowed(n3, n2_c1, call));
            Assert.That(IsAllowed(n3, n1_c2, call));
            // 0 absences

            // 0 divergences
            
            AssertEventCountEquals<EdgeChange>(9);
            // 0 removed edges
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Architecture, ignorePropagated: false);
        }

        [Test]
        public void TestConvergences2()
        {
            NewEdge(n2, n1, call);
            graph.RunAnalysis();

            // 1 propagated edges
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture, ignorePropagated: false);

            // 1 convergences
            Assert.That(IsConvergent(N2, N1, call));
            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(n2, n1, call));
            // 3 absences
            Assert.That(IsAbsent(N1_C1, N2_C1, call));
            Assert.That(IsAbsent(N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(N3, N2_C1, call));
            // 0 divergences
            AssertEventCountEquals<EdgeChange>(5);
            // 0 removed edges
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Architecture, ignorePropagated: false);
        }

        private void CommonTestConvergences345()
        {
            // 1 propagated edges
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture, ignorePropagated: false);

            // 1 convergences
            Assert.That(IsConvergent(N2, N1, call));
            // 3 absences
            Assert.That(IsAbsent(N1_C1, N2_C1, call));
            Assert.That(IsAbsent(N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(N3, N2_C1, call));
            // 0 divergences
            AssertEventCountEquals<EdgeChange>(5);
            // 0 removed edges
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Architecture, ignorePropagated: false);
        }

        [Test]
        public void TestConvergences3()
        {
            NewEdge(n2_c1, n1_c2, call);
            graph.RunAnalysis();

            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(n2_c1, n1_c2, call));

            CommonTestConvergences345();
        }

        [Test]
        public void TestConvergences4()
        {
            NewEdge(n2_c1, n1_c1_c2, call);
            graph.RunAnalysis();

            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(n2_c1, n1_c1_c2, call));

            CommonTestConvergences345();
        }

        [Test]
        public void TestConvergences5()
        {
            NewEdge(n2_c1, n1, call);
            graph.RunAnalysis();

            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(n2_c1, n1, call));

            CommonTestConvergences345();
        }

        //-------------------
        // Divergences
        //-------------------

        private void CommonAbsences(int divergences = 1)
        {
            // 1 propagated edges
            AssertEventCountEquals<EdgeEvent>(1, ChangeType.Addition, ReflexionSubgraph.Architecture, ignorePropagated: false);

            // 4 absences
            Assert.That(IsAbsent(N2, N1, call));
            Assert.That(IsAbsent(N1_C1, N2_C1, call));
            Assert.That(IsAbsent(N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(N3, N2_C1, call));
            // 0 convergences
            AssertEventCountEquals<EdgeChange>(4 + divergences);

            // 0 removed edges
            AssertEventCountEquals<EdgeEvent>(0, ChangeType.Removal, ReflexionSubgraph.Architecture, ignorePropagated: false);
        }

        [Test]
        public void TestDivergences1()
        {
            NewEdge(n1, n2, call);
            graph.RunAnalysis();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(n1, n2, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences2()
        {
            NewEdge(n1_c1, n2, call);
            graph.RunAnalysis();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(n1_c1, n2, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences3()
        {
            NewEdge(n1_c1_c1, n2, call);
            NewEdge(n1_c1_c2, n2, call);
            graph.RunAnalysis();

            // 2 divergent propagated dependencies
            Assert.That(IsDivergent(n1_c1_c1, n2, call));
            Assert.That(IsDivergent(n1_c1_c2, n2, call));

            CommonAbsences(2);
        }

        [Test]
        public void TestDivergences4()
        {
            NewEdge(n1, n2_c1, call);
            graph.RunAnalysis();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(n1, n2_c1, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences5()
        {
            NewEdge(n1_c2, n2_c1, call);
            graph.RunAnalysis();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(n1_c2, n2_c1, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences6()
        {
            NewEdge(n1_c1_c1, n1_c2, call);
            NewEdge(n1_c1_c2, n1_c2, call);
            graph.RunAnalysis();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(n1_c1_c1, n1_c2, call));
            Assert.That(IsDivergent(n1_c1_c2, n1_c2, call));

            CommonAbsences(2);
        }
    }
}
