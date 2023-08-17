using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows connected edges when the user hovers over or selects a node.
    /// </summary>
    public class ShowEdges : InteractableObjectAction
    {
        /// <summary>
        /// True if the object is currently being hovered over.
        /// </summary>
        private bool isHovered;

        /// <summary>
        /// True if the object is currently selected.
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// The city object this edge is rendered in.
        /// </summary>
        private AbstractSEECity codeCity;

        /// <summary>
        /// Registers On() and Off() for the respective hovering and selection events.
        /// </summary>
        protected void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn += SelectionOn;
                Interactable.SelectOut += SelectionOff;
                Interactable.HoverIn += HoverOn;
                Interactable.HoverOut += HoverOff;
            }
            else
            {
                Debug.LogError($"ShowEdges.OnEnable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective hovering and selection events.
        /// </summary>
        protected void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn -= SelectionOn;
                Interactable.SelectOut -= SelectionOff;
                Interactable.HoverIn -= HoverOn;
                Interactable.HoverOut -= HoverOff;
                codeCity = null; // Reset codeCity
            }
            else
            {
                Debug.LogError($"ShowEdges.OnDisable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is selected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the connected edges are shown.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = true;
                // if the object is currently hovered over, the edges are already shown
                if (!isHovered)
                {
                    On();
                }
            }
        }

        /// <summary>
        /// Called when the object is deselected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the shown edges are hidden unless the object is still hovered.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = false;
                if (!isHovered)
                {
                    Off();
                }
            }
        }

        /// <summary>
        /// Called when the object is being hovered over. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the connected edges are shown.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isHovered = true;
                // if the object is currently selected, the edges are already shown
                if (!isSelected)
                {
                    On();
                }
            }
        }

        /// <summary>
        /// Called when the object is no longer hovered over. If <paramref name="isInitiator"/>
        /// is false, a remote player has triggered this event and, hence, nothing will be done.
        /// Otherwise the connected edges are hidden unless the object is still selected.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isHovered = false;
                if (!isSelected)
                {
                    Off();
                }
            }
        }

        /// <summary>
        /// Returns the code city holding the settings for the visualization of the node.
        /// May be null.
        /// </summary>
        private AbstractSEECity City()
        {
            GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
            if (codeCityObject == null)
            {
                Debug.LogError($"Could not retrieve CodeCity for {gameObject.name}!");
                return null;
            }

            codeCityObject.TryGetComponent(out AbstractSEECity city);
            return city;
        }

        /// <summary>
        /// Shows all incoming/outgoing edges of the node this component is
        /// attached to.
        /// </summary>
        private void On()
        {
            OnOff(true);
        }

        /// <summary>
        /// Hides all incoming/outgoing edges of the node this component is
        /// attached to.
        /// </summary>
        private void Off()
        {
            OnOff(false);
        }

        /// <summary>
        /// Shows/hides all incoming/outgoing edges of the node this component is attached to.
        /// </summary>
        /// <param name="show">if true, the edges are shown; otherwise hidden</param>
        private void OnOff(bool show)
        {
            if (gameObject.TryGetNode(out Node node))
            {
                codeCity ??= City();

                IEnumerable<Edge> edges = codeCity.EdgeLayoutSettings.AnimateInnerEdges
                    ? node.PostOrderDescendants().SelectMany(x => x.Edges)
                    : node.Edges;

                EdgeAnimationKind animationKind = codeCity.EdgeLayoutSettings.AnimationKind;

                foreach (Edge edge in edges.Distinct().Where(x => x.HasToggle(Edge.IsHiddenToggle)))
                {
                    edge.Operator().ShowOrHide(show, animationKind);
                }
            }
        }
    }
}