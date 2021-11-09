using System;
using DG.Tweening;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Highlights the corresponding erosion icons of the node this component is attached to.
    /// If no erosion icons are present, this component won't do anything.
    /// </summary>
    public class HighlightErosion : InteractableObjectAction
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

        /// <summary>
        /// Registers On() and Off() for the respective hovering and selection events.
        /// </summary>
        protected void OnEnable()
        {
            if (interactable == null)
            {
                Debug.LogError($"HighlightErosion.OnEnable for {name} has no interactable.\n");
                enabled = false;
            }
            else if ((nodeRef != null || gameObject.TryGetComponent(out nodeRef)) && nodeRef.Value != null)
            {
                interactable.SelectIn += SelectionOn;
                interactable.SelectOut += SelectionOff;
                interactable.HoverIn += HoverOn;
                interactable.HoverOut += HoverOff;
            }
            else
            {
                Debug.LogError($"HighlightErosion.OnEnable for {name} has no valid node reference.\n");
                enabled = false;
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
                Debug.LogError($"HighlightErosion.OnDisable for {name} has NO interactable.\n");
                enabled = false;
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

        /// <summary>
        /// Returns the code city holding the settings for the visualization of the node.
        /// May be null.
        /// </summary>
        private AbstractSEECity City()
        {
            GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
            if (codeCityObject == null)
            {
                return null;
            }

            codeCityObject.TryGetComponent(out AbstractSEECity city);
            return city;
        }

        /**
         * Returns the animation duration using values defined in AbstractSEECity.
         * <param name="node">The node.</param>
         * <param name="city">The city object from which to retrieve the duration.
         * If <code>null</code>, the city object will be retrieved by a call to <see cref="City"/>.</param>
         */
        private float AnimationDuration(Node node, AbstractSEECity city = null)
        {
            city ??= City();
            return (node.IsLeaf() ? city.LeafNodeSettings.LabelSettings : city.InnerNodeSettings.LabelSettings).AnimationDuration;
        }

        private void On()
        {
            if (sequence != null)
            {
                sequence.PlayForward();
            }
            else
            {
                sequence = DOTween.Sequence();
                sequence.SetAutoKill(false);
                sequence.SetRecyclable(true);
                float duration = AnimationDuration(nodeRef.Value);
                const float SCALING_FACTOR = 1.3f;
                ForEachErosion((sprite, textMesh, layoutGroup) =>
                {
                    // We have to delete the text first to animate it more nicely, so we save it here before that
                    string metricText = textMesh.text;
                    // This will enlarge the sprite, make it more opaque, and fade in the text
                    sequence.Insert(0, DOTween.To(() => textMesh.text, t => textMesh.text = t, string.Empty, 0.01f))
                            .InsertCallback(0.02f, () => textMesh.gameObject.SetActive(!sequence.isBackwards))
                                               .Insert(0.03f, DOTween.To(() => sprite.transform.localScale,
                                                                     s => sprite.transform.localScale = s,
                                                                     sprite.transform.localScale * SCALING_FACTOR, duration))
                                               .Insert(0.03f, DOTween.ToAlpha(() => sprite.color, color => sprite.color = color,
                                                                          1f, duration))
                                               .Insert(0.03f, DOTween.To(() => textMesh.text, t => textMesh.text = t,
                                                                     metricText, duration));
                });
                sequence.PlayForward();
            }
        }

        private void Off()
        {
            sequence?.PlayBackwards();
        }

        /// <summary>
        /// Applies the given <paramref name="spriteAction"/> to each erosion of this node.
        /// </summary>
        /// <param name="spriteAction">The action to apply to each erosion sprite renderer, metric text and
        /// corresponding horizontal layout group.</param>
        private void ForEachErosion(Action<SpriteRenderer, TextMeshPro, HorizontalLayoutGroup> spriteAction)
        {
            if (spriteAction == null)
            {
                throw new ArgumentNullException(nameof(spriteAction));
            }

            foreach (Transform childTransform in nodeRef.transform)
            {
                if (childTransform.name.StartsWith(ErosionIssues.EROSION_SPRITE_PREFIX))
                {
                    HorizontalLayoutGroup layoutGroup = childTransform.GetComponent<HorizontalLayoutGroup>();
                    SpriteRenderer spriteRenderer = childTransform.GetComponentInChildren<SpriteRenderer>();
                    TextMeshPro[] textMesh = childTransform.GetComponentsInChildren<TextMeshPro>(true);
                    Assert.IsNotNull(spriteRenderer);
                    Assert.IsTrue(textMesh.Length > 0);
                    Assert.IsNotNull(layoutGroup);
                    spriteAction.Invoke(spriteRenderer, textMesh[0], layoutGroup);
                }
            }
        }
    }
}