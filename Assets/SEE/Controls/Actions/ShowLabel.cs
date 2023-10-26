﻿using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows the source name of the hovered or selected object as a text label above the
    /// object. In between that label and the game object, a connecting bar
    /// will be shown.
    /// </summary>
    public class ShowLabel : InteractableObjectAction
    {
        // There can be two reasons why the label needs to be shown: because it is selected
        // or because it is hovered over. Those two conditions are not mutually exclusive.
        // The label will be shown if and only if isHovered or isSelected.

        /// <summary>
        /// True if the object is currently being hovered over.
        /// </summary>
        private bool isHovered = false;

        /// <summary>
        /// True if the object is currently selected.
        /// </summary>
        private bool isSelected = false;

        /// <summary>
        /// Operator component for this object.
        /// </summary>
        private NodeOperator nodeOperator;

        /// <summary>
        /// List of operators of currently displayed node labels.
        /// </summary>
        public static IList<NodeOperator> DisplayedLabelOperators = new List<NodeOperator>();

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
                Debug.LogError($"ShowLabel.OnEnable for {name} has no interactable.\n");
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
            }
            else
            {
                Debug.LogError($"ShowLabel.OnDisable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is selected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the label is shown.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = true;
                // if the object is currently hovered over, the label is already shown
                if (!isHovered)
                {
                    On();
                }
            }
        }

        /// <summary>
        /// Called when the object is deselected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the label is destroyed unless the object is still hovered.
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
        /// the label is shown.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isHovered = true;
                // if the object is currently selected, the label is already shown
                if (!isSelected)
                {
                    On();
                }
            }
        }

        /// <summary>
        /// Called when the object is no longer hovered over. If <paramref name="isInitiator"/>
        /// is false, a remote player has triggered this event and, hence, nothing will be done.
        /// Otherwise the label is destroyed unless the object is still selected.
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
        /// Creates a text label above the object with its node's SourceName if the label doesn't exist yet.
        /// </summary>
        private void On()
        {
            if (nodeOperator == null)
            {
                nodeOperator = gameObject.NodeOperator();
            }

            if (nodeOperator.Node != null)
            {
                LabelAttributes settings = GetLabelSettings(nodeOperator.Node, nodeOperator.City);
                if (settings.Show)
                {
                    DisplayedLabelOperators.Add(nodeOperator);
                    nodeOperator.FadeLabel(settings.LabelAlpha, settings.AnimationFactor);
                }
            }
        }

        /// <summary>
        /// Hides the text label above the object if it exists.
        ///
        /// </summary>
        /// <seealso cref="SelectionOn"/>
        private void Off()
        {
            if (nodeOperator.Node != null)
            {
                LabelAttributes settings = GetLabelSettings(nodeOperator.Node, nodeOperator.City);
                nodeOperator.FadeLabel(0f, settings.AnimationFactor);
                DisplayedLabelOperators.Remove(nodeOperator);
            }
        }

        /// <summary>
        /// Returns the label attributes for <paramref name="node"/> using values
        /// defined in <paramref name="city"/>.
        ///
        /// Assumption: <paramref name="node"/> is "contained" in <paramref name="city"/>.
        /// </summary>
        /// <param name="node">node whose label settings are requested</param>
        /// <param name="city">the city holding the settings</param>
        /// <returns>label attributes for <paramref name="node"/></returns>
        private static LabelAttributes GetLabelSettings(Node node, AbstractSEECity city)
        {
            return city.NodeTypes[node.Type].LabelSettings;
        }

        /// <summary>
        /// If <paramref name="gameObject"/> has a label attached to it, that label
        /// will be turned off.
        /// </summary>
        /// <param name="gameObject">the GameObject whose label shall be turned off</param>
        public static void Off(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out ShowLabel showLabel))
            {
                showLabel.Off();
            }
        }
    }
}
