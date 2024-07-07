using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.PropertyDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class UserStudy : MonoBehaviour
    {
        private bool iterationFinished;

        private SEEReflexionCity city;

        private CandidateRecommendationViz candidateRecommendation;

        private IList<UserStudyRun> userStudyRuns = new List<UserStudyRun>();

        private UserStudyRun currentRun;

        public async UniTask StartStudy(SEEReflexionCity city, CandidateRecommendationViz candidateRecommendation)
        {
            this.city = city;
            this.candidateRecommendation = candidateRecommendation;

            SetupRuns();

            foreach(UserStudyRun run in userStudyRuns)
            {
                currentRun = run;
                await ResetCity(run);

                this.StartNextIteration();

                if (!run.Automatic)
                {
                    await this.WaitForParticipant(); 
                } else
                {
                    // TODO:
                }

                candidateRecommendation.CalculateResults();

                UnityEngine.Debug.Log("Finished Iteration.");
            }

            async UniTask ResetCity(UserStudyRun run)
            {
                city.Reset();
                await city.LoadDataAsync();
                city.DrawGraph();
                await city.UpdateRecommendationSettings(run.Settings);
                MoveAction.UnblockMovement();
                await candidateRecommendation.CreateInitialMappingAsync(run.Settings.InitialMappingPercentage,
                                                                        run.Settings.RootSeed, 
                                                                        syncWithView: false,
                                                                        delay: 0);
                BlockUnnecessaryMovement();
                candidateRecommendation.ColorUnmappedCandidates(Color.white);
                candidateRecommendation.StartRecording();
            }

            void BlockUnnecessaryMovement()
            {
                IEnumerable<Node> unmappedCandidates = candidateRecommendation.GetUnmappedCandidates();
                candidateRecommendation.ReflexionGraphVisualized.Nodes().Where(n => !unmappedCandidates.Contains(n)).ForEach(n => MoveAction.BlockMovement(n.ID));
            }
        }

        public void StartNextIteration()
        {
            iterationFinished = false;
        }

        public async UniTask WaitForParticipant()
        {
            while (true)
            {
                await UniTask.WaitWhile(() => !iterationFinished);
                UnityEngine.Debug.Log("Participant finished.");
                await UniTask.Delay(500);
                int retVal = await OpenConfirmationDialog();
                if (retVal == 0)
                {
                    UnityEngine.Debug.Log("Mapping confirmed.");
                    return;
                }
                else
                {
                    UnityEngine.Debug.Log("Mapping declined.");
                    iterationFinished = false;
                }
            }
        }

        public void Update()
        {
            if (SEEInput.FinishStudyIteration() && (this.candidateRecommendation.GetUnmappedCandidates().Count() <= 0 || currentRun.TestRun))
            {
                iterationFinished = true;
            }
        }

        public async UniTask<int> OpenConfirmationDialog()
        {
            GameObject dialog = new GameObject("ConfirmationDialog");
            PropertyGroup propertyGroup = dialog.AddComponent<PropertyGroup>();
            propertyGroup.Name = "Confirm Mapping";

            propertyGroup.GetReady();
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Confirm Mapping?";
            propertyDialog.Description = $"Did you finish the current mapping?";
            propertyDialog.AddGroup(propertyGroup);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;

            using var confirmHandler = propertyDialog.OnConfirm.GetAsyncEventHandler(default);
            using var cancelHandler = propertyDialog.OnCancel.GetAsyncEventHandler(default);

            int retVal = await UniTask.WhenAny(confirmHandler.OnInvokeAsync(), cancelHandler.OnInvokeAsync());

            SEEInput.KeyboardShortcutsEnabled = true;

            return retVal;
        }

        public void SetupRuns()
        {
            userStudyRuns.Clear();
            
            // First Run
            UserStudyRun run = new();
            run.Automatic = false;
            run.Settings = SetDefaultSettings();
            run.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.CountAttract;
            run.Settings.ExperimentName = "Run_CA";
            userStudyRuns.Add(run);

            // Second Run 
            run = new();
            run.Automatic = false;
            run.Settings = SetDefaultSettings();
            run.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.NBAttract;
            run.Settings.ExperimentName = "Run_NB";
            userStudyRuns.Add(run);

            //Third Run
            run = new();
            run.Automatic = false;
            run.Settings = SetDefaultSettings();
            run.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.ADCAttract;
            run.Settings.ExperimentName = "Run_ADC";
            userStudyRuns.Add(run);

            // Fourth Run
            run = new();
            run.Automatic = true;
            run.Settings = SetDefaultSettings();
            run.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.CountAttract;
            run.Settings.ExperimentName = "Run_CA";
            userStudyRuns.Add(run);

            RecommendationSettings SetDefaultSettings()
            {
                RecommendationSettings settings = new();
                settings.OutputPath = city.RecommendationSettings.OutputPath;
                settings.RootSeed = 5788925;
                settings.syncExperimentWithView = true;
                settings.IgnoreTieBreakers = false;
                settings.InitialMappingPercentage = 0.96;
                settings.ADCAttractConfig.MergingType = Document.DocumentMergingType.Intersection;
                return settings;
            }
        }

        private class UserStudyRun
        {
            public bool TestRun {  get; set; }

            public bool Automatic { get; set; }

            public RecommendationSettings Settings { get; set; }
        }

    }
}
