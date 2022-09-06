//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.Evolution;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// A SEECityEvolution combines all necessary components for the animations
    /// of an evolving SEECity.
    /// </summary>
    [Serializable]
    public class SEECityEvolution : AbstractSEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="SEECityEvolution.Save(ConfigWriter)"/> and
        /// <see cref="SEECityEvolution.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// <summary>
        /// Name of the Inspector foldout group for the specific evolution setttings.
        /// </summary>
        private const string EvolutionFoldoutGroup = "Evolution settings";

        /// <summary>
        /// Sets the maximum number of revisions to load.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Maximum number of revisions to load."), FoldoutGroup(EvolutionFoldoutGroup)]
        public int MaxRevisionsToLoad = 500;  // serialized by Unity

        /// <summary>
        /// The renderer for rendering the evolution of the graph series.
        ///
        /// Neither serialized nor saved in the configuration file.
        /// </summary>
        private EvolutionRenderer evolutionRenderer;  // not serialized by Unity; will be set in Start()

        /// <summary>
        /// The directory in which the GXL files of the graph series are located.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("The directory in which the GXL files are located."), FoldoutGroup(DataFoldoutGroup)]
        public DirectoryPath GXLDirectory = new DirectoryPath();

        //-----------------------------------------------------
        // Attributes to mark changes
        //-----------------------------------------------------

        /// <summary>
        /// The height of posts used as markers for new and deleted elements.
        /// </summary>
        [Tooltip("The height of posts used as markers for new, changed, and deleted elements (>=0).")]
        [SerializeField, ShowInInspector, FoldoutGroup(EvolutionFoldoutGroup)]
        public float MarkerHeight = 0.2f;

        /// <summary>
        /// The width (x and z lengths) of posts used as markers for new and deleted elements.
        /// </summary>
        [Tooltip("The width (x and z lengths) of posts used as markers for new and deleted elements (>=0).")]
        [SerializeField, ShowInInspector, FoldoutGroup(EvolutionFoldoutGroup)]
        public float MarkerWidth = 0.01f;

        /// <summary>
        /// Color for power beams of newly added nodes, can be set in inspector
        /// </summary>
        [Tooltip("The color of the beam for newly created nodes.")]
        [SerializeField, ShowInInspector, FoldoutGroup(EvolutionFoldoutGroup)]
        public Color AdditionBeamColor = Color.green;

        /// <summary>
        /// Changed nodes beam color to be pickable in inspector
        /// </summary>
        [Tooltip("The color of the beam for changed nodes.")]
        [SerializeField, ShowInInspector, FoldoutGroup(EvolutionFoldoutGroup)]
        public Color ChangeBeamColor = Color.yellow;

        /// <summary>
        /// Deleted nodes beam color to be pickable in inspector
        /// </summary>
        [Tooltip("The color of the beam for deleted nodes.")]
        [SerializeField, ShowInInspector, FoldoutGroup(EvolutionFoldoutGroup)]
        public Color DeletionBeamColor = Color.black;

        /// <summary>
        /// Factory method to create the used EvolutionRenderer.
        /// </summary>
        /// <returns>the current or new evolution renderer attached to this city</returns>
        protected EvolutionRenderer CreateEvolutionRenderer(List<Graph> graphs)
        {
            if (!gameObject.TryGetComponent(out EvolutionRenderer result))
            {
                result = gameObject.AddComponent<EvolutionRenderer>();
                result.SetGraphEvolution(graphs);
            }
            return result;
        }

        /// <summary>
        /// Will be called whenever a new value is assigned to <see cref="ProjectPath"/>.
        /// In this case, we will update all loaded graphs that we have passed to the
        /// <see cref="evolutionRenderer"/>.
        /// </summary>
        protected override void ProjectPathChanged()
        {
            evolutionRenderer?.ProjectPathChanged(SourceCodeDirectory.Path);
        }

        /// <summary>
        /// Loads the graph data from the GXL files and the metrics from the CSV files contained
        /// in the directory with path PathPrefix and the metrics.
        /// </summary>
        private List<Graph> LoadDataSeries()
        {
            GraphsReader graphsReader = new GraphsReader();
            // Load all GXL graphs and CSV files in directory PathPrefix but not more than maxRevisionsToLoad many.
            graphsReader.Load(GXLDirectory.Path, HierarchicalEdges, basePath: SourceCodeDirectory.Path, rootName: GXLDirectory.Path, MaxRevisionsToLoad);
            return graphsReader.graphs;
        }

        /// <summary>
        /// The first graph of the graph series. It is used only to let the user see
        /// his/her settings in action. It will be destroyed when the game starts.
        /// </summary>
        [NonSerialized]
        private Graph firstGraph;

        /// <summary>
        /// Dumps the metric names of all node types of the currently loaded graph.
        /// </summary>
        protected override void DumpNodeMetrics()
        {
            if (firstGraph == null)
            {
                Debug.Log("No graph loaded yet.");
            }
            else
            {
                DumpNodeMetrics(new List<Graph>() { firstGraph });
            }
        }

        /// <summary>
        /// Loads the first graph of the graph series. If a graph was already
        /// loaded, that graph will be destroyed.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public void LoadData()
        {
            if (firstGraph != null)
            {
                Reset();
            }
            firstGraph = LoadFirstGraph();
            if (firstGraph != null)
            {
                Debug.Log($"Loaded graph with {firstGraph.NodeCount} nodes and {firstGraph.EdgeCount} edges.\n");
            }
            else
            {
                Debug.LogWarning("Could not load graph.\n");
            }
        }

        /// <summary>
        /// Destroys <see cref="firstGraph"/> if not <c>null</c>.
        /// Postcondition: <see cref="firstGraph"/> will be <c>null</c>.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup)]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            // Delete the underlying graph.
            firstGraph?.Destroy();
            firstGraph = null;
        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public void DrawGraph()
        {
            if (firstGraph)
            {
                GraphRenderer graphRenderer = new GraphRenderer(this, firstGraph);
                graphRenderer.DrawGraph(firstGraph, gameObject);
            }
            else
            {
                Debug.LogWarning("No graph loaded yet.\n");
            }
        }

        /// <summary>
        /// Yields the graph of the first GXL found in the directory named <code>PathPrefix</code>.
        /// The order is ascending and alphabetic by the GXL filenames located in that directory.
        /// If the first GXL file has a corresponding CSV with additional metrics, this CSV file
        /// will be read, too, and the node metrics added to the graph.
        /// Furthermore the selection of the specific node types selected by the user is applied in case
        /// the user specified it before. By default every node type is selected.
        ///
        /// Precondition: PathPrefix must be set and denote an existing directory in the
        /// file system containing at least one GXL file.
        /// </summary>
        /// <returns>the loaded graph or null if none could be found</returns>
        public Graph LoadFirstGraph()
        {
            GraphsReader reader = new GraphsReader();
            reader.Load(GXLDirectory.Path, HierarchicalEdges, basePath: SourceCodeDirectory.Path, rootName: GXLDirectory.Path, 1);
            List<Graph> graphs = reader.graphs;
            if (graphs.Count == 0)
            {
                return null;
            }
            else
            {
                Graph graph = graphs.First();
                InspectSchema(graph);
                graph = RelevantGraph(graph);
                return graph;
            }
        }

        /// <summary>
        /// Draws the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph to be drawn</param>
        public void DrawGraph(Graph graph)
        {
            DrawGraphs(new List<Graph>() { graph });
        }

        /// <summary>
        /// Loads all graphs, calculates their layouts, and displays the first graph in the
        /// graph series.
        /// </summary>
        override internal void Start()
        {
            Reset();
            base.Start();

            List<Graph> graphs = LoadDataSeries();
            evolutionRenderer = CreateEvolutionRenderer(graphs);
            DrawGraphs(graphs);

            if (!gameObject.TryGetComponent(out AnimationInteraction animationInteraction))
            {
                animationInteraction = gameObject.AddComponent<AnimationInteraction>();
            }
            animationInteraction.EvolutionRenderer = evolutionRenderer;

            evolutionRenderer.ShowGraphEvolution();
        }

        /// <summary>
        /// Creates <see cref="evolutionRenderer"/> and shows the nodes having one of the selected
        /// node types and the edges of these specific nodes of the graph evolution
        /// for given <paramref name="graphs"/> using it.
        /// </summary>
        /// <param name="graphs">the series of graph to be drawn</param>
        private void DrawGraphs(List<Graph> graphs)
        {
            for (int i = 0; i < graphs.Count; i++)
            {
                Graph relevantGraph = RelevantGraph(graphs[i]);
                if (relevantGraph != graphs[i])
                {
                    // Node types have been filtered out. Because of that
                    // there may now be multiple roots again.
                    relevantGraph.AddSingleRoot(name: "ROOT", type: "ROOT");
                }
                graphs[i] = relevantGraph;
                LoadDataForGraphListing(graphs[i]);
            }
        }

        /// <summary>
        /// Returns the names of all node metrics that truly exist in the underlying
        /// graph, that is, there is at least one node in the graph that has this
        /// metric.
        ///
        /// The metric names are derived from the graph currently drawn by the
        /// evolution renderer.
        /// If no graph has been loaded yet, the empty list will be returned.
        /// </summary>
        /// <returns>names of all existing node metrics</returns>
        public override ISet<string> AllExistingMetrics()
        {
            return evolutionRenderer.AllExistingMetrics();
        }

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GXLDirectory"/> in the configuration file.
        /// </summary>
        private const string GXLDirectoryLabel = "GXLDirectory";
        /// <summary>
        /// Label of attribute <see cref="MaxRevisionsToLoad"/> in the configuration file.
        /// </summary>
        private const string MaxRevisionsToLoadLabel = "MaxRevisionsToLoad";
        /// <summary>
        /// Label of attribute <see cref="MarkerHeight"/> in the configuration file.
        /// </summary>
        private const string MarkerHeightLabel = "MarkerHeight";
        /// <summary>
        /// Label of attribute <see cref="MarkerWidth"/> in the configuration file.
        /// </summary>
        private const string MarkerWidthLabel = "MarkerWidth";
        /// <summary>
        /// Label of attribute <see cref="AdditionBeamColor"/> in the configuration file.
        /// </summary>
        private const string AdditionBeamColorLabel = "AdditionBeamColor";
        /// <summary>
        /// Label of attribute <see cref="ChangeBeamColor"/> in the configuration file.
        /// </summary>
        private const string ChangeBeamColorLabel = "ChangeBeamColor";
        /// <summary>
        /// Label of attribute <see cref="DeletionBeamColor"/> in the configuration file.
        /// </summary>
        private const string DeletionBeamColorLabel = "DeletionBeamColor";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            GXLDirectory.Save(writer, GXLDirectoryLabel);
            writer.Save(MaxRevisionsToLoad, MaxRevisionsToLoadLabel);
            writer.Save(MarkerHeight, MarkerHeightLabel);
            writer.Save(MarkerWidth, MarkerWidthLabel);
            writer.Save(AdditionBeamColor, AdditionBeamColorLabel);
            writer.Save(ChangeBeamColor, ChangeBeamColorLabel);
            writer.Save(DeletionBeamColor, DeletionBeamColorLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            GXLDirectory.Restore(attributes, GXLDirectoryLabel);
            ConfigIO.Restore(attributes, MaxRevisionsToLoadLabel, ref MaxRevisionsToLoad);
            ConfigIO.Restore(attributes, MarkerHeightLabel, ref MarkerHeight);
            ConfigIO.Restore(attributes, MarkerWidthLabel, ref MarkerWidth);
            ConfigIO.Restore(attributes, AdditionBeamColorLabel, ref AdditionBeamColor);
            ConfigIO.Restore(attributes, ChangeBeamColorLabel, ref ChangeBeamColor);
            ConfigIO.Restore(attributes, DeletionBeamColorLabel, ref DeletionBeamColor);
        }
    }
}