using System;
using System.Collections.Generic;
using DG.Tweening;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.City
{
    /// <summary>
    /// Component responsible for implementing the results reported by the <see cref="ReflexionAnalysis"/>
    /// in the scene.
    /// Must be attached to a <see cref="SEECity"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ReflexionVisualization : MonoBehaviour, Observer
    {
        /// <summary>
        /// Reflexion analysis. Use this to make changes to the graph
        /// (such as mappings, hierarchies, and so on), <b>do not modify
        /// the underlying Graph directly!</b>
        /// </summary>
        [NonSerialized]
        public Reflexion Analysis;

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
        private Graph CityGraph;

        /// <summary>
        /// Duration of any animation (edge movement, color change...) in seconds.
        /// </summary>
        private const float ANIMATION_DURATION = 5f;

        /// <summary>
        /// A queue of <see cref="ChangeEvent"/>s which were received from the analysis, but not yet handled.
        /// More specifically, these are intended to be handled after the city has been drawn.
        /// </summary>
        private readonly Queue<ChangeEvent> UnhandledEvents = new Queue<ChangeEvent>();

        private void Start()
        {
            // We have to set an initial color for the edges.
            foreach (Edge edge in CityGraph.Edges())
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
        
        private void Update()
        {
            // Unhandled events should only be handled once the city is drawn.
            while (UnhandledEvents.Count > 0 && CityDrawn)
            {
                HandleChange(UnhandledEvents.Dequeue());
            }
        }

        /// <summary>
        /// Starts the reflexion analysis from scratch, clearing any existing events.
        /// </summary>
        /// <param name="graph">The graph on which the reflexion analysis shall run</param>
        public void StartFromScratch(Graph graph)
        {
            CityGraph = graph;
            Events.Clear();
            Analysis = new Reflexion(CityGraph);
            Analysis.Register(this);
            Analysis.Run();
        }
        
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
            // TODO: Handle these asynchronously?
            
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

            if (edge != null)
            {
                (Color start, Color end) newColors = GetEdgeGradient(edgeChange.NewState);
                // Animate color change for nicer visuals.
                edge.AddOrGetComponent<EdgeOperator>().FadeColorsTo(newColors.start, newColors.end, ANIMATION_DURATION);
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
            nodeOperator.MoveYTo(newPosition.y, ANIMATION_DURATION);
            nodeOperator.ScaleTo(newScale, ANIMATION_DURATION);
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