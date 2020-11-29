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
        private readonly HashSet<InteractableObject> linkedInteractableObjects = new HashSet<InteractableObject>();
        public IEnumerable<InteractableObject> LinkedInteractableObjects { get => linkedInteractableObjects; }

        /// <summary>
        /// A text popup containing useful information about the marker and its <see cref="LinkedInteractable"/>.
        /// </summary>
        [SerializeField] private TextMeshProUGUI infoText;

        /// <summary>
        /// The <see cref="GameObject" /> making the marker look highlighted when active.
        /// </summary>
        [SerializeField] private GameObject markerHighlight;

        /// <summary>
        /// True iff the marker is accentuated.
        /// </summary>
        private bool _accentuated;

        private void Awake()
        {
            infoText.text = string.Empty;
        }

        public void AddInteractableObject(InteractableObject interactableObject, string infoText)
        {
            if (linkedInteractableObjects.Add(interactableObject))
            {
                interactableObject.HoverIn += OnHoverIn;
                interactableObject.HoverOut += OnHoverOut;
                interactableObject.SelectIn += OnSelectIn;
                interactableObject.SelectOut += OnSelectOut;

                this.infoText.text += "\n" + infoText;
            }
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

        #region UnityEngine Callbacks

        public void ButtonClicked()
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

        public void OnHoverIn(bool isOwner) => infoText.gameObject.SetActive(true);
        public void OnHoverOut(bool isOwner) => infoText.gameObject.SetActive(false);
        public void OnSelectIn(bool isOwner) => markerHighlight.SetActive(true);
        public void OnSelectOut(bool isOwner) => markerHighlight.SetActive(false);

        #endregion
    }
}