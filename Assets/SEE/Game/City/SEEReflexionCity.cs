using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.GO;
using SEE.Utils;
using SEE.Tools.ReflexionAnalysis;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using DG.Tweening;
using SEE.Game.Operator;
using SEE.Game.UI.Notification;
using UnityEngine.Assertions;

namespace SEE.Game.City
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// NOTE: It is assumed the implementation and architecture graphs are not edited!
    /// TODO: We should allow changes, but trigger the respective incremental reflexion analysis methods.
    /// </summary>
    public class SEEReflexionCity : SEECity, Observer
    {
        /// <summary>
        /// The path to the GXL file containing the architecture graph data.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of GXL file for the architecture"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath GxlArchitecturePath = new FilePath();

        /// <summary>
        /// The path to the GXL file containing the mapping graph data.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of GXL file for the mapping from the implementation onto the architecture"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath GxlMappingPath = new FilePath();

        /// <summary>
        /// The path to the CSV file containing the architecture metric data.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of CSV file for the metrics of the architecture"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath CsvArchitecturePath = new FilePath();

        /// <summary>
        /// Name of this code city.
        /// </summary>
        public string CityName = "Reflexion Analysis";

        /// <summary>
        /// Reflexion analysis. Use this to make changes to the graph
        /// (such as mappings, hierarchies, and so on), <b>do not modify
        /// the underlying Graph directly!</b>
        /// </summary>
        [NonSerialized]
        public Reflexion Analysis;

        /// <summary>
        /// Root node of the implementation subgraph.
        /// </summary>
        private Node implementationRoot;

        /// <summary>
        /// Root node of the architecture subgraph.
        /// </summary>
        private Node architectureRoot;

        /// <summary>
        /// Root node of the implementation subgraph.
        /// </summary>
        public Node ImplementationRoot => implementationRoot;

        /// <summary>
        /// Root node of the architecture subgraph.
        /// </summary>
        public Node ArchitectureRoot => architectureRoot;

        /// <summary>
        /// List of <see cref="ChangeEvent"/>s received from the reflexion <see cref="Analysis"/>.
        /// Note that this list is constructed by using <see cref="ReflexionGraphTools.Incorporate"/>.
        /// </summary>
        private readonly List<ChangeEvent> Events = new List<ChangeEvent>();

        // TODO: Is this assumption (cities' child count > 0 <=> city drawn) alright to make?
        /// <summary>
        /// Returns true if this city has been drawn in the scene.
        /// </summary>
        private bool CityDrawn => gameObject.transform.childCount > 0;

        /// <summary>
        /// A queue of <see cref="ChangeEvent"/>s which were received from the analysis, but not yet handled.
        /// More specifically, these are intended to be handled after <see cref="DrawGraph"/> has been called.
        /// </summary>
        private readonly Queue<ChangeEvent> UnhandledEvents = new Queue<ChangeEvent>();

        /// <summary>
        /// All tweens which control edges' colors on reflexion changes.
        /// </summary>
        private readonly Dictionary<string, ICollection<Tween>> edgeTweens = new Dictionary<string, ICollection<Tween>>();
        
        /// <summary>
        /// All tweens which control nodes' movement and scale on reflexion changes.
        /// </summary>
        private readonly Dictionary<string, ICollection<Tween>> nodeTweens = new Dictionary<string, ICollection<Tween>>();

        /// <summary>
        /// Duration of any animation (edge movement, color change...) in seconds.
        /// </summary>
        private const float ANIMATION_DURATION = 0.5f;

        /// <summary>
        /// Ease function of any animation (edge movement, color change...).
        /// </summary>
        private const Ease ANIMATION_EASE = Ease.OutExpo;

        /// <summary>
        /// First, if a graph was already loaded, everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override void LoadData()
        {
            if (string.IsNullOrEmpty(GxlArchitecturePath.Path))
            {
                Debug.LogError("Architecture graph path is empty.\n");
            }
            else if (string.IsNullOrEmpty(GXLPath.RootPath))
            {
                Debug.LogError("Implementation graph path is empty.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }

                LoadAllGraphs().Forget(); // needs to be async call due to metric retrieval
            }

            #region Local Functions

            async UniTaskVoid LoadAllGraphs()
            {
                Graph ArchitectureGraph = LoadGraph(GxlArchitecturePath.Path, "");
                Graph ImplementationGraph = LoadGraph(GXLPath.Path, "");
                Graph MappingGraph;
                if (string.IsNullOrEmpty(GxlMappingPath.Path))
                {
                    Debug.LogWarning("Mapping graph path is empty. Will create new mapping from scratch.\n");
                    /// The mapping graph may contain nodes and edges from the implementation. Possibly, their
                    /// <see cref="GraphElement.AbsolutePlatformPath()"/> will be retrieved. That is why we
                    /// will set the base path to <see cref="ProjectPath.Path"/>.
                    MappingGraph = new Graph(SourceCodeDirectory.Path);
                }
                else
                {
                    MappingGraph = LoadGraph(GxlMappingPath.Path, "");
                }

                // We collect the tasks here so we can wait on them both at the same time instead of sequentially
                IList<UniTask> tasks = new List<UniTask>();
                if (!string.IsNullOrEmpty(CsvArchitecturePath.Path))
                {
                    tasks.Add(LoadGraphMetrics(ArchitectureGraph, CsvArchitecturePath.Path, ErosionSettings));
                }

                if (!string.IsNullOrEmpty(CsvArchitecturePath.Path))
                {
                    tasks.Add(LoadGraphMetrics(ArchitectureGraph, CsvArchitecturePath.Path, ErosionSettings));
                }

                await UniTask.WhenAll(tasks);

                LoadedGraph = Assemble(ArchitectureGraph, ImplementationGraph, MappingGraph, CityName, out architectureRoot, out implementationRoot);
                Debug.Log($"Loaded graph {LoadedGraph.Name}.\n");
                Events.Clear();
                Analysis = new Reflexion(LoadedGraph);
                Analysis.Register(this);
                Analysis.Run();
                Debug.Log("Initialized Reflexion Analysis.\n");
            }

            #endregion
        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public override void DrawGraph()
        {
            base.DrawGraph();
            while (UnhandledEvents.Count > 0)
            {
                HandleChange(UnhandledEvents.Dequeue());
            }
        }

        /// <summary>
        /// Saves implementation, architecture, and mapping graphs as GXL.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderSave)]
        public override void SaveData()
        {
            IList<string> NoPathGraphs = new[]
            {
                GxlArchitecturePath.Path, GXLPath.Path, GxlMappingPath.Path
            }.Where(string.IsNullOrEmpty).ToList();
            if (NoPathGraphs.Count > 0)
            {
                Debug.LogError($"Couldn't find any graph at path{(NoPathGraphs.Count > 1 ? "s" : "")} " +
                               string.Join(", ", NoPathGraphs) + ".\n");
            }
            else
            {
                string hierarchicalType = HierarchicalEdges.First();
                (Graph implementation, Graph architecture, Graph mapping) = LoadedGraph.Disassemble();
                GraphWriter.Save(GxlArchitecturePath.Path, architecture, hierarchicalType);
                Debug.Log($"Architecture graph saved at {GxlArchitecturePath.Path}.\n");
                GraphWriter.Save(GXLPath.Path, implementation, hierarchicalType);
                Debug.Log($"Implementation graph saved at {GXLPath.Path}.\n");
                GraphWriter.Save(GxlMappingPath.Path, mapping, hierarchicalType);
                Debug.Log($"Mapping graph saved at {GxlMappingPath.Path}.\n");
            }
        }

        internal override void Start()
        {
            base.Start();

            foreach (Edge edge in LoadedGraph.Edges())
            {
                GameObject edgeObject = GameObject.Find(edge.ID);
                if (edgeObject != null && edgeObject.TryGetComponent(out SEESpline spline))
                {
                    spline.GradientColors = GetEdgeGradient(edge.State());
                }
                else
                {
                    Debug.LogError($"Edge has no associated game object: {edge}\n");
                }
            }
        }

        #region Configuration file input/output

        /// <summary>
        /// Label of attribute <see cref="GxlArchitecturePath"/> in the configuration file.
        /// </summary>
        private const string GxlArchitectureLabel = "ArchitectureGXL";

        /// <summary>
        /// Label of attribute <see cref="GxlMappingPath"/> in the configuration file.
        /// </summary>
        private const string GxlMappingLabel = "MappingGXL";

        /// <summary>
        /// Label of attribute <see cref="CsvArchitecturePath"/> in the configuration file.
        /// </summary>
        private const string CsvArchitectureLabel = "ArchitectureCSV";

        /// <summary>
        /// Label of attribute <see cref="CityName"/> in the configuration file.
        /// </summary>
        private const string CityNameLabel = "CityName";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            GxlArchitecturePath.Save(writer, GxlArchitectureLabel);
            GxlMappingPath.Save(writer, GxlMappingLabel);
            CsvArchitecturePath.Save(writer, CsvArchitectureLabel);
            writer.Save(CityName, CityNameLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            GxlArchitecturePath.Restore(attributes, GxlArchitectureLabel);
            GxlMappingPath.Restore(attributes, GxlMappingLabel);
            CsvArchitecturePath.Restore(attributes, CsvArchitectureLabel);
            ConfigIO.Restore(attributes, CityNameLabel, ref CityName);
        }

        #endregion

        /// <summary>
        /// Returns a fitting color gradient from the first to the second color for the given state.
        /// </summary>
        /// <param name="state">state for which to yield a color gradient</param>
        /// <returns>color gradient</returns>
        private static (Color, Color) GetEdgeGradient(State state) =>
            state switch
            {
                State.Undefined => (Color.black, Color.Lerp(Color.gray, Color.black, 0.9f)),
                State.Specified => (Color.gray, Color.Lerp(Color.gray, Color.black, 0.5f)),
                State.Unmapped => (Color.gray, Color.Lerp(Color.gray, Color.black, 0.5f)),
                State.ImplicitlyAllowed => (Color.green, Color.white),
                State.AllowedAbsent => (Color.green, Color.white),
                State.Allowed => (Color.green, Color.white),
                State.Divergent => (Color.magenta, Color.Lerp(Color.magenta, Color.black, 0.5f)),
                State.Absent => (Color.red, Color.Lerp(Color.red, Color.black, 0.5f)),
                State.Convergent => (Color.green, Color.Lerp(Color.green, Color.black, 0.5f)),
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown state!")
            };

        /// <summary>
        /// Incorporates the given <paramref name="changeEvent"/> into <see cref="Events"/>, logs it to the console,
        /// and handles the changes by modifying this city.
        /// </summary>
        /// <param name="changeEvent">The change event received from the reflexion analysis</param>
        public void HandleChange(ChangeEvent changeEvent)
        {
            if (!CityDrawn)
            {
                UnhandledEvents.Enqueue(changeEvent);
                return;
            }

            // TODO: Make sure these actions don't interfere with reversible actions.
            // TODO: Send these changes over the network? Maybe not the edges themselves, but the events?
            // TODO: Handle these asynchronously
            
            switch (changeEvent)
            {
                case EdgeChange edgeChange:
                    HandleEdgeChange(edgeChange);
                    break;
                case EdgeEvent edgeEvent:
                    HandleEdgeEvent(edgeEvent);
                    break;
                case HierarchyChangeEvent hierarchyChangeEvent:
                    HandleHierarchyChangeEvent(hierarchyChangeEvent);
                    break;
                case NodeChangeEvent nodeChangeEvent:
                    HandleNodeChangeEvent(nodeChangeEvent);
                    break;
                case PropagatedEdgeEvent propagatedEdgeEvent:
                    HandlePropagatedEdgeEvent(propagatedEdgeEvent);
                    break;
            }

            Events.Incorporate(changeEvent);
        }

        private void HandleEdgeChange(EdgeChange edgeChange)
        {
            Debug.Log(edgeChange);
            GameObject edge = GameObject.Find(edgeChange.Edge.ID);
            if (edge == null)
            {
                // If no such edge can be found, the given edge must be propagated
                string edgeId = Analysis.GetOriginatingEdge(edgeChange.Edge)?.ID;
                edge = edgeId != null ? GameObject.Find(edgeId) : null;
            }

            if (edge != null && edge.TryGetComponent(out SEESpline spline))
            {
                (Color start, Color end) newColors = GetEdgeGradient(edgeChange.NewState);
                // Animate color change for nicer visuals.
                // We need two tweens for this, one for each end of the gradient.
                Tween startTween = DOTween.To(() => spline.GradientColors.start,
                                              c => spline.GradientColors = (c, spline.GradientColors.end),
                                              newColors.start, ANIMATION_DURATION).SetEase(ANIMATION_EASE);
                Tween endTween = DOTween.To(() => spline.GradientColors.end,
                                            c => spline.GradientColors = (spline.GradientColors.start, c),
                                            newColors.end, ANIMATION_DURATION).SetEase(ANIMATION_EASE);
                
                // If there are existing tweens, remove them.
                ICollection<Tween> tweens = CleanTweens(edgeTweens, edgeChange.Edge.ID);
                
                // Pressing `Play` will have an effect at the next frame, so they will be played simultaneously.
                tweens.Add(startTween.Play());
                tweens.Add(endTween.Play());
            }
            else
            {
                Debug.LogError($"Couldn't find edge {edgeChange.Edge}, whose state changed "
                               + $"from {edgeChange.OldState} to {edgeChange.NewState}!");
            }
        }

        private void HandleEdgeEvent(EdgeEvent edgeEvent)
        {
            if (edgeEvent.Change == ChangeType.Addition)
            {
                if (edgeEvent.Affected == ReflexionSubgraph.Mapping)
                {
                    HandleNewMapping(edgeEvent.Edge);
                }
                else
                {
                    // FIXME: Handle edge additions other than new mapping
                }
            }

            if (edgeEvent.Change == ChangeType.Removal)
            {
                if (edgeEvent.Affected == ReflexionSubgraph.Mapping)
                {
                    HandleRemovedMapping(edgeEvent.Edge);
                }
            }
        }

        private void HandleRemovedMapping(Edge mapsToEdge)
        {
            ShowNotification.Info("Reflexion Analysis", $"Unmapping node '{mapsToEdge.Source.ToShortString()}'.");
            Node implNode = mapsToEdge.Source;
            GameObject implGameNode = implNode.RetrieveGameNode();
            implGameNode.AddOrGetComponent<NodeOperator>().UpdateAttachedEdges(ANIMATION_DURATION);
        }

        private void HandleNewMapping(Edge mapsToEdge)
        {
            ShowNotification.Info("Reflexion Analysis", $"Mapping node '{mapsToEdge.Source.ToShortString()}' "
                                                        + $"onto '{mapsToEdge.Target.ToShortString()}'.");
            // Maps-To edges must not be drawn, as we will visualize mappings differently.
            mapsToEdge.SetToggle(Edge.IsVirtualToggle);

            Node implNode = mapsToEdge.Source;
            GameObject archGameNode = mapsToEdge.Target.RetrieveGameNode();
            GameObject implGameNode = implNode.RetrieveGameNode();

            Vector3 oldPosition = implGameNode.transform.position;

            // TODO: Rather than returning the old scale from PutOn, lossyScale should be used.
            Vector3 oldScale = GameNodeMover.PutOn(implGameNode.transform, archGameNode, scaleDown: true, topPadding: 0.3f);
            Vector3 newPosition = implGameNode.transform.position;
            Vector3 newScale = implGameNode.transform.localScale;
            implGameNode.transform.position = oldPosition;
            implGameNode.transform.localScale = oldScale;
            NodeOperator nodeOperator = implGameNode.AddOrGetComponent<NodeOperator>();
            nodeOperator.MoveNodeY(newPosition.y, ANIMATION_DURATION);
            nodeOperator.ScaleNode(newScale, ANIMATION_DURATION);
        }

        private void HandleHierarchyChangeEvent(HierarchyChangeEvent hierarchyChangeEvent)
        {
            // FIXME: Handle event
        }

        private void HandleNodeChangeEvent(NodeChangeEvent nodeChangeEvent)
        {
            // FIXME: Handle event
        }

        private void HandlePropagatedEdgeEvent(PropagatedEdgeEvent propagatedEdgeEvent)
        {
            // FIXME: Handle event
        }

        /// <summary>
        /// Goes through all tweens in the <paramref name="tweenDictionary"/> under the given
        /// <paramref name="cleanKey"/>, kills them if they're still active, and clears them all out of the dictionary.
        /// </summary>
        /// <param name="tweenDictionary">The dictionary in which <paramref name="cleanKey"/> shall be "cleaned".</param>
        /// <param name="cleanKey">The key whose tweens shall be killed and removed.</param>
        /// <param name="complete">Whether to set the tween to its target value before killing it</param>
        /// <typeparam name="T">Type of the key in <paramref name="tweenDictionary"/>.</typeparam>
        /// <returns>The newly created empty list within <paramref name="tweenDictionary"/>.</returns>
        private static ICollection<Tween> CleanTweens<T>(IDictionary<T, ICollection<Tween>> tweenDictionary, T cleanKey, bool complete = false)
        {
            // Clean out old tweens while killing them
            if (tweenDictionary.ContainsKey(cleanKey))
            {
                foreach (Tween tween in tweenDictionary[cleanKey])
                {
                    if (tween.IsActive())
                    {
                        tween.Kill(complete);
                    }
                }
            }
            return tweenDictionary[cleanKey] = new List<Tween>();
        }

        public void KillNodeTweens(Node node, bool complete = true)
        {
            Debug.Log($"Killing tweens for {node.ID}");
            CleanTweens(nodeTweens, node.ID, complete);
            
            // We will also kill any connected edge tweens.
            foreach (Edge edge in node.Incomings.Concat(node.Outgoings))
            {
                CleanTweens(edgeTweens, edge.ID, complete);
            }
        }
    }
}