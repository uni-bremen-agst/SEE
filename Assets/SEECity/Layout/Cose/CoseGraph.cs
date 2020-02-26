using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SEE.DataModel;

namespace SEE.Layout
{
    public class CoseGraph 
    {
        /// <summary>
        /// the parent of the graph
        /// </summary>
        private CoseNode parent;

        /// <summary>
        /// the graphmanager of the current CoseLayout
        /// </summary>
        private CoseGraphManager graphManager;

        /// <summary>
        /// the nodes contained by this graph
        /// </summary>
        private List<CoseNode> nodes = new List<CoseNode>();

        /// <summary>
        /// edges of this graph
        /// </summary>
        private List<CoseEdge> edges = new List<CoseEdge>();

        /// <summary>
        /// top position of the graph
        /// </summary>
        private float top;

        /// <summary>
        /// left position of the graph
        /// </summary>
        private float left;

        /// <summary>
        /// bottom position of the graph
        /// </summary>
        private float bottom;

        /// <summary>
        /// right position of the graph
        /// </summary>
        private float right;

        /// <summary>
        /// the bounding rect of this graph
        /// </summary>
        public Rect boudingRect = new Rect();

        /// <summary>
        /// the esitmated size of this graph
        /// </summary>
        private float estimatedSize = Int32.MinValue;

        /// <summary>
        /// the margin around this graph
        /// </summary>
        private float defaultMargin = CoseLayoutSettings.Graph_Margin;

        /// <summary>
        /// Indicates if the graph is connected
        /// </summary>
        private bool isConnected = false;

        /// <summary>
        /// the original node
        /// </summary>
        private Node graphObject;

        /// <summary>
        /// Indicates if this graph is a sublayout
        /// </summary>
        private bool isSubLayout = false;

        public Node GraphObject { get => graphObject; set => graphObject = value; }
        public float Left { get => left; set => left = value; }
        public float Top { get => top; set => top = value; }
        public float Bottom { get => bottom; set => bottom = value; }
        public float Right { get => right; set => right = value; }
        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }
        public CoseNode Parent { get => parent; set => parent = value; }
        public List<CoseNode> Nodes { get => nodes; set => nodes = value; }
        public Rect BoudingRect { get => boudingRect; set => boudingRect = value; }
        public bool IsConnected { get => isConnected; set => isConnected = value; }
        public List<CoseEdge> Edges { get => edges; set => edges = value; }
        public float EstimatedSize { get => estimatedSize; set => estimatedSize = value; }
        public bool IsSubLayout { get => isSubLayout; set => isSubLayout = value; }
    }
}

