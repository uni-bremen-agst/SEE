using System.Collections.Generic;
using DG.Tweening;
using JetBrains.Annotations;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using Valve.VR.InteractionSystem;

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
        /// Registers On() and Off() for the respective hovering and selection events.
        /// </summary>
        protected void OnEnable()
        {
            if (interactable != null)
            {
                interactable.SelectIn += SelectionOn;
                interactable.SelectOut += SelectionOff;
                interactable.HoverIn += HoverOn;
                interactable.HoverOut += HoverOff;
            }
            else
            {
                Debug.LogErrorFormat("ShowLabel.OnEnable for {0} has NO interactable.\n", name);
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
                Debug.LogErrorFormat("ShowLabel.OnDisable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Called when the object is selected. If <paramref name="isOwner"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the label is shown.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void SelectionOn(bool isOwner)
        {
            if (isOwner)
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
        /// Called when the object is deselected. If <paramref name="isOwner"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the label is destroyed unless the object is still hovered.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void SelectionOff(bool isOwner)
        {
            if (isOwner)
            {
                isSelected = false;
                if (!isHovered)
                {
                    Off();
                }
            }
        }

        /// <summary>
        /// Called when the object is being hovered over. If <paramref name="isOwner"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the label is shown.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void HoverOn(bool isOwner)
        {
            if (isOwner)
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
        /// Called when the object is no longer hovered over. If <paramref name="isOwner"/> 
        /// is false, a remote player has triggered this event and, hence, nothing will be done.
        /// Otherwise the label is destroyed unless the object is still selected.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void HoverOff(bool isOwner)
        {
            if (isOwner)
            {
                isHovered = false;
                if (!isSelected)
                {
                    Off();
                }
            }
        }

        /// <summary>
        /// The text label that's displayed above the object when the user hovers over it.
        /// Will be <code>null</code> when the label is not currently being displayed.
        /// This nodeLabel will contain a TextMeshPro component for the label text and a
        /// LineRenderer that connects the labeled object and the label text visually.
        /// </summary>
        private GameObject nodeLabel;

        /// <summary>
        /// True iff the label is currently playing an animation after which it will be destroyed.
        /// </summary>
        private bool currentlyDestroying;

        /// <summary>
        /// All currently active tweens.
        /// </summary>
        private List<Tween> tweens = new List<Tween>();

        /// <summary>
        /// Returns the code city holding the settings for the visualization of the node.
        /// 
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

        /// <summary>
        /// Returns true iff labels are enabled for this kind of node.
        /// </summary>
        /// <param name="city">the code city holding the attributes for showing labels</param>
        /// <param name="isLeaf">whether this node is a leaf</param>
        /// <returns>true iff labels are enabled for this kind of node</returns>
        private bool LabelsEnabled(AbstractSEECity city, bool isLeaf)
        {
            return isLeaf && city.ShowLabel || !isLeaf && city.InnerNodeShowLabel;
        }

        /// <summary>
        /// Creates a text label above the object with its node's SourceName if the label doesn't exist yet.
        /// </summary>
        private void On()
        {
            AbstractSEECity city = City();
            if (city == null)
            {
                // The game node is currently not part of a city. This may happen, for instance,
                // if a node is just created and not yet added to a city. In this case, we 
                // are not doing anything.
                return;
            }

            bool isLeaf = SceneQueries.IsLeaf(gameObject);
            if (!LabelsEnabled(city, isLeaf))
            {
                return; // If labels are disabled, we don't need to do anything
            }

            // If label already exists or the game object has no node reference, nothing needs to be done
            if (nodeLabel != null && !currentlyDestroying || !gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                return;
            }

            currentlyDestroying = false;

            Node node = nodeRef.node;
            if (node == null)
            {
                Debug.LogErrorFormat("Game node {0} has no valid node reference.\n", name);
                return;
            }

            // Now we create the label
            // We define starting and ending positions for the animation
            Vector3 startLabelPosition = gameObject.transform.position;
            nodeLabel = TextFactory.GetTextWithSize(node.SourceName, startLabelPosition,
                                                    isLeaf ? city.LeafLabelFontSize : city.InnerNodeLabelFontSize, 
                                                    textColor: Color.black.ColorWithAlpha(0f));
            nodeLabel.name = $"Label {node.SourceName}";
            nodeLabel.transform.SetParent(gameObject.transform);
            
            SetOutline();

            // Add connecting line between "roof" of object and text
            Vector3 startLinePosition = gameObject.transform.position;
            startLinePosition.y = BoundingBox.GetRoof(new List<GameObject> {gameObject});
            LineFactory.Draw(nodeLabel, new[] {startLinePosition, startLinePosition}, 0.01f,
                             Materials.New(Materials.ShaderType.TransparentLine, Color.black));
            Portal.SetInfinitePortal(nodeLabel);

            AnimateLabel(true, city, isLeaf);
        }

        /// <summary>
        /// Enables or disables an outline around TextMeshPro instances based on our platform.
        /// </summary>
        private void SetOutline()
        {
            // On the HoloLens, we want to make the text a bit easier to read by making it bolder.
            // We do this by adding a slight outline.
            bool enableOutline = PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.HoloLens;
            // However, when developing on a PC/Emulator, the background will be black, so we add a white outline.
            Color outlineColor = Debug.isDebugBuild ? Color.white : Color.black;
            if (nodeLabel.TryGetComponent(out TextMeshPro tm))
            {
                TextFactory.SetOutline(enableOutline, tm, outlineColor: outlineColor);
            }
            else
            {
                Debug.LogError("No TextMeshPro has been found on a newly created label.\n");
            }
        }

        /// <summary>
        /// Animates the given labels by fading them in/out and gradually changing their position.
        /// </summary>
        /// <param name="animateIn">If true, we will fade-in and move the label to the top.
        /// If false, we will fade out and move the label to the bottom.</param>
        /// <param name="city">The <see cref="AbstractSEECity"/> object from which to get the settings.</param>
        /// <param name="isLeaf">Whether this node is a leaf.</param>
        private void AnimateLabel(bool animateIn, AbstractSEECity city, bool isLeaf)
        {
            // TODO: Maybe the class in Tweens.cs should be used instead.
            // However, I'm not sure why that class is a MonoBehaviour (shouldn't it be a static helper class?)
            // and DOTween instead recommends using extension methods, so this is what's used here.
            // Additionally, some specific functionality (e.g. callbacks), isn't available from Tweens.cs.
            
            Vector3 endLabelPosition, endLinePosition;
            float endAlpha;
            if (animateIn)
            {
                endLabelPosition = nodeLabel.transform.position;
                endLabelPosition.y += isLeaf ? city.LeafLabelDistance : city.InnerNodeLabelDistance;
                endLinePosition = endLabelPosition;
                float nodeTopPosition = nodeLabel.GetComponent<TextMeshPro>().textBounds.extents.y;
                endLinePosition.y -= nodeTopPosition * 1.3f; // add slight gap to make it slightly more aesthetic
                endAlpha = 1f;
            }
            else
            {
                endLabelPosition = gameObject.transform.position;
                endLinePosition = endLabelPosition;
                endLinePosition.y = BoundingBox.GetRoof(new List<GameObject> {gameObject});
                endAlpha = 0f;
            }

            float animationDuration = AnimationDuration(isLeaf, city);
            AnimateLabelText();
            AnimateLabelLine();

            #region Local Methods
            void AnimateLabelText()
            {
                // Animated label to move to top and fade in
                if (nodeLabel.TryGetComponent(out TextMeshPro text))
                {
                    tweens.Add(nodeLabel.transform.DOMove(endLabelPosition, animationDuration));
                    tweens.Add(DOTween.ToAlpha(() => text.color, color => text.color = color, endAlpha, animationDuration));
                    tweens.Add(DOTween.ToAlpha(() => text.outlineColor, color => text.outlineColor = color, endAlpha, animationDuration));
                }
                else
                {
                    Debug.LogError("Couldn't find text component in newly created label.\n");
                }
            }

            void AnimateLabelLine()
            {
                // Animated line to move to top and fade in
                if (nodeLabel.TryGetComponent(out LineRenderer line))
                {
                    // Reset colors to clear first
                    LineFactory.SetColors(line, Color.clear, Color.clear);

                    // Lower start of line should be visible almost immediately due to reduced alpha (smooth transition)
                    tweens.Add(DOTween.ToAlpha(() => line.startColor, c => line.startColor = c, endAlpha * 0.5f, animationDuration*0.1f));
                    tweens.Add(DOTween.ToAlpha(() => line.endColor, c => line.endColor = c, endAlpha, animationDuration));
                    Tween lastTween = DOTween.To(() => line.GetPosition(1), p => line.SetPosition(1, p), endLinePosition, animationDuration);
                    tweens.Add(lastTween);
                    if (!animateIn)
                    {
                        lastTween.OnComplete(DestroyLabel);
                    }
                }
                else
                {
                    Debug.LogError("Couldn't find line component in newly created label.\n");
                }
            }
            #endregion
        }

        /// <summary>
        /// Destroys the text label above the object if it exists.
        /// 
        /// <seealso cref="SelectionOn"/>
        /// </summary>
        private void Off()
        {
            if (nodeLabel == null)
            {
                return;
            }

            currentlyDestroying = true;
            // Fade out and move label down
            AnimateLabel(false, City(), SceneQueries.IsLeaf(gameObject));
        }

        /**
         * Returns the animation duration using values defined in AbstractSEECity.
         * <param name="isLeaf">Must be true iff the node attached to the game object is a leaf.</param>
         * <param name="city">The city object from which to retrieve the duration.
         * If <code>null</code>, the city object will be retrieved by a call to <see cref="City"/>.</param>
         */
        private float AnimationDuration(bool isLeaf, [CanBeNull] AbstractSEECity city = null)
        {
            city = city ?? City();
            return isLeaf ? city.LeafLabelAnimationDuration : city.InnerNodeLabelAnimationDuration;
        }

        /// <summary>
        /// Destroys the node label. Should only be called after the animations have completed.
        /// </summary>
        private void DestroyLabel() {
            // Only destroy label if we are actually still destroying it.
            // currentlyDestroying may be false if the user hovers away and quickly hovers back, in which case this
            // method will still be called.
            if (nodeLabel != null && currentlyDestroying)
            {
                // FIXME there's what appears to be a racing condition here, where sometimes labels don't get destroyed
                tweens.ForEach(tween => tween.Kill());
                Destroyer.DestroyGameObject(nodeLabel);
                nodeLabel = null;
            }

            currentlyDestroying = false;
        }
    }
}