using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public class ReturnRequest
    {
        public List<ReturnColumn> Requests { get; set; }

        public ReturnRequest(List<ReturnColumn> requests)
        {
            Requests = requests;
        }
    }

    public class ReturnColumn
    {
        public string Variable { get; set; }
        public string? Property { get; set; }

        public ReturnColumn(string variable, string? property)
        {
            Variable = variable;
            Property = property;
        }
    }
}
