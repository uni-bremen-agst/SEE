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

using SEE.DataModel.DG;
using System.Collections.Generic;

namespace SEE.Layout.Utils
{
    /// <summary>
    /// Provides extensions to SEE.DataModel.Graph related to layout properties.
    /// </summary>
    public static class LayoutGraphExtensions
    {
        /// <summary>
        /// Returns all edges of graph whose source and target is contained in <paramref name="selectedNodes"/>.
        /// </summary>
        /// <param name="selectedNodes">the set of nodes for which to determine the connecting edges</param>
        /// <returns>all edges of graph whose source and target is contained in <paramref name="selectedNodes"/></returns>
        public static IList<Edge> ConnectingEdges(this Graph graph, ICollection<ILayoutNode> selectedNodes)
        {
            IList<Edge> result = new List<Edge>();

            foreach (Edge edge in graph.Edges())
            {
                if (FilterListForLayoutNode(edge.Source.ID, selectedNodes) && FilterListForLayoutNode(edge.Target.ID, selectedNodes))
                {
                    result.Add(edge);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if there is a node with the given <paramref name="ID"/> contained in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="ID">the requested node ID</param>
        /// <param name="layoutNodes">the set of nodes for which to determine whether they have a matching ID</param>
        /// <returns>true if the node with the given <paramref name="ID"/> is contained in <paramref name="layoutNodes"/></returns>
        private static bool FilterListForLayoutNode(string ID, ICollection<ILayoutNode> layoutNodes)
        {
            foreach (ILayoutNode gameNode in layoutNodes)
            {
                if (gameNode.ID == ID)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
