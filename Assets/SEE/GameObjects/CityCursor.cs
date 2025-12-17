using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Controls.Interactables;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Tools.OpenTelemetry;
using SEE.UI3D;
using SEE.Utils;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Cursor for a code city that captures the city's objects when they are hovered over.
    /// It is really only a wrapper around a <see cref="Cursor3D"/> that is attached to the city.
    ///
    /// This class is assumed to be attached to a component holding an
    /// <see cref="AbstractSEECity"/> component.
    /// </summary>
    public class CityCursor : MonoBehaviour
    {
        /// <summary>
        /// The cursor that is attached to this city.
        /// </summary>
        internal Cursor3D Cursor { get; private set; }

        /// <summary>
        /// The city this cursor is attached to.
        /// </summary>
        private AbstractSEECity city;

        /// <summary>
        /// Initializes the <see cref="Cursor"/> and subscribes to the necessary hovering events.
        /// </summary>
        private void Start()
        {
            if (TryGetComponent(out AbstractSEECity city))
            {
                this.city = city;
#if UNITY_EDITOR
                Cursor = Cursor3D.Create(city.name);
#else
                Cursor = Cursor3D.Create();
#endif

                InteractableObject.AnyHoverIn += AnyHoverIn;
                InteractableObject.AnyHoverOut += AnyHoverOut;
            }
            else
            {
                Debug.LogWarning($"{name} has no {nameof(AbstractSEECity)} component attached to it. {nameof(CityCursor)} will be disabled.\n");
                enabled = false;
            }
        }

        /// <summary>
        /// Unsubscribes from events when this object is destroyed
        /// and destroys the <see cref="Cursor"/>.
        /// </summary>
        private void OnDestroy()
        {
            InteractableObject.AnyHoverIn -= AnyHoverIn;
            InteractableObject.AnyHoverOut -= AnyHoverOut;
            Destroyer.Destroy(Cursor);
        }

        /// <summary>
        /// Tracks the hover start times for each hovered <see cref="InteractableObject"/>.
        /// </summary>
        private readonly Dictionary<InteractableObject, float> hoverStartTimes = new();
        /// <summary>
        /// Represents a collection of interactable objects that have reached the hover threshold.
        /// </summary>
        /// <remarks>This collection is used to track objects that meet the hover threshold criteria.</remarks>
        private readonly HashSet<InteractableObject> hoverThresholdReached = new();
        /// <summary>
        /// The threshold duration (in seconds) for hovering over an object to be tracked.
        /// All objects hovered over for less than this duration will not be considered
        /// for tracking.
        /// </summary>
        private readonly float hoverThreshold = 0.5f;

        /// <summary>
        /// Calls and forgets <see cref="AnyHoverInAsync(InteractableObject, bool)"/>.
        /// </summary>
        /// <param name="interactableObject">The hoverered object.</param>
        /// <param name="isInitiator">True if a local player initiated this call.</param>
        private void AnyHoverIn(InteractableObject interactableObject, bool isInitiator)
        {
            AnyHoverInAsync(interactableObject, isInitiator).Forget();
        }

        /// <summary>
        /// Makes <paramref name="interactableObject"/> the <see cref="Cursor"/> focus when
        /// it belongs to <see cref="city"/>.
        /// Called whenever an <see cref="InteractableObject"/> is hoverered.
        /// </summary>
        /// <param name="interactableObject">The hoverered object.</param>
        private async UniTask AnyHoverInAsync(InteractableObject interactableObject, bool _)
        {
            if (AnyHoverIsDoable(interactableObject)
                && interactableObject is InteractableGraphElement graphElement)
            {
                Graph selectedGraph = graphElement.GraphElemRef.Elem.ItsGraph;
                if (selectedGraph != null
                    && city.LoadedGraph != null
                    && selectedGraph.Equals(city.LoadedGraph))
                {
                    Cursor.AddFocus(interactableObject);
                    hoverStartTimes[interactableObject] = Time.time;

                    float start = Time.time;
                    await Task.Delay(TimeSpan.FromSeconds(hoverThreshold));

                    if (hoverStartTimes.TryGetValue(interactableObject, out float hoverTime)
                        && Math.Abs(hoverTime - start) < 0.1f)
                    {
                        hoverThresholdReached.Add(interactableObject);
                    }
                }
            }
        }

        /// <summary>
        /// Removes <paramref name="interactableObject"/> from the <see cref="Cursor"/> focus when
        /// it belongs to <see cref="city"/>.
        /// Called whenever an <see cref="InteractableObject"/> is unselected.
        /// </summary>
        /// <param name="interactableObject">The unselected object.</param>
        private void AnyHoverOut(InteractableObject interactableObject, bool _)
        {
            if (AnyHoverIsDoable(interactableObject)
                && interactableObject is InteractableGraphElement graphElement)
            {
                Graph selectedGraph = graphElement.GraphElemRef.Elem.ItsGraph;
                if (selectedGraph != null && selectedGraph.Equals(city.LoadedGraph))
                {
                    Cursor.RemoveFocus(interactableObject);

                    if (hoverStartTimes.TryGetValue(interactableObject, out float startTime)
                        && hoverThresholdReached.Contains(interactableObject))
                    {
                        float duration = Time.time - startTime;
                        TracingHelperService.Instance?.TrackHoverDuration(
                            interactableObject.gameObject,
                            duration,
                            hoverThreshold);
                    }

                    hoverStartTimes.Remove(interactableObject);
                    hoverThresholdReached.Remove(interactableObject);
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the given <paramref name="interactableObject"/> is valid,
        /// that is, if it is not null, has a non-null <see cref="GraphElementRef"/>,
        /// which references a non-null <see cref="GraphElement"/>, which in turn
        /// is contained in a non-null <see cref="Graph"/>.
        /// </summary>
        /// <param name="interactableObject">The selected object to be checked.</param>
        /// <returns>True if <paramref name="interactableObject"/> is valid.</returns>
        private static bool AnyHoverIsDoable(InteractableObject interactableObject)
        {
            if (interactableObject == null)
            {
                Debug.LogError($"{nameof(AnyHoverInAsync)} called with null {nameof(InteractableObject)}.\n");
                return false;
            }
            if (interactableObject is InteractableGraphElement graphElement)
            {
                if (graphElement.GraphElemRef == null)
                {
                    Debug.LogError($"{nameof(AnyHoverInAsync)} called with null {nameof(GraphElementRef)}.\n");
                    return false;
                }
                if (graphElement.GraphElemRef.Elem == null)
                {
                    //Debug.LogError($"{nameof(AnyHoverInAsync)} called with null {nameof(GraphElement)}.\n");
                    return false;
                }
                if (graphElement.GraphElemRef.Elem.ItsGraph == null)
                {
                    Debug.LogError($"{nameof(AnyHoverInAsync)} called with null {nameof(Graph)}.\n");
                    return false;
                }
            }
            return true;
        }
    }
}
