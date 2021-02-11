using System.Collections.Generic;
using DG.Tweening;
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
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void SelectionOn(InteractableObject interactableObject, bool isOwner)
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
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void SelectionOff(InteractableObject interactableObject, bool isOwner)
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
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void HoverOn(InteractableObject interactableObject, bool isOwner)
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
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void HoverOff(InteractableObject interactableObject, bool isOwner)
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
        /// Will be <c>null</c> when the label is not currently being displayed.
        /// This nodeLabel will contain a TextMeshPro component for the label text.
        /// </summary>
        private GameObject nodeLabel;

        /// <summary>
        /// The edge connecting a <see cref="nodeLabel"/> to its node.
        /// Is a child of <see cref="nodeLabel"/> and contains a
        /// LineRenderer that connects the labeled object and the label text visually.
        /// </summary>
        private GameObject edge;

        /// <summary>
        /// All currently active tweens, collected in a sequence.
        /// </summary>
        private Sequence sequence;

        private bool currentlyDestroying;

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
            return isLeaf && city.LeafLabelSettings.Show || !isLeaf && city.InnerNodeLabelSettings.Show;
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
            // If sequence is being destroyed, we need to stop that and play it forwards again.
            if (nodeLabel != null && currentlyDestroying)
            {
                currentlyDestroying = false;
                // Label and its tweens already exist, so we don't need to change any of that.
                sequence.PlayForward();
                return;
            }
            currentlyDestroying = false;


            Node node = nodeRef.Value;
            if (node == null)
            {
                Debug.LogErrorFormat("Game node {0} has no valid node reference.\n", name);
                return;
            }

            // Now we create the label
            // We define starting and ending positions for the animation
            Vector3 startLabelPosition = gameObject.transform.position;
            nodeLabel = TextFactory.GetTextWithSize(node.SourceName, startLabelPosition,
                                                    (isLeaf ? city.LeafLabelSettings : city.InnerNodeLabelSettings).FontSize, 
                                                    textColor: Color.black.ColorWithAlpha(0f));
            nodeLabel.name = $"Label {node.SourceName}";
            nodeLabel.transform.SetParent(gameObject.transform);
            
            SetOutline();

            // Add connecting line between "roof" of object and text
            Vector3 startLinePosition = gameObject.transform.position;
            startLinePosition.y = BoundingBox.GetRoof(new List<GameObject> {gameObject});
            edge = new GameObject();
            LineFactory.Draw(edge, new[] {startLinePosition, startLinePosition}, 0.01f,
                             Materials.New(Materials.ShaderType.TransparentLine, Color.black));
            edge.transform.SetParent(nodeLabel.transform);
            Portal.SetInfinitePortal(nodeLabel);

            AnimateLabel(city, isLeaf);
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
        /// Animates the given labels by fading them in and gradually changing their position.
        /// </summary>
        /// <param name="city">The <see cref="AbstractSEECity"/> object from which to get the settings.</param>
        /// <param name="isLeaf">Whether this node is a leaf.</param>
        private void AnimateLabel(AbstractSEECity city, bool isLeaf)
        {
            // TODO: Maybe the class in Tweens.cs should be used instead.
            // However, I'm not sure why that class is a MonoBehaviour (shouldn't it be a static helper class?)
            // and DOTween instead recommends using extension methods, so this is what's used here.
            // Additionally, some specific functionality (e.g. callbacks), isn't available from Tweens.cs.

            const float endAlpha = 1f;  // Alpha value the text and line will have at the end of the animation.
            const float lineStartAlpha = endAlpha * 0.5f;  // Alpha value the start of the line should have.
            Vector3 endLabelPosition = nodeLabel.transform.position;
            endLabelPosition.y += (isLeaf ? city.LeafLabelSettings : city.InnerNodeLabelSettings).Distance;
            // Due to the line not using world space, we need to transform its position accordingly
            Vector3 endLinePosition = edge.transform.InverseTransformPoint(endLabelPosition);
            float nodeTopPosition = nodeLabel.GetComponent<TextMeshPro>().textBounds.extents.y;
            endLinePosition.y -= nodeTopPosition * 1.3f; // add slight gap to make it slightly more aesthetic

            float animationDuration = AnimationDuration(isLeaf, city);
            if (animationDuration <= 0)
            {
                // If animation duration is set to 0, all otherwise animated attributes should be set immediately
                SetAttributesImmediately();

                return;
            }
            
            // We will play the animation backwards when going away, so we'll kill the tweens ourself.
            sequence = DOTween.Sequence();
            sequence.SetAutoKill(false);
            sequence.SetLink(nodeLabel, LinkBehaviour.KillOnDestroy);
            // By inserting all tweens in the same position, they will play simultaneously.
            AnimateLabelText();
            AnimateLabelLine();

            #region Local Methods
            void AnimateLabelText()
            {
                // Animated label to move to top and fade in
                if (nodeLabel.TryGetComponent(out TextMeshPro text))
                {
                    sequence.Insert(0, nodeLabel.transform.DOMove(endLabelPosition, animationDuration));
                    sequence.Insert(0, DOTween.ToAlpha(() => text.color, color => text.color = color, endAlpha, animationDuration));
                    sequence.Insert(0, DOTween.ToAlpha(() => text.outlineColor, color => text.outlineColor = color, endAlpha, animationDuration));
                }
                else
                {
                    Debug.LogError("Couldn't find text component in newly created label.\n");
                }
            }

            void AnimateLabelLine()
            {
                // Animated line to move to top and fade in
                if (edge.TryGetComponent(out LineRenderer line))
                {
                    // Reset colors to clear first
                    LineFactory.SetColors(line, Color.clear, Color.clear);

                    // Lower start of line should be visible almost immediately due to reduced alpha (smooth transition)
                    sequence.Insert(0, DOTween.ToAlpha(() => line.startColor, c => line.startColor = c, lineStartAlpha, animationDuration*0.1f));
                    sequence.Insert(0, DOTween.ToAlpha(() => line.endColor, c => line.endColor = c, endAlpha, animationDuration));
                    sequence.Insert(0, DOTween.To(() => line.GetPosition(1), p => line.SetPosition(1, p), endLinePosition, animationDuration));
                }
                else
                {
                    Debug.LogError("Couldn't find line component in newly created label.\n");
                }
            }
            #endregion

            void SetAttributesImmediately()
            {
                // If we have an animation duration of 0, we can set the positions immediately and return.
                if (nodeLabel.TryGetComponent(out TextMeshPro text) && edge.TryGetComponent(out LineRenderer line))
                {
                    nodeLabel.transform.position = endLabelPosition;
                    text.alpha = endAlpha;
                    line.startColor = line.startColor.ColorWithAlpha(lineStartAlpha);
                    line.endColor = line.endColor.ColorWithAlpha(endAlpha);
                    line.SetPosition(1, endLinePosition);
                }
                else
                {
                    Debug.LogError("Couldn't find required component in newly created label.\n");
                }
            }
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
            if (sequence == null)
            {
                // If no sequence exists, animation duration is 0, so we immediately destroy the label.
                DestroyLabel(nodeLabel);
            }
            else
            {
                // Fade out and move label down
                sequence?.PlayBackwards();
                sequence?.OnPause(() => DestroyLabel(nodeLabel));
            }
        }

        /**
         * Returns the animation duration using values defined in AbstractSEECity.
         * <param name="isLeaf">Must be true iff the node attached to the game object is a leaf.</param>
         * <param name="city">The city object from which to retrieve the duration.
         * If <code>null</code>, the city object will be retrieved by a call to <see cref="City"/>.</param>
         */
        private float AnimationDuration(bool isLeaf, AbstractSEECity city = null)
        {
            city = city ?? City();
            return (isLeaf ? city.LeafLabelSettings : city.InnerNodeLabelSettings).AnimationDuration;
        }

        /// <summary>
        /// Destroys the node label. Should only be called after the animations have completed.
        /// </summary>
        private void DestroyLabel(GameObject animatedLabel) {
            // Only destroy label if it still exists and if no animation is playing.
            if (animatedLabel != null && currentlyDestroying)
            {
                Destroyer.DestroyGameObject(animatedLabel);
            }

            currentlyDestroying = false;
        }
    }
}
