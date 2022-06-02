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
using DG.Tweening;
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
        /// The path to the GXL file containing the implementation graph data.
        /// </summary>
        public DataPath GxlImplementationPath = new DataPath();

        /// <summary>
        /// The path to the GXL file containing the architecture graph data.
        /// </summary>
        public DataPath GxlArchitecturePath = new DataPath();

        /// <summary>
        /// The path to the GXL file containing the mapping graph data.
        /// </summary>
        public DataPath GxlMappingPath = new DataPath();

        /// <summary>
        /// The path to the CSV file containing the implementation metric data.
        /// </summary>
        public DataPath CsvImplementationPath = new DataPath();

        /// <summary>
        /// The path to the CSV file containing the architecture metric data.
        /// </summary>
        public DataPath CsvArchitecturePath = new DataPath();

        /// <summary>
        /// Name of this code city.
        /// </summary>
        public string CityName = "Reflexion Analysis";

        /// <summary>
        /// Reflexion analysis. Use this to make changes to the graph
        /// (such as mappings, hierarchies, and so on), <b>do not modify
        /// the underlying Graph directly!</b>
        /// </summary>
        public Reflexion Analysis;

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
        /// Duration of any animation (edge movement, color change...) in seconds.
        /// </summary>
        private const float ANIMATION_DURATION = 2.0f;

        /// <summary>
        /// Ease function of any animation (edge movement, color change...).
        /// </summary>
        private const Ease ANIMATION_EASE = Ease.InOutExpo;

        /// <summary>
        /// First, if a graph was already loaded, everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        public override void LoadData()
        {
            if (string.IsNullOrEmpty(GxlArchitecturePath.Path))
            {
                Debug.LogError("Architecture graph path is empty.\n");
            }
            else if (string.IsNullOrEmpty(GxlImplementationPath.RootPath))
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
                Graph ImplementationGraph = LoadGraph(GxlImplementationPath.Path, "");
                Graph MappingGraph;
                if (string.IsNullOrEmpty(GxlMappingPath.Path))
                {
                    Debug.LogWarning("Mapping graph path is empty. Will create new mapping from scratch.\n");
                    /// The mapping graph may contain nodes and edges from the implementation. Possibly, their
                    /// <see cref="GraphElement.AbsolutePlatformPath()"/> will be retrieved. That is why we
                    /// will set the base path to <see cref="ProjectPath.Path"/>.
                    MappingGraph = new Graph(ProjectPath.Path);
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

                LoadedGraph = Assemble(ArchitectureGraph, ImplementationGraph, MappingGraph, CityName);
                Debug.Log($"Loaded graph {LoadedGraph.Name}.\n");
                Events.Clear();
                Analysis = new Reflexion(LoadedGraph);
                Analysis.Register(this);
                Analysis.Run();
                Debug.Log("Initialized Reflexion Analysis.\n");
            }

            #endregion
        }

        public override void DrawGraph()
        {
            base.DrawGraph();
            while (UnhandledEvents.Count > 0)
            {
                HandleChange(UnhandledEvents.Dequeue());
            }
        }

        public override void SaveData()
        {
            IList<string> NoPathGraphs = new[]
            {
                GxlArchitecturePath.Path, GxlImplementationPath.Path, GxlMappingPath.Path
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
                GraphWriter.Save(GxlImplementationPath.Path, implementation, hierarchicalType);
                Debug.Log($"Implementation graph saved at {GxlImplementationPath.Path}.\n");
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
        /// Label of attribute <see cref="GxlImplementationPath"/> in the configuration file.
        /// </summary>
        private const string GxlImplementationLabel = "ImplementationGXL";

        /// <summary>
        /// Label of attribute <see cref="GxlMappingPath"/> in the configuration file.
        /// </summary>
        private const string GxlMappingLabel = "MappingGXL";

        /// <summary>
        /// Label of attribute <see cref="CsvArchitecturePath"/> in the configuration file.
        /// </summary>
        private const string CsvArchitectureLabel = "ArchitectureCSV";

        /// <summary>
        /// Label of attribute <see cref="CsvImplementationPath"/> in the configuration file.
        /// </summary>
        private const string CsvImplementationLabel = "ImplementationCSV";

        /// <summary>
        /// Label of attribute <see cref="CityName"/> in the configuration file.
        /// </summary>
        private const string CityNameLabel = "CityName";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            GxlArchitecturePath.Save(writer, GxlArchitectureLabel);
            GxlImplementationPath.Save(writer, GxlImplementationLabel);
            GxlMappingPath.Save(writer, GxlMappingLabel);
            CsvArchitecturePath.Save(writer, CsvArchitectureLabel);
            CsvImplementationPath.Save(writer, CsvImplementationLabel);
            writer.Save(CityName, CityNameLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            GxlArchitecturePath.Restore(attributes, GxlArchitectureLabel);
            GxlImplementationPath.Restore(attributes, GxlImplementationLabel);
            GxlMappingPath.Restore(attributes, GxlMappingLabel);
            CsvArchitecturePath.Restore(attributes, CsvArchitectureLabel);
            CsvImplementationPath.Restore(attributes, CsvImplementationLabel);
            ConfigIO.Restore(attributes, CityNameLabel, ref CityName);
        }

        #endregion

        // Returns a fitting color gradient from the first to the second color for the given state.
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
                // Pressing `Play` will have an effect at the next frame, so they will be played simultaneously.
                startTween.Play();
                endTween.Play();
            }
            else
            {
                Debug.LogError($"Couldn't find edge {edgeChange.Edge}!");
            }
        }

        private void HandleEdgeEvent(EdgeEvent edgeEvent)
        {
            if (edgeEvent.Change == ChangeType.Addition)
            {
                if (edgeEvent.Affected == ReflexionSubgraph.Mapping)
                {
                    // Maps-To edges must not be drawn, as we will visualize mappings differently.
                    edgeEvent.Edge.SetToggle(Edge.IsVirtualToggle);

                    Edge mapsToEdge = edgeEvent.Edge;
                    HandleNewMapping(mapsToEdge);
                }
                else
                {
                    // FIXME: Handle edge additions other than new mapping
                }
            }

            if (edgeEvent.Change == ChangeType.Removal)
            {
                // FIXME: Handle edge removal
            }
        }

        private static void HandleNewMapping(Edge mapsToEdge)
        {
            Node implNode = mapsToEdge.Source;
            GameObject archGameNode = mapsToEdge.Target.RetrieveGameNode();
            GameObject implGameNode = implNode.RetrieveGameNode();

            // Move implementation node to architecture node, sizing it down accordingly.
            // TODO: Handle children as well, if that's necessary?
            Vector3 oldPosition = implGameNode.transform.position;
            Vector3 newPosition = archGameNode.transform.position;
            implGameNode.transform.position = newPosition;
            GameNodeMover.PutOn(implGameNode.transform, archGameNode);
            newPosition = implGameNode.transform.position;
            // Mapped node should be half its parent's size
            implGameNode.transform.DOScale(new Vector3(0.5f, 0.5f, 0.5f), ANIMATION_DURATION);

            // Recalculate edge layout and animate edges due to new node positioning.
            // TODO: Iterating over all game edges is currently very costly,
            //       consider adding a cached mapping either here or in SceneQueries.
            //       Alternatively, we can iterate over game edges instead.
            foreach (Edge edge in implNode.Incomings.Union(implNode.Outgoings).Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
            {
                GameObject gameEdge = GameObject.Find(edge.ID);
                Assert.IsNotNull(gameEdge);
                GameObject source = edge.Source == implNode ? implGameNode : edge.Source.RetrieveGameNode();
                GameObject target = edge.Target == implNode ? implGameNode : edge.Target.RetrieveGameNode();
                GameObject newEdge = GameEdgeAdder.Add(source, target, edge.Type, edge);
                AddEdgeSplineAnimation(gameEdge, newEdge);
            }
            
            // Movement of the node should be animated as well. For this, we have to reset its position first.
            implGameNode.transform.position = oldPosition;
            implGameNode.transform.DOMove(newPosition, ANIMATION_DURATION).SetEase(ANIMATION_EASE).Play();

            #region Local Methods

            static void AddEdgeSplineAnimation(GameObject sourceEdge, GameObject targetEdge)
            {
                if (targetEdge.TryGetComponentOrLog(out SEESpline splineTarget)
                    && sourceEdge.TryGetComponentOrLog(out SEESpline splineSource))
                {
                    // We deactivate the target edge first so it's not visible.
                    targetEdge.SetActive(false);
                    // We now use the EdgeAnimator and SplineMorphism to actually move the edge.
                    SplineMorphism morphism = splineSource.gameObject.AddComponent<SplineMorphism>();
                    morphism.CreateTween(splineSource.Spline, splineTarget.Spline, ANIMATION_DURATION)
                            .OnComplete(() => Destroy(targetEdge)).SetEase(ANIMATION_EASE).Play();
                }
            }

            #endregion
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
    }
}