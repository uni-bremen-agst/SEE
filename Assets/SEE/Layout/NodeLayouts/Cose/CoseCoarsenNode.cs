// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace SEE.Layout.NodeLayouts.Cose
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
        public CoseCoarsenNode(ILayoutNode node, CoseGraphManager graphManager) : base(node, graphManager)
        {
            weight = 1;
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

