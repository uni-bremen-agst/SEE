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
    public class SEEBranchCity : AbstractSEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="SEECityEvolution.Save(ConfigWriter)"/> and
        /// <see cref="SEECityEvolution.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// <summary>
        /// The path to the Version Control System
        /// </summary>
        /*[SerializeField, ShowInInspector, Tooltip("Path of VersionControlSystem"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public BranchesLayoutAttributes VersionControlSystem = new();*/

        /// The path to the GXL file containing the graph data.
        /// Note that any deriving class may use multiple GXL paths from which the single city is constructed.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of first GXL file"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath GXLPath1 = new();

        /// <summary>
        /// The path to the CSV file containing the additional metric values.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of second GXL file"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public FilePath GXLPath2 = new();

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
        [NonSerialized]
        private Graph loadedGraph = null;

        /// <summary>
        /// The graph that should be compared to.
        /// </summary>
        [NonSerialized]
        private Graph nextGraph = null;

        private Graph diffGraph;

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
        public override Graph LoadedGraph
        {
            get => loadedGraph;
            protected set
            {
                loadedGraph = value;
                InspectSchema(loadedGraph);
            }
        }

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
            
            LoadData();
            //InitializeAfterDrawn();
            //BoardSettings.LoadBoard();
        }


        //TODO
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
        public void LoadData()
        {
            Debug.Log("Load Data");
            if (string.IsNullOrEmpty(GXLPath1.Path) || string.IsNullOrEmpty(GXLPath2.Path))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }


                LoadedGraph = LoadGraph(GXLPath1.Path);
                nextGraph = LoadGraph(GXLPath2.Path);

                InspectSchema(loadedGraph);
                loadedGraph = RelevantGraph(loadedGraph);

                Debug.Log($"Loaded graph with {loadedGraph.NodeCount} nodes and {loadedGraph.EdgeCount} edges.\n");

                CalculateDiff();

                Debug.Log("Size AddedNodes: " + addedNodes.Count + "\n");
                Debug.Log("Size RemovedNodes: " + removedNodes.Count + "\n");
                Debug.Log("Size ChangedNodes: " + changedNodes.Count + "\n");
                Debug.Log("Size EqualNodes: " + equalNodes.Count + "\n");

                CreateDiffGraph();
                
            }
        }

        private void CalculateDiff()
        {
            //Node Comparison
            NextGraph.Diff(LoadedGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(loadedGraph, nextGraph),
                          nodeEqualityComparer,
                          out addedNodes,
                          out removedNodes,
                          out changedNodes,
                          out equalNodes);

            //Edge Comparison
            NextGraph.Diff(LoadedGraph,
                         g => g.Edges(),
                        (g, id) => g.GetEdge(id),
                        GraphExtensions.AttributeDiff(loadedGraph, nextGraph),
                        edgeEqualityComparer,
                        out addedEdges,
                        out removedEdges,
                        out changedEdges,
                        out equalEdges);
        }

        private void CreateDiffGraph()
        {
            diffGraph = new Graph(loadedGraph);
            //Draw Nodes
            addedNodes.ForEach(node =>
            {
                diffGraph.AddNode(node);
            });

            //Draw Edges
            addedEdges.ForEach(edge =>
            {
                diffGraph.AddEdge(edge);
            });

        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public void DrawGraph()
        {
            if (diffGraph != null)
            {
                GraphRenderer graphRenderer = new GraphRenderer(this, diffGraph);
                graphRenderer.DrawGraph(diffGraph, gameObject);
            }
            else
            {
                Debug.LogWarning("No graph loaded yet.\n");
            }
        }

       



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
            if (loadedGraph != null)
            {
                loadedGraph.BasePath = SourceCodeDirectory.Path;
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
        public override ISet<string> AllExistingMetrics()
        {
            if (loadedGraph == null)
            {
                return new HashSet<string>();
            }
            else
            {
                ISet<string> set1 = loadedGraph.AllNumericNodeAttributes();
                ISet<string> set2 = nextGraph.AllNumericNodeAttributes();

                ISet<string> result = set1;
                result.UnionWith(set2);

                return result;
            }
        }

        /// <summary>
        /// Dumps the metric names of all node types of the currently loaded graph.
        /// </summary>
        protected override void DumpNodeMetrics()
        {
            if (loadedGraph == null || nextGraph)
            {
                Debug.Log("Either the first graph or the second graph have not been loaded");
            }
            else
            {
                DumpNodeMetrics(new List<Graph>() { loadedGraph, nextGraph });
            }
        }




        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GXLPath"/> in the configuration file.
        /// </summary>
        private const string gxlPathLabel = "GXLPath";
/*

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            VersionControlSystem.FilePath.Save(writer, gxlPathLabel);
            VersionControlSystem.GLXPath1.Save(writer, gxlPathLabel);
            VersionControlSystem.GLXPath2.Save(writer, gxlPathLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            VersionControlSystem.FilePath.Restore(attributes, gxlPathLabel);
            VersionControlSystem.GLXPath1.Restore(attributes, gxlPathLabel);
            VersionControlSystem.GLXPath2.Restore(attributes, gxlPathLabel);
        }
*/
    }
}

