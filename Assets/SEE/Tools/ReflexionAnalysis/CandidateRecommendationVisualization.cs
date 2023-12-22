using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Window;
using SEE.UI.Window.TreeWindow;
using SEE.Utils;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;
using static Assets.SEE.Tools.ReflexionAnalysis.CandidateRecommendation;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateRecommendationVisualization : MonoBehaviour, IObserver<ChangeEvent>
    {
        /// <summary>
        /// 
        /// </summary>
        private CandidateRecommendation candidateRecommendation;

        private static float BLINK_EFFECT_DELAY = 0.1f;

        private Coroutine blinkEffectCoroutine;

        private ReflexionGraph reflexionGraph;

        private Graph oracleMapping;

        private Queue<ChangeEvent> changeEventQueue = new Queue<ChangeEvent>();

        private object reflexionGraphLockObject = new object();

        private bool ProcessingEvents { get; set; }

        public CandidateRecommendation CandidateRecommendation
        {
            get 
            {
                if (candidateRecommendation == null)
                {
                    candidateRecommendation = new CandidateRecommendation();
                    candidateRecommendation.Statistics.CsvPath = csvPath; 
                }
                return candidateRecommendation; 
            }
        }

        private const string updateConfigurationButtonLabel = "Update configuration";
        private const string createInitialMappingLabel = "Create initial Mapping";
        private const string startAutomatedMappingLabel = "Start automated mapping";
        private const string showRecommendationLabel = "Show recommendation";
        private const string startRecordingLabel = "Start recording";
        private const string stopRecordingLabel = "Stop recording";
        private const string processDataLabel = "Process data";
        private const string dumbTrainingDataLabel = "Dumb training data";

        private const string statisticButtonGroup = "statisticButtonsGroup";
        private const string mappingButtonGroup = "mappingButtonsGroups";

        // TODO: include cda option into the updating of the configuration
        [SerializeField]
        public bool useCDA;

        [SerializeField]
        public AttractFunctionType attractFunctionType = AttractFunctionType.CountAttract;

        [SerializeField]
        public string candidateType = "Class";

        [SerializeField]
        public FilePath csvPath;

        [SerializeField]
        public FilePath xmlPath;

        [SerializeField]
        public double initialMappingPercentage = 0.5;

        [SerializeField]
        public int initialMappingSeed = 593946;

        public ReflexionGraph ReflexionGraph
        {
            set 
            {
                reflexionGraph = value;
            }
        }

        public Graph OracleMapping
        {
            set
            {
                oracleMapping = value;
            }
            get
            {
                return CandidateRecommendation.Statistics.OracleGraph;
            }
        }

        #region IObserver
        public void OnCompleted()
        {
            CandidateRecommendation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            CandidateRecommendation.OnError(error);
        }

        public void OnNext(ChangeEvent value)
        {
            changeEventQueue.Enqueue(value);

            // TODO: How to solve event filtering in both classes, EventFilter class?
            if (!ProcessingEvents && value is EdgeEvent edgeEvent && edgeEvent.Affected == ReflexionSubgraphs.Mapping)
            {
                ProcessEvents().Forget();
                TriggerBlinkAnimation().Forget();
            }
        }
        #endregion

        public async UniTaskVoid TriggerBlinkAnimation()
        {
            Dictionary<Node, HashSet<MappingPair>> recommendations = await this.GetRecommendations();
            await UniTask.SwitchToMainThread();
            List<NodeOperator> nodeOperators = new List<NodeOperator>();
            foreach (Node cluster in recommendations.Keys)
            {
                NodeOperator nodeOperator;

                nodeOperator = cluster.GameObject().AddOrGetComponent<NodeOperator>();
                nodeOperators.Add(nodeOperator);

                foreach (MappingPair mappingPair in CandidateRecommendation.Recommendations[cluster])
                {
                    nodeOperators.Add(mappingPair.Candidate.GameObject().AddOrGetComponent<NodeOperator>());
                }
            }

            if (!ProcessingEvents)
            {
                // blink effect
                // TODO: Distinction between different hypothesized entities is required
                if (blinkEffectCoroutine != null) StopCoroutine(blinkEffectCoroutine);
                blinkEffectCoroutine = StartCoroutine(StartBlinkEffect(nodeOperators));
            }
        } 

        public async UniTask ProcessEvents()
        {
            ProcessingEvents = true;
            await UniTask.RunOnThreadPool(() =>
            {
                lock (reflexionGraphLockObject)
                {
                    while (changeEventQueue.Count > 0)
                    {
                        CandidateRecommendation.OnNext(changeEventQueue.Dequeue());
                    }
                }
                ProcessingEvents = false;
            }); 
        }

        #region Buttons
        [Button(startRecordingLabel,ButtonSizes.Small)]
        [ButtonGroup(statisticButtonGroup)]
        public void StartRecording()
        {
            CandidateRecommendation.Statistics.StartRecording();
        }

        [Button(stopRecordingLabel, ButtonSizes.Small)]
        [ButtonGroup(statisticButtonGroup)]
        public void StopRecording()
        {
            CandidateRecommendation.Statistics.StopRecording();
        }

        [Button(processDataLabel,ButtonSizes.Small)]
        [ButtonGroup(statisticButtonGroup)]
        public void ProcessData()
        {
            CandidateRecommendation.Statistics.StopRecording();
            CandidateRecommendation.Statistics.ProcessMappingData(csvPath.Path, xmlPath.Path);
        }

        public void UpdateConfiguration()
        {
            // These calls are triggering rerunning of the reflexion analysis 
            // within the reflexion graph and the oracle graph. During the analysis
            // we will exclude any parallel writes through processing events
            // or assignments of recommendations towards the graphs
            lock (reflexionGraphLockObject)
            {
                CandidateRecommendation.UpdateConfiguration(reflexionGraph, attractFunctionType, candidateType);
                CandidateRecommendation.Statistics.SetOracleMapping(oracleMapping);
            }
        }
       
        public async UniTask UpdateConfigurationAsync()
        {
            await UniTask.RunOnThreadPool(async () => 
            {
            	await UniTask.WaitWhile(() => ProcessingEvents);
                await UniTask.SwitchToMainThread();
                UpdateConfiguration();
            });
        }

        [Button(updateConfigurationButtonLabel, ButtonSizes.Small)]
        public void UpdateConfigurationCommand()
        {
            UpdateConfigurationAsync().Forget();
        }

        [Button(createInitialMappingLabel,ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        public void CreateInitialMapping()
        {
            Dictionary<Node, HashSet<Node>> initialMapping = CandidateRecommendation.Statistics.CreateInitialMapping(
                                                                                                initialMappingPercentage, 
                                                                                                initialMappingSeed);
            foreach (Node cluster in initialMapping.Keys)
            {
                foreach (Node candidate in initialMapping[cluster])
                {
                    Debug.Log($"Artificial initial mapping {candidate.ID} --> {cluster.ID}");
                    MapRecommendation(candidate, cluster).Forget();
                }
            }
        }

        [Button(dumbTrainingDataLabel, ButtonSizes.Small)]
        public void DumpTrainingData()
        {
            Debug.Log(CandidateRecommendation.AttractFunction.DumpTrainingData());
        }

        public async UniTask<Dictionary<Node, HashSet<MappingPair>>> GetRecommendations() 
        {
            await UniTask.WaitWhile(() => ProcessingEvents);
            return new Dictionary<Node, HashSet<MappingPair>>(CandidateRecommendation.Recommendations);
        }

        [Button(startAutomatedMappingLabel,ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        public void StartAutomatedMappingCommand()
        {
            StartAutomatedMapping().Forget();
        }

        [Button(showRecommendationLabel, ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        public void ShowRecommendation()
        {
            TriggerBlinkAnimation().Forget();
        }

        /// <summary>
        /// 
        /// </summary>
        public async UniTaskVoid StartAutomatedMapping()
        {
            Dictionary<Node, HashSet<MappingPair>> recommendations = await this.GetRecommendations();
            
            // While next recommendation still exists      
            while (recommendations.Count != 0)
            {
                MappingPair chosenMappingPair;
                // TODO: wrap recommendations within own class?
                if(CandidateRecommendation.IsRecommendationDefinite())
                {
                    chosenMappingPair = CandidateRecommendation.GetDefiniteRecommendation();
                    Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}");
                } 
                else
                {
                    chosenMappingPair = CandidateRecommendation.Recommendations[CandidateRecommendation.Recommendations.Keys.First<Node>()].FirstOrDefault<MappingPair>();

                    // TODO: handle ambigous mapping steps
                    Debug.Log("Warning: Ambigous recommendation.");
                }
                
                Debug.Log($"Chosen Mapping Pair {chosenMappingPair.CandidateID} --> {chosenMappingPair.CandidateID}");
                MapRecommendation(chosenMappingPair.Candidate, chosenMappingPair.Cluster).Forget();

                recommendations = await this.GetRecommendations();
            }
            Debug.Log("Automatic Mapping stopped.");
        }
        #endregion

        private IEnumerator StartBlinkEffect(List<NodeOperator> nodeOperators)
        {
            // Wait for the delay duration
            yield return new WaitForSeconds(BLINK_EFFECT_DELAY);

            // Start blink effect
            nodeOperators.ForEach((n) => n.Blink(10, 2));
        }

        private async UniTask MapRecommendation(Node candidate, Node cluster)
        {
            await UniTask.WaitWhile(() => ProcessingEvents);
            await UniTask.SwitchToMainThread();
            lock (reflexionGraphLockObject)
            {
                // TODO: Wrap automatic mapping in action?
                // TODO: Implement as action to visualize mapping/ Trigger Animation.
                CandidateRecommendation.ReflexionGraph.AddToMapping(candidate, cluster); 
            }
        }
    }
}
