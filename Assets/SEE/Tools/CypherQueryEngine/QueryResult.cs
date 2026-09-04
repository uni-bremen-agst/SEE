using System.Collections.Generic;
using System.Linq;
using System;
using SEE.DataModel.DG;

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

        /// <summary>
        /// Contains all GraphElements that are added to the rows
        /// Used to easier highlight all GraphElements in Table
        /// </summary>
        public HashSet<GraphElement> graphElementsInTable { get; set; } = new();

        // Constructor
        public QueryResult(List<string> columns, List<List<object>> rows)
        {
            Columns = columns;
            Rows = rows;
        }
        /*
        public List<GraphElement> graphElementsInTable()
        {
            List<GraphElement> result = new();
            foreach (List<object> row in this.Rows)
            {
                foreach (object obj in row)
                {
                    if (obj is GraphElement)
                    {
                        result.Add(obj);
                    }
                }
            }
            return result;
        }*/
    }
}
