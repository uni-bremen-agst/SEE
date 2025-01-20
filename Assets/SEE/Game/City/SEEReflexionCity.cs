using System;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.GraphProviders;
using SEE.Utils.Paths;
using SEE.Game.CityRendering;
using MoreLinq;
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
            // Makes the necessary changes for the inital types of a reflexion city.
            AddAndSetInitialType(ReflexionGraph.ArchitectureType, new(1.0f, 0.7569f, 0.0275f));
            AddAndSetInitialType(ReflexionGraph.ImplementationType, new(0.3922f, 0.7843f, 0.3922f));

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
        public ReflexionGraphProvider GetReflexionGraphProvider()
        {
            ReflexionGraphProvider provider = null;
            DataProvider.Pipeline.ForEach(p =>
            {
                if (p is ReflexionGraphProvider)
                {
                    provider = (ReflexionGraphProvider)p;
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
            await renderer.DrawGraphAsync(graph, root.GameObject(), loadReflexionFiles: true);
            // Adds the graph to the existing reflexion graph.
            graph.Nodes().ForEach(node =>
            {
                node.ItsGraph = null;
                if (projectFolder != null)
                {
                    ReflexionGraph.AddToImplementation(node);
                } else
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
    }
}
