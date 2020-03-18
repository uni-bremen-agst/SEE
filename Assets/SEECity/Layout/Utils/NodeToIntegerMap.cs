using SEE.DataModel;
using UnityEngine;
using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// Provides a one-to-one mapping for a collection of nodes to the integer range
    /// $[0, n)$ where n is the number of nodes in the collection.
    /// </summary>
    internal class NodeToIntegerMap
    {
        private readonly Dictionary<Node, int> nodeMap;
        private readonly Node[] indexList;

        /// <summary>
        /// Create a new mapping from a set of nodes.
        /// Precondition: nodes is not null.
        /// </summary>
        /// <param name="nodes">the input list of nodes</param>
        public NodeToIntegerMap(IList<Node> nodes)
        {
            nodeMap = new Dictionary<Node, int>(nodes.Count);
            indexList = new Node[nodes.Count];
            int i = 0;
            foreach (Node v in nodes)
            {
                nodeMap.Add(v, nodeMap.Count);
                indexList[i] = v;
                i++;
            }
        }
        /// <summary>
        /// Create a new mapping from a list of vertices.The input list will be used as the
        /// indexList, so it must not be modified.
        /// Precondition: nodes is not null.
        /// </summary>
        /// <param name="nodes">the input list of nodes</param>
        //public NodeToIntegerMap(List<Node> nodes)
        //{
        //    nodeMap = new Dictionary<Node, int>(nodes.Count);
        //    indexList = nodes.ToArray();

        //    int i = 0;
        //    foreach (Node v in nodes)
        //    {
        //        nodeMap.Add(v, i);
        //        i++;
        //    }
        //}

        /**
         * Create a new mapping from a collection of vertices.
         *
         * @param vertices the input collection of vertices
         * @throws NullPointerException if {@code vertices} is {@code null}
         * @throws IllegalArgumentException if the vertices are not distinct
         */
        //public NodeToIntegerMap(ICollection<Node> vertices)
        //{
        //    nodeMap = new Dictionary<Node, int>(vertices.Count);
        //    indexList = new List<Node>(vertices.Count);
        //    foreach (Node v in vertices)
        //    {
        //        nodeMap.Add(v, nodeMap.Count);
        //        indexList.Add(v);
        //    }
        //}

        /**
         * Get the {@code vertexMap}, a mapping from vertices to integers (i.e. the inverse of
         * {@code indexList}).
         *
         * @return a mapping from vertices to integers
         */
        public Dictionary<Node, int> NodeMap()
        {
            return nodeMap;
        }

        /**
         * Get the {@code indexList}, a mapping from integers to vertices (i.e. the inverse of
         * {@code vertexMap}).
         *
         * @return a mapping from integers to vertices
         */
        public Node[] IndexList()
        {
            return indexList;
        }

        internal void Dump()
        {
            Debug.Log("vertexMap\n");
            foreach (KeyValuePair<Node, int> entry in nodeMap)
            {
                Debug.Log(" key: " + entry.Key.LinkName + " value: " + entry.Value + "\n");
            }
            Debug.Log("indexList\n");
            foreach (Node node in indexList)
            {
                Debug.Log(" element: " + node.LinkName + "\n");
            }
        }
    }
}
