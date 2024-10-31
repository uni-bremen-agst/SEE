using System;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using Sirenix.OdinInspector;
using Assets.SEE.Tools.ReflexionAnalysis;
using SEE.GraphProviders;
using System.Collections.Generic;
using Sirenix.Serialization;
using SEE.UI.Notification;
using System.IO;
using SEE.Utils.Config;

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
            // Tooltip("A graph provider yielding the oracle mapping for a corresponding reflexion graph"),
            TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup),
            HideReferenceObjectPicker]
        public SingleGraphPipelineProvider OracleMappingProvider { get; set; }

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
            	await UpdateRecommendationSettings();
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

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            OracleMappingProvider?.Save(writer, oracleProviderPathLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            UnityEngine.Debug.Log($"Restore: attributes.ContainsKey(oracleProviderPathLabel)={attributes.ContainsKey(oracleProviderPathLabel)} ");
            base.Restore(attributes);
            if (attributes.ContainsKey(oracleProviderPathLabel))
            {
                OracleMappingProvider = SingleGraphProvider.Restore(attributes, oracleProviderPathLabel) as SingleGraphPipelineProvider; 
            } 
            else
            {
                // TODO: Maybe a provider returning an empty graph would be the better approach
                OracleMappingProvider = null;
            }
            RecommendationSettings.OutputPath.Path = Path.Combine(Path.GetDirectoryName(ConfigurationPath.Path), "Results");
        }

        #region CandidateRecommendation

        /// <summary>
        /// 
        /// </summary>
        private RecommendationsViz candidateRecommendationViz;

        /// <summary>
        /// TODO:
        /// </summary>
        [OdinSerialize, ShowInInspector,
        // Tooltip("Settings that are used to calculate candidate recommendations within the reflexion graph."),
        TabGroup(RecommendationsFoldoutGroup), RuntimeTab(RecommendationsFoldoutGroup),
        HideReferenceObjectPicker]
        [PropertyOrder(2)]
        public RecommendationSettings RecommendationSettings = new RecommendationSettings();

        private async UniTask UpdateRecommendationSettings(ReflexionGraph loadedGraph, RecommendationSettings recommendationSettings, Graph oracleMapping)
        {
            candidateRecommendationViz = gameObject.AddOrGetComponent<RecommendationsViz>();
            this.RecommendationSettings = recommendationSettings;
            if (candidateRecommendationViz != null)
            {
                loadedGraph.Subscribe(candidateRecommendationViz);
                try
                {
                    await candidateRecommendationViz.UpdateConfiguration(loadedGraph, recommendationSettings, this, oracleMapping);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        public async UniTask UpdateRecommendationSettings(RecommendationSettings recommendationSettings)
        {
            await UpdateRecommendationSettings(VisualizedSubGraph as ReflexionGraph, recommendationSettings, oracleMapping);
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


        #region RecommendationButtons

        /// <summary>
        /// The name of the group for the Inspector buttons managing the candidate recommendation.
        /// </summary>
        protected const string RecommendationsButtonsGroup = "RecommendationsButtonsGroup";

        protected const string RecommendationsFoldoutGroup = "Recommendations";

        [Button("Update Attract Config", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Update Attract Config")]
        [PropertyOrder(2)]
        public async UniTask UpdateRecommendationSettings()
        {
            using (LoadingSpinner.ShowDeterminate($"Update attract config...",
                           out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }

                if (VisualizedSubGraph != null)
                {
                    await UpdateRecommendationSettings(VisualizedSubGraph as ReflexionGraph, RecommendationSettings, oracleMapping);
                    ShowNotification.Info("Updated Attract config.", "Updated Attract config.");
                }
                else
                {
                    ShowNotification.Warn("Couldn't update Attract config.", "Couldn't update Attract config. No graph has been loaded.");
                }

                UpdateProgress(1.0f);
            }
        }

        [Button("Reset Mapping", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Reset Mapping")]
        [PropertyOrder(3)]
        public async UniTask ResetMappingAsync()
        {
            if (VisualizedSubGraph == null)
            {
                ShowNotification.Warn("Cannot reset mapping.", "Cannot reset mapping. No graph has been loaded.");
                return;
            }

            using (LoadingSpinner.ShowDeterminate($"Reset mapping...",
                                       out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }

                await this.candidateRecommendationViz.ResetMappingAsync();
                ShowNotification.Info("Resetted Mapping.", "Resetted Mapping.");

                UpdateProgress(1.0f);
            }
        }

        [Button("Create Initial Mapping", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Create Initial mapping")]
        public async UniTask GenerateInitialMappingAsync()
        {
            if (VisualizedSubGraph == null)
            {
                ShowNotification.Warn("Couldn't start automated mapping.", "Couldn't start automated mapping. No graph has been loaded.");
                return;
            }

            using (LoadingSpinner.ShowDeterminate($"Generate initial mapping...",
                                       out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }


                await candidateRecommendationViz.CreateInitialMappingAsync(RecommendationSettings.initialMappingPercentage,
                                                                                RecommendationSettings.rootSeed,
                                                                                RecommendationSettings.moveNodes);

                ShowNotification.Info("Created initial mapping.", "Created initial mapping.");
                
                UpdateProgress(1.0f);
            }      
        }

        [Button("Start Automated Mapping", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Start Automated Mapping")]
        public async UniTask StartAutomatedMappingAsync()
        {
            if (VisualizedSubGraph == null)
            {
                ShowNotification.Warn("Couldn't start automated mapping.", "Couldn't start automated mapping. No graph has been loaded.");
                return;
            }

            using (LoadingSpinner.ShowDeterminate($"Automate mapping...",
                                       out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }

                await candidateRecommendationViz.StartAutomatedMappingAsync(
                                                    candidateRecommendation: candidateRecommendationViz.Recommendations,
                                                    ignoreTieBreakers: RecommendationSettings.IgnoreTieBreakers,
                                                    mapInViz: true,
                                                    random: new System.Random(RecommendationSettings.rootSeed));

                ShowNotification.Info("Automated mapping stopped.", "Automated mapping stopped.");
                UpdateProgress(1.0f);
            }
        }

        [Button("Run Experiment", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Run Experiment")]
        public async UniTask RunMappingExperiment()
        {
            if (VisualizedSubGraph == null)
            {
                ShowNotification.Warn("Couldn't start experiment.", "Couldn't start experiment. No graph has been loaded.");
                return;
            }

            using (LoadingSpinner.ShowDeterminate($"Run experiment...",
                           out Action<float> reportProgress))
            {
                void UpdateProgress(float progress)
                {
                    reportProgress(progress);
                    ProgressBar = progress;
                }

                try
                {
                    await candidateRecommendationViz.RunExperimentInBackground(this.RecommendationSettings, 
                                                                                             oracleMapping, 
                                                                                             UpdateProgress);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    ShowNotification.Info("Experiment failed.", "Experiment failed. Please check output.");
                    return;
                }
                ShowNotification.Info("Experiment finished.", $"Experiment finished. {System.Environment.NewLine} Results are located in {this.RecommendationSettings.OutputPath.Path}");

                UpdateProgress(1.0f);
            }
        }

        [Button("Evaluation", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Evaluation")]
        public async UniTask Evaluation()
        {
            await candidateRecommendationViz.Evaluation();
            ShowNotification.Info("finished Evaluation.", "finished Evaluation.", duration:1000);
        }

        [Button("Dump Tree", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Dump Tree")]
        public async void DumpGraph()
        {
            this.ReflexionGraph?.DumpTree();
        }

        #endregion

        #region UserStudy

        private bool studyStarted;

        [Button("Start Study", ButtonSizes.Small)]
        [ButtonGroup(RecommendationsButtonsGroup), RuntimeButton(RecommendationsButtonsGroup, "Start Study")]
        public async UniTask StartStudy()
        {
            if(studyStarted || candidateRecommendationViz == null)
            {
                return;
            }

            studyStarted = true;

            UserStudy userStudy = gameObject.AddOrGetComponent<UserStudy>();
            if (userStudy != null)
            {
                await userStudy.StartStudy(this, candidateRecommendationViz);
            } 
            else
            {
                ShowNotification.Warn("Couldn't start Userstudy.", "Couldn't start Userstudy.");
            }
        }
        #endregion

        #endregion

    }
}
