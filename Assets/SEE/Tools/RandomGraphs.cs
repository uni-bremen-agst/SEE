﻿using SEE.DataModel;
using System.Collections.Generic;
using System;
using UnityEngine;
using SEE.Utils;
using System.Linq;

namespace SEE.Tools
{
    /// <summary>
    /// A descriptor defining the name of an attribute and the normal distribution
    /// (mean, standard deviation) from which to draw its values randomly.
    /// 
    /// Note: This class must be serializable. The names of the attributes must
    /// be consistent with the string literals used to retrieved them in 
    /// SeeCityRandomEditor.
    /// </summary>
    [Serializable]
    public class RandomAttributeDescriptor
    {
        public RandomAttributeDescriptor()
        { }

        public RandomAttributeDescriptor(string name, float mean, float standardDeviation)
        {
            this.Name = name;
            this.Mean = mean;
            this.StandardDeviation = standardDeviation;
        }
        [SerializeField]
        public string Name;
        [SerializeField]
        public float Mean;
        [SerializeField]
        public float StandardDeviation;
    }

    /// <summary>
    /// Specifies parameters for the random generation of leaf nodes and inner nodes and their edges.
    /// </summary>
    [Serializable]
    public class Constraint
    {
        /// <summary>
        /// The type of the nodes to be generated.
        /// </summary>
        public string NodeType;
        /// <summary>
        /// The type for edges connecting the nodes.
        /// </summary>
        public string EdgeType;
        /// <summary>
        /// The number of nodes to be generated.
        /// </summary>
        public int NodeNumber;
        /// <summary>
        /// The likelihood of an edge between two generated nodes. Must be in the
        /// range [0, 1].
        /// </summary>
        public float EdgeDensity;

        public Constraint()
        {
        }

        public Constraint(string nodeType, int nodeNumber, string edgeType, float edgeDensity)
        {
            this.NodeType = nodeType;
            this.NodeNumber = nodeNumber;
            this.EdgeType = edgeType;
            this.EdgeDensity = edgeDensity;
            Check();
        }

        internal void Check()
        {
            if (string.IsNullOrEmpty(NodeType))
            {
                throw new Exception("Node type must neither be null nor empty.");
            }
            else if (NodeNumber < 0)
            {
                throw new Exception("Number of nodes must be at least 0.");
            }
            else if (EdgeDensity < 0)
            {
                throw new Exception("Edge densitiy must be at least 0.");
            }
            else if (EdgeDensity > 1)
            {
                throw new Exception("Edge density must not be greater than 1.");
            }
        }
    }

    /// <summary>
    /// A generator of random graphs based on the Erdős–Rényi model, where a given probability
    /// determines whether there is an edge between every pair of nodes.
    /// </summary>
    public class RandomGraphs
    {
        /// <summary>
        /// The seed for the random generators. A fixed value to make the random generation
        /// deterministic.
        /// </summary>
        private const int Seed = 500;

        /// <summary>
        /// Creates and returns a randomly generated graph for the given constraints.
        /// </summary>
        /// <param name="leafConstraint">constraint for the leaf nodes to be generated</param>
        /// <param name="innerNodeConstraint">constraint for the inner nodes to be generated</param>
        /// <param name="leafAttributes">constraints for node attributes to be generated</param>
        /// <returns>a random graph fulfilling the given constraints</returns>
        public Graph Create
            (Constraint leafConstraint, 
             Constraint innerNodeConstraint, 
             ICollection<RandomAttributeDescriptor> leafAttributes)
        {
            leafConstraint.Check();
            innerNodeConstraint.Check();
            Graph graph = new Graph();
            ICollection<Node> leaves = CreateLeaves(graph, leafConstraint, leafAttributes);
            ICollection<Edge> leafEdges = CreateEdges(graph, leaves, leafConstraint);
            IList<Node> innerNodes = CreateTree(graph, innerNodeConstraint);
            AssignLeaves(graph, leaves, innerNodes);
            ICollection<Edge> innerEdges = CreateEdges(graph, innerNodes, innerNodeConstraint);
            graph.CalculateLevels();
            PrintStatistics(graph, leaves.Count, leafEdges.Count, innerNodes.Count, innerEdges.Count);
            return graph;
        }

        private void PrintStatistics(Graph graph, int leavesCount, int leafEdgesCount, int innerNodesCount, int innerEdgesCount)
        {
            Debug.LogFormat("Number of nodes:       {0}\n", graph.NodeCount);
            Debug.LogFormat("Number of leaf nodes:  {0}\n", leavesCount);
            Debug.LogFormat("Number of inner nodes: {0}\n", innerNodesCount);

            Debug.LogFormat("Number of edges:       {0}\n", graph.EdgeCount);
            Debug.LogFormat("Number of leaf edges:  {0}\n", leafEdgesCount);
            Debug.LogFormat("Leaf edge density:     {0}\n", (float)leafEdgesCount / (float)leavesCount);
            Debug.LogFormat("Number of inner edges: {0}\n", innerEdgesCount);
            Debug.LogFormat("Inner edge density:    {0}\n", (float)innerEdgesCount / (float)innerNodesCount);

            Debug.LogFormat("Maximal tree depth:    {0}\n", graph.GetMaxDepth());
        }

        private void AssignLeaves(Graph graph, ICollection<Node> leaves, IList<Node> innerNodes)
        {
            System.Random random = new System.Random(Seed);
            foreach (Node leaf in leaves)
            {
                // Next(n) yields a non-negative number smaller than n.
                int index = random.Next(innerNodes.Count);
                // index is in range [0, innerNodes.Count-1]
                innerNodes[index].AddChild(leaf);
            }
        }

        private IList<Node> CreateTree(Graph graph, Constraint innerNodeConstraint)
        {
            // Create the inner nodes
            IList<Node> innerNodes = new List<Node>(innerNodeConstraint.NodeNumber);
            for (int i = 1; i <= innerNodeConstraint.NodeNumber; i++)
            {
                innerNodes.Add(CreateNode(graph, "Inner~" + i, innerNodeConstraint.NodeType));
            }
            // Create the tree.
            int[] parent = RandomTrees.Random(innerNodeConstraint.NodeNumber, out int root);
            for (int i = 0; i < parent.Length; i++)
            {
                if (parent[i] != -1)
                {
                    // i is not the root; add i to its parent
                    innerNodes[parent[i]].AddChild(innerNodes[i]);
                }
            }
            return innerNodes;
        }

        private ICollection<Edge> CreateEdges(Graph graph, ICollection<Node> nodes, Constraint constraint)
        {
            System.Random random = new System.Random(Seed);
            ICollection<Edge> result = new List<Edge>();
            foreach (Node source in nodes)
            {
                foreach (Node target in nodes)
                {
                    if (constraint.EdgeDensity == 1 || random.NextDouble() < constraint.EdgeDensity)
                    {
                        Edge edge = new Edge(source, target, constraint.EdgeType);
                        result.Add(edge);
                        graph.AddEdge(edge);
                    }
                }
            }
            return result;
        }

        private ICollection<Node> CreateLeaves(Graph graph, Constraint leafConstraint, ICollection<RandomAttributeDescriptor> leafAttributes)
        {
            ICollection<Node> leaves = CreateNodes(graph, leafConstraint.NodeNumber, "Leaf~", leafConstraint.NodeType);
            CreateAttributes(leaves, leafAttributes);
            return leaves;
        }

        private void CreateAttributes(ICollection<Node> nodes, ICollection<RandomAttributeDescriptor> attributes)
        {
            System.Random random = new System.Random(Seed);

            foreach (Node node in nodes)
            {
                foreach (RandomAttributeDescriptor attr in attributes)
                {
                    node.SetFloat(attr.Name, RandomGaussian(random, attr.Mean, attr.StandardDeviation));
                }
            }
        }

        private ICollection<Node> CreateNodes(Graph graph, int numberOfNodes, string linkPrefix, string nodeType)
        {
            ICollection<Node> nodes = new List<Node>();
            for (int i = 1; i <= numberOfNodes; i++)
            {
                nodes.Add(CreateNode(graph, linkPrefix + i, nodeType));
            }
            return nodes;
        }

        private Node CreateNode(Graph graph, string linkname, string type)
        {
            Node result = new Node
            {
                ID = linkname,
                SourceName = linkname,
                Type = type
            };
            result.SetString(Node.LinknameAttribute, linkname);
            graph.AddNode(result);
            return result;
        }
        

        /// <summary>
        /// Returns a random number drawn from a normal distribution with given 
        /// <paramref name="mean"/> and standard deviation <paramref name="stddev"/>.
        /// </summary>
        /// <param name="mean">mean of the normal distribution</param>
        /// <param name="stddev">standard deviation of the normal distribution</param>
        /// <returns>random number drawn from a normal distribution</returns>
        private float RandomGaussian(System.Random random, float mean, float stddev)
        {
            // Using two random variables, you can generate random values along a Gaussian 
            // distribution using the Box-Muller transformation.
            // The method requires sampling from a uniform random of (0,1]
            // but Random.NextDouble() returns a sample of [0,1).
            double x1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
            double x2 = 1.0 - random.NextDouble();

            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2); //random normal(0,1)
            return (float)y1 * stddev + mean; //random normal(mean,stdDev^2)
        }
    }
}