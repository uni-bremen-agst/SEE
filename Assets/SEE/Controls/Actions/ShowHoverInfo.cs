using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.UI;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows a tooltip with a short block of information about the object when it is hovered over.
    /// The content of the tooltip is configurable through the city's <see cref="TooltipSettings"/>.
    /// </summary>
    public class ShowHoverInfo : InteractableObjectAction
    {
        /// <summary>
        /// Operator component for this object.
        /// </summary>
        private NodeOperator nodeOperator;

        /// <summary>
        /// Cached reference to the city's tooltip settings.
        /// </summary>
        private TooltipSettings tooltipSettings;

        /// <summary>
        /// The node attribute name for explicit hover text.
        /// </summary>
        private const string hoverTextAttribute = "HoverText";

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
                Debug.LogError($"{nameof(ShowHoverInfo)}.{nameof(OnEnable)} for {name} has no interactable.\n");
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
                Debug.LogError($"{nameof(ShowHoverInfo)}.{nameof(OnDisable)} for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Gets the tooltip settings from the containing city (cached after first access).
        /// </summary>
        /// <returns>The tooltip settings, or null if not available.</returns>
        private TooltipSettings GetTooltipSettings()
        {
            return tooltipSettings ??= gameObject.ContainingCity()?.TooltipSettings;
        }

        /// <summary>
        /// Called when the object is being hovered over. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the hover information is shown.
        /// </summary>
        /// <param name="interactableObject">The object being hovered over.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void HoverOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (!isInitiator)
            {
                return;
            }

            nodeOperator ??= gameObject.NodeOperator();
            if (nodeOperator?.Node == null)
            {
                return;
            }

            string hoverText = GetHoverText();
            if (!string.IsNullOrEmpty(hoverText))
            {
                Tooltip.ActivateWith(hoverText);
            }
        }

        /// <summary>
        /// Generates the hover text based on the city's tooltip settings.
        /// </summary>
        /// <returns>The formatted hover text.</returns>
        private string GetHoverText()
        {
            // Check for explicit HoverText attribute first (highest priority)
            if (nodeOperator.Node.TryGetString(hoverTextAttribute, out string explicitHoverText))
            {
                return explicitHoverText;
            }

            // Use tooltip settings from the city configuration
            TooltipSettings settings = GetTooltipSettings();
            if (settings != null)
            {
                string tooltipText = TooltipContentBuilder.BuildTooltip(nodeOperator.Node, settings);
                if (!string.IsNullOrEmpty(tooltipText))
                {
                    return tooltipText;
                }
            }

            // Fallback to the type if nothing else is configured
            return nodeOperator.Node.Type;
        }

        /// <summary>
        /// Called when the object is no longer hovered over. If <paramref name="isInitiator"/>
        /// is false, a remote player has triggered this event and, hence, nothing will be done.
        /// Otherwise, the hover information is hidden.
        /// </summary>
        /// <param name="interactableObject">The object being hovered over.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void HoverOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                Tooltip.Deactivate();
            }
        }
    }
}
