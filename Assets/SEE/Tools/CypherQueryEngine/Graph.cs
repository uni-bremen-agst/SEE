/*
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public class Graph
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
    }

    public abstract class GraphElement
    {
        public int Id { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class GraphNode : GraphElement
    {
        public string Type { get; set; }

        public GraphNode(int id, string type)
        {
            Id = id;
            Type = type;
        }
        public override string ToString()
        {
            string result = $"{Id}, {Type}";
            return result;
        }
    }

    public class GraphEdge : GraphElement
    {
        public GraphNode From { get; set; }
        public GraphNode To { get; set; }

        public string Relation { get; set; }

        public GraphEdge(int id, GraphNode from, string relation, GraphNode to)
        {
            Id = id;
            From = from;
            Relation = relation;
            To = to;
        }

        public override string ToString()
        {
            string result = $"ID: {Id}, Relation: {Relation}, Start ID: {From.Id}, Goal ID: {To.Id}";
            return result;
        }
    }
}
*/
