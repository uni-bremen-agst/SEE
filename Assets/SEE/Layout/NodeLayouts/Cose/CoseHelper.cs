using SEE.DataModel;
using SEE.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public class CoseHelper
    {
        public static void CalculateListSublayouts()
        {

        }

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

        public static NodeLayout GetNodelayout(NodeLayouts nodeLayout, float groundLevel, float unit, AbstractSEECity settings)
        {
            switch (nodeLayout)
            {
                case NodeLayouts.Manhattan:
                    return new ManhattanLayout(groundLevel, unit);
                case NodeLayouts.RectanglePacking:
                    return new RectanglePackingNodeLayout(groundLevel, unit);
                case NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, unit);
                case NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, 1000.0f * unit, 1000.0f * unit);
                case NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel);
                case NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel);
                case NodeLayouts.CompoundSpringEmbedder:
                    return new CoseLayout(groundLevel, settings);
                default:
                    throw new Exception("Unhandled node layout " + nodeLayout);
            }
        }

        public static int GetGoodEgdeLength(int countNodes, int maxDepth)
        {
            float constant = -5.882f;
            float countNodesConstant = 0.153f;
            float countMaxDepthConstant = 2.392f;

            float edgeLength = countNodesConstant * countNodes + countMaxDepthConstant * maxDepth + constant;
            return Math.Max((int)Math.Ceiling(edgeLength), 2);
        }

        public static int GetGoodRepulsionRange(int maxDepth, int countEdges)
        {
            float constant = 0.537f;
            float countMaxDepthConstant = 0.958f;
            float countEdgeConstant = 0.372f;

            float repulsionStrength = countMaxDepthConstant * maxDepth + countEdgeConstant * countEdges + constant;
            return Math.Max((int)Math.Ceiling(repulsionStrength), 2);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ILayoutNode GetLayoutNodeFromLinkname(String ID, List<ILayoutNode> layoutNodes)
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


