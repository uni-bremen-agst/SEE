// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// A basic pointer for SteamVR.
    ///
    /// Based on: https://www.youtube.com/watch?v=3mRI1hu9Y3w
    /// </summary>
    public class Pointer : MonoBehaviour
    {
        /// <summary>
        /// The initial length of the laser pointer's beam.
        /// </summary>
        public float DefaultLength = 5.0f;
#if UNITY_ANDROID
#else
        /// <summary>
        /// The input for the pointer.
        /// </summary>
        public VRInputModule InputModule;
#endif
        /// <summary>
        /// The laser points consists of a line with a dot (sphere) at
        /// the end of the line. This game object represents the dot.
        /// </summary>
        private GameObject dot;
        /// <summary>
        /// The line of the laser pointer starting at the hand and ranging
        /// to the dot.
        /// </summary>
        private LineRenderer lineRenderer;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            dot = transform.Find("Dot").gameObject;
        }

        void Update()
        {
#if UNITY_ANDROID
#else
            PointerEventData data = InputModule.GetData();
            float targetLength = data.pointerCurrentRaycast.distance == 0 ? DefaultLength
                : data.pointerCurrentRaycast.distance;
            RaycastHit hit = CreateRaycast(targetLength);
            Vector3 endPosition = transform.position + transform.forward * targetLength;
            if (hit.collider != null)
            {
                endPosition = hit.point;
            }

            dot.transform.position = endPosition;

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPosition);
#endif
        }

        /// <summary>
        /// Returns the hit of a raycast starting of the game object
        /// in forward direction with given <paramref name="length"/>.
        /// </summary>
        /// <param name="length"></param>
        /// <returns>the hit of the raycast</returns>
        private RaycastHit CreateRaycast(float length)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            Physics.Raycast(ray, out RaycastHit hit, length);
            return hit;
        }
    }
}
