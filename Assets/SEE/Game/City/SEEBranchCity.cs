using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.CityRendering;
using SEE.Game.Evolution;
using SEE.GO;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.City
{
    public class SEEBranchCity : SEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="SEECityEvolution.Save(ConfigWriter)"/> and
        /// <see cref="SEECityEvolution.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// The path to the GXL file containing the graph data.
        /// Note that any deriving class may use multiple GXL paths from which the single city is constructed.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of GXLBasePath file"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath GXLBasePath = new();

        /// <summary>
        /// The path to the CSV file containing the additional metric values.
        /// </summary>
        /*[SerializeField, ShowInInspector, Tooltip("Path of second GXL file"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath GXLPath2 = new();*/

        /// <summary>
        /// The graph that is visualized in the scene and whose visualization settings are
        /// managed here.
        /// We do not want to serialize it using Unity or Odin because both frameworks are
        /// insufficient for the highly recursive structure of all the graph objects.
        /// There are different points in time in which the underlying graph is created:
        /// (1) in the editor mode or (2) during the game. If the graph is created in the
        /// editor mode by a graph renderer, the graph renderer will attach the NodeRefs
        /// to the game objects representing the nodes. So all the information about nodes
        /// is available, for instance for the layouts or the inspector. During the game
        /// there are two different possible scenarios: (a) this SEECity is is created
        /// and configured at runtime or (b) the SEECity was created in the editor and then
        /// the game is started. For scenario (a), we expect the graph to be loaded and
        /// all NodeRefs be defined accordingly. For scenario (b), the graph attribute
        /// will not be serialized and, hence, be null. In that case, we load the graph
        /// from the GXL file, i.e., the GXL file is our persistent serialization we
        /// use to re-create the graph. We need, however, to set the NodeRefs at runtime.
        /// All that is being done in Start() below.
        ///
        /// Neither serialized nor saved to the config file.
        /// </summary>
        //[NonSerialized]
        //private Graph loadedGraph = null;

        /// <summary>
        /// The graph that should be compared to.
        /// </summary>
        [NonSerialized]
        private Graph nextGraph = null;

        private Graph diffGraph;

        /// <summary>
        /// List to save the old Attributes from
        /// </summary>
        private List<Tuple<string, int>> oldNodeAttributes = new List<Tuple<string, int>>();



        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/> from different graphs.
        /// </summary>
        private static readonly EdgeEqualityComparer edgeEqualityComparer = new();

        private GraphRenderer graphRenderer;

        /// <summary>
        /// Yields the graph renderer that draws this city.
        /// </summary>
        /// <remarks>Implements <see cref="AbstractSEECity.Renderer"/>.</remarks>
        public override IGraphRenderer Renderer => graphRenderer ??= new GraphRenderer(this, diffGraph);


        /// <summary>
        /// The graph underlying this SEE city that was loaded from disk. May be null.
        /// If a new graph is assigned to this property, the selected node types will
        /// be updated, too.
        ///
        /// Neither serialized nor saved to the config file.
        /// </summary>
        /*public override Graph LoadedGraph
        {
            get => loadedGraph;
            protected set
            {
                loadedGraph = value;
                InspectSchema(loadedGraph);
            }
        }*/

        /// <summary>
        /// The graph underlying this SEE city that was loaded from disk. May be null.
        /// If a new graph is assigned to this property, the selected node types will
        /// be updated, too.
        ///
        /// Neither serialized nor saved to the config file.
        /// </summary>
        public Graph NextGraph
        {
            get => nextGraph;
            protected set
            {
                nextGraph = value;
                InspectSchema(nextGraph);
            }
        }


        /// <summary>
        /// Sets up drawn city (if it has been drawn yet) and loads the metric board.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            //LoadData();
            //InitializeAfterDrawn();
            //BoardSettings.LoadBoard();
        }


        /// <summary>
        /// First, if a graph was already loaded (<see cref="LoadedGraph"/> is not null),
        /// everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath() are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Data")]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override void LoadData()
        {
            Debug.Log("Load Data");
            if (string.IsNullOrEmpty(GXLBasePath.Path) || string.IsNullOrEmpty(GXLPath.Path))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                if (LoadedGraph != null || NextGraph != null)
                {
                    Reset();
                }

                LoadedGraph = LoadGraph(GXLPath.Path);
                nextGraph = LoadGraph(GXLBasePath.Path);

                InspectSchema(LoadedGraph);
                LoadedGraph = RelevantGraph(LoadedGraph);

                InspectSchema(nextGraph);
                NextGraph = RelevantGraph(NextGraph);

                //SaveData();

                CalculateDiff();

                CreateDiffGraph();

                //loadedGraph = diffGraph;

            }
        }

        private void CalculateDiff()
        {
            //Node Comparison
            LoadedGraph.Diff(NextGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(LoadedGraph, nextGraph),
                          nodeEqualityComparer,
                          out addedNodes,
                          out removedNodes,
                          out changedNodes,
                          out equalNodes);

            //Edge Comparison
            LoadedGraph.Diff(NextGraph,
                         g => g.Edges(),
                        (g, id) => g.GetEdge(id),
                        GraphExtensions.AttributeDiff(LoadedGraph, nextGraph),
                        edgeEqualityComparer,
                        out addedEdges,
                        out removedEdges,
                        out changedEdges,
                        out equalEdges);

            Debug.Log("addedNodes: " + addedNodes.Count);
            Debug.Log("changedNodes: " + changedNodes.Count);
            Debug.Log("removedNodes: " + removedNodes.Count);
            Debug.Log("equalNodes: " + equalNodes.Count);
            Debug.Log("changedEdges: " + changedEdges.Count);
            Debug.Log("addedEdges: " + addedEdges.Count);
        }

        /// <summary>
        /// Calculates the graph to be drawn in the code city.
        /// </summary>
        private void CreateDiffGraph()
        {
            diffGraph = new Graph(NextGraph);



            List<Node> listdiffGraph = diffGraph.Nodes();
            List<Node> listLoadedGraph = LoadedGraph.Nodes();


            //Delete Nodes and Edges for the new Graph to avoid multiple nodes with the same ID
            LoadedGraph.Nodes().ForEach(node =>
            {
                LoadedGraph.RemoveNode(node);
            });

            LoadedGraph.Edges().ForEach(edge => {
                LoadedGraph.RemoveEdge(edge);
            });



            //Draw Nodes

            //Add Node with attribute
            addedNodes.ForEach(node =>
            {
                node.SetToggle("addedNode");
                diffGraph.AddNode(node);
            });

            //RemovedNodes marked with attribute
            removedNodes.ForEach(node =>
            {

                Node diffNode = diffGraph.Nodes().Find(diffNode2 => diffNode2.ID == node.ID);
                diffNode.SetToggle("deletedNode");
                //Debug.Log("Removed Erfolgreich" + node);
            });


            //Go through every changedNode and update their attributes
            changedNodes.ForEach(node =>
            {
                //Node toBeComparedNode;
                ISet<string> metricListNodeA;
                ISet<string> metricListNodeB = node.AllMetrics();

                //Suche richtige NodeID
                listdiffGraph.ForEach(nodeGraphA => {
                    if (node.ID == nodeGraphA.ID)
                    {
                        metricListNodeA = diffGraph.AllMetrics();
                        //Gehe durch die Metriken
                        //Wenn B mehrere Metriken hat, dann gehe da durch
                        /*if(metricListNodeB.Count > metricListNodeA.Count)
                        {
                            metricListNodeB.ForEach(metrics =>
                            {
                                int a = node.GetInt(metrics);
                                //Debug.Log("Metrics: " + metrics + " hat die Nummer: " + a);
                                oldNodeAttributes.Add(new Tuple<string, int>(metrics, a));
                                nodeGraphA.SetInt(metrics, a);
                            });
                        }
                        //Sonst gehe durch A
                        else
                        {
                            metricListNodeA.ForEach(metrics =>
                            {
                                int a = node.GetInt(metrics);
                                //Debug.Log("Metrics: " + metrics + " hat die Nummer: " + a);
                                oldNodeAttributes.Add(new Tuple<string, int>(metrics, a));
                                nodeGraphA.SetInt(metrics, a);
                            });
                        }*/
                        metricListNodeB.ForEach(metric =>
                        {
                            int a = node.GetInt(metric);
                            //Debug.Log("Metrics: " + metrics + " hat die Nummer: " + a);
                            oldNodeAttributes.Add(new Tuple<string, int>(metric, a));
                            nodeGraphA.SetInt(metric, a);
                        });

                    }
                });
            });


            //Debug.Log(addedEdges.Count);

            addedEdges.ForEach(edge =>
            {
                Node sourceNode = diffGraph.Nodes().Find(diffEdge => diffEdge.ID == edge.Source.ID);
                Node targetNode = diffGraph.Nodes().Find(diffEdge => diffEdge.ID == edge.Target.ID);

                //edge.SetToggle("addedEdge");
                diffGraph.AddEdge(sourceNode, targetNode, edge.Type);

                //Debug.Log("Operation erfolgreich");

            });


            //Add or remove Edges
            List<Node> diffNodes = diffGraph.Nodes();

            //Add Edges with attribute
            //Such die Source und Target Nodes manuell und füge dann ein Edge hinzu
            /*addedEdges.ForEach(edge =>
            {
                diffNodes.ForEach(node1 => {
                    string targetID = edge.Target.ID;
                    string node1ID = node1.ID;
                    string sourceID = edge.Source.ID;

                    if (targetID == node1ID)
                    {
                        diffNodes.ForEach(node2 =>
                        {
                            string node2ID = node2.ID;

                            if(sourceID == node2ID)
                            {
                                diffGraph.AddEdge(node2, node1, edge.Type);
                            }
                        });
                    }

                });
                //edge.SetToggle("addedEdge");
            });*/


            //Mark removed Edge with attribute
            removedEdges.ForEach(edge =>
            {
                //edge.SetToggle("removedEdge");
            });

            /* Debug.Log("LoadGraph Edges: " + LoadedGraph.EdgeCount);

             Debug.Log("NextGraph Edges: " + NextGraph.EdgeCount);

             Debug.Log("DiffGraph Nodes: " + diffGraph.NodeCount);

             Debug.Log("DiffGraph Edges: " + diffGraph.EdgeCount);*/
             Debug.Log(diffGraph);

        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graphs have been loaded and the graph to be visualised has been calculated
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public override void DrawGraph()
        {
            /*if (diffGraph != null)
            {
                //Debug.Log(diffGraph);
                diffGraph = RelevantGraph(diffGraph);
                //LoadDataForGraphListing(diffGraph);
                GraphRenderer graphRenderer = new GraphRenderer(this, diffGraph);
                graphRenderer.DrawGraph(diffGraph, gameObject);
            }
            else
            {
                Debug.LogWarning("No graph loaded yet.\n");
            }*/
            LoadedGraph = diffGraph;
            Debug.Log(LoadedGraph);
            //Debug.Log(LoadedGraph);
            base.DrawGraph();
        }


        /// <summary>
        /// Saves the graph data to the GXL file with GXLPath().
        /// </summary>
        /*[Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Save Data")]
        [PropertyOrder(DataButtonsGroupOrderSave)]
        public override void SaveData()
        {
            if (string.IsNullOrEmpty(GXLPath1.Path) || string.IsNullOrEmpty(GXLPath2.Path))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else if (LoadedGraph != null)
            {
                GraphWriter.Save(GXLPath1.Path, LoadedGraph, HierarchicalEdges.First());
                GraphWriter.Save(GXLPath2.Path, NextGraph, HierarchicalEdges.First());
            }
        }*/




        /// <summary>
        /// Set of added nodes from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Node> addedNodes;
        /// <summary>
        /// Set of removed nodes from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Node> removedNodes;
        /// <summary>
        /// Set of changed nodes from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> changedNodes;
        /// <summary>
        /// Set of equal nodes (i.e., nodes without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> equalNodes;

        /// <summary>
        /// Set of added edges from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Edge> addedEdges;
        /// <summary>
        /// Set of removed edges from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Edge> removedEdges;
        /// <summary>
        /// Set of changed edges from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> changedEdges;
        /// <summary>
        /// Set of equal edges (i.e., edges without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> equalEdges;

        /// <summary>
        /// Will be called whenever a new value is assigned to <see cref="ProjectPath"/>.
        /// In this case, we will update <see cref="loadedGraph.BasePath"/> with the
        /// new <see cref="ProjectPath.Path"/> if <see cref="loadedGraph"/> is not null.
        /// </summary>
        protected override void ProjectPathChanged()
        {
            if (LoadedGraph != null)
            {
                LoadedGraph.BasePath = SourceCodeDirectory.Path;
            }
        }

        /// <summary>
        /// Returns the names of all node metrics that truly exist in the underlying
        /// graph, that is, there is at least one node in the graph that has this
        /// metric.
        ///
        /// If no graph has been loaded yet, the empty list will be returned.
        /// </summary>
        /// <returns>names of all existing node metrics</returns>
        /*public override ISet<string> AllExistingMetrics()
        {
            if (LoadedGraph == null)
            {
                return new HashSet<string>();
            }
            else
            {
                ISet<string> set1 = LoadedGraph.AllNumericNodeAttributes();
                ISet<string> set2 = nextGraph.AllNumericNodeAttributes();

                ISet<string> result = set1;
                result.UnionWith(set2);

                return result;
            }
        }*/

        /// <summary>
        /// Dumps the metric names of all node types of the currently loaded graph.
        /// </summary>
        protected override void DumpNodeMetrics()
        {
            if (LoadedGraph == null || nextGraph)
            {
                Debug.Log("Either the first graph or the second graph have not been loaded");
            }
            else
            {
                DumpNodeMetrics(new List<Graph>() { LoadedGraph, nextGraph });
            }
        }


        /// <summary>
        /// Resets everything that is specific to a given graph. Here: the selected node types,
        /// the underlying graph, and all game objects visualizing information about it.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset Data")]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            // Delete the underlying graph.
            diffGraph?.Destroy();
            LoadedGraph = null;
            NextGraph = null;
            diffGraph = null;
        }

        protected override void InitializeAfterDrawn()
        {
            base.InitializeAfterDrawn();
        }


        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GXLPath"/> in the configuration file.
        /// </summary>
        private const string gxlBasePathLabel = "GXLBasePath";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            GXLBasePath.Save(writer, gxlBasePathLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            GXLBasePath.Restore(attributes, gxlBasePathLabel);
        }
    }
}

