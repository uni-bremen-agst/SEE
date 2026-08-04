using System.Collections.Generic;
using System.Linq;
using System;
/*
KI-Generierter Test Graph
*/

namespace Cypher
{
    public static class TestGraph
    {
        public static Graph Create()
        {
            Graph graph = new Graph();

            List<GraphNode> nodes = new List<GraphNode>
            {
                new GraphNode(1, ":Class")
                {
                    Properties =
                    {
                        {".Name", "PlayerController"},
                        {".Lines", 5}
                    }
                },

                new GraphNode(2, ":Class")
                {
                    Properties =
                    {
                        {".Name", "EnemyController"}
                    }
                },

                new GraphNode(3, ":Class")
                {
                    Properties =
                    {
                        {".Name", "GameManager"}
                    }
                },

                new GraphNode(4, ":Method")
                {
                    Properties =
                    {
                        {".Name", "Update"},
                        {".Lines", 20}
                    }
                },

                new GraphNode(5, ":Method")
                {
                    Properties =
                    {
                        {".Name", "Move"},
                        {".Lines", 8}
                    }
                },

                new GraphNode(6, ":Method")
                {
                    Properties =
                    {
                        {".Name", "CalculatePath"},
                        {".Lines", 35}
                    }
                },

                new GraphNode(7, ":Method")
                {
                    Properties =
                    {
                        {".Name", "Start"},
                        {".Lines", 12}
                    }
                },

                new GraphNode(8, ":Field")
                {
                    Properties =
                    {
                        {".Name", "Health"}
                    }
                },

                new GraphNode(9, ":Field")
                {
                    Properties =
                    {
                        {".Name", "Position"}
                    }
                },

                new GraphNode(10, ":Field")
                {
                    Properties =
                    {
                        {".Name", "NavMesh"}
                    }
                }
            };

            List<GraphEdge> edges = new List<GraphEdge>
            {
                new GraphEdge(1, nodes[0], ":CALLS", nodes[3]) {Properties ={{".Name", "fun"}}},
                new GraphEdge(2, nodes[0], ":CALLS", nodes[4]) {Properties ={{".Name", "fun"}}},

                new GraphEdge(3, nodes[1], ":CALLS", nodes[4]),
                new GraphEdge(4, nodes[1], ":CALLS", nodes[5]),

                new GraphEdge(5, nodes[2], ":CALLS", nodes[6]) {Properties ={{".Name", "nice"}}},

                new GraphEdge(6, nodes[3], ":READS", nodes[7]),
                new GraphEdge(7, nodes[4], ":WRITES", nodes[8]) {Properties ={{".Name", "fun"}}},
                new GraphEdge(8, nodes[5], ":READS", nodes[9])
            };

            graph.Nodes = nodes;
            graph.Edges = edges;

            return graph;
        }
    }
}
