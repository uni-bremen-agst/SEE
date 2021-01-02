using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
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
        /// <summary>
        /// Sets <see cref="isLeaf"/> and <see cref="city"/>.
        /// </summary>
        protected override void Awake()           
        {   
            base.Awake();  
            isLeaf = SceneQueries.IsLeaf(gameObject);
            GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
            Assert.IsTrue(codeCityObject != null);
            codeCityObject.TryGetComponent(out city);
        }

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
        /// True if this node is a leaf. This value is cached to avoid frequent retrievals.
        /// </summary>
        private bool isLeaf;

        /// <summary>
        /// The text label that's displayed above the object when the user hovers over it.
        /// Will be <code>null</code> when the label is not currently being displayed.
        /// This nodeLabel will contain a TextMeshPro component for the label text and a
        /// LineRenderer that connects the labeled object and the label text visually.
        /// </summary>
        private GameObject nodeLabel;

        /// <summary>
        /// Settings for the visualization of the node.
        /// </summary>
        private AbstractSEECity city;

        /// <summary>
        /// Returns true iff labels are enabled for this node type.
        /// </summary>
        /// <returns>true iff labels are enabled for this node type</returns>
        private bool LabelsEnabled()
        {
            return isLeaf && city.ShowLabel || !isLeaf && city.InnerNodeShowLabel;
        }

        /// <summary>
        /// Creates a text label above the object with its node's SourceName if the label doesn't exist yet.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        private void On()
        {
            if (!LabelsEnabled())
            {
                return;  // If labels are disabled, we don't need to do anything
            }

            // If label already exists, nothing needs to be done
            if (nodeLabel != null || !gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                return;
            }

            Node node = nodeRef.Value;
            if (node == null)
            {
                return;
            }

            // Add text
            Vector3 position = gameObject.transform.position;
            position.y += isLeaf ? city.LeafLabelDistance : city.InnerNodeLabelDistance;
            nodeLabel = TextFactory.GetTextWithSize(node.SourceName, position,
                isLeaf ? city.LeafLabelFontSize : city.InnerNodeLabelFontSize, textColor: Color.black);
            nodeLabel.name = "Label " + node.SourceName;
            nodeLabel.transform.SetParent(gameObject.transform);
            
            // Add connecting line between "roof" of object and text
            Vector3 labelPosition = nodeLabel.transform.position;
            Vector3 nodeTopPosition = gameObject.transform.position;
            nodeTopPosition.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
            labelPosition.y -= nodeLabel.GetComponent<TextMeshPro>().textBounds.extents.y;
            LineFactory.Draw(nodeLabel, new[] { nodeTopPosition, labelPosition }, 0.01f,
                Materials.New(Materials.ShaderType.TransparentLine, Color.black.ColorWithAlpha(0.98f)));

            Portal.SetInfinitePortal(nodeLabel);
        }

        /// <summary>
        /// Destroys the text label above the object if it exists.
        /// 
        /// <seealso cref="SelectionOn"/>
        /// </summary>
        private void Off()
        {
            // If labels are disabled, we don't need to do anything
            if (LabelsEnabled() && nodeLabel != null)
            {
                Destroyer.DestroyGameObject(nodeLabel);
            }
        }
    }
}