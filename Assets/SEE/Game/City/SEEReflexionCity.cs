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
using UnityEditor;
using SEE.Game.CityRendering;

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

        struct Area
        {
            public Area(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }
            public Vector3 Position;
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
    }
}
