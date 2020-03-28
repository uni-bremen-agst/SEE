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

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="layout">the layout</param>
        public CoseGraphManager(CoseLayout layout)
        {
            this.layout = layout;
            Init();
        }

        /// <summary>
        /// initalizes the graph manager
        /// </summary>
        private void Init()
        {
            graphs = new List<CoseGraph>();
            edges = new List<CoseEdge>();
            allNodes = null;
            allEdges = null;
            nodesToApplyGravitation = null;
            rootGraph = null;
        }

        /// <summary>
        /// Adds the root graph to this graph manager
        /// </summary>
        /// <returns></returns>
        public CoseGraph AddRootGraph()
        {
            rootGraph = Add(new CoseGraph(null, this), new CoseNode(null, this));
            return rootGraph;
        }

        /// <summary>
        /// Sets this graph to root graph
        /// </summary>
        /// <param name="graph">the graph</param>
        public void setRootGraph(CoseGraph graph)
        {
            if (graph.GraphManager != this)
            {
                throw new System.Exception("Root not in this graph manager");
            }

            rootGraph = graph;

            if (graph.Parent == null)
            {
                graph.Parent = layout.NewNode();
            }
        }

        /// <summary>
        /// Adds a graph to this graphManager
        /// </summary>
        /// <param name="newGraph">the new graph</param>
        /// <param name="parent">the parent node of the graph</param>
        /// <returns>the graph</returns>
        public CoseGraph Add(CoseGraph newGraph, CoseNode parent)
        {
            if (newGraph == null || parent == null)
            {
                throw new System.Exception("Parameter is null");
            }
            if (graphs.Contains(newGraph))
            {
                throw new System.Exception("Graph is already in GraphManager");
            }

            graphs.Add(newGraph);

            if (newGraph.Parent != null || parent.Child != null)
            {
                throw new System.Exception("Elements already having this parameter");
            }

            newGraph.Parent = parent;
            parent.Child = newGraph;

            return newGraph;
        }

        /// <summary>
        /// Adds an edge to this graphManager
        /// </summary>
        /// <param name="newEdge">the new edge</param>
        /// <param name="cSource">the source of this edge</param>
        /// <param name="cTarget">the target of the edge</param>
        /// <returns></returns>
        public CoseEdge Add(CoseEdge newEdge, CoseNode cSource, CoseNode cTarget)
        {
            CoseGraph sourceGraph = cSource.Owner;
            CoseGraph targetGraph = cTarget.Owner;

            if (sourceGraph == null || sourceGraph.GraphManager != this)
            {
                throw new System.Exception("Source not in this graph manager");
            }

            if (targetGraph == null || targetGraph.GraphManager != this)
            {
                throw new System.Exception("Target not in this graph manager");
            }

            if (sourceGraph == targetGraph)
            {
                newEdge.IsInterGraph = false;
                return sourceGraph.Add(newEdge, cSource, cTarget);
            }
            else
            {
                newEdge.IsInterGraph = true;
                newEdge.Source = cSource;
                newEdge.Target = cTarget;

                if (edges.Contains(newEdge))
                {
                    throw new System.Exception("Edge already in inter-graph list");
                }
                edges.Add(newEdge);

                if (newEdge.Source == null || newEdge.Target == null)
                {
                    throw new System.Exception("Edge source or traget is null");
                }

                if (newEdge.Source.Edges.Contains(newEdge) || newEdge.Target.Edges.Contains(newEdge))
                {
                    throw new System.Exception("Edge is already in source or target edge list");

                }
                newEdge.Source.Edges.Add(newEdge);
                newEdge.Target.Edges.Add(newEdge);
                return newEdge;
            }
        }

        /// <summary>
        /// Updates the bounds of this graph, recursively, starting with the root graph
        /// </summary>
        public void UpdateBounds()
        {
            rootGraph.UpdateBounds(true);
        }

        /// <summary>
        /// Returns all nodes of the graphManager
        /// </summary>
        /// <returns></returns>
        public List<CoseNode> GetAllNodes()
        {
            if (allNodes == null)
            {
                List<CoseNode> nodeList = new List<CoseNode>();

                foreach (CoseGraph graph in graphs)
                {
                    nodeList.AddRange(graph.Nodes);
                }
                allNodes = nodeList;
            }

            return allNodes;
        }

        public void CalcInclusionTreeDepths()
        {
            CalcInclusionTreeDepth(RootGraph, 1);
        }


        /// <summary>
        /// TODO
        /// </summary>
        private void CalcInclusionTreeDepth(CoseGraph graph, int depth)
        {
            graph.Nodes.ForEach(node =>
            {
                node.InclusionTreeDepth = depth;

                if (node.Child != null)
                {
                    CalcInclusionTreeDepth(node.Child, depth +1);
                }
            });
        }


        /// <summary>
        /// calculates all lowest common anchestors for all edges
        /// </summary>
        public void CalcLowestCommonAncestors()
        {
            CoseNode sourceNode;
            CoseNode targetNode;
            CoseGraph sourceAnchestorGraph;
            CoseGraph targetAnchestorGraph;

            foreach (CoseEdge edge in GetAllEdges())
            {
                sourceNode = edge.Source;
                targetNode = edge.Target;
                edge.LowestCommonAncestor = null;
                edge.SourceInLca = sourceNode;
                edge.TargetInLca = targetNode;

                if (sourceNode == targetNode)
                {
                    edge.LowestCommonAncestor = sourceNode.Owner;
                    continue;
                }

                sourceAnchestorGraph = sourceNode.Owner;

                while (edge.LowestCommonAncestor == null)
                {
                    edge.TargetInLca = targetNode;
                    targetAnchestorGraph = targetNode.Owner;

                    while (edge.LowestCommonAncestor == null)
                    {
                        if (targetAnchestorGraph == sourceAnchestorGraph)
                        {
                            edge.LowestCommonAncestor = targetAnchestorGraph;
                            break;
                        }

                        if (targetAnchestorGraph == rootGraph)
                        {
                            break;
                        }

                        if (edge.LowestCommonAncestor != null)
                        {
                            throw new System.Exception("lowest anchestor should be null");
                        }
                        edge.TargetInLca = targetAnchestorGraph.Parent;
                        targetAnchestorGraph = edge.TargetInLca.Owner;
                    }

                    if (sourceAnchestorGraph == rootGraph)
                    {
                        break;
                    }

                    if (edge.LowestCommonAncestor == null)
                    {
                        edge.SourceInLca = sourceAnchestorGraph.Parent;
                        sourceAnchestorGraph = edge.SourceInLca.Owner;
                    }
                }

                if (edge.LowestCommonAncestor == null)
                {
                    throw new System.Exception("lowest common anchestor not allowed to be null");
                }
            }
        }

        /// <summary>
        /// Returns all edges of the graphManager
        /// </summary>
        /// <returns>all edges</returns>
        public List<CoseEdge> GetAllEdges()
        {
            if (allEdges == null)
            {
                List<CoseEdge> edgeList = new List<CoseEdge>();

                foreach (CoseGraph graph in graphs)
                {
                    edgeList.AddRange(graph.Edges);
                }

                edgeList.AddRange(edges);

                allEdges = edgeList;
            }

            return allEdges;
        }

        /// <summary>
        /// Creates all coarsen graphs for the multilevel scaling
        /// </summary>
        /// <returns> a list of coarsen graphManager</returns>
        public List<CoseGraphManager> CoarsenGraph()
        {
            List<CoseGraphManager> gmList = new List<CoseGraphManager>();
            int prevNodeCount;
            int currNodeCount;

            gmList.Add(this);

            CoseCoarsenGraph g = new CoseCoarsenGraph(layout);

            ConvertToCoarseningGraph(rootGraph, g);

            currNodeCount = g.Nodes.Count;

            CoseGraphManager lastM;
            CoseGraphManager newM;

            do
            {
                prevNodeCount = currNodeCount;
                g.Coarsen();

                lastM = gmList[gmList.Count - 1];
                newM = Coarsen(lastM);

                gmList.Add(newM);
                currNodeCount = g.Nodes.Count;

            } while ((prevNodeCount != currNodeCount) && currNodeCount > 1);

            layout.GraphManager = this;

            gmList.RemoveAt(gmList.Count - 1);

            return gmList;
        }

        /// <summary>
        /// Coarsen a graph manager (once)
        /// </summary>
        /// <param name="lastM"></param>
        /// <returns></returns>
        private CoseGraphManager Coarsen(CoseGraphManager lastM)
        {
            CoseGraphManager newM = new CoseGraphManager(lastM.Layout);

            newM.Layout.GraphManager = newM;
            newM.AddRootGraph();

            newM.RootGraph.GraphObject = lastM.RootGraph.GraphObject;

            CoarsenNodes(lastM.RootGraph, newM.RootGraph);

            lastM.Layout.GraphManager = lastM;

            return newM;
        }

        /// <summary>
        /// Coarsen the nodes of a given graph to a more coarser graph (once)
        /// </summary>
        /// <param name="graph">the uncoarsen graph</param>
        /// <param name="coarserGraph">the coarser graph</param>
        private void CoarsenNodes(CoseGraph graph, CoseGraph coarserGraph)
        {
            foreach (CoseNode node in graph.Nodes)
            {
                if (node.Child != null)
                {
                    node.LayoutValues.Next = coarserGraph.GraphManager.Layout.NewNode();
                    coarserGraph.GraphManager.Add(coarserGraph.GraphManager.Layout.NewGraph(), node.LayoutValues.Next);
                    node.LayoutValues.Next.LayoutValues.Pred1 = node;
                    coarserGraph.AddNode(node.LayoutValues.Next);

                    CoarsenNodes(node.Child, node.LayoutValues.Next.Child);
                }
                else
                {
                    if (!node.LayoutValues.Next.LayoutValues.IsProcessed)
                    {
                        coarserGraph.AddNode(node.LayoutValues.Next);
                        node.LayoutValues.Next.LayoutValues.IsProcessed = true;
                    }
                }

                node.LayoutValues.Next.SetLocation(node.rect.x, node.rect.y);
                node.LayoutValues.Next.SetHeight(node.rect.height);
                node.LayoutValues.Next.SetWidth(node.rect.width);
            }
        }

        /// <summary>
        /// Converts a graph to a coarsengraph
        /// </summary>
        /// <param name="coseGraph">the original graph</param>
        /// <param name="graph">the coarsen graph</param>
        private void ConvertToCoarseningGraph(CoseGraph coseGraph, CoseCoarsenGraph graph)
        {
            Dictionary<CoseNode, CoseCoarsenNode> dict = new Dictionary<CoseNode, CoseCoarsenNode>();

            foreach (CoseNode node in coseGraph.Nodes)
            {
                if (node.Child != null)
                {
                    ConvertToCoarseningGraph(node.Child, graph);
                }
                else
                {
                    CoseCoarsenNode coarsenNode = new CoseCoarsenNode();
                    coarsenNode.Reference = node;
                    dict.Add(node, coarsenNode);
                    graph.AddNode(coarsenNode);
                }
            }

            foreach (CoseEdge edge in coseGraph.Edges)
            {
                if (edge.Source.Child == null && edge.Target.Child == null)
                {
                    graph.Add(new CoseCoarsenEdge(), dict[edge.Source], dict[edge.Target]);
                }
            }
        }

        /// <summary>
        /// Removes an edge from this graphManager
        /// </summary>
        /// <param name="edge"></param>
        public void Remove(CoseEdge edge)
        {
            if (edge == null)
            {
                throw new System.Exception("Edge is null");
            }
            if (!edge.IsInterGraph)
            {
                throw new System.Exception("Edge is not an intergraph edge");
            }
            if (edge.Source == null || edge.Target == null)
            {
                throw new System.Exception("source or target is null");
            }
            if (!edge.Source.Edges.Contains(edge) || !edge.Target.Edges.Contains(edge))
            {
                throw new System.Exception("source or target doesnt know this edge");
            }

            edge.Source.Edges.Remove(edge);
            edge.Target.Edges.Remove(edge);

            if (edge.Source.Owner == null || edge.Source.Owner.GraphManager == null)
            {
                throw new System.Exception("Edge owner or edge owner graphmanager is null");
            }
            if (!edge.Source.Owner.GraphManager.Edges.Contains(edge))
            {
                throw new System.Exception("not in owner graph managers edge list");
            }

            edge.Source.Owner.GraphManager.Edges.Remove(edge);
        }

    }
}

