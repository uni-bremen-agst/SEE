using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System.Linq;
using System;
using SEE.Game;
using static SEE.Game.AbstractSEECity;
using SEE.GO;

namespace SEE.Layout
{
    public class CoseLayout : NodeLayout
    {
        /// <summary>
        /// Graphmanager
        /// </summary>
        private CoseGraphManager graphManager;

        /// <summary>
        /// collection with all Edge Objects
        /// </summary>
        private ICollection<Edge> edges = new List<Edge>();

        /// <summary>
        /// Mapping from Node to CoseNode
        /// </summary>
        private Dictionary<ILayoutNode, CoseNode> nodeToCoseNode;

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
        private List<SublayoutLayoutNode> sublayoutNodes = new List<SublayoutLayoutNode>();

        /// <summary>
        /// Collection with all layoutNodes
        /// </summary>
        private List<ILayoutNode> layoutNodes;

        /// <summary>
        /// Indicates Whether the inner nodes are circles
        /// </summary>
        private bool innerNodesAreCircles;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        Dictionary<ILayoutNode, NodeTransform> layout_result;

        /// <summary>
        /// A list with all graph managers (multilevel scaling)
        /// </summary>
        private List<CoseGraphManager> gmList;

        /// <summary>
        /// the settings for this graph
        /// </summary>
        private AbstractSEECity settings;

        private readonly NodeFactory leafNodeFactory;

        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }
        public CoseLayoutSettings CoseLayoutSettings { get => coseLayoutSettings; set => coseLayoutSettings = value; }
        public Dictionary<string, NodeLayouts> Sublayouts { get => sublayouts; set => sublayouts = value; }
        public bool InnerNodesAreCircles { get => innerNodesAreCircles; set => innerNodesAreCircles = value; }
        public List<SublayoutLayoutNode> SublayoutNodes { get => sublayoutNodes; set => sublayoutNodes = value; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        /// <param name="edges">List of Edges</param>
        /// <param name="coseGraphSettings">Graph Settings, choosed by user</param>
        public CoseLayout(float groundLevel, AbstractSEECity settings, NodeFactory leafNodeFactory) : base(groundLevel)
        {
            name = "Compound Spring Embedder Layout";
            nodeToCoseNode = new Dictionary<ILayoutNode, CoseNode>();
            this.settings = settings;
            this.leafNodeFactory = leafNodeFactory;
            SetupGraphSettings(settings.CoseGraphSettings);
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> gameNodes, ICollection<Edge> edges, ICollection<SublayoutLayoutNode> coseSublayoutNodes)
        {
            this.edges = edges;
            this.SublayoutNodes = coseSublayoutNodes.ToList();

            layout_result = new Dictionary<ILayoutNode, NodeTransform>();
            this.layoutNodes = gameNodes.ToList();

            ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(gameNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root node.");
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }

            ILayoutNode root = roots.FirstOrDefault();

            PlaceNodes(root);

            Vector3 relativePositionRootGraph = graphManager.RootGraph.CenterPosition; // set to (0.0)
            graphManager.RootGraph.CenterPosition = new Vector3(0.0f, groundLevel, 0.0f);

            PlacePositionNodes(graphManager.RootGraph, relativePositionRootGraph);

            foreach (CoseGraph graph in graphManager.Graphs)
            {
                
                Vector3 position = graph != graphManager.RootGraph ? new Vector3(graph.CenterPosition.x - relativePositionRootGraph.x, groundLevel, graph.CenterPosition.z - relativePositionRootGraph.z) : new Vector3(graph.CenterPosition.x, groundLevel, graph.CenterPosition.z);


                float width = graph.Scale.x;
                float height = graph.Scale.z;


                if (graph.Parent != null && graph.Parent.NodeObject != null && SublayoutNodes.Count() > 0)
                {
                    SublayoutLayoutNode sublayoutNode = CoseHelper.CheckIfNodeIsSublayouRoot(SublayoutNodes, graph.Parent.NodeObject.LinkName);

                    if (sublayoutNode != null && sublayoutNode.NodeLayout == NodeLayouts.EvoStreets)
                    {
                        width = graph.Parent.SublayoutValues.Sublayout.RootNodeRealScale.x;
                        height = graph.Parent.SublayoutValues.Sublayout.RootNodeRealScale.z;
                        // maybe todo
                        position -= graph.Parent.SublayoutValues.Sublayout.LayoutOffset;

                    }
                }

                if (graph != graphManager.RootGraph)
                {
                    position.y += LevelLift(graph.Parent.NodeObject);
                }

                //falls der knoten/ graph teil eines sublayouts war, dann ohne rotation, sonst mit rotation ... frag mich nicht worum
                bool applyRotation = true;

                foreach(SublayoutLayoutNode node in coseSublayoutNodes)
                {
                    if (node.Nodes.Contains(graph.GraphObject) && !node.Node.IsSublayoutRoot)
                    {
                        applyRotation = false;
                        break;
                    }
                }

                float rotation = applyRotation ? graph.GraphObject.Rotation : 0.0f;
                layout_result[graph.GraphObject] = new NodeTransform(position, new Vector3(width, innerNodeHeight, height),  rotation);
            }

            return layout_result;
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> gameNodes)
        {
            throw new System.NotImplementedException();
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
        private void SetScale(ICollection<ILayoutNode> layoutNodes)
        {
            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                CoseNode coseNode = nodeToCoseNode[layoutNode];

                if (layoutNode.IsLeaf)
                {
                    coseNode.SetWidth(layoutNode.Scale.x);
                    coseNode.SetHeight(layoutNode.Scale.z);
                }
            }
        }

        /// <summary>
        /// places the original node objects to the clauclated positions
        /// </summary>
        /// <param name="root"></param>
        private void PlacePositionNodes(CoseGraph root, Vector3 relativePositionRootGraph)
        {
            foreach (CoseNode node in root.Nodes)
            {
                if (node.IsLeaf())
                {
                    ILayoutNode nNode = node.NodeObject;

                    bool applyRotation = true;

                    foreach (SublayoutLayoutNode sublayoutNode in SublayoutNodes)
                    {
                        if (sublayoutNode.Nodes.Contains(node.NodeObject) && !sublayoutNode.Node.IsSublayoutRoot)
                        {
                            applyRotation = false;
                            break;
                        }
                    }

                    float rotation = applyRotation ? node.NodeObject.Rotation : 0.0f;

                    Vector3 position = new Vector3(node.CenterPosition.x - relativePositionRootGraph.x, groundLevel, node.CenterPosition.z - relativePositionRootGraph.z);
                    // TODO Levelshift?
                    NodeTransform transform = new NodeTransform(position, node.NodeObject.Scale, rotation);//, node.NodeObject.Rotation);
                    layout_result[nNode] = transform;
                }

                if (node.Child != null)
                {
                    PlacePositionNodes(node.Child, relativePositionRootGraph);
                }
            }
        }

        /// <summary>
        /// Places the Nodes 
        /// </summary>
        /// <param name="root">Root Node</param>
        private void PlaceNodes(ILayoutNode root)
        {
            CreateTopology(root);
            CalculateSubLayouts();

            if (SublayoutNodes.Count > 0 && CoseHelper.CheckIfNodeIsSublayouRoot(SublayoutNodes, graphManager.RootGraph.Parent.NodeObject.LinkName) != null)
            {
                graphManager.UpdateBounds();
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
        private void CalculateSubLayouts()
        {
            // was nagepasst werden muss:
            // x sublayoutvalue.isSublayoutNode/ Root auf ILayoutNode, bzw alle dieser werte
            // scale aus den ILayouts auch für die Graphen und die position ist wichtig , cNode.child auf postion/ scale setzen
            // centerposition und postion schaune, ob das überein stimt 
            // coseNode.SetPositionScale

            SetSublayoutValuesToNode(node: graphManager.RootGraph.Parent);
            foreach (CoseNode node in graphManager.GetAllNodes())
            {
                SetSublayoutValuesToNode(node: node);
            }
            
        }


        private void SetSublayoutValuesToNode(CoseNode node)
        {
            node.SublayoutValues.IsSubLayoutNode = node.NodeObject.IsSublayoutNode;
            node.SublayoutValues.IsSubLayoutRoot = node.NodeObject.IsSublayoutRoot;
            node.SublayoutValues.Sublayout = node.NodeObject.Sublayout;
            if (node.NodeObject.SublayoutRoot != null)
            {
                node.SublayoutValues.SubLayoutRoot = nodeToCoseNode[node.NodeObject.SublayoutRoot];
            }

            if (node.SublayoutValues.IsSubLayoutNode)
            {
                Rect rect = new Rect
                {
                    x = node.NodeObject.CenterPosition.x - node.NodeObject.Scale.x / 2,
                    y = node.NodeObject.CenterPosition.z - node.NodeObject.Scale.z / 2,
                    width = node.NodeObject.Scale.x,
                    height = node.NodeObject.Scale.z
                };

                node.SublayoutValues.RelativeScale = new Vector3(node.NodeObject.Scale.x, node.NodeObject.Scale.y, node.NodeObject.Scale.z);
                node.SublayoutValues.RelativeCenterPosition = new Vector3(node.NodeObject.CenterPosition.x, node.NodeObject.CenterPosition.y, node.NodeObject.CenterPosition.z);

                node.Scale = new Vector3(node.NodeObject.Scale.x, node.NodeObject.Scale.y, node.NodeObject.Scale.z);
                node.CenterPosition = new Vector3(node.NodeObject.CenterPosition.x, node.NodeObject.CenterPosition.y, node.NodeObject.CenterPosition.z);

                if (!node.SublayoutValues.IsSubLayoutRoot)
                {
                    node.SetPositionRelativ(node.SublayoutValues.SubLayoutRoot);

                    if (node.Child != null)
                    {
                        node.Child.LeftFrontCorner = node.GetLeftFrontCorner();
                        node.Child.RightBackCorner = node.GetRightBackCorner();
                        node.Child.UpdateBounding();
                    }
                }
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
                    
                    coseLayoutSettings.CoolingFactor = Mathf.Max(coseLayoutSettings.InitialCoolingFactor - Mathf.Pow(coseLayoutSettings.Coolingcycle, Mathf.Log(100 * (coseLayoutSettings.InitialCoolingFactor - coseLayoutSettings.FinalTemperature)) / Mathf.Log(coseLayoutSettings.MaxCoolingCycle)) / 100 * CoseLayoutSettings.Cooling_Adjuster, coseLayoutSettings.FinalTemperature);
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
        private void CalcSpringForce(CoseEdge edge, float idealEdgeLength)
        {
            CoseNode source = edge.Source;
            CoseNode target = edge.Target;

            float length;
            float springForce;
            float springForceX;
            float springForceY;

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

            float dl = length - idealEdgeLength;

            if (dl == 0.0)
            {
                springForceX = 0;
                springForceY = 0;
            } else
            {
                springForce = CoseLayoutSettings.Spring_Strength * dl;
                springForceX = springForce * (edge.LengthX / length);
                springForceY = springForce * (edge.LengthY / length);
            }

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
                        AddNodeToGrid(node, graphManager.RootGraph.LeftFrontCorner.x, graphManager.RootGraph.RightBackCorner.y);
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
                                    double distanceX = Mathf.Abs(nodeA.GetCenterX() - nodeB.GetCenterX()) - ((nodeA.Scale.x / 2) + (nodeB.Scale.x / 2));
                                    double distanceY = Mathf.Abs(nodeA.GetCenterY() - nodeB.GetCenterY()) - ((nodeA.Scale.z / 2) + (nodeB.Scale.z / 2));
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
            //var rectA = nodeA.rect;
            //var rectB = nodeB.rect;
            double[] overlapAmount = new double[2];
            double[] clipPoints = new double[4];
            float distanceX;
            float distanceY;
            float distanceSquared;
            float distance;
            float repulsionForce;
            float repulsionForceX;
            float repulsionForceY;

            if (nodeA.CalcOverlap(nodeB, overlapAmount))
            {
                repulsionForceX = 2 * (float) overlapAmount[0];
                repulsionForceY = 2 * (float) overlapAmount[1];

                float childrenConstant = nodeA.NoOfChildren * nodeB.NoOfChildren / (float)(nodeA.NoOfChildren + nodeB.NoOfChildren);

                nodeA.LayoutValues.RepulsionForceX -= childrenConstant * repulsionForceX;
                nodeA.LayoutValues.RepulsionForceY -= childrenConstant * repulsionForceY;

                nodeB.LayoutValues.RepulsionForceX += childrenConstant * repulsionForceX;
                nodeB.LayoutValues.RepulsionForceY += childrenConstant * repulsionForceY;
            }
            else
            {
                if (CoseLayoutSettings.Uniform_Leaf_Node_Size && nodeA.Child == null && nodeB.Child == null)
                {
                    distanceX = nodeB.CenterPosition.x - nodeA.CenterPosition.x;
                    distanceY = nodeB.CenterPosition.z - nodeA.CenterPosition.z;
                } else
                {
                    clipPoints = nodeA.CalcIntersection(nodeB, clipPoints);
                    distanceX = (float)clipPoints[2] - (float)clipPoints[0];
                    distanceY = (float)clipPoints[3] - (float)clipPoints[1];
                }

                if (Mathf.Abs(distanceX) < CoseLayoutSettings.Min_Repulsion_Dist)
                {
                    distanceX = Mathf.Sign(distanceX) * CoseLayoutSettings.Min_Repulsion_Dist;
                }

                if (Mathf.Abs(distanceY) < CoseLayoutSettings.Min_Repulsion_Dist)
                {
                    distanceY = Mathf.Sign(distanceY) * CoseLayoutSettings.Min_Repulsion_Dist;
                }

                distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
                distance = Mathf.Sqrt((float)distanceSquared);

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
            int startX = (int)Math.Floor(((v.CenterPosition.x - v.Scale.x / 2) - left) / coseLayoutSettings.RepulsionRange);
            int finishX = (int)Math.Floor((v.Scale.x + (v.CenterPosition.x - v.Scale.x / 2) - left) / coseLayoutSettings.RepulsionRange);
            int startY = (int)Math.Floor(((v.CenterPosition.z - v.Scale.z / 2) - top) / coseLayoutSettings.RepulsionRange);
            int finishY = (int)Math.Floor(((v.CenterPosition.z - v.Scale.z / 2) - top) / coseLayoutSettings.RepulsionRange);

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

            var diff = (graph.RightBackCorner.x - graph.LeftFrontCorner.x);
            var diffRep = diff / coseLayoutSettings.RepulsionRange;
            var ceilDiff = Mathf.Ceil((float)diffRep);
            var sizeX = (int)Mathf.Ceil((float)((graph.RightBackCorner.x - graph.LeftFrontCorner.x) / coseLayoutSettings.RepulsionRange));
            var sizeY = (int)Mathf.Ceil((float)((graph.LeftFrontCorner.y - graph.RightBackCorner.y) / coseLayoutSettings.RepulsionRange));

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
            float ownerCenterX;
            float ownerCenterY;
            float distanceX;
            float distanceY;
            float absDistanceX;
            float absDistanceY;
            int estimatedSize;

            ownerGraph = node.Owner;
            ownerCenterX = ownerGraph.CenterPosition.x; 
            ownerCenterY = ownerGraph.CenterPosition.z;
            distanceX = node.GetCenterX() - ownerCenterX;
            distanceY = node.GetCenterY() - ownerCenterY;
            absDistanceX = Mathf.Abs(distanceX) + node.Scale.x / 2;
            absDistanceY = Mathf.Abs(distanceY) + node.Scale.z / 2;

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
                coseLayoutSettings.CoolingFactor = 0.8f;
                coseLayoutSettings.InitialCoolingFactor = 0.8f;
                coseLayoutSettings.MaxNodeDisplacement = CoseLayoutSettings.Max_Node_Displacement_Incremental;
            }
            else
            {
                coseLayoutSettings.CoolingFactor = 1.0f;
                coseLayoutSettings.InitialCoolingFactor = 1.0f;
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

                    // von depth zu level geändert
                    edge.IdealEdgeLength += CoseLayoutSettings.Edge_Length * CoseLayoutSettings.Per_Level_Ideal_Edge_Length_Factor * (source.NodeObject.Level + target.NodeObject.Level - 2 * lcaDepth);
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
        private void CreateTopology(ILayoutNode root)
        {
            graphManager = new CoseGraphManager(this);

            //CreateNode(root, null);
            CoseNode rootNode = NewNode(root);
            CoseGraph rootGraph = graphManager.AddRootGraph();
            rootGraph.GraphObject = root;
            rootGraph.Parent = rootNode;
            rootNode.Child = rootGraph;
            this.nodeToCoseNode.Add(root, rootNode);

            foreach (ILayoutNode child in root.Children())
            {
                CreateNode(child, null);
            }
            //SetScale(layoutNodes);
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

            //graphManager.UpdateBounds();
        }

        /// <summary>
        /// Creates a new node 
        /// </summary>
        /// <param name="node">the original node</param>
        /// <param name="parent">the parent node</param>
        private void CreateNode(ILayoutNode node, CoseNode parent)
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

            // inital position
            cNode.SetLocation(node.CenterPosition.x, node.CenterPosition.z);

            if (!node.IsLeaf)
            {
                CoseGraph graph = new CoseGraph(null, this.graphManager);
                graph.GraphObject = node;
                graphManager.Add(graph, cNode);

                foreach (ILayoutNode child in cNode.NodeObject.Children())
                {
                    CreateNode(child, cNode);
                }

                //cNode.UpdateBounds();
            }
            else
            {
                Vector3 size = node.Scale;
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
        private CoseNode NewNode(ILayoutNode node)
        {
            return new CoseNode(node, graphManager);
        }

        /// <summary>
        /// Creates a new edge 
        /// </summary>
        /// <param name="edge">the new edge </param>
        private void CreateEdge(Edge edge)
        {
            CoseEdge cEdge = new CoseEdge(nodeToCoseNode[GetLayoutNodeFromLinkname(edge.Source.LinkName)], nodeToCoseNode[GetLayoutNodeFromLinkname(edge.Target.LinkName)]);

            graphManager.Add(cEdge, cEdge.Source, cEdge.Target);
        }

        private ILayoutNode GetLayoutNodeFromLinkname(String linkName)
        {
            List<ILayoutNode> nodes = layoutNodes.Where(layoutNode => layoutNode.LinkName == linkName).ToList();

            if (nodes.Count > 1)
            {
                throw new System.Exception("Linkname should be unique");
            } else if (nodes.Count == 0)
            {
                throw new System.Exception("No node exists with this linkname");
            }

            return nodes.First();
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

        public override bool IsHierarchical()
        {
            return true;
        }

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return true;
        }
    }
}

