using SEE.DataModel.DG;

namespace Cypher
{
    /// <summary>
    /// This class is used to invoke a search with Cypher.
    /// </summary>
    public class CypherQuery
    {
        /// <summary>
        /// Stores the query text.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// The graph to search in.
        /// </summary>
        public Graph SearchGraph;

        /// <summary>
        /// Constructor for the Query-Service.
        /// </summary>
        /// <param name="query">A Query in Cypher.</param>
        /// <param name="graph">The graph of the code city to search in.</param>
        public CypherQuery(string query, Graph graph)
        {
            Query = query;
            SearchGraph = graph;
            ParseTree pt = new ParseTree(query);
            ASTRoot resultRoot = pt.GetTypedTree();
        }

        /// <summary>
        /// The constructor constructs and transforms the syntax tree.
        /// This performs the actual search.
        /// </summary>
        /// <returns>A query result object.</returns>
        public QueryResult ProcessQuery()
        {
            // something stops the processing in the current HEAD/MASTER version of SEE
            return null;
        }


    }


}