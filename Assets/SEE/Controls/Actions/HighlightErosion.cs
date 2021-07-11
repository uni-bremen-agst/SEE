using System;
using DG.Tweening;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Highlights the corresponding erosion icons of the node this component is attached to.
    /// If no erosion icons are present, this component won't do anything.
    /// </summary>
    public class HighlightErosion: InteractableObjectAction
    {
        // TODO: This file heavily clones code from ShowLabel.cs. It may be worthwhile to put this common behavior
        // into a shared superclass.
        
        /// <summary>
        /// True if the object is currently being hovered over.
        /// </summary>
        private bool isHovered;

        /// <summary>
        /// True if the object is currently selected.
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// The node reference this component is attached to.
        /// </summary>
        private NodeRef nodeRef;
        
        /// <summary>
        /// All currently active tweens, collected in a sequence.
        /// </summary>
        private Sequence sequence;
        
        //TODO: Use sequence to animate tween
        
        /// <summary>
        /// Registers On() and Off() for the respective hovering and selection events.
        /// </summary>
        protected void OnEnable()
        {
            if (interactable != null && (nodeRef != null || gameObject.TryGetComponent(out nodeRef)) && nodeRef.Value != null)
            {
                interactable.SelectIn += SelectionOn;
                interactable.SelectOut += SelectionOff;
                interactable.HoverIn += HoverOn;
                interactable.HoverOut += HoverOff;
            }
            else
            {
                Debug.LogError($"ShowLabel.OnEnable for {name} has NO interactable.\n");
            }
        }
        
        /// <summary>
        /// Unregisters On() and Off() from the respective hovering and selection events.
        /// </summary>
        protected void OnDisable()
        {
            if (interactable != null)
            {
                interactable.SelectIn -= SelectionOn;
                interactable.SelectOut -= SelectionOff;
                interactable.HoverIn -= HoverOn;
                interactable.HoverOut -= HoverOff;
            }
            else
            {
                Debug.LogError($"ShowLabel.OnDisable for {name} has NO interactable.\n");
            }
        }
        
        /// <summary>
        /// Called when the object is selected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the erosion icons are highlighted.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = true;
                // if the object is currently hovered over, the erosion icon is already hovered over
                if (!isHovered)
                {
                    On();
                }
            }
        }

        /// <summary>
        /// Called when the object is deselected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the highlight is removed unless the object is still hovered.
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
        /// the highlight is enabled.
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
        /// Otherwise the highlight is disabled unless the object is still selected.
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

        private void On()
        {
            Debug.Log("On erosion");
            ForEachErosionSprite(sprite =>
            {
                sprite.transform.localScale *= 2;
            });
        }

        private void Off()
        {
            Debug.Log("Off erosion");
            //TODO: Make sure this doesn't cause floating point precision problems
            ForEachErosionSprite(s => s.transform.localScale /= 2);
        }

        /// <summary>
        /// Applies the given <paramref name="spriteAction"/> to each erosion sprite renderer of this node.
        /// </summary>
        /// <param name="spriteAction">The action to apply to each erosion sprite renderer.</param>
        private void ForEachErosionSprite(Action<SpriteRenderer> spriteAction)
        {
            if (spriteAction == null)
            {
                throw new ArgumentNullException(nameof(spriteAction));
            }

            foreach (Transform childTransform in nodeRef.transform)
            {
                if (childTransform.name.StartsWith(ErosionIssues.EROSION_SPRITE_PREFIX))
                {
                    Assert.IsTrue(childTransform.childCount == 1, "Only child of sprite object should be renderer");
                    Transform spriteChild = childTransform.GetChild(0);
                    if (spriteChild.gameObject.TryGetComponentOrLog(out SpriteRenderer spriteRenderer))
                    {
                        spriteAction.Invoke(spriteRenderer);
                    }
                }
            }
        }
    }
}