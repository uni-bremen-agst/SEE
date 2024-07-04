using System;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using UnityEngine;
using Sirenix.OdinInspector;
using Assets.SEE.Tools.ReflexionAnalysis;
using SEE.GraphProviders;
using System.Collections.Generic;
using Sirenix.Serialization;
using SEE.UI.Notification;

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

        private Graph oracleMapping = null;

        /// <summary>
        /// The <see cref="ReflexionVisualization"/> responsible for handling reflexion analysis changes.
        /// </summary>
        private ReflexionVisualization visualization;

        private string oracleProviderPathLabel = "OracleGraph";

        /// <summary>
        /// A provider of the data shown as code city.
        /// </summary>
        [OdinSerialize, ShowInInspector,
            Tooltip("A graph provider yielding the oracle mapping for a corresponding reflexion graph"),
            TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup),
            HideReferenceObjectPicker]
        public PipelineGraphProvider OracleMappingProvider { get; set; }

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
	            if(OracleMappingProvider != null)
	            {
	                oracleMapping = await OracleMappingProvider.ProvideAsync(new Graph(""), this, UpdateProgress, cancellationTokenSource.Token);
	            }
            	UpdateRecommendationSettings();
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
        protected override void Restore(Dictionary<string, object> attributes)
        {
            UnityEngine.Debug.Log($"Restore: attributes.ContainsKey(oracleProviderPathLabel)={attributes.ContainsKey(oracleProviderPathLabel)} ");
            base.Restore(attributes);
            if (attributes.ContainsKey(oracleProviderPathLabel))
            {
                OracleMappingProvider = GraphProvider.Restore(attributes, oracleProviderPathLabel) as PipelineGraphProvider; 
            } 
            else
            {
                // TODO: Maybe a provider returning an empty graph would be the better approach
                OracleMappingProvider = null;
            }
        }

        #region CandidateRecommendation

        /// <summary>
        /// 
        /// </summary>
        private CandidateRecommendationViz candidateRecommendationViz;

        /// <summary>
        /// TODO:
        /// </summary>
        [OdinSerialize, ShowInInspector,
        Tooltip("Settings that are used to calculate candidate recommendations within the reflexion graph."),
        TabGroup(RecommendationsFoldoutGroup), RuntimeTab(RecommendationsFoldoutGroup),
        HideReferenceObjectPicker]
        [PropertyOrder(2)]
        public RecommendationSettings recommendationSettings = new RecommendationSettings();

        private void UpdateRecommendationSettings(ReflexionGraph loadedGraph, RecommendationSettings recommendationSettings, Graph oracleMapping)
        {
            candidateRecommendationViz = gameObject.AddOrGetComponent<CandidateRecommendationViz>();
            if (candidateRecommendationViz != null)
            {
                loadedGraph.Subscribe(candidateRecommendationViz);
                candidateRecommendationViz.UpdateConfiguration(loadedGraph, recommendationSettings, oracleMapping);
            }
        }

        #region Interfaces

        public async UniTask ShowRecommendations(Node node)
        {
            if(candidateRecommendationViz != null)
            {
                await this.candidateRecommendationViz.ShowRecommendations(node);
            }
        }

        #endregion


        #region CandidateRecommendationButtons

        /// <summary>
        /// The name of the group for the Inspector buttons managing the candidate recommendation.
        /// </summary>
        protected const string RecommendationsButtonsGroup = "RecommendationsButtonsGroup";

        protected const string RecommendationsFoldoutGroup = "Recommendations";

        [Button("Update MappingChoice Settings", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Update Attract function")]
        // TODO: Property order?
        // necessary regarding disabling enabling?
        [PropertyOrder(2)]
        public void UpdateRecommendationSettings()
        {
            UpdateRecommendationSettings(VisualizedSubGraph as ReflexionGraph, recommendationSettings, oracleMapping);
        }

        [Button("Reset Mapping", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Reset Mapping")]
        // TODO: Property order?
        // necessary regarding disabling enabling?
        // TODO: Make this call async?
        [PropertyOrder(3)]
        public async UniTask ResetMappingAsync()
        {
            using (LoadingSpinner.ShowDeterminate($"reset mapping...",
                                       out Action<float> reportProgress))
            {
                if (VisualizedSubGraph != null)
                {
                    await this.candidateRecommendationViz.ResetMappingAsync();
                }
                else
                {
                    ShowNotification.Warn("Cannot reset Mapping.", "Cannot reset Mapping. No Graph has been loaded.");
                } 
            }
        }

        [Button("Generate initial mapping", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Generate initial mapping")]
        public async UniTask GenerateInitialMappingAsync()
        {
            using (LoadingSpinner.ShowDeterminate($"Generate initial mapping...",
                                       out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }

                if (candidateRecommendationViz != null)
                {
                    await candidateRecommendationViz.CreateInitialMappingAsync(recommendationSettings.InitialMappingPercentage, 
                                                                               recommendationSettings.MasterSeed);
                }
            }
        }

        [Button("Start automated mapping", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Start automated mapping")]
        public async UniTask StartAutomatedMappingAsync()
        {
            using (LoadingSpinner.ShowDeterminate($"automate mapping...",
                                       out Action<float> reportProgress))
            {
                await candidateRecommendationViz.StartAutomatedMappingAsync(null,
                                                                    syncWithView: true,
                                                                    ignoreTieBreakers: recommendationSettings.IgnoreTieBreakers,
                                                                    new System.Random(recommendationSettings.MasterSeed)); 
            }
        }

        [Button("Run Experiment", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Run Experiment")]
        public async void RunMappingExperiment()
        {
            await candidateRecommendationViz.RunExperimentAsync(this.recommendationSettings, oracleMapping);
        }

        #endregion

        #endregion
    }
}
