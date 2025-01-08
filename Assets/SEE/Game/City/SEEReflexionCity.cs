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
            DrawBothGraphsAsync(visualizedSubGraph).Forget();
        }

        /// <summary>
        /// Draws <paramref name="graph"/>.
        /// Precondition: The <paramref name="graph"/> and its metrics have been loaded.
        /// </summary>
        /// <param name="graph">graph to be drawn</param>
        protected async UniTaskVoid DrawBothGraphsAsync(ReflexionGraph graph)
        {
            try
            {
                using (LoadingSpinner.ShowDeterminate($"Drawing city \"{gameObject.name}\"", out Action<float> updateProgress))
                {
                    void ReportProgress(float x)
                    {
                        ProgressBar = x;
                        updateProgress(x);
                    }

                    (Graph implementation, Graph architecture, _) = graph.Disassemble();

                    // Draw implementation.
                    {
                        GraphRenderer renderer = new(this, implementation);
                        await renderer.DrawGraphAsync(implementation, gameObject, ReportProgress, cancellationTokenSource.Token);
                    }

                    GameObject implementationRoot = gameObject.transform.GetChild(0).gameObject;
                    implementationRoot.transform.SetParent(null);

                    // Draw architecture.
                    {
                        GraphRenderer renderer = new(this, architecture);
                        await renderer.DrawGraphAsync(architecture, gameObject, ReportProgress, cancellationTokenSource.Token);
                    }

                    implementationRoot.transform.SetParent(gameObject.transform);
                }
            }
            catch (OperationCanceledException)
            {
                ShowNotification.Warn("Drawing cancelled", "Drawing was cancelled.\n", log: true);
                throw;
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
