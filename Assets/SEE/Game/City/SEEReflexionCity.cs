using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.GO;
using SEE.GraphProviders;
using SEE.Layout;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

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
        /// Indicates whether the initial Reflexion city should be loaded.
        /// </summary>
        private bool IsInitialReflexionCity => initialReflexionCity;

        /// <summary>
        /// Indicates whether the Reflexion city should be loaded from a file.
        /// </summary>
        private bool IsLoadedReflexionCity => !initialReflexionCity;

        /// <summary>
        /// Whether the initial city state has been loaded.
        /// </summary>
        private bool initialCityStateLoaded = false;

        /// <summary>
        /// Checks whether the city is in its initial configuration state.
        /// </summary>
        /// <returns><c>true</c> if the configuration should be reloaded. Otherwise, false.</returns>
        private bool IsInitialState() => initialCityStateLoaded;

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
        /// Executes <see cref="AbstractSEECity.LoadConfiguration"/>
        /// and sets <see cref="initialReflexionCity"/> to false.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(ConfigurationButtonsGroup), RuntimeButton(ConfigurationButtonsGroup, "Load Configuration")]
        [PropertyOrder(ConfigurationButtonsGroupLoad), RuntimeGroupOrder(ConfigurationButtonsGroupLoad)]
        public override void LoadConfiguration()
        {
            base.LoadConfiguration();
            SwitchLoaded();
            initialCityStateLoaded = false;
        }

        /// <summary>
        /// First, if a graph was already loaded, everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        /// <returns>Whether the menus needs adjustment.</returns>
        [Button("Load Data", ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Data")]
        [PropertyOrder(DataButtonsGroupOrderLoad), RuntimeGroupOrder(DataButtonsGroupOrderLoad)]
        public override async UniTask<bool> LoadDataAsync()
        {
            bool needMenuAdjustments = false;
            if (initialReflexionCity)
            {
                needMenuAdjustments = !IsInitialState();
                ResetToInitial();
                LoadInitial(gameObject.name);
                return needMenuAdjustments;
            }
            else if (IsInitialState())
            {
                LoadConfiguration();
                needMenuAdjustments = true;
            }

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
            return needMenuAdjustments;
        }

        /// <summary>
        /// Resets all configuration values to their initial default state.
        /// </summary>
        private void ResetToInitial()
        {
            LODCulling = 0.001f;
            HierarchicalEdges = HierarchicalEdgeTypes();
            HiddenEdges = new();
            NodeTypes = new();
            AddRootNodeType();
            IgnoreSelfLoopsInLifting = false;
            MaximalAntennaSegmentHeight = 0.5f;
            AntennaWidth = 0.1f;
            BaseAnimationDuration = 1.0f;
            MetricToColor = new();
            ZScoreScale = false;
            ScaleOnlyLeafMetrics = true;
            ErosionSettings = new();
            BoardSettings = new();
            NodeLayoutSettings = new();
            EdgeLayoutSettings = new();
            EdgeSelectionSettings = new();
            MarkerAttributes = new();
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
        /// Ensures that the initial Reflexion city is loaded.
        /// Is used in the Odin and Runtime menu.
        /// </summary>
        [RuntimeButton(DataButtonsGroup, "Switch to initial Reflexion city")]
        [PropertyOrder(DataButtonsGroupOrderSwitch), RuntimeGroupOrder(DataButtonsGroupOrderSwitch)]
        [RuntimeEnableIf(nameof(IsLoadedReflexionCity))]
        [RuntimeHideIf(nameof(IsInitialReflexionCity))]
        public void SwitchInitial()
        {
            initialReflexionCity = true;
        }

        /// <summary>
        /// Ensures that the Reflexion city is loaded from a file.
        /// </summary>
        [Button("Switch to loaded Reflexion city", ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Switch to loaded Reflexion city")]
        [PropertyOrder(DataButtonsGroupOrderSwitch), RuntimeGroupOrder(DataButtonsGroupOrderSwitch)]
        [EnableIf(nameof(IsInitialReflexionCity)), RuntimeEnableIf(nameof(IsInitialReflexionCity))]
        [HideIf(nameof(IsLoadedReflexionCity)), RuntimeHideIf(nameof(IsLoadedReflexionCity))]
        public void SwitchLoaded()
        {
            initialReflexionCity = false;
        }

        /// <summary>
        /// Re-draws the graph without deleting the underlying loaded graph.
        /// Only the game objects generated for the nodes and edges are deleted first
        /// and then they are re-created.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Re-Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Re-Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw), RuntimeGroupOrder(DataButtonsGroupOrderDraw)]
        [EnableIf(nameof(IsGraphDrawn)), RuntimeEnableIf(nameof(IsGraphDrawn))]
        public override void ReDrawGraph()
        {
            const string Prefix = "Text";
            if (LoadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                ReDrawGraphAsync().Forget();

            }
            return;
            async UniTask ReDrawGraphAsync()
            {
                // Gather the previous node layouts.
                (ICollection<LayoutGraphNode> layoutGraphNodes, Dictionary<string, (Vector3, Vector2, Vector3)> decorationValues)
                    = GatherNodeLayouts(AllNodeDescendants(gameObject));
                // Remember the previous position and lossy scale to detect whether the layout was rotated and to calculate the scale factor.
                Vector3 prevArchPos = ReflexionGraph.ArchitectureRoot.GameObject().transform.position;
                Vector3 prevArchLossyScale = ReflexionGraph.ArchitectureRoot.GameObject().transform.lossyScale;

                // Restore the original implementation graph.
                (Graph impl, _, Graph mapped) = ReflexionGraph.Disassemble();
                if (mapped.EdgeCount > 0)
                {
                    await RestoreImplementation(impl);
                    mapped.Edges().ForEach(edge =>
                        ReflexionGraph.RemoveFromMapping(VisualizedSubGraph.GetEdge(edge.ID)));
                }

                // Delete the previous city and draw the new one.
                DeleteGraphGameObjects();
                await UniTask.DelayFrame(2);
                DrawGraph();
                await UniTask.WaitUntil(() => gameObject.IsCodeCityDrawn())
                    .ContinueWith(() => UniTask.DelayFrame(2)); // Will be needed for restore the position of the edges.
                // Restores the previous architecture layout.
                RestoreArchitectureLayout(layoutGraphNodes, decorationValues, prevArchPos, prevArchLossyScale);
                // Restores the previous mapping.
                RestoreMapping(layoutGraphNodes, mapped);
                visualization.InitializeEdges();
                graphRenderer = null;
            }

            (ICollection<LayoutGraphNode>, Dictionary<string, (Vector3, Vector2, Vector3)>) GatherNodeLayouts(ICollection<GameObject> gameObjects)
            {
                IList<LayoutGraphNode> result = new List<LayoutGraphNode>();
                Dictionary<string, (Vector3, Vector2, Vector3)> textValues = new();
                foreach (GameObject gameObject in gameObjects)
                {
                    Node node = gameObject.GetComponent<NodeRef>().Value;
                    // Skip the root node. For implementation nodes, we only need the position.
                    if (node.IsRoot())
                    {
                        continue;
                    }
                    LayoutGraphNode layoutNode = new(node)
                    {
                        CenterPosition = gameObject.transform.position,
                        AbsoluteScale = gameObject.transform.localScale,
                    };
                    result.Add(layoutNode);

                    // Skip non-architecture nodes. Their text restoration is handled by the layout itself.
                    if (node.IsInArchitecture())
                    {
                        // Case for decorative texts that start with the prefix "Text".
                        if (gameObject.FindChildWithPrefix(Prefix) != null)
                        {
                            RectTransform text = (RectTransform)gameObject.FindChildWithPrefix(Prefix).transform;
                            textValues.Add(node.ID, (text.localPosition, text.rect.size, text.localScale));
                        }
                        // Case for label texts that start with the prefix "Label".
                        else if (gameObject.GetComponentInChildren<TextMeshPro>() != null)
                        {
                            textValues.Add(node.ID,
                                (Vector3.zero, Vector2.zero, gameObject.GetComponentInChildren<TextMeshPro>().transform.localScale));
                        }
                    }
                }
                LayoutNodes.SetLevels(result);
                return (result, textValues);
            }

            async UniTask RestoreImplementation(Graph copyCurrentImpl)
            {
                // Create a copy of the original implementation graph to restore the current one to its original state.
                GameObject tempGO = new($"COPY OF {gameObject.name}");
                SEEReflexionCity tempCity = tempGO.AddComponent<SEEReflexionCity>();
                tempCity.LoadInitial(tempGO.name);
                Graph implCopy = await tempCity.GetReflexionGraphProvider()
                    .LoadGraphAsync(GetReflexionGraphProvider().Implementation, tempCity);
                Node currentImplRoot = copyCurrentImpl.GetRoots().First(node => node.Type == ReflexionGraph.ImplementationType);
                implCopy.AddSingleRoot(out Node root, currentImplRoot.ID, ReflexionGraph.ImplementationType);

                // Restore to the original parents.
                foreach (Node copyNode in copyCurrentImpl.Nodes())
                {
                    if (copyNode == null
                        || copyNode.Type.Equals(ReflexionGraph.ImplementationType))
                    {
                        continue;
                    }

                    Node copyOriginalNode = implCopy.GetNode(copyNode.ID);
                    Node copyOriginalParent = copyOriginalNode?.Parent;
                    Node copyCurrentParent = copyNode.Parent;
                    bool shouldReparent = copyOriginalParent != null
                        && (copyCurrentParent == null || !copyCurrentParent.ID.Equals(copyOriginalParent.ID));

                    if (shouldReparent)
                    {
                        Node realParent = VisualizedSubGraph.GetNode(copyOriginalParent.ID);
                        VisualizedSubGraph.GetNode(copyNode.ID).Reparent(realParent);

                    }
                }
                Destroyer.Destroy(tempGO);
            }

            void RestoreArchitectureLayout(ICollection<LayoutGraphNode> layoutGraphNodes,
                                        Dictionary<string, (Vector3 pos, Vector2 rect, Vector3 scale)> decorationValues,
                                        Vector3 pArchPos, Vector3 pArchLossyScale)
            {
                layoutGraphNodes.ForEach(nodeLayout =>
                {
                    GameObject node = GraphElementIDMap.Find(nodeLayout.ID);
                    if (node != null && !node.IsArchitectureOrImplementationRoot() && node.GetNode().IsInArchitecture())
                    {
                        node.NodeOperator().ScaleTo(DetermineScale(nodeLayout, pArchLossyScale), 0);
                        node.NodeOperator().MoveTo(nodeLayout.CenterPosition, 0);
                        node.GetComponentsInChildren<TextMeshPro>().ForEach(tmp =>
                        {
                            if (decorationValues.ContainsKey(nodeLayout.ID))
                            {
                                RectTransform tmpRect = (RectTransform)tmp.transform;
                                tmpRect.localScale = decorationValues[nodeLayout.ID].scale;
                                if (tmp.name.StartsWith(Prefix))
                                {
                                    tmpRect.sizeDelta = decorationValues[nodeLayout.ID].rect;
                                    tmpRect.localPosition = decorationValues[nodeLayout.ID].pos;
                                }
                            }
                        });
                    }
                });
            }

            Vector3 DetermineScale(LayoutGraphNode nodeLayout, Vector3 prevLossyScale)
            {
                Vector3 currentLossyScale = ReflexionGraph.ArchitectureRoot.GameObject().transform.lossyScale;

                Vector3 scaleFactor = new(
                    currentLossyScale.x / prevLossyScale.x,
                    currentLossyScale.y / prevLossyScale.y,
                    currentLossyScale.z / prevLossyScale.z);

                return new(
                    nodeLayout.AbsoluteScale.x / scaleFactor.x,
                    nodeLayout.AbsoluteScale.y,
                    nodeLayout.AbsoluteScale.z / scaleFactor.z);
            }

            void RestoreMapping(ICollection<LayoutGraphNode> layoutGraphNodes,
                                Graph previousMapping)
            {
                previousMapping.Edges().ForEach(edge =>
                {
                    Node source = ReflexionGraph.GetNode(edge.Source.ID);
                    Node target = ReflexionGraph.GetNode(edge.Target.ID);
                    GameObject sourceGO = source.GameObject();
                    if (sourceGO != null)
                    {
                        LayoutGraphNode sourceLayout = layoutGraphNodes.First(layout =>
                            layout.ID.Equals(source.ID));
                        sourceGO.NodeOperator().MoveTo(sourceLayout.CenterPosition, 0);
                        ReflexionMapper.SetParent(sourceGO, target.GameObject());
                    }
                });
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
            initialCityStateLoaded = true;
            AddInitialSubrootTypes();
            CheckAndAddUnknownNodeType();
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
            if (!DataProvider.Pipeline.Any(provider => provider is ReflexionGraphProvider))
            {
                DataProvider.Add(new ReflexionGraphProvider());
            }
            NodeLayoutSettings.Kind = NodeLayoutKind.Reflexion;
            NodeLayoutSettings.ArchitectureLayoutProportion = 0.6f;
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
            Node root = ReflexionGraph.GetNode(projectFolder == null ? ReflexionGraph.ArchitectureRoot.ID : ReflexionGraph.ImplementationRoot.ID);

            // Draws the graph.
            await renderer.DrawGraphAsync(graph, root.GameObject(), doNotAddUniqueRoot: true);
            // Adds the graph to the existing reflexion graph.
            ReflexionGraph.AddSubgraphInContext(graph, root, projectFolder != null);

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

                UpdateRuntimeMenu().Forget();

                return (graph, (GraphRenderer)Renderer);
            }

            async UniTask UpdateRuntimeMenu()
            {
                // We need to wait for the changes to occur.
                await UniTask.Yield();
                if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
                {
                    runtimeConfigMenu.PerformUpdate(this);
                }
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
