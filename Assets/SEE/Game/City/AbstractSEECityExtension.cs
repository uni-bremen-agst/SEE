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

namespace SEE.Game.City
{
    /// <summary>
    /// Class with properties for a nodelayout.
    /// </summary>
    public class NodelayoutModel
    {
        /// <summary>
        /// true if the nodelayout only visualize leaf nodes
        /// </summary>
        public readonly bool OnlyLeaves;

        /// <summary>
        /// true if the nodelayout can handle sublayouts
        /// </summary>
        public readonly bool CanApplySublayouts;

        /// <summary>
        /// true if the inner nodes enclose the leaf nodes
        /// </summary>
        public readonly bool InnerNodesEncloseLeafNodes;

        /// <summary>
        /// true if the layout is a circular layout
        /// </summary>
        public readonly bool IsCircular;

        /// <summary>
        /// true if the layout displays the hierarchie of the graph
        /// </summary>
        public readonly bool IsHierarchical;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="OnlyLeaves">true if the nodelayout only visualize leaf nodes</param>
        /// <param name="CanApplySublayouts">true if the nodelayout can handle sublayouts</param>
        /// <param name="InnerNodesEncloseLeafNodes">true if the inner nodes enclose the leaf nodes</param>
        /// <param name="IsCircular"> true if the layout is a circular layout</param>
        /// <param name="isHierarchical">true if the layout displays the hierarchie of the graph</param>
        public NodelayoutModel(bool OnlyLeaves, bool CanApplySublayouts, bool InnerNodesEncloseLeafNodes, bool IsCircular, bool isHierarchical)
        {
            this.OnlyLeaves = OnlyLeaves;
            this.CanApplySublayouts = CanApplySublayouts;
            this.InnerNodesEncloseLeafNodes = InnerNodesEncloseLeafNodes;
            this.IsCircular = IsCircular;
            IsHierarchical = isHierarchical;
        }
    }

    /// <summary>
    /// class with properties for inner node kind
    /// </summary>
    public class InnerNodeKindsModel
    {
        /// <summary>
        /// true if the inner node kind has a circluar shape
        /// </summary>
        public readonly bool IsCircular;

        /// <summary>
        /// true if the inner node kind has a rectangular shape
        /// </summary>
        public readonly bool IsRectangular;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="IsCircular">true if the inner node kind has a circluar shape</param>
        /// <param name="IsRectangular"> true if the inner node kind has a rectangular shape</param>
        public InnerNodeKindsModel(bool IsCircular, bool IsRectangular)
        {
            this.IsCircular = IsCircular;
            this.IsRectangular = IsRectangular;
        }
    }

    /// <summary>
    /// Extension class for abstractSeeCity
    /// </summary>
    public static class AbstractSEECityExtension
    {
        /// <summary>
        /// Returns a model for the given nodelayout
        /// </summary>
        /// <param name="nodeLayout">the nodelayout</param>
        /// <returns>the model</returns>
        public static NodelayoutModel GetModel(this NodeLayoutKind nodeLayout)
        {
            switch (nodeLayout)
            {
                case NodeLayoutKind.CompoundSpringEmbedder:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: true, InnerNodesEncloseLeafNodes: true, IsCircular: false, isHierarchical: true);
                case NodeLayoutKind.EvoStreets:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: false, IsCircular: false, isHierarchical: true);
                case NodeLayoutKind.Balloon:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: true, isHierarchical: true);
                case NodeLayoutKind.RectanglePacking:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: false, isHierarchical: true);
                case NodeLayoutKind.Treemap:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: false, isHierarchical: true);
                case NodeLayoutKind.CirclePacking:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: true, isHierarchical: true);
                case NodeLayoutKind.Manhattan:
                    return new NodelayoutModel(OnlyLeaves: true, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: false, IsCircular: false, isHierarchical: false);
                default:
                    return new NodelayoutModel(OnlyLeaves: false, CanApplySublayouts: false, InnerNodesEncloseLeafNodes: true, IsCircular: false, isHierarchical: true);
            }
        }

        /// <summary>
        /// Returns the possible inner node kinds for a layout
        /// </summary>
        /// <param name="nodeLayout">the nodelayout</param>
        /// <returns>the inner node kinds</returns>
        public static List<NodeShapes> GetInnerNodeKinds(this NodeLayoutKind nodeLayout)
        {
            List<NodeShapes> values = Enum.GetValues(typeof(NodeShapes)).Cast<NodeShapes>().ToList();
            List<NodeShapes> list = new List<NodeShapes>();

            list = nodeLayout.GetModel().IsCircular ? values.Where(kind => kind.GetModel().IsCircular).ToList() : values.Where(kind => kind.GetModel().IsRectangular).ToList();
            list.OrderBy(kind => kind.ToString());
            return list;
        }

        /// <summary>
        /// Returns all nodelayout wich are possible sublayouts for the given nodelayout
        /// </summary>
        /// <param name="nodeLayout">the given nodelayout</param>
        /// <returns>a list of possible sublayout</returns>
        public static List<NodeLayoutKind> GetPossibleSublayouts(this NodeLayoutKind nodeLayout)
        {
            List<NodeLayoutKind> values = Enum.GetValues(typeof(NodeLayoutKind)).Cast<NodeLayoutKind>().ToList();
            values.Remove(NodeLayoutKind.FromFile);

            if (nodeLayout == NodeLayoutKind.EvoStreets)
            {
                return values.Where(layout => !layout.GetModel().IsCircular).ToList();
            }

            return values; //nodeLayout.IsCircular() ? values.Where(layout => layout.IsCircular()).ToList() : values.Where(layout => !layout.IsCircular()).ToList();
        }

        /// <summary>
        /// Returns the model for a node shape.
        /// </summary>
        /// <param name="shape">the node shape</param>
        /// <returns>the innernodeKindModel</returns>
        public static InnerNodeKindsModel GetModel(this NodeShapes shape)
        {
            switch (shape)
            {
                case NodeShapes.Blocks:
                    return new InnerNodeKindsModel(IsCircular: false, IsRectangular: true);
                case NodeShapes.Cylinders:
                    return new InnerNodeKindsModel(IsCircular: true, IsRectangular: false);
                default:
                    throw new NotImplementedException($"Unexpected case {shape}");
            }
        }
    }
}


