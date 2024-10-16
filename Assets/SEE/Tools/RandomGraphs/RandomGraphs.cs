﻿using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Utils;
using SEE.Utils.Config;
using UnityEngine;

namespace SEE.Tools.RandomGraphs
{
    /// <summary>
    /// A descriptor defining the name of an attribute and the normal distribution
    /// (mean, standard deviation, minimum, maximum) from which to draw its values randomly.
    ///
    /// Note: This class must be serializable. The names of the attributes must
    /// be consistent with the string literals used to retrieved them in
    /// SEECityRandomEditor.
    /// </summary>
    [Serializable]
    public class RandomAttributeDescriptor : ConfigIO.IPersistentConfigItem
    {
        public RandomAttributeDescriptor()
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">name of the attribute</param>
        /// <param name="mean">mean of the distribution of the attribute's value range</param>
        /// <param name="standardDeviation">standard deviation of the distribution of the attribute's value range</param>
        /// <param name="minimum">minimum of the attribute's value range</param>
        /// <param name="maximum">maximum of the attribute's value range</param>
        public RandomAttributeDescriptor
            (string name,
            float mean,
            float standardDeviation,
            float minimum = 0,
            float maximum = float.PositiveInfinity)
        {
            Name = name;
            Mean = mean;
            StandardDeviation = standardDeviation;
            Minimum = minimum;
            Maximum = maximum;
        }
        /// <summary>
        /// Name of the attribute.
        /// </summary>
        [SerializeField]
        public string Name;
        /// <summary>
        /// The mean of the distribution of the attribute's value range.
        /// </summary>
        [SerializeField]
        public float Mean;
        /// <summary>
        /// The standard deviation of the distribution of the attribute's value range.
        /// </summary>
        [SerializeField]
        public float StandardDeviation;
        /// <summary>
        /// The minimum of the attribute's value range.
        /// </summary>
        public float Minimum = 0;
        /// <summary>
        /// The maximum of the attribute's value range.
        /// </summary>
        public float Maximum = float.PositiveInfinity;

        /// <summary>
        /// Label of <see cref="Name"/> in the configuration file.
        /// </summary>
        private const string nameLabel = "Name";
        /// <summary>
        /// Label of <see cref="Mean"/> in the configuration file.
        /// </summary>
        private const string meanLabel = "Mean";
        /// <summary>
        /// Label of <see cref="StandardDeviation"/> in the configuration file.
        /// </summary>
        private const string standardDeviationLabel = "StandardDeviation";
        /// <summary>
        /// Label of <see cref="Minimum"/> in the configuration file.
        /// </summary>
        private const string minimumLabel = "Minimum";
        /// <summary>
        /// Label of <see cref="Maximum"/> in the configuration file.
        /// </summary>
        private const string maximumLabel = "Maximum";

        /// <summary>
        /// <see cref="ConfigIO.IPersistentConfigItem.Save()"/>
        /// </summary>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Name, nameLabel);
            writer.Save(Mean, meanLabel);
            writer.Save(StandardDeviation, standardDeviationLabel);
            writer.Save(Minimum, minimumLabel);
            writer.Save(Maximum, maximumLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// <see cref="ConfigIO.IPersistentConfigItem.Restore()"/>
        /// </summary>
        public bool Restore(Dictionary<string, object> attributes, string label = "")
        {
            Dictionary<string, object> values;
            if (string.IsNullOrEmpty(label))
            {
                // no label given => attributes contains already the data to be restored
                values = attributes;
            }
            else if (attributes.TryGetValue(label, out object dictionary))
            {
                // label was given => attributes is a dictionary where we need to look up the data
                // using the label
                values = dictionary as Dictionary<string, object>;
            }
            else
            {
                // label was given, but attributes does not know it
                // => no data; we cannot restore the object
                return false;
            }

            bool result = ConfigIO.Restore(values, nameLabel, ref Name);
            result = ConfigIO.Restore(values, meanLabel, ref Mean) || result;
            result = ConfigIO.Restore(values, standardDeviationLabel, ref StandardDeviation) || result;
            result = ConfigIO.Restore(values, minimumLabel, ref Minimum) || result;
            result = ConfigIO.Restore(values, maximumLabel, ref Maximum) || result;
            return result;
        }
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
            NodeType = nodeType;
            NodeNumber = nodeNumber;
            EdgeType = edgeType;
            EdgeDensity = edgeDensity;
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

        private const string nodeTypeLabel = "NodeType";
        private const string edgeTypeLabel = "EdgeType";
        private const string nodeNumberLabel = "NodeNumber";
        private const string edgeDensityLabel = "EdgeDensity";

        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(NodeType, nodeTypeLabel);
            writer.Save(EdgeType, edgeTypeLabel);
            writer.Save(NodeNumber, nodeNumberLabel);
            writer.Save(EdgeDensity, edgeDensityLabel);
            writer.EndGroup();
        }

        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    ConfigIO.Restore(values, nodeTypeLabel, ref NodeType);
                    ConfigIO.Restore(values, edgeTypeLabel, ref EdgeType);
                    ConfigIO.Restore(values, nodeNumberLabel, ref NodeNumber);
                    ConfigIO.Restore(values, edgeDensityLabel, ref EdgeDensity);
                }
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
        private const int seed = 500;

        /// <summary>
        /// Creates and returns a randomly generated graph for the given constraints.
        /// </summary>
        /// <param name="leafConstraint">constraint for the leaf nodes to be generated</param>
        /// <param name="innerNodeConstraint">constraint for the inner nodes to be generated</param>
        /// <param name="leafAttributes">constraints for node attributes to be generated</param>
        /// <param name="printStatistics">if true statistics about the graph will be printed to the debugging console</param>
        /// <returns>a random graph fulfilling the given constraints</returns>
        public static Graph Create
            (Constraint leafConstraint,
             Constraint innerNodeConstraint,
             ICollection<RandomAttributeDescriptor> leafAttributes,
             bool printStatistics = false)
        {
            leafConstraint.Check();
            innerNodeConstraint.Check();
            // Randomly generated graph do not need to have a base path; that is why we use the empty string.
            Graph graph = new("");
            ICollection<Node> leaves = CreateLeaves(graph, leafConstraint, leafAttributes);
            ICollection<Edge> leafEdges = CreateEdges(graph, leaves, leafConstraint);
            IList<Node> innerNodes = CreateTree(graph, innerNodeConstraint);
            AssignLeaves(graph, leaves, innerNodes);
            ICollection<Edge> innerEdges = CreateEdges(graph, innerNodes, innerNodeConstraint);
            if (printStatistics)
            {
                PrintStatistics(graph, leaves.Count, leafEdges.Count, innerNodes.Count, innerEdges.Count);
            }
            return graph;
        }

        private static void PrintStatistics(Graph graph, int leavesCount, int leafEdgesCount, int innerNodesCount, int innerEdgesCount)
        {
            Debug.Log($"Number of nodes:       {graph.NodeCount}\n");
            Debug.Log($"Number of leaf nodes:  {leavesCount}\n");
            Debug.Log($"Number of inner nodes: {innerNodesCount}\n");

            Debug.Log($"Number of edges:       {graph.EdgeCount}\n");
            Debug.Log($"Number of leaf edges:  {leafEdgesCount}\n");
            Debug.Log($"Leaf edge density:     {leafEdgesCount / (float)leavesCount}\n");
            Debug.Log($"Number of inner edges: {innerEdgesCount}\n");
            Debug.Log($"Inner edge density:    {innerEdgesCount / (float)innerNodesCount}\n");

            Debug.Log($"Maximal tree depth:    {graph.MaxDepth}\n");
        }

        private static void AssignLeaves(Graph graph, IEnumerable<Node> leaves, IList<Node> innerNodes)
        {
            System.Random random = new System.Random(seed);
            foreach (Node leaf in leaves)
            {
                // Next(n) yields a non-negative number smaller than n.
                int index = random.Next(innerNodes.Count);
                // index is in range [0, innerNodes.Count-1]
                innerNodes[index].AddChild(leaf);
            }
        }

        private static IList<Node> CreateTree(Graph graph, Constraint innerNodeConstraint)
        {
            // Create the inner nodes
            IList<Node> innerNodes = new List<Node>(innerNodeConstraint.NodeNumber);
            for (int i = 1; i <= innerNodeConstraint.NodeNumber; i++)
            {
                innerNodes.Add(CreateNode(graph, "Inner~" + i, innerNodeConstraint.NodeType));
            }
            // Create the tree.
            int[] parent = RandomTrees.Random(innerNodeConstraint.NodeNumber, out int _);
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

        private static ICollection<Edge> CreateEdges(Graph graph, ICollection<Node> nodes, Constraint constraint)
        {
            System.Random random = new System.Random(seed);
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

        private static ICollection<Node> CreateLeaves(Graph graph, Constraint leafConstraint, ICollection<RandomAttributeDescriptor> leafAttributes)
        {
            ICollection<Node> leaves = CreateNodes(graph, leafConstraint.NodeNumber, "Leaf~", leafConstraint.NodeType);
            CreateAttributes(leaves, leafAttributes);
            return leaves;
        }

        private static void CreateAttributes(IEnumerable<Node> nodes, ICollection<RandomAttributeDescriptor> attributes)
        {
            System.Random random = new System.Random(seed);

            foreach (Node node in nodes)
            {
                foreach (RandomAttributeDescriptor attr in attributes)
                {
                    node.SetFloat(attr.Name,
                                  Mathf.Clamp(RandomGaussian(random, attr.Mean, attr.StandardDeviation),
                                  attr.Minimum, attr.Maximum));
                }
            }
        }

        private static ICollection<Node> CreateNodes(Graph graph, int numberOfNodes, string linkPrefix, string nodeType)
        {
            ICollection<Node> nodes = new List<Node>();
            for (int i = 1; i <= numberOfNodes; i++)
            {
                nodes.Add(CreateNode(graph, linkPrefix + i, nodeType));
            }
            return nodes;
        }

        private static Node CreateNode(Graph graph, string linkName, string type)
        {
            Node result = new()
            {
                ID = linkName,
                SourceName = linkName,
                Type = type
            };
            result.SetString(Node.LinknameAttribute, linkName);
            graph.AddNode(result);
            return result;
        }


        /// <summary>
        /// Returns a random number drawn from a normal distribution with given
        /// <paramref name="mean"/> and <paramref name="standardDeviation"/>.
        /// </summary>
        /// <param name="mean">mean of the normal distribution</param>
        /// <param name="standardDeviation">standard deviation of the normal distribution</param>
        /// <returns>random number drawn from a normal distribution</returns>
        private static float RandomGaussian(System.Random random, float mean, float standardDeviation)
        {
            // Using two random variables, you can generate random values along a Gaussian
            // distribution using the Box-Muller transformation.
            // The method requires sampling from a uniform random of (0,1]
            // but Random.NextDouble() returns a sample of [0,1).
            double x1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
            double x2 = 1.0 - random.NextDouble();

            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2); //random normal(0,1)
            return (float)y1 * standardDeviation + mean; //random normal(mean,stdDev^2)
        }
    }
}