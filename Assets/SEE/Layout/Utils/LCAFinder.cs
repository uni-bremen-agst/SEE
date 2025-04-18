﻿using System;
using System.Collections.Generic;

namespace SEE.Layout.Utils
{
    /// <summary>
    /// Computes the lowest common ancestors (LCA) in a rooted tree or a forest based on
    /// the algorithm by Berkman, Omer and Vishkin, Uzi (1993), "Recursive Star-Tree
    /// Parallel Data Structure", SIAM Journal on Computing, 22 (2): 221–242.
    /// </summary>
    public class LCAFinder<HNode> where HNode : IHierarchyNode<HNode>
    {
        /// <summary>
        /// The maximum depth of the tree.
        /// </summary>
        private uint maxLevel;

        /// <summary>
        /// Mapping of nodes onto their tree index.
        /// </summary>
        private Dictionary<HNode, int> nodeMap;

        /// <summary>
        /// The list of nodes in the tree.
        /// </summary>
        private HNode[] indexList;

        /// <summary>
        /// The Euler tour through the tree.
        /// </summary>
        private int[] eulerTour;
        /// <summary>
        /// The length of the Euler tour.
        /// </summary>
        private int tourLength;

        /// <summary>
        /// The component number of each node in the Euler tour.
        /// </summary>
        private int numberComponent;
        /// <summary>
        /// The tree number of each node in the Euler tour.
        /// </summary>
        private int[] tree;

        /// <summary>
        /// The level of each node in the Euler tour.
        /// </summary>
        private int[] level;
        /// <summary>
        /// The representative of each node in the Euler tour.
        /// </summary>
        private int[] representative;

        /// <summary>
        /// The two-dimensional Range Minimum Query (RMQ) data structure.
        /// </summary>
        private int[,] rmq;
        /// <summary>
        /// The logarithm base 2 of the length of the Euler tour.
        /// </summary>
        private int[] log2;

        /// <summary>
        /// Runs the preprocessing step to find the LCA in O(|V| log(|V|)) time and space.
        /// Precondition: The descendants of <paramref name="root"/> form a tree.
        /// </summary>
        /// <param name="root">root of the tree</param>
        public LCAFinder(HNode root)
        {
            if (root == null)
            {
                throw new ArgumentNullException("Root must not be null.");
            }
            ICollection<HNode> roots = new List<HNode>
            {
                root
            };
            Run(roots);
        }

        /// <summary>
        /// Runs the preprocessing step to find the LCA in O(|V| log(|V|)) time and space.
        /// Precondition: The roots and their descendants form a forest (non-cyclic; every nodes
        /// has at most one parent).
        /// </summary>
        /// <param name="root">roots of the forest</param>
        /// <exception cref="Exception">thrown if <paramref name="roots"/> is empty</exception>
        public LCAFinder(ICollection<HNode> roots)
        {
            if (roots.Count == 0)
            {
                throw new Exception("Empty set of roots given.");
            }
            Run(roots);
        }

        /// <summary>
        /// Runs the preprocessing step to find the LCA in O(|V| log(|V|)) time and space.
        /// </summary>
        /// <param name="roots">roots of the forest</param>
        private void Run(ICollection<HNode> roots)
        {
            // CheckTree(roots);
            int numberOfNodes = MapAllNodes(roots);
            maxLevel = 1 + Log2((uint)numberOfNodes);
            DetermineEulerTours(roots, numberOfNodes);
        }

        /// <summary>
        /// Creates the mapping of nodes onto integers and the index list.
        /// </summary>
        /// <param name="roots">roots of the forest</param>
        private int MapAllNodes(ICollection<HNode> roots)
        {
            ICollection<HNode> allNodes = AllNodes(roots);
            NodeToIntegerMap nodeToIntegerMapping = new(allNodes);
            nodeMap = nodeToIntegerMapping.NodeMap();
            indexList = nodeToIntegerMapping.IndexList();
            return allNodes.Count;
        }

        /// <summary>
        /// Gathers all nodes: <paramref name="roots"/> and their transitive descendants.
        /// </summary>
        /// <param name="roots">list of root nodes</param>
        /// <returns><paramref name="roots"/> and their transitive descendants</returns>
        private static ICollection<HNode> AllNodes(ICollection<HNode> roots)
        {
            List<HNode> result = new(roots);
            Queue<HNode> todos = new(roots);
            while (todos.Count > 0)
            {
                HNode node = todos.Dequeue();
                ICollection<HNode> children = node.Children();
                result.AddRange(children);
                foreach (HNode child in children)
                {
                    todos.Enqueue(child);
                }
            }
            return result;
        }

        /// <summary>
        /// Euler tour using depth first traversal.
        /// </summary>
        /// <param name="n">node to be visited</param>
        /// <param name="level">the starting level in the tree</param>
        private void DepthFirstTraversal(int n, int level)
        {
            // List of nodes already visited
            HashSet<int> visited = new();

            Stack<Pair<int, int>> stack = new();
            stack.Push(Pair<int, int>.Of(n, level));

            while (stack.Count > 0)
            {
                Pair<int, int> pair = stack.Pop();
                n = pair.GetFirst();
                int lvl = pair.GetSecond();

                if (!visited.Contains(n))
                {
                    visited.Add(n);

                    tree[n] = numberComponent;
                    eulerTour[tourLength] = n;
                    this.level[tourLength] = lvl;
                    tourLength++;

                    HNode node = indexList[n];
                    foreach (HNode edge in node.Children())
                    {
                        nodeMap.TryGetValue(edge, out int child);

                        if (!visited.Contains(child))
                        {
                            stack.Push(pair);
                            stack.Push(Pair<int, int>.Of(child, lvl + 1));
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
        /// Determines the Range Minimum Query (RMQ). See the paper.
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
        /// Computes <see cref="eulerTour"/>, <see cref="level"/>, <see cref="representative"/>,
        /// <see cref="numberComponent"/>, and <see cref="tree"/>.
        /// </summary>
        /// <param name="roots">list of root nodes</param>
        /// <param name="numberOfNodes">number of nodes in the forest</param>
        private void DetermineEulerTours(ICollection<HNode> roots, int numberOfNodes)
        {
            eulerTour = new int[2 * numberOfNodes];
            level = new int[2 * numberOfNodes];
            representative = new int[numberOfNodes];

            numberComponent = 0;
            tree = new int[numberOfNodes];

            foreach (HNode root in roots)
            {
                nodeMap.TryGetValue(root, out int u);

                if (tree[u] == 0)
                {
                    numberComponent++;
                    DepthFirstTraversal(u, -1);
                }
                else
                {
                    throw new Exception("LCSFinder: multiple roots in the same tree.");
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
        /// <returns>lowest common ancestor</returns>
        public HNode LCA(HNode nodeA, HNode nodeB)
        {
            if (!nodeMap.TryGetValue(nodeA, out int indexOfA))
            {
                throw new Exception("invalid vertex: " + nodeA);
            }
            if (!nodeMap.TryGetValue(nodeB, out int indexOfB))
            {
                throw new Exception("invalid vertex: " + nodeB);
            }
            // Check if a == b because lca(a, a) == a
            if (nodeA.Equals(nodeB))
            {
                return nodeA;
            }

            // If a and b are in different components, they do not have an LCA
            if (tree[indexOfA] != tree[indexOfB] || tree[indexOfA] == 0)
            {
                return default;
            }
            indexOfA = representative[indexOfA];
            indexOfB = representative[indexOfB];

            if (indexOfA > indexOfB)
            {
                (indexOfB, indexOfA) = (indexOfA, indexOfB);
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

        /// <summary>
        /// Provides a one-to-one mapping for a collection of nodes to the integer range
        /// [0, n), where n is the number of nodes in the collection.
        /// </summary>
        private class NodeToIntegerMap
        {
            /// <summary>
            /// The mapping from nodes onto integers, i.e., the inverse of <see cref="indexList"/>.
            /// </summary>
            private readonly Dictionary<HNode, int> nodeMap;

            /// <summary>
            /// The mapping from integers onto nodes, i.e., the inverse of <see cref="nodeMap"/>.
            /// </summary>
            private readonly HNode[] indexList;

            /// <summary>
            /// Create a new mapping from a set of nodes.
            /// Precondition: nodes is not null.
            /// </summary>
            /// <param name="nodes">the input list of nodes</param>
            public NodeToIntegerMap(ICollection<HNode> nodes)
            {
                nodeMap = new Dictionary<HNode, int>(nodes.Count);
                indexList = new HNode[nodes.Count];
                int i = 0;
                foreach (HNode v in nodes)
                {
                    nodeMap.Add(v, nodeMap.Count);
                    indexList[i] = v;
                    i++;
                }
            }

            /// <summary>
            /// Yields the mapping from nodes onto integers, i.e., the inverse of <see cref="indexList"/>.
            /// </summary>
            /// <returns>a mapping from nodes onto integers</returns>
            public Dictionary<HNode, int> NodeMap()
            {
                return nodeMap;
            }

            /// <summary>
            /// Yields the mapping from integers onto nodes, i.e., the inverse of <see cref="nodeMap"/>.
            /// </summary>
            /// <returns>mapping from integers onto nodes</returns>
            public HNode[] IndexList()
            {
                return indexList;
            }
        }

        #region Utils
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

        /// <summary>
        /// Returns the number of 1 bits in integer <paramref name="x"/>.
        /// </summary>
        /// <param name="x">the integer whose 1 bits should be counted</param>
        /// <returns>number of 1 bits in integer <paramref name="x"/></returns>
        private static int NumBitsSet(uint x)
        {
            x -= ((x >> 1) & 0x55555555);
            x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
            x = (((x >> 4) + x) & 0x0f0f0f0f);
            x += (x >> 8);
            x += (x >> 16);

            return (int)(x & 0x0000003f);
        }
        #endregion Utils

        #region Debugging
        /// <summary>
        /// Checks if all trees rooted by a root listed in <paramref name="roots"/> are valid trees
        /// (nodes can be reached  only once) and whether the parent-child information is consistent.
        /// </summary>
        /// <remarks>This method can be used for debugging.</remarks>
        private static void CheckTree(ICollection<HNode> roots)
        {
            HashSet<HNode> visited = new();
            foreach (HNode root in roots)
            {
                CheckTree(root, visited);
            }

            return;

            static void CheckTree(HNode node, HashSet<HNode> visited)
            {
                if (visited.Contains(node))
                {
                    // node was already visited
                    throw new Exception("Input is not a tree. Node " + node + " can be reached more than once.");
                }
                else
                {
                    visited.Add(node);
                    HNode parent = node.Parent;
                    // We should have visited the parent already in this pre-order depth-first traversal.
                    if (parent != null && !visited.Contains(parent))
                    {
                        throw new Exception("Parent in tree is inconsistent. Violating node: " + parent);
                    }
                    foreach (HNode child in node.Children())
                    {
                        // Does child also believe it is a child of node?
                        if (!ReferenceEquals(child.Parent, node))
                        {
                            throw new Exception("Parenting in tree is inconsistent: Child " + child + " is not a child of node " + parent);
                        }
                        CheckTree(child, visited);
                    }
                }
            }
        }
        #endregion Debugging
    }
}
