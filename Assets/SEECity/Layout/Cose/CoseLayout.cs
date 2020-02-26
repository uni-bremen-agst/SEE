using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using static SEE.GraphSettings;

namespace SEE.Layout
{
    public class CoseLayout : NodeLayout
    {
        /// <summary>
        /// Graphmanager
        /// </summary>
        private CoseGraphManager graphManager;

        /// <summary>
        /// List with all Edge Objects
        /// </summary>
        private List<Edge> edges = new List<Edge>();

        /// <summary>
        /// Mapping from Node to CoseNode
        /// </summary>
        private Dictionary<Node, CoseNode> nodeToCoseNode;

        /// <summary>
        /// Mapping from CoseNode to Node
        /// </summary>
        private Dictionary<CoseNode, Node> coseNodeToNode;

        /// <summary>
        /// Layout Settings (idealEdgeLength etc.)
        /// </summary>
        private CoseLayoutSettings coseLayoutSettings;

        /// <summary>
        /// Grid for smart ideal edge length calculation
        /// </summary>
        private List<CoseNode>[,] grid;

        /// <summary>
        /// List with sublayouts, choosed by the user
        /// </summary>
        private Dictionary<string, NodeLayouts> sublayouts = new Dictionary<string, NodeLayouts>();

        /// <summary>
        /// Collection with all gamenodes
        /// </summary>
        private ICollection<GameObject> gameNodes;

        /// <summary>
        /// Indicates Whether the inner nodes are circles
        /// </summary>
        private bool innerNodesAreCircles;

        /// <summary>
        /// Result of the Layout
        /// </summary>
        Dictionary<GameObject, NodeTransform> layoutResult;

        /// <summary>
        /// A list with all graph managers (multilevel scaling)
        /// </summary>
        private List<CoseGraphManager> gmList;

        /// <summary>
        /// the settings for this graph
        /// </summary>
        private GraphSettings settings;

        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }
        public List<Edge> Edges { get => edges; set => edges = value; }
        public Dictionary<Node, CoseNode> NodeToCoseNode { get => nodeToCoseNode; set => nodeToCoseNode = value; }
        public Dictionary<CoseNode, Node> CoseNodeToNode { get => coseNodeToNode; set => coseNodeToNode = value; }
        public CoseLayoutSettings CoseLayoutSettings { get => coseLayoutSettings; set => coseLayoutSettings = value; }
        public List<CoseNode>[,] Grid { get => grid; set => grid = value; }
        public Dictionary<string, NodeLayouts> Sublayouts { get => sublayouts; set => sublayouts = value; }
        public ICollection<GameObject> GameNodes { get => gameNodes; set => gameNodes = value; }
        public bool InnerNodesAreCircles { get => innerNodesAreCircles; set => innerNodesAreCircles = value; }
        public Dictionary<GameObject, NodeTransform> LayoutResult { get => layoutResult; set => layoutResult = value; }
        public List<CoseGraphManager> GmList { get => gmList; set => gmList = value; }
        public GraphSettings Settings { get => settings; set => settings = value; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        /// <param name="edges">List of Edges</param>
        /// <param name="coseGraphSettings">Graph Settings, choosed by user</param>
        public CoseLayout(float groundLevel, NodeFactory leafNodeFactory, bool isCircle, List<Edge> edges, GraphSettings settings) : base(groundLevel, leafNodeFactory)
        {
            throw new System.NotImplementedException();
        }

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            throw new System.NotImplementedException();
        }
    }
}

