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
            name = "Compound Spring Embedder Layout";
            this.Edges = edges;
            this.NodeToCoseNode = new Dictionary<Node, CoseNode>();
            this.CoseNodeToNode = new Dictionary<CoseNode, Node>();
            this.InnerNodesAreCircles = isCircle;
            this.settings = settings;
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
            PlacePositionNodes(GraphManager.RootGraph);

            foreach (CoseGraph graph in GraphManager.Graphs)
            {
                float width = graph.BoudingRect.width;
                float height = graph.BoudingRect.height;

                Vector3 position = new Vector3(graph.BoudingRect.x + (width / 2), groundLevel, graph.BoudingRect.y + (height / 2));

                if (graph != GraphManager.RootGraph)
                {
                    position.y += LevelLift(coseNodeToNode[graph.Parent]);
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

            FilterSubLayouts(settings);
        }

        /// <summary>
        /// Filter for the Sublayouts (choosed by the user) 
        /// </summary>
        /// <param name="settings">Graph Settings, choosed by user</param>
        private void FilterSubLayouts(CoseGraphSettings settings)
        {
            Dictionary<string, NodeLayouts> sublayouts = new Dictionary<string, NodeLayouts>();
            foreach (KeyValuePair<string, NodeLayouts> dir in settings.DirNodeLayout)
            {
                if (dir.Value != NodeLayouts.CompoundSpringEmbedder)
                {
                    sublayouts.Add(dir.Key, dir.Value);
                }
            }
            Sublayouts = sublayouts;
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
                var scale = gameObject.transform.localScale;
                Node node = gameObject.GetComponent<NodeRef>().node;
                if (roots.Contains(node))
                {
                    break;
                }
                CoseNode coseNode = NodeToCoseNode[node];

                if (node.IsLeaf())
                {
                    coseNode.SetWidth(scale.x);
                    coseNode.SetHeight(scale.z);
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
                    Node nNode = CoseNodeToNode[node];
                    layoutResult[to_game_node[nNode]] = new NodeTransform(new Vector3((float)node.GetCenterX(), groundLevel, (float)node.GetCenterY()), Vector3.one);
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
            CalculateSubLayouts(graphManager.RootGraph);


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

        /// <summary>
        /// Starts the layout process of a classic layout
        /// </summary>
        private void ClassicLayout()
        {
            CalculateNodesToApplyGravityTo();
            GraphManager.CalcLowestCommonAncestors();
            GraphManager.RootGraph.CalcEstimatedSize();
            CalcIdealEdgeLength();
            InitSpringEmbedder();
            RunSpringEmbedder();
        }

        /// <summary>
        /// Starts the layout process of a multi level scaling layout
        /// </summary>
        private void MultiLevelScaling()
        {
            Debug.Log("multilevel scaling");

            CoseGraphManager gm = GraphManager;
            gmList = gm.CoarsenGraph();

            CoseLayoutSettings.NoOfLevels = gmList.Count - 1;
            CoseLayoutSettings.Level = coseLayoutSettings.NoOfLevels;

            while (CoseLayoutSettings.Level >= 0)
            {
                GraphManager = gmList[CoseLayoutSettings.Level];
                ClassicLayout();
                CoseLayoutSettings.Incremental = true;

                if (CoseLayoutSettings.Level >= 1)
                {
                    Uncoarsen();
                }

                CoseLayoutSettings.TotalIterations = 0;
                CoseLayoutSettings.Level--;
            }
            CoseLayoutSettings.Incremental = false;
        }

        /// <summary>
        /// uncoarsen all nodes from the layouts graph manager
        /// </summary>
        private void Uncoarsen()
        {
            foreach (CoseNode node in GraphManager.GetAllNodes())
            {
                node.CNodeLayoutValues.Pred1.SetLocation((float)node.GetLeft(), (float)node.GetTop());
                if (node.CNodeLayoutValues.Pred2 != null)
                {
                    node.CNodeLayoutValues.Pred2.SetLocation((float)(node.GetLeft() + CoseLayoutSettings.Edge_Length), (float)(node.GetTop() + CoseLayoutSettings.Edge_Length));
                }
            }
        }

        /// <summary>
        /// calculates the sublayouts
        /// </summary>
        /// <param name="graph">the graph for which the sublayout is calculated</param>
        private void CalculateSubLayouts(CoseGraph graph)
        {
            foreach (CoseNode child in graph.Nodes)
            {
                if (child.CNodeSublayoutValues.IsSubLayoutRoot)
                {
                    CoseSublayout sublayout = new CoseSublayout(child, to_game_node, groundLevel, leafNodeFactory, innerNodeHeight);
                    sublayout.Layout();
                }
                else
                {
                    if (child.Child != null)
                    {
                        CalculateSubLayouts(child.Child);
                    }
                }
            }
        }

        /// <summary>
        /// Runs the spring embedder 
        /// </summary>
        private void RunSpringEmbedder()
        {
            GraphManager.UpdateBounds();
            do
            {
                CoseLayoutSettings.TotalIterations++;
                if (CoseLayoutSettings.TotalIterations % CoseLayoutSettings.Convergence_Check_Periode == 0)
                {
                    if (IsConverged())
                    {
                        break;
                    }
                    CoseLayoutSettings.Coolingcycle++;
                    //CoolingFactor = initialCoolingFactor * ((maxIterations - totalIterations) / maxIterations);
                    // TODO
                    // based on www.btluke.com/simanf1.html, schedule 3
                    CoseLayoutSettings.CoolingFactor = Mathf.Max((float)(CoseLayoutSettings.InitialCoolingFactor - Mathf.Pow((float)CoseLayoutSettings.Coolingcycle, Mathf.Log((float)(100 * (CoseLayoutSettings.InitialCoolingFactor - CoseLayoutSettings.FinalTemperature))) / Mathf.Log(CoseLayoutSettings.MaxCoolingCycle)) / 100 * CoseLayoutSettings.Cooling_Adjuster), (float)CoseLayoutSettings.FinalTemperature);
                }

                CoseLayoutSettings.TotalDisplacement = 0;
                CalcSpringForces();
                CalcRepulsionForces();
                CalcGravitationalForces();
                MoveNodes();
                GraphManager.UpdateBounds();
                ResetForces();

            } while (CoseLayoutSettings.TotalIterations < CoseLayoutSettings.MaxIterations);
            GraphManager.UpdateBounds();
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

            source.CNodeLayoutValues.SpringForceX += springForceX;
            source.CNodeLayoutValues.SpringForceY += springForceY;
            target.CNodeLayoutValues.SpringForceX -= springForceX;
            target.CNodeLayoutValues.SpringForceY -= springForceY;
        }

        /// <summary>
        /// Returns all edges of this layout
        /// </summary>
        /// <returns></returns>
        private List<CoseEdge> GetAllEdges()
        {
            return GraphManager.GetAllEdges();
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
                if ((CoseLayoutSettings.TotalIterations % CoseLayoutSettings.Grid_Calculation_Check_Periode) == 1)
                {
                    Grid = CalcGrid(GraphManager.RootGraph);

                    foreach (CoseNode node in nodes)
                    {
                        AddNodeToGrid(node, GraphManager.RootGraph.Left, GraphManager.RootGraph.Top);
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

            if (CoseLayoutSettings.TotalIterations % CoseLayoutSettings.Grid_Calculation_Check_Periode == 1)
            {
                HashSet<CoseNode> surrounding = new HashSet<CoseNode>();

                for (i = (nodeA.CNodeLayoutValues.StartX - 1); i < (nodeA.CNodeLayoutValues.FinishX + 2); i++)
                {
                    for (j = (nodeA.CNodeLayoutValues.StartY - 1); j < (nodeA.CNodeLayoutValues.FinishY + 2); j++)
                    {

                        if (!((i < 0) || (j < 0) || (i >= Grid.GetLength(0)) || (j >= Grid.GetLength(1))))
                        {
                            var list = Grid[i, j];



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
                                    if ((distanceX <= CoseLayoutSettings.RepulsionRange) && (distanceY <= CoseLayoutSettings.RepulsionRange))
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
            double distanceX = 0;
            double distanceY = 0;
            double distanceSquared;
            double distance;
            double repulsionForce;
            double repulsionForceX;
            double repulsionForceY;

            if (nodeA.CalcOverlap(nodeB, overlapAmount))
            {
                repulsionForceX = 2 * overlapAmount[0];
                repulsionForceY = 2 * overlapAmount[1];

                double childrenConstant = nodeA.NumberOfChildren() * nodeB.NumberOfChildren() / (double)(nodeA.NumberOfChildren() + nodeB.NumberOfChildren());

                nodeA.CNodeLayoutValues.RepulsionForceX -= childrenConstant * repulsionForceX;
                nodeA.CNodeLayoutValues.RepulsionForceY -= childrenConstant * repulsionForceY;

                nodeB.CNodeLayoutValues.RepulsionForceX += childrenConstant * repulsionForceX;
                nodeB.CNodeLayoutValues.RepulsionForceY += childrenConstant * repulsionForceY;
            }
            else
            {
                if (CoseLayoutSettings.Uniform_Leaf_Node_Size && nodeA.Child == null && nodeB.Child == null)
                {
                    distanceX = ((rectB.x + rectB.width) / 2) - ((rectA.x + rectA.width) / 2);
                    distanceY = ((rectB.y + rectB.height) / 2) - ((rectA.y + rectA.height) / 2);
                }
                {
                    clipPoints = nodeA.CalcIntersection(nodeB, clipPoints);
                    distanceX = clipPoints[2] - clipPoints[0];
                    distanceY = clipPoints[3] - clipPoints[1];
                }

                if (Mathf.Abs((float)distanceX) < CoseLayoutSettings.Min_Repulsion_Dist)
                {
                    distanceX = Math.Sign(distanceX) * CoseLayoutSettings.Min_Repulsion_Dist;
                }

                if (Mathf.Abs((float)distanceY) < CoseLayoutSettings.Min_Repulsion_Dist)
                {
                    distanceY = Mathf.Sign((float)distanceY) * CoseLayoutSettings.Min_Repulsion_Dist;
                }

                distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
                distance = Mathf.Sqrt((float)distanceSquared);

                repulsionForce = CoseLayoutSettings.Repulsion_Strength * nodeA.NumberOfChildren() * nodeB.NumberOfChildren() / distanceSquared;

                repulsionForceX = repulsionForce * distanceX / distance;
                repulsionForceY = repulsionForce * distanceY / distance;

                nodeA.CNodeLayoutValues.RepulsionForceX -= repulsionForceX;
                nodeA.CNodeLayoutValues.RepulsionForceY -= repulsionForceY;

                nodeB.CNodeLayoutValues.RepulsionForceX += repulsionForceX;
                nodeB.CNodeLayoutValues.RepulsionForceY += repulsionForceY;
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
            int startX = (int)Math.Floor((v.rect.x - left) / CoseLayoutSettings.RepulsionRange);
            int finishX = (int)Math.Floor((v.rect.width + v.rect.x - left) / CoseLayoutSettings.RepulsionRange);
            int startY = (int)Math.Floor((v.rect.y - top) / CoseLayoutSettings.RepulsionRange);
            int finishY = (int)Math.Floor((v.rect.height + v.rect.y - top) / CoseLayoutSettings.RepulsionRange);

            for (int i = startX; i <= finishX; i++)
            {
                for (int j = startY; j <= finishY; j++)
                {
                    Grid[i, j].Add(v);
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

            var sizeX = (int)Mathf.Ceil((float)((graph.Right - graph.Left) / CoseLayoutSettings.RepulsionRange));
            var sizeY = (int)Mathf.Ceil((float)((graph.Bottom - graph.Top) / CoseLayoutSettings.RepulsionRange));

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
            if (node.CNodeLayoutValues.GravitationForceX != 0 && node.CNodeLayoutValues.GravitationForceY != 0)
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

            if (node.Owner == GraphManager.RootGraph)
            {
                estimatedSize = (int)(ownerGraph.EstimatedSize * CoseLayoutSettings.Gravity_Range_Factor);

                if (absDistanceX > estimatedSize || absDistanceY > estimatedSize)
                {
                    node.CNodeLayoutValues.GravitationForceX = -CoseLayoutSettings.Gravity_Strength * distanceX;
                    node.CNodeLayoutValues.GravitationForceY = -CoseLayoutSettings.Gravity_Strength * distanceY;
                }
            }
            else
            {
                estimatedSize = (int)(ownerGraph.EstimatedSize * CoseLayoutSettings.Compound_Gravity_Range_Factor);

                if (absDistanceX > estimatedSize || absDistanceY > estimatedSize)
                {
                    node.CNodeLayoutValues.GravitationForceX = -CoseLayoutSettings.Gravity_Strength * distanceX * CoseLayoutSettings.Compound_Gravity_Strength;
                    node.CNodeLayoutValues.GravitationForceY = -CoseLayoutSettings.Gravity_Strength * distanceY * CoseLayoutSettings.Compound_Gravity_Strength;
                }
            }
        }

        /// <summary>
        /// Calculates the gravitational forces for all nodes of this layout
        /// </summary>
        private void CalcGravitationalForces()
        {
            List<CoseNode> nodes = GraphManager.NodesToApplyGravitation;

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
                if (!node.CNodeSublayoutValues.IsSubLayoutNode || node.CNodeSublayoutValues.IsSubLayoutRoot)
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

            if (CoseLayoutSettings.TotalIterations > CoseLayoutSettings.MaxIterations / 3)
            {
                oscilating = Mathf.Abs((float)(CoseLayoutSettings.TotalDisplacement - CoseLayoutSettings.OldTotalDisplacement)) < 2;
            }
            converged = CoseLayoutSettings.TotalDisplacement < CoseLayoutSettings.TotalDisplacementThreshold;
            CoseLayoutSettings.OldTotalDisplacement = CoseLayoutSettings.TotalDisplacement;
            return converged || oscilating;
        }

        /// <summary>
        /// Initalizes the spring embedder
        /// </summary>
        private void InitSpringEmbedder()
        {
            if (CoseLayoutSettings.Incremental)
            {
                CoseLayoutSettings.CoolingFactor = 0.8;
                CoseLayoutSettings.InitialCoolingFactor = 0.8;
                CoseLayoutSettings.MaxNodeDisplacement = CoseLayoutSettings.Max_Node_Displacement_Incremental;
            }
            else
            {
                CoseLayoutSettings.CoolingFactor = 1.0;
                CoseLayoutSettings.InitialCoolingFactor = 1.0;
                CoseLayoutSettings.MaxNodeDisplacement = CoseLayoutSettings.MaxNodeDisplacement;
            }

            CoseLayoutSettings.MaxIterations = Math.Max(GetAllNodes().Count * 5, CoseLayoutSettings.MaxIterations);
            CoseLayoutSettings.TotalDisplacementThreshold = CoseLayoutSettings.DisplacementThresholdPerNode * GetAllNodes().Count;
            CoseLayoutSettings.RepulsionRange = CalcRepulsionRange();
        }

        /// <summary>
        /// Calculates the repulsion range
        /// </summary>
        /// <returns></returns>
        private double CalcRepulsionRange()
        {
            double repulsionRange = (2 * (CoseLayoutSettings.Level + 1) * CoseLayoutSettings.Edge_Length);
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

            foreach (CoseEdge edge in GraphManager.GetAllEdges())
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
                    // TODO
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
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the Topology for the given graph structure
        /// </summary>
        /// <param name="root"></param>
        private void CreateTopology(Node root)
        {
            GraphManager = new CoseGraphManager(this);
            CoseGraph rootGraph = GraphManager.AddRootGraph();
            rootGraph.GraphObject = root;

            foreach (Node child in root.Children())
            {
                CreateNode(child, null);
            }
            SetScale(gameNodes);
            Debug.Log("I am Groot");

            foreach (Edge edge in Edges)
            {
                CreateEdge(edge);
            }

            GraphManager.UpdateBounds();
        }

        /// <summary>
        /// Creates a new node 
        /// </summary>
        /// <param name="node">the original node</param>
        /// <param name="parent">the parent node</param>
        private void CreateNode(Node node, CoseNode parent)
        {
            CoseNode cNode = NewNode(node);
            CoseGraph rootGraph = GraphManager.RootGraph;

            this.NodeToCoseNode.Add(node, cNode);
            this.CoseNodeToNode.Add(cNode, node);

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

                CoseGraph graph = new CoseGraph(null, this.GraphManager);
                graph.GraphObject = node;
                GraphManager.Add(graph, cNode);

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
            return new CoseGraph(null, GraphManager);
        }

        /// <summary>
        /// Creates a new node
        /// </summary>
        /// <param name="node"></param>
        /// <returns>the new node</returns>
        private CoseNode NewNode(Node node)
        {
            return new CoseNode(node, GraphManager);
        }

        /// <summary>
        /// Creates a new edge 
        /// </summary>
        /// <param name="edge">the new edge </param>
        private void CreateEdge(Edge edge)
        {
            CoseEdge cEdge = new CoseEdge(NodeToCoseNode[edge.Source], NodeToCoseNode[edge.Target]);

            GraphManager.Add(cEdge, cEdge.Source, cEdge.Target);
        }

        /// <summary>
        /// - finds a list of nodes for which gravitation should be applied
        /// - for connected graphs(root graph or compound/ children graphs) there is no need to apply gravitation
        /// - each graph/ children node is marked as connected or not
        /// </summary>
        private void CalculateNodesToApplyGravityTo()
        {
            List<CoseNode> listNodes = new List<CoseNode>();

            foreach (CoseGraph graph in GraphManager.Graphs)
            {
                graph.UpdateConnected();

                if (!graph.IsConnected)
                {
                    listNodes.AddRange(graph.Nodes);
                }
            }

            GraphManager.NodesToApplyGravitation = listNodes;
        }

        /// <summary>
        /// Creates a new node
        /// </summary>
        /// <returns>the new node</returns>
        public CoseNode NewNode()
        {
            return new CoseNode(null, GraphManager);
        }

        /// <summary>
        /// Returns all nodes of this layout
        /// </summary>
        /// <returns>all nodes</returns>
        private List<CoseNode> GetAllNodes()
        {
            return GraphManager.GetAllNodes();
        }
    }
}

