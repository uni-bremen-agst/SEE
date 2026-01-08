using SEE.DataModel.DG;
using SEE.Game.Charts;
using SEE.Game.City;
using SEE.UI.Notification;
using SEE.Layout;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using SEE.Game.CityRendering;
using SEE.UI;
using SEE.Layout.NodeLayouts;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Renders the evolution of the graph series through animations. Incrementally updates
    /// the graph (removal/addition of nodes/edges).
    ///
    /// Note: The renderer is a MonoBehaviour, thus, will be added as a component to a game
    /// object. As a consequence, a constructor will not be called and is meaningless.
    ///
    /// Assumption: This EvolutionRenderer is attached to a game object representing a code
    /// city that has another component of type <see cref="SEECityEvolution"/>.
    /// </summary>
    public class EvolutionRenderer : MonoBehaviour, IGraphRenderer
    {
        /// <summary>
        /// The delay in seconds before starting the auto-play transition to the next graph.
        /// </summary>
        private float AutoPlayDelay => cityEvolution.AutoPlayDelay;

        /// <summary>
        /// The city evolution component attached to the same game object.
        /// It will be used to create the graph renderer and to retrieve visual attributes.
        /// </summary>
        private SEECityEvolution cityEvolution;

        /// <summary>
        /// The series of underlying graphs to be rendered.
        /// </summary>
        private IList<Graph> graphs;

        /// <summary>
        /// The number of graphs of the graph series to be rendered.
        /// </summary>
        public int GraphCount => graphs.Count;

        /// <summary>
        /// Current graph of the graph series to be rendered.
        /// </summary>
        public Graph GraphCurrent => graphs[currentGraphIndex];

        /// <summary>
        /// The index of the currently visualized graph.
        /// </summary>
        private int currentGraphIndex = 0;

        /// <summary>
        /// Returns the index of the currently shown graph.
        /// </summary>
        public int CurrentGraphIndex
        {
            get => currentGraphIndex;
            private set
            {
                currentGraphIndex = value;
                shownGraphHasChangedEvent.Invoke();
            }
        }

        /// <summary>
        /// The message to be displayed while rendering the evolution city.
        /// </summary>
        private string LoadingMessage => $"Rendering evolution city {gameObject.name}...";

        /// <summary>
        /// An event fired when the view graph has changed.
        /// </summary>
        private readonly UnityEvent shownGraphHasChangedEvent = new();

        /// <summary>
        /// Registers <paramref name="action"/> to be called back when the shown
        /// graph has changed.
        /// </summary>
        /// <param name="action">Action to be called back.</param>
        internal void RegisterOnNewGraph(UnityAction action)
        {
            shownGraphHasChangedEvent.AddListener(action);
        }

        /// <summary>
        /// An event fired upon the end of an animation.
        /// </summary>
        private readonly UnityEvent animationFinishedEvent = new();

        /// <summary>
        /// True if an animation cycle is still ongoing. It is false, when all
        /// nodes and edges are at their final position.
        /// </summary>
        public bool IsStillAnimating { get; private set; }

        /// <summary>
        /// The default factor for the complete graph transition animation.
        /// </summary>
        private const float defaultAnimationFactor = 1.0f;

        /// <summary>
        /// A factor controlling the speed of an animation (higher = slower). This value can be controlled by the user.
        /// </summary>
        [FormerlySerializedAs("animationDuration")]
        [SerializeField]
        private float animationFactor = defaultAnimationFactor;

        /// <summary>
        /// The animation factor for showing a single graph revision during auto-play.
        /// </summary>
        public float AnimationLagFactor
        {
            get => animationFactor;
            set
            {
                if (value >= 0)
                {
                    animationFactor = value;
                    shownGraphHasChangedEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// The graph currently shown.
        /// </summary>
        private Graph currentCity;

        #region User Messages

        /// <summary>
        /// The title of user notifications used here.
        /// </summary>
        private const string notificationTitle = "City Evolution";

        /// <summary>
        /// Informs the user that graph transition is currently blocked.
        /// </summary>
        private static void UserInfoGraphTransitionIsBlocked()
        {
            ShowNotification.Info(notificationTitle, "Graph transition is blocked while animations are running.");
        }

        /// <summary>
        /// Informs the user that the renderer is currently occupied with an animation.
        /// </summary>
        private static void UserInfoStillOccupied()
        {
            ShowNotification.Info(notificationTitle, "The renderer is already occupied with animating, wait until animations are finished.");
        }

        /// <summary>
        /// Informs the user that we already reached the begin of the graph series.
        /// </summary>
        private static void UserInfoFirstGraph()
        {
            ShowNotification.Info(notificationTitle, "This is already the first graph revision.");
        }

        /// <summary>
        /// Informs the user that we already reached the end of the graph series.
        /// </summary>
        private static void UserInfoLastGraph()
        {
            ShowNotification.Info(notificationTitle, "This is already the last graph revision.");
        }

        /// <summary>
        /// Informs the user that auto play is on.
        /// </summary>
        private static void UserInfoAutoPlayIsOn()
        {
            ShowNotification.Info(notificationTitle, "Auto-play mode is turned on. You cannot move to the next graph manually.");
        }
        #endregion

        /// <summary>
        /// Sets the evolving series of <paramref name="graphs"/> to be visualized.
        /// The actual visualization is triggered by <see cref="ShowGraphEvolutionAsync"/>
        /// that can be called next.
        /// This method is expected to be called before attemption to draw any graph.
        /// </summary>
        /// <param name="graphs">Series of graphs to be visualized.</param>
        public void SetGraphEvolution(IList<Graph> graphs)
        {
            this.graphs = graphs;
            if (gameObject.TryGetComponent(out cityEvolution))
            {
                // A constructor with a parameter is meaningless for a class that derives from MonoBehaviour.
                // So we cannot make the following assignment in the constructor. Neither
                // can we assign this value at the declaration of graphRenderer because
                // we need the city argument, which comes only later. Anyhow, whenever we
                // assign a new city, we also need a new graph renderer for that city.
                // So in fact this is the perfect place to assign graphRenderer.
                Renderer = new GraphRenderer(cityEvolution, graphs);
                transitionRenderer = new TransitionRenderer(cityEvolution.MarkerAttributes);
            }
            else
            {
                Debug.LogError($"This {nameof(EvolutionRenderer)} attached to {name} has no sibling component of type {nameof(SEECityEvolution)}.\n");
                enabled = false;
            }
            Renderer.SetScaler(graphs);
        }

        /// <summary>
        /// Initiates the visualization of the evolving series of graphs
        /// provided earlier by <see cref="SetGraphEvolution(List{Graph})"/>
        /// (the latter function must have been called before).
        /// </summary>
        public async UniTask ShowGraphEvolutionAsync()
        {
            CurrentGraphIndex = 0;
            // We transfer from the empty graph to the first graph in the series.
            // All nodes and edges in that first graph are to be considered new.
            currentCity = null;

            if (graphs.Count > 0)
            {
                LoadingSpinner.ShowIndeterminate(LoadingMessage);
            }

            await DisplayGraphAsNewAsync(graphs[CurrentGraphIndex]);
            shownGraphHasChangedEvent.Invoke();
        }

        #region Transition between graphs in the series

        /// <summary>
        /// Displays the given graph instantly if all animations are finished. This is
        /// called when we jump directly to a specific graph in the graph series: when
        /// we start the visualization of the graph evolution initially and when the user
        /// selects a specific graph revision.
        /// The graph is drawn from scratch.
        /// </summary>
        /// <param name="graph">Graph to be drawn initially.</param>
        private async UniTask DisplayGraphAsNewAsync(Graph graph)
        {
            graph.AssertNotNull("graph");

            if (IsStillAnimating)
            {
                UserInfoGraphTransitionIsBlocked();
                return;
            }
            await RenderGraphAsync(currentCity, graph);
        }

        /// <summary>
        /// Starts the animations to transition from the <paramref name="current"/> graph
        /// to the <paramref name="next"/> graph.
        /// </summary>
        /// <param name="current">The currently shown graph.</param>
        /// <param name="next">The next graph to be shown.</param>
        private async UniTask TransitionToNextGraphAsync(Graph current, Graph next, float delay = 0f)
        {
            current.AssertNotNull("current");
            next.AssertNotNull("next");
            if (IsStillAnimating)
            {
                UserInfoGraphTransitionIsBlocked();
                return;
            }
            if (delay > 0f && InAutoPlayMode)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }
            await RenderGraphAsync(current, next);
        }

        /// <summary>
        /// If animations are still ongoing, auto-play mode is turned on, or <paramref name="index"/>
        /// does not denote a valid index in the graph series, false is returned and nothing else
        /// happens. Otherwise the graph with the given index in the graph series becomes the new
        /// currently shown graph.
        /// </summary>
        /// <param name="index">Index of the graph to be shown in the graph series.</param>
        /// <returns>True if that graph could be shown successfully.</returns>
        public bool TryShowSpecificGraph(int index)
        {
            if (IsStillAnimating)
            {
                UserInfoStillOccupied();
                return false;
            }
            if (InAutoPlayMode)
            {
                UserInfoAutoPlayIsOn();
                return false;
            }
            if (index < 0 || index >= GraphCount)
            {
                Debug.LogError($"The value {index} is no valid index.\n");
                return false;
            }
            CurrentGraphIndex = index;
            TransitionToNextGraphAsync(currentCity, graphs[index]).Forget();
            return true;
        }

        /// <summary>
        /// If animation is still ongoing, auto-play mode is turned on, or we are at
        /// the end of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct successor graph in the graph series.
        /// </summary>
        /// <remarks>This method is called upon a specific request by the user to
        /// move one graph further. It is not called in auto-play mode.</remarks>
        public async UniTask ShowNextGraphAsync()
        {
            if (IsStillAnimating)
            {
                UserInfoStillOccupied();
                return;
            }
            if (InAutoPlayMode)
            {
                UserInfoAutoPlayIsOn();
                return;
            }
            if (!HasNextGraph)
            {
                UserInfoLastGraph();
                return;
            }
            await ShowNextAsync();
        }

        /// <summary>
        /// If we are at the beginning of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct predecessor graph in the graph series. CurrentGraphIndex is decreased
        /// by one accordingly.
        /// </summary>
        /// <remarks>This method is called upon a specific request by the user to
        /// move one graph back. It is not called in auto-play mode.</remarks>
        public async UniTask ShowPreviousGraphAsync()
        {
            if (IsStillAnimating)
            {
                UserInfoStillOccupied();
                return;
            }
            if (InAutoPlayMode)
            {
                UserInfoAutoPlayIsOn();
                return;
            }
            if (!HasPreviousGraph)
            {
                UserInfoFirstGraph();
                return;
            }
            await ShowPreviousAsync();
        }

        /// <summary>
        /// True if there is a next graph in the series to be shown.
        /// </summary>
        private bool HasNextGraph => CurrentGraphIndex < graphs.Count - 1;

        /// <summary>
        /// True if there is a previous graph in the series to be shown.
        /// </summary>
        private bool HasPreviousGraph => CurrentGraphIndex > 0;

        /// <summary>
        /// If we are at the end of the graph series, false is returned and nothing else happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly.
        /// </summary>
        /// <returns>Task.</returns>
        private async UniTask ShowNextAsync()
        {
            Assert.IsTrue(HasNextGraph, "There is no next graph to be shown.");
            CurrentGraphIndex++;
            NodeChangesBuffer.Instance().RevisionChanged = true;
            await TransitionToNextGraphAsync(currentCity, graphs[CurrentGraphIndex], delay: AutoPlayDelay);
        }

        /// <summary>
        /// If we are at the beginning of the graph series, false is returned and nothing else happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct predecessor graph in the graph series. CurrentGraphIndex is decreased
        /// by one accordingly.
        /// </summary>
        /// <returns>Task.</returns>
        private async UniTask ShowPreviousAsync()
        {
            Assert.IsTrue(HasPreviousGraph, "There is no previous graph to be shown.");
            CurrentGraphIndex--;
            NodeChangesBuffer.Instance().RevisionChanged = true;
            await TransitionToNextGraphAsync(currentCity, graphs[CurrentGraphIndex], delay: AutoPlayDelay);
        }

        private TransitionRenderer transitionRenderer;

        /// <summary>
        /// Renders the animation from <paramref name="current"/> to <paramref name="next"/>.
        /// </summary>
        /// <param name="current">The graph currently shown that is to be migrated into the next graph; may be null.</param>
        /// <param name="next">The new graph to be shown, in which to migrate the current graph; must not be null.</param>
        private async UniTask RenderGraphAsync(Graph current, Graph next)
        {
            next.AssertNotNull("next");

            await UniTask.WaitUntil(() => !IsStillAnimating);

            IsStillAnimating = true;

            await transitionRenderer.RenderAsync(current, next, Renderer.AreEdgesDrawn(), gameObject, Renderer);

            LoadingSpinner.Hide(LoadingMessage);

            // We have made the transition to the next graph.
            currentCity = next;

            IsStillAnimating = false;
            animationFinishedEvent.Invoke();
        }

        #endregion

        #region auto play mode

        /// <summary>
        /// Possible states of the auto-play mode.
        /// </summary>
        private enum AutoPlayMode
        {
            /// <summary>
            /// Auto-play mode is turned off.
            /// </summary>
            Off,
            /// <summary>
            /// Auto-play mode is turned on for forward animations.
            /// </summary>
            Forward,
            /// <summary>
            /// Auto-play mode is turned on for reverse animations.
            /// </summary>
            Reverse
        }

        /// <summary>
        /// The current auto-play mode.
        /// </summary>
        private AutoPlayMode autoPlayMode = AutoPlayMode.Off;

        /// <summary>
        /// Returns true if automatic forward animations are active.
        /// </summary>
        public bool IsAutoPlayForward
        {
            get => autoPlayMode == AutoPlayMode.Forward;
            private set
            {
                shownGraphHasChangedEvent.Invoke();
                autoPlayMode = value ? AutoPlayMode.Forward : AutoPlayMode.Off;
            }
        }

        /// <summary>
        /// Returns true if automatic reverse animations are active.
        /// </summary>
        public bool IsAutoPlayReverse
        {
            get => autoPlayMode == AutoPlayMode.Reverse;
            private set
            {
                shownGraphHasChangedEvent.Invoke();
                autoPlayMode = value ? AutoPlayMode.Reverse : AutoPlayMode.Off;
            }
        }

        /// <summary>
        /// True if auto-play mode is turned on (either forward or reverse).
        /// </summary>
        private bool InAutoPlayMode => IsAutoPlayForward || IsAutoPlayReverse;

        /// <summary>
        /// Toggles the auto-play mode. Equivalent to: SetAutoPlay(!IsAutoPlay)
        /// where IsAutoPlay denotes the current state of the auto-play mode.
        /// </summary>
        internal void ToggleAutoPlay()
        {
            SetAutoPlayAsync(!IsAutoPlayForward).Forget();
        }

        /// <summary>
        /// Toggles the reverse auto-play mode. Equivalent to: SetAutoPlayReverse(!IsAutoPlayReverse)
        /// where IsAutoPlayReverse denotes the current state of the reverse auto-play mode.
        /// </summary>
        internal void ToggleAutoPlayReverse()
        {
            SetAutoPlayReverseAsync(!IsAutoPlayReverse).Forget();
        }

        /// <summary>
        /// Sets auto-play mode to <paramref name="enabled"/>. If <paramref name="enabled"/>
        /// is true, the next graph in the series is shown and from there all other
        /// following graphs until we reach the end of the graph series or auto-play
        /// mode is turned off again. If <paramref name="enabled"/> is false instead,
        /// the currently shown graph remains visible.
        /// </summary>
        /// <param name="enabled"> Specifies whether reverse auto-play mode should be enabled. </param>
        /// <returns>Task.</returns>
        internal async UniTask SetAutoPlayAsync(bool enabled)
        {
            IsAutoPlayForward = enabled;
            if (IsAutoPlayForward)
            {
                if (!HasNextGraph)
                {
                    UserInfoLastGraph();
                    return;
                }
                animationFinishedEvent.AddListener(OnAutoPlayCanContinue);
                await ShowNextAsync();
                shownGraphHasChangedEvent.Invoke();
            }
            else
            {
                animationFinishedEvent.RemoveListener(OnAutoPlayCanContinue);
            }
        }

        /// <summary>
        /// Sets reverse auto-play mode to <paramref name="enabled"/>. If <paramref name="enabled"/>
        /// is true, the previous graph in the series is shown and from there all other
        /// previous graphs until we reach the beginning of the graph series or reverse auto-play
        /// mode is turned off again. If <paramref name="enabled"/> is false instead,
        /// the currently shown graph remains visible.
        /// </summary>
        /// <param name="enabled"> Specifies whether reverse auto-play mode should be enabled. </param>
        /// <returns>Task.</returns>
        private async UniTask SetAutoPlayReverseAsync(bool enabled)
        {
            IsAutoPlayReverse = enabled;
            if (IsAutoPlayReverse)
            {
                if (!HasPreviousGraph)
                {
                    UserInfoFirstGraph();
                    return;
                }
                animationFinishedEvent.AddListener(OnAutoPlayReverseCanContinue);
                await ShowPreviousAsync();
                shownGraphHasChangedEvent.Invoke();
            }
            else
            {
                animationFinishedEvent.RemoveListener(OnAutoPlayReverseCanContinue);
            }
        }

        /// <summary>
        /// If we at the end of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its next
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly and auto-play mode is toggled (switched off actually).
        /// </summary>
        private void OnAutoPlayCanContinue()
        {
            if (HasNextGraph)
            {
                ShowNextAsync().Forget();
            }
            else
            {
                ToggleAutoPlay();
            }
        }

        /// <summary>
        /// If we are at the beginning of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its next
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly and auto-play mode is toggled (switched off actually).
        /// </summary>
        private void OnAutoPlayReverseCanContinue()
        {
            if (HasPreviousGraph)
            {
                ShowPreviousAsync().Forget();
            }
            else
            {
                ToggleAutoPlayReverse();
            }
        }

        #endregion

        /// <summary>
        /// Yields a graph renderer that can draw code cities for the each graph of
        /// the graph series.
        /// </summary>
        /// <remarks>Implements <see cref="AbstractSEECity.Renderer"/>.</remarks>
        public GraphRenderer Renderer { get; private set; }

        /// <summary>
        /// Creates and returns a new game edge between <paramref name="source"/> and <paramref name="target"/>
        /// based on the current settings. A new graph edge will be added to the underlying graph, too.
        ///
        /// Note: The default edge layout <see cref="IGraphRenderer.EdgeLayoutDefault"/> will be used if no edge layout,
        /// i.e., <see cref="EdgeLayoutKind.None>"/>, was chosen in the settings.
        ///
        /// Precondition: <paramref name="source"/> and <paramref name="target"/> must have a valid
        /// node reference. The corresponding graph nodes must be in the same graph.
        /// </summary>
        /// <param name="source">Source of the new edge.</param>
        /// <param name="target">Target of the new edge.</param>
        /// <param name="edgeType">The type of the edge to be created.</param>
        /// <returns>The new game object representing the new edge from <paramref name="source"/> to <paramref name="target"/>.</returns>
        /// <exception cref="System.Exception">Thrown if <paramref name="source"/> or <paramref name="target"/>
        /// are not contained in any graph or contained in different graphs.</exception>
        /// <remarks>Implements <see cref="IGraphRenderer.DrawEdge(GameObject, GameObject, string, Edge)"/>.</remarks>
        public GameObject DrawEdge(GameObject source, GameObject target, string edgeType)
        {
            return Renderer.DrawEdge(source, target, edgeType);
        }

        /// <summary>
        /// Creates and returns a new game object for representing the given <paramref name="node"/>.
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// LOD is added and the resulting node is prepared for interaction.
        /// </summary>
        /// <param name="node">Graph node to be represented.</param>
        /// <param name="city">The game object representing the city in which to draw this node;
        /// it has the information about how to draw the node and portal of the city.</param>
        /// <returns>Game object representing given <paramref name="node"/>.</returns>
        /// <remarks>Implements <see cref="IGraphRenderer.DrawNode(Node, GameObject)"/>.</remarks>
        public GameObject DrawNode(Node node, GameObject city = null)
        {
            return Renderer.DrawNode(node, city);
        }

        /// <summary>
        /// Placeholder to satisfy the compiler. This method is not
        /// called anywhere as of yet, but was required in <see
        /// cref="EdgeRenderer"/>.
        /// </summary>
        public GameObject DrawEdge(Edge edge, GameObject source = null, GameObject target = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an edge layout for the given <paramref name="gameEdges"/>.
        ///
        /// The result is a mapping of the names of the game objects in <paramref name="gameEdges"/>
        /// onto the layout for those edges.
        ///
        /// Precondition: The game objects in <paramref name="gameEdges"/> represent graph edges.
        /// </summary>
        /// <param name="gameEdges">The edges for which to create a layout.</param>
        /// <returns>Mapping of the names of the game objects in <paramref name="gameEdges"/> onto
        /// their layout information.</returns>
        /// <remarks>Implements <see cref="IGraphRenderer.LayoutEdges(ICollection{GameObject})"/>.</remarks>
        public IDictionary<string, ILayoutEdge<ILayoutNode>> LayoutEdges(ICollection<GameObject> gameEdges)
        {
            return Renderer.LayoutEdges(gameEdges);
        }

        /// <summary>
        /// Returns the names of all node metrics that truly exist in the underlying
        /// graph currently shown, that is, there is at least one node in the graph
        /// that has this metric.
        ///
        /// The metric names are derived from the graph currently drawn by the
        /// evolution renderer.
        /// If no graph has been loaded yet, the empty list will be returned.
        /// </summary>
        /// <returns>Names of all existing node metrics.</returns>
        internal ISet<string> AllExistingMetrics()
        {
            if (currentCity == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return currentCity.AllNumericNodeAttributes();
            }
        }

        /// <summary>
        /// Updates the base path of all graphs.
        /// </summary>
        /// <param name="basePath">The new base path to be set.</param>
        internal void ProjectPathChanged(string basePath)
        {
            if (graphs != null)
            {
                foreach (Graph graph in graphs)
                {
                    graph.BasePath = basePath;
                }
            }
        }

        /// <summary>
        /// Implements <see cref="IGraphRenderer.AdjustStyle(GameObject)"/>.
        /// </summary>
        public void AdjustStyle(GameObject gameNode)
        {
            Renderer.AdjustStyle(gameNode);
        }

        /// <summary>
        /// Implements <see cref="IGraphRenderer.GetLayout()"/>.
        /// </summary>
        public NodeLayout GetLayout()
        {
            return Renderer.GetLayout();
        }

        /// <summary>
        /// Implements <see cref="IGraphRenderer.AdjustScaleOfLeaf(GameObject)"/>.
        /// </summary>
        public void AdjustScaleOfLeaf(GameObject gameNode)
        {
            Renderer.AdjustScaleOfLeaf(gameNode);
        }

        /// <summary>
        /// Implements <see cref="IGraphRenderer.LayoutEdges{T}(ICollection{T})"/>.
        /// </summary>
        public ICollection<LayoutGraphEdge<T>> LayoutEdges<T>(ICollection<T> layoutNodes) where T : AbstractLayoutNode
        {
            return Renderer.LayoutEdges(layoutNodes);
        }

        /// <summary>
        /// Implements <see cref="IGraphRenderer.AdjustAntenna(GameObject)"/>.
        /// </summary>
        public void AdjustAntenna(GameObject gameNode)
        {
            Renderer.AdjustAntenna(gameNode);
        }
    }
}
