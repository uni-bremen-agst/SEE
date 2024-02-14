using NUnit.Framework;
using System;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Tests for <see cref="MergeDiffGraphExtensions.MergeDiff"/>.
    /// </summary>
    internal class TestMergeDiffGraphExtension : TestGraphBase
    {
        private const float FloatValue = 1.0f;
        private const int IntValue = 1;
        private const string StringValue = "a";
        private const bool ToggleValue = true;

        private Graph graph;
        private Node n1;
        private Node n2;
        private Node n3;
        private Edge e1;
        private Edge e2;

        [SetUp]
        public void SetUp()
        {
            graph = NewEmptyGraph();
            n1 = NewNode(graph, "n1"); AddAttributes(n1);
            n2 = NewNode(graph, "n2"); AddAttributes(n2);
            n3 = NewNode(graph, "n3"); AddAttributes(n3);
            e1 = NewEdge(graph, n1, n2); AddAttributes(e1);
            e2 = NewEdge(graph, n2, n3); AddAttributes(e2);

            static void AddAttributes(GraphElement ge)
            {
                ge.SetFloat(FloatAttribute, FloatValue);
                ge.SetInt(IntAttribute, IntValue);
                ge.SetString(StringAttribute, StringValue);
                ge.SetToggle(ToggleAttribute, ToggleValue);
            }
        }

        [TearDown]
        public void TearDown()
        {
            graph = null;
            n1 = null;
            n2 = null;
            n3 = null;
            e1 = null;
            e2 = null;
        }

        /// <summary>
        /// New graph is null.
        /// </summary>
        [Test]
        public void TestNullNewGraph()
        {
            Graph newGraph = null;
            Graph oldGraph = NewEmptyGraph();
            Assert.Throws<ArgumentNullException>(() => newGraph.MergeDiff(oldGraph));
        }

        /// <summary>
        /// Merging null into a new graph.
        /// </summary>
        [Test]
        public void TestNullOldGraph()
        {
            Graph newGraph = graph;
            Graph oldGraph = null;
            newGraph.MergeDiff(oldGraph);
            Assert.AreEqual(3, newGraph.NodeCount);
            Assert.AreEqual(2, newGraph.EdgeCount);
        }

        #region toggle attribute

        [Test]
        public void TestToggleNodeAttributeOnNewGraph()
        {
            TestToggleAttributeOnNewGraph(graph, new(graph), n1);
        }

        [Test]
        public void TestToggleEdgeAttributeOnNewGraph()
        {
            TestToggleAttributeOnNewGraph(graph, new(graph), e1);
        }

        private void TestToggleAttributeOnNewGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            ge.UnsetToggle(ToggleAttribute);
            TestToggleAttribute(newGraph, oldGraph, ge, false);
        }

        [Test]
        public void TestToggleNodeAttributeOnOldGraph()
        {
            TestToggleAttributeOnOldGraph(graph, new(graph), n1);
        }

        [Test]
        public void TestToggleEdgeAttributeOnOldGraph()
        {
            TestToggleAttributeOnOldGraph(graph, new(graph), e1);
        }

        private void TestToggleAttributeOnOldGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            GraphElement oldGe = Pendant(oldGraph, ge);
            oldGe.UnsetToggle(ToggleAttribute);
            TestToggleAttribute(newGraph, oldGraph, ge, true);
        }

        private void TestToggleAttribute(Graph newGraph, Graph oldGraph, GraphElement ge, bool newValue)
        {
            newGraph.MergeDiff(oldGraph);
            Assert.AreEqual(newValue, ge.HasToggle(ToggleAttribute));
            Assert.AreEqual(!newValue, ge.HasToggle(ToggleAttribute + MergeDiffGraphExtension.AttributeOldValuePostfix));
        }

        #endregion

        #region string attribute

        [Test]
        public void TestStringNodeAttributeOnNewGraph()
        {
            TestStringAttributeOnNewGraph(graph, new(graph), n1);
        }

        [Test]
        public void TesStringEdgeAttributeOnNewGraph()
        {
            TestStringAttributeOnNewGraph(graph, new(graph), e1);
        }

        private void TestStringAttributeOnNewGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            const string newValue = "new value";
            ge.SetString(StringAttribute, newValue);
            TestAttribute(newGraph, oldGraph, ge, newValue, StringValue, StringAttribute,
                          (GraphElement ge, string m, out string v) => ge.TryGetString(m, out v));
        }

        [Test]
        public void TestStringNodeAttributeOnOldGraph()
        {
            TestStringAttributeOnOldGraph(graph, new(graph), n1);
        }

        [Test]
        public void TestStringEdgeAttributeOnOldGraph()
        {
            TestStringAttributeOnOldGraph(graph, new(graph), e1);
        }

        private void TestStringAttributeOnOldGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            const string oldValue = "new value";
            GraphElement oldGe = Pendant(oldGraph, ge);
            oldGe.SetString(StringAttribute, oldValue);
            TestAttribute(newGraph, oldGraph, ge, StringValue, oldValue, StringAttribute,
                          (GraphElement ge, string m, out string v) => ge.TryGetString(m, out v));
        }
        #endregion

        #region int attribute

        [Test]
        public void TestIntNodeAttributeOnNewGraph()
        {
            TestIntAttributeOnNewGraph(graph, new(graph), n1);
        }

        [Test]
        public void TestIntEdgeAttributeOnNewGraph()
        {
            TestIntAttributeOnNewGraph(graph, new(graph), e1);
        }

        private void TestIntAttributeOnNewGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            const int newValue = IntValue - 3;
            ge.SetInt(IntAttribute, newValue);
            TestAttribute(newGraph, oldGraph, ge, newValue, IntValue, IntAttribute,
                          (GraphElement ge, string m, out int v) => ge.TryGetInt(m, out v));
        }

        [Test]
        public void TestIntNodeAttributeOnOldGraph()
        {
            TestFloatAttributeOnOldGraph(graph, new(graph), n1);
        }

        [Test]
        public void TestIntEdgeAttributeOnOldGraph()
        {
            TestIntAttributeOnOldGraph(graph, new(graph), e1);
        }

        private void TestIntAttributeOnOldGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            const int oldValue = IntValue - 3;
            GraphElement oldGe = Pendant(oldGraph, ge);
            oldGe.SetInt(IntAttribute, oldValue);
            TestAttribute(newGraph, oldGraph, ge, IntValue, oldValue, IntAttribute,
                          (GraphElement ge, string m, out int v) => ge.TryGetInt(m, out v));
        }

        #endregion

        #region float attribute

        [Test]
        public void TestFloatNodeAttributeOnNewGraph()
        {
            TestFloatAttributeOnNewGraph(graph, new(graph), n1);
        }

        [Test]
        public void TestFloatEdgeAttributeOnNewGraph()
        {
            TestFloatAttributeOnNewGraph(graph, new(graph), e1);
        }

        private void TestFloatAttributeOnNewGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            const float newValue = FloatValue - 1;
            ge.SetFloat(FloatAttribute, newValue);
            TestAttribute(newGraph, oldGraph, ge, newValue, FloatValue, FloatAttribute,
                          (GraphElement ge, string m, out float v) => ge.TryGetFloat(m, out v));
        }

        [Test]
        public void TestFloatNodeAttributeOnOldGraph()
        {
            TestFloatAttributeOnOldGraph(graph, new(graph), n1);
        }

        [Test]
        public void TestFloatEdgeAttributeOnOldGraph()
        {
            TestFloatAttributeOnOldGraph(graph, new(graph), e1);
        }

        private void TestFloatAttributeOnOldGraph(Graph newGraph, Graph oldGraph, GraphElement ge)
        {
            const float oldValue = FloatValue - 1;
            GraphElement oldGe = Pendant(oldGraph, ge);
            oldGe.SetFloat(FloatAttribute, oldValue);
            TestAttribute(newGraph, oldGraph, ge, FloatValue, oldValue, FloatAttribute,
                          (GraphElement ge, string m, out float v) => ge.TryGetFloat(m, out v));
        }

        #endregion

        private delegate bool TryGet<T>(GraphElement graphElement, string attributeName, out T value);

        private void TestAttribute<T>(Graph newGraph, Graph oldGraph, GraphElement ge, T newValue, T oldValue,
                                      string attribute, TryGet<T> tryGet)
        {
            newGraph.MergeDiff(oldGraph);
            {
                Assert.IsTrue(tryGet(ge, attribute, out T value));
                Assert.AreEqual(newValue, value);
            }
            Assert.IsTrue(ge.HasToggle(ChangeMarkers.IsChanged));
            {
                Assert.IsTrue(tryGet(ge, attribute + MergeDiffGraphExtension.AttributeOldValuePostfix, out T value));
                Assert.AreEqual(oldValue, value);
            }
        }
    }
}
