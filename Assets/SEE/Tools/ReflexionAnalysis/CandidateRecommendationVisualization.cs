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
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private const string updateConfigurationButtonLabel = "Update Configuration";
        private const string createInitialMappingLabel = "Create Initial Mapping";
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
        public MappingExperimentConfig mappingConfig;

        private string csvFileName = "output.csv";
        private string xmlFileName = "output.xml";

        public CandidateRecommendation CandidateRecommendation { get; private set; }

        private ReflexionGraph reflexionGraphViz;

        private Graph oracleMapping;

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
            if (value is NodeEvent || value is EdgeEvent || value is HierarchyEvent)
            {
                changeEventQueue.Enqueue(value);
                // UnityEngine.Debug.Log($"Received Change event {value}. Enqueued event.");
            }

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
                default: break;
            }
            
        }

        #endregion

        public async UniTaskVoid TriggerBlinkAnimation()
        {
            Dictionary<Node, HashSet<MappingPair>> recommendations = await this.SyncRecommendations(CandidateRecommendation);
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
                        this.CandidateRecommendation.UpdateRecommendations();
                    }
                }
                ProcessingEvents = false;
            }); 
        }

        #region Buttons

        [Button(generateOracleLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void GenerateOracleLabel()
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
            string csvFile = Path.Combine(this.mappingConfig.OutputPath.Path, csvFileName);
            // CandidateRecommendation.Statistics.AddConfigInformation(this.mappingConfig);
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

        public void UpdateConfiguration(ReflexionGraph visualizedGraph, Graph oracleMapping = null)
        {
            ProcessingEvents = false;
            changeEventQueue.Clear();

            if (visualizedGraph != null) 
            {
                this.reflexionGraphViz = visualizedGraph;     
            } 
            else
            {
                throw new Exception("Given Reflexion Graph was null. Cannot update Candidate Recommendation configuration.");
            }

            if(oracleMapping != null)
            {
                this.oracleMapping = oracleMapping;
            }

            if(CandidateRecommendation == null)
            {
                CandidateRecommendation = new CandidateRecommendation();
            }

            if(mappingConfig == null)
            {
                mappingConfig = new MappingExperimentConfig();
            }

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
       
        //public async UniTask UpdateConfigurationAsync()
        //{
        //    await UniTask.RunOnThreadPool(async () => 
        //    {
        //    	await UniTask.WaitWhile(() => ProcessingEvents);
        //        await UniTask.SwitchToMainThread();
        //        UpdateConfiguration(reflexionGraphViz, oracleMapping);
        //    });
        //}

        [Button(updateConfigurationButtonLabel, ButtonSizes.Small)]
        public void UpdateConfigurationCommand()
        {
            UpdateConfiguration(this.reflexionGraphViz, this.oracleMapping);
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
                 initialMapping = this.CandidateRecommendation.CreateInitialMapping(mappingConfig.InitialMappingPercentage,
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
                reflexionGraphViz.ResetMapping();
            }
        }

        [Button(dumbTrainingDataLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void DumpTrainingData()
        {
            Debug.Log(CandidateRecommendation.AttractFunction.DumpTrainingData());
        }

        /// <summary>
        /// Copies the recommendation of the given candidateRecommendation object and 
        /// creates clone corresponding to the visualizedGraph
        /// </summary>
        /// <param name="candidateRecommendation"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async UniTask<Dictionary<Node, HashSet<MappingPair>>> SyncRecommendations(CandidateRecommendation candidateRecommendation) 
        {
            await UniTask.WaitWhile(() => ProcessingEvents);
            Dictionary<Node, HashSet<MappingPair>> RecommendationsVisualized = new Dictionary<Node, HashSet<MappingPair>>();
            lock (visualizedReflexionGraphLock)
            {
                foreach (Node key in candidateRecommendation.Recommendations.Keys)
                {
                    HashSet<MappingPair> visualizedMappingPairs = new HashSet<MappingPair>();
                    Node keyInViz = reflexionGraphViz.GetNode(key.ID);
                    RecommendationsVisualized.Add(keyInViz, visualizedMappingPairs);
                    foreach (MappingPair mappingPair in candidateRecommendation.Recommendations[key])
                    {
                        Node visualizedCandidate = reflexionGraphViz.GetNode(mappingPair.CandidateID);
                        Node visualizedCluster = reflexionGraphViz.GetNode(mappingPair.ClusterID);
                        if (visualizedCandidate == null || visualizedCluster == null)
                        {
                            throw new Exception($"Couldn't map recommendations to visualized reflexion graph." +
                                $" {mappingPair.CandidateID} --> {visualizedCandidate?.ID} | {mappingPair.ClusterID} --> {visualizedCluster?.ID}");
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
                RunExperiment(this.mappingConfig, graph, oracleMapping);
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
            string outputFile = Path.Combine(this.mappingConfig.OutputPath.Path, "oracle.txt");

            File.WriteAllText(outputFile, output);
        }

        public static void RunExperiment(MappingExperimentConfig config, 
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
                Dictionary<Node, HashSet<Node>> initialMapping = recommendations.CreateInitialMapping(initialMappingPercentage,                                                                                                     currentSeed);
                                                                                                     
                UnityEngine.Debug.Log($"initialMapping keys={initialMapping.Keys.Count} values={initialMapping.Values.Count}");
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
                Debug.Log(recommendations.AttractFunction.DumpTrainingData());
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

                trainingData = recommendations.AttractFunction.DumpTrainingData();
                Debug.Log(recommendations.AttractFunction.DumpTrainingData());
                trainingDataFile = Path.Combine(config.OutputPath.Path, $"trainingData2_{currentSeed}.txt");
                File.WriteAllText(trainingDataFile, trainingData);

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
                graph.StartCaching();
                graph.ResetMapping();
                graph.ReleaseCaching();


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
            Dictionary<Node, HashSet<MappingPair>> recommendations = await this.SyncRecommendations(CandidateRecommendation);
            
            // While next recommendation still exists      
            while (recommendations.Count != 0)
            {
                MappingPair chosenMappingPair;

                // TODO: Wrap recommendations within own class?
                if(CandidateRecommendation.IsRecommendationDefinite(recommendations))
                {
                    chosenMappingPair = CandidateRecommendation.GetDefiniteRecommendation(recommendations);
                    // Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}");
                } 
                else
                {
                    chosenMappingPair = recommendations[recommendations.Keys.First()].FirstOrDefault();

                    // TODO: Handle ambigous mapping steps
                    Debug.Log("Warning: Ambigous recommendations.");
                }
                
                Debug.Log($"Chosen Mapping Pair {chosenMappingPair.CandidateID} --> {chosenMappingPair.CandidateID}");

                await MapRecommendation(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
                recommendations = await this.SyncRecommendations(CandidateRecommendation);
            }
            Debug.Log("Automatic Mapping stopped.");
        }

        public static void StartAutomatedMapping(CandidateRecommendation recommendation, ReflexionGraph graph)
        {
            Dictionary<Node, HashSet<MappingPair>> recommendations = recommendation.Recommendations;

            // While next recommendation still exists      
            while (recommendations.Count != 0)
            {
                MappingPair chosenMappingPair;

                // TODO: Wrap recommendations within own class?
                if (CandidateRecommendation.IsRecommendationDefinite(recommendations))
                {
                    chosenMappingPair = CandidateRecommendation.GetDefiniteRecommendation(recommendations);
                    // Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}");
                }
                else
                {
                    chosenMappingPair = recommendations[recommendations.Keys.First()].FirstOrDefault();

                    // TODO: Handle ambigous mapping steps
                    // Debug.Log("Warning: Ambigous recommendation.");
                }

                // Debug.Log($"Chosen Mapping Pair {chosenMappingPair.CandidateID} --> {chosenMappingPair.CandidateID}");

                //await MapRecommendation(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
                // Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}. candidates left: {recommendation.UnmappedCandidates.Count}");
                graph.AddToMapping(chosenMappingPair.Candidate, chosenMappingPair.Cluster);

                recommendations = recommendation.Recommendations;
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
