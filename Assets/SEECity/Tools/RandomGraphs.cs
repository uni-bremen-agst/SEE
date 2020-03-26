using SEE.DataModel;
using System.Collections.Generic;
using System;
using UnityEngine;

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
    /// A generator of random graphs based on the Erdős–Rényi model, where a given probability
    /// determines whether there is an edge between every pair of nodes.
    /// </summary>
    public class RandomGraphs
    {
        public RandomGraphs
            (string leafNodeType = "File", 
             string innerNodeType = "Directory", 
             string edgeType = "Source_Dependency")
        {
            this.leafNodeType = leafNodeType;
            this.innerNodeType = innerNodeType;
            this.edgeType = edgeType;
        }

        /// <summary>
        /// The node type to be used for new leaf nodes of the graph.
        /// </summary>
        private readonly string leafNodeType;

        /// <summary>
        /// The node type to be used for new inner nodes of the graph.
        /// </summary>
        private readonly string innerNodeType;

        /// <summary>
        /// The edge type to be used for new edges of the graph.
        /// </summary>
        private readonly string edgeType;

        public Graph Create(int numberOfNodes, float edgeDensity, ICollection<RandomAttributeDescriptor> leafAttributes)
        {
            if (edgeDensity < 0.0f || edgeDensity > 1.0f)
            {
                throw new Exception("Edge density must be between 0 and 1.");
            }
            Graph graph = new Graph();
            ICollection<Node> leaves = CreateLeaves(graph, numberOfNodes, leafAttributes);
            CreateEdges(graph, leaves, edgeDensity);
            return graph;
        }

        private void CreateEdges(Graph graph, ICollection<Node> leaves, float probability)
        {
            System.Random random = new System.Random();

            foreach (Node source in leaves)
            {
                foreach (Node target in leaves)
                {
                    if (random.NextDouble() < probability)
                    {
                        graph.AddEdge(new Edge(source, target, edgeType));
                    }
                }
            }
        }

        private ICollection<Node> CreateLeaves(Graph graph, int numberOfNodes, ICollection<RandomAttributeDescriptor> leafAttributes)
        {
            ICollection<Node> leaves = CreateNodes(graph, numberOfNodes, "Leaf~", leafNodeType);
            CreateAttributes(leaves, leafAttributes);
            return leaves;
        }

        private void CreateAttributes(ICollection<Node> nodes, ICollection<RandomAttributeDescriptor> attributes)
        {
            System.Random random = new System.Random();

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
                LinkName = linkname,
                Type = type
            };
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