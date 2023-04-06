using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Layout.NodeLayouts.IncrementalTreeMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// </summary>
    public class IncrementalTreeMapLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor. The width and depth are assumed to be in Unity units.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="width">width of the rectangle in which to place all nodes in Unity units</param>
        /// <param name="depth">width of the rectangle in which to place all nodes in Unity units</param>
        public IncrementalTreeMapLayout(float groundLevel,
                             float width,
                             float depth)
        : base(groundLevel)
        {
            name = "IncrementalTreeMap";
            this.width = width;
            this.depth = depth;
        }

        /// <summary>
        /// The width of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float width;

        /// <summary>
        /// The depth of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float depth;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private Dictionary<ILayoutNode, NodeTransform> layout_result;

        private Dictionary<ILayoutNode, DataModel.DG.Node> tNodes;

        public Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, int width)
        {
            throw new NotImplementedException();
        }
        public Dictionary<ILayoutNode, NodeTransform> Layout
            (ICollection<ILayoutNode> layoutNodes, IncrementalTreeMapLayout oldLayout)
        {
            // init nodes

            // trav tree
            // for each set of siblings
            //     dissect or local moves

            // make nodeTransforms out of nodes_list -> save to layout_result

            // return layout_result

            throw new NotImplementedException();
        }    

        private void initTNodes()
        {
            // create a tNode entry for each ilayout node
            // calc area size this node should occupy
            // starts with this.roots 
        }

        private void calculateLayout(ICollection<TNode> siblings,TRectangle rectangle)
        {
            // 
        }


        public override Dictionary<ILayoutNode, NodeTransform> Layout
            (ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges,
             ICollection<SublayoutLayoutNode> sublayouts)
        {
            throw new NotImplementedException();
        }
        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes)
        {
            throw new NotImplementedException();
        }
        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false;
        }
    }
}
