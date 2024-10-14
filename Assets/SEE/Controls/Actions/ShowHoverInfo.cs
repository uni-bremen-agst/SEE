using SEE.Game.Operator;
using SEE.GO;
using SEE.UI;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows a tooltip with a short block of information about the object when it is hovered over.
    /// </summary>
    public class ShowHoverInfo : InteractableObjectAction
    {
        /// <summary>
        /// Operator component for this object.
        /// </summary>
        private NodeOperator nodeOperator;

        /// <summary>
        /// Registers On() and Off() for the respective hovering and selection events.
        /// </summary>
        protected void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.HoverIn += HoverOn;
                Interactable.HoverOut += HoverOff;
            }
            else
            {
                Debug.LogError($"{nameof(ShowHoverInfo)}.OnEnable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective hovering and selection events.
        /// </summary>
        protected void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.HoverIn -= HoverOn;
                Interactable.HoverOut -= HoverOff;
            }
            else
            {
                Debug.LogError($"{nameof(ShowHoverInfo)}.OnDisable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is being hovered over. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the hover information is shown.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                nodeOperator ??= gameObject.NodeOperator();

                if (nodeOperator.Node != null)
                {
                    if (!nodeOperator.Node.TryGetString("HoverText", out string hoverText))
                    {
                        // Show the type if there is no explicit hover text.
                        hoverText = nodeOperator.Node.Type;
                    }
                    Tooltip.ActivateWith(hoverText);
                }
            }
        }

        /// <summary>
        /// Called when the object is no longer hovered over. If <paramref name="isInitiator"/>
        /// is false, a remote player has triggered this event and, hence, nothing will be done.
        /// Otherwise, the hover information is hidden.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                Tooltip.Deactivate();
            }
        }
    }
}
