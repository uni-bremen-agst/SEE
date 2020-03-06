using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;

namespace SEE.Layout
{
    public class CoseCoarsenNode : CoseNode
    {
        /// <summary>
        /// the reference node
        /// </summary>
        private CoseNode reference;

        /// <summary>
        /// indicates whether the node was matched with another node
        /// </summary>
        private bool matched;

        /// <summary>
        /// the weight of the node
        /// </summary>
        private int weight;

        /// <summary>
        /// the first node of the matching
        /// </summary>
        private CoseCoarsenNode node1;

        /// <summary>
        /// the second node of the matching
        /// </summary>
        private CoseCoarsenNode node2;

        public CoseNode Reference { get => reference; set => reference = value; }
        public bool Matched { get => matched; set => matched = value; }
        public int Weight { get => weight; set => weight = value; }
        public CoseCoarsenNode Node1 { get => node1; set => node1 = value; }
        public CoseCoarsenNode Node2 { get => node2; set => node2 = value; }

        /// <summary>
        /// constructor, inital weight of a coarsenNode is 1
        /// </summary>
        /// <param name="node">the original node</param>
        /// <param name="graphManager">the current graphmanager</param>
        public CoseCoarsenNode(Node node, CoseGraphManager graphManager) : base(node, graphManager)
        {
            Weight = 1;
        }

        /// <summary>
        /// constructor
        /// </summary>
        public CoseCoarsenNode() : base(null, null)
        {
        }

        /// <summary>
        /// Calculates the best node for a matching
        /// </summary>
        /// <returns> neighbour node with the smallest weight </returns>
        public CoseCoarsenNode GetMatching()
        {
            CoseCoarsenNode minWeighted = null;
            int minWeight = int.MaxValue;

            foreach (CoseCoarsenNode node in GetNeighborsList())
            {
                if (!node.Matched && node != this && (node.Weight < minWeight))
                {
                    minWeighted = node;
                    minWeight = node.Weight;
                }
            }
            return minWeighted;
        }
    }
}

