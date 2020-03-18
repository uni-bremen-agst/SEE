using SEE.DataModel;
using System.Collections.Generic;

using UnityEngine;

namespace SEE.Layout
{

    /// <summary>
    /// Computes the lowest common ancestors (LCA) in a rooted tree or a forest based on
    /// the algorithm by Berkman, Omer and Vishkin, Uzi(1993), "Recursive Star-Tree 
    /// Parallel Data Structure", SIAM Journal on Computing, 22 (2): 221–242.
    /// </summary>
    internal class LCAFinder
    {
        private Graph graph;
        private IList<Node> roots;
        private uint maxLevel;

        private Dictionary<Node, int> nodeMap;
        private Node[] indexList;

        private int[] eulerTour;
        private int tourLength;

        private int numberComponent;
        private int[] tree;

        private int[] level;
        private int[] representative;

        private int[,] rmq;  // two-dimensional array
        private int[] log2;

        /// <summary>
        /// Runs the preprocessing step to find the LCA in O(|V| log(|V|)) time and space.
        /// Precondition: The graph forms a tree.
        /// </summary>
        /// <param name="graph">input graph (tree, really)</param>
        /// <param name="root">root of the tree</param>
        public LCAFinder(Graph graph, Node root)
        {
            IList<Node> roots = new List<Node>();
            roots.Add(root);
            Run(graph, roots);
        }

        /// <summary>
        /// Yields the floor of the binary logarithm of n.
        /// </summary>
        /// <param name="n">the input number</param>
        /// <returns>floor of the binary logarithm</returns>
        private static uint Log2(uint n)
        {
            n |= (n >> 1);
            n |= (n >> 2);
            n |= (n >> 4);
            n |= (n >> 8);
            n |= (n >> 16);

            return (uint)(NumBitsSet(n) - 1);
        }

        private static int NumBitsSet(uint x)
        {
            x -= ((x >> 1) & 0x55555555);
            x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
            x = (((x >> 4) + x) & 0x0f0f0f0f);
            x += (x >> 8);
            x += (x >> 16);

            return (int)(x & 0x0000003f);
        }

        /// <summary>
        /// Runs the preprocessing step to find the LCA in O(|V| log(|V|)) time and space.
        /// Precondition: The graph forms a forest.
        /// </summary>
        /// <param name="graph">input graph (forest, really)</param>
        /// <param name="root">roots of the forest</param>
        public LCAFinder(Graph graph, IList<Node> roots)
        {
            Run(graph, roots);
        }

        /// <summary>
        /// Runs the preprocessing step to find the LCA in O(|V| log(|V|)) time and space.
        /// Precondition: The graph forms a forest.
        /// </summary>
        /// <param name="graph">input graph (forest, really)</param>
        /// <param name="root">roots of the forest</param>
        private void Run(Graph graph, IList<Node> roots)
        {
            this.graph = graph;
            this.maxLevel = 1 + Log2((uint)graph.Nodes().Count);
            this.roots = roots;

            if (this.roots.Count == 0)
            {
                Debug.LogError("LCAFinder: empty set of roots.\n");
            }
            else
            {
                DetermineEulerTours();
            }
        }

        /// <summary>
        /// Creates the mapping of nodes onto integers and the index list.
        /// </summary>
        private void MapAllNodes()
        {
            NodeToIntegerMap nodeToIntegerMapping = new NodeToIntegerMap(graph.Nodes());
            nodeMap = nodeToIntegerMapping.NodeMap();
            indexList = nodeToIntegerMapping.IndexList();
            //nodeToIntegerMapping.Dump();
        }

        /// <summary>
        /// Euler tour using depth first traversal.
        /// </summary>
        /// <param name="n">node to be visited</param>
        /// <param name="level">the starting level in the tree</param>
        private void DepthFirstTraversal(int n, int level)
        {
            // List of nodes already visited
            HashSet<int> visited = new HashSet<int>();

            Stack<Pair<int, int>> stack = new Stack<Pair<int, int>>();
            stack.Push(Pair<int, int>.of(n, level));

            while (stack.Count > 0)
            {
                Pair<int, int> pair = stack.Pop();
                n = pair.getFirst();
                int lvl = pair.getSecond();

                if (!visited.Contains(n))
                {
                    visited.Add(n);

                    tree[n] = numberComponent;
                    eulerTour[tourLength] = n;
                    this.level[tourLength] = lvl;
                    tourLength++;

                    Node node = indexList[n];
                    foreach (Node edge in node.Children())
                    {
                        nodeMap.TryGetValue(edge, out int child);

                        if (!visited.Contains(child))
                        {
                            stack.Push(pair);
                            stack.Push(Pair<int, int>.of(child, lvl + 1));
                        }
                    }
                }
                else
                {
                    eulerTour[tourLength] = n;
                    this.level[tourLength] = lvl;
                    tourLength++;
                }
            }
        }

        /// <summary>
        /// Determines the RMQ. See the paper.
        /// </summary>
        private void DetermineRMQ()
        {
            rmq = new int[maxLevel + 1, tourLength];
            log2 = new int[tourLength + 1];

            for (int i = 0; i < tourLength; i++)
            {
                rmq[0, i] = i;
            }

            for (int i = 1; (1 << i) <= tourLength; i++)
            {
                for (int j = 0; j + (1 << i) - 1 < tourLength; j++)
                {
                    int p = 1 << (i - 1);

                    if (level[rmq[i - 1, j]] < level[rmq[i - 1, j + p]])
                    {
                        rmq[i, j] = rmq[i - 1, j];
                    }
                    else
                    {
                        rmq[i, j] = rmq[i - 1, j + p];
                    }
                }
            }

            for (int i = 2; i <= tourLength; ++i)
            {
                log2[i] = log2[i / 2] + 1;
            }
        }

        /// <summary>
        /// Determines the Euler tours for all subtrees rooted by any of the given roots.
        /// Computes eulerTour, level, and representative.
        /// </summary>
        private void DetermineEulerTours()
        {
            MapAllNodes();

            int numberOfNodes = graph.Nodes().Count;
            eulerTour = new int[2 * numberOfNodes];
            level = new int[2 * numberOfNodes];
            representative = new int[numberOfNodes];

            numberComponent = 0;
            tree = new int[numberOfNodes];

            foreach (Node root in roots)
            {
                nodeMap.TryGetValue(root, out int u);

                if (tree[u] == 0)
                {
                    numberComponent++;
                    DepthFirstTraversal(u, -1);
                }
                else
                {
                    Debug.LogError("LCSFinder: multiple roots in the same tree.\n");
                    throw new System.Exception("LCSFinder: multiple roots in the same tree.");
                }
            }

            // TODO: .Net Standard 2.1 will provide Array.Fill(). Then we can
            // replace this code.
            // Array.Fill<int>(representative, -1);
            for (int i = 0; i < representative.Length; i++)
            {
                representative[i] = -1;
            }
            for (int i = 0; i < tourLength; i++)
            {
                if (representative[eulerTour[i]] == -1)
                {
                    representative[eulerTour[i]] = i;
                }
            }

            DetermineRMQ();
        }

        /// <summary>
        /// Returns the lowest common ancestor of the two given nodes in the tree.
        /// May return null if the two nodes are in different trees.
        /// </summary>
        /// <param name="nodeA">first node</param>
        /// <param name="nodeB">second</param>
        /// <returns></returns>
        public Node LCA(Node nodeA, Node nodeB)
        {
            if (!nodeMap.TryGetValue(nodeA, out int indexOfA))
            {
                throw new System.Exception("invalid vertex: " + nodeA);
            }
            if (!nodeMap.TryGetValue(nodeB, out int indexOfB))
            {
                throw new System.Exception("invalid vertex: " + nodeB);
            }
            // Check if a == b because lca(a, a) == a
            if (nodeA.Equals(nodeB))
            {
                return nodeA;
            }

            // If a and b are in different components, they do not have an LCA
            if (tree[indexOfA] != tree[indexOfB] || tree[indexOfA] == 0)
            {
                return null;
            }
            indexOfA = representative[indexOfA];
            indexOfB = representative[indexOfB];

            if (indexOfA > indexOfB)
            {
                int swap = indexOfA;
                indexOfA = indexOfB;
                indexOfB = swap;
            }

            int l = log2[indexOfB - indexOfA + 1];
            int pwl = 1 << l;
            int sol = rmq[l, indexOfA];

            if (level[sol] > level[rmq[l, indexOfB - pwl + 1]])
            {
                sol = rmq[l, indexOfB - pwl + 1];
            }
            return indexList[eulerTour[sol]];
        }

    }
}
