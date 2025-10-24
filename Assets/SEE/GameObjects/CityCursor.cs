using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private readonly Dictionary<InteractableObject, float> hoverStartTimes = new();
        private readonly HashSet<InteractableObject> hoverThresholdReached = new();
        private readonly float hoverThreshold = 0.5f;

        /// <summary>
        /// Makes <paramref name="interactableObject"/> the <see cref="Cursor"/> focus when
        /// it belongs to <see cref="city"/>.
        /// Called whenever an <see cref="InteractableObject"/> is selected.
        /// </summary>
        /// <param name="interactableObject">the selected object</param>
        private async void AnyHoverIn(InteractableObject interactableObject, bool _)
        {
            if (AnyHoverIsDoable(interactableObject)
                && interactableObject is InteractableGraphElement graphElement)
            {
                Graph selectedGraph = graphElement.GraphElemRef.Elem.ItsGraph;
                if (selectedGraph != null && city.LoadedGraph != null
                                          && selectedGraph.Equals(city.LoadedGraph))
                {
                    Cursor.AddFocus(interactableObject);
                    hoverStartTimes[interactableObject] = Time.time;

                    float start = Time.time;
                    await Task.Delay(TimeSpan.FromSeconds(hoverThreshold));

                    if (hoverStartTimes.TryGetValue(interactableObject, out float hoverTime) &&
                        Math.Abs(hoverTime - start) < 0.1f)
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
        /// <param name="interactableObject">the unselected object</param>
        private void AnyHoverOut(InteractableObject interactableObject, bool _)
        {
            if (AnyHoverIsDoable(interactableObject)
                && interactableObject is InteractableGraphElement graphElement)
            {
                Graph selectedGraph = graphElement.GraphElemRef.Elem.ItsGraph;
                if (selectedGraph != null && selectedGraph.Equals(city.LoadedGraph))
                {
                    Cursor.RemoveFocus(interactableObject);

                    if (hoverStartTimes.TryGetValue(interactableObject, out float startTime) &&
                        hoverThresholdReached.Contains(interactableObject))
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
        /// <param name="interactableObject">the selected object to be checked</param>
        /// <returns>true if <paramref name="interactableObject"/> is valid</returns>
        private static bool AnyHoverIsDoable(InteractableObject interactableObject)
        {
            if (interactableObject == null)
            {
                Debug.LogError($"{nameof(AnyHoverIn)} called with null {nameof(InteractableObject)}.\n");
                return false;
            }
            if (interactableObject is InteractableGraphElement graphElement)
            {
                if (graphElement.GraphElemRef == null)
                {
                    Debug.LogError($"{nameof(AnyHoverIn)} called with null {nameof(GraphElementRef)}.\n");
                    return false;
                }
                if (graphElement.GraphElemRef.Elem == null)
                {
                    Debug.LogError($"{nameof(AnyHoverIn)} called with null {nameof(GraphElement)}.\n");
                    return false;
                }
                if (graphElement.GraphElemRef.Elem.ItsGraph == null)
                {
                    Debug.LogError($"{nameof(AnyHoverIn)} called with null {nameof(Graph)}.\n");
                    return false;
                }
            }
            return true;
        }
    }
}
