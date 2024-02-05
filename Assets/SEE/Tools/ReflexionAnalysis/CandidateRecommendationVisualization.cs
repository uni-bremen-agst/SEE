using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        private ReflexionGraph reflexionGraphViz;

        private ReflexionGraph reflexionGraphCalc;

        private Graph oracleMapping;

        private Queue<ChangeEvent> changeEventQueue = new Queue<ChangeEvent>();

        private object calculationReflexionGraphLock = new object();

        private object visualizedReflexionGraphLock = new object();

        private bool ProcessingEvents { get; set; }

        private const string updateConfigurationButtonLabel = "Update Configuration";
        private const string createInitialMappingLabel = "Create Initial Mapping";
        private const string startAutomatedMappingLabel = "Start Automated Mapping";
        private const string showRecommendationLabel = "Show Recommendation";
        private const string startRecordingLabel = "Start Recording";
        private const string stopRecordingLabel = "Stop Recording";
        private const string processDataLabel = "Process Data";
        private const string dumbTrainingDataLabel = "Dumb Training Data";
        private const string resetMappingLabel = "Reset Mapping";
        private const string debugScenarioLabel = "Debug Scenario";

        private const string statisticButtonGroup = "statisticButtonsGroup";
        private const string mappingButtonGroup = "mappingButtonsGroup";
        private const string debugButtonGroup = "debugScenarioButtonGroup";

        // TODO: Resolve target language properly, when creating AttractFunctions
        public MappingExperimentConfig mappingConfig;

        private string csvFileName = "output.csv";
        private string xmlFileName = "output.xml";

        public void Awake()
        {
            candidateRecommendation = new CandidateRecommendation();
        }

        public CandidateRecommendation CandidateRecommendation
        {
            get
            {
                return candidateRecommendation;
            }
        }

        public ReflexionGraph ReflexionGraphViz
        {
            set 
            {
                reflexionGraphViz = value;
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
            // UnityEngine.Debug.Log($"Received Change event {value}. Enqueued event.");

            // TODO: How to solve event filtering in both classes, EventFilter class?
            if (!ProcessingEvents && value is EdgeEvent edgeEvent && edgeEvent.Affected == ReflexionSubgraphs.Mapping)
            {
                // Debug.Log($"In Vizualization: Queued Changeevent in Mapping... {edgeEvent.ToString()} sender: {edgeEvent.Sender} to process...");
                ProcessEvents().Forget();
                TriggerBlinkAnimation().Forget();
            }
        }

        public void SendEventToCalculationGraph(ChangeEvent value)
        {
            // UnityEngine.Debug.Log($"Try to send Change event {value} to calculation reflexion graph");
            switch (value)
            {
                case NodeEvent nodeEvent:

                    if (value.Change == ChangeType.Addition)
                    {
                        // TODO: Cloning the node on a pool thread is not safe
                        Node nodeClone = (Node)nodeEvent.Node.Clone();
                        reflexionGraphCalc.AddNode(nodeClone);
                    } 
                    else if (value.Change == ChangeType.Removal)
                    {
                        Node nodeToRemove = reflexionGraphCalc.GetNode(nodeEvent.Node.ID);
                        if (nodeToRemove == null) throw new Exception($"Node {nodeEvent.Node.ID} not found in calculation reflexion graph" +
                                                                      $" when trying to synchronize with visualization reflexion graph.");
                        
                        // TODO: Add the orphans option to the ChangeEvent object to copy the change consistently
                        reflexionGraphCalc.RemoveNode(nodeToRemove);
                    }
                    break;
                case EdgeEvent edgeEvent:

                    if(value.Change == ChangeType.Addition)
                    {
                        if (edgeEvent.Affected == ReflexionSubgraphs.Mapping)
                        {
                            Node nodeSource = reflexionGraphCalc.GetNode(edgeEvent.Edge.Source.ID);
                            Node nodeTarget = reflexionGraphCalc.GetNode(edgeEvent.Edge.Target.ID);
                            reflexionGraphCalc.AddToMapping(nodeSource, nodeTarget);
                        }
                        else
                        {
                            // Filter for propagated edges, because
                            // the reflexion graph for calculation will add them by itself
                            if (!edgeEvent.Edge.IsInArchitecture() || ReflexionGraph.IsSpecified(edgeEvent.Edge))
                            {
                                // TODO: Cloning the edge on a pool thread is not safe
                                Edge edgeClone = (Edge)edgeEvent.Edge.Clone();
                                Node nodeSource = reflexionGraphCalc.GetNode(edgeEvent.Edge.Source.ID);
                                Node nodeTarget = reflexionGraphCalc.GetNode(edgeEvent.Edge.Target.ID);
                                edgeClone.Source = nodeSource;
                                edgeClone.Target = nodeTarget;
                                reflexionGraphCalc.AddEdge(edgeClone); 
                            }
                        }
                    }
                    // Do not remove propagated edges, because their ID is not transferable
                    // and they are not add in the first place
                    else if(value.Change == ChangeType.Removal 
                        && (!edgeEvent.Edge.IsInArchitecture() || ReflexionGraph.IsSpecified(edgeEvent.Edge)))
                    {
                        Edge edgeToRemove = reflexionGraphCalc.GetEdge(edgeEvent.Edge.ID);
                        if (edgeToRemove == null) throw new Exception($"Edge {edgeEvent.Edge.ID} not found in calculation reflexion graph" +
                                                                      $" when trying to synchronize with visualization reflexion graph.");
                        reflexionGraphCalc.RemoveEdge(edgeToRemove);
                    }
                    break;
                case HierarchyEvent e:
                // TODO Handling of Hierarchy event necessary? Yes, this needs to be handled.
                    break;
                case AttributeEvent<int> e:
                // TODO Handling of Attribute events necessary?
                    break;
                default: break;
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

                foreach (MappingPair mappingPair in recommendations[cluster])
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
                lock (calculationReflexionGraphLock)
                {
                    while (changeEventQueue.Count > 0)
                    {
                        reflexionGraphCalc.StartCaching();
                        SendEventToCalculationGraph(changeEventQueue.Dequeue());
                        reflexionGraphCalc.ReleaseCaching();
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
            string csvFile = Path.Combine(this.mappingConfig.OutputPath.Path, csvFileName);
            CandidateRecommendation.Statistics.StartRecording(csvFile);
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
            string csvFile = Path.Combine(this.mappingConfig.OutputPath.Path, csvFileName);
            string xmlFile = Path.Combine(this.mappingConfig.OutputPath.Path, xmlFileName);
            CandidateRecommendation.Statistics.StopRecording();
            CandidateRecommendation.Statistics.ProcessMappingData(csvFile, xmlFile);
        }

        public void UpdateConfiguration()
        {
                lock (visualizedReflexionGraphLock)
                {
                    reflexionGraphCalc = new ReflexionGraph(reflexionGraphViz);
                }
                reflexionGraphCalc.Name = "reflexionGraph for Recommendations";
                
                // These calls are triggering rerunning of the reflexion analysis 
                // within the reflexion graph and the oracle graph. During the analysis
                // we will exclude any parallel writes through processing events
                // or assignments of recommendations towards the graphs
                lock (calculationReflexionGraphLock)
                {
                    UnityEngine.Debug.Log($"Update Configuration called! {oracleMapping}");
                    CandidateRecommendation.UpdateConfiguration(reflexionGraphCalc, 
                                                                mappingConfig,
                                                                oracleMapping);
                    
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
        // TODO: Move into other class? CandidateRecommendation or ReflexionGraph class?
        public void CreateInitialMapping()
        {
            Dictionary<Node, HashSet<Node>> initialMapping;
            UnityEngine.Debug.Log($"Create Initial mapping with seed {mappingConfig.InitialMappingPercentage}");
            lock (visualizedReflexionGraphLock)
            {
                 initialMapping = CandidateRecommendation.Statistics.CreateInitialMapping(mappingConfig.InitialMappingPercentage,
                                                                                          mappingConfig.MasterSeed,
                                                                                          reflexionGraphViz); 
            }
            foreach (Node cluster in initialMapping.Keys)
            {
                foreach (Node candidate in initialMapping[cluster])
                {
                    // Debug.Log($"Artificial initial mapping {candidate.ID} --> {cluster.ID}");
                    MapRecommendation(candidate, cluster).Forget();
                }
            }
        }

        [Button(resetMappingLabel, ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        // TODO: Move into other class? CandidateRecommendation or ReflexionGraph class?
        public void ResetMapping()
        {
            // TODO: Implement this within the reflexion graph itself?
            // TODO: Make this call async
            lock (visualizedReflexionGraphLock)
            {
                List<Node> nodes = reflexionGraphViz.Nodes().Where(n => n.IsInImplementation() && reflexionGraphViz.MapsTo(n) != null).ToList();
                nodes.ForEach(n => reflexionGraphViz.RemoveFromMapping(n)); 
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
            Dictionary<Node, HashSet<MappingPair>> RecommendationsVisualized = new Dictionary<Node, HashSet<MappingPair>>();
            lock (visualizedReflexionGraphLock)
            {
                foreach (Node key in CandidateRecommendation.Recommendations.Keys)
                {
                    HashSet<MappingPair> visualizedMappingPairs = new HashSet<MappingPair>();
                    Node keyInViz = reflexionGraphViz.GetNode(key.ID);
                    RecommendationsVisualized.Add(keyInViz, visualizedMappingPairs);
                    foreach (MappingPair mappingPair in CandidateRecommendation.Recommendations[key])
                    {
                        Node visualizedCandidate = reflexionGraphViz.GetNode(mappingPair.CandidateID);
                        Node visualizedCluster = reflexionGraphViz.GetNode(mappingPair.ClusterID);
                        if (visualizedCandidate == null || visualizedCluster == null)
                        {
                            Debug.LogWarning($"Couldn't map recommendation to visualized reflexion graph." +
                                $" {mappingPair.CandidateID} --> {visualizedCandidate?.ID} | {mappingPair.ClusterID} --> {visualizedCluster?.ID}");
                            continue;
                        }
                        MappingPair mappingPairVisualized = new MappingPair(visualizedCandidate,
                                                                            visualizedCluster,
                                                                            mappingPair.AttractionValue);
                        visualizedMappingPairs.Add(mappingPairVisualized);
                    }
                } 
            }
            return RecommendationsVisualized;
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

        [Button(debugScenarioLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void StartDebugScenario()
        {
            string nameNode1 = "minilax.c";
            string nameNode2 = "scanner.h";
            string clusterName1 = "Main";
            string clusterName2 = "FrontEnd";
            Node node1 = reflexionGraphViz.Nodes().Where(n => n.ID.Contains(nameNode1)).FirstOrDefault();
            Node node2 = reflexionGraphViz.Nodes().Where(n => n.ID.Contains(nameNode2)).FirstOrDefault();

            Node cluster1 = reflexionGraphViz.Nodes().Where(n => n.ID.Contains(clusterName1)).FirstOrDefault();
            Node cluster2 = reflexionGraphViz.Nodes().Where(n => n.ID.Contains(clusterName2)).FirstOrDefault();

            lock (calculationReflexionGraphLock)
            {
                reflexionGraphViz.AddToMapping(node1, cluster1);
            }

            lock (calculationReflexionGraphLock)
            {
                reflexionGraphViz.AddToMapping(node2, cluster2); 
            }

            foreach(Edge edge in reflexionGraphViz.Edges().Where(  e =>
                                                                   (e.Source.ID.Contains(nameNode2) || e.Target.ID.Contains(nameNode2))
                                                                && (e.Source.ID.Contains(nameNode1)  || e.Target.ID.Contains(nameNode1))  
                                                                && (e.State() == State.Allowed || e.State() == State.ImplicitlyAllowed)
                                                                && e.IsInImplementation()))
            {
                // UnityEngine.Debug.Log($"After Analysis: Edge {edge.Source.ID} --> {edge.Target.ID} is in State.(State: {edge.State()}, Graph: {edge.ItsGraph.Name})");
            }
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

                // TODO: Wrap recommendations within own class?
                if(CandidateRecommendation.IsRecommendationDefinite(recommendations))
                {
                    chosenMappingPair = CandidateRecommendation.GetDefiniteRecommendation(recommendations);
                    Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}");
                } 
                else
                {
                    chosenMappingPair = recommendations[recommendations.Keys.First<Node>()].FirstOrDefault<MappingPair>();

                    // TODO: Handle ambigous mapping steps
                    Debug.Log("Warning: Ambigous recommendation.");
                }
                
                Debug.Log($"Chosen Mapping Pair {chosenMappingPair.CandidateID} --> {chosenMappingPair.CandidateID}");

                await MapRecommendation(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
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
            lock (visualizedReflexionGraphLock)
            {
                // TODO: Wrap automatic mapping in action?
                // TODO: Implement as action to visualize mapping/ Trigger Animation.
                Debug.Log($"About to map: candidate {candidate.ID} in {candidate.ItsGraph.Name} Into cluster {cluster.ID} in {cluster.ItsGraph.Name}");
                reflexionGraphViz.AddToMapping(candidate, cluster); 
            }
        }
    }
}
