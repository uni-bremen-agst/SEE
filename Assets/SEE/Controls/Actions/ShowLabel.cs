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
            else
            {
                codeCityObject.TryGetComponent(out AbstractSEECity city);
                return city;
            }
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
            if (nodeLabel != null || !gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                return;
            }

            Node node = nodeRef.node;
            if (node == null)
            {
                Debug.LogErrorFormat("Game node {0} has no valid node reference.\n", name);
                return;
            }

            // Now we create the label
            // We define starting and ending positions for the animation
            Vector3 startLabelPosition = gameObject.transform.position;
            Vector3 endLabelPosition = gameObject.transform.position;
            endLabelPosition.y += isLeaf ? city.LeafLabelDistance : city.InnerNodeLabelDistance;
            nodeLabel = TextFactory.GetTextWithSize(node.SourceName, startLabelPosition,
                                                    isLeaf ? city.LeafLabelFontSize : city.InnerNodeLabelFontSize, textColor: Color.black.ColorWithAlpha(0f));
            nodeLabel.name = $"Label {node.SourceName}";
            nodeLabel.transform.SetParent(gameObject.transform);

            // Add connecting line between "roof" of object and text
            Vector3 startLinePosition = gameObject.transform.position;
            Vector3 endLinePosition = endLabelPosition;
            float nodeTopPosition = nodeLabel.GetComponent<TextMeshPro>().textBounds.extents.y;
            startLinePosition.y = BoundingBox.GetRoof(new List<GameObject> {gameObject});
            endLinePosition.y -= nodeTopPosition * 1.3f; // add slight gap to make it slightly more aesthetic
            LineFactory.Draw(nodeLabel, new[] {startLinePosition, startLinePosition}, 0.01f,
                             Materials.New(Materials.ShaderType.TransparentLine, Color.black));
            Portal.SetInfinitePortal(nodeLabel);

            // Animated label to move to top and fade in
            if (nodeLabel.TryGetComponent(out TextMeshPro text))
            {
                // TODO: Maybe the class in Tweens.cs should be used instead.
                // However, I'm not sure why that class is a MonoBehaviour (shouldn't it be a static helper class?)
                // and DOTween instead recommends using extension methods, so this is what's used here.
                nodeLabel.transform.DOMove(endLabelPosition, 0.5f);
                DOTween.ToAlpha(() => text.color, color => text.color = color, 1f, 0.5f);
            }
            else
            {
                Debug.LogError("Couldn't find text component in newly created label.\n");
            }

            // Animated line to move to top and fade in
            if (nodeLabel.TryGetComponent(out LineRenderer line))
            {
                // Reset colors to clear first
                LineFactory.SetColors(line, Color.clear, Color.clear);

                DOTween.ToAlpha(() => line.startColor, c => line.startColor = c, 0.25f, 0.5f);
                DOTween.ToAlpha(() => line.endColor, c => line.endColor = c, 1f, 0.5f);
                DOTween.To(() => line.GetPosition(1), p => line.SetPosition(1, p), endLinePosition, 0.5f);
            }
            else
            {
                Debug.LogError("Couldn't find line component in newly created label.\n");
            }
        }

        /// <summary>
        /// Destroys the text label above the object if it exists.
        /// 
        /// <seealso cref="SelectionOn"/>
        /// </summary>
        private void Off()
        {
            if (nodeLabel != null)
            {
                Destroyer.DestroyGameObject(nodeLabel);
                nodeLabel = null;
            }
        }
    }
}