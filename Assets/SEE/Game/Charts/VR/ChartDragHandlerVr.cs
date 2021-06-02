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
    /// The VR version of <see cref="ChartDragHandler" />.
    /// </summary>
    public class ChartDragHandlerVr : ChartDragHandler
    {
        /// <summary>
        /// The transform of the object to drag.
        /// </summary>
        private Transform _parent;

        /// <summary>
        /// The distance between the pointer and the middle of the chart.
        /// </summary>
        private Vector3 _distance;

        /// <summary>
        /// Finds the <see cref="_parent" />.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            // FIXME the line below was here before refactoring charts and needs to be reintroduced for VR
            //_parent = transform.parent.GetComponent<ChartContent>().parent.transform;
        }

        /// <summary>
        /// Moves the chart to the new position of the pointer if the raycast hit something.
        /// </summary>
        /// <param name="eventData">Contains position data of the pointer.</param>
        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.worldPosition != Vector3.zero)
            {
                _parent.position = eventData.pointerCurrentRaycast.worldPosition - _distance;
            }
        }

        /// <summary>
        /// Saves the distance between the pointer and the middle of the chart.
        /// </summary>
        /// <param name="eventData">Contains position data of the pointer.</param>
        public override void OnPointerDown(PointerEventData eventData)
        {
            _distance = eventData.pointerCurrentRaycast.worldPosition - chart.position;
        }
    }
}