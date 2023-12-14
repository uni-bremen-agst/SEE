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
        /// <summary>
        /// The path to the Version Control System
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of VersionControlSystem"), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public BranchesLayoutAttributes VersionControlSystem = new();

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

        private Graph diffGraph = new Graph("basePath", "diffGraph");

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
            //base.Start();
            
            //LoadData();
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
        public override void LoadData()
        {
            if (string.IsNullOrEmpty(GXLPath.Path))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }


                loadedGraph = LoadGraph(VersionControlSystem.GLXPath1.Path, null);
                nextGraph = LoadGraph(VersionControlSystem.GLXPath2.Path, null);


                CalculateDiff();
                CreateDiffGraph();
                
            }
        }

        private void CalculateDiff()
        {
            //Node Comparison
            loadedGraph.Diff(nextGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(loadedGraph, nextGraph),
                          nodeEqualityComparer,
                          out addedNodes,
                          out removedNodes,
                          out changedNodes,
                          out equalNodes);

            //Edge Comparison
            loadedGraph.Diff(nextGraph,
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
            //Draw Nodes
            addedNodes.ForEach(diffGraph.AddNode);
            equalNodes.ForEach(diffGraph.AddNode);
            changedNodes.ForEach(diffGraph.AddNode);
            equalNodes.ForEach(diffGraph.AddNode);

            //Draw Edges
            addedEdges.ForEach(diffGraph.AddEdge);
            equalEdges.ForEach(diffGraph.AddEdge);
            changedEdges.ForEach(diffGraph.AddEdge);
            equalEdges.ForEach(diffGraph.AddEdge);
        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph has been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public override void DrawGraph()
        {
            if (loadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                Graph theVisualizedSubGraph = diffGraph;
                if (ReferenceEquals(theVisualizedSubGraph, null))
                {
                    Debug.LogError("No graph loaded.\n");
                }
                else
                {
                    graphRenderer = new GraphRenderer(this, theVisualizedSubGraph);
                    // We assume here that this SEECity instance was added to a game object as
                    // a component. The inherited attribute gameObject identifies this game object.
                    graphRenderer.DrawGraph(theVisualizedSubGraph, gameObject);

                    // If we're in editmode, InitializeAfterDrawn() will be called by Start() once the
                    // game starts. Otherwise, in playmode, we have to call it ourselves.
                    if (Application.isPlaying)
                    {
                        InitializeAfterDrawn();
                    }
                }
            }
        }

        /// <summary>
        /// Loads the graph and metric data and sets all NodeRef and EdgeRef components to the
        /// loaded nodes and edges. This "deserializes" the graph to make it available at runtime.
        /// Note: <see cref="LoadedGraph"/> will be <see cref="VisualizedSubGraph"/> afterwards,
        /// that is, if node types are filtered, <see cref="LoadedGraph"/> may not contain all
        /// nodes saved in the underlying GXL file.
        /// Also note that this method may only be called after the code city has been drawn.
        /// </summary>
        protected override void InitializeAfterDrawn()
        {
            Assert.IsTrue(gameObject.IsCodeCityDrawn());
            Graph subGraph = diffGraph;
            if (subGraph != null)
            {
                foreach (GraphElement graphElement in loadedGraph.Elements().Except(subGraph.Elements()))
                {
                    // All other elements are virtual, i.e., should not be drawn.
                    graphElement.SetToggle(GraphElement.IsVirtualToggle);
                }

                SetNodeEdgeRefs(subGraph, gameObject);
            }
            else
            {
                Debug.LogError($"Could not load city {name}.\n");
            }

            // Add EdgeMeshScheduler to convert edge lines to meshes over time.
            gameObject.AddOrGetComponent<EdgeMeshScheduler>().Init(EdgeLayoutSettings, EdgeSelectionSettings,
                                                                   diffGraph);
            loadedGraph = subGraph;

            UpdateGraphElementIDMap(gameObject);
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

