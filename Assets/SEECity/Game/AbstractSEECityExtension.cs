using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Game
{
    public class NodelayoutModel
    {
        public readonly bool OnlyLeaves;
        public readonly bool CanApplySublayouts;
        public readonly bool InnerNodesEncloseLeafNodes;
        public readonly bool IsCircular;

        public NodelayoutModel(bool OnlyLeaves, bool CanApplySublayouts, bool InnerNodesEncloseLeafNodes, bool IsCircular)
        {
            this.OnlyLeaves = OnlyLeaves;
            this.CanApplySublayouts = CanApplySublayouts;
            this.InnerNodesEncloseLeafNodes = InnerNodesEncloseLeafNodes;
            this.IsCircular = IsCircular;
        }
    }

    public class InnerNodeKindsModel
    {
        public readonly bool IsCircular;
        public readonly bool IsRectangular;

        public InnerNodeKindsModel(bool IsCircular, bool IsRectangular)
        {
            this.IsCircular = IsCircular;
            this.IsRectangular = IsRectangular;
        }
    }

    public static class AbstractSEECityExtension
    {
        public static NodelayoutModel GetModel(this NodeLayouts nodeLayout)
        {
            switch (nodeLayout)
            {
                case NodeLayouts.CompoundSpringEmbedder:
                    return new NodelayoutModel(OnlyLeaves: false,  CanApplySublayouts: true,  InnerNodesEncloseLeafNodes: true,  IsCircular: false);
                case NodeLayouts.EvoStreets:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: false, IsCircular: false);
                case NodeLayouts.Balloon:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: true);
                case NodeLayouts.FlatRectanglePacking:
                    return new NodelayoutModel(OnlyLeaves: true, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: false, IsCircular: false);
                case NodeLayouts.Treemap:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: false);
                case NodeLayouts.CirclePacking:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: true);
                case NodeLayouts.Manhattan: 
                    return new NodelayoutModel(OnlyLeaves: true, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: false, IsCircular: false); 
                default:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: false);
            }
        }


        public static List<InnerNodeKinds> GetInnerNodeKinds(this NodeLayouts nodeLayout)
        {
            List<InnerNodeKinds> values = Enum.GetValues(typeof(InnerNodeKinds)).Cast<InnerNodeKinds>().ToList();
            List<InnerNodeKinds> list = new List<InnerNodeKinds>();

            list = nodeLayout.GetModel().IsCircular ? values.Where(kind => kind.GetModel().IsCircular).ToList() : values.Where(kind => kind.GetModel().IsRectangular).ToList();
            list.OrderBy(kind => kind.ToString());
            return list;
        }

        public static List<NodeLayouts> GetPossibleSublayouts(this NodeLayouts nodeLayout)
        {
            List<NodeLayouts> values = Enum.GetValues(typeof(NodeLayouts)).Cast<NodeLayouts>().ToList();
            return values; //nodeLayout.IsCircular() ? values.Where(layout => layout.IsCircular()).ToList() : values.Where(layout => !layout.IsCircular()).ToList();
        }

        public static InnerNodeKindsModel GetModel(this InnerNodeKinds innerNodeKind)
        {
            switch (innerNodeKind)
            {
                case InnerNodeKinds.Blocks:
                    return new InnerNodeKindsModel(IsCircular: false, IsRectangular: true);
                case InnerNodeKinds.Rectangles:
                    return new InnerNodeKindsModel(IsCircular: false, IsRectangular: true);
                case InnerNodeKinds.Donuts:
                    return new InnerNodeKindsModel(IsCircular: true, IsRectangular: false);
                case InnerNodeKinds.Circles:
                    return new InnerNodeKindsModel(IsCircular: true, IsRectangular: false);
                case InnerNodeKinds.Empty:
                    return new InnerNodeKindsModel(IsCircular: true, IsRectangular: true);
                case InnerNodeKinds.Cylinders:
                    return new InnerNodeKindsModel(IsCircular: true, IsRectangular: false);
                default:
                    return new InnerNodeKindsModel(IsCircular: false, IsRectangular: true);
            }
        }
    }
}


