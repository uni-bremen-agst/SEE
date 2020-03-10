using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using SEE;
using static SEE.GraphSettings;

namespace SEE.Layout
{
    public class CoseNodeSublayoutValues
    {
        /// <summary>
        /// the bounds of the node relative to its sublayout root 
        /// </summary>
        public Rect relativeRect = new Rect(0, 0, 0, 0);

        /// <summary>
        /// if the node is a sublayout root thats the list of its sublayout nodes 
        /// </summary>
        private List<CoseNode> sublayoutNodes = new List<CoseNode>();

        /// <summary>
        /// Indicates whether the node is a sublayout root
        /// </summary>
        private bool isSubLayoutRoot = false;

        /// <summary>
        /// Indicates whether the node is a sublayout node
        /// </summary>
        private bool isSubLayoutNode = false;

        /// <summary>
        /// The root node of the sublayout the node is part of 
        /// </summary>
        private CoseNode subLayoutRoot = null;
        
        /// <summary>
        /// The layout if the sublayout 
        /// </summary>
        private NodeLayouts nodeLayout;

        /// <summary>
        /// 
        /// </summary>
        private CoseSublayout sublayout;

        public List<Node> removedChildren = new List<Node>();

        public List<CoseNode> SublayoutNodes { get => sublayoutNodes; set => sublayoutNodes = value; }
        public bool IsSubLayoutRoot { get => isSubLayoutRoot; set => isSubLayoutRoot = value; }
        public bool IsSubLayoutNode { get => isSubLayoutNode; set => isSubLayoutNode = value; }
        public CoseNode SubLayoutRoot { get => subLayoutRoot; set => subLayoutRoot = value; }
        public NodeLayouts NodeLayout { get => nodeLayout; set => nodeLayout = value; }
        public CoseSublayout Sublayout { get => sublayout; set => sublayout = value; }



        /// <summary>
        /// updates the relative bounding rect
        /// </summary>
        /// <param name="left">the left position</param>
        /// <param name="right">the right position</param>
        /// <param name="top">the top position</param>
        /// <param name="bottom">the bottom position</param>
        public void UpdateRelativeRect(float left, float right, float top, float bottom)
        {
            relativeRect = new Rect(left, top, right - left, bottom - top);
        }
    }
}

