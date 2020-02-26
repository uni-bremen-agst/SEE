using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Layout
{
    public class CoseGraphManager 
    {
        /// <summary>
        /// the layout 
        /// </summary>
        private CoseLayout layout;

        /// <summary>
        /// all graphs 
        /// </summary>
        private List<CoseGraph> graphs;

        /// <summary>
        /// all intergraph edges
        /// </summary>
        private List<CoseEdge> edges;

        /// <summary>
        /// all nodes
        /// </summary>
        private List<CoseNode> allNodes;

        /// <summary>
        /// all edges
        /// </summary>
        private List<CoseEdge> allEdges;

        /// <summary>
        /// the root graph
        /// </summary>
        private CoseGraph rootGraph;

        /// <summary>
        /// nodes to which gravitation is applied to 
        /// </summary>
        private List<CoseNode> nodesToApplyGravitation;

        public List<CoseGraph> Graphs { get => graphs; set => graphs = value; }
        public List<CoseEdge> Edges { get => edges; set => edges = value; }
        public List<CoseNode> AllNodes { get => allNodes; set => allNodes = value; }
        public List<CoseEdge> AllEdges { get => allEdges; set => allEdges = value; }
        public CoseGraph RootGraph { get => rootGraph; set => rootGraph = value; }
        public List<CoseNode> NodesToApplyGravitation { get => nodesToApplyGravitation; set => nodesToApplyGravitation = value; }
        public CoseLayout Layout { get => layout; set => layout = value; }
    }
}

