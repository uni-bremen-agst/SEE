
using NUnit.Framework;
using Cypher;
using System.Collections.Generic;
using SEE.DataModel.DG;

namespace SEE.Utils
{
    internal class TestQueryExecutor
    {
        private Condition condition;
        private Node node;
        private Node node2;
        private Node node3;
        private Graph graph;
        private MatchResult match;

        [SetUp]
        public void SetUp()
        {
            node = new Node
            {
                ID = "n1",
                SourceName = "n1",
                Type = "Type1"
            };
            node2 = new Node
            {
                ID = "n2",
                SourceName = "n2",
                Type = "Type1"
            };

            node3 = new Node
            {
                ID = "n3",
                SourceName = "n3",
                Type = "Type2"
            };
            node.SetInt("TestInt", 10);
            node.SetString("Name", "Test");

            graph = new Graph("TESTPATH", "Test Graph");
            graph.AddNode(node);
            graph.AddNode(node2);
            graph.AddNode(node3);

            graph.AddEdge(node, node2, "call");
            graph.AddEdge(node, node2, "use");
            graph.AddEdge(node2, node3, "call");

            match = new(new Dictionary<string, GraphElement>
            {
                { "n", node }
            });
        }
        // CHECKCONDITION
        [Test]
        public void CheckCondition_GreaterThan_ReturnsTrue()
        {
            Condition c = new Condition(
                new Condition("n", "TestInt"),
                new Condition(3),
                ">"
            );

            Assert.IsTrue(c.CheckCondition(c, match));
        }
        [Test]
        public void CheckCondition_GreaterEquals_ReturnsTrue()
        {
            Condition c = new Condition(
                new Condition("n", "TestInt"),
                new Condition(3),
                ">="
            );

            Assert.IsTrue(c.CheckCondition(c, match));
        }

        [Test]
        public void CheckCondition_LessThan_ReturnsFalse()
        {
            Condition c = new Condition(
                new Condition("n", "TestInt"),
                new Condition(3),
                "<"
            );

            Assert.IsFalse(c.CheckCondition(c, match));
        }
        [Test]
        public void CheckCondition_LessEquals_ReturnsFalse()
        {
            Condition c = new Condition(
                new Condition("n", "TestInt"),
                new Condition(3),
                "<="
            );

            Assert.IsFalse(c.CheckCondition(c, match));
        }

        [Test]
        public void CheckCondition_EqualityInt_ReturnsTrue()
        {
            Condition c = new Condition(
                new Condition("n", "TestInt"),
                new Condition(10),
                "="
            );

            Assert.IsTrue(c.CheckCondition(c, match));
        }
        [Test]
        public void CheckCondition_EqualityStirng_ReturnsTrue()
        {
            Condition c = new Condition(
                new Condition("n", "Name"),
                new Condition("Test"),
                "="
            );

            Assert.IsTrue(c.CheckCondition(c, match));
        }

        [Test]
        public void CheckCondition_NotEqual_ReturnsFalse()
        {
            Condition c = new Condition(
                new Condition("n", "TestInt"),
                new Condition(3),
                "="
            );

            Assert.IsFalse(c.CheckCondition(c, match));
        }
        [Test]
        public void CheckCondition_AND_ReturnsTrue()
        {
            Condition c = new Condition(
                new Condition(new Condition("n", "TestInt"),new Condition(10),"="),
                new Condition(new Condition("n", "Name"),new Condition("Test"),"="),
                "AND"
            );

            Assert.IsTrue(c.CheckCondition(c, match));
        }
        [Test]
        public void CheckCondition_OR_ReturnsTrue()
        {
            Condition c = new Condition(
                new Condition(new Condition("n", "TestInt"),new Condition(10),"="),
                new Condition(new Condition("n", "Name"),new Condition("False"),"="),
                "OR"
            );

            Assert.IsTrue(c.CheckCondition(c, match));
        }
        [Test]
        public void CheckCondition_NOT_ReturnsTrue()
        {
            Condition c = new Condition(
                new Condition(new Condition("n", "TestInt"),new Condition(3),"="),
                null,
                "NOT"
            );

            Assert.IsTrue(c.CheckCondition(c, match));
        }
        [Test]
        public void CheckCondition_NonExistingProperty_ReturnsFalse()
        {
            Condition c = new(
                new Condition("n", "NonExistingProperty"),
                new Condition(10),
                "="
            );

            Assert.IsFalse(c.CheckCondition(c, match));
        }

        // FINDPATTERN
        [Test]
        public void FindPattern_SingleNode_ReturnsAllMatchingNodes()
        {
            Pattern pattern = new Pattern(new PatternNode("n", null), null, null);
            QueryExecutor qx = new();

            List<MatchResult> result = qx.FindPattern(graph, pattern);

            Assert.AreEqual(3, result.Count);
        }
        [Test]
        public void FindPattern_Relationship_ReturnsMatchingPattern()
        {
            Pattern pattern = new Pattern(
                new PatternNode("a", null),
                new PatternEdge(null, null),
                new PatternNode("b", null)
            );

            QueryExecutor qx = new();

            List<MatchResult> result = qx.FindPattern(graph, pattern);

            Assert.AreEqual(3, result.Count);
        }
        [Test]
        public void FindPattern_Relationship_NothingFound()
        {
            Pattern pattern = new Pattern(
                new PatternNode("a", null),
                new PatternEdge("c", "False"),
                new PatternNode("b", null)
            );

            QueryExecutor qx = new();

            List<MatchResult> result = qx.FindPattern(graph, pattern);

            Assert.AreEqual(0, result.Count);
        }
        [Test]
        public void FindPattern_Relationship_SpecificResultsFound()
        {
            // Arrange
            Pattern pattern = new Pattern(
                new PatternNode("a", null),
                new PatternEdge("c", "call"),
                new PatternNode("b", null)
            );

            QueryExecutor qx = new();

            List<MatchResult> result = qx.FindPattern(graph, pattern);

            Assert.AreEqual(2, result.Count);
        }

        // EXECUTERETURN
        [Test]
        public void ExecuteReturn_OnlyVariable_ReturnsGraphElement()
        {
            ReturnRequest request = new ReturnRequest(
                new List<ReturnColumn>
                {
                    new ReturnColumn("n", null)
                });

            QueryExecutor qx = new();

            QueryResult result = qx.ExecuteReturn(
                new List<MatchResult> { match }, request);
            Assert.AreEqual(1, result.Columns.Count);
            Assert.AreEqual("n", result.Columns[0]);
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreSame(node, result.Rows[0][0]);
            Assert.IsTrue(result.graphElementsInTable.Contains(node));
        }
        [Test]
        public void ExecuteReturn_Property_ReturnsPropertyValue()
        {

            ReturnRequest request = new ReturnRequest(
                new List<ReturnColumn>
                {
                    new ReturnColumn("n", "Name")
                });

            QueryExecutor qx = new();

            QueryResult result = qx.ExecuteReturn(
                new List<MatchResult> { match }, request);

            // Assert
            Assert.AreEqual("n.Name", result.Columns[0]);
            Assert.AreEqual(node.GetString("Name"), result.Rows[0][0]);
        }
        [Test]
        public void ExecuteReturn_VariableAndProperty_ReturnsBothColumns()
        {
            ReturnRequest request = new ReturnRequest(
                new List<ReturnColumn>
                {
                    new ReturnColumn("n", null),
                    new ReturnColumn("n", "Name")
                });

            QueryExecutor qx = new();

            QueryResult result = qx.ExecuteReturn(
                new List<MatchResult> { match }, request);

            Assert.AreEqual(2, result.Columns.Count);
            Assert.AreEqual("n", result.Columns[0]);
            Assert.AreEqual("n.Name", result.Columns[1]);
            Assert.AreSame(node, result.Rows[0][0]);
            Assert.AreEqual(node.GetString("Name"), result.Rows[0][1]);
        }
        [Test]
        public void ExecuteReturn_NonExistingProperty_ReturnsNullString()
        {
            ReturnRequest request = new ReturnRequest(
                new List<ReturnColumn>
                {
                    new ReturnColumn("n", "Fake")
                });

            QueryExecutor qx = new();

            QueryResult result = qx.ExecuteReturn(
                new List<MatchResult> { match }, request);

            Assert.AreEqual("null", result.Rows[0][0]);
        }
    }
}
