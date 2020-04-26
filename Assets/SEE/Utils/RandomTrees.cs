using System;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// A random generator for directed trees with a single root.
    /// </summary>
    public class RandomTrees
    {
        /// <summary>
        /// Creates a random tree based on a random Prüfer sequence given as <paramref name="prufer"/>.
        /// For details on the implementation, see https://en.wikipedia.org/wiki/Prüfer_sequence.
        /// 
        /// The resulting tree, T, will have prufer.Length + 2 many nodes and a single <paramref name="root"/>.
        /// The nodes of T will be represented by integer values in the range [0, prufer.Length + 1].
        /// Let parent be the result of this method; then parent[i] denotes the parent of i in T
        /// for every i different from <paramref name="root"/> and parent[<paramref name="root"/>] = -1. 
        /// Thus, the value range of parent[i] is [-1, prufer.Length + 1].
        /// </summary>
        /// <param name="prufer">a Prüfer sequence, where prufer[i] > 0 for i in [0, pruefer.Length-1]</param>
        /// <param name="root">the resulting root node of the tree</param>
        /// <returns>the parent of each node</returns>
        private static int[] PruferSequenceToTree(int[] prufer, out int root)
        {
            // Let prufer = {a[1], a[2], ..., a[n]} be a Prüfer sequence.
            // The tree will have n+2 nodes, numbered from 0 to n+1.
            int numberOfNodes = prufer.Length + 2;
            // Number of times each node appears in the prufer sequence.
            int[] degree = new int[numberOfNodes];
            // Representation of the resulting tree: parent[i] denotes the parent of node i.
            // If parent[i] = -1, i is the root of the tree.
            int[] parent = new int[numberOfNodes];

            // For each node, set its degree to the number of times it appears in the sequence. 
            for (int i = 0; i < numberOfNodes - 2; i++)
            {
                degree[prufer[i] - 1]++;
            }

            // Note: an edge (c, p) added to T means that c is a child of p in the following.
            // Next, for each number in the sequence a[i], find the first (lowest-numbered) node, j, 
            // with degree equal to 0, add the edge (j, a[i]) to the tree, and decrement the degrees 
            // of j and a[i].
            int j = 0;
            // Find the smallest label not present in prufer[].
            // For each value i in a do:
            for (int i = 0; i < numberOfNodes - 2; i++)
            {
                // For each node j in T (T is the tree)
                for (j = 0; j < numberOfNodes; j++)
                {
                    // If j + 1 is not present in prufer set 
                    if (degree[j] == 0)
                    {
                        // Remove from Prufer set and add edge.
                        degree[j] = -1;
                        // Note, j in [0, numberOfNodes-1], but prufer[i] > 0 relating to node prufer[i]-1 of T.
                        // Insert edge (j, prufer[i]-1) into T, where j is the child and prufer[i]-1 is the parent.
                        int c = j;
                        int p = prufer[i] - 1;
                        // c = j and j >= 0 => c >= 0
                        // p = prufer[i]-1 and prufer[i] > 0 => p >= 0
                        parent[c] = p;
                        //Debug.Log("(" + (j + 1) + ", " + prufer[i] + ") \n");
                        degree[prufer[i] - 1]--;
                        break;
                    }
                }
            }

            // Two nodes with degree 0 remain (call them u, v).
            int u = 0;
            int v = 0;
            bool lookingForFirstOne = true;
            for (int i = 0; i < numberOfNodes; i++)
            {
                if (degree[i] == 0)
                {
                    if (lookingForFirstOne)
                    {
                        u = i;
                        lookingForFirstOne = false;
                    }
                    else
                    {
                        v = i;
                        break;
                    }
                }
            }
            // Both nodes, u and v, can be considered the root; the choice is arbitrary.
            // We add the edge (u,v) to the tree. Thus, v is the root of the tree.
            root = v;
            parent[u] = root;
            parent[root] = -1;
            return parent;
        }

        /// <summary>
        /// Prints the tree a a list of chains from each leaf to the root. Used for debugging.
        /// </summary>
        /// <param name="parent">list of parents</param>
        /// <param name="root">root node of the tree</param>
        private static void Print(int[] parent, int root)
        {
            // for each leaf node i
            for (int i = 0; i < parent.Length; i++)
            {
                if (IsLeaf(i, parent))
                {
                    string path = i.ToString();
                    int cursor = parent[i];
                    while (cursor != -1)
                    {
                        path += " -> " + cursor;
                        cursor = parent[cursor];
                    }
                    Debug.Log(path + "\n");
                }
            }
        }

        /// <summary>
        /// Yields true if <paramref name="node"/> is a leaf node (has no child).
        /// </summary>
        /// <param name="node">node to be checked</param>
        /// <param name="parent">list of parents</param>
        /// <returns>true if <paramref name="node"/> is a leaf</returns>
        private static bool IsLeaf(int node, int[] parent)
        {
            foreach (int p in parent)
            {
                if (p == node)
                {
                    // if l occurs in parent, l is a parent, hence, no leaf
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a random tree with <paramref name="numberOfNodes"/> many nodes with the 
        /// resulting <paramref name="root"/>.
        /// 
        /// The resulting tree, T, will have numberOfNodes many nodes and a single <paramref name="root"/>.
        /// The nodes of T will be represented by integer values in the range [0, numberOfNodes-1].
        /// Let parent be the result of this method; then parent[i] denotes the parent of i in T
        /// for every i different from <paramref name="root"/> and parent[<paramref name="root"/>] = -1. 
        /// Thus, the value range of parent[i] is [-1, numberOfNodes-1].
        /// 
        /// Precondition: <paramref name="numberOfNodes"/> >= 0.
        /// </summary>
        /// <param name="numberOfNodes">the requested number of nodes</param>
        /// <param name="root">the resulting root node of the tree</param>
        /// <returns>the parent of each node</returns>
        public static int[] Random(int numberOfNodes, out int root)
        {
            if (numberOfNodes < 0)
            {
                throw new Exception("The requested number of nodes for a random tree must be at least 0");
            }
            else
            {
                switch (numberOfNodes)
                {
                    case 0:
                        {
                            int[] parent = new int[0];
                            root = -1;
                            return parent;
                        }
                    case 1:
                        {
                            int[] parent = new int[1];
                            root = 0;
                            parent[root] = -1;
                            return parent;
                        }
                    case 2:
                        {
                            int[] parent = new int[2];
                            root = 0;
                            parent[root] = -1;
                            parent[1] = root;
                            return parent;
                        }
                    default:
                        {
                            System.Random rand = new System.Random(10);
                            int length = numberOfNodes - 2;
                            int[] pruferSequence = new int[length];

                            // Generate a random Prüfer sequence
                            for (int i = 0; i < length; i++)
                            {
                                // Assert: pruferSequence[i] > 0
                                pruferSequence[i] = rand.Next(length + 1) + 1;
                            }
                            int[] parent = PruferSequenceToTree(pruferSequence, out root);
                            //Print(parent, root);
                            return parent;
                        }
                }
            }
        }
    }
}
