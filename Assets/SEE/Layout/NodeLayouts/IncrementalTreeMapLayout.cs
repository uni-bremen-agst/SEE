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

        private IncrementalTreeMapLayout oldLayout;

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
                    this.roots = LayoutNodes.GetRoots(layoutNodeList);
                    this.oldLayout = oldLayout;
                    InitTNodes();
                    {
                        // TODO remove if checked
                        float total_size = tNodes.Sum( x => x.Parent == null ? x.Size : 0);
                        if( total_size != width * depth)
                        {
                            Debug.Log("total size: " + total_size.ToString()
                                        + "\n rect size:  " + (width*depth).ToString()
                            );
                        }
                    }
                    CalculateLayout();
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

        /// <summary>
        /// Adds positioning and scales to <see cref="layout_result"/> for all root nodes (nodes with no parent)
        /// within a rectangle whose center position is Vector3.zero and whose width and depth is
        /// as specified by the constructor call. This function is then called recursively for the
        /// children of each root (until leaves are reached).
        /// </summary>
        private void CalculateLayout()
        {
            /// Our "logical" rectangle in which to put the whole treemap is assumed to have its
            /// center at Vector3.zero here. <see cref="CalculateLayout(ICollection{ILayoutNode}, float, float, float, float)"/>
            /// assumes the rectangle's location be specified by its left front corner.
            /// Hence, we need to transform the center of the "logical" rectangle to the left front
            /// corner of the rectangle by -width/2 and -depth/2, respectively.
            TRectangle rectangle = new TRectangle(x: -width / 2.0f, z: -depth / 2.0f, width, depth);
            if (roots.Count == 1)
            {
                ILayoutNode root = roots[0];
                Assert.AreEqual(root.AbsoluteScale, root.LocalScale);
                layout_result[root] = new NodeTransform(Vector3.zero,
                                                        new Vector3(width, root.LocalScale.y, depth));                                                        
                CalculateLayout(root.Children(), rectangle);
            }
            else
            {
                CalculateLayout(roots,rectangle);
            }
        }

        private void CalculateLayout(ICollection<ILayoutNode> siblings,TRectangle rectangle)
        {
            if(    this.oldLayout == null
                || this.oldLayout.NumberOfOccurrencesInOldGraph(siblings) <= 1
                || this.oldLayout.HaveSingleParentInOldGraph(siblings))
            {
                // dissect
                // extract segments?
            }
            else
            {
                //TNodes.add(new_nodes_for_old_siblings)
                //TSegments.add(all_Segments(old_siblings))
                //siblings.register()
                //siblings.apply(layout_old)
                //transform Rectangles 
                //# correct 
                //addNewNodes
                //deleteOldeNodes
                //#correct
                //localMoves(siblings, segments)
            }

            foreach (ILayoutNode node in siblings)
            {
                ICollection<ILayoutNode> children = node.Children();
                if (children.Count > 0)
                {
                    // Note: nodeTransform.position is the center position, while
                    // CalculateLayout assumes co-ordinates x and z as the left front corner

                    // tNodes to dic -> rectangle

                    //Assert.AreEqual(node.AbsoluteScale, node.LocalScale);
                    //NodeTransform nodeTransform = layout_result[node];
                    //CalculateLayout(children,
                    //                nodeTransform.position.x - nodeTransform.scale.x / 2.0f,
                    //                nodeTransform.position.z - nodeTransform.scale.z / 2.0f,
                    //                nodeTransform.scale.x,
                    //                nodeTransform.scale.z);
                }
            }
        }

        private bool HaveSingleParentInOldGraph(ICollection<ILayoutNode> nodes)
        {
            HashSet<ILayoutNode> parents = new HashSet<ILayoutNode>();
            foreach(ILayoutNode node in nodes)
            {
                ILayoutNode oldNode = oldLayout.findILayoutNodeByID(node.ID);
                if(oldNode != null)
                {
                    parents.Add(oldNode.Parent);
                }
            }
            return parents.Count == 1;
        }
        
        private int NumberOfOccurrencesInOldGraph(ICollection<ILayoutNode> nodes)
        {
            int counter = 0;
            foreach (ILayoutNode node in nodes)
            {
                if(oldLayout.findILayoutNodeByID(node.ID) != null)
                {
                    counter ++;
                }
            }
            return counter;
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

        public ILayoutNode findILayoutNodeByID(string ID)
        {
            foreach(ILayoutNode node in roots)
            {
                ILayoutNode result = findILayoutNodeByID(node, ID);
                if( result != null)
                {
                    return result;
                }
            }
            return null;
            ILayoutNode findILayoutNodeByID(ILayoutNode node, string ID)
            {
                if(node.ID.Equals(ID))
                {
                    return node;
                }
                foreach(ILayoutNode child in node.Children())
                {
                    ILayoutNode result = findILayoutNodeByID(child, ID);
                    if( result != null)
                    {
                        return result;
                    }
                }
                return null;
            }
        }

    }
}
