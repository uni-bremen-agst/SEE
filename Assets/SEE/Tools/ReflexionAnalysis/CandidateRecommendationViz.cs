using Antlr4.Runtime.Misc;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Assets.SEE.UI.PropertyDialog;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using XInputDotNetPure;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This class provides operations to visualize and calculate candidate recommendations 
    /// for the reflexion city. It abstracts operations to calculate the recommendations asynchronous 
    /// and should be the interface through which the reflexion city game object request operations 
    /// regarding recommendations for mapping operations. This objects forwards operations on the reflexion graph 
    /// to a clone of the visualized graph to calculate recommendations in the background and sync them after 
    /// they finished.
    /// </summary>
    public class CandidateRecommendationViz : MonoBehaviour, IObserver<ChangeEvent>
    {
        /// <summary>
        /// Recommendation settings object given by the reflexion city.
        /// </summary>
        private RecommendationSettings RecommendationSettings;

        /// <summary>
        /// Candidate recommendation object used to retrieve recommendations. 
        /// Provides all operations regarding candidate recommendations and should 
        /// work on the calculation reflexion graph.
        /// </summary>
        public CandidateRecommendation CandidateRecommendation { get; private set; }

        /// <summary>
        /// Reflexion graph visualized by the reflexion city.
        /// </summary>
        private ReflexionGraph reflexionGraphViz;

        /// <summary>
        /// Reflexion graph visualized by the reflexion city.
        /// </summary>
        public ReflexionGraph ReflexionGraphVisualized { get => reflexionGraphViz; }

        /// <summary>
        /// Cloned reflexion graph which is used to calculate recommendations for 
        /// the visualized reflexion graph. The graph should be detached from all previous 
        /// event listener and should not be associated with any game objects. 
        /// </summary>
        private ReflexionGraph reflexionGraphCalc;

        /// <summary>
        /// Property set to true, if a oracle graph is loaded within the candidate recommendation.
        /// </summary>
        public bool OracleGraphLoaded { get => CandidateRecommendation.OracleGraphLoaded; }

        #region synchronization
        /// <summary>
        /// Queue which contains all events received by the visualized graph and which are forwarded 
        /// towards the reflexion graph.
        /// </summary>
        private Queue<ChangeEvent> changeEventQueue = new Queue<ChangeEvent>();

        /// <summary>
        /// Lock object restricting write operations on the reflexion graph for calculations.
        /// </summary>
        private object calculationReflexionGraphLock = new object();

        /// <summary>
        /// Lock object restricting write operations on the visualized reflexion graph.
        /// </summary>
        private object visualizedReflexionGraphLock = new object();

        /// <summary>
        /// Flag signaling if events are currently processed.
        /// </summary>
        private bool ProcessingEvents { get; set; }

        /// <summary>
        /// Flag signaling if data is currently processed.
        /// TODO: Necessary? Never set to true
        /// </summary>
        private bool ProcessingData { get; set; }

        /// <summary>
        /// Flag signaling if any processing is currently executed.
        /// TODO: Not Necessary if processing data will be deleted. 
        /// </summary>
        private bool Processing { get => ProcessingEvents || ProcessingData; } 
        #endregion

        #region button labels

        private const string showMappingChoicesLabel = "Show MappingChoices";
        private const string startRecordingLabel = "Start Recording";
        private const string stopRecordingLabel = "Stop Recording";
        private const string calculateResultsLabel = "Calculate Results";
        private const string dumbTrainingDataLabel = "Dump Training Data";
        private const string debugScenarioLabel = "Debug Scenario";
        private const string dumpOracleLabel = "Dump Oracle";
        private const string createOracleMappingLabel = "Create Oracle GXLs";
        private const string createMappingGXLMappingLabel = "Create Mapping GXL";
        private const string dumpSystemStatisticsLabel = "Dump System Statistics";

        private const string statisticButtonGroup = "statisticButtonsGroup";
        private const string mappingButtonGroup = "mappingButtonsGroup";
        private const string debugButtonGroup = "debugButtonGroup"; 
        private const string gxlButtonGroup = "gxlButtonGroup"; 

        #endregion

        #region eventHandling

        /// <summary>
        /// Completion callback called by the event system.
        /// TODO: any handling necessary?
        /// </summary>
        public void OnCompleted()
        {
            CandidateRecommendation.OnCompleted();
        }

        /// <summary>
        /// Error callback called by the event system.
        /// TODO: any handling necessary?
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
            CandidateRecommendation.OnError(error);
        }

        /// <summary>
        /// Event callback called by the event system.
        /// 
        /// This operation receives all change events of the visualized reflexion graph and 
        /// adds the events, which are changing the structure of the reflexion graph 
        /// to a queue. If currently no events are processed and a change within 
        /// the mapping subgraph is detected, the asynchronous processing of the
        /// events by forwarding them to the calculation reflexion graph is started. 
        /// 
        /// PostCondition: The Processing Events flag is true, 
        /// if the processing of events was started 
        ///
        /// </summary>
        /// <param name="value">Change events received by the visualized reflexion graph.</param>
        public void OnNext(ChangeEvent value)
        {
            if (value is NodeEvent || value is EdgeEvent || value is HierarchyEvent)
            {
                changeEventQueue.Enqueue(value);
            }

            // TODO: How to solve event filtering in both classes candidateRecommendation and candidateRecommendationViz, EventFilter class?
            if (!ProcessingEvents && value is EdgeEvent edgeEvent && edgeEvent.Affected == ReflexionSubgraphs.Mapping)
            {
                ProcessingEvents = true;
                ProcessEvents().Forget();         
            }
        }

        #endregion

        #region eventForwarding
        /// <summary>
        /// Operation which starts the processing of the enqueued change events
        /// on the thread pool. All enqueued events are forwarded to the calculation 
        /// reflexion graph and the recommendations are updated.
        /// 
        /// PostCondition: The Processing Events flag is false, after 
        /// all events are processed.
        /// 
        /// </summary>
        /// <returns>Awaitable Unitask</returns>
        public async UniTask ProcessEvents()
        {
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

        /// <summary>
        /// This method translates received change events into operations updating the structure 
        /// of the calculation graph to keep the graph in sync with the visualized reflexion graph.
        /// It only translates events of the types <see cref="NodeEvent"/>, <see cref="EdgeEvent"/>
        /// and <see cref="HierarchyEvent"/>. The calculation reflexion graph is updated by adding 
        /// cloned nodes and edges or mirror operations contained by the received events.
        /// 
        /// THe method does not forward temporary artificial edges. 
        /// 
        /// </summary>
        /// <param name="value">received change event</param>
        /// <exception cref="Exception">Throws exceptions if nodes or edges 
        /// of the visualized reflexion graph could not be synchronized with the calculation reflexion graph.</exception>
        public void SendEventToCalculationGraph(ChangeEvent value)
        {
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

                        reflexionGraphCalc.RemoveNode(nodeToRemove, nodeEvent.OrphansBecomeRoots);
                    }
                    break;
                case MapsToChange mapsToChange:
                    if(mapsToChange.Change == ChangeType.Addition)
                    {
                        reflexionGraphCalc.AddToMapping(mapsToChange.Source, mapsToChange.Target, overrideMapping: true);
                    } 
                    else if (mapsToChange.Change == ChangeType.Removal)
                    {
                        reflexionGraphCalc.RemoveFromMapping(mapsToChange.Source, ignoreUnmapped: true);
                    }
                    break;
                case EdgeEvent edgeEvent:

                    if (value.Change == ChangeType.Addition)
                    {
                        if (edgeEvent.Affected == ReflexionSubgraphs.Mapping)
                        {
                            Node nodeSource = reflexionGraphCalc.GetNode(edgeEvent.Edge.Source.ID);
                            Node nodeTarget = reflexionGraphCalc.GetNode(edgeEvent.Edge.Target.ID);
                            reflexionGraphCalc.AddToMapping(nodeSource, nodeTarget, overrideMapping: true);
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
                    else if (value.Change == ChangeType.Removal
                        && (!edgeEvent.Edge.IsInArchitecture() || ReflexionGraph.IsSpecified(edgeEvent.Edge)))
                    {
                        Edge edgeToRemove = reflexionGraphCalc.GetEdge(edgeEvent.Edge.ID);
                        if (edgeToRemove == null)
                        {
                            throw new Exception($"Edge {edgeEvent.Edge.ID} not found in calculation reflexion graph" +
                                                                      $" when trying to synchronize with visualization reflexion graph.");
                        }
                        reflexionGraphCalc.RemoveEdge(edgeToRemove);
                    }
                    break;
                case HierarchyEvent e:
                    if (e.Change == ChangeType.Addition)
                    {
                        Node child = reflexionGraphCalc.GetNode(e.Child.ID);
                        Node parent = reflexionGraphCalc.GetNode(e.Parent.ID);

                        if (child == null || parent == null)
                        {
                            throw new Exception("Could not send HierarchyEvent to calculation reflexion graph. " +
                                                "Either child or parent could not be found in the receiving graph.");
                        }

                        if (child.Parent != null)
                        {
                            child.Reparent(parent);
                        }
                        else
                        {
                            parent.AddChild(child);
                        }
                    }
                    else if (e.Change == ChangeType.Removal)
                    {
                        // Does not need to get handled. This case is handled by using reparent above.
                    }

                    break;
                default: break;
            }
        }
        #endregion

        /// <summary>
        /// Returns all nodes which are currently unmapped.
        /// </summary>
        /// <returns>IEnumerable object containing currently unmapped nodes.</returns>
        public IEnumerable<Node> GetUnmappedCandidates()
        {
            return CandidateRecommendation.GetUnmappedCandidates().Select(n => reflexionGraphViz.GetNode(n.ID));
        }

        public bool IsCandidate(string candidateId)
        {
            if(reflexionGraphCalc.ContainsNodeID(candidateId))
            {
                return CandidateRecommendation.IsCandidate(reflexionGraphCalc.GetNode(candidateId));
            } 
            else
            {
                return false;
            } 
        }

        SEEReflexionCity city;

        /// <summary>
        /// This operations updates the candidate recommendation object with the given recommendation settings. 
        /// Before updating the attract function it copies the given <param name="visualizedGraph"> to instantiate
        /// the <see cref="reflexionGraphCalc"/>. The <see cref="CandidateRecommendation"/> object will be instantiated 
        /// if it does not exist yet and it will registered to the event system of the  instantiated reflexion graph.
        /// 
        /// A reflexion analysis on the instantiated reflexion graph will be run.
        /// 
        /// </summary>
        /// <param name="visualizedGraph">visualized graph contained by the reflexion city.</param>
        /// <param name="recommendationSettings">recommendation setting object containg information 
        /// about the attract function and instructions for the experiment.</param>
        /// <param name="oracleMapping">Optional graph containing maps to edges. 
        ///                             The graph will be used to construct an oracle reflexion graph.</param>
        /// <exception cref="Exception">Throws if the <param name="visualizedGraph"> or <param name="recommendationSettings"> are null.</exception>
        public async UniTask UpdateConfiguration(ReflexionGraph visualizedGraph,
                                       RecommendationSettings recommendationSettings,
                                       SEEReflexionCity city,
                                       Graph oracleMapping = null)
        {
            await UniTask.WaitWhile(() => Processing);
            this.city = city;
            ProcessingEvents = false;
            ProcessingData = false;
            changeEventQueue.Clear();

            if (CandidateRecommendation == null)
            {
                CandidateRecommendation = new CandidateRecommendation();
            }

            if (recommendationSettings == null)
            {
                throw new Exception("Given recommendation settings were null.");
            }

            // save recommendation settings from reflexion city to
            // access chosen paths used by debug buttons and operations.
            this.RecommendationSettings = recommendationSettings;

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
                        throw new Exception("Given Reflexion Graph was null. Cannot update Candidate MappingChoice configuration.");
                    }

                    reflexionGraphCalc = new ReflexionGraph(reflexionGraphViz);
                }

                reflexionGraphCalc.Name = "reflexionGraph for Recommendations";
                CandidateRecommendation.UpdateConfiguration(reflexionGraphCalc,
                                                            recommendationSettings,
                                                            oracleMapping);
            }
        }

        /// <summary>
        /// Returns the recommended mapping pairs for the visualized reflexion graph. 
        /// This operations is synchronized with the processing of events.
        /// </summary>
        /// <returns>IEnumerable object containing the recommendations for the next mapping</returns>
        private async Task<IEnumerable<MappingPair>> GetAutomaticMappingsAsync()
        {
            await UniTask.WaitWhile(() => ProcessingEvents);
            return await UniTask.RunOnThreadPool(() =>
            {
                lock (calculationReflexionGraphLock)
                {
                    return this.CandidateRecommendation.GetAutomaticMappings();
                }
            });
        }

        #region experiment

        private bool evaluationRunning = false;

        public async UniTask Evaluation()
        {
            if(evaluationRunning)
            {
                return;
            }

            evaluationRunning = true;

            UnityEngine.Debug.Log("Start Evaluation...");
            GameObject gameobject = SceneQueries.GetCodeCity(this.transform)?.gameObject;

            if (gameobject == null)
            {
                throw new Exception("Could not get Reflexion city when loading oracle instructions.");
            }

            gameobject.TryGetComponent(out SEEReflexionCity city);

            int n = 200;

            List<RecommendationSettings> settings = new()
            {
                RecommendationSettings.CreateGroup(n, 0.5f, "CAAttract_Zero_05", AttractFunction.AttractFunctionType.CountAttract, 0.0f),
                RecommendationSettings.CreateGroup(n, 0.5f, "CAAttract_One_05", AttractFunction.AttractFunctionType.CountAttract, 1.0f),
                RecommendationSettings.CreateGroup(n, 0.5f, "ADCAttract_05", AttractFunction.AttractFunctionType.ADCAttract),
                RecommendationSettings.CreateGroup(n, 0.5f, "NBAttract_05", AttractFunction.AttractFunctionType.NBAttract),
                RecommendationSettings.CreateGroup(n, 0.7f, "CAAttract_Zero_07", AttractFunction.AttractFunctionType.CountAttract, 0.0f),
                RecommendationSettings.CreateGroup(n, 0.7f, "CAAttract_One_07", AttractFunction.AttractFunctionType.CountAttract, 1.0f),
                RecommendationSettings.CreateGroup(n, 0.7f, "ADCAttract_07", AttractFunction.AttractFunctionType.ADCAttract),
                RecommendationSettings.CreateGroup(n, 0.7f, "NBAttract_07", AttractFunction.AttractFunctionType.NBAttract),
                RecommendationSettings.CreateGroup(n, 0.9f, "CAAttract_Zero_09", AttractFunction.AttractFunctionType.CountAttract, 0.0f),
                RecommendationSettings.CreateGroup(n, 0.9f, "CAAttract_One_09", AttractFunction.AttractFunctionType.CountAttract, 1.0f),
                RecommendationSettings.CreateGroup(n, 0.9f, "ADCAttract_09", AttractFunction.AttractFunctionType.ADCAttract),
                RecommendationSettings.CreateGroup(n, 0.9f, "NBAttract_09", AttractFunction.AttractFunctionType.NBAttract)
            };

            city.Reset();
            await city.LoadDataAsync();

            foreach(RecommendationSettings setting in settings)
            {
                UnityEngine.Debug.Log($"iterate group {setting.ExperimentName}...");
                setting.iterations = n;
                await city.UpdateRecommendationSettings(setting);
                city.RecommendationSettings = setting;
                setting.OutputPath.Path = Path.Combine(Path.Combine(GetConfigPath(), "Results"), setting.ExperimentName);
                await city.RunMappingExperiment();
            }
            evaluationRunning = false;
            UnityEngine.Debug.Log($"finish Evaluation...");
        }

        /// <summary>
        /// Constructs an initial mapping for the visualized reflexion graph based on the loaded oracle graph. 
        /// The construction of the mapping is synchronized with the view.
        /// </summary>
        /// <param name="initialMappingPercentage">percentage of the candidates nodes that should be mapped after this operation.</param>
        /// <param name="seed">Seed determined pseudo randomness to select the initially mapped candidates.</param>
        /// <param name="delay">Delay which is waited after a mapping was animated.</param>
        /// <param name="reportProgress">Callback action to report progress.</param>
        /// <returns>Awaitable UniTask</returns>
        public async UniTask CreateInitialMappingAsync(double initialMappingPercentage,
                                                       int seed,
                                                       bool syncWithView = false,
                                                       int delay = 500,
                                                       Action<float> reportProgress = null)
        {
            if (!this.OracleGraphLoaded)
            {
                ShowNotification.Warn("Cannot generate initial mapping", "No Oracle Graph loaded. Cannot generate inital mapping.");
                return;
            }

            // TODO: use reportProgress parameter
            // TODO: change datatype to mapping pair list
            Dictionary<Node, HashSet<Node>> initialMapping;
            lock (visualizedReflexionGraphLock)
            {
                initialMapping = this.CandidateRecommendation.CreateInitialMapping(initialMappingPercentage,
                                                                                   seed);
            }
            try
            {
                foreach (Node cluster in initialMapping.Keys)
                {
                    foreach (Node candidate in initialMapping[cluster])
                    {
                        await MapRecommendationInVizAsync(candidate, cluster, syncWithView);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        /// <summary>
        /// Resets the current mapping of the visualized reflexion graph. 
        /// This operations is synced with the view. 
        /// After this call the previous maps to relations 
        /// are forgotten.
        /// </summary>
        /// <param name="delay">Delay that is waited after the movement of a node is animated.</param>
        /// <returns>Awaitable UniTask</returns>
        public async UniTask ResetMappingAsync(int delay = 500)
        {
            if (reflexionGraphViz == null)
            {
                return;
            }

            IEnumerable<Node> unmappedNodes;
            lock (visualizedReflexionGraphLock)
            {
                unmappedNodes = reflexionGraphViz.ResetMapping();
            }

            if (RecommendationSettings.syncWithView)
            {
                foreach (Node unmappedNode in unmappedNodes)
                {
                    GameNodeMover.MoveTo(unmappedNode.GameObject(), unmappedNode.Parent.GameObject().GetGroundCenter(), 1.0f);
                    new MoveNetAction(unmappedNode.GameObject().name, unmappedNode.Parent.GameObject().GetGroundCenter(), 1.0f).Execute();
                    await UniTask.Delay(delay);
                } 
            }
        }

        public async UniTask RunExperimentInBackground(RecommendationSettings recommendationSettings,
                                Graph oracleMapping)
        {
            if (recommendationSettings.syncWithView)
            {
                throw new Exception($"Cannot run experiment in background if the experiment shall be synced with view. recommendationSettings.syncExperimentWithView={recommendationSettings.syncWithView}");
            }
      
            System.Random rand = new System.Random(recommendationSettings.rootSeed);
            int currentSeed = rand.Next(int.MaxValue);
            double initialMappingPercentage = recommendationSettings.initialMappingPercentage;
            string experimentName = recommendationSettings.ExperimentName;

            ReflexionGraph graph;
            // Calculate experiment in the background
            await UniTask.WaitWhile(() => Processing);
            lock (visualizedReflexionGraphLock)
            {
                graph = new ReflexionGraph(reflexionGraphViz);
            }
            graph.Name = $"reflexionGraph for Experiment {experimentName}";

            CandidateRecommendation recommendations = new CandidateRecommendation();

            UnityEngine.Debug.Log("Setup configuration...");

            await UniTask.RunOnThreadPool(() => recommendations.UpdateConfiguration(graph, recommendationSettings, oracleMapping));

            await UniTask.RunOnThreadPool(() => recommendations.ReflexionGraph.ResetMapping());

            Debug.Log($"Start Experiment {experimentName} for graph {recommendations.ReflexionGraph.Name}({recommendations.ReflexionGraph.Nodes().Count} nodes, candidates={recommendations.ReflexionGraph.Nodes().Where(n => recommendations.IsCandidate(n))} {recommendations.ReflexionGraph.Edges().Count} edges)");

            FileStream stream;

            List<MappingExperimentResult> results = new List<MappingExperimentResult>();

            Directory.CreateDirectory(recommendationSettings.OutputPath.Path);

            for (int i = 0; i < recommendationSettings.iterations; ++i)
            {
                UnityEngine.Debug.Log($"Experiment run={i}...");
                // STEPS
                // 1. Create initial mapping based on current seed
                Dictionary<Node, HashSet<Node>> initialMapping = await UniTask.RunOnThreadPool(() => recommendations.CreateInitialMapping(initialMappingPercentage, currentSeed));
                UnityEngine.Debug.Log("Create initial mapping...");

                foreach (Node cluster in initialMapping.Keys)
                {
                    foreach (Node candidate in initialMapping[cluster])
                    {
                        await UniTask.RunOnThreadPool(() =>
                        {
                            recommendations.ReflexionGraph.StartCaching();
                            // UnityEngine.Debug.Log($"Map the node {candidate.ID} to the cluster {cluster.ID} as initial mapping");
                            recommendations.ReflexionGraph.AddToMapping(candidate, cluster);
                            recommendations.ReflexionGraph.ReleaseCaching();
                        });
                    }
                }

                string csvFile = Path.Combine(recommendationSettings.OutputPath.Path, $"{experimentName}_output_{currentSeed}.csv");

                await UniTask.RunOnThreadPool(() => recommendations.UpdateRecommendations());

                // 3. Start Recording to csv file
                recommendations.Statistics.StartRecording(csvFile);

                UnityEngine.Debug.Log("Start automated mapping...");
                // 4. Start automated mapping
                await StartAutomatedMappingAsync(recommendations, 
                                                syncWithView:false, 
                                                ignoreTieBreakers:true, 
                                                new System.Random(currentSeed));

                // 5. Stop Recording
                recommendations.Statistics.StopRecording();

                // 6. Process Data and keep result object
                MappingExperimentResult result = await UniTask.RunOnThreadPool(() => recommendations.Statistics.CalculateResults(csvFile));
                result.CurrentSeed = currentSeed;
                result.MasterSeed = recommendationSettings.rootSeed;
                results.Add(result);
                string xmlFile = Path.Combine(recommendationSettings.OutputPath.Path, $"{experimentName}_output_{currentSeed}.xml");
                stream = new FileStream(xmlFile, FileMode.Create);
                result.CreateXml().Save(stream);
                stream.Close();
                Debug.Log($"Saved Result of Run to {xmlFile}");

                UnityEngine.Debug.Log("reset mapping...");
                await UniTask.RunOnThreadPool(() => recommendations.ReflexionGraph.ResetMapping(true));

                if (!recommendations.AttractFunction.EmptyTrainingData())
                {
                    throw new Exception("Training Data was not resetted correctly after resetting mapping during experiment.");
                }

                if (recommendations.AttractFunction.HandledCandidates.Count != 0)
                {
                    throw new Exception("Handled candidates left despite mapping was resetted.");
                }

                // 8. Create next seed
                currentSeed = rand.Next(int.MaxValue);
            }

            MappingExperimentResult averageResult = await UniTask.RunOnThreadPool(() => MappingExperimentResult.AverageResults(results, recommendationSettings));
            averageResult.MasterSeed = recommendationSettings.rootSeed;
            averageResult.Iterations = recommendationSettings.iterations;
            string resultXml = Path.Combine(recommendationSettings.OutputPath.Path, $"{experimentName}_result.xml");
            stream = new FileStream(resultXml, FileMode.Create);
            averageResult.CreateXml().Save(stream);
            stream.Close();
            UnityEngine.Debug.Log($"Finished Experiment for seed {recommendationSettings.rootSeed}. Saved averaged result to {resultXml}");
        }

        /// <summary>
        /// Starts an automated experiment and saves the results to the output path 
        /// defined in the <paramref name="recommendationSettings"/> object. 
        /// 
        /// The experiment will be excuted multiple times defined by the iteration 
        /// setting.
        /// 
        /// If the syncExperimentWithView flag within the setting object is set to true 
        /// the experiment is synchronized with the view within the city. Otherwise 
        /// it is calculated asynchronously on the thread pool using a cloned reflexion graph.
        /// 
        /// If the IgnoreTieBreaker flag is true ambigous mappings will be mapped 
        /// randomnly based on the given masterSeed. Otherwise a dialog will pop up 
        /// that allows to resolve the tie breaker manually.
        /// 
        /// </summary>
        /// <param name="recommendationSettings">Setting object containing all parameters for the experiment.</param>
        /// <param name="oracleMapping">Oracle mapping</param>
        /// <returns></returns>
        /// <exception cref="Exception">Throws if the training within a attract function is not correctly resetted
        /// after the mapping was resetted when the current iteration is finished.</exception>
        public async UniTask RunExperimentAsync(RecommendationSettings recommendationSettings,
                                        Graph oracleMapping)
        {
            bool syncWithView = recommendationSettings.syncWithView;

            int currentSeed;
            System.Random rand = new System.Random(recommendationSettings.rootSeed);
            if (recommendationSettings.iterations <= 1)
            {
                currentSeed = recommendationSettings.rootSeed;
            } 
            else
            {
                currentSeed = rand.Next(int.MaxValue);
            }

            double initialMappingPercentage = recommendationSettings.initialMappingPercentage;
            string experimentName = recommendationSettings.ExperimentName;

            CandidateRecommendation recommendations;

            if (syncWithView)
            {
                // Calculate experiment in the foreground and sync with view
                recommendations = this.CandidateRecommendation;
            }
            else
            {
                ReflexionGraph graph;

                // Calculate experiment in the background
                lock (visualizedReflexionGraphLock)
                {
                    UniTask.WaitWhile(() => Processing);
                    graph = new ReflexionGraph(reflexionGraphViz);
                }
                graph.Name = $"reflexionGraph for Experiment {experimentName}";

                recommendations = new CandidateRecommendation();
                recommendations.UpdateConfiguration(graph, recommendationSettings, oracleMapping);
            }

            Debug.Log($"Start Experiment {experimentName} for graph {recommendations.ReflexionGraph.Name}({recommendations.ReflexionGraph.Nodes().Count} nodes, candidates={recommendations.ReflexionGraph.Nodes().Where(n => recommendations.IsCandidate(n))} {recommendations.ReflexionGraph.Edges().Count} edges)");

            if (syncWithView)
            {
                await this.ResetMappingAsync();
            }
            else
            {
                recommendations.ReflexionGraph.ResetMapping();
            }

            FileStream stream;
            List<MappingExperimentResult> results = new List<MappingExperimentResult>();

            Directory.CreateDirectory(recommendationSettings.OutputPath.Path);

            for (int i = 0; i < recommendationSettings.iterations; ++i)
            {
                //Create initial mapping based on current seed
                Dictionary<Node, HashSet<Node>> initialMapping = recommendations.CreateInitialMapping(initialMappingPercentage, currentSeed);

                UnityEngine.Debug.Log($"Experiment run={i} initialMapping keys={initialMapping.Keys.Count} values={initialMapping.Values.Count}");


                if (syncWithView)
                {   // create initial mapping to viz graph
                    foreach (Node cluster in initialMapping.Keys)
                    {
                        foreach (Node candidate in initialMapping[cluster])
                        {
                            await this.MapRecommendationInVizAsync(candidate, cluster, syncWithView);
                        }
                    }
                }
                else
                {
                    // create initial mapping for calc graph
                    foreach (Node cluster in initialMapping.Keys)
                    {
                        foreach (Node candidate in initialMapping[cluster])
                        {
                            recommendations.ReflexionGraph.StartCaching();
                            recommendations.ReflexionGraph.AddToMapping(candidate, cluster);
                            recommendations.ReflexionGraph.ReleaseCaching();
                        }
                    }

                }

                if (recommendationSettings.logTrainingData)
                {
                    string trainingData = recommendations.AttractFunction.DumpTrainingData();
                    string trainingDataFile = Path.Combine(recommendationSettings.OutputPath.Path, $"{experimentName}_trainingData_{currentSeed}.txt");
                    File.WriteAllText(trainingDataFile, trainingData); 
                }

                //Generate .csv-file based on seed name/output path
                string csvFile = Path.Combine(recommendationSettings.OutputPath.Path, $"{experimentName}_output_{currentSeed}.csv");

                recommendations.UpdateRecommendations();

                //Start Recording to csv file
                recommendations.Statistics.StartRecording(csvFile);

                //Start automated mapping
                bool ignoreTieBreakers = !syncWithView || recommendationSettings.IgnoreTieBreakers;
                await StartAutomatedMappingAsync(recommendations, syncWithView, ignoreTieBreakers, new System.Random(currentSeed));
            
                //Stop Recording
                recommendations.Statistics.StopRecording();

                //Process Data and keep result object
                MappingExperimentResult result = recommendations.Statistics.CalculateResults(csvFile);
                result.CurrentSeed = currentSeed;
                result.MasterSeed = recommendationSettings.rootSeed;
                results.Add(result);
                string xmlFile = Path.Combine(recommendationSettings.OutputPath.Path, $"{experimentName}_output_{currentSeed}.xml");
                stream = new FileStream(xmlFile, FileMode.Create);
                result.CreateXml().Save(stream);
                stream.Close();
                Debug.Log($"Saved Result of Run to {xmlFile}");

                // ResetMapping
                if (!syncWithView)
                {
                    //sync not with viz
                    recommendations.ReflexionGraph.ResetMapping(true);
                }
                else
                {
                    //sync with viz
                    await this.ResetMappingAsync();
                }

                if (!recommendations.AttractFunction.EmptyTrainingData())
                {
                    throw new Exception("Training Data was not resetted correctly after resetting mapping during experiment.");
                }

                if (recommendations.AttractFunction.HandledCandidates.Count != 0)
                {
                    throw new Exception("Handled candidates left despite mapping was resetted.");
                }

                // Create next seed
                currentSeed = rand.Next(int.MaxValue);
            }

            // Save Average Results
            MappingExperimentResult averageResult = MappingExperimentResult.AverageResults(results, recommendationSettings);
            averageResult.MasterSeed = recommendationSettings.rootSeed;
            averageResult.Iterations = recommendationSettings.iterations;
            string resultXml = Path.Combine(recommendationSettings.OutputPath.Path, $"{experimentName}_result.xml");
            stream = new FileStream(resultXml, FileMode.Create);
            averageResult.CreateXml().Save(stream);
            stream.Close();
            UnityEngine.Debug.Log($"Finished Experiment for seed {recommendationSettings.rootSeed}. Saved averaged result to {resultXml}");
        }

        /// <summary>
        /// Starts the processing of automated mapping of candidates until no candidates can be mapped anymore.
        /// </summary>
        /// <param name="candidateRecommendation">recommendation object that is used to calculate the recommendations.
        /// Can be null if syncWithView is set to true.</param>
        /// <param name="syncWithView">Determines if the automated mapping should be synchronized with the view.</param>
        /// <param name="ignoreTieBreakers">Determines if tie breakers should be resolved by the user or by randomness</param>
        /// <param name="random">random object used to choose ambigous recommendations. 
        /// Can be null if <param name="ignoreTieBreakers"> is set to false.</param>
        /// <returns>Awaitable UniTask</returns>
        /// <exception cref="Exception">Throws if parameters are inconsistent.</exception>
        public async UniTask StartAutomatedMappingAsync(CandidateRecommendation candidateRecommendation,
                                                        bool syncWithView = false,
                                                        bool ignoreTieBreakers = true,
                                                        System.Random random = null)
        {
            if (random == null && ignoreTieBreakers)
            {
                throw new Exception("No Object for generating randomness defined. Cannot ignore tie breaker without using randomness");
            }

            if (syncWithView == false && candidateRecommendation == null)
            {
                throw new Exception("No candidate recommendation object was given. This parameter is required if syncWithView is set to false.");
            }

            // Get recommendations which can be mapped automatically
            // List<MappingPair> recommendations = (await this.GetAutomaticMappingsAsync()).ToList();
            List<MappingPair> recommendations = (syncWithView ? await this.GetAutomaticMappingsAsync() : candidateRecommendation.GetAutomaticMappings()).ToList();

            while (true)
            {
                UnityEngine.Debug.Log($"Number retrieved recommendations: {recommendations.Count()}");
                if (recommendations.Count() == 0 && !ignoreTieBreakers)
                {
                    // Get recommendations and show them to the users
                    recommendations = candidateRecommendation.GetRecommendations().ToList();
                    if (recommendations.Count > 0)
                    {
                        recommendations = (await StartMappingChoiceDialog(recommendations)).ToList();
                        UnityEngine.Debug.Log($"Number of chosen recommendations after dialog: {recommendations.Count()}"); 
                    }
                }

                if (recommendations.Count() == 0)
                {
                    break;
                }

                while (recommendations.Count() > 0)
                {
                    MappingPair chosenMappingPair = recommendations[random.Next(recommendations.Count())];

                    if (syncWithView)
                    {
                        await MapRecommendationInVizAsync(chosenMappingPair.Candidate, chosenMappingPair.Cluster, syncWithView);
                        await UniTask.WaitWhile(() => Processing);
                    }
                    else
                    {
                        await UniTask.RunOnThreadPool(() =>
                        {
                            candidateRecommendation.ReflexionGraph.StartCaching();
                            candidateRecommendation.ReflexionGraph.AddToMapping(chosenMappingPair.Candidate, 
                                                                                chosenMappingPair.Cluster, 
                                                                                overrideMapping: true);
                            candidateRecommendation.ReflexionGraph.ReleaseCaching();
                        });
                    }
                    recommendations.RemoveAll(m => m.CandidateID.Equals(chosenMappingPair.CandidateID));
                }
                candidateRecommendation.UpdateRecommendations();
                recommendations = (syncWithView ? (await this.GetAutomaticMappingsAsync()) : candidateRecommendation.GetAutomaticMappings()).ToList();
            }

            Debug.Log("Automatic Mapping stopped.");
        }

        /// <summary>
        /// Debug function to print mapping pairs
        /// </summary>
        /// <param name="mappingPairs">printed mapping pairs</param>
        private void PrintMappingsPairs(IEnumerable<MappingPair> mappingPairs)
        {
            StringBuilder sb = new();
            sb.Append("{");
            foreach (MappingPair pair in mappingPairs)
            {
                sb.Append(Environment.NewLine);
                sb.Append("\t");
                sb.Append(pair.ToShortString());
            }
            sb.Append("}");
            UnityEngine.Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Maps a candidate node to a given cluster and synchronized with the view.
        /// </summary>
        /// <param name="candidate">Candidate that should mapped.</param>
        /// <param name="cluster">Cluster to which the cancidate should be mapped.</param>
        /// <param name="delay">Delay waited after the execution to let the animation finish.</param>
        /// <returns>Awaitable UniTask</returns>
        private async UniTask MapRecommendationInVizAsync(Node candidate, Node cluster, bool syncWithView = false, int delay = 500)
        {
            await UniTask.WaitWhile(() => ProcessingEvents);
            await UniTask.SwitchToMainThread();
            Node candidateInViz;
            Node clusterInViz;
            lock (visualizedReflexionGraphLock)
            {
                UnityEngine.Debug.Log($"About to map: candidate {candidate.ID} Into cluster {cluster.ID}");

                candidateInViz = reflexionGraphViz.GetNode(candidate.ID);
                clusterInViz = reflexionGraphViz.GetNode(cluster.ID);

                if (candidateInViz == null || clusterInViz == null)
                {
                    ShowNotification.Error("Could not sync mapped child with View.", "Could not sync mapped child with View. Recommendations might be inconsistent.");
                    return;
                }

                if (syncWithView)
                {
                    Debug.Log($"move candidate {candidate.ID} Into cluster {cluster.ID}");
                    GameNodeMover.MoveTo(candidateInViz.GameObject(), clusterInViz.GameObject().GetGroundCenter(), 1.0f);
                    new MoveNetAction(candidateInViz.GameObject().name, clusterInViz.GameObject().GetGroundCenter(), 1.0f).Execute(); 
                    ReflexionMapper.SetParent(candidateInViz.GameObject(), clusterInViz.GameObject());
                } 
                else
                {
                    reflexionGraphViz.AddToMapping(candidateInViz, clusterInViz);
                }

            }
            await UniTask.Delay(delay);
        }

        /// <summary>
        /// Opens a property dialog to present mapping pair choices to the user.
        /// </summary>
        /// <param name="mappingPairChoices">Mapping pair choices presented to the user.</param>
        /// <returns>Mapping Pairs chosen by the user.</returns>
        public async UniTask<IEnumerable<MappingPair>> StartMappingChoiceDialog(IEnumerable<MappingPair> mappingPairChoices)
        {
            GameObject dialog = new GameObject("MappingChoiceDialog");
            PropertyGroup propertyGroup = dialog.AddComponent<PropertyGroup>();
            propertyGroup.Name = "Mapping Choice Dialog";
            List<MappingChoiceProperty> properties = new();
            foreach (var mappingPairChoice in mappingPairChoices)
            {
                MappingChoiceProperty property = dialog.AddComponent<MappingChoiceProperty>();
                property.mappingPair = mappingPairChoice;
                property.Name = mappingPairChoice.Candidate.ToShortString() + mappingPairChoice.Cluster.ToShortString();
                propertyGroup.AddProperty(property);
                properties.Add(property);
            }

            propertyGroup.GetReady();
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Mapping Choice Dialog";
            propertyDialog.Description = $"Candidates attracted to the clusters with {mappingPairChoices.FirstOrDefault()?.AttractionValue ?? -1.0}";
            propertyDialog.AddGroup(propertyGroup);

            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;

            using var confirmHandler = propertyDialog.OnConfirm.GetAsyncEventHandler(default);
            using var cancelHandler = propertyDialog.OnCancel.GetAsyncEventHandler(default);

            int retVal = await UniTask.WhenAny(confirmHandler.OnInvokeAsync(), cancelHandler.OnInvokeAsync());

            SEEInput.KeyboardShortcutsEnabled = true;

            if (retVal == 0)
            {
                return properties.Where(p => p.Value).Select(p => p.mappingPair);
            }

            return new List<MappingPair>();
        }

        #endregion

        #region visualFeedback

        /// <summary>
        /// A delay passed before the blink effect for candidates is triggered.
        /// TODO: Necessary???
        /// </summary>
        private static float BLINK_EFFECT_DELAY = 0.1f;

        /// <summary>
        /// Started couroutine which executes the blink effect for candidates.
        /// </summary>
        private Coroutine blinkEffectCoroutine;

        /// <summary>
        /// Changes the color if all unmapped Candidates to a given color.
        /// </summary>
        /// <param name="color">Given color</param>
        public void ColorUnmappedCandidates(Color color)
        {
            IEnumerable<string> ids = this.CandidateRecommendation.GetUnmappedCandidates().Select(n => n.ID);
            List<Node> nodes = new();
            foreach(string id in ids) 
            {
                Node node = reflexionGraphViz.GetNode(id);
                if (node != null)
                {
                    try
                    {
                        UnityEngine.Debug.Log($"try to change color of node {node.ID}");
                        NodeOperator nodeOperator = node.GameObject().AddOrGetComponent<NodeOperator>();
                        Color currentColor = nodeOperator.TargetColor;
                        nodeOperator.ChangeColorsTo(color, 1);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
                else 
                {
                    UnityEngine.Debug.LogWarning($"Couldn't retrieve node id {id} from visualized graph.");
                }
            }
        }

        /// <summary>
        /// Starts the blink effect for a given node and its recommendations. 
        /// If no recommendations can be found for the given node or the node 
        /// is either a current candidate or cluster, nothing will happen.
        /// </summary>
        /// <param name="node">Given node</param>
        /// <returns>Awaitable unitask</returns>
        public async UniTask ShowRecommendations(Node node)
        {
            await UniTask.WaitWhile(() => ProcessingEvents);
            IEnumerable<MappingPair> recommendations = this.CandidateRecommendation.GetRecommendations(node);
            IEnumerable<Node> recommendedNodes = recommendations.Select(m => node.IsInImplementation() ? m.Cluster : m.Candidate);
            List<NodeOperator> nodeOperators = new List<NodeOperator>();
            recommendedNodes.ForEach(n => nodeOperators.Add(n.GameObject().AddOrGetComponent<NodeOperator>()));
            if (recommendedNodes.Count() > 0)
            {
                nodeOperators.Add(node.GameObject().AddOrGetComponent<NodeOperator>()); 
            }
            await StartBlinkEffect(nodeOperators);
        }

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

        #endregion

        #region DebugAndDeveloperButtons

        /// <summary>
        /// Starts the recording of the mapping process within the visualized reflexion graph 
        /// to a .csv file located within the output path of the recommendation settings.
        /// </summary>
        [Button(startRecordingLabel, ButtonSizes.Small)]
        [ButtonGroup(statisticButtonGroup)]
        public void StartRecording()
        {
            string csvFile = Path.Combine(this.RecommendationSettings.OutputPath.Path, this.RecommendationSettings.ExperimentName + ".csv");
            CandidateRecommendation.Statistics.StartRecording(csvFile);
        }

        [Button("Log Oracle", ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        private void LogOracle()
        {
            Dictionary<Node, HashSet<Node>> initialMapping = new Dictionary<Node, HashSet<Node>>();

            List<Node> candidates = CandidateRecommendation.GetCandidates();

            int unknownCandidates = 0;
            foreach(Node candidate in candidates)
            {
                string expectedCluster = CandidateRecommendation.GetExpectedClusterID(candidate.ID);

                if (expectedCluster == null)
                {
                    Debug.LogWarning($"There is no information where to map node {candidate.ID} within the oracle graph.");
                    unknownCandidates++;
                } 
                else
                {
                    Debug.Log($"Candidate {candidate.ID} expected to map to {expectedCluster}");
                }
            }

            UnityEngine.Debug.Log($"There are {unknownCandidates} unknown candidates of {candidates.Count} candidates.");          
        }

        /// <summary>
        /// Stops the recording of the mapping process.
        /// </summary>
        [Button(stopRecordingLabel, ButtonSizes.Small)]
        [ButtonGroup(statisticButtonGroup)]
        public void StopRecording()
        {
            CandidateRecommendation.Statistics.StopRecording();
        }

        /// <summary>
        /// Processes the data recorded between the call of <see cref="StartRecording"/> and <see cref="StopRecording"/>
        /// </summary>
        [Button(calculateResultsLabel, ButtonSizes.Small)]
        [ButtonGroup(statisticButtonGroup)]
        public void CalculateResults()
        {
            try
            {
                string csvFile = CandidateRecommendation.Statistics.csvFile;
                string xmlFile = Path.Combine(this.RecommendationSettings.OutputPath.Path, this.RecommendationSettings.ExperimentName + ".xml");
                CandidateRecommendation.Statistics.StopRecording();
                UnityEngine.Debug.Log($"Write results to {xmlFile}...");
                CandidateRecommendation.Statistics.WriteResultsToXml(csvFile, xmlFile);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        [Button(showMappingChoicesLabel, ButtonSizes.Small)]
        [ButtonGroup(mappingButtonGroup)]
        public void ShowMappingChoices()
        {
            
        }

        /// <summary>
        /// This method generates tow .gxl files describing a oracle mapping and a architecture 
        /// based on a instructions .txt file. 
        /// The instruction file is expected to be located within the same folder as the configuration 
        /// file currently loaded within the reflexion city.
        /// </summary>
        /// <exception cref="Exception">Throws if the reflexion city could not be retrieved.</exception>
        [Button(createOracleMappingLabel, ButtonSizes.Small)]
        [ButtonGroup(gxlButtonGroup)]
        public void CreateOracleGXLs()
        {
            string configPath = GetConfigPath();

            string instructions = Path.Combine(configPath, "oracleInstructions.txt");

            if(this.reflexionGraphViz == null)
            {
                UnityEngine.Debug.LogWarning("Reflexiongraph is null. Cannot generate an oracle mapping.");
                return;
            }

            (Graph implementation, _, _) = this.reflexionGraphViz.Disassemble();
            ReflexionGraph oracleGraph = GenerateOracleGraph(implementation, instructions);
            (_, Graph architecture, Graph mapping) = oracleGraph.Disassemble();
            string architectureGxl = Path.Combine(configPath, "Architecture.gxl");
            string oracleMappingGxl = Path.Combine(configPath, "OracleMapping.gxl");
            GraphWriter.Save(oracleMappingGxl, mapping, AbstractSEECity.HierarchicalEdgeTypes().First());
            GraphWriter.Save(architectureGxl, architecture, AbstractSEECity.HierarchicalEdgeTypes().First());
            UnityEngine.Debug.Log($"Saved oracle mapping to {oracleMappingGxl}");
            UnityEngine.Debug.Log($"Saved architecture to {architectureGxl}");
        }

        [Button(dumpSystemStatisticsLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public async UniTask Generate()
        {
            await this.ResetMappingAsync();
            await this.CreateInitialMappingAsync(1, 245345, syncWithView: false);

            int CadidatesTotal = this.ReflexionGraphVisualized.Nodes().Where(n => CandidateRecommendation.IsCandidate(n)).Count();
            int ClusterTotal = this.ReflexionGraphVisualized.Nodes().Where(n => CandidateRecommendation.IsCluster(n)).Count();
            int archDependencies = this.ReflexionGraphVisualized.Edges().Where(e => e.IsInArchitecture()).Count();
            int convergentEdges = this.ReflexionGraphVisualized.Edges().Where(e => e.IsInImplementation() && e.State() == State.Allowed).Count();
            int impliticlyConvergentEdges = this.ReflexionGraphVisualized.Edges().Where(e => e.IsInImplementation() && e.State() == State.ImplicitlyAllowed).Count();
            int divergentEdges = this.ReflexionGraphVisualized.Edges().Where(e => e.IsInImplementation() && e.State() == State.Divergent).Count();
            int Edges = this.ReflexionGraphVisualized.Edges().Where(e => e.IsInImplementation()).Count();

            string path = this.GetConfigPath();

            // Define CSV headers
            var headers = new[] { "CadidatesTotal", "ClusterTotal", "archDependencies", "convergentEdges", "impliticlyConvergentEdges", "divergentEdges", "Edges" };

            // Define CSV data
            var data = new[] {
            CadidatesTotal.ToString(),
            ClusterTotal.ToString(),
            archDependencies.ToString(),
            convergentEdges.ToString(),
            impliticlyConvergentEdges.ToString(),
            divergentEdges.ToString(),
            Edges.ToString()
            };

            // Create CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine(string.Join(",", headers));
            csvContent.AppendLine(string.Join(",", data));

            // Write CSV to file
            await File.WriteAllTextAsync(Path.Combine(path, "SystemStatistics.csv"), csvContent.ToString());
        }

        [Button(createMappingGXLMappingLabel, ButtonSizes.Small)]
        [ButtonGroup(gxlButtonGroup)]
        public void CreateMappingGXL()
        {
            string configPath = GetConfigPath();

            string instructions = Path.Combine(configPath, "mappingInstructions.txt");

            if (this.reflexionGraphViz == null)
            {
                UnityEngine.Debug.LogWarning("Reflexiongraph is null. Cannot generate an oracle mapping.");
                return;
            }

            ReflexionGraph clone = new ReflexionGraph(this.reflexionGraphViz);
            clone.ResetMapping();
            Graph mapping = GenerateInitialMappingGxl(clone, instructions);

            string mappingGxl = Path.Combine(configPath, "Mapping.gxl");
            GraphWriter.Save(mappingGxl, mapping, AbstractSEECity.HierarchicalEdgeTypes().First());
            UnityEngine.Debug.Log($"Saved Mapping.gxl to {mapping}");
        }

        /// <summary>
        /// Gets the path where the loaded Reflexion.cfg is located.
        /// </summary>
        /// <returns>The location of the loaded Reflexion.cfg</returns>
        /// <exception cref="Exception">Throws if the city could not be qeueried.</exception>
        private string GetConfigPath()
        {
            GameObject codeCityObject = SceneQueries.GetCodeCity(this.transform)?.gameObject;

            if (codeCityObject == null)
            {
                throw new Exception("Could not get Reflexion city when loading oracle instructions.");
            }

            codeCityObject.TryGetComponent(out AbstractSEECity city);

            return Path.GetDirectoryName(city.ConfigurationPath.Path);
        }

        /// <summary>
        /// Generates an oracle reflexion graph for a given implementation graph and a string containing instructions.
        /// 
        /// The instruction file have to formatted like the following example:
        /// 
        /// 'cluster:
        /// [NEW_ARCH_NODE]{newline}
        /// [NEW_ARCH_NODE] ...
        /// relations:
        /// [NEW_ARCH_NODE],[NEW_ARCH_NODE]{newline}
        /// [NEW_ARCH_NODE],[NEW_ARCH_NODE] ...
        /// mapping:
        /// [IMPL_NODE],[NEW_ARCH_NODE]{newline}
        /// [IMPL_NODE],[NEW_ARCH_NODE]...'
        /// 
        /// </summary>
        /// <param name="implementation">given implementation graph</param>
        /// <param name="oracleInstructions">string containing instructions</param>
        /// <returns>An assembled oracle reflexion graph</returns>
        /// <exception cref="Exception">Throws if the instructions wrong</exception>
        public static ReflexionGraph GenerateOracleGraph(Graph implementation, string oracleInstructions)
        {
            string currentMode = string.Empty;

            Graph architecture = new Graph(implementation.BasePath, "Architecture");
            Graph oracleMapping = new Graph(implementation.BasePath, "OracleMapping");

            // Open the file for reading using StreamReader
            using (StreamReader sr = new StreamReader(oracleInstructions))
            {
                string line;
                // Read and display lines from the file until the end of the file is reached
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Replace(" ", "");

                    if (line.IsNullOrWhitespace())
                    {
                        break;
                    }

                    if (line.Contains(":"))
                    {
                        currentMode = line;
                        continue;
                    }

                    switch (currentMode)
                    {
                        case "cluster:":
                            AddCluster(architecture, line);
                            break;
                        case "relations:":
                            AddClusterRelation(architecture, line);
                            break;
                        case "mapping:":
                            AddOracleRelation(implementation, architecture, oracleMapping, line);
                            break;
                        default:
                            throw new Exception($"Unknown instruction mode when processing oracle instructions: {currentMode}");

                    }
                }
            }

            ReflexionGraph oracleGraph = new ReflexionGraph(implementation: implementation,
                                          architecture: architecture,
                                          mapping: oracleMapping);

            return oracleGraph;

            void AddCluster(Graph arch, string line)
            {
                UnityEngine.Debug.Log($"line='{line}'");
                Node cluster = new Node();
                cluster.ID = line;
                cluster.Type = "Cluster";
                arch.AddNode(cluster);
            }

            void AddClusterRelation(Graph arch, string line)
            {
                string[] nodes = line.Split(',');
                Node source = arch.GetNode(nodes[0]);
                Node target = arch.GetNode(nodes[1]);
                Edge edge = new Edge(source, target, "Source_Dependency");
                arch.AddEdge(edge);
            }

            void AddOracleRelation(Graph impl, Graph arch, Graph oracle, string line)
            {
                string[] nodes = line.Split(',');

                Node implNode = impl.GetNode(nodes[0]);
                Node archNode = arch.GetNode(nodes[1]);

                if(implNode == null)
                {
                    UnityEngine.Debug.LogWarning($"Node for id {nodes[0]} could not be found within Implementation. Skip the oracle relation: {line}");
                    return;
                }

                if (archNode == null)
                {
                    UnityEngine.Debug.LogWarning($"Node for id {nodes[1]} could not be found within Implementation. Skip the oracle relation: {line}");
                    return;
                }

                Node source = new Node();
                source.ID = implNode.ID;
                Node target = new Node();
                target.ID = archNode.ID;

                oracle.AddNode(source);

                if (oracle.GetNode(target.ID) == null)
                {
                    oracle.AddNode(target); 
                } 
                else
                {
                    target = oracle.GetNode(target.ID);
                }
                Edge edge = new Edge(source, target, "Maps_To");
                oracle.AddEdge(edge);
            }
        }

        /// <summary>
        /// This method is used to generate a initial mapping graph based on given node names, 
        /// a given graph and the loaded oracle graph.
        /// </summary>
        /// <param name="graph">the given graph</param>
        /// <param name="instructions">a string representing the nodes that should be mapped.</param>
        /// <returns>A mapping graph that contains the mapping between the specified nodes and their expected cluster</returns>
        private Graph GenerateInitialMappingGxl(ReflexionGraph graph, string instructions)
        {
            graph.ResetMapping();
            // Open the file for reading using StreamReader
            using (StreamReader sr = new StreamReader(instructions))
            {
                string nodeID;
                // Read and display lines from the file until the end of the file is reached
                while ((nodeID = sr.ReadLine()) != null)
                {
                    nodeID = nodeID.Replace(" ", "");

                    if (nodeID.IsNullOrWhitespace())
                    {
                        break;
                    }

                    if (graph.TryGetNode(nodeID, out Node node))
                    {
                        string expectedClusterID = this.CandidateRecommendation.GetExpectedClusterID(nodeID);
                        if (expectedClusterID != null)
                        {
                            if (graph.TryGetNode(expectedClusterID, out Node cluster))
                            {
                                graph.AddToMapping(node, cluster, overrideMapping: true);
                            } 
                            else
                            {
                                UnityEngine.Debug.Log($"");
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"No expected cluster found in OracleGraph for {nodeID}. The will be ignored and not contained within the mapping.");
                        }
                    } 
                    else
                    {
                        UnityEngine.Debug.Log($"Unknown node {nodeID}. The node will be ignored and not contained within the mapping.");
                    }
                }
            }

            (_, _, Graph mapping) = graph.Disassemble();

            return mapping;
        }

        /// <summary>
        /// Generates a text file containg a table showing the mapping of the current candidates 
        /// with their expected cluster regarding the loaded oracle graph. The file can be used 
        /// to check if the oracle mapping is complete or other debug purposes.
        /// </summary>
        [Button(dumpOracleLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void DumpOracle()
        {
            ReflexionGraph graph = CandidateRecommendation.ReflexionGraph;
            ReflexionGraph oracleGraph = CandidateRecommendation.OracleGraph;

            if (oracleGraph == null)
            {
                UnityEngine.Debug.Log("Could not generate Oracle mapping test. No Oracle Graph loaded.");
                return;
            }

            StringBuilder sb = new StringBuilder();

            IEnumerable<Node> nodes = graph.Nodes().Where(n => n.IsInImplementation());

            foreach (Node node in nodes)
            {
                sb.Append(node.ID.PadRight(120));
                sb.Append(" Maps To ".PadRight(20));
                Node oracleCluster = null;
                try
                {
                    UnityEngine.Debug.Log($"Try to retrieve expected cluster for node {node?.ID}...");
                    oracleCluster = oracleGraph.MapsTo(node);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    continue;
                }

                string oracleClusterID = oracleCluster != null ? oracleCluster.ID : "UNKNOWN";
                sb.Append(oracleClusterID.PadRight(20));
                string type = $"Type:{node.Type}";
                sb.Append(type.PadRight(35));
                sb.Append(Environment.NewLine);
            }

            string output = sb.ToString();
            string configPath = GetConfigPath();
            string outputFile = Path.Combine(configPath, "oracle.txt");
            UnityEngine.Debug.Log($"Saved oracle dump to {outputFile}");

            File.WriteAllText(outputFile, output);
        }

        /// <summary>
        /// Logs the current training data hold by a loaded attract function to the console.
        /// </summary>
        [Button(dumbTrainingDataLabel, ButtonSizes.Small)]
        [ButtonGroup(debugButtonGroup)]
        public void DumpTrainingData()
        {
            Debug.Log(CandidateRecommendation.AttractFunction.DumpTrainingData());
        }

        // [Button(debugScenarioLabel, ButtonSizes.Small)]
        // [ButtonGroup(debugButtonGroup)]
        public void StartDebugScenario()
        {
            // Empty method for debugging
        }
        #endregion
    }
}
