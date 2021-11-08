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

using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Game.City;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.Cose
{
    /// <summary>
    /// Helper functions
    /// </summary>
    public class CoseHelper
    {
        /// <summary>
        /// checks if the node with the given id is a sublayout root node
        /// </summary>
        /// <param name="sublayoutNodes">all sublayout nodes</param>
        /// <param name="ID">the id</param>
        /// <returns>true if the node with the given id is a sublayout root node</returns>
        public static SublayoutNode CheckIfNodeIsSublayouRoot(ICollection<SublayoutNode> sublayoutNodes, string ID)
        {
            foreach (SublayoutNode subLayoutNode in sublayoutNodes)
            {
                if (subLayoutNode.Node.ID == ID)
                {
                    return subLayoutNode;
                }
            }
            return null;
        }

        /// <summary>
        /// checks if the node with the given id is a sublayout root node
        /// </summary>
        /// <param name="sublayoutNodes">all sublayout nodes</param>
        /// <param name="ID">the id</param>
        /// <returns>true if the node with the given id is a sublayout root node</returns>
        public static SublayoutLayoutNode CheckIfNodeIsSublayouRoot(ICollection<SublayoutLayoutNode> sublayoutNodes, string ID)
        {
            foreach (SublayoutLayoutNode subLayoutNode in sublayoutNodes)
            {
                if (subLayoutNode.Node.ID == ID)
                {
                    return subLayoutNode;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a new Rect with the given scale and center position
        /// </summary>
        /// <param name="scale">the scale</param>
        /// <param name="center">the center postion</param>
        /// <returns>a new rect</returns>
        public static Rect NewRect(Vector3 scale, Vector3 center)
        {
            return new Rect
            {
                x = center.x - scale.x / 2,
                y = center.z - scale.z / 2,
                width = scale.x,
                height = scale.z
            };
        }

        /// <summary>
        /// Sign function 
        /// </summary>
        /// <param name="value">the value </param>
        /// <returns>the sign of the given value</returns>
        public static int Sign(float value)
        {
            if (value > 0)
            {
                return 1;
            }
            else if (value < 0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// return a NodeLayout of the given nodeLayout type
        /// </summary>
        /// <param name="nodeLayout">the node layout</param>
        /// <param name="groundLevel">the groundlevel</param>
        /// <param name="unit">the leafNode unit</param>
        /// <param name="settings">the abstract see city settings</param>
        /// <returns>a node layout object</returns>
        public static NodeLayout GetNodelayout(NodeLayoutKind nodeLayout, float groundLevel, float unit, AbstractSEECity settings)
        {
            switch (nodeLayout)
            {
                case NodeLayoutKind.Manhattan:
                    return new ManhattanLayout(groundLevel, unit);
                case NodeLayoutKind.RectanglePacking:
                    return new RectanglePackingNodeLayout(groundLevel, unit);
                case NodeLayoutKind.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, unit);
                case NodeLayoutKind.Treemap:
                    return new TreemapLayout(groundLevel, 1000.0f * unit, 1000.0f * unit);
                case NodeLayoutKind.Balloon:
                    return new BalloonNodeLayout(groundLevel);
                case NodeLayoutKind.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel);
                case NodeLayoutKind.CompoundSpringEmbedder:
                    return new CoseLayout(groundLevel, settings);
                default:
                    throw new Exception("Unhandled node layout " + nodeLayout);
            }
        }

        /// <summary>
        /// Calculates a good edge length for the given number of nodes and maximal depth
        /// </summary>
        /// <param name="countNodes">the number of nodes</param>
        /// <param name="maxDepth">the maximal depth</param>
        /// <returns>edge length</returns>
        public static int GetGoodEgdeLength(int countNodes, int maxDepth, int leafNodesCount, int countEdges)
        {
            float constant = -5.055f;
            float countNodesConstant = 0.206f;
            float countMaxDepthConstant = 2.279f;
            float edgeDensityConstant = -57.829f;

            float edgeDensity;

            if (leafNodesCount == 0 || countEdges == 0)
            {
                edgeDensity = 0;
            }
            else
            {
                float leafNodeCountMinus = leafNodesCount > 1 ? leafNodesCount - 1 : 1;
                edgeDensity = countEdges / (leafNodesCount * leafNodeCountMinus);
            }

            float edgeLength = countNodesConstant * countNodes + countMaxDepthConstant * maxDepth + edgeDensityConstant * edgeDensity + constant;
            return Math.Max((int)Math.Ceiling(edgeLength), 2);
        }

        /// <summary>
        /// Calculates a good repulsion strength for the given number of edges and maximal depth
        /// </summary>
        /// <param name="countNodes">the number of nodes</param>
        /// <param name="maxDepth">the maximal depth</param>
        /// <returns>repulsion strength</returns>
        public static int GetGoodRepulsionRange(int maxDepth, int leafNodesCount, int countEdges)
        {
            float constant = -1.995f;
            float countMaxDepthConstant = 1.107f;
            float countNodesConstant = 0.209f;

            float edgeDensityConstant = 91.799f;

            float edgeDensity;

            if (leafNodesCount == 0 || countEdges == 0)
            {
                edgeDensity = 0;
            }
            else
            {
                float leafNodeCountMinus = leafNodesCount > 1 ? leafNodesCount - 1 : 1;
                edgeDensity = countEdges / (leafNodesCount * leafNodeCountMinus);
            }


            float repulsionStrength = countMaxDepthConstant * maxDepth + leafNodesCount * countNodesConstant + edgeDensity * edgeDensityConstant + constant;
            return Math.Max((int)Math.Ceiling(repulsionStrength), 2);
        }

        /// <summary>
        /// Returns the layout node with the given ID
        /// </summary>
        /// <param name="ID">the ID</param>
        /// <returns>layout node with the given ID</returns>
        public static ILayoutNode GetLayoutNodeFromLinkname(String ID, ICollection<ILayoutNode> layoutNodes)
        {
            List<ILayoutNode> nodes = layoutNodes.Where(layoutNode => layoutNode.ID == ID).ToList();

            if (nodes.Count > 1)
            {
                throw new System.Exception("Linkname should be unique");
            }
            else if (nodes.Count == 0)
            {
                throw new System.Exception("No node exists with this linkname");
            }

            return nodes.First();
        }

    }
}


