using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Component responsible for implementing the results reported by the <see cref="ReflexionAnalysis"/>
    /// in the scene.
    /// Must be attached to a <see cref="SEEReflexionCity"/>.
    /// <em><see cref="StartFromScratch"/> must be called when adding this component to a city!</em>
    /// </summary>
    [DisallowMultipleComponent]
    public class ReflexionVisualization : MonoBehaviour, IObserver<ChangeEvent>
    {
        /// <summary>
        /// List of <see cref="ChangeEvent"/>s received from the reflexion <see cref="Analysis"/>.
        /// Note that this list is constructed by using <see cref="ReflexionGraphTools.Incorporate"/>.
        /// </summary>
        private IList<ChangeEvent> events = new List<ChangeEvent>();

        /// <summary>
        /// The graph used for the reflexion analysis.
        /// </summary>
        private ReflexionGraph cityGraph;

        /// <summary>
        /// The city this component is attached to.
        /// </summary>
        private SEEReflexionCity city;

        /// <summary>
        /// Percentage by which the starting color of an edge differs to its end color.
        /// </summary>
        private const float edgeGradientFactor = 0.8f;

        /// <summary>
        /// States in which an edge shall be hidden.
        /// </summary>
        private static readonly ISet<State> hiddenEdgeStates = new HashSet<State>
        {
            // TODO: Make this configurable in, e.g., the SEECity editor.
            // We hide all implementation edges except divergences by default.
            State.Unmapped, State.ImplicitlyAllowed, State.AllowedAbsent, State.Allowed
        };

        /// <summary>
        /// A queue of <see cref="ChangeEvent"/>s which were received from the analysis, but not yet handled.
        /// More specifically, these are intended to be handled after the city has been drawn.
        /// </summary>
        private readonly Queue<ChangeEvent> unhandledEvents = new();

        /// <summary>
        /// A queue of <see cref="EdgeOperator"/>s associated with edges which are currently highlighted, that is,
        /// edges which have changed compared to the <see cref="PreviousVersion"/>.
        /// </summary>
        private readonly Queue<EdgeOperator> highlightedEdgeOperators = new();

        /// <summary>
        /// Mapping from Edge IDs to the state they had in the previous version.
        /// This is used to check for changes from the previous to this version.
        /// </summary>
        private IDictionary<string, State> previousEdgeStates = new Dictionary<string, State>();

        /// <summary>
        /// Sets edges' gradient to correct colors depending on reflexion state and hides edges
        /// whose <see cref="IsHiddenToggle"/> is set.
        /// </summary>
        public void InitializeEdges()
        {
            if (!gameObject.IsCodeCityDrawn())
            {
                Debug.LogWarning($"There is no code city drawn for {gameObject.FullName()}. "
                                 + "Self-destruction imminent.\n");
                Destroyer.Destroy(this);
                return;
            }

            EdgeAnimationKind animationKind = city.EdgeLayoutSettings.AnimationKind;
            // We have to set an initial color for the edges, and we have to convert them to meshes.
            foreach (Edge edge in cityGraph.Edges().Where(x => !x.HasToggle(GraphElement.IsVirtualToggle)))
            {
                GameObject edgeObject = edge.GameObject();
                if (edgeObject != null && edgeObject.TryGetComponent(out SEESpline spline))
                {
                    EdgeOperator edgeOp = edgeObject.EdgeOperator();
                    (Color start, Color end) newColors = GetEdgeGradient(edge.State());
                    edgeOp.ChangeColorsTo((newColors.start, newColors.end), useAlpha: false);
                    if (edge.HasToggle(Edge.IsHiddenToggle))
                    {
                        // We will instantly hide this edge. It should not show up yet.
                        edgeObject.EdgeOperator().Hide(animationKind, 0f);
                    }
                }
                else
                {
                    Debug.LogError($"Edge has no associated game object: {edge}\n");
                }
            }
        }

        private void Update()
        {
            // Unhandled events should only be handled once the city is drawn.
            while (unhandledEvents.Count > 0 && gameObject.IsCodeCityDrawn())
            {
                OnNext(unhandledEvents.Dequeue());
            }
            // TODO: Why is the below commented out? Do we still need it?
            //if (SEEInput.ShowAllDivergences())
            //{
            //    ShowAllDivergences(true);
            //}
            //else
            //{
            //    ShowAllDivergences(false);
            //}
        }

        /// <summary>
        /// Whether all divergent implementation dependencies are currently shown.
        /// </summary>
        private bool allDivergencesAreShown;

        /// <summary>
        /// If <paramref name="show"/>, all divergent implementation dependencies will
        /// be shown; otherwise they will be hidden.
        /// </summary>
        /// <param name="show">Whether all divergent implementation dependencies should
        /// be shown.</param>
        private void ShowAllDivergences(bool show)
        {
            // Do we really have a change of the visibility?
            if (allDivergencesAreShown != show)
            {
                allDivergencesAreShown = show;
                foreach (Edge edge in cityGraph.Edges())
                {
                    if (!edge.HasToggle(GraphElement.IsVirtualToggle)
                        && edge.IsInImplementation()
                        && edge.State() == State.Divergent)
                    {
                        GameObject gameEdge = edge.GameObject();
                        if (gameEdge != null)
                        {
                            EdgeOperator edgeOperator = gameEdge.EdgeOperator();
                            edgeOperator.ShowOrHide(allDivergencesAreShown, city.EdgeLayoutSettings.AnimationKind);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Starts the reflexion analysis from scratch, clearing any existing events.
        /// Note that this must be called after adding this component to a game object!
        /// </summary>
        /// <param name="graph">The graph on which the reflexion analysis shall run.</param>
        public void StartFromScratch(ReflexionGraph graph, SEEReflexionCity city)
        {
            this.city = city;
            cityGraph = graph;
            events.Clear();
            graph.Subscribe(this);
            graph.RunAnalysis();
            graph.NewVersion(); // required because we don't want to highlight any initial changes
        }

        /// <summary>
        /// Returns a fitting color gradient from the first to the second color for the given edge state.
        /// </summary>
        /// <param name="edgeState">Edge state for which to yield a color gradient.</param>
        /// <returns>Color gradient.</returns>
        public static (Color, Color) GetEdgeGradient(State edgeState)
        {
            (Color, Color) gradient = edgeState switch
            {
                State.Undefined => (Color.black, Color.Lerp(Color.gray, Color.black, edgeGradientFactor)),
                State.Specified => (Color.gray, Color.Lerp(Color.gray, Color.black, edgeGradientFactor)),
                State.Unmapped => (Color.gray, Color.Lerp(Color.gray, Color.black, edgeGradientFactor)),
                State.ImplicitlyAllowed => (Color.green, Color.white),
                State.AllowedAbsent => (Color.green, Color.white),
                State.Allowed => (Color.green, Color.white),
                State.Divergent => (Color.red, Color.Lerp(Color.red, Color.black, edgeGradientFactor)),
                State.Absent => (Color.yellow, Color.Lerp(Color.yellow, Color.black, edgeGradientFactor)),
                State.Convergent => (Color.green, Color.Lerp(Color.green, Color.black, edgeGradientFactor)),
                _ => throw new ArgumentOutOfRangeException(nameof(edgeState), edgeState, "Unknown state of given edge!")
            };

            return gradient;
        }

        public void OnCompleted()
        {
            events.Clear();
        }

        public void OnError(Exception error)
        {
            // We simply show the error to the user.
            ShowNotification.Error("Error in Reflexion Analysis", error.Message, log: false);
            Debug.LogError(error);
        }

        /// <summary>
        /// Incorporates the given <paramref name="changeEvent"/> into <see cref="events"/>, logs it to the console,
        /// and handles the changes by modifying this city.
        /// </summary>
        /// <param name="changeEvent">The change event received from the reflexion analysis.</param>
        public void OnNext(ChangeEvent changeEvent)
        {
            if (!gameObject.IsCodeCityDrawn())
            {
                unhandledEvents.Enqueue(changeEvent);
                return;
            }

            switch (changeEvent)
            {
                case EdgeChange edgeChange:
                    HandleEdgeChangeAsync(edgeChange).Forget();
                    break;
                case EdgeEvent edgeEvent:
                    HandleEdgeEvent(edgeEvent);
                    break;
                case VersionChangeEvent versionEvent:
                    HandleVersionEvent(versionEvent);
                    break;
            }

            events = events.Incorporate(changeEvent);
        }

        /// <summary>
        /// Handles the given <paramref name="versionChange"/> by "unhighlighting" all changes
        /// and marking the given old version as the new <see cref="PreviousVersion"/>.
        /// </summary>
        /// <param name="versionChange">The event which shall be handled.</param>
        private void HandleVersionEvent(VersionChangeEvent versionChange)
        {
            SaveEdgeStates();
            ResetEdgeHighlights();

            #region Local Functions

            void ResetEdgeHighlights()
            {
                while (highlightedEdgeOperators.Count > 0)
                {
                    // Fade out the highlights for each previously marked edge.
                    EdgeOperator edgeOperator = highlightedEdgeOperators.Dequeue();
                    if (edgeOperator != null)
                    {
                        edgeOperator.GlowOut();
                    }
                }
            }

            void SaveEdgeStates()
            {
                // Due to us using `Incorporate`, only the most recent edge change will exist.
                previousEdgeStates = events.OfType<EdgeChange>().ToDictionary(x => x.Edge.ID, x => x.NewState);
            }

            #endregion
        }

        /// <summary>
        /// Handles the given <paramref name="edgeChange"/> by modifying the scene accordingly.
        /// </summary>
        /// <param name="edgeChange">The event which shall be handled.</param>
        private async UniTaskVoid HandleEdgeChangeAsync(EdgeChange edgeChange)
        {
            // We first check if the corresponding edge should be hidden.
            if (hiddenEdgeStates.Contains(edgeChange.NewState))
            {
                edgeChange.Edge.SetToggle(Edge.IsHiddenToggle);
            }
            else
            {
                edgeChange.Edge.UnsetToggle(Edge.IsHiddenToggle);
            }

            GameObject edge = edgeChange.Edge.GameObject();

            if (edge == null)
            {
                // If edge was just recently added, we have to wait until its GameObject is created.
                // This should be the case by the end of this frame.
                // TODO: In the future, the GraphRenderer should be an observer to the Graph,
                //       so that these cases are handled properly.
                //await UniTask.WaitForEndOfFrame();
                //await UniTask.DelayFrame(2);
                await UniTask.WaitUntil(() => edgeChange.Edge.GameObject() is { } go && go.GetComponent<GraphElementOperator>() != null
                    || edgeChange.Edge.HasToggle(GraphElement.IsVirtualToggle));
                edge = edgeChange.Edge.GameObject();
            }

            if (edge != null)
            {
                await UniTask.WaitForEndOfFrame();
                (Color start, Color end) newColors = GetEdgeGradient(edgeChange.Edge.State());
                EdgeOperator edgeOperator = edge.EdgeOperator();
                edgeOperator.ShowOrHide(!edgeChange.Edge.HasToggle(Edge.IsHiddenToggle), city.EdgeLayoutSettings.AnimationKind);
                edgeOperator.ChangeColorsTo((newColors.start, newColors.end), useAlpha: false);

                if (!previousEdgeStates.TryGetValue(edgeChange.Edge.ID, out State previous) || previous != edgeChange.NewState)
                {
                    // Mark changed edges compared to previous version.
                    edgeOperator.GlowIn();
                    edgeOperator.HitEffect();
                    highlightedEdgeOperators.Enqueue(edgeOperator);
                }
            }
            else if (!edgeChange.Edge.HasToggle(GraphElement.IsVirtualToggle))
            {
                Debug.LogError($"Couldn't find edge {edgeChange.Edge}, whose state changed "
                               + $"from {edgeChange.OldState} to {edgeChange.NewState}!");
            }
        }

        /// <summary>
        /// Handles the given <paramref name="edgeEvent"/> by modifying the scene accordingly.
        /// </summary>
        /// <param name="edgeEvent">The event which shall be handled.</param>
        private static void HandleEdgeEvent(EdgeEvent edgeEvent)
        {
            // We only care about new mappings here, since the nodes will have to visually show that they've been
            // mapped. Other additions or removals are of no relevance here and are handled as usual.
            switch (edgeEvent.Change, edgeEvent.Affected)
            {
                case (ChangeType.Addition, ReflexionSubgraphs.Mapping):
                    HandleNewMapping(edgeEvent.Edge);
                    break;
            }
        }

        /// <summary>
        /// Handles the given new <paramref name="mapsToEdge"/> by modifying the scene accordingly.
        /// </summary>
        /// <param name="mapsToEdge">The edge which has been added.</param>
        private static void HandleNewMapping(Edge mapsToEdge)
        {
            // Maps-To edges must not be drawn, as we will visualize mappings differently.
            mapsToEdge.SetToggle(GraphElement.IsVirtualToggle);
        }
    }
}
