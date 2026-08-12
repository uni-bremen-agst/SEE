using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    /// <summary>
    /// Contains the RETURN Clause in usable form.
    /// </summary>
    public class ReturnRequest
    {
        /// <summary>
        /// A list of Columns, later used in QueryResult
        /// </summary>
        public List<ReturnColumn> Requests { get; set; }

        // Constructor
        public ReturnRequest(List<ReturnColumn> requests)
        {
            Requests = requests;
        }
    }

    /// <summary>
    /// A Column of a table (QueryResult).
    /// Either only has a Variable or has both a Variable and Property.
    /// Used for title of Column
    /// </summary>
    public class ReturnColumn
    {
        /// <summary>
        /// Variable to search in MATCH Pattern
        /// </summary>
        public string Variable { get; set; }

        /// <summary>
        /// Determines which Property is searched for.
        /// </summary>
        public string? Property { get; set; }

        // Constructor
        public ReturnColumn(string variable, string? property)
        {
            Variable = variable;
            Property = property;
        }
    }
}
