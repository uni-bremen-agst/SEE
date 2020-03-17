using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using static SEE.GraphSettings;
using System.Linq;
using System;

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
        /// TODO
        /// </summary>
        private List<SublayoutNode> sublayoutNodes = new List<SublayoutNode>();

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
        public CoseLayoutSettings CoseLayoutSettings { get => coseLayoutSettings; set => coseLayoutSettings = value; }
        public Dictionary<string, NodeLayouts> Sublayouts { get => sublayouts; set => sublayouts = value; }
        public bool InnerNodesAreCircles { get => innerNodesAreCircles; set => innerNodesAreCircles = value; }
        public List<SublayoutNode> SublayoutNodes { get => sublayoutNodes; set => sublayoutNodes = value; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        /// <param name="edges">List of Edges</param>
        /// <param name="coseGraphSettings">Graph Settings, choosed by user</param>
        public CoseLayout(float groundLevel, NodeFactory leafNodeFactory, bool isCircle, List<Edge> edges, GraphSettings settings, List<SublayoutNode> coseSublayoutNodes) : base(groundLevel, leafNodeFactory)
        {
            name = "Compound Spring Embedder Layout";
            this.edges = edges;
            this.nodeToCoseNode = new Dictionary<Node, CoseNode>();
            this.innerNodesAreCircles = isCircle;
            this.settings = settings;
            this.SublayoutNodes = coseSublayoutNodes;
            SetupGraphSettings(settings.CoseGraphSettings);
        }

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            this.gameNodes = gameNodes;
            layoutResult = new Dictionary<GameObject, NodeTransform>();

            List<Node> roots = GetRoots(gameNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root node.");
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }

            to_game_node = NodeMapping(gameNodes);
            Node root = roots[0];
            PlaceNodes(root);
            PlacePositionNodes(graphManager.RootGraph);

            foreach (CoseGraph graph in graphManager.Graphs)
            {
                float width = graph.BoudingRect.width;
                float height = graph.BoudingRect.height;

                if (graph.Parent != null && graph.Parent.NodeObject != null && SublayoutNodes.Count() > 0)
                {
                    SublayoutNode sublayoutNode = CoseHelperFunctions.CheckIfNodeIsSublayouRoot(SublayoutNodes, graph.Parent.NodeObject);

                    if (sublayoutNode != null && sublayoutNode.NodeLayout == NodeLayouts.EvoStreets)
                    {
                        width = graph.Parent.SublayoutValues.Sublayout.LayoutScale.x;
                        height = graph.Parent.SublayoutValues.Sublayout.LayoutScale.z;
                    } 
                }

                Vector3 position = new Vector3(graph.BoudingRect.center.x, groundLevel, graph.BoudingRect.center.y);

                if (graph != graphManager.RootGraph)
                {
                    position.y += LevelLift(graph.Parent.NodeObject);
                }

                layoutResult[to_game_node[graph.GraphObject]] = new NodeTransform(position, new Vector3(width, innerNodeHeight, height));
            }
            to_game_node = null;
            return layoutResult;
        }


        /// <summary>
        /// Setup function for the CoseGraphSettings
        /// Values for Graph Layout Settings and Setup for Layouts
        /// </summary>
        /// <param name="settings">Graph Settings, choosed by user</param>
        private void SetupGraphSettings(CoseGraphSettings settings)
        {
            CoseLayoutSettings.Edge_Length = settings.EdgeLength;
            CoseLayoutSettings.Use_Smart_Ideal_Edge_Calculation = settings.UseSmartIdealEdgeCalculation;
            CoseLayoutSettings.Per_Level_Ideal_Edge_Length_Factor = settings.PerLevelIdealEdgeLengthFactor;
            CoseLayoutSettings.Incremental = settings.Incremental;
            CoseLayoutSettings.Use_Smart_Repulsion_Range_Calculation = settings.UseSmartRepulsionRangeCalculation;
            CoseLayoutSettings.Gravity_Strength = settings.GravityStrength;
            CoseLayoutSettings.Compound_Gravity_Strength = settings.CompoundGravityStrength;
            CoseLayoutSettings.Repulsion_Strength = settings.RepulsionStrength;
            CoseLayoutSettings.Multilevel_Scaling = settings.multiLevelScaling;

            coseLayoutSettings = new CoseLayoutSettings();

            FilterSubLayouts();
        }

        /// <summary>
        /// Filter for the Sublayouts (choosed by the user) 
        /// </summary>
        private void FilterSubLayouts()
        {
            SublayoutNodes.RemoveAll(node => node.NodeLayout == NodeLayouts.CompoundSpringEmbedder);
        }

        /// <summary>
        /// Sets the scale to the CoseNodes
        /// </summary>
        /// <param name="gameNodes"></param>
        private void SetScale(ICollection<GameObject> gameNodes)
        {
            List<Node> roots = GetRoots(gameNodes);

            foreach (GameObject gameObject in gameNodes)
            {
                var size = leafNodeFactory.GetSize(gameObject);
                Node node = gameObject.GetComponent<NodeRef>().node;
                if (roots.Contains(node))
                {
                    break;
                }
                CoseNode coseNode = nodeToCoseNode[node];

                if (node.IsLeaf())
                {
                    coseNode.SetWidth(size.x);
                    coseNode.SetHeight(size.z);
                }
            }
        }

        /// <summary>
        /// places the original node objects to the clauclated positions
        /// </summary>
        /// <param name="root"></param>
        private void PlacePositionNodes(CoseGraph root)
        {

            foreach (CoseNode node in root.Nodes)
            {
                if (node.IsLeaf())
                {
                    Node nNode = node.NodeObject;
                    NodeTransform transform = new NodeTransform(new Vector3((float)node.GetCenterX(), groundLevel, (float)node.GetCenterY()), new Vector3(node.rect.width, groundLevel, node.rect.height));
                    layoutResult[to_game_node[nNode]] = transform;
                }

                if (node.Child != null)
                {
                    PlacePositionNodes(node.Child);
                }
            }
        }

        /// <summary>
        /// Places the Nodes 
        /// </summary>
        /// <param name="root">Root Node</param>
        private void PlaceNodes(Node root)
        {
            CreateTopology(root);
            CalculateSubLayouts(graphManager.RootGraph.Parent);

            if (SublayoutNodes.Count > 0 && CoseHelperFunctions.CheckIfNodeIsSublayouRoot(SublayoutNodes, graphManager.RootGraph.Parent.NodeObject) != null)
            {
                // nothing to do
            } else
            {
                if (CoseLayoutSettings.Multilevel_Scaling)
                {
                    // TODO sollte das auch in der Gui dann abgehakt werden?
                    CoseLayoutSettings.Incremental = false;
                    MultiLevelScaling();
                }
                else
                {
                    ClassicLayout();
                }
            } 
        }

        /// <summary>
        /// Starts the layout process of a classic layout
        /// </summary>
        private void ClassicLayout()
        {
            CalculateNodesToApplyGravityTo();
            CalcNoOfChildrenForAllNodes();
            graphManager.CalcLowestCommonAncestors();
            graphManager.CalcInclusionTreeDepths();
            graphManager.RootGraph.CalcEstimatedSize();
            CalcIdealEdgeLength();
            InitSpringEmbedder();
            RunSpringEmbedder();
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void CalcNoOfChildrenForAllNodes()
        {
            graphManager.GetAllNodes().ForEach(node => node.NoOfChildren = node.CalcNumberOfChildren());
        }

        /// <summary>
        /// Starts the layout process of a multi level scaling layout
        /// </summary>
        private void MultiLevelScaling()
        {
            Debug.Log("multilevel scaling");

            CoseGraphManager gm = graphManager;
            gmList = gm.CoarsenGraph();

            coseLayoutSettings.NoOfLevels = gmList.Count - 1;
            coseLayoutSettings.Level = coseLayoutSettings.NoOfLevels;

            while (coseLayoutSettings.Level >= 0)
            {
                graphManager = gmList[coseLayoutSettings.Level];
                ClassicLayout();
                CoseLayoutSettings.Incremental = true;

                if (coseLayoutSettings.Level >= 1)
                {
                    Uncoarsen();
                }

                coseLayoutSettings.TotalIterations = 0;
                coseLayoutSettings.Level--;
            }
            CoseLayoutSettings.Incremental = false;
        }

        /// <summary>
        /// uncoarsen all nodes from the layouts graph manager
        /// </summary>
        private void Uncoarsen()
        {
            foreach (CoseNode node in graphManager.GetAllNodes())
            {
                node.LayoutValues.Pred1.SetLocation(node.GetLeft(), node.GetTop());
                if (node.LayoutValues.Pred2 != null)
                {
                    node.LayoutValues.Pred2.SetLocation(node.GetLeft() + CoseLayoutSettings.Edge_Length, node.GetTop() + CoseLayoutSettings.Edge_Length);
                }
            }
        }


        /// <summary>
        /// calculates the sublayouts
        /// </summary>
        /// <param name="graph">the graph for which the sublayout is calculated</param>
        private void CalculateSubLayouts(CoseNode root)
        {
            // sind immernoch nach Leveln sortiert
            foreach (SublayoutNode sublayoutNode in sublayoutNodes)
            {
                List<CoseNode> allNodes = sublayoutNode.Nodes.Select(node => nodeToCoseNode[node]).ToList();
                List<CoseNode> removedNodes = sublayoutNode.RemovedChildren.Select(node => nodeToCoseNode[node]).ToList();
                CoseSublayout sublayout = new CoseSublayout(nodeToCoseNode[sublayoutNode.Node], to_game_node, groundLevel, leafNodeFactory, innerNodeHeight, sublayoutNode.NodeLayout, allNodes, removedNodes);
                sublayout.Layout();
            }
        }

    
        /// <summary>
        /// Runs the spring embedder 
        /// </summary>
        private void RunSpringEmbedder()
        {
            graphManager.UpdateBounds();
            do
            {
                coseLayoutSettings.TotalIterations++;
                if (coseLayoutSettings.TotalIterations % CoseLayoutSettings.Convergence_Check_Periode == 0)
                {
                    if (IsConverged())
                    {
                        break;
                    }
                    coseLayoutSettings.Coolingcycle++;
                    //CoolingFactor = initialCoolingFactor * ((maxIterations - totalIterations) / maxIterations);
                    // TODO
                    // based on www.btluke.com/simanf1.html, schedule 3
                    
                    coseLayoutSettings.CoolingFactor = Math.Max(coseLayoutSettings.InitialCoolingFactor - Math.Pow(coseLayoutSettings.Coolingcycle, Math.Log(100 * (coseLayoutSettings.InitialCoolingFactor - coseLayoutSettings.FinalTemperature)) / Math.Log(coseLayoutSettings.MaxCoolingCycle)) / 100 * CoseLayoutSettings.Cooling_Adjuster, coseLayoutSettings.FinalTemperature);
                    //Debug.Log("cooling: " + coseLayoutSettings.CoolingFactor);
                }

                coseLayoutSettings.TotalDisplacement = 0;
                CalcSpringForces();
                CalcRepulsionForces();
                CalcGravitationalForces();
                MoveNodes();
                graphManager.UpdateBounds();
                ResetForces();

            } while (coseLayoutSettings.TotalIterations < coseLayoutSettings.MaxIterations);
            graphManager.UpdateBounds();
        }

        /// <summary>
        /// Calculates the spring forces for all edges
        /// </summary>
        private void CalcSpringForces()
        {
            List<CoseEdge> edges = GetAllEdges();

            foreach (CoseEdge edge in edges)
            {
                CalcSpringForce(edge, edge.IdealEdgeLength);
            }
        }

        /// <summary>
        /// Calculates the spring force for one edge
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="idealEdgeLength"></param>
        private void CalcSpringForce(CoseEdge edge, double idealEdgeLength)
        {
            CoseNode source = edge.Source;
            CoseNode target = edge.Target;

            double length;
            double springForce;
            double springForceX;
            double springForceY;

            if (CoseLayoutSettings.Uniform_Leaf_Node_Size && source.Child == null && target.Child == null)
            {
                edge.UpdateLengthSimple();
            }
            else
            {
                edge.UpdateLenght();

                if (edge.IsOverlappingSourceAndTarget)
                {
                    return;
                }
            }

            length = edge.Length;

            double dl = length - idealEdgeLength;

            if (dl == 0.0)
            {
                throw new System.Exception("Length cant be 0.0");
            }

            springForce = CoseLayoutSettings.Spring_Strength * dl;
            springForceX = springForce * (edge.LengthX / length);
            springForceY = springForce * (edge.LengthY / length);

            source.LayoutValues.SpringForceX += springForceX;
            source.LayoutValues.SpringForceY += springForceY;
            target.LayoutValues.SpringForceX -= springForceX;
            target.LayoutValues.SpringForceY -= springForceY;
        }

        /// <summary>
        /// Returns all edges of this layout
        /// </summary>
        /// <returns></returns>
        private List<CoseEdge> GetAllEdges()
        {
            return graphManager.GetAllEdges();
        }

        /// <summary>
        /// calculates the repuslion forces for all nodes
        /// </summary>
        private void CalcRepulsionForces()
        {
            int i;
            int j;
            List<CoseNode> nodes = GetAllNodes();
            HashSet<CoseNode> processedNodeSet;

            if (CoseLayoutSettings.Use_Smart_Repulsion_Range_Calculation)
            {
                // TODO Sublayout
                if ((coseLayoutSettings.TotalIterations % CoseLayoutSettings.Grid_Calculation_Check_Periode) == 1)
                {
                    grid = CalcGrid(graphManager.RootGraph);

                    foreach (CoseNode node in nodes)
                    {
                        AddNodeToGrid(node, graphManager.RootGraph.Left, graphManager.RootGraph.Top);
                    }
                }

                processedNodeSet = new HashSet<CoseNode>();

                foreach (CoseNode node in nodes)
                {
                    CalculateRepulsionForceForNode(node, processedNodeSet);
                    processedNodeSet.Add(node);
                }
            }
            else
            {
                for (i = 0; i < nodes.Count; i++)
                {
                    CoseNode nodeA = nodes[i];

                    for (j = i + 1; j < nodes.Count; j++)
                    {
                        CoseNode nodeB = nodes[j];

                        if (nodeA.Owner != nodeB.Owner) 
                        {
                            continue;
                        }

                        CalcRepulsionForce(nodeA, nodeB);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the repulsion force for one node
        /// </summary>
        /// <param name="nodeA"></param>
        /// <param name="processedNodeSet"></param>
        private void CalculateRepulsionForceForNode(CoseNode nodeA, HashSet<CoseNode> processedNodeSet)
        {
            int i;
            int j;

            if (coseLayoutSettings.TotalIterations % CoseLayoutSettings.Grid_Calculation_Check_Periode == 1)
            {
                HashSet<CoseNode> surrounding = new HashSet<CoseNode>();

                for (i = (nodeA.LayoutValues.StartX - 1); i < (nodeA.LayoutValues.FinishX + 2); i++)
                {
                    for (j = (nodeA.LayoutValues.StartY - 1); j < (nodeA.LayoutValues.FinishY + 2); j++)
                    {

                        if (!((i < 0) || (j < 0) || (i >= grid.GetLength(0)) || (j >= grid.GetLength(1))))
                        {
                            var list = grid[i, j];



                            for (var k = 0; k < list.Count; k++)
                            {
                                var nodeB = list[k];

                                if ((nodeA.Owner != nodeB.Owner) || (nodeA == nodeB))
                                {
                                    continue;
                                }

                                if (!processedNodeSet.Contains(nodeB) && !surrounding.Contains(nodeB))
                                {
                                    double distanceX = Math.Abs(nodeA.GetCenterX() - nodeB.GetCenterX()) - ((nodeA.rect.width / 2) + (nodeB.rect.width / 2));
                                    double distanceY = Math.Abs(nodeA.GetCenterY() - nodeB.GetCenterY()) - ((nodeA.rect.height / 2) + (nodeB.rect.height / 2));
                                    if ((distanceX <= coseLayoutSettings.RepulsionRange) && (distanceY <= coseLayoutSettings.RepulsionRange))
                                    {
                                        surrounding.Add(nodeB);
                                    }
                                }
                            }
                        }
                    }
                }
                nodeA.Surrounding = surrounding.ToList<CoseNode>();
            }

            foreach (CoseNode node in nodeA.Surrounding)
            {

                //Debug.Log("node A, node "+ nodeA.SourceName + " "+ node.SourceName);
                CalcRepulsionForce(nodeA, node);

            }
        }

        /// <summary>
        /// Calculates the repulsion force between two nodes
        /// </summary>
        /// <param name="nodeA"></param>
        /// <param name="nodeB"></param>
        private void CalcRepulsionForce(CoseNode nodeA, CoseNode nodeB)
        {
            var rectA = nodeA.rect;
            var rectB = nodeB.rect;
            double[] overlapAmount = new double[2];
            double[] clipPoints = new double[4];
            double distanceX;
            double distanceY;
            double distanceSquared;
            double distance;
            double repulsionForce;
            double repulsionForceX;
            double repulsionForceY;

            if (nodeA.CalcOverlap(nodeB, overlapAmount))
            {
                repulsionForceX = 2 * overlapAmount[0];
                repulsionForceY = 2 * overlapAmount[1];

                double childrenConstant = nodeA.NoOfChildren * nodeB.NoOfChildren / (double)(nodeA.NoOfChildren + nodeB.NoOfChildren);

                nodeA.LayoutValues.RepulsionForceX -= childrenConstant * repulsionForceX;
                nodeA.LayoutValues.RepulsionForceY -= childrenConstant * repulsionForceY;

                nodeB.LayoutValues.RepulsionForceX += childrenConstant * repulsionForceX;
                nodeB.LayoutValues.RepulsionForceY += childrenConstant * repulsionForceY;
            }
            else
            {
                if (CoseLayoutSettings.Uniform_Leaf_Node_Size && nodeA.Child == null && nodeB.Child == null)
                {
                    distanceX = ((rectB.x + rectB.width) / 2) - ((rectA.x + rectA.width) / 2);
                    distanceY = ((rectB.y + rectB.height) / 2) - ((rectA.y + rectA.height) / 2);
                } else
                {
                    clipPoints = nodeA.CalcIntersection(nodeB, clipPoints);
                    distanceX = clipPoints[2] - clipPoints[0];
                    distanceY = clipPoints[3] - clipPoints[1];
                }

                if (Math.Abs(distanceX) < CoseLayoutSettings.Min_Repulsion_Dist)
                {
                    distanceX = Math.Sign(distanceX) * CoseLayoutSettings.Min_Repulsion_Dist;
                }

                if (Math.Abs(distanceY) < CoseLayoutSettings.Min_Repulsion_Dist)
                {
                    distanceY = Math.Sign(distanceY) * CoseLayoutSettings.Min_Repulsion_Dist;
                }

                distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
                distance = Math.Sqrt(distanceSquared);

                repulsionForce = CoseLayoutSettings.Repulsion_Strength * nodeA.NoOfChildren * nodeB.NoOfChildren / distanceSquared;

                repulsionForceX = repulsionForce * distanceX / distance;
                repulsionForceY = repulsionForce * distanceY / distance;

                nodeA.LayoutValues.RepulsionForceX -= repulsionForceX;
                nodeA.LayoutValues.RepulsionForceY -= repulsionForceY;

                nodeB.LayoutValues.RepulsionForceX += repulsionForceX;
                nodeB.LayoutValues.RepulsionForceY += repulsionForceY;
            }
        }

        /// <summary>
        /// Adds a node to the grid (multi level scaling)
        /// </summary>
        /// <param name="v">the node</param>
        /// <param name="left">the left position</param>
        /// <param name="top">the top position</param>
        private void AddNodeToGrid(CoseNode v, double left, double top)
        {
            int startX = (int)Math.Floor((v.rect.x - left) / coseLayoutSettings.RepulsionRange);
            int finishX = (int)Math.Floor((v.rect.width + v.rect.x - left) / coseLayoutSettings.RepulsionRange);
            int startY = (int)Math.Floor((v.rect.y - top) / coseLayoutSettings.RepulsionRange);
            int finishY = (int)Math.Floor((v.rect.height + v.rect.y - top) / coseLayoutSettings.RepulsionRange);

            for (int i = startX; i <= finishX; i++)
            {
                for (int j = startY; j <= finishY; j++)
                {
                    grid[i, j].Add(v);
                    v.SetGridCoordinates(startX, finishX, startY, finishY);
                }
            }
        }

        /// <summary>
        /// calculates the grid for multi level scaling
        /// </summary>
        /// <param name="graph">the graph to calcualte the grid for</param>
        /// <returns>the grid</returns>
        private List<CoseNode>[,] CalcGrid(CoseGraph graph)
        {
            int i;
            int j;

            List<CoseNode>[,] collect;

            var sizeX = (int)Mathf.Ceil((float)((graph.Right - graph.Left) / coseLayoutSettings.RepulsionRange));
            var sizeY = (int)Mathf.Ceil((float)((graph.Bottom - graph.Top) / coseLayoutSettings.RepulsionRange));

            collect = new List<CoseNode>[sizeX, sizeY];

            for (i = 0; i < sizeX; i++)
            {
                for (j = 0; j < sizeY; j++)
                {
                    collect[i, j] = new List<CoseNode>();
                }
            }
            return collect;
        }

        /// <summary>
        /// Calculates the gravitational forces for a node
        /// </summary>
        /// <param name="node"></param>
        private void CalcGravitationalForce(CoseNode node)
        {
            if (node.LayoutValues.GravitationForceX != 0 && node.LayoutValues.GravitationForceY != 0)
            {
                throw new System.Exception("gravitational force needs to be 0.0");
            }
            CoseGraph ownerGraph;
            double ownerCenterX;
            double ownerCenterY;
            double distanceX;
            double distanceY;
            double absDistanceX;
            double absDistanceY;
            int estimatedSize;

            ownerGraph = node.Owner;
            ownerCenterX = ownerGraph.BoudingRect.center.x; 
            ownerCenterY = ownerGraph.BoudingRect.center.y;
            distanceX = node.GetCenterX() - ownerCenterX;
            distanceY = node.GetCenterY() - ownerCenterY;
            absDistanceX = Math.Abs(distanceX) + node.rect.width / 2;
            absDistanceY = Math.Abs(distanceY) + node.rect.height / 2;

            if (node.Owner == graphManager.RootGraph)
            {
                estimatedSize = (int)(ownerGraph.EstimatedSize * CoseLayoutSettings.Gravity_Range_Factor);

                if (absDistanceX > estimatedSize || absDistanceY > estimatedSize)
                {
                    node.LayoutValues.GravitationForceX = -CoseLayoutSettings.Gravity_Strength * distanceX;
                    node.LayoutValues.GravitationForceY = -CoseLayoutSettings.Gravity_Strength * distanceY;
                }
            }
            else
            {
                estimatedSize = (int)(ownerGraph.EstimatedSize * CoseLayoutSettings.Compound_Gravity_Range_Factor);

                if (absDistanceX > estimatedSize || absDistanceY > estimatedSize)
                {
                    node.LayoutValues.GravitationForceX = -CoseLayoutSettings.Gravity_Strength * distanceX * CoseLayoutSettings.Compound_Gravity_Strength;
                    node.LayoutValues.GravitationForceY = -CoseLayoutSettings.Gravity_Strength * distanceY * CoseLayoutSettings.Compound_Gravity_Strength;
                }
            }
        }

        /// <summary>
        /// Calculates the gravitational forces for all nodes of this layout
        /// </summary>
        private void CalcGravitationalForces()
        {
            List<CoseNode> nodes = graphManager.NodesToApplyGravitation;

            foreach (CoseNode node in nodes)
            {
                CalcGravitationalForce(node);
            }
        }

        /// <summary>
        /// Move all nodes 
        /// </summary>
        private void MoveNodes()
        {
            List<CoseNode> nodes = GetAllNodes();
            foreach (CoseNode node in nodes)
            {
                if (!node.SublayoutValues.IsSubLayoutNode || node.SublayoutValues.IsSubLayoutRoot)
                {
                    node.Move();
                }
            }
        }

        /// <summary>
        /// Resets all forces acting on the nodes of this layout
        /// </summary>
        private void ResetForces()
        {
            List<CoseNode> nodes = GetAllNodes();
            foreach (CoseNode node in nodes)
            {
                node.Reset();
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        private bool IsConverged()
        {
            bool converged;
            bool oscilating = false;

            if (coseLayoutSettings.TotalIterations > coseLayoutSettings.MaxIterations / 3)
            {
                oscilating = Math.Abs(coseLayoutSettings.TotalDisplacement - coseLayoutSettings.OldTotalDisplacement) < 2;
            }
            converged = coseLayoutSettings.TotalDisplacement < coseLayoutSettings.TotalDisplacementThreshold;
            coseLayoutSettings.OldTotalDisplacement = coseLayoutSettings.TotalDisplacement;
            return converged || oscilating;
        }

        /// <summary>
        /// Initalizes the spring embedder
        /// </summary>
        private void InitSpringEmbedder()
        {
            if (CoseLayoutSettings.Incremental)
            {
                coseLayoutSettings.CoolingFactor = 0.8;
                coseLayoutSettings.InitialCoolingFactor = 0.8;
                coseLayoutSettings.MaxNodeDisplacement = CoseLayoutSettings.Max_Node_Displacement_Incremental;
            }
            else
            {
                coseLayoutSettings.CoolingFactor = 1.0;
                coseLayoutSettings.InitialCoolingFactor = 1.0;
                coseLayoutSettings.MaxNodeDisplacement = coseLayoutSettings.MaxNodeDisplacement;
            }

            coseLayoutSettings.MaxIterations = Math.Max(GetAllNodes().Count * 5, coseLayoutSettings.MaxIterations);
            coseLayoutSettings.TotalDisplacementThreshold = coseLayoutSettings.DisplacementThresholdPerNode * GetAllNodes().Count;
            coseLayoutSettings.RepulsionRange = CalcRepulsionRange();
        }

        /// <summary>
        /// Calculates the repulsion range
        /// </summary>
        /// <returns></returns>
        private double CalcRepulsionRange()
        {
            double repulsionRange = (2 * (coseLayoutSettings.Level + 1) * CoseLayoutSettings.Edge_Length);
            return repulsionRange;

        }

        /// <summary>
        /// Calculates the ideal edge length for each edge
        /// </summary>
        private void CalcIdealEdgeLength()
        {
            int lcaDepth;
            CoseNode source;
            CoseNode target;
            int sizeOfSourceInLca;
            int sizeOfTargetInLca;

            foreach (CoseEdge edge in graphManager.GetAllEdges())
            {
                edge.IdealEdgeLength = CoseLayoutSettings.Edge_Length;

                if (edge.IsInterGraph)
                {
                    source = edge.Source;
                    target = edge.Target;

                    sizeOfSourceInLca = (int)Math.Round(edge.SourceInLca.EstimatedSize);
                    sizeOfTargetInLca = (int)Math.Round(edge.TargetInLca.EstimatedSize);

                    if (CoseLayoutSettings.Use_Smart_Ideal_Edge_Calculation)
                    {
                        edge.IdealEdgeLength += sizeOfSourceInLca + sizeOfTargetInLca - 2 * CoseLayoutSettings.Simple_Node_Size;
                    }

                    lcaDepth = edge.LowestCommonAncestor.GetInclusionTreeDepth();

                    edge.IdealEdgeLength += CoseLayoutSettings.Edge_Length * CoseLayoutSettings.Per_Level_Ideal_Edge_Length_Factor * (source.NodeObject.Depth() + target.NodeObject.Depth() - 2 * lcaDepth);
                }
                else
                {
                    /*// TODO
                    // density mit einbeziehen, aber nur in dem Graphen an sich

                    CoseGraph owner = edge.Source.Owner;
                    int edgeSum = 0;

                    foreach (CoseEdge e in owner.Edges)
                    {
                        if (!e.IsInterGraph)
                        {
                            edgeSum++;
                        }
                    }

                    if (edgeSum > 200)
                    {
                        edge.IdealEdgeLength = 300;
                    }*/
                }
            }
        }

        /// <summary>
        /// Indicates whether the edge is allowed in coselayout or not.
        /// All Edge between predecessors and successors in the same hierarchie (inclusion branch) are not allowed in the cose layout 
        /// </summary>
        /// <param name="edge">the edge to check</param>
        /// <returns>is Allowed in layout</returns>
        private bool ExcludeEdgesInSameHierarchie(Edge edge)
        {
            // Kanten zwischen Vorgängern und Nachfolgern in der gleichen Hierarchie werden vom Layout ausgeschlossen 

            Node source = edge.Source;

            Node target = edge.Target;

            if (target.Level == source.Level)
            {
                return true;
            }

            Node startNode = source.Level <= target.Level ? source : target;

            return CheckChildrenForHierachie(startNode, startNode.Children());
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="node"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        private bool CheckChildrenForHierachie(Node node, List<Node> children) 
        {
            foreach(Node child in children)
            {
                if (node.Equals(child))
                {
                    return false;
                }

                if (!CheckChildrenForHierachie(node, child.Children()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculates the Topology for the given graph structure
        /// </summary>
        /// <param name="root"></param>
        private void CreateTopology(Node root)
        {
            graphManager = new CoseGraphManager(this);

            //CreateNode(root, null);
            CoseNode rootNode = NewNode(root);
            CoseGraph rootGraph = graphManager.AddRootGraph();
            rootGraph.GraphObject = root;
            rootGraph.Parent = rootNode;
            rootNode.Child = rootGraph;
            this.nodeToCoseNode.Add(root, rootNode);

            foreach (Node child in root.Children())
            {
                CreateNode(child, null);
            }
            SetScale(gameNodes);
            Debug.Log("I am Groot");

            foreach (Edge edge in edges)
            {
                if (ExcludeEdgesInSameHierarchie(edge))
                {
                    CreateEdge(edge);
                } else
                {
                    Debug.Log("Edge: "+edge+" is excluded from the layout prozess, because the source and target node are in the same inclusion branch (hierarchie)");
                }
                
            }

            graphManager.UpdateBounds();
        }

        /// <summary>
        /// Creates a new node 
        /// </summary>
        /// <param name="node">the original node</param>
        /// <param name="parent">the parent node</param>
        private void CreateNode(Node node, CoseNode parent)
        {
            CoseNode cNode = NewNode(node);
            CoseGraph rootGraph = graphManager.RootGraph;

            this.nodeToCoseNode.Add(node, cNode);

            if (parent != null)
            {
                //CoseNode cParent = nodeToCoseNode[parent];
                if (parent.Child == null)
                {
                    throw new System.Exception("Parent Node doesnt have a child graph");
                }
                parent.Child.AddNode(cNode);
            }
            else
            {
                rootGraph.AddNode(cNode);
            }

            cNode.SetLocation(to_game_node[node].transform.position.x, to_game_node[node].transform.position.z);

            if (!node.IsLeaf())
            {

                CoseGraph graph = new CoseGraph(null, this.graphManager);
                graph.GraphObject = node;
                graphManager.Add(graph, cNode);

                foreach (Node child in cNode.NodeObject.Children())
                {
                    CreateNode(child, cNode);
                }

                cNode.UpdateBounds();
            }
            else
            {
                Vector3 size = leafNodeFactory.GetSize(to_game_node[node]);
                cNode.SetWidth(size.x);
                cNode.SetHeight(size.z);
            }
        }

        /// <summary>
        /// Creates a new graph
        /// </summary>
        /// <returns>the new graph</returns>
        public CoseGraph NewGraph()
        {
            return new CoseGraph(null, graphManager);
        }

        /// <summary>
        /// Creates a new node
        /// </summary>
        /// <param name="node"></param>
        /// <returns>the new node</returns>
        private CoseNode NewNode(Node node)
        {
            return new CoseNode(node, graphManager);
        }

        /// <summary>
        /// Creates a new edge 
        /// </summary>
        /// <param name="edge">the new edge </param>
        private void CreateEdge(Edge edge)
        {
            CoseEdge cEdge = new CoseEdge(nodeToCoseNode[edge.Source], nodeToCoseNode[edge.Target]);

            graphManager.Add(cEdge, cEdge.Source, cEdge.Target);
        }

        /// <summary>
        /// - finds a list of nodes for which gravitation should be applied
        /// - for connected graphs(root graph or compound/ children graphs) there is no need to apply gravitation
        /// - each graph/ children node is marked as connected or not
        /// </summary>
        private void CalculateNodesToApplyGravityTo()
        {
            List<CoseNode> listNodes = new List<CoseNode>();

            foreach (CoseGraph graph in graphManager.Graphs)
            {
                graph.UpdateConnected();

                if (!graph.IsConnected)
                {
                    listNodes.AddRange(graph.Nodes);
                }
            }

            graphManager.NodesToApplyGravitation = listNodes;
        }

        /// <summary>
        /// Creates a new node
        /// </summary>
        /// <returns>the new node</returns>
        public CoseNode NewNode()
        {
            return new CoseNode(null, graphManager);
        }

        /// <summary>
        /// Returns all nodes of this layout
        /// </summary>
        /// <returns>all nodes</returns>
        private List<CoseNode> GetAllNodes()
        {
            return graphManager.GetAllNodes();
        }
    }
}

