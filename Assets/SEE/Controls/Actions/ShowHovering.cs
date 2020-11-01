﻿using UnityEngine;

namespace SEE.Controls.Actions
{
    public class ShowHovering : InteractableObjectHoveringAction
    {
        /// <summary>
        /// The local hovering color of the outline.
        /// </summary>
        private readonly static Color LocalHoverColor = Utils.ColorPalette.Viridis(0.0f);

        /// <summary>
        /// The remote hovering color of the outline.
        /// </summary>
        private readonly static Color RemoteHoverColor = Utils.ColorPalette.Viridis(0.2f);

        /// <summary>
        /// If the object is neither selected nor grabbed, an outline will be
        /// created. Depending upon <paramref name="isOwner"/> one of two different
        /// colors will be used for the outline.
        /// Called when the object is hovered over.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected override void Show(bool isOwner)
        {
            if (!interactable.IsSelected && !interactable.IsGrabbed)
            {
                if (TryGetComponent(out Outline outline))
                {
                    outline.SetColor(isOwner ? LocalHoverColor : RemoteHoverColor);
                }
                else
                {
                    Outline.Create(gameObject, isOwner ? LocalHoverColor : RemoteHoverColor);
                }
            }
        }

        /// <summary>
        /// If the object is neither selected nor grabbed and if it has an outline,
        /// this outline will be destroyed.
        /// Called when the object is no longer being hovered over.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected override void Hide(bool isOwner)
        {
            if (!interactable.IsSelected && !interactable.IsGrabbed)
            {
                if (TryGetComponent(out Outline outline))
                {
                    DestroyImmediate(outline);
                }
            }
        }
    }
}