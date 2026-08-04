using System.Collections.Generic;
using System.Linq;
using System;
namespace Cypher
{
    public class MatchResult
    {
        public Dictionary<string, GraphElement> Variables { get; set; }

        public GraphEdge? Edge { get; set; }
        public GraphNode? Node { get; set; }

        public MatchResult(Dictionary<string, GraphElement> dict, GraphEdge? edge = null, GraphNode? node = null)
        {
            Variables = dict;
            Edge = edge;
            Node = node;
        }
    }
}
