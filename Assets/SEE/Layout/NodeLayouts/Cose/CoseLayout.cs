// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game.City;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.Cose
{
    internal class IterationConstraint
    {
        /// <summary>
        /// the start value
        /// </summary>
        public int start;

        /// <summary>
        /// the end value
        /// </summary>
        public int end;

        /// <summary>
        /// The value by which the parameter value is increased within each iteration
        /// </summary>
        public int iterationStep;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="start"> the start value</param>
        /// <param name="end">the end value</param>
        /// <param name="iterationStep">The value by which the parameter value is increased within each iteration</param>
        public IterationConstraint(int start, int end, int iterationStep)
        {
            this.start = start;
            this.end = end;
            this.iterationStep = iterationStep;
        }
    }

    public class CoseLayout : NodeLayout
    {
        /// <summary>
        /// the Graphmanager of the layout
        /// </summary>
        private CoseGraphManager graphManager;

        /// <summary>
        /// collection with all Edge Objects
        /// </summary>
        private ICollection<Edge> edges = new List<Edge>();

        /// <summary>
        /// Mapping from Node to CoseNode
        /// </summary>
        private readonly Dictionary<ILayoutNode, CoseNode> nodeToCoseNode;

        /// <summary>
        /// Layout Settings (idealEdgeLength etc.)
        /// </summary>
        private CoseLayoutSettings coseLayoutSettings;

        /// <summary>
        /// Grid for smart repulsion range calculation
        /// </summary>
        private List<CoseNode>[,] grid;

        /// <summary>
        /// All sublayouts (choosed by the user via GUI)
        /// </summary>
        private List<SublayoutLayoutNode> sublayoutNodes = new List<SublayoutLayoutNode>();

        /// <summary>
        /// Collection with all layoutNodes
        /// </summary>
        private List<ILayoutNode> layoutNodes;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private Dictionary<ILayoutNode, NodeTransform> layout_result;

        /// <summary>
        /// A list with all graph managers (used for multilevel scaling)
        /// </summary>
        private List<CoseGraphManager> gmList;

        /// <summary>
        /// the abstarct see city settings
        /// </summary>
        private readonly AbstractSEECity settings;

        public CoseGraphManager GraphManager { get => graphManager; set => graphManager = value; }
        public CoseLayoutSettings CoseLayoutSettings { get => coseLayoutSettings; set => coseLayoutSettings = value; }
        public List<SublayoutLayoutNode> SublayoutNodes { get => sublayoutNodes; set => sublayoutNodes = value; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="settings">Graph Settings</param>
        public CoseLayout(float groundLevel, AbstractSEECity settings) : base(groundLevel)
        {
            name = "Compound Spring Embedder Layout";
            nodeToCoseNode = new Dictionary<ILayoutNode, CoseNode>();
            this.settings = settings;
            SetupGraphSettings(settings.CoseGraphSettings);
        }

        /// <summary>
        /// Computes the Layout
        /// </summary>
        /// <param name="layoutNodes">the nodes to layout</param>
        /// <param name="edges">the edges of the graph</param>
        /// <param name="coseSublayoutNodes">the sublayouts</param>
        /// <returns></returns>
        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges, ICollection<SublayoutLayoutNode> coseSublayoutNodes)
        {
            this.edges = edges;
            SublayoutNodes = coseSublayoutNodes.ToList();

            layout_result = new Dictionary<ILayoutNode, NodeTransform>();
            this.layoutNodes = layoutNodes.ToList();

            ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root node.");
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }

            ILayoutNode root = roots.FirstOrDefault();

            if (CoseLayoutSettings.Automatic_Parameter_Calculation || CoseLayoutSettings.Iterativ_Parameter_Calculation)
            {
                GetGoodParameter();
            }

            PlaceNodes(root);
            SetCalculatedLayoutPositionToNodes();

            if (CoseLayoutSettings.Iterativ_Parameter_Calculation)
            {
                CalculateParameterAutomatically();
            }

            return layout_result;
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> gameNodes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the calulated position and scale from the coseNodes to NodeTransfrom
        /// </summary>
        public void SetCalculatedLayoutPositionToNodes()
        {
            Vector3 relativePositionRootGraph = graphManager.RootGraph.CenterPosition;
            graphManager.RootGraph.CenterPosition = new Vector3(0.0f, groundLevel, 0.0f);

            PlacePositionNodes(graphManager.RootGraph, relativePositionRootGraph);

            foreach (CoseGraph graph in graphManager.Graphs)
            {

                Vector3 position = graph != graphManager.RootGraph ? new Vector3(graph.CenterPosition.x - relativePositionRootGraph.x, graph.CenterPosition.y, graph.CenterPosition.z - relativePositionRootGraph.z) : new Vector3(graph.CenterPosition.x, graph.CenterPosition.y, graph.CenterPosition.z);


                float width = graph.Scale.x;
                float height = graph.Scale.z;


                if (graph.Parent != null && graph.Parent.NodeObject != null && SublayoutNodes.Count() > 0)
                {
                    SublayoutLayoutNode sublayoutNode = CoseHelper.CheckIfNodeIsSublayouRoot(SublayoutNodes, graph.Parent.NodeObject.ID);

                    if (sublayoutNode != null && sublayoutNode.NodeLayout == NodeLayoutKind.EvoStreets)
                    {
                        width = graph.Parent.SublayoutValues.Sublayout.RootNodeRealScale.x;
                        height = graph.Parent.SublayoutValues.Sublayout.RootNodeRealScale.z;
                        position -= graph.Parent.SublayoutValues.Sublayout.LayoutOffset;
                    }
                }

                if (graph != graphManager.RootGraph)
                {
                    position.y += LevelLift(graph.Parent.NodeObject);
                }

                bool applyRotation = true;

                foreach (SublayoutLayoutNode node in SublayoutNodes)
                {
                    if (node.Nodes.Contains(graph.GraphObject) && !node.Node.IsSublayoutRoot)
                    {
                        applyRotation = false;
                        break;
                    }
                }

                if (graph.GraphObject != null)
                {
                    float rotation = applyRotation ? graph.GraphObject.Rotation : 0.0f;
                    layout_result[graph.GraphObject] = new NodeTransform(position, new Vector3(width, graph.GraphObject.LocalScale.y, height), rotation);
                }

            }
        }

        /// <summary>
        /// Resets the node position and rotation to inital
        /// </summary>
        public void Reset()
        {
            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                layoutNode.CenterPosition = new Vector3(0, groundLevel, 0);
                layoutNode.Rotation = 0.0f;
            }
        }

        /// <summary>
        /// calculates edgeLength and repulsionStrength
        /// </summary>
        public void GetGoodParameter()
        {
            int countNode = layoutNodes.Where(node => !node.IsSublayoutNode || node.IsSublayoutRoot).ToList().Count;
            int countMax = CountDepthMax(layoutNodes);

            int leafNodesCount = layoutNodes.Where(node => node.IsLeaf && (!node.IsSublayoutNode || node.IsSublayoutRoot)).ToList().Count;

            int edgesCount = edges.Where(edge => edge.Source.Children().Count > 0 && edge.Target.Children().Count > 0).ToList().Count;

            int edgeLength = CoseHelper.GetGoodEgdeLength(countNode, countMax, leafNodesCount, edgesCount);
            int repulsionStrength = CoseHelper.GetGoodRepulsionRange(countMax, countNode, edgesCount);

            settings.CoseGraphSettings.RepulsionStrength = repulsionStrength;
            settings.CoseGraphSettings.EdgeLength = edgeLength;

            CoseLayoutSettings.Edge_Length = edgeLength;
            CoseLayoutSettings.Repulsion_Strength = repulsionStrength;
        }

        /// <summary>
        /// Calculates the values for edgelength and repuslionforce iterativaly by increasing the values each iteration until a "good" layout is found
        /// </summary>
        private void CalculateParameterAutomatically()
        {
            int currentEdgeLength = CoseLayoutSettings.Edge_Length;
            int currentRepulsionStrength = (int)CoseLayoutSettings.Repulsion_Strength;

            IterationConstraint edgeLengthConstraint = new IterationConstraint(start: currentEdgeLength, end: currentEdgeLength + 15, iterationStep: 5);
            IterationConstraint repulsionRangeConstraint = new IterationConstraint(start: currentRepulsionStrength, end: currentRepulsionStrength + 15, iterationStep: 5);

            ApplyLayout();
            Measurements measurements = new Measurements(layoutNodes: layoutNodes, edges: edges.ToList());

            while (measurements.OverlappingGameNodes > 0)
            {
                int nextRep = currentRepulsionStrength + repulsionRangeConstraint.iterationStep;
                if (nextRep <= repulsionRangeConstraint.end)
                {
                    currentRepulsionStrength = nextRep;
                }
                else
                {
                    int nextEdgeLength = currentEdgeLength + edgeLengthConstraint.iterationStep;

                    if (nextEdgeLength <= edgeLengthConstraint.end)
                    {
                        currentEdgeLength = nextEdgeLength;
                        currentRepulsionStrength = repulsionRangeConstraint.start;
                    }
                    else
                    {
                        break;
                    }
                }

                Reset();
                CalculateLayout(edgeLength: currentEdgeLength, repulsionStrength: currentRepulsionStrength);
                ApplyLayout();
                measurements = new Measurements(layoutNodes: layoutNodes, edges: edges.ToList());
            }
            Reset();
        }

        /// <summary>
        /// Applies the caluclated positions to the layout nodes
        /// </summary>
        private void ApplyLayout()
        {
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in layout_result)
            {
                ILayoutNode node = entry.Key;
                NodeTransform transform = entry.Value;
                Vector3 position = transform.position;
                position.y += transform.scale.y / 2.0f;
                node.CenterPosition = position;
                node.LocalScale = transform.scale;
                node.Rotation = transform.rotation;
            }
        }

        /// <summary>
        /// Calculates the layout with the given edgeLength and repuslionStrength
        /// </summary>
        /// <param name="edgeLength">the edgelength</param>
        /// <param name="repulsionStrength">the repulsionStrength</param>
        private void CalculateLayout(int edgeLength, int repulsionStrength)
        {
            CoseLayoutSettings.Edge_Length = edgeLength;
            CoseLayoutSettings.Repulsion_Strength = repulsionStrength;
            settings.CoseGraphSettings.EdgeLength = edgeLength;
            settings.CoseGraphSettings.RepulsionStrength = repulsionStrength;

            StartLayoutProzess();
            SetCalculatedLayoutPositionToNodes();
        }

        /// <summary>
        /// Calculates the maximal depth of a node
        /// </summary>
        /// <param name="layoutNodes">the layout nodes</param>
        /// <returns></returns>
        private int CountDepthMax(ICollection<ILayoutNode> layoutNodes)
        {
            int depth = 0;
            foreach (ILayoutNode node in layoutNodes)
            {
                if (node.IsLeaf)
                {
                    if (depth < node.Level)
                    {
                        depth = node.Level;
                    }
                }
            }

            return depth;
        }

        /// <summary>
        /// Setup function for the CoseGraphSettings
        /// Values for Graph Layout Settings and Setup for Layouts
        /// </summary>
        /// <param name="settings">Graph Settings, choosed by user</param>
        private void SetupGraphSettings(CoseGraphAttributes settings)
        {
            CoseLayoutSettings.Edge_Length = settings.EdgeLength;
            CoseLayoutSettings.Use_Smart_Ideal_Edge_Calculation = settings.UseSmartIdealEdgeCalculation;
            CoseLayoutSettings.Per_Level_Ideal_Edge_Length_Factor = settings.PerLevelIdealEdgeLengthFactor;
            CoseLayoutSettings.Use_Smart_Repulsion_Range_Calculation = settings.UseSmartRepulsionRangeCalculation;
            CoseLayoutSettings.Gravity_Strength = settings.GravityStrength;
            CoseLayoutSettings.Compound_Gravity_Strength = settings.CompoundGravityStrength;
            CoseLayoutSettings.Repulsion_Strength = settings.RepulsionStrength;
            CoseLayoutSettings.Multilevel_Scaling = settings.MultiLevelScaling;
            CoseLayoutSettings.Use_Smart_Multilevel_Calculation = settings.UseSmartMultilevelScaling;
            CoseLayoutSettings.Automatic_Parameter_Calculation = settings.UseCalculationParameter;
            CoseLayoutSettings.Iterativ_Parameter_Calculation = settings.UseIterativeCalculation;

            coseLayoutSettings = new CoseLayoutSettings();

        }

        /// <summary>
        /// places the original node objects to the calculated positions
        /// and applys relativ position of the root graph to all nodes
        /// </summary>
        /// <param name="root">the root graph</param>
        /// <param name="relativePositionRootGraph">the relative postion of the root graph.</param>
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

                    Vector3 position = new Vector3(node.CenterPosition.x - relativePositionRootGraph.x, node.CenterPosition.y, node.CenterPosition.z - relativePositionRootGraph.z);
                    NodeTransform transform = new NodeTransform(position, node.NodeObject.LocalScale, rotation);
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

            if (SublayoutNodes.Count > 0 && CoseHelper.CheckIfNodeIsSublayouRoot(SublayoutNodes, graphManager.RootGraph.Nodes.First().NodeObject.ID) != null)
            {
                graphManager.UpdateBounds();
            }
            else
            {
                StartLayoutProzess();
            }
        }

        /// <summary>
        /// Starts the layout process
        /// </summary>
        private void StartLayoutProzess()
        {
            if (CoseLayoutSettings.Multilevel_Scaling)
            {
                CoseLayoutSettings.Incremental = false;
                MultiLevelScaling();
            }
            else
            {
                CoseLayoutSettings.Incremental = true;
                ClassicLayout();
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
        /// Calculates the number of children of every node
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
            CoseGraphManager gm = graphManager;
            gmList = gm.CoarsenGraph();

            coseLayoutSettings.NoOfLevels = gmList.Count - 1;
            coseLayoutSettings.Level = coseLayoutSettings.NoOfLevels;

            Dictionary<int, int> edgeLengths = new Dictionary<int, int>();

            if (CoseLayoutSettings.Use_Smart_Multilevel_Calculation)
            {
                int originalEdgeLength = CoseLayoutSettings.Edge_Length;

                edgeLengths.Add(0, originalEdgeLength);

                // we dont have to look at the original graph
                for (int i = 1; i < gmList.Count; i++)
                {
                    int k = edgeLengths[i - 1];
                    double fac = Math.Sqrt(4.0 / 7);
                    int newEdgeLength = (int)(fac * k);
                    newEdgeLength = Math.Max(newEdgeLength, 1);
                    Debug.Log("");
                    edgeLengths.Add(i, newEdgeLength);
                }
            }

            while (coseLayoutSettings.Level >= 0)
            {
                graphManager = gmList[coseLayoutSettings.Level];

                if (CoseLayoutSettings.Use_Smart_Multilevel_Calculation)
                {
                    CoseLayoutSettings.Edge_Length = edgeLengths[coseLayoutSettings.Level];
                }

                ClassicLayout();
                CoseLayoutSettings.Incremental = true;

                if (coseLayoutSettings.Level >= 1)
                {
                    Uncoarsen();
                }

                coseLayoutSettings.TotalIterations = 0;
                coseLayoutSettings.Coolingcycle = 0;
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
                node.LayoutValues.Pred1.SetLocation(node.CenterPosition.x, node.CenterPosition.z);
                if (node.LayoutValues.Pred2 != null)
                {
                    node.LayoutValues.Pred2.SetLocation(node.CenterPosition.x + CoseLayoutSettings.Edge_Length, node.CenterPosition.z + CoseLayoutSettings.Edge_Length);
                }
            }
        }

        /// <summary>
        /// calculates the sublayouts
        /// </summary>
        private void CalculateSubLayouts()
        {
            foreach (CoseNode node in graphManager.GetAllNodes())
            {
                SetSublayoutValuesToNode(node: node);
            }
        }

        /// <summary>
        /// Set sublayout values from the nodeobject of this node to the layoutValues of this node
        /// </summary>
        /// <param name="node">a cose node</param>
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
                node.SublayoutValues.RelativeScale = new Vector3(node.NodeObject.LocalScale.x, node.NodeObject.LocalScale.y, node.NodeObject.LocalScale.z);
                node.SublayoutValues.RelativeCenterPosition = new Vector3(node.NodeObject.CenterPosition.x, node.NodeObject.CenterPosition.y, node.NodeObject.CenterPosition.z);

                node.Scale = new Vector3(node.NodeObject.LocalScale.x, node.NodeObject.LocalScale.y, node.NodeObject.LocalScale.z);
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

                    // based on www.btluke.com/simanf1.html, schedule 3
                    coseLayoutSettings.CoolingFactor = Mathf.Max(coseLayoutSettings.InitialCoolingFactor - Mathf.Pow(coseLayoutSettings.Coolingcycle, Mathf.Log(100 * (coseLayoutSettings.InitialCoolingFactor - coseLayoutSettings.FinalTemperature)) / Mathf.Log(coseLayoutSettings.MaxCoolingCycle)) / 100, coseLayoutSettings.FinalTemperature);
                    //Debug.LogFormat("cooling factor: {0}\n", coseLayoutSettings.CoolingFactor);
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
        /// <param name="edge">the edge </param>
        /// <param name="idealEdgeLength">the ideal edge length of this edge</param>
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

            if (length == 0.0)
            {
                length = 0.1f;
            }

            if (dl == 0.0)
            {
                springForceX = 0;
                springForceY = 0;
            }
            else
            {
                springForce = CoseLayoutSettings.Spring_Strength * dl;
                springForceX = springForce * (edge.LengthX / length);
                springForceY = springForce * (edge.LengthY / length);
            }

            if (float.IsNaN(springForceX))
            {
                Debug.Log("");
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
        /// <param name="nodeA">the node</param>
        /// <param name="processedNodeSet">a set of nodes that have already been processed (repulsion forces already have been calcualted) </param>
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
                            List<CoseNode> list = grid[i, j];

                            for (int k = 0; k < list.Count; k++)
                            {
                                CoseNode nodeB = list[k];

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
                CalcRepulsionForce(nodeA, node);
            }
        }

        /// <summary>
        /// Calculates the repulsion force between two nodes
        /// </summary>
        /// <param name="nodeA">the first node</param>
        /// <param name="nodeB">the second node</param>
        private void CalcRepulsionForce(CoseNode nodeA, CoseNode nodeB)
        {
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
                repulsionForceX = 2 * (float)overlapAmount[0];
                repulsionForceY = 2 * (float)overlapAmount[1];

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
                }
                else
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

            if (float.IsNaN(nodeA.LayoutValues.RepulsionForceX))
            {
                Debug.Log("");
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
            int startX = (int)Math.Floor((v.GetLeftFrontCorner().x - left) / coseLayoutSettings.RepulsionRange);
            int finishX = (int)Math.Floor((v.Scale.x + v.GetLeftFrontCorner().x - left) / coseLayoutSettings.RepulsionRange);
            int startY = (int)Math.Floor((v.GetRightBackCorner().y - top) / coseLayoutSettings.RepulsionRange);
            int finishY = (int)Math.Floor((v.Scale.z + v.GetRightBackCorner().y - top) / coseLayoutSettings.RepulsionRange);

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

            int sizeX = (int)Mathf.Ceil((float)((graph.RightBackCorner.x - graph.LeftFrontCorner.x) / coseLayoutSettings.RepulsionRange));
            int sizeY = (int)Mathf.Ceil((float)((graph.LeftFrontCorner.y - graph.RightBackCorner.y) / coseLayoutSettings.RepulsionRange));

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
        /// This methode inspects whether the graph has reached to a minima. It returns true if the layout seems to be oscillating as well.
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
            converged = coseLayoutSettings.TotalDisplacement < (decimal)coseLayoutSettings.TotalDisplacementThreshold;
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
                coseLayoutSettings.MaxNodeDisplacement = CoseLayoutSettings.Max_Node_Displacement;
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

            foreach (CoseEdge edge in graphManager.GetAllEdges())
            {
                edge.IdealEdgeLength = CoseLayoutSettings.Edge_Length;

                if (edge.IsInterGraph)
                {
                    source = edge.Source;
                    target = edge.Target;



                    if (CoseLayoutSettings.Use_Smart_Ideal_Edge_Calculation)
                    {
                        int sizeOfSourceInLca = (int)Math.Round(edge.SourceInLca.EstimatedSize);
                        int sizeOfTargetInLca = (int)Math.Round(edge.TargetInLca.EstimatedSize);

                        edge.IdealEdgeLength += sizeOfSourceInLca + sizeOfTargetInLca - 2 * CoseLayoutSettings.Simple_Node_Size;
                    }

                    lcaDepth = edge.LowestCommonAncestor.GetInclusionTreeDepth();

                    edge.IdealEdgeLength += CoseLayoutSettings.Edge_Length * CoseLayoutSettings.Per_Level_Ideal_Edge_Length_Factor * (source.InclusionTreeDepth + target.InclusionTreeDepth - 2 * lcaDepth);
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

            ILayoutNode sourceNode = layoutNodes.Where(node => node.ID == edge.Source.ID).First();
            ILayoutNode targetNode = layoutNodes.Where(node => node.ID == edge.Target.ID).First();

            if (sourceNode.Level == targetNode.Level)
            {
                return true;
            }

            ILayoutNode startNode = sourceNode.Level <= targetNode.Level ? sourceNode : targetNode;

            return CheckChildrenForHierachie(targetNode, startNode.Children());
        }

        /// <summary>
        /// Check whether the node is with one of the given nodes in the same hierarchy
        /// </summary>
        /// <param name="node"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        private bool CheckChildrenForHierachie(ILayoutNode node, ICollection<ILayoutNode> children)
        {
            foreach (ILayoutNode child in children)
            {
                if (node.ID == child.ID)
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
            CoseGraph _ = graphManager.AddRootGraph();
            CreateNode(root, null);

            foreach (Edge edge in edges)
            {
                if (ExcludeEdgesInSameHierarchie(edge) && (edge.Source.ID != edge.Target.ID))
                {
                    CreateEdge(edge);
                }
            }
            graphManager.UpdateBounds();
        }

        /// <summary>
        /// Creates a new node from a ILayoutNode
        /// </summary>
        /// <param name="node">the original node</param>
        /// <param name="parent">the parent node</param>
        private void CreateNode(ILayoutNode node, CoseNode parent)
        {
            CoseNode cNode = NewNode(node);
            CoseGraph rootGraph = graphManager.RootGraph;

            nodeToCoseNode.Add(node, cNode);

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
                CoseGraph graph = new CoseGraph(null, graphManager);
                graph.GraphObject = node;
                graphManager.Add(graph, cNode);

                foreach (ILayoutNode child in cNode.NodeObject.Children())
                {
                    CreateNode(child, cNode);
                }

                cNode.UpdateBounds();
            }
            else
            {
                Vector3 size = node.LocalScale;
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
        /// Creates a new node from a ILayoutNode
        /// </summary>
        /// <param name="node">a ILayoutNode</param>
        /// <returns>the new node</returns>
        private CoseNode NewNode(ILayoutNode node)
        {
            return new CoseNode(node, graphManager);
        }

        /// <summary>
        /// Creates a new cose edge
        /// </summary>
        /// <param name="edge">the new edge </param>
        private void CreateEdge(Edge edge)
        {
            CoseEdge cEdge = new CoseEdge(nodeToCoseNode[CoseHelper.GetLayoutNodeFromLinkname(edge.Source.ID, layoutNodes)],
                                          nodeToCoseNode[CoseHelper.GetLayoutNodeFromLinkname(edge.Target.ID, layoutNodes)]);

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

