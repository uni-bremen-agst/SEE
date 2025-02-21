using System;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.Game.CityRendering;
using MoreLinq;
using SEE.GraphProviders;
using SEE.Utils.Paths;
using SEE.Utils;

namespace SEE.Game.City
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// NOTE: It is assumed the implementation and architecture graphs are not edited!
    /// </summary>
    public class SEEReflexionCity : SEECity
    {
        /// <summary>
        /// Reflexion analysis graph. Note that this simply casts <see cref="LoadedGraph"/>,
        /// to make it easier to call reflexion-specific methods.
        /// May be <c>null</c> if the graph has not yet been loaded.
        /// </summary>
        public ReflexionGraph ReflexionGraph => VisualizedSubGraph as ReflexionGraph;

        /// <summary>
        /// The <see cref="ReflexionVisualization"/> responsible for handling reflexion analysis changes.
        /// </summary>
        private ReflexionVisualization visualization;

        /// <summary>
        /// Indicator of whether the initial reflexion city is being loaded.
        /// This is important for the <see cref="Start"/> method of this class.
        /// </summary>
        private bool initialReflexionCity = false;

        /// <summary>
        /// Executes the <see cref="SEECity.Start"/> only if the initial city is not supposed to be loaded.
        /// Regular loading of a city from files is not used for the initial reflexion city.
        /// </summary>
        protected override void Start()
        {
            if (!initialReflexionCity)
            {
                base.Start();
            }
        }

        /// <summary>
        /// First, if a graph was already loaded, everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        [Button("Load Data", ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Data")]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override async UniTask LoadDataAsync()
        {
            // Makes the necessary changes for the initial types of a reflexion city.
            AddInitialSubrootTypes();

            if (LoadedGraph != null)
            {
                Reset();
            }
            using (LoadingSpinner.ShowDeterminate($"Loading reflexion city \"{gameObject.name}\"...",
                                                  out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }

                LoadedGraph = await DataProvider.ProvideAsync(new Graph(""), this, UpdateProgress, cancellationTokenSource.Token);
            }
            visualization = gameObject.AddOrGetComponent<ReflexionVisualization>();
            visualization.StartFromScratch(VisualizedSubGraph as ReflexionGraph, this);
        }

        protected override void InitializeAfterDrawn()
        {
            base.InitializeAfterDrawn();

            // We also need to have the ReflexionVisualization apply the correct edge
            // visualization, but we have to wait until all edges have become meshes.
            if (gameObject.TryGetComponentOrLog(out EdgeMeshScheduler scheduler))
            {
                scheduler.OnInitialEdgesDone += visualization.InitializeEdges;
            }
        }

        #region SEEReflexionCity creation during in play mode
        /// <summary>
        /// Loads the initial reflexion city.
        /// </summary>
        /// <param name="cityName">the name of the city.</param>
        public void LoadInitial(string cityName)
        {
            initialReflexionCity = true;
            AddInitialSubrootTypes();
            AddUnkownNodeType();
            if (LoadedGraph != null)
            {
                Reset();
            }
            SetupInitialReflexionCity();
            using (LoadingSpinner.ShowDeterminate($"Creating initial reflexion city \"{gameObject.name}\"...",
                                                  out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }

                LoadedGraph = GetReflexionGraphProvider().ProvideInitial(cityName, this,
                    UpdateProgress, cancellationTokenSource.Token);
            }
            visualization = gameObject.AddOrGetComponent<ReflexionVisualization>();
            visualization.StartFromScratch(VisualizedSubGraph as ReflexionGraph, this);
        }

        /// <summary>
        /// Sets the initial values of a reflexion city.
        /// </summary>
        private void SetupInitialReflexionCity()
        {
            DataProvider.Add(new ReflexionGraphProvider());
            NodeLayoutSettings.Kind = NodeLayoutKind.Treemap;
        }

        /// <summary>
        /// Adds the <see cref="Graph.UnknownType"/> to the <see cref="AbstractSEECity.NodeTypes"/>
        /// with a magenta color.
        /// </summary>
        private void AddUnkownNodeType()
        {
            if (!NodeTypes.TryGetValue(Graph.UnknownType, out VisualNodeAttributes _))
            {
                VisualNodeAttributes visualNodeAttributes = new();
                visualNodeAttributes.ColorProperty.TypeColor = Color.magenta;
                visualNodeAttributes.ShowNames = true;
                NodeTypes[Graph.UnknownType] = visualNodeAttributes;
            }
        }

        /// <summary>
        /// Adds the initial subroot node types for the architecture and implementation to the <see cref="AbstractSEECity.NodeTypes"/>.
        /// </summary>
        private void AddInitialSubrootTypes()
        {
            AddAndSetInitialType(ReflexionGraph.ArchitectureType, new(1.0f, 0.7569f, 0.0275f));
            AddAndSetInitialType(ReflexionGraph.ImplementationType, new(0.3922f, 0.7843f, 0.3922f));
        }

        /// <summary>
        /// Adds the <paramref name="nodeType"/> if it does not already exist, and then configure the settings.
        /// </summary>
        /// <param name="nodeType">The node type to be added</param>
        /// <param name="color">The color for the node type.</param>
        private void AddAndSetInitialType(string nodeType, Color color)
        {
            if (!NodeTypes.TryGetValue(nodeType, out VisualNodeAttributes _))
            {
                NodeTypes[nodeType] = new VisualNodeAttributes();
            }
            SetInitialType(NodeTypes[nodeType], color);
        }

        /// <summary>
        /// Sets the necessary properties for the <paramref name="type"/>
        /// </summary>
        /// <param name="type">The type whose properties are to be set.</param>
        /// <param name="color">The node color.</param>
        private void SetInitialType(VisualNodeAttributes type, Color color)
        {
            type.AllowManualResize = true;
            type.ColorProperty.TypeColor = color;
            type.ShowNames = false;
        }

        /// <summary>
        /// Returns the <see cref="ReflexionGraphProvider"/> of this city.
        /// </summary>
        /// <returns>The <see cref="ReflexionGraphProvider"/> if it exists, otherwise null.</returns>
        private ReflexionGraphProvider GetReflexionGraphProvider()
        {
            ReflexionGraphProvider provider = null;
            DataProvider.Pipeline.ForEach(p =>
            {
                if (p is ReflexionGraphProvider pAsReflexionGraphProvider)
                {
                    provider = pAsReflexionGraphProvider;
                }
            });
            return provider;
        }

        /// <summary>
        /// Loads a part of the ReflexionCity.
        /// If the <paramref name="projectFolder"/> is null,
        /// an architecture graph is loaded; otherwise, an implementation graph is loaded.
        /// </summary>
        /// <param name="path">The data path of the graph to be loaded.</param>
        /// <param name="projectFolder">The project folder associated with the implementation graph.</param>
        /// <returns>Nothing, it is an asynchronous method that needs to wait.</returns>
        public async UniTask LoadAndDrawSubgraphAsync(DataPath path, DataPath projectFolder = null)
        {
            // FIXME (#825): This code should be moved to ReflexionGraph. It updates a subgraph.
            // Clearly, what this update requires is an implementation detail of ReflexionGraph.

            (Graph graph, GraphRenderer renderer) = await LoadGraphAsync(path, projectFolder == null);
            if (projectFolder != null)
            {
                // Sets the project directory.
                SourceCodeDirectory = projectFolder;
            }
            // Adds the missing node types with default values to the existing reflexion graph.
            AddMissingNodeTypes(graph, renderer);
            /// Attention: At this point, the root node must come from the graph's nodes list <see cref="Graph.nodes"/>.
            /// If the <see cref="ReflexionGraph.ImplementationRoot"/> or <see cref="ReflexionGraph.ArchitectureRoot"/> is used,
            /// loading doesn't work because, the children are not added to <see cref="Graph.nodes"/>.
            Node root = ReflexionGraph.GetNode(projectFolder == null? ReflexionGraph.ArchitectureRoot.ID : ReflexionGraph.ImplementationRoot.ID);

            // Draws the graph.
            await renderer.DrawGraphAsync(graph, root.GameObject(), doNotAddUniqueRoot: true);
            // Adds the graph to the existing reflexion graph.
            graph.Nodes().ForEach(node =>
            {
                node.ItsGraph = null;
                if (projectFolder != null)
                {
                    ReflexionGraph.AddToImplementation(node);
                }
                else
                {
                    ReflexionGraph.AddToArchitecture(node);
                }
            });
            graph.GetRoots().ForEach(subRoot => root.AddChild(subRoot));
            graph.Edges().ForEach((edge) =>
            {
                edge.ItsGraph = null;
                if (projectFolder != null)
                {
                    ReflexionGraph.AddToImplementation(edge);
                }
                else
                {
                    ReflexionGraph.AddToArchitecture(edge);
                }
            });

            // Ensures that the newly drawn graph is displayed.
            root.GameObject().SetActive(false);
            root.GameObject().SetActive(true);

            return;

            async UniTask<(Graph, GraphRenderer)> LoadGraphAsync(DataPath path, bool loadArchitecture)
            {
                // Loads the graph from the given path.
                ReflexionGraphProvider graphProvider = GetReflexionGraphProvider();
                Graph graph = await graphProvider.LoadGraphAsync(path, this);
                // Marks the nodes in the graph as architecture-/implementation-nodes.
                graph.MarkGraphNodesIn(loadArchitecture ? ReflexionSubgraphs.Architecture : ReflexionSubgraphs.Implementation);
                graph.Edges().ForEach(edge =>
                {
                    if (loadArchitecture)
                    {
                        edge.SetInArchitecture();
                    }
                    else
                    {
                        edge.SetInImplementation();
                    }
                });

                /// Add the path to the <see cref="SEECity.DataProvider"/>
                if (loadArchitecture)
                {
                    graphProvider.Architecture = path;
                }
                else
                {
                    graphProvider.Implementation = path;
                }

                /// Notify <see cref="RuntimeConfigMenu"/> about changes.
                if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
                {
                    runtimeConfigMenu.PerformRebuildOnNextOpening();
                }
                return (graph, (GraphRenderer)Renderer);
            }

            void AddMissingNodeTypes(Graph graph, GraphRenderer renderer)
            {
                foreach (string type in graph.AllNodeTypes())
                {
                    if (!NodeTypes.TryGetValue(type, out _))
                    {
                        NodeTypes[type] = new VisualNodeAttributes();
                        renderer.AddNewNodeType(type);
                    }
                }
            }
        }

        #endregion SEEReflexionCity creation during in play mode
    }
}
