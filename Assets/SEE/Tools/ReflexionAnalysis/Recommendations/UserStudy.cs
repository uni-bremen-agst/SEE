using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static SEE.Utils.Paths.DataPath;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class UserStudy : MonoBehaviour
    {
        private bool iterationFinished;

        private SEEReflexionCity city;

        private RecommendationsViz candidateRecommendation;

        private IList<UserStudyRun> userStudyRuns = new List<UserStudyRun>();

        private UserStudyRun currentRun;

        private string BuildGroup = "A";

        private string pathInStreamingAssets = @"/reflexion/SpringPetClinicFramework";

        public async UniTask StartStudy(SEEReflexionCity city, RecommendationsViz candidateRecommendation)
        {
            this.city = city;
            this.candidateRecommendation = candidateRecommendation;

            SetupRuns();

            foreach(UserStudyRun run in userStudyRuns)
            {
                currentRun = run;
                await NextCity(run);

                this.StartNextIteration();

                candidateRecommendation.StartRecording();

                await this.WaitForParticipant();

                UnityEngine.Debug.Log("Calculate results...");
                candidateRecommendation.CalculateResults();

                UnityEngine.Debug.Log("Finished Iteration.");
            }

            async UniTask NextCity(UserStudyRun run)
            {
                try
                {
                    UnityEngine.Debug.Log($"Load City for group {run.group}");
                    city.Reset();
                    city.ConfigurationPath.RelativePath = Path.Combine(pathInStreamingAssets + run.group, "Reflexion.cfg");
                    city.ConfigurationPath.Root = RootKind.StreamingAssets;

                    UnityEngine.Debug.Log($"Configuration path of run {city.ConfigurationPath.ToString()}");
                    city.LoadConfiguration();
                    await city.LoadDataAsync();
                    
                    await city.UpdateRecommendationSettings(run.Settings);
                    await UniTask.Delay(500);
                    city.ClearGraphElementIDMap();
                    await UniTask.Delay(500);
                    city.DrawGraph();
                    await UniTask.Delay(500);
                    city.LoadLayout();

                    //Delete root labels
                    (Graph implementation, Graph architecture, _) = city.ReflexionGraph.Disassemble();
                    Node ArchitectureRoot = architecture.GetRoots().FirstOrDefault();
                    Node ImplementationRoot = implementation.GetRoots().FirstOrDefault();
                    DisableLabel(ArchitectureRoot);
                    DisableLabel(ImplementationRoot);

                    MoveAction.UnblockMovement();
                    BlockUnnecessaryMovement();
                    candidateRecommendation.ColorUnmappedCandidates(Color.blue);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }

            void DisableLabel(Node node)
            {
                GameObject rootGameObject = node.GameObject();
                for (int i = 0; i < rootGameObject.transform.childCount; i++)
                {
                    Transform child = rootGameObject.transform.GetChild(i);

                    if (child.transform.tag.Equals("Text") && child.name.StartsWith("Text"))
                    {
                        UnityEngine.Debug.Log($"Found text tranform {child.name}");
                        child.gameObject.SetActive(false);
                    }
                }
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
            if (SEEInput.FinishStudyIteration())
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

            UserStudyRun run1 = new();
            run1.Settings = SetDefaultSettings();
            run1.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.ADCAttract;
            run1.Settings.ExperimentName = "ADCAttract";
            run1.group = "A";


            UserStudyRun run2 = new();
            run2.Settings = SetDefaultSettings();
            run2.Settings.AttractFunctionType = AttractFunction.AttractFunctionType.NoAttract;
            run2.Settings.ExperimentName = "NoAttract";
            run2.group = "B";

            if(BuildGroup.Equals("A"))
            {   
                //AB
                userStudyRuns.Add(run1);
                userStudyRuns.Add(run2);
            } 
            else
            {   
                //BA
                userStudyRuns.Add(run2);
                userStudyRuns.Add(run1);
            }

            RecommendationSettings SetDefaultSettings()
            {
                RecommendationSettings settings = new();
                settings.OutputPath = city.RecommendationSettings.OutputPath;
                settings.rootSeed = 5788925;
                settings.moveNodes = true;
                settings.IgnoreTieBreakers = false;
                settings.initialMappingPercentage = 0.80f;
                settings.ADCAttractConfig.MergingType = Document.DocumentMergingType.Intersection;
                return settings;
            }
        }

        private class UserStudyRun
        {
            public bool TestRun {  get; set; }

            public RecommendationSettings Settings { get; set; }

            public string group;
        }

    }
}
