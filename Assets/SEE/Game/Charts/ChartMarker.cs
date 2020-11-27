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
        private InteractableObject linkedInteractable = null;

        /// <summary>
        /// The <see cref="GameObject" /> in the code city that is connected with this button.
        /// </summary>
        [HideInInspector] public GameObject LinkedObject
        {
            get
            {
                return linkedInteractable ? linkedInteractable.gameObject : null;
            }
            set
            {
                if (linkedInteractable)
                {
                    linkedInteractable.HoverIn   -= OnHoverIn;
                    linkedInteractable.HoverOut  -= OnHoverOut;
                    linkedInteractable.SelectIn  -= OnSelectIn;
                    linkedInteractable.SelectOut -= OnSelectOut;
                }

                if (value && value.TryGetComponent(out InteractableObject interactableObj))
                {
                    linkedInteractable = interactableObj;
                    linkedInteractable.HoverIn   += OnHoverIn;
                    linkedInteractable.HoverOut  += OnHoverOut;
                    linkedInteractable.SelectIn  += OnSelectIn;
                    linkedInteractable.SelectOut += OnSelectOut;
                }
                else
                {
                    linkedInteractable = null;
                }
            }
        }
        [HideInInspector] public InteractableObject LinkedInteractable => linkedInteractable;

        /// <summary>
        /// A text popup containing useful information about the marker and its <see cref="LinkedObject"/>.
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

        /// <summary>
        /// Changes the <see cref="infoText"/> of this marker.
        /// </summary>
        /// <param name="info">The new text.</param>
        public void SetInfoText(string info)
        {
            infoText.text = info;
        }

        #region UnityEngine Callbacks

        public void ButtonClicked() => linkedInteractable?.SetSelect(!linkedInteractable.IsSelected, true);
        public void OnPointerEnter(PointerEventData eventData) => linkedInteractable?.SetHover(true, true);
        public void OnPointerExit(PointerEventData eventData) => linkedInteractable?.SetHover(false, true);

        #endregion

        #region InteractableObject Callbacks

        public void OnHoverIn(bool isOwner) => infoText.gameObject.SetActive(true);
        public void OnHoverOut(bool isOwner) => infoText.gameObject.SetActive(false);
        public void OnSelectIn(bool isOwner) => markerHighlight.SetActive(true);
        public void OnSelectOut(bool isOwner) => markerHighlight.SetActive(false);

        #endregion
    }
}