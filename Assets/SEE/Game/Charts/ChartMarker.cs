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
        private readonly List<InteractableObject> linkedInteractableObjects = new List<InteractableObject>();
        public IEnumerable<InteractableObject> LinkedInteractableObjects { get => linkedInteractableObjects; }

        private readonly List<string> infoTexts = new List<string>();

        public ChartContent chartContent;

        private UnityEngine.UI.Image image;

        /// <summary>
        /// A text popup containing useful information about the marker and its <see cref="LinkedInteractable"/>.
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
            foreach (InteractableObject interactableObject in linkedInteractableObjects)
            {
                interactableObject.HoverIn -= OnHoverIn;
                interactableObject.HoverOut -= OnHoverOut;
                interactableObject.SelectIn -= OnSelectIn;
                interactableObject.SelectOut -= OnSelectOut;
            }
        }

        public void PushInteractableObject(InteractableObject interactableObject, string infoText)
        {
            if (!linkedInteractableObjects.Contains(interactableObject))
            {
                linkedInteractableObjects.Add(interactableObject);

                interactableObject.HoverIn += OnHoverIn;
                interactableObject.HoverOut += OnHoverOut;
                interactableObject.SelectIn += OnSelectIn;
                interactableObject.SelectOut += OnSelectOut;

                infoTexts.Add(infoText);
            }
        }

        public void UpdateInfoText()
        {
            string text = string.Empty;
            for (int i = 0; i < linkedInteractableObjects.Count; i++)
            {
                bool showInChart = (bool)linkedInteractableObjects[i].GetComponent<NodeRef>().highlights.showInChart[chartContent];
                bool isHighlighted = linkedInteractableObjects[i].IsHovered || linkedInteractableObjects[i].IsSelected;
                if (showInChart && isHighlighted)
                {
                    text += infoTexts[i] + '\n';
                }
            }
            infoText.text = text;
        }

        public void UpdateVisibility()
        {
            bool isVisible = false;
            foreach (InteractableObject interactableObject in linkedInteractableObjects)
            {
                bool showInChart = (bool)interactableObject.GetComponent<NodeRef>().highlights.showInChart[chartContent];
                if (showInChart)
                {
                    isVisible = true;
                    break;
                }
            }
            image.enabled = isVisible;
        }

        #region UnityEngine Callbacks

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

                foreach (InteractableObject interactableObject in linkedInteractableObjects)
                {
                    interactableObject.SetSelect(!interactableObject.IsSelected, true);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            foreach (InteractableObject interactableObject in linkedInteractableObjects)
            {
                interactableObject.SetHoverFlag(HoverFlag.ChartMarker, true, true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            foreach (InteractableObject interactableObject in linkedInteractableObjects)
            {
                interactableObject.SetHoverFlag(HoverFlag.ChartMarker, false, true);
            }
        }

        #endregion

        #region InteractableObject Callbacks

        public void OnHoverIn(bool isOwner)
        {
            UpdateInfoText();
            infoText.gameObject.SetActive(true);
        }

        public void OnHoverOut(bool isOwner)
        {
            if (!linkedInteractableObjects[0].IsSelected)
            {
                infoText.gameObject.SetActive(false);
            }
            else
            {
                UpdateInfoText();
            }
        }

        public void OnSelectIn(bool isOwner)
        {
            UpdateInfoText();
            infoText.gameObject.SetActive(true);
            markerHighlight.SetActive(true);
        }

        public void OnSelectOut(bool isOwner)
        {
            if (!linkedInteractableObjects[0].IsHovered)
            {
                infoText.gameObject.SetActive(false);
            }
            else
            {
                UpdateInfoText();
            }
            markerHighlight.SetActive(false);
        }

        #endregion
    }
}