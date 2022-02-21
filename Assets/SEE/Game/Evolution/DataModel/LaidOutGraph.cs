//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.DataModel.DG;
using SEE.Layout;
using SEE.Utils;
using System.Collections.Generic;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Data model containing a <see cref="Graph"/> with its node and edge
    /// layout. The layouts are indexed by the node's or edge's ID,
    /// respectively.
    /// </summary>
    public class LaidOutGraph
    {
        /// <summary>
        /// The graph.
        /// </summary>
        private readonly Graph graph;

        /// <summary>
        /// The layout of the nodes as a mapping of the nodes' IDs onto their ILayoutNode.
        /// </summary>
        private readonly Dictionary<string, ILayoutNode> nodeLayout;

        /// <summary>
        /// The layout of the edges as a mapping of the edges' IDs onto their ILayoutEdge.
        /// May be null if edges are not drawn.
        /// </summary>
        private readonly Dictionary<string, ILayoutEdge<ILayoutNode>> edgeLayout;

        /// <summary>
        /// The graph.
        /// </summary>
        public Graph Graph => graph;

        /// <summary>
        /// The layout of the nodes as a mapping of the nodes' IDs onto their ILayoutNode.
        /// </summary>
        public Dictionary<string, ILayoutNode> Layout => nodeLayout;

        /// <summary>
        /// The layout of the edges as a mapping of the edges' IDs onto their ILayoutEdge.
        /// May be null if edges are not drawn.
        /// </summary>
        public Dictionary<string, ILayoutEdge<ILayoutNode>> EdgeLayout => edgeLayout;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph">the graph</param>
        /// <param name="nodeLayout">its node layout as a mapping of the nodes' IDs onto their ILayoutNode</param>
        /// <param name="edgeLayout">its edge layout as a mapping of the edges' IDs onto their ILayoutEdge</param>
        public LaidOutGraph(Graph graph, Dictionary<string, ILayoutNode> nodeLayout, Dictionary<string, ILayoutEdge<ILayoutNode>> edgeLayout)
        {
            this.graph = graph.AssertNotNull("graph");
            this.nodeLayout = nodeLayout.AssertNotNull("node layout");
            this.edgeLayout = edgeLayout.AssertNotNull("edge layout");
        }
    }
}