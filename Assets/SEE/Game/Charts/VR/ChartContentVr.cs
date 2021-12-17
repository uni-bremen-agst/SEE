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

using System.Collections.Generic;
using SEE.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.Charts.VR
{
    /// <summary>
    /// The virtual reality version of <see cref="ChartContent" />.
    /// </summary>
    public class ChartContentVr : ChartContent
    {
        /// <summary>
        /// A cube behind the chart to make it look three dimensional.
        /// </summary>
        public GameObject physicalOpen;

        /// <summary>
        /// The minimized chart displayed as a cube.
        /// </summary>
        public GameObject physicalClosed;

        /// <summary>
        /// FIXME: Obsolete. Should be removed. Is this used in the prefab?
        /// A checkbox to toggle the <see cref="ChartManager.selectionMode"/>.
        /// </summary>
        [SerializeField] private Toggle selectionToggle;

        /// <summary>
        /// Activates the <see cref="selectionToggle"/>.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            selectionToggle.gameObject.SetActive(true);
        }

        /// <summary>
        /// Selects every linked <see cref="InteractableObject"/> of every
        /// <see cref="ChartMarker"/> within given bounds.
        /// </summary>
        /// <param name="min">The min value of the bounds.</param>
        /// <param name="max">The max value of the bounds.</param>
        public override void AreaSelection(Vector2 min, Vector2 max)
        {
            foreach (ChartMarker marker in activeMarkers)
            {
                Vector2 markerPos = marker.GetComponent<RectTransform>().anchoredPosition;
                if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                {
                    List<string> ids = marker.ids;
                    foreach (string id in ids)
                    {
                        InteractableObject o = InteractableObject.Get(id);
                        o.SetSelect(true, true);
                    }
                }
            }
        }
    }
}