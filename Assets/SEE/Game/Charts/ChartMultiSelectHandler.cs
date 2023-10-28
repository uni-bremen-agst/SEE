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
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Handles selection of multiple markers in selection mode.
    /// </summary>
    public class ChartMultiSelectHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>
        /// The rectangle used to visualize the selection process for the user.
        /// </summary>
        [SerializeField] protected RectTransform SelectionRect;

        /// <summary>
        /// Needed for access to <see cref="Scripts.ChartContent.AreaSelection"/>.
        /// </summary>
        protected ChartContent ChartContent;

        /// <summary>
        /// The position the user started the drag at.
        /// </summary>
        protected Vector3 StartingPos;

        /// <summary>
        /// Assigns <see cref="ChartContent"/>.
        /// </summary>
        private void Awake()
        {
            ChartContent = transform.parent.GetComponent<ChartContent>();
        }

        #region UnityEngine Callbacks

        /// <summary>
        /// Activates and sets starting position of <see cref="SelectionRect"/>.
        /// </summary>
        /// <param name="eventData">Contains the position data.</param>
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            SelectionRect.gameObject.SetActive(true);
            SelectionRect.position = eventData.pressPosition;
            StartingPos = SelectionRect.position;
            SelectionRect.sizeDelta = new Vector2(0.0f, 0.0f);
            if (!SEEInput.ToggleMetricHoveringSelection())
            {
                InteractableObject.UnhoverAll(true);
                InteractableObject.UnselectAll(true);
            }
        }

        /// <summary>
        /// Resizes the <see cref="SelectionRect"/> to make it span from <see cref="StartingPos"/> to
        /// <see cref="PointerEventData.position"/>.
        /// </summary>
        /// <param name="eventData">Contains the position data.</param>
        public virtual void OnDrag(PointerEventData eventData)
        {
            bool negative = false;
            Vector3 lossyScale = SelectionRect.lossyScale;
            Vector2 sizeDelta = Vector2.zero;

            if (eventData.position.x - StartingPos.x < 0)
            {
                SelectionRect.sizeDelta = new Vector2(
                    Mathf.Abs(eventData.position.x - StartingPos.x) / SelectionRect.lossyScale.x,
                    (eventData.position.y - StartingPos.y) / lossyScale.y
                );
                sizeDelta = SelectionRect.sizeDelta;
                SelectionRect.position = new Vector3(
                    StartingPos.x - sizeDelta.x / 2 * lossyScale.x,
                    StartingPos.y + sizeDelta.y / 2 * lossyScale.y,
                    0
                );
                negative = true;
            }

            if (eventData.position.y - StartingPos.y < 0)
            {
                if (negative)
                {
                    SelectionRect.sizeDelta = new Vector2(
                        SelectionRect.sizeDelta.x,
                        Mathf.Abs(eventData.position.y - StartingPos.y) / SelectionRect.lossyScale.y
                    );
                    SelectionRect.position = new Vector3(
                        SelectionRect.position.x,
                        StartingPos.y - SelectionRect.sizeDelta.y / 2 * lossyScale.y,
                        0
                    );
                }
                else
                {
                    SelectionRect.sizeDelta = new Vector2(
                        (eventData.position.x - StartingPos.x) / lossyScale.x,
                        Mathf.Abs(eventData.position.y - StartingPos.y) / lossyScale.y
                    );
                    sizeDelta = SelectionRect.sizeDelta;
                    SelectionRect.position = new Vector3(
                        StartingPos.x + sizeDelta.x / 2 * lossyScale.x,
                        StartingPos.y - sizeDelta.y / 2 * lossyScale.y,
                        0
                    );
                    negative = true;
                }
            }

            if (!negative)
            {
                SelectionRect.sizeDelta = new Vector2(
                    (eventData.position.x - StartingPos.x) / lossyScale.x,
                    (eventData.position.y - StartingPos.y) / lossyScale.y
                );
                sizeDelta = SelectionRect.sizeDelta;
                SelectionRect.position = new Vector3(
                    StartingPos.x + sizeDelta.x / 2 * lossyScale.x,
                    StartingPos.y + sizeDelta.y / 2 * lossyScale.y,
                    0
                );
            }

            Vector2 min = Vector2.Min(StartingPos, eventData.position);
            Vector2 max = Vector2.Max(StartingPos, eventData.position);
            ChartContent.AreaHover(min, max);
        }

        /// <summary>
        /// Highlights all markers in <see cref="SelectionRect"/> and deactivates it.
        /// </summary>
        /// <param name="eventData">Contains the position data.</param>
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            InteractableObject.UnhoverAll(true);
            Vector2 min = Vector2.Min(StartingPos, eventData.position);
            Vector2 max = Vector2.Max(StartingPos, eventData.position);
            ChartContent.AreaSelection(min, max);
            SelectionRect.gameObject.SetActive(false);
        }

        #endregion
    }
}
