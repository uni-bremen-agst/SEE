using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Component responsible for implementing the results reported by the <see cref="ReflexionAnalysis"/>
    /// in the scene.
    /// Must be attached to a <see cref="SEECity"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ReflexionVisualization : MonoBehaviour, IObserver<ChangeEvent>
    {
        // TODO: Is this assumption (cities' child count > 0 <=> city drawn) alright to make?
        //       Or perhaps there's a better way to check whether a given city has been drawn?
        /// <summary>
        /// Returns true if this city has been drawn in the scene.
        /// </summary>
        private bool CityDrawn => gameObject.transform.childCount > 0;

        /// <summary>
        /// List of <see cref="ChangeEvent"/>s received from the reflexion <see cref="Analysis"/>.
        /// Note that this list is constructed by using <see cref="ReflexionGraphTools.Incorporate"/>.
        /// </summary>
        private readonly List<ChangeEvent> Events = new List<ChangeEvent>();

        /// <summary>
        /// The graph used for the reflexion analysis.
        /// </summary>
        private ReflexionGraph CityGraph;

        /// <summary>
        /// Duration of any animation (edge movement, color change...) in seconds.
        /// </summary>
        private const float ANIMATION_DURATION = 2f;

        /// <summary>
        /// Percentage by which the starting color of an edge differs to its end color.
        /// </summary>
        private const float EDGE_GRADIENT_FACTOR = 0.7f;

        /// <summary>
        /// States in which an edge shall be hidden.
        /// </summary>
        private static readonly ISet<State> HiddenEdgeStates = new HashSet<State>
        {
            // We hide all implementation edges by default.
            State.Unmapped, State.ImplicitlyAllowed, State.AllowedAbsent, State.Allowed, State.Divergent
        };

        /// <summary>
        /// A queue of <see cref="ChangeEvent"/>s which were received from the analysis, but not yet handled.
        /// More specifically, these are intended to be handled after the city has been drawn.
        /// </summary>
        private readonly Queue<ChangeEvent> UnhandledEvents = new Queue<ChangeEvent>();

        private void Start()
        {
            // We have to set an initial color for the edges.
            foreach (Edge edge in CityGraph.Edges().Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
            {
                GameObject edgeObject = GraphElementIDMap.Find(edge.ID);
                if (edgeObject != null && edgeObject.TryGetComponent(out SEESpline spline))
                {
                    spline.GradientColors = GetEdgeGradient(edge);
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
            while (UnhandledEvents.Count > 0 && CityDrawn)
            {
                OnNext(UnhandledEvents.Dequeue());
            }
        }

        /// <summary>
        /// Starts the reflexion analysis from scratch, clearing any existing events.
        /// </summary>
        /// <param name="graph">The graph on which the reflexion analysis shall run</param>
        public void StartFromScratch(ReflexionGraph graph)
        {
            CityGraph = graph;
            Events.Clear();
            graph.Subscribe(this);
            graph.Run();
        }

        /// <summary>
        /// Returns a fitting color gradient from the first to the second color for the given edge by examining
        /// its state.
        /// </summary>
        /// <param name="edge">edge for which to yield a color gradient</param>
        /// <returns>color gradient</returns>
        private static (Color, Color) GetEdgeGradient(Edge edge)
        {
            (Color, Color) gradient = edge.State() switch
            {
                State.Undefined => (Color.black, Color.Lerp(Color.gray, Color.black, EDGE_GRADIENT_FACTOR)),
                State.Specified => (Color.gray, Color.Lerp(Color.gray, Color.black, EDGE_GRADIENT_FACTOR)),
                State.Unmapped => (Color.gray, Color.Lerp(Color.gray, Color.black, EDGE_GRADIENT_FACTOR)),
                State.ImplicitlyAllowed => (Color.green, Color.white),
                State.AllowedAbsent => (Color.green, Color.white),
                State.Allowed => (Color.green, Color.white),
                State.Divergent => (Color.red, Color.Lerp(Color.red, Color.black, EDGE_GRADIENT_FACTOR)),
                State.Absent => (Color.yellow, Color.Lerp(Color.yellow, Color.black, EDGE_GRADIENT_FACTOR)),
                State.Convergent => (Color.green, Color.Lerp(Color.green, Color.black, EDGE_GRADIENT_FACTOR)),
                _ => throw new ArgumentOutOfRangeException(nameof(edge), edge.State(), "Unknown state of given edge!")
            };
            if (edge.HasToggle(Edge.IsHiddenToggle))
            {
                // If the edge is supposed to be hidden, its alpha value must be zero.
                return (gradient.Item1.WithAlpha(0f), gradient.Item2.WithAlpha(0f));
            }
            else
            {
                return gradient;
            }
        }

        public void OnCompleted()
        {
            // Should never be called.
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            // We simply show the error to the user.
            ShowNotification.Error("Error in Reflexion Analysis", error.Message, log: false);
            Debug.LogError(error);
        }

        /// <summary>
        /// Incorporates the given <paramref name="changeEvent"/> into <see cref="Events"/>, logs it to the console,
        /// and handles the changes by modifying this city.
        /// </summary>
        /// <param name="changeEvent">The change event received from the reflexion analysis</param>
        public void OnNext(ChangeEvent changeEvent)
        {
            if (!CityDrawn)
            {
                UnhandledEvents.Enqueue(changeEvent);
                return;
            }

            // TODO: Make sure these actions don't interfere with reversible actions.
            // TODO: Send these changes over the network? Maybe not the edges themselves, but the events?
            // TODO: Handle these asynchronously?

            switch (changeEvent)
            {
                case EdgeChange edgeChange:
                    HandleEdgeChange(edgeChange).Forget();
                    break;
                case EdgeEvent edgeEvent:
                    HandleEdgeEvent(edgeEvent);
                    break;
            }

            Events.Incorporate(changeEvent);
        }

        /// <summary>
        /// Handles the given <paramref name="edgeChange"/> by modifying the scene accordingly.
        /// </summary>
        /// <param name="edgeChange">The event which shall be handled.</param>
        private async UniTaskVoid HandleEdgeChange(EdgeChange edgeChange)
        {
            // We first check if the corresponding edge should be hidden.
            if (HiddenEdgeStates.Contains(edgeChange.NewState))
            {
                edgeChange.Edge.SetToggle(Edge.IsHiddenToggle);
            }
            else
            {
                edgeChange.Edge.UnsetToggle(Edge.IsHiddenToggle);
            }

            GameObject edge = GraphElementIDMap.Find(edgeChange.Edge.ID);

            if (edge == null)
            {
                // If edge was just recently added, we have to wait until its GameObject is created.
                // This should be the case by the end of this frame.
                // TODO: In the future, the GraphRenderer should be an observer to the Graph,
                //       so that these cases are handled properly.
                await UniTask.WaitForEndOfFrame();
                edge = GraphElementIDMap.Find(edgeChange.Edge.ID);
            }

            if (edge != null)
            {
                (Color start, Color end) newColors = GetEdgeGradient(edgeChange.Edge);
                edge.AddOrGetComponent<EdgeOperator>().ChangeColorsTo(newColors.start, newColors.end, ANIMATION_DURATION);
            }
            else
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
                case (ChangeType.Addition, ReflexionSubgraph.Mapping):
                    HandleNewMapping(edgeEvent.Edge);
                    break;
                case (ChangeType.Removal, ReflexionSubgraph.Mapping):
                    HandleRemovedMapping(edgeEvent.Edge);
                    break;
            }
        }

        /// <summary>
        /// Handles the given removed <paramref name="mapsToEdge"/> by modifying the scene accordingly.
        /// </summary>
        /// <param name="mapsToEdge">The edge which has been removed.</param>
        private static void HandleRemovedMapping(Edge mapsToEdge)
        {
            // FIXME: This code is currently commented out. If we are confident that it is really
            // not needed, we need to remove it.

            //Node implNode = mapsToEdge.Source;
            //GameObject implGameNode = implNode.RetrieveGameNode();

            // Node's original parent should be restored.
            //implGameNode.transform.SetParent(implNode.Parent.RetrieveGameNode().transform);

            // The layout of all attached edges need to be updated as well.
            //implGameNode.AddOrGetComponent<NodeOperator>().UpdateAttachedEdges(ANIMATION_DURATION);
        }

        /// <summary>
        /// Handles the given new <paramref name="mapsToEdge"/> by modifying the scene accordingly.
        /// </summary>
        /// <param name="mapsToEdge">The edge which has been added.</param>
        private static void HandleNewMapping(Edge mapsToEdge)
        {
            // Maps-To edges must not be drawn, as we will visualize mappings differently.
            mapsToEdge.SetToggle(Edge.IsVirtualToggle);

            // FIXME: This code is currently commented out. If we are confident that it is really
            // not needed, we need to remove it.

            //Node implNode = mapsToEdge.Source;
            //GameObject archGameNode = mapsToEdge.Target.RetrieveGameNode();
            //GameObject implGameNode = implNode.RetrieveGameNode();

            //Vector3 oldPosition = implGameNode.transform.position;

            // TODO: Rather than returning the old scale from PutOn, lossyScale should be used.
            //Vector3 oldScale = GameNodeMover.PutOn(implGameNode.transform, archGameNode, scaleDown: true, topPadding: 0.3f);
            //Vector3 newPosition = implGameNode.transform.position;
            //Vector3 newScale = implGameNode.transform.localScale;
            //implGameNode.transform.position = oldPosition;
            //implGameNode.transform.localScale = oldScale;
            //NodeOperator nodeOperator = implGameNode.AddOrGetComponent<NodeOperator>();
            //nodeOperator.MoveYTo(newPosition.y, ANIMATION_DURATION);
            //nodeOperator.ScaleTo(newScale, ANIMATION_DURATION);
        }
    }
}