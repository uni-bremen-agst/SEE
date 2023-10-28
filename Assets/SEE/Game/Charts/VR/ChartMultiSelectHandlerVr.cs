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

using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts.VR
{
    /// <summary>
    /// The VR version of <see cref="ChartMultiSelectHandler" />.
    /// </summary>
    public class ChartMultiSelectHandlerVr : ChartMultiSelectHandler
    {
        /// <summary>
        /// An object helping to transform world coordinated to chart coordinates.
        /// </summary>
        [SerializeField] private RectTransform reference;

        /// <summary>
        /// The position of the <see cref="reference" />.
        /// </summary>
        private Vector3 referencePos;

        /// <summary>
        /// Sets the starting positions of <see cref="ChartMultiSelectHandler.SelectionRect" /> and
        /// <see cref="reference" />.
        /// </summary>
        /// <param name="eventData">Contains position data of the pointer.</param>
        public override void OnPointerDown(PointerEventData eventData)
        {
            SelectionRect.gameObject.SetActive(true);
            SelectionRect.position = eventData.pointerCurrentRaycast.worldPosition;
            SelectionRect.anchoredPosition3D = new Vector3(SelectionRect.anchoredPosition.x,
                SelectionRect.anchoredPosition.y, 0);
            StartingPos = SelectionRect.anchoredPosition;
            SelectionRect.sizeDelta = new Vector2(0f, 0f);
            reference.anchoredPosition3D = SelectionRect.anchoredPosition3D;
        }

        /// <summary>
        /// Updates the <see cref="referencePos" /> and the
        /// <see cref="ChartMultiSelectHandler.SelectionRect" />.
        /// </summary>
        /// <param name="eventData">Contains position data of the pointer.</param>
        public override void OnDrag(PointerEventData eventData)
        {
            bool negative = false;
            Vector2 sizeDelta = Vector2.zero;
            reference.position = eventData.pointerCurrentRaycast.worldPosition;
            reference.anchoredPosition3D = new Vector3(reference.anchoredPosition.x,
                reference.anchoredPosition.y, 0);
            referencePos = reference.anchoredPosition;

            if (referencePos.x - StartingPos.x < 0)
            {
                SelectionRect.sizeDelta = new Vector2(Mathf.Abs(referencePos.x - StartingPos.x),
                    referencePos.y - StartingPos.y);
                sizeDelta = SelectionRect.sizeDelta;
                SelectionRect.anchoredPosition = new Vector3(StartingPos.x - sizeDelta.x / 2,
                    StartingPos.y + sizeDelta.y / 2, 0);
                negative = true;
            }

            if (referencePos.y - StartingPos.y < 0)
            {
                if (negative)
                {
                    SelectionRect.sizeDelta = new Vector2(SelectionRect.sizeDelta.x,
                        Mathf.Abs(referencePos.y - StartingPos.y));
                    SelectionRect.anchoredPosition = new Vector3(SelectionRect.anchoredPosition.x,
                        StartingPos.y - SelectionRect.sizeDelta.y / 2, 0);
                }
                else
                {
                    SelectionRect.sizeDelta = new Vector2(referencePos.x - StartingPos.x,
                        Mathf.Abs(referencePos.y - StartingPos.y));
                    sizeDelta = SelectionRect.sizeDelta;
                    SelectionRect.anchoredPosition = new Vector3(StartingPos.x + sizeDelta.x / 2,
                        StartingPos.y - sizeDelta.y / 2, 0);
                    negative = true;
                }
            }

            if (negative)
            {
                return;
            }

            SelectionRect.sizeDelta = new Vector2(referencePos.x - StartingPos.x,
                referencePos.y - StartingPos.y);
            sizeDelta = SelectionRect.sizeDelta;
            SelectionRect.anchoredPosition = new Vector3(StartingPos.x + sizeDelta.x / 2,
                StartingPos.y + sizeDelta.y / 2, 0);
        }

        /// <summary>
        /// Checks the area that has been dragged and sends it to <see cref="ChartContentVr.AreaSelection" />.
        /// </summary>
        /// <param name="eventData">Contains position data of the pointer.</param>
        public override void OnPointerUp(PointerEventData eventData)
        {
            Vector2 min = Vector2.Min(StartingPos, referencePos);
            Vector2 max = Vector2.Max(StartingPos, referencePos);
            ChartContent.AreaSelection(min, max);
            SelectionRect.gameObject.SetActive(false);
        }
    }
}