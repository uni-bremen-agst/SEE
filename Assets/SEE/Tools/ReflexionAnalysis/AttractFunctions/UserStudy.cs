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

        private string Group = "A";

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

                candidateRecommendation.StartRecording();

                await this.WaitForParticipant();

                UnityEngine.Debug.Log("Calculate results...");
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
                                                                        syncWithView: true,
                                                                        delay: 500);
                BlockUnnecessaryMovement();
                // candidateRecommendation.ColorUnmappedCandidates(Color.blue);
                candidateRecommendation.StartRecording();
            }

            void BlockUnnecessaryMovement()
            {
                candidateRecommendation.ReflexionGraphVisualized.Nodes().Where(n => !candidateRecommendation.IsCandidate(n.ID)).ForEach(n => MoveAction.BlockMovement(n.ID));
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
            UserStudyRun run1 = new();
            run1.Settings = SetDefaultSettings();
            run1.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.NBAttract;
            run1.Settings.ExperimentName = "NBAttract";

            UserStudyRun run2 = new();
            run2.Settings = SetDefaultSettings();
            run2.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.NoAttract;
            run2.Settings.ExperimentName = "NoAttract";
            run2.Settings.RootSeed = 239637258;

            if(Group.Equals("A"))
            {
                userStudyRuns.Add(run1);
                userStudyRuns.Add(run2);
            } 
            else
            {
                userStudyRuns.Add(run2);
                userStudyRuns.Add(run1);
            }

            RecommendationSettings SetDefaultSettings()
            {
                RecommendationSettings settings = new();
                settings.OutputPath = city.RecommendationSettings.OutputPath;
                settings.RootSeed = 5788925;
                settings.syncExperimentWithView = true;
                settings.IgnoreTieBreakers = false;
                settings.InitialMappingPercentage = 0.80;
                settings.ADCAttractConfig.MergingType = Document.DocumentMergingType.Intersection;
                return settings;
            }
        }

        private class UserStudyRun
        {
            public bool TestRun {  get; set; }

            public RecommendationSettings Settings { get; set; }
        }

    }
}
