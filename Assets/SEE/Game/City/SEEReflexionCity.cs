using System;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.Utils.Config;
using System.Collections.Generic;
using System.Linq;
using SEE.UI.Notification;
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
        /// The node layout settings for the architecture graph.
        /// </summary>
        [Tooltip("Settings for the architecture node layout."),
            TabGroup(NodeFoldoutGroup), RuntimeTab(NodeFoldoutGroup)]
        public NodeLayoutAttributes ArchitectureNodeLayoutSettings = new();

        /// <summary>
        /// The proportion of space allocated for the architecture.
        /// This number relates to the longer edge of the available rectangle.
        /// </summary>
        [Tooltip("The proportion of space allocated for the architecture. This number relates to the longer edge of the available rectangle."),
            TabGroup(NodeFoldoutGroup), RuntimeTab(NodeFoldoutGroup)]
        [Range(0f, 1f)]
        public float ArchitectureLayoutProportion = 0.5f;

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

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        [EnableIf(nameof(IsGraphLoaded))]
        public override void DrawGraph()
        {
            if (IsPipelineRunning)
            {
                ShowNotification.Error("Graph Drawing", "Graph provider pipeline is still running.");
                return;
            }
            ReflexionGraph visualizedSubGraph = ReflexionGraph;
            if (visualizedSubGraph == null)
            {
                ShowNotification.Error("Graph Drawing", "No graph loaded.");
            }
            RenderReflexionGraphAsync(visualizedSubGraph, gameObject).Forget();
        }

        #region Rendering
        /// <summary>
        /// Represents a plane in 3D space where to draw a code city for reflexion analysis,
        /// that is, an area for the implementation city or the architecture city.
        /// </summary>
        struct Area
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="position">The center point of the area in world space.</param>
            /// <param name="scale">The scale of the area in world space.</param>
            public Area(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }
            /// <summary>
            /// The center point of the area in world space.
            /// </summary>
            public Vector3 Position;
            /// <summary>
            /// The scale of the area in world space. Only its x and z components
            /// are relevant.
            /// </summary>
            public Vector3 Scale;
        }

        /// <summary>
        /// Draws <paramref name="graph"/>.
        /// Precondition: The <paramref name="graph"/> and its metrics have been loaded.
        /// </summary>
        /// <param name="graph">graph to be drawn</param>
        /// <param name="codeCity">the game object representing the code city and holding
        /// a <see cref="SEEReflexionCity"/> component</param>
        protected async UniTaskVoid RenderReflexionGraphAsync(ReflexionGraph graph, GameObject codeCity)
        {
            if (codeCity.TryGetComponent(out SEEReflexionCity reflexionCity))
            {
                // The original real-world position and scale of codeCity.
                Area codeCityOriginal = new(codeCity.transform.position, codeCity.transform.lossyScale);

                Split(codeCity, reflexionCity.ArchitectureLayoutProportion,
                    out Area implementionArea, out Area architectureArea);

                try
                {
                    using (LoadingSpinner.ShowDeterminate($"Drawing reflexion city \"{codeCity.name}\"", out Action<float> updateProgress))
                    {
                        void ReportProgress(float x)
                        {
                            ProgressBar = x;
                            updateProgress(x);
                        }

                        (Graph implementation, Graph architecture, _) = graph.Disassemble();

                        // There should be no more than one root.
                        Node reflexionRoot = graph.GetRoots().FirstOrDefault();

                        // There could be no root at all in case the architecture and implementation
                        // graphs are both empty.
                        if (reflexionRoot != null)
                        {
                            // The parent of the two game object hierarchies for the architecture and implementation.
                            GameObject reflexionCityRoot;

                            // Draw implementation.
                            {
                                GraphRenderer renderer = new(this, implementation);
                                // reflexionCityRoot will be the direct and only child of gameObject
                                reflexionCityRoot = renderer.DrawNode(reflexionRoot, codeCity);
                                reflexionCityRoot.transform.SetParent(codeCity.transform);
                                // Render the implementation graph under reflexionCityRoot.
                                await renderer.DrawGraphAsync(implementation, reflexionCityRoot, ReportProgress, cancellationTokenSource.Token);
                            }

                            // We need to temporarily unlink the implementation graph from reflexionCityRoot
                            // because graph renderering assumes that the parent has no other child.
                            GameObject implementationRoot = reflexionCityRoot.transform.GetChild(0).gameObject;
                            implementationRoot.transform.SetParent(null);

                            // Draw architecture.
                            {
                                GraphRenderer renderer = new(this, architecture);
                                await renderer.DrawGraphAsync(architecture, reflexionCityRoot, ReportProgress, cancellationTokenSource.Token);
                            }

                            implementationRoot.transform.SetParent(reflexionCityRoot.transform);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    ShowNotification.Warn("Drawing cancelled", "Drawing was cancelled.\n", log: true);
                    throw;
                }
                finally
                {
                    RestoreCodeCity();
                }

                return;

                // Restores codeCity to its codeCityOriginalPosition and codeCityOriginalScale.
                void RestoreCodeCity()
                {
                    codeCity.transform.position = codeCityOriginal.Position;
                    codeCity.SetAbsoluteScale(codeCityOriginal.Scale, false);
                }

                void Split(GameObject codeCity, float architectureLayoutProportion,
                    out Area implementionArea, out Area architectureArea)
                {
                    bool xIsLongerEdge = codeCity.transform.lossyScale.x >= codeCity.transform.lossyScale.y;

                    if (architectureLayoutProportion <= 0)
                    {
                        // the implemenation takes all the available space
                        implementionArea = new(codeCity.transform.position, codeCity.transform.lossyScale);
                        // the architecture sits at the end of the longer edge of the implementation with zero space
                        Vector3 architecturePos = implementionArea.Position;
                        if (xIsLongerEdge)
                        {
                            architecturePos.x = implementionArea.Position.x + implementionArea.Scale.x / 2;
                        }
                        else
                        {
                            architecturePos.z = implementionArea.Position.z + implementionArea.Scale.z / 2;
                        }
                        architectureArea = new(architecturePos, Vector3.zero);
                    }
                    else if (architectureLayoutProportion >= 1)
                    {
                        // the architecture takes all the available space
                        architectureArea = new(codeCity.transform.position, codeCity.transform.lossyScale);
                        // the implementation sits at the begin of the longer edge of the architecture with zero space
                        Vector3 implementationPos = architectureArea.Position;
                        if (xIsLongerEdge)
                        {
                            implementationPos.x = architectureArea.Position.x - architectureArea.Scale.x / 2;
                        }
                        else
                        {
                            implementationPos.z = architectureArea.Position.z - architectureArea.Scale.z / 2;
                        }
                        implementionArea = new(implementationPos, Vector3.zero);
                    }
                    else
                    {
                        implementionArea = new(codeCity.transform.position, codeCity.transform.lossyScale);
                        architectureArea = new(codeCity.transform.position, codeCity.transform.lossyScale);
                        if (xIsLongerEdge)
                        {
                            // Shrink and move the implementionArea to the left.
                            {
                                // The proportion of the implemenation area.
                                float implementationLayoutProportion = 1 - architectureLayoutProportion;
                                // The begin of the longer edge in world space. This reference point will stay the same.
                                float shorterLeftWorldSpaceEdge = implementionArea.Position.x - implementionArea.Scale.x / 2;
                                // Distance from shorterLeftWorldSpaceEdge to original center.
                                float originalRelativeCenter = implementionArea.Position.x - shorterLeftWorldSpaceEdge;
                                implementionArea.Position.x = shorterLeftWorldSpaceEdge + originalRelativeCenter * implementationLayoutProportion;
                                implementionArea.Scale.x *= implementationLayoutProportion;
                            }


                            architectureArea.Scale.x *= architectureLayoutProportion;
                        }

                    }
                }
            }
            else
            {
                ShowNotification.Error("Graph Drawing", $"Code city {codeCity.name} is missing a reflexion-city component.");
            }
        }
        #endregion Rendering

        #region ConfigIO
        /// <summary>
        /// Label in the configuration file for <see cref="ArchitectureNodeLayoutSettings"/>.
        /// </summary>
        private const string architectureLayoutSettingsLabel = "ArchitectureNodeLayout";
        /// <summary>
        /// Label in the configuration file for <see cref="ArchitectureLayoutProportion"/>.
        /// </summary>
        private const string architectureLayoutProportionLabel = "architectureProportion";

        /// <summary>
        /// Saves all attributes of this instance in the configuration file
        /// using the given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">writer for the configuration file</param>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            ArchitectureNodeLayoutSettings.Save(writer, architectureLayoutSettingsLabel);
            writer.Save(ArchitectureLayoutProportion, architectureLayoutProportionLabel);
        }

        /// <summary>
        /// Restores all attributes of this instance from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary containing the attributes (key = attribute label, value = attribute value)</param>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            ConfigIO.Restore(attributes, architectureLayoutProportionLabel, ref ArchitectureLayoutProportion);
            ArchitectureNodeLayoutSettings.Restore(attributes, architectureLayoutSettingsLabel);
        }
        #endregion ConfigIO

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
    }
}
