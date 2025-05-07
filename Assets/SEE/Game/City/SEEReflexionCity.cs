using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Game.Table;
using SEE.GO;
using SEE.GraphProviders;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
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
        /// Re-draws the graph without deleting the underlying loaded graph.
        /// Only the game objects generated for the nodes and edges are deleted first
        /// and then they are re-created.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Re-Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Re-Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        [EnableIf(nameof(IsGraphDrawn))]
        public override void ReDrawGraph()
        {
            const string Prefix = "Text";
            if (LoadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                // Gather the previous architecture layout.
                (ICollection<LayoutGraphNode> layoutGraphNodes, Dictionary<string, (Vector3, Vector2, Vector3)> decorationValues)
                    = GatherNodeLayouts(AllNodeDescendants(gameObject));
                // Remember the previous position and lossy scale to detect whether the layout was rotated and to calculate the scale factor.
                Vector3 pArchPos = ReflexionGraph.ArchitectureRoot.GameObject().transform.position;
                Vector3 pArchLossyScale = ReflexionGraph.ArchitectureRoot.GameObject().transform.lossyScale;

                // Delete the previous city and draw the new one.
                DeleteGraphGameObjects();
                DrawGraph();
                // Restores the previous architecture layout.
                RestoreLayout(layoutGraphNodes, decorationValues, pArchPos, pArchLossyScale).Forget();
                graphRenderer = null;
            }
            return;

            async UniTask RestoreLayout(ICollection<LayoutGraphNode> layoutGraphNodes,
                                        Dictionary<string, (Vector3 pos, Vector2 rect, Vector3 scale)> decorationValues,
                                        Vector3 pArchPos, Vector3 pArchLossyScale)
            {
                await UniTask.WaitUntil(() => gameObject.IsCodeCityDrawn());

                // Checks if the city's layout was rotated.
                Vector3 newArchPos = ReflexionGraph.ArchitectureRoot.GameObject().transform.position;
                bool cityWasRotated = !Mathf.Approximately(pArchPos.x, newArchPos.x)
                                        && !Mathf.Approximately(pArchPos.z, newArchPos.z);

                layoutGraphNodes.ForEach(nodeLayout =>
                {
                    GameObject node = GraphElementIDMap.Find(nodeLayout.ID);
                    if (node != null)
                    {
                        node.NodeOperator().ScaleTo(DetermineScale(nodeLayout, pArchLossyScale, cityWasRotated), 0);
                        node.NodeOperator().MoveTo(DeterminePosition(nodeLayout, cityWasRotated), 0);
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

            Vector3 DetermineScale(LayoutGraphNode nodeLayout, Vector3 prevLossyScale, bool cityWasRotated)
            {
                Vector3 currentLossyScale = ReflexionGraph.ArchitectureRoot.GameObject().transform.lossyScale;

                if (!cityWasRotated)
                {
                    Vector3 scaleFactor = new(
                        currentLossyScale.x / prevLossyScale.x,
                        currentLossyScale.y / prevLossyScale.y,
                        currentLossyScale.z / prevLossyScale.z);

                    return new(
                        nodeLayout.AbsoluteScale.x / scaleFactor.x,
                        nodeLayout.AbsoluteScale.y,
                        nodeLayout.AbsoluteScale.z / scaleFactor.z);
                }
                else
                {
                    Vector3 rotatedScaleFactor = new(
                        currentLossyScale.z / prevLossyScale.x,
                        currentLossyScale.y / prevLossyScale.y,
                        currentLossyScale.x / prevLossyScale.z);

                    return new(
                        nodeLayout.AbsoluteScale.z / rotatedScaleFactor.z,
                        nodeLayout.AbsoluteScale.y,
                        nodeLayout.AbsoluteScale.x / rotatedScaleFactor.x);
                }
            }

            Vector3 DeterminePosition(LayoutGraphNode nodeLayout, bool cityWasRotated)
            {
                Vector3 pos = nodeLayout.CenterPosition;
                if (cityWasRotated)
                {
                    pos = new(pos.x, pos.y, -pos.z);
                }
                return ReflexionGraph.ArchitectureRoot.GameObject().transform.TransformPoint(pos);
            }

            (ICollection<LayoutGraphNode>, Dictionary<string, (Vector3, Vector2, Vector3)>) GatherNodeLayouts(ICollection<GameObject> gameObjects)
            {
                IList<LayoutGraphNode> result = new List<LayoutGraphNode>();
                Dictionary<string, (Vector3, Vector2, Vector3)> textValues = new();
                foreach (GameObject gameObject in gameObjects)
                {
                    Node node = gameObject.GetComponent<NodeRef>().Value;
                    // skip root or non architecture nodes. Their restoration is handled by the layout itself.
                    if (node.IsRoot() || node.IsArchitectureOrImplementationRoot() || !node.IsInArchitecture())
                    {
                        continue;
                    }
                    LayoutGraphNode layoutNode = new(node)
                    {
                        CenterPosition = gameObject.transform.localPosition,
                        AbsoluteScale = gameObject.transform.localScale,
                    };
                    result.Add(layoutNode);
                    // Case for decorative texts that start with the prefix "Text".
                    if (gameObject.FindChildWithPrefix(Prefix) != null)
                    {
                        RectTransform text = (RectTransform)gameObject.FindChildWithPrefix(Prefix);
                        textValues.Add(node.ID, (text.localPosition, text.rect.size, text.localScale));
                    }
                    // Case for label texts that start with the prefix "Label".
                    else if (gameObject.GetComponentInChildren<TextMeshPro>() != null)
                    {
                        textValues.Add(node.ID,
                            (Vector3.zero, Vector2.zero, gameObject.GetComponentInChildren<TextMeshPro>().transform.localScale));
                    }
                }
                LayoutNodes.SetLevels(result);
                return (result, textValues);
            }
        }

        /// <summary>
        /// Extends the existing reset functionality so that,
        /// in the case of an initial ReflexionCity, the empty initial
        /// ReflexionCity is loaded again.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            if (initialReflexionCity)
            {
                LoadInitial(gameObject.name);
                DrawGraph();
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
            NodeLayoutSettings.Kind = NodeLayoutKind.Reflexion;
            NodeLayoutSettings.ArchitectureLayoutProportion = 0.6f;
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
