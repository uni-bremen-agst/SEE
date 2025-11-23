using System.Data;

namespace Cypher
{
    /// <summary>
    /// This is used to create a result object for a cypher query.
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// Used to determine and handle the content of the result.
        /// </summary>
        public enum Content
        {
            Integer,
            Double,
            SingleResult,
            Table,
            Nodes,
            Edges,
            Graph
        }

        /// <summary>
        /// Visible indicator of the content.
        /// </summary>
        public Content ResultType { get; }

        /// <summary>
        /// DataTable to return a list of results.
        /// </summary>
        public DataTable ResultTable { get; }

        /// <summary>
        /// List of nodes of a result set.
        /// </summary>
        public object Nodes { get; }

        /// <summary>
        /// List of edges of a result set.
        /// </summary>
        public object Edges { get; }

        /// <summary>
        /// A single result use Content to return its type.
        /// </summary>
        public object SingleResult;

        /// <summary>
        /// Constructor used to store a single result.
        /// </summary>
        /// <param name="resultType">Used to type the result.</param>
        /// <param name="singleResult">Used to store the single result.</param>
        public QueryResult(Content resultType, object singleResult)
        {
            ResultType = resultType;
            SingleResult = singleResult;
        }

        /// <summary>
        /// Constructor used to return a table.
        /// </summary>
        /// <param name="table">The data table with data.</param>
        public QueryResult(DataTable table)
        {
            ResultType = Content.Table;
            ResultTable = table;
        }

        /// <summary>
        /// Constructor used to return a set of nodes.
        /// </summary>
        /// <param name="nodes">Nodes to return.</param>
        public QueryResult(object nodes)
        {
            ResultType = Content.Nodes;
            Nodes = nodes;
        }

        /// <summary>
        /// Used to return a set of edges.
        /// </summary>
        /// <param name="edges">Edges to return.</param>
        public QueryResult(string edges)
        {
            ResultType = Content.Edges;
            Edges = edges;
        }

        public QueryResult(object nodes, object edges)
        {
            ResultType = Content.Graph;
            Nodes = nodes;
            Edges = edges;
        }

        /// <summary>
        /// Used to return a single integer.
        /// </summary>
        /// <returns>Result number.</returns>
        public int? GetIntegerSingleResult()
        { 
            return ResultType == Content.Integer ? (int?)SingleResult : null;
        }

        /// <summary>
        /// Used to return a single double.
        /// </summary>
        /// <returns>Result number.</returns>
        public double? GetIntegerDoubleResult()
        {
            return ResultType == Content.Double ? (double?)SingleResult : null;
        }

        /// <summary>
        /// Used to return a single string.
        /// </summary>
        /// <returns>Result string.</returns>
        public string GetSingleResult()
        {
            return SingleResult as string;
        }
    }

}
