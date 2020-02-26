using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;


namespace SEE.Layout
{
    public class CoseSublayout
    {
        /// <summary>
        /// the layout of the sublayout
        /// </summary>
        private readonly GraphSettings.NodeLayouts nodeLayout;

        /// <summary>
        /// the root of the sublayout
        /// </summary>
        private readonly CoseNode root;

        /// <summary>
        /// Indicates whether the sublayout contains only leaf nodes
        /// </summary>
        private readonly bool onlyLeaves;

        /// <summary>
        /// a map from every node to the corresponding gameobject
        /// </summary>
        private Dictionary<Node, GameObject> nodeMap;

        /// <summary>
        /// a map from every sublayout node to the corresponding gameobject
        /// </summary>
        private Dictionary<CoseNode, GameObject> nodeMapSublayout;

        /// <summary>
        /// the y co-ordinate setting the ground level; all nodes will be placed on this level
        /// </summary>
        private readonly float groundLevel;

        /// <summary>
        /// the factory used to created leaf nodes
        /// </summary>
        private readonly NodeFactory leafNodeFactory;

        /// <summary>
        /// he height of objects (y co-ordinate) drawn for inner nodes
        /// </summary>
        private readonly float innerNodeHeight;

        public GraphSettings.NodeLayouts NodeLayout => nodeLayout;
        public CoseNode Root => root;
        public bool OnlyLeaves => onlyLeaves;
        public Dictionary<Node, GameObject> NodeMap { get => nodeMap; set => nodeMap = value; }
        public Dictionary<CoseNode, GameObject> NodeMapSublayout { get => nodeMapSublayout; set => nodeMapSublayout = value; }
        public float GroundLevel => groundLevel;
        public NodeFactory LeafNodeFactory => leafNodeFactory;
        public float InnerNodeHeight => innerNodeHeight;
    }
}

