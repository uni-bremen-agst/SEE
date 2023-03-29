using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Tests for <see cref="GraphExtensions.Diff"/>.
    /// </summary>
    class TestGraphDiff : TestGraphBase
    {
        /// <summary>
        /// Name of the toggle attribute.
        /// </summary>
        private const string ToggleAttribute = "Toggle";
        /// <summary>
        /// Name of the float attribute.
        /// </summary>
        private const string FloatAttribute = "Float";
        /// <summary>
        /// Name of the int attribute.
        /// </summary>
        private const string IntAttribute = "Int";
        /// <summary>
        /// Name of the string attribute.
        /// </summary>
        private const string StringAttribute = "String";

        /// <summary>
        /// Returns a new <see cref="GraphElementDiff"/> that considers all
        /// numeric node attributes
        /// </summary>
        /// <param name="graphs"></param>
        /// <returns></returns>
        private static GraphElementDiff AttributeDiff(params Graph[] graphs)
        {
            ISet<string> floatAttributes = new HashSet<string>();
            ISet<string> intAttributes = new HashSet<string>();
            ISet<string> stringAttributes = new HashSet<string>();
            ISet<string> toggleAttributes = new HashSet<string>();
            graphs.ToList().ForEach(graph =>
            {
                if (graph != null)
                {
                    floatAttributes.UnionWith(graph.AllFloatGraphElementAttributes());
                    intAttributes.UnionWith(graph.AllIntGraphElementAttributes());
                    stringAttributes.UnionWith(graph.AllStringGraphElementAttributes());
                    toggleAttributes.UnionWith(graph.AllToggleGraphElementAttributes());
                }
            });
            return new AttributeDiff(floatAttributes, intAttributes, stringAttributes, toggleAttributes);
        }

        /// <summary>
        /// Two non-empty graphs are compared. Once with and once without attributes.
        /// </summary>
        [Test]
        public void TwoNonEmptyGraphs()
        {
            Test(true);
            Test(false);

            static void Test(bool withAttributes)
            {
                CreateGraphs(out Graph g1, out Graph g2,
                             out Node only_g1,  out Node only_g2,  out Edge e_only_g1,
                             out Node n1_in_g1, out Node n1_in_g2, out Node n2_in_g1,
                             out Node n2_in_g2, out Edge e_in_g1,  out Edge e_in_g2,
                             withAttributes);

                Check(g1, g2,
                      expectedAdded:   S(only_g2), expectedRemoved: S(only_g1),
                      expectedChanged: S<Node>(),  expectedEqual: S(n1_in_g2, n2_in_g2));
                Check(g2, g1,
                      expectedAdded:   S(only_g1), expectedRemoved: S(only_g2),
                      expectedChanged: S<Node>(),  expectedEqual: S(n1_in_g1, n2_in_g1));

                Check(g1, g2,
                      expectedAdded:   S<Edge>(), expectedRemoved: S(e_only_g1),
                      expectedChanged: S<Edge>(), expectedEqual:   S(e_in_g2));
                Check(g2, g1,
                      expectedAdded:   S(e_only_g1), expectedRemoved: S<Edge>(),
                      expectedChanged: S<Edge>(),    expectedEqual: S(e_in_g1));
            }
        }

        /// <summary>
        /// Two non-empty graphs are compared where nodes and edges have attributes.
        /// The two graphs differ in terms of a toggle attribute.
        /// </summary>
        [Test]
        public void TestToggleAttribute()
        {
            CreateGraphs(out Graph g1, out Graph g2,
                         out Node only_g1, out Node only_g2, out Edge e_only_g1,
                         out Node n1_in_g1, out Node n1_in_g2, out Node n2_in_g1,
                         out Node n2_in_g2, out Edge e_in_g1, out Edge e_in_g2,
                         true);


            e_in_g2.UnsetToggle(ToggleAttribute);
            n1_in_g1.UnsetToggle(ToggleAttribute);

            CheckWithChangedAttributes(g1, g2, only_g1, only_g2, e_only_g1,
                n1_in_g1, n1_in_g2, n2_in_g1, n2_in_g2, e_in_g1, e_in_g2);
        }

        /// <summary>
        /// Checks the difference for the case that there are attribute changes in
        /// <paramref name="e_in_g1"/> or <paramref name="e_in_g2"/>, respectively,
        /// and <paramref name="n1_in_g1"/> or <paramref name="n1_in_g2"/>, respectively.
        /// </summary>
        /// <param name="g1">first graph</param>
        /// <param name="g2">second graph</param>
        /// <param name="only_g1">node contained only in <paramref name="g1"/></param>
        /// <param name="only_g2">node contained only in <paramref name="g2"/></param>
        /// <param name="e_only_g1">edge contained only in <paramref name="g2"/></param>
        /// <param name="n1_in_g1">node in <paramref name="g1"/> corresponding to <paramref name="n1_in_g2"/></param>
        /// <param name="n1_in_g2">node in <paramref name="g2"/> corresponding to <paramref name="n1_in_g1"/></param>
        /// <param name="n2_in_g1">node in <paramref name="g1"/> corresponding to <paramref name="n2_in_g2"/></param>
        /// <param name="n2_in_g2">node in <paramref name="g2"/> corresponding to <paramref name="n2_in_g1"/></param>
        /// <param name="e_in_g1">edge in <paramref name="g1"/> corresponding to <paramref name="e_in_g2"/></param>
        /// <param name="e_in_g2">edge in <paramref name="g2"/> corresponding to <paramref name="e_in_g1"/></param>
        private static void CheckWithChangedAttributes
            (Graph g1, Graph g2, Node only_g1, Node only_g2, Edge e_only_g1,
            Node n1_in_g1, Node n1_in_g2, Node n2_in_g1, Node n2_in_g2,
            Edge e_in_g1, Edge e_in_g2)
        {
            Check(g1, g2,
                  expectedAdded: S(only_g2), expectedRemoved: S(only_g1),
                  expectedChanged: S(n1_in_g2), expectedEqual: S(n2_in_g2));
            Check(g2, g1,
                  expectedAdded: S(only_g1), expectedRemoved: S(only_g2),
                  expectedChanged: S(n1_in_g1), expectedEqual: S(n2_in_g1));

            Check(g1, g2,
                  expectedAdded: S<Edge>(), expectedRemoved: S(e_only_g1),
                  expectedChanged: S(e_in_g2), expectedEqual: S<Edge>());
            Check(g2, g1,
                  expectedAdded: S(e_only_g1), expectedRemoved: S<Edge>(),
                  expectedChanged: S(e_in_g1), expectedEqual: S<Edge>());
        }

        /// <summary>
        /// Two non-empty graphs are compared where nodes and edges have attributes.
        /// The two graphs differ in terms of a float attribute.
        /// </summary>
        [Test]
        public void TestFloatAttribute()
        {
            CreateGraphs(out Graph g1, out Graph g2,
                         out Node only_g1, out Node only_g2, out Edge e_only_g1,
                         out Node n1_in_g1, out Node n1_in_g2, out Node n2_in_g1,
                         out Node n2_in_g2, out Edge e_in_g1, out Edge e_in_g2,
                         true);


            e_in_g2.SetFloat(FloatAttribute, 2);
            n1_in_g1.SetFloat(FloatAttribute, 3);

            CheckWithChangedAttributes(g1, g2, only_g1, only_g2, e_only_g1,
                n1_in_g1, n1_in_g2, n2_in_g1, n2_in_g2, e_in_g1, e_in_g2);
        }

        /// <summary>
        /// Two non-empty graphs are compared where nodes and edges have attributes.
        /// The two graphs differ in terms of an integer attribute.
        /// </summary>
        [Test]
        public void TestIntAttribute()
        {
            CreateGraphs(out Graph g1, out Graph g2,
                         out Node only_g1, out Node only_g2, out Edge e_only_g1,
                         out Node n1_in_g1, out Node n1_in_g2, out Node n2_in_g1,
                         out Node n2_in_g2, out Edge e_in_g1, out Edge e_in_g2,
                         true);


            e_in_g2.SetInt(IntAttribute, 2);
            n1_in_g1.SetInt(IntAttribute, 3);

            CheckWithChangedAttributes(g1, g2, only_g1, only_g2, e_only_g1,
                n1_in_g1, n1_in_g2, n2_in_g1, n2_in_g2, e_in_g1, e_in_g2);
        }

        /// <summary>
        /// Two non-empty graphs are compared where nodes and edges have attributes.
        /// The two graphs differ in terms of an integer attribute.
        /// </summary>
        [Test]
        public void TestStringAttribute()
        {
            CreateGraphs(out Graph g1, out Graph g2,
                         out Node only_g1, out Node only_g2, out Edge e_only_g1,
                         out Node n1_in_g1, out Node n1_in_g2, out Node n2_in_g1,
                         out Node n2_in_g2, out Edge e_in_g1, out Edge e_in_g2,
                         true);


            e_in_g2.SetString(StringAttribute, "hello");
            n1_in_g1.SetString(StringAttribute, "world");

            CheckWithChangedAttributes(g1, g2, only_g1, only_g2, e_only_g1,
                n1_in_g1, n1_in_g2, n2_in_g1, n2_in_g2, e_in_g1, e_in_g2);
        }

        /// <summary>
        /// Graphs are compared to themselves. Once with and once without attributes.
        /// </summary>
        [Test]
        public void IdenticalGraphs()
        {
            Test(false);
            Test(true);

            static void Test(bool withAttributes)
            {
                CreateGraphs(out Graph g1, out Graph g2,
                         out Node only_g1, out Node only_g2, out Edge e_only_g1,
                         out Node n1_in_g1, out Node n1_in_g2, out Node n2_in_g1,
                         out Node n2_in_g2, out Edge e_in_g1, out Edge e_in_g2,
                         withAttributes);

                Check(g1, g1, S<Node>(), S<Node>(), S<Node>(), S(only_g1, n1_in_g1, n2_in_g1));
                Check(g2, g2, S<Node>(), S<Node>(), S<Node>(), S(only_g2, n1_in_g2, n2_in_g2));

                Check(g1, g1, S<Edge>(), S<Edge>(), S<Edge>(), S(e_only_g1, e_in_g1));
                Check(g2, g2, S<Edge>(), S<Edge>(), S<Edge>(), S(e_in_g2));
            }
        }

        /// <summary>
        /// Returns two graphs as follows:
        ///
        /// Graph <paramref name="g1"/> consists of:
        ///   nodes: <paramref name="only_g1"/>, <paramref name="n1_in_g1"/>, <paramref name="n2_in_g1"/>
        ///   edges: <paramref name="e_in_g1"/> from <paramref name="n1_in_g1"/> to <paramref name="n2_in_g1"/>
        ///      and <paramref name="e_only_g1"/> from <paramref name="only_g1"/> to <paramref name="only_g1"/>
        ///
        /// Graph <paramref name="g2"/> consists of:
        ///   nodes: <paramref name="only_g2"/>, <paramref name="n1_in_g2"/>, <paramref name="n2_in_g2"/>
        ///   edges: <paramref name="e_in_g2"/> from <paramref name="n1_in_g2"/> to <paramref name="n2_in_g2"/>
        ///
        /// with the following correspondences between the two graphs:
        ///    <paramref name="n1_in_g1"/> corresponds to <paramref name="n1_in_g2"/>
        ///    <paramref name="n2_in_g1"/> corresponds to <paramref name="n2_in_g2"/>
        ///     <paramref name="e_in_g1"/> corresponds to <paramref name="e_in_g2"/>
        ///
        /// whereas
        ///    <paramref name="only_g1"/> and <paramref name="e_only_g1"/> are contained only in <paramref name="g1"/>
        ///    and <paramref name="only_g2"/> is contained only in <paramref name="g2"/>.
        ///
        /// None of the nodes or edges has any attribute unless <paramref name="addAttributes"/> is true.
        /// </summary>
        /// <param name="g1">first graph</param>
        /// <param name="g2">second graph</param>
        /// <param name="only_g1">node contained only in <paramref name="g1"/></param>
        /// <param name="only_g2">node contained only in <paramref name="g2"/></param>
        /// <param name="e_only_g1">edge contained only in <paramref name="g2"/></param>
        /// <param name="n1_in_g1">node in <paramref name="g1"/> corresponding to <paramref name="n1_in_g2"/></param>
        /// <param name="n1_in_g2">node in <paramref name="g2"/> corresponding to <paramref name="n1_in_g1"/></param>
        /// <param name="n2_in_g1">node in <paramref name="g1"/> corresponding to <paramref name="n2_in_g2"/></param>
        /// <param name="n2_in_g2">node in <paramref name="g2"/> corresponding to <paramref name="n2_in_g1"/></param>
        /// <param name="e_in_g1">edge in <paramref name="g1"/> corresponding to <paramref name="e_in_g2"/></param>
        /// <param name="e_in_g2">edge in <paramref name="g2"/> corresponding to <paramref name="e_in_g1"/></param>
        /// <param name="addAttributes">if true, all nodes and edges will have attributes</param>
        private static void CreateGraphs
            (out Graph g1,
             out Graph g2,
             out Node only_g1,
             out Node only_g2,
             out Edge e_only_g1,
             out Node n1_in_g1,
             out Node n1_in_g2,
             out Node n2_in_g1,
             out Node n2_in_g2,
             out Edge e_in_g1,
             out Edge e_in_g2,
             bool addAttributes)
        {
            g1 = NewGraph();
            g2 = NewGraph();

            only_g1 = NewNode(g1, nameof(only_g1));
            only_g2 = NewNode(g2, nameof(only_g2));

            e_only_g1 = NewEdge(g1, only_g1, only_g1);

            n1_in_g1 = NewNode(g1, "N1");
            n1_in_g2 = NewNode(g2, "N1");

            n2_in_g1 = NewNode(g1, "N2");
            n2_in_g2 = NewNode(g2, "N2");

            e_in_g1 = NewEdge(g1, n1_in_g1, n2_in_g1);
            e_in_g2 = NewEdge(g2, n1_in_g2, n2_in_g2);

            if (addAttributes)
            {
                AddAttributes(only_g1, only_g2, e_only_g1, n1_in_g1, n1_in_g2, n2_in_g1, n2_in_g2, e_in_g1, e_in_g2);
            }
        }

        /// <summary>
        /// Adds the <see cref="ToggleAttribute"/>, <see cref="FloatAttribute"/>, <see cref="IntAttribute"/>,
        /// and <see cref="StringAttribute"/> to all <paramref name="elements"/>.
        /// </summary>
        /// <param name="elements">list of elements whose attributes are to be set</param>
        private static void AddAttributes(params GraphElement[] elements)
        {
            foreach (GraphElement element in elements)
            {
                element.SetToggle(ToggleAttribute);
                element.SetFloat(FloatAttribute, 1.0f);
                element.SetInt(IntAttribute, 1);
                element.SetString(StringAttribute, "string");
            }
        }

        /// <summary>
        /// Returns <paramref name="elements"/> as a set. Used as a shortcut.
        /// </summary>
        /// <typeparam name="T">type of <see cref="GraphElement"/></typeparam>
        /// <param name="elements">the elements to be turned into a list</param>
        /// <returns><paramref name="elements"/> as a set</returns>
        private static ISet<T> S<T>(params T[] elements) where T : GraphElement
        {
            return new HashSet<T>(elements);
        }

        /// <summary>
        /// Checks whether the difference of <paramref name="newGraph"/> relative to the
        /// baseline <paramref name="oldGraph"/> has the differences specified in the
        /// remaining parameters.
        ///
        /// This check is for the nodes only.
        /// </summary>
        /// <param name="oldGraph">baseline graph</param>
        /// <param name="newGraph">the new graph whose diff is to be checked relative to <paramref name="oldGraph"/></param>
        /// <param name="expectedAdded">expected added elements in the diff</param>
        /// <param name="expectedRemoved">expected removed elements in the diff</param>
        /// <param name="expectedChanged">expected changed elements in the diff (i.e., in both graphs, but changed)</param>
        /// <param name="expectedEqual">expected queal elements in the diff (i.e., unchanged in both graphs)</param>
        private static void Check(Graph oldGraph,
                                  Graph newGraph,
                                  ISet<Node> expectedAdded,
                                  ISet<Node> expectedRemoved,
                                  ISet<Node> expectedChanged,
                                  ISet<Node> expectedEqual)
        {
            newGraph.Diff(oldGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          AttributeDiff(oldGraph, newGraph),
                          new NodeEqualityComparer(),
                          out ISet<Node> added,
                          out ISet<Node> removed,
                          out ISet<Node> changed,
                          out ISet<Node> equal);
            AreEqual(expectedAdded, added);
            AreEqual(expectedRemoved, removed);
            AreEqual(expectedChanged, changed);
            AreEqual(expectedEqual, equal);
        }

        /// <summary>
        /// Checks whether the difference of <paramref name="newGraph"/> with respect to
        /// the baseline <paramref name="oldGraph"/> has the expected differences.
        /// </summary>
        /// <param name="oldGraph">baseline graph</param>
        /// <param name="newGraph">new graph to be compared against <paramref name="oldGraph"/></param>
        /// <param name="expectedAdded">expected added elements</param>
        /// <param name="expectedRemoved">expected removed elements</param>
        /// <param name="expectedChanged">expected changed elements</param>
        /// <param name="expectedEqual">expected equal elements</param>
        private static void Check(Graph oldGraph,
                                  Graph newGraph,
                                  ISet<Edge> expectedAdded,
                                  ISet<Edge> expectedRemoved,
                                  ISet<Edge> expectedChanged,
                                  ISet<Edge> expectedEqual)
        {
            newGraph.Diff(oldGraph,
                          g => g.Edges(),
                          (g, id) => g.GetEdge(id),
                          AttributeDiff(oldGraph, newGraph),
                          new EdgeEqualityComparer(),
                          out ISet<Edge> added,
                          out ISet<Edge> removed,
                          out ISet<Edge> changed,
                          out ISet<Edge> equal);
            AreEqual(expectedAdded, added);
            AreEqual(expectedRemoved, removed);
            AreEqual(expectedChanged, changed);
            AreEqual(expectedEqual, equal);
        }

        /// <summary>
        /// Checks the assertions that <paramref name="expected"/> and <paramref name="actual"/> are equal.
        /// </summary>
        /// <typeparam name="T"><type of <see cref="GraphElement"/>/typeparam>
        /// <param name="expected">expected value</param>
        /// <param name="actual">actual value</param>
        private static void AreEqual<T>(ISet<T> expected, ISet<T> actual) where T : GraphElement
        {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (GraphElement element in expected)
            {
                Assert.IsTrue(actual.Contains(element));
            }
        }

        /// <summary>
        /// Empty graph against null graph.
        /// </summary>
        [Test]
        public void EmptyAndNullGraphs()
        {
            CompareEmptyGraphs(NewGraph(), NewGraph());
            CompareEmptyGraphs(null, NewGraph());
            CompareEmptyGraphs(NewGraph(), null);
            CompareEmptyGraphs(null, null);
        }

        /// <summary>
        /// Compares <paramref name="right"/> to <paramref name="left"/>, and vice
        /// versa, where there are no added, no removed, no changed, and no equal
        /// graph elements are to be expected, in other words, for all combinations
        /// of empty and null graphs.
        /// </summary>
        /// <param name="left">to be compared to <paramref name="right"/>; assumed to
        /// be <c>null</c> or empty</param>
        /// <param name="right">to be compared to <paramref name="left"/>; assumed to
        /// be <c>null</c> or empty</param>
        private static void CompareEmptyGraphs(Graph left, Graph right)
        {
            // Node comparison.
            {
                right.Diff(left,
                              g => g.Nodes(),
                              (g, id) => g.GetNode(id),
                              AttributeDiff(left, right),
                              new NodeEqualityComparer(),
                              out ISet<Node> added,
                              out ISet<Node> removed,
                              out ISet<Node> changed,
                              out ISet<Node> equal);
                Assert.AreEqual(0, added.Count);
                Assert.AreEqual(0, removed.Count);
                Assert.AreEqual(0, changed.Count);
                Assert.AreEqual(0, equal.Count);
            }

            // Edge comparison.
            {
                right.Diff(left,
                              g => g.Edges(),
                              (g, id) => g.GetEdge(id),
                              AttributeDiff(left, right),
                              new EdgeEqualityComparer(),
                              out ISet<Edge> added,
                              out ISet<Edge> removed,
                              out ISet<Edge> changed,
                              out ISet<Edge> equal);
                Assert.AreEqual(0, added.Count);
                Assert.AreEqual(0, removed.Count);
                Assert.AreEqual(0, changed.Count);
                Assert.AreEqual(0, equal.Count);
            }
        }
    }
}
