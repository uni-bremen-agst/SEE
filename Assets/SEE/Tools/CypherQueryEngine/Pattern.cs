using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public class Pattern
    {
        public PatternNode Start { get; set; }
        public PatternEdge? Relation { get; set; }
        public PatternNode? Goal { get; set; }

        public Pattern(PatternNode start, PatternEdge? r, PatternNode? goal)
        {
            Start = start;
            Relation = r;
            Goal = goal;
        }
    }

    public class PatternNode
    {
        public string? Variable { get; set; }
        public string? Type { get; set; }

        public PatternNode(string? v, string? t)
        {
            Variable = v;
            Type = t;
        }
    }

    public class PatternEdge
    {
        public string? Variable { get; set; }
        public string? Type { get; set; }

        public PatternEdge(string? v, string? t)
        {
            Variable = v;
            Type = t;
        }
    }
}
