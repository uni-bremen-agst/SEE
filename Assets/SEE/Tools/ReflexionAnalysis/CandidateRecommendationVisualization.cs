using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateRecommendationVisualization : MonoBehaviour, IObserver<ChangeEvent>
    {
        private static float BLINK_EFFECT_DELAY = 0.1f;

        private Coroutine blinkEffectCoroutine;

        private ReflexionGraph reflexionGraphCalc;

        private Queue<ChangeEvent> changeEventQueue = new Queue<ChangeEvent>();

        private object calculationReflexionGraphLock = new object();

        private object visualizedReflexionGraphLock = new object();

        private bool ProcessingEvents { get; set; }
        
        private bool ProcessingData { get; set; }

        private bool Processing { get => ProcessingEvents || ProcessingData; }

        private const string updateConfigurationButtonLabel = "Update Configuration";
        private const string startAutomatedMappingLabel = "Start Automated Mapping";
        private const string runExperimentLabel = "Run Experiment";
        private const string showRecommendationLabel = "Show Recommendation";
        private const string startRecordingLabel = "Start Recording";
        private const string stopRecordingLabel = "Stop Recording";
        private const string processDataLabel = "Process Data";
        private const string dumbTrainingDataLabel = "Dumb Training Data";
        private const string resetMappingLabel = "Reset Mapping";
        private const string debugScenarioLabel = "Debug Scenario";
        private const string testOracleLabel = "Test Oracle";
        private const string generateOracleLabel = "Generate Oracle";

        private const string statisticButtonGroup = "statisticButtonsGroup";
        private const string mappingButtonGroup = "mappingButtonsGroup";
        private const string debugButtonGroup = "debugButtonGroup";

        // TODO: Resolve target language properly, when creating AttractFunctions
        private RecommendationSettings recommendationSettings;

        private string csvFileName = "output.csv";
        private string xmlFileName = "output.xml";

        public CandidateRecommendation CandidateRecommendation { get; private set; }

        private ReflexionGraph reflexionGraphViz;

        private Graph oracleMapping;

        /// <summary>
        /// 
        /// </summary>
        public bool OracleGraphLoaded { get => CandidateRecommendation.OracleGraphLoaded; }

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
            // UnityEngine.Debug.Log($"Received event in Visualization: {value.ToString()}");

            if (value is NodeEvent || value is EdgeEvent || value is HierarchyEvent)
            {
                changeEventQueue.Enqueue(value);
                UnityEngine.Debug.Log($"Received Change event {value}. Enqueued event.");
            }

            // TODO: How to solve event filtering in both classes, EventFilter class?
            if (!ProcessingEvents && value is EdgeEvent edgeEvent && edgeEvent.Affected == ReflexionSubgraphs.Mapping)
            {
                ProcessEvents().ContinueWith(() => TriggerBlinkAnimation().Forget());         
            }
        }

        public void SendEventToCalculationGraph(ChangeEvent value)
        {
            UnityEngine.Debug.Log($"Try to send Change event {value} to calculation reflexion graph");
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
                default: break;
            }
            
        }

        #endregion

        public async UniTaskVoid TriggerBlinkAnimation()
        {
            IEnumerable<MappingPair> recommendations = await this.GetRecommendations(CandidateRecommendation);
            await UniTask.SwitchToMainThread();

            List<NodeOperator> nodeOperators = new List<NodeOperator>();

            // TODO: Distinction between different clusters is required(different color intensity)
            foreach (MappingPair mappingPair in recommendations)
            {
                // sync node objects
                Node cluster = this.reflexionGraphViz.GetNode(mappingPair.ClusterID);
                Node candidate = this.reflexionGraphViz.GetNode(mappingPair.CandidateID);

                if(cluster != null && candidate != null)
                {
                    nodeOperators.Add(cluster.GameObject().AddOrGetComponent<NodeOperator>());
                    nodeOperators.Add(candidate.GameObject().AddOrGetComponent<NodeOperator>());
                }
            }

            if (!ProcessingEvents && nodeOperators.Count > 0)
            {
                // blink effect
                if (blinkEffectCoroutine != null)
                {
                    StopCoroutine(blinkEffectCoroutine);
                }
                blinkEffectCoroutine = StartCoroutine(StartBlinkEffect(nodeOperators));
            }
        }

        private async Task<IEnumerable<MappingPair>> GetRecommendations(CandidateRecommendation candidateRecommendation)
        {
            await UniTask.WaitWhile(() => ProcessingEvents);
            return await UniTask.RunOnThreadPool(() =>
            {
                lock (calculationReflexionGraphLock)
                {
                    return candidateRecommendation.GetRecommendations();
                }
            });
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
                        this.CandidateRecommendation.UpdateRecommendations();
                    }
                }
                ProcessingEvents = false;
            }); 
        }

        #region Buttons

        [Button(generateOracleLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void GenerateOracleMapping()
        {            
            GameObject codeCityObject = SceneQueries.GetCodeCity(this.transform)?.gameObject;
            if (codeCityObject == null)
            {
                throw new Exception("Could not get Reflexion city when loading oracle instructions.");
            }

            codeCityObject.TryGetComponent(out AbstractSEECity city);

            string configPath = Path.GetDirectoryName(city.ConfigurationPath.Path);

            string instructions = Path.Combine(configPath, "oracleInstructions.txt");

            (Graph implementation, _, _) = this.reflexionGraphViz.Disassemble();
            ReflexionGraph oracleGraph = CandidateRecommendation.GenerateOracleMapping(implementation, instructions);
            (_, Graph architecture, Graph mapping) = oracleGraph.Disassemble();
            string architectureGxl = Path.Combine(configPath, "Architecture.gxl");
            string oracleMappingGxl = Path.Combine(configPath, "OracleMapping.gxl");
            GraphWriter.Save(oracleMappingGxl, mapping, AbstractSEECity.HierarchicalEdgeTypes().First());
            GraphWriter.Save(architectureGxl, architecture, AbstractSEECity.HierarchicalEdgeTypes().First());
            UnityEngine.Debug.Log($"Saved oracle mapping to {oracleMappingGxl}");
            UnityEngine.Debug.Log($"Saved architecture to {architectureGxl}");
        }

        [Button(startRecordingLabel,ButtonSizes.Small)]
        [ButtonGroup(statisticButtonGroup)]
        public void StartRecording()
        {
            string csvFile = Path.Combine(this.recommendationSettings.OutputPath.Path, csvFileName);
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
            string csvFile = Path.Combine(this.recommendationSettings.OutputPath.Path, csvFileName);
            string xmlFile = Path.Combine(this.recommendationSettings.OutputPath.Path, xmlFileName);
            CandidateRecommendation.Statistics.StopRecording();
            CandidateRecommendation.Statistics.ProcessMappingData(csvFile, xmlFile);
        }

        public async void UpdateConfiguration(ReflexionGraph visualizedGraph, 
                                        RecommendationSettings recommendationSettings,
                                        Graph oracleMapping = null)
        {
            await UniTask.WaitWhile(() => Processing);
            ProcessingEvents = false;
            ProcessingData = false;
            changeEventQueue.Clear();

            if (oracleMapping != null)
            {
                this.oracleMapping = oracleMapping;
            }

            if (CandidateRecommendation == null)
            {
                CandidateRecommendation = new CandidateRecommendation();
            }

            if (recommendationSettings == null)
            {
                throw new Exception("Given Recommendation Settings were null.");
            }
           
            // These calls are triggering rerunning of the reflexion analysis 
            // within the reflexion graph and the oracle graph. During the analysis
            // we will exclude any parallel writes through processing events
            // or assignments of recommendations towards the graphs
            lock (calculationReflexionGraphLock)
            {
                lock (visualizedReflexionGraphLock)
                {
                    if (visualizedGraph != null)
                    {
                        this.reflexionGraphViz = visualizedGraph;
                    }
                    else
                    {
                        throw new Exception("Given Reflexion Graph was null. Cannot update Candidate Recommendation configuration.");
                    }

                    reflexionGraphCalc = new ReflexionGraph(reflexionGraphViz);
                }

                reflexionGraphCalc.Name = "reflexionGraph for Recommendations";
                CandidateRecommendation.UpdateConfiguration(reflexionGraphCalc, 
                                                            recommendationSettings,
                                                            oracleMapping);
                TriggerBlinkAnimation().Forget();
            }
        }

        [Button(updateConfigurationButtonLabel, ButtonSizes.Small)]
        public void UpdateConfigurationCommand()
        {
            UpdateConfiguration(this.reflexionGraphViz, this.recommendationSettings, this.oracleMapping);
        }

        // TODO: Move into other class? CandidateRecommendation or ReflexionGraph class?
        public async UniTask CreateInitialMapping(RecommendationSettings recommendationSettings)
        {
            // TODO: change datatype to mapping pair list
            Dictionary<Node, HashSet<Node>> initialMapping;
            UnityEngine.Debug.Log($"Create Initial mapping with seed {recommendationSettings.InitialMappingPercentage}");
            lock (visualizedReflexionGraphLock)
            {
                 initialMapping = this.CandidateRecommendation.CreateInitialMapping(recommendationSettings.InitialMappingPercentage,
                                                                               recommendationSettings.MasterSeed,
                                                                               reflexionGraphViz);

            }
            foreach (Node cluster in initialMapping.Keys)
            {
                foreach (Node candidate in initialMapping[cluster])
                {
                    Debug.Log($"Artificial initial mapping: {candidate.ID} -mapped to-> {cluster.ID}");
                    MapRecommendation(candidate, cluster).Forget();
                }
            }
        }

        [Button(resetMappingLabel, ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        // TODO: Move into other class? CandidateRecommendation or ReflexionGraph class?
        public async UniTask ResetMappingAsync()
        {
            // TODO: Implement this within the reflexion graph itself?
            await UniTask.RunOnThreadPool(() =>
            {
                lock (visualizedReflexionGraphLock)
                {
                    reflexionGraphViz?.ResetMapping();
                }
            });
        }

        [Button(dumbTrainingDataLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void DumpTrainingData()
        {
            Debug.Log(CandidateRecommendation.AttractFunction.DumpTrainingData());
        }

        [Button(startAutomatedMappingLabel,ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        public void StartAutomatedMappingCommand()
        {
            StartAutomatedMappingViz().Forget();
        }

        [Button(runExperimentLabel, ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        public async void RunMappingExperiment()
        {
            ReflexionGraph graph = reflexionGraphViz;

            lock (visualizedReflexionGraphLock)
            {
                graph = new ReflexionGraph(reflexionGraphViz);
            }
            graph.Name = "reflexionGraph for Experiment";


            await UniTask.RunOnThreadPool(() =>
            {
                RunExperiment(this.recommendationSettings, graph, oracleMapping);
            });
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
            // Empty method for debugging
        }

        [Button(testOracleLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void TestOracle()
        {
            ReflexionGraph graph = CandidateRecommendation.ReflexionGraph;
            ReflexionGraph oracleGraph = CandidateRecommendation.OracleGraph;

            if(oracleGraph == null)
            {
                UnityEngine.Debug.Log("Could not generate Oracle mapping test. No Oracle Graph loaded.");
                return;
            }

            StringBuilder sb = new StringBuilder();

            IEnumerable<Node> nodes = graph.Nodes().Where(n => n.IsInImplementation() 
                                     && n.Type.Equals(this.CandidateRecommendation.AttractFunction.CandidateType)
                                     && !n.ToggleAttributes.Contains("Element.Is_Artificial")
                                     && !n.ToggleAttributes.Contains("Element.Is_Anonymous"));

            foreach(Node node in nodes)
            {        
                sb.Append(node.ID.PadRight(120));
                sb.Append(" Maps To ".PadRight(20));
                Node oracleCluster = oracleGraph.MapsTo(node);
                string oracleClusterID = oracleCluster != null ? oracleCluster.ID : "UNKNOWN";
                sb.Append(oracleClusterID.PadRight(20));
                string isArtificial = $"Element.Is_Artificial:{node.ToggleAttributes.Contains("Element.Is_Artificial")}";
                sb.Append(isArtificial.PadRight(35));
                string isAnonymous = $"Element.Is_Anonymous:{node.ToggleAttributes.Contains("Element.Is_Anonymous")}";
                sb.Append(isAnonymous.PadRight(35));
                sb.Append(Environment.NewLine);
            }

            string output = sb.ToString();
            string outputFile = Path.Combine(this.recommendationSettings.OutputPath.Path, "oracle.txt");

            File.WriteAllText(outputFile, output);
        }

        public static void RunExperiment(RecommendationSettings config, 
                                         ReflexionGraph graph, 
                                         Graph oracleMapping)
        {
            Debug.Log($"Start Experiment for graph {graph.Name}({graph.Nodes().Count} nodes, {graph.Edges().Count} edges)");

            System.Random rand = new System.Random(config.MasterSeed);
            int currentSeed = rand.Next(int.MaxValue);

            double initialMappingPercentage = config.InitialMappingPercentage;

            List<MappingExperimentResult> results = new List<MappingExperimentResult>();

            graph.ResetMapping();

            CandidateRecommendation recommendations = new CandidateRecommendation();
            recommendations.UpdateConfiguration(graph, config, oracleMapping);

            FileStream stream;

            for (int i = 0; i < config.Iterations; ++i)
            {
                // STEPS
                // 1. Create initial mapping based on current seed
                Dictionary<Node, HashSet<Node>> initialMapping = recommendations.CreateInitialMapping(initialMappingPercentage, currentSeed);
                                                                                                     
                UnityEngine.Debug.Log($"Experiment run={i} initialMapping keys={initialMapping.Keys.Count} values={initialMapping.Values.Count}");
                
                // TODO:
                // case 1.1 create initial mapping for calc graph
                if (true)
                {
                    foreach (Node cluster in initialMapping.Keys)
                    {
                        foreach (Node candidate in initialMapping[cluster])
                        {
                            graph.StartCaching();
                            graph.AddToMapping(candidate, cluster);
                            graph.ReleaseCaching();
                        }
                    }
                }
                // case 1.2 create initial mapping to viz graph
                else
                {
                    // TODO:
                }

                string trainingData = recommendations.AttractFunction.DumpTrainingData();
                UnityEngine.Debug.Log(trainingData);
                string trainingDataFile = Path.Combine(config.OutputPath.Path, $"trainingData_{currentSeed}.txt");
                File.WriteAllText(trainingDataFile, trainingData);

                // 2. Generate csv file based on seed name/output path
                string csvFile = Path.Combine(config.OutputPath.Path, $"output{currentSeed}.csv");

                recommendations.UpdateRecommendations();

                // 3. Start Recording with csv file
                recommendations.Statistics.StartRecording(csvFile);

                // return;

                // 4. Start automated mapping
                // TODO:
                // case 4.1 sync with viz
                // case 4.2 sync not with viz
                StartAutomatedMapping(recommendations, graph);

                // 5. Stop Recording
                recommendations.Statistics.StopRecording();

                // 6. Process Data and keep result object
                MappingExperimentResult result = recommendations.Statistics.ProcessMappingData(csvFile);
                result.Seed = currentSeed;
                results.Add(result);
                string xmlFile = Path.Combine(config.OutputPath.Path, $"output{currentSeed}.xml");
                stream = new FileStream(xmlFile, FileMode.Create);
                result.CreateXml().Save(stream);
                stream.Close();
                Debug.Log($"Saved Result of Run to {xmlFile}");

                // 6. Delete csv File?
                // TODO:
                // case 6.1 keep csv File
                // case 6.3 keep csv File

                // TODO:
                // 7. ResetMapping
                // case 7.1 sync with viz
                // case 7.2 sync not with viz
                // graph.StartCaching();
                graph.ResetMapping(true);
                // graph.ReleaseCaching();

                UnityEngine.Debug.Log(recommendations.AttractFunction.DumpTrainingData());

                if (!recommendations.AttractFunction.EmptyTrainingData())
                {
                    foreach(string handledCandidate in recommendations.AttractFunction.HandledCandidates)
                    {
                        UnityEngine.Debug.Log($"Handled candidate: {handledCandidate}");
                    }
                    throw new Exception("Training Data was not resetted correctly after resetting mapping during experiment.");
                }

                if(recommendations.AttractFunction.HandledCandidates.Count != 0)
                {
                    throw new Exception("Handled candidates left despite mapping was resetted.");
                }

                // 8. Create next seed
                currentSeed = rand.Next(int.MaxValue);
            }

            // 9. save Average Results
            MappingExperimentResult averageResult = MappingExperimentResult.AverageResults(results, config);
            string resultXml = Path.Combine(config.OutputPath.Path, $"result.xml");
            stream = new FileStream(resultXml, FileMode.Create);
            averageResult.CreateXml().Save(stream);
            stream.Close();
            UnityEngine.Debug.Log($"Finished Experiment for seed {config.MasterSeed}. Saved averaged result to {resultXml}");
        }

        /// <summary>
        /// 
        /// </summary>
        public async UniTaskVoid StartAutomatedMappingViz()
        {
            IEnumerable<MappingPair> recommendations = await this.GetRecommendations(CandidateRecommendation);
            
            // While next recommendation still exists      
            while (recommendations.Count() != 0)
            {
                MappingPair chosenMappingPair;

                // TODO: Wrap recommendations within own class?
                if(recommendations.Count() == 1)
                {
                    chosenMappingPair = recommendations.FirstOrDefault();
                    // Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}");
                } 
                else
                {
                    // TODO: Handle ambigous mapping steps
                    Debug.Log("Warning: Ambigous recommendations.");
                    chosenMappingPair = recommendations.FirstOrDefault();
                }
                
                Debug.Log($"Chosen Mapping Pair {chosenMappingPair.CandidateID} --> {chosenMappingPair.CandidateID}");

                await MapRecommendation(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
                recommendations = await this.GetRecommendations(CandidateRecommendation);
            }
            Debug.Log("Automatic Mapping stopped.");
        }

        public static void StartAutomatedMapping(CandidateRecommendation recommendation, ReflexionGraph graph)
        {
            IEnumerable<MappingPair> recommendations = recommendation.GetRecommendations();

            // While next recommendation still exists      
            while (recommendations.Count() != 0)
            {
                MappingPair chosenMappingPair;

                if (recommendations.Count() == 1)
                {
                    // Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}");
                    chosenMappingPair = recommendations.FirstOrDefault();
                }
                else
                {
                    // TODO: Handle ambigous mapping steps
                    // Debug.Log("Warning: Ambigous recommendation.");
                    chosenMappingPair = recommendations.FirstOrDefault();
                }

                // Debug.Log($"Chosen Mapping Pair {chosenMappingPair.CandidateID} --> {chosenMappingPair.CandidateID}");

                //await MapRecommendation(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
                Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}. candidates left: {recommendation.UnmappedCandidates.Count}");
                graph.StartCaching();
                graph.AddToMapping(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
                graph.ReleaseCaching();

                recommendations = recommendation.GetRecommendations();
            }
        }

        #endregion

        private IEnumerator StartBlinkEffect(List<NodeOperator> nodeOperators)
        {
            // Wait for the delay duration
            yield return new WaitForSeconds(BLINK_EFFECT_DELAY);

            // Start blink effect
            try
            {
                nodeOperators.ForEach((n) => n.Blink(10, 2));
            }
            catch (Exception e)
            {
                Debug.LogError("Exception occured during blink operation." + Environment.NewLine + e.ToString());
            }
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
