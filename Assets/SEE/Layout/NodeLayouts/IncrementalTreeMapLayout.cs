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

        private IList<TNode> tNodes;


        public Dictionary<ILayoutNode, NodeTransform> Layout
            (ICollection<ILayoutNode> layoutNodes, IncrementalTreeMapLayout oldLayout)
        {
            layout_result = new Dictionary<ILayoutNode, NodeTransform>();

            IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
            switch (layoutNodeList.Count)
            {
                case 0:
                    throw new ArgumentException("No nodes to be laid out.");
                case 1:
                {
                    using IEnumerator<ILayoutNode> enumerator = layoutNodeList.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        // MoveNext() must be called before we can call Current.
                        ILayoutNode gameNode = enumerator.Current;
                        UnityEngine.Assertions.Assert.AreEqual(gameNode.AbsoluteScale, gameNode.LocalScale);
                        layout_result[gameNode] = new NodeTransform(Vector3.zero,
                            new Vector3(width, gameNode.LocalScale.y, depth));
                    }
                    else
                    {
                        Assert.IsTrue(false, "We should never arrive here.\n");
                    }

                    break;
                }
                default:
                    roots = LayoutNodes.GetRoots(layoutNodeList);
                    InitTNodes();
                    {
                        float total_size = tNodes.Sum( x => x.Parent == null ? x.Size : 0);
                        if( total_size != width * depth)
                        {
                            Debug.Log("total size: " + total_size.ToString()
                                        + "\n rect size:  " + (width*depth).ToString()
                            );
                        }
                    }
                    //CalculateLayout();
                    break;
            }
            return layout_result;
            throw new NotImplementedException();
        }    

        private void InitTNodes()
        {
            tNodes = new List<TNode>();
            foreach(ILayoutNode node in roots)
            {
                InitTNode(node, null);
            }
        }

        private float InitTNode(ILayoutNode node, TNode parent)
        {
            if (node.IsLeaf)
            {
                // a leaf
                Vector3 size = node.LocalScale;
                // x and z lengths may differ; we need to consider the larger value
                float result = Mathf.Max(size.x, size.z);
                TNode newTNode = new TNode(node,result,parent);
                tNodes.Add(newTNode);
                return result;
            }
            else
            {
                TNode newTNode = new TNode(node, 0, parent);
                tNodes.Add(newTNode);
                float total_size = 0.0f;
                foreach (ILayoutNode child in node.Children())
                {
                    total_size += InitTNode(child, newTNode);
                }
                newTNode.Size = total_size;
                return total_size;
            }
        }

        private void CalculateLayout(ICollection<TNode> siblings,TRectangle rectangle)
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
