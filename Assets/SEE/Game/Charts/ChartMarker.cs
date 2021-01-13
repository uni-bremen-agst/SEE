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
using UnityEngine.UI;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Contains the logic for the markers representing entries linked to objects in the chart.
    /// </summary>
    public class ChartMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// The <see cref="GameObject" /> in the code city that is connected with this button.
        /// </summary>
        [HideInInspector] public GameObject linkedObject;

        /// <summary>
        /// The toggle linked to this marker.
        /// </summary>
        public ScrollViewToggle ScrollViewToggle { private get; set; }

        /// <summary>
        /// The active <see cref="Camera" /> in the scene.
        /// </summary>
        private Camera _activeCamera;

        /// <summary>
        /// The currently running camera movement <see cref="Coroutine" />.
        /// </summary>
        private Coroutine _cameraMoving;

        /// <summary>
        /// The currently running <see cref="TimedHighlightRoutine" />.
        /// </summary>
        public Coroutine TimedHighlight { get; private set; }

        /// <summary>
        /// Counts the time <see cref="TimedHighlight" /> has been running for.
        /// </summary>
        public float HighlightTime { get; private set; }

        /// <summary>
        /// The <see cref="GameObject" /> making the marker look highlighted when active.
        /// </summary>
        [Header("Highlight Properties"), SerializeField]
        private GameObject markerHighlight;

        /// <summary>
        /// True iff the marker is accentuated.
        /// </summary>
        private bool _accentuated;

        /// <summary>
        /// A text popup containing useful information about the marker and its <see cref="linkedObject" />.
        /// </summary>
        [Header("Other"), SerializeField]
        private TextMeshProUGUI infoText;

        /// <summary>
        /// Reactivates the highlight if a previous marker linked to the same <see cref="linkedObject" />
        /// highlighted it.
        /// </summary>
        private void Start()
        {
            //for (var i = 0; i < linkedObject.transform.childCount; i++)
            //{
            //    var child = linkedObject.transform.GetChild(i);
            //    if (child.name.Equals(linkedObject.name + "(Clone)"))
            //    {
            //        TriggerTimedHighlight(ChartManager.Instance.highlightDuration, false, false);
            //        break;
            //    }
            //}
        }

        /// <summary>
        /// Adds the time that passed since the last <see cref="Update" /> to the <see cref="HighlightTime" />.
        /// </summary>
        private void Update()
        {
            // FIXME: Can be deleted.
            if (TimedHighlight != null)
            {
                HighlightTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Called by Unity when the button assigned to the <see cref="ChartMarker" /> is pressed.
        /// </summary>
        public void ButtonClicked()
        {
            ChartManager.OnSelect(linkedObject);
        }

        /// <summary>
        /// Sets the highlight of the <see cref="linkedObject" /> and this marker
        /// to <paramref name="isHighlighted"/>.
        /// </summary>
        /// <param name="isHighlighted">whether or not the marker should be highlighted</param>
        public void SetHighlightLinkedObject(bool isHighlighted)
        {
            if (linkedObject.TryGetComponent(out InteractableObject interactableObject))
            {
                // We need to call SetSelect only if there is a difference.
                if (isHighlighted != interactableObject.IsSelected)
                {
                    interactableObject.SetSelect(isHighlighted, true);
                }
                // Assert: isHighlighted = interactableObject.IsSelected.
                _accentuated = !isHighlighted;

                markerHighlight.SetActive(isHighlighted);
                if (ScrollViewToggle)
                {
                    ScrollViewToggle.SetHighlighted(isHighlighted);
                }
            }
        }

        /// <summary>
        /// Changes the color of the marker to the accentuation color.
        /// </summary>
        public void ToggleAccentuation()
        {
            if (markerHighlight.TryGetComponent(out Image image))
            {
                image.color = _accentuated ? ChartManager.Instance.standardColor : ChartManager.Instance.accentuationColor;
            }
            _accentuated = !_accentuated;
        }

        /// <summary>
        /// Changes the <see cref="infoText" /> of this marker.
        /// </summary>
        /// <param name="info">The new text.</param>
        public void SetInfoText(string info)
        {
            infoText.text = info;
        }

        /// <summary>
        /// Activates the <see cref="infoText" />.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            infoText.gameObject.SetActive(true);
            if (TimedHighlight != null)
            {
                ChartManager.Accentuate(linkedObject);
            }
        }

        /// <summary>
        /// Deactivates the <see cref="infoText" />.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            infoText.gameObject.SetActive(false);
            if (_accentuated)
            {
                ChartManager.Accentuate(linkedObject);
            }
        }

        /// <summary>
        /// Stops all co-routines.
        /// </summary>
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}