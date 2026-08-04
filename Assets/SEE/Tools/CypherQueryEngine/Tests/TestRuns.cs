using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public class TestRuns
    {
        public List<TestCase> TestQueries { get; set; } = new();

        public void RunTests()
        {
            QueryExecutor executor = new();
            foreach (TestCase c in this.TestQueries)
            {
                Console.WriteLine("------------------------");
                Console.WriteLine($"Test Name: {c.Name}");
                try
                {
                    executor.ExecuteQuery(c.Graph, c.Query);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED: {ex.Message}");
                }
                Console.WriteLine();
            }
        }
        public void CreateTestList(Graph graph)
        {
            TestQueries = new();
            TestCase t;
            // Node Tests //////////////////////////////////////////
            t = new TestCase(
                "Node Test",
                graph,
                "MATCH (c) " +
                "RETURN c"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Node Typed Test",
                graph,
                "MATCH (c) " +
                "RETURN c.Name"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Full Node Test",
                graph,
                "MATCH (c) " +
                "WHERE c.Lines > 5 " +
                "RETURN c.Name, c.Lines"
            );
            TestQueries.Add(t);

            // Edge Tests //////////////////////////////////////////
            t = new TestCase(
                "Edge Test",
                graph,
                "MATCH (c:Class)-[r:CALLS]->(m:Method) " +
                "RETURN c.Name, m.Name"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Relation Return Test",
                graph,
                "MATCH (c)-[r]->(m) " +
                "RETURN r"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Edge Only start node Test",
                graph,
                "MATCH (c)-[]->() " +
                "RETURN c"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Edge Only goal node Test",
                graph,
                "MATCH ()-[]->(m) " +
                "RETURN m"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Relation Return Test 2",
                graph,
                "MATCH ()-[r]->() " +
                "RETURN r"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Edge with Where Test",
                graph,
                "MATCH (c)-[r:CALLS]->(m) " +
                "WHERE r.Name = fun " +
                "RETURN c.Name, r, m.Name"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Full Test",
                graph,
                "MATCH (c:Class)-[r:CALLS]->(m:Method) " +
                "WHERE m.Lines > 10 " +
                "RETURN c, r, m, c.Name, r.Name, m.Name"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Only Variables Test",
                graph,
                "MATCH (c)-[r]->(m) " +
                "RETURN c, r, m"
            );
            TestQueries.Add(t);

            // Exceptions Tests ///////////////////////////////
            t = new TestCase(
                "FAKE Relation Condition Test",
                graph,
                "MATCH (c:Class)-[r:CALLS]->(m:Method) " +
                "WHERE r.FAKE = fun " +
                "RETURN r"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Throw Exception Variable not Defined",
                graph,
                "MATCH (c) " +
                "RETURN c, r, m, c.Name, r.Name, m.Name"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Not supported Pattern Test",
                graph,
                "MATCH (c)-->(m) " +
                "RETURN c"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Fake Type in MATCH Test",
                graph,
                "MATCH (c:FAKE) " +
                "RETURN c"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "No Goal in Edge Pattern Test",
                graph,
                "MATCH (c)-[]-> " +
                "RETURN c"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "Double Variable Error Test",
                graph,
                "MATCH (c)-[m]->(m) " +
                "RETURN c, m"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "FAKE RETRUN Property Test",
                graph,
                "MATCH (c) " +
                "RETURN c.Name, c.Fake"
            );
            TestQueries.Add(t);

            t = new TestCase(
                "No Match",
                graph,
                "RETURN 0"
            );
            TestQueries.Add(t);
            /////////////////////////////////////////////////
        }
    }

    public class TestCase
    {
        public string Name { get; set; }
        public Graph Graph { get; set; }
        public string Query { get; set; }

        public TestCase(string name, Graph graph, string query)
        {
            Name = name;
            Graph = graph;
            Query = query;
        }
    }
}
