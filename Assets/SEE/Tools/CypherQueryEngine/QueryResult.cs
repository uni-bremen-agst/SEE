using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public class QueryResult
    {
        public List<string> Columns { get; set; } // Die "Titel" der Spalte
        public List<List<object>> Rows { get; set; } // Alle Daten in den Reihen

        public QueryResult(List<string> columns, List<List<object>> rows)
        {
            Columns = columns;
            Rows = rows;
        }
    }
}
