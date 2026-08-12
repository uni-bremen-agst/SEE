using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    /// <summary>
    /// Table as the Output of the Query
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// Name of the GraphResults its from.
        /// </summary>
        public string graphName { get; set; } = "";

        /// <summary>
        /// Is the Top Row on the table and contains the "Titles" of each Column
        /// </summary>
        public List<string> Columns { get; set; }

        /// <summary>
        /// 2-Dimensional List which contains the data.
        /// </summary>
        public List<List<object>> Rows { get; set; }

        // Constructor
        public QueryResult(List<string> columns, List<List<object>> rows)
        {
            Columns = columns;
            Rows = rows;
        }
    }
}
