// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.Controls;
using SEE.GO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Contains the logic for the markers representing entries linked to objects in the chart.
    /// </summary>
    public class ChartMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// The interactable objects, that are displayed by this marker.
        /// </summary>
        public readonly List<InteractableObject> LinkedInteractableObjects = new List<InteractableObject>();

        /// <summary>
        /// The chart content, on which this marker is displayed.
        /// </summary>
        public ChartContent chartContent;

        /// <summary>
        /// The information to be displayed for each linked interactable object,
        /// respectively.
        /// </summary>
        private readonly List<string> infoTexts = new List<string>();

        /// <summary>
        /// The icon of the marker.
        /// </summary>
        private UnityEngine.UI.Image image;

        /// <summary>
        /// A text popup containing useful information about the marker and its linked
        /// interactable objects.
        /// </summary>
        [SerializeField] private TextMeshProUGUI infoText;

        /// <summary>
        /// The <see cref="GameObject"/> making the marker look highlighted when active.
        /// </summary>
        [SerializeField] private GameObject markerHighlight;

        private void Awake()
        {
            infoText.text = string.Empty;
            infoText.color = UIColorScheme.GetLight(0);
            image = GetComponent<UnityEngine.UI.Image>();
            markerHighlight.GetComponent<UnityEngine.UI.Image>().color = UIColorScheme.GetLight(2);
        }

        private void OnDestroy()
        {
            foreach (InteractableObject interactableObject in LinkedInteractableObjects)
            {
                interactableObject.HoverIn -= OnHoverIn;
                interactableObject.HoverOut -= OnHoverOut;
                interactableObject.SelectIn -= OnSelectIn;
                interactableObject.SelectOut -= OnSelectOut;
            }
        }

        /// <summary>
        /// Adds an interactable object with given info text to this marker.
        /// </summary>
        /// <param name="interactableObject">The object to be added.</param>
        /// <param name="infoText">The text to be displayed for the given object.</param>
        public void PushInteractableObject(InteractableObject interactableObject, string infoText)
        {
            if (!LinkedInteractableObjects.Contains(interactableObject))
            {
                LinkedInteractableObjects.Add(interactableObject);

                interactableObject.HoverIn += OnHoverIn;
                interactableObject.HoverOut += OnHoverOut;
                interactableObject.SelectIn += OnSelectIn;
                interactableObject.SelectOut += OnSelectOut;

                infoTexts.Add(infoText);

                if (interactableObject.IsHovered)
                {
                    // TODO(torben): the owner should be cached inside InteractableObject, create functions like e.g. IsHoveredByThisClient()...
                    OnHoverIn(interactableObject, true);
                }
                if (interactableObject.IsSelected)
                {
                    // TODO(torben): the owner should be cached inside InteractableObject, create functions like e.g. IsHoveredByThisClient()...
                    OnSelectIn(interactableObject, true);
                }
            }
        }

        /// <summary>
        /// Updates the info text to be displayed for this marker. The info texts for
        /// every hovered and/or selected object is displayed. If no such object exists,
        /// the info text is disabled.
        /// </summary>
        public void UpdateInfoText()
        {
            bool showInfoText = false;
            foreach (InteractableObject interactableObject in LinkedInteractableObjects)
            {
                if (interactableObject.IsHovered || interactableObject.IsSelected)
                {
                    showInfoText = true;
                    break;
                }
            }

            infoText.gameObject.SetActive(showInfoText);
            if (showInfoText)
            {
                string text = string.Empty;
                for (int i = 0; i < LinkedInteractableObjects.Count; i++)
                {
                    bool showInChart = (bool)LinkedInteractableObjects[i].GetComponent<NodeRef>().showInChart[chartContent];
                    bool isHighlighted = LinkedInteractableObjects[i].IsHovered || LinkedInteractableObjects[i].IsSelected;
                    if (showInChart && isHighlighted)
                    {
                        text += infoTexts[i] + '\n';
                    }
                }
                infoText.text = text;
            }
        }

        /// <summary>
        /// Updates the visibility of the marker. If none of this marker's linked
        /// interactable objects should be shown in the chart, this marker is made
        /// invisible.
        /// </summary>
        public void UpdateVisibility()
        {
            bool isVisible = false;
            foreach (InteractableObject interactableObject in LinkedInteractableObjects)
            {
                bool showInChart = (bool)interactableObject.GetComponent<NodeRef>().showInChart[chartContent];
                if (showInChart)
                {
                    isVisible = true;
                    break;
                }
            }
            image.enabled = isVisible;
        }

        #region UnityEngine Callbacks

        /// <summary>
        /// Called by Unity, if this marker is clicked.
        /// 
        /// Selects/Toggles the linked interactable objects of this marker.
        /// </summary>
        public void ButtonClicked()
        {
            if (image.enabled)
            {
                // TODO(torben): the action state could be global for some cases. the line below exists in DesktopNavigationAction.cs and could somewhat be shared
                //actionState.selectToggle = Input.GetKey(KeyCode.LeftControl);
                if (!Input.GetKey(KeyCode.LeftControl))
                {
                    InteractableObject.UnhoverAll(true);
                    InteractableObject.UnselectAll(true);
                }

                foreach (InteractableObject interactableObject in LinkedInteractableObjects)
                {
                    interactableObject.SetSelect(!interactableObject.IsSelected, true);
                }
            }
        }

        /// <summary>
        /// Called by Unity, if the mouse hovers over this marker.
        /// 
        /// Hoveres every linked interactable object of this marker.
        /// </summary>
        /// <param name="eventData">Ignored.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            foreach (InteractableObject interactableObject in LinkedInteractableObjects)
            {
                interactableObject.SetHoverFlag(HoverFlag.ChartMarker, true, true);
            }
        }

        /// <summary>
        /// Called by Unity, if the mouse stops hovering over this marker.
        /// 
        /// Unhoveres every linked interactable object of this marker.
        /// </summary>
        /// <param name="eventData">Ignored.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            foreach (InteractableObject interactableObject in LinkedInteractableObjects)
            {
                interactableObject.SetHoverFlag(HoverFlag.ChartMarker, false, true);
            }
        }

        #endregion

        #region InteractableObject Callbacks

        /// <summary>
        /// Called through event <see cref="InteractableObject.HoverIn"/>.
        /// 
        /// Updates the info text.
        /// </summary>
        /// <param name="interactableObject">Ignored.</param>
        /// <param name="isOwner">Ignored.</param>
        public void OnHoverIn(InteractableObject interactableObject, bool isOwner)
        {
            UpdateInfoText();
        }

        /// <summary>
        /// Called through event <see cref="InteractableObject.HoverOut"/>.
        /// 
        /// Updates the info text.
        /// </summary>
        /// <param name="interactableObject">Ignored.</param>
        /// <param name="isOwner">Ignored.</param>
        public void OnHoverOut(InteractableObject interactableObject, bool isOwner)
        {
            UpdateInfoText();
        }

        /// <summary>
        /// Called through event <see cref="InteractableObject.SelectIn"/>.
        /// 
        /// Updates the info text and highlights this marker.
        /// </summary>
        /// <param name="interactableObject">Ignored.</param>
        /// <param name="isOwner">Ignored.</param>
        public void OnSelectIn(InteractableObject interactableObject, bool isOwner)
        {
            UpdateInfoText();
            markerHighlight.SetActive(true);
        }

        /// <summary>
        /// Called through event <see cref="InteractableObject.SelectOut"/>.
        /// 
        /// Updates the info text and stops highlighting this marker, if no other linked
        /// interactable object is still selected.
        /// </summary>
        /// <param name="interactableObject">Ignored.</param>
        /// <param name="isOwner">Ignored.</param>
        public void OnSelectOut(InteractableObject interactableObject, bool isOwner)
        {
            UpdateInfoText();
            bool showMarker = false;
            foreach (InteractableObject io in LinkedInteractableObjects)
            {
                if (io.IsSelected)
                {
                    showMarker = true;
                    break;
                }
            }
            markerHighlight.SetActive(showMarker);
        }

        #endregion
    }
}