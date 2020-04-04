using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Game
{
    public static class AbstractSEECityExtension
    {
        public static List<InnerNodeKinds> GetInnerNodeKinds(this NodeLayouts nodeLayout)
        {
            List<InnerNodeKinds> values = Enum.GetValues(typeof(InnerNodeKinds)).Cast<InnerNodeKinds>().ToList();
            List<InnerNodeKinds> list = new List<InnerNodeKinds>();

            switch (nodeLayout)
            {
                case NodeLayouts.CompoundSpringEmbedder:
                    list = values.Where(kind => kind.IsRectangular()).ToList();
                    break;
                case NodeLayouts.EvoStreets:
                    list = values.Where(kind => kind.IsRectangular()).ToList();
                    break;
                case NodeLayouts.Balloon:
                    list = values.Where(kind => kind.IsCircular()).ToList();
                    break;
                case NodeLayouts.FlatRectanglePacking:
                    list = values.Where(kind => kind.IsRectangular()).ToList();
                    break;
                case NodeLayouts.Treemap:
                    list = values.Where(kind => kind.IsRectangular()).ToList();
                    break;
                case NodeLayouts.CirclePacking:
                    list = values.Where(kind => kind.IsCircular()).ToList();
                    break;
                case NodeLayouts.Manhattan:
                    list = values.Where(kind => kind.IsRectangular()).ToList();
                    break;
            }

            list.OrderBy(kind => kind.ToString());
            return list;
        }

        public static bool OnlyLeaves(this NodeLayouts nodeLayout)
        {
            switch (nodeLayout)
            {
                case NodeLayouts.CompoundSpringEmbedder:
                    return false;
                case NodeLayouts.EvoStreets:
                    return false;
                case NodeLayouts.Balloon:
                    return false;
                case NodeLayouts.FlatRectanglePacking:
                    return true;
                case NodeLayouts.Treemap:
                    return false;
                case NodeLayouts.CirclePacking:
                    return false;
                case NodeLayouts.Manhattan:
                    return true;
                default:
                    return false;
            }
        }

        public static bool InnerNodesEncloseLeafNodes(this NodeLayouts nodeLayout)
        {
            switch (nodeLayout)
            {
                case NodeLayouts.CompoundSpringEmbedder:
                    return true;
                case NodeLayouts.EvoStreets:
                    return false;
                case NodeLayouts.Balloon:
                    return true;
                case NodeLayouts.FlatRectanglePacking:
                    return false;
                case NodeLayouts.Treemap:
                    return true;
                case NodeLayouts.CirclePacking:
                    return true;
                case NodeLayouts.Manhattan:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsCircular(this InnerNodeKinds innerNodeKind)
        {
            switch (innerNodeKind)
            {
                case InnerNodeKinds.Blocks:
                    return false;
                case InnerNodeKinds.Rectangles:
                    return false;
                case InnerNodeKinds.Donuts:
                    return true;
                case InnerNodeKinds.Circles:
                    return true;
                case InnerNodeKinds.Empty:
                    return true;
                case InnerNodeKinds.Cylinders:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsRectangular(this InnerNodeKinds innerNodeKind)
        {
            switch (innerNodeKind)
            {
                case InnerNodeKinds.Blocks:
                    return true;
                case InnerNodeKinds.Rectangles:
                    return true;
                case InnerNodeKinds.Donuts:
                    return false;
                case InnerNodeKinds.Circles:
                    return false;
                case InnerNodeKinds.Empty:
                    return true;
                case InnerNodeKinds.Cylinders:
                    return false;
                default:
                    return false;
            }
        }
    }
}


