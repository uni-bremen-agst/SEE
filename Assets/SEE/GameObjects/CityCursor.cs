using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.City;
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
            InteractableObject.AnyHoverIn  -= AnyHoverIn;
            InteractableObject.AnyHoverOut -= AnyHoverOut;
            Destroyer.Destroy(Cursor);
        }

        /// <summary>
        /// Makes <paramref name="interactableObject"/> the <see cref="Cursor"/> focus when
        /// it belongs to <see cref="city"/>.
        /// Called whenever an <see cref="InteractableObject"/> is selected.
        /// </summary>
        /// <param name="interactableObject">the selected object</param>
        private void AnyHoverIn(InteractableObject interactableObject, bool _)
        {
            Graph selectedGraph = interactableObject.GraphElemRef.Elem.ItsGraph;
            if (selectedGraph != null && city.LoadedGraph != null
                && selectedGraph.Equals(city.LoadedGraph))
            {
                Cursor.AddFocus(interactableObject);
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
            Graph selectedGraph = interactableObject.GraphElemRef.Elem.ItsGraph;
            if (selectedGraph != null && selectedGraph.Equals(city.LoadedGraph))
            {
                Cursor.RemoveFocus(interactableObject);
            }
        }
    }
}
