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

using System.Collections;
using UnityEngine;

namespace SEE.Game.Charts.VR
{
    /// <summary>
    /// Manages the position of the <see cref="GameObject" /> containing the charts in VR.
    /// </summary>
    public class ChartPositionVr : MonoBehaviour
    {
        /// <summary>
        /// Contains position data of the assigned camera. This information is needed
        /// so that the charts can be moved along with the camera.
        /// </summary>
        [SerializeField] public Transform CameraTransform;

        /// <summary>
        /// The minimum distance between the players head and the <see cref="GameObject" /> the charts are
        /// attached to to trigger it to follow the players head.
        /// </summary>
        private float _distanceThreshold;

        /// <summary>
        /// The <see cref="Coroutine" /> that moves the <see cref="GameObject" /> containing the charts.
        /// </summary>
        private Coroutine _movingChart;

        private const float ChartSpeed = 2f; //TODO: Move to manager

        /// <summary>
        /// Calls methods for initialization.
        /// </summary>
        private void Awake()
        {
            GetSettingData();
        }

        /// <summary>
        /// Links the <see cref="ChartManager" /> and gets its setting data.
        /// </summary>
        private void GetSettingData()
        {
            _distanceThreshold = ChartManager.Instance.DistanceThreshold;
        }

        /// <summary>
        /// Checks if the player moved more than <see cref="_distanceThreshold" />. If so, the charts will
        /// follow the player.
        /// </summary>
        private void Update()
        {
            if (Vector3.Distance(transform.position, CameraTransform.position) <=
                _distanceThreshold)
            {
                return;
            }

            if (_movingChart != null)
            {
                StopCoroutine(_movingChart);
                gameObject.SetActive(true);
            }

            _movingChart = StartCoroutine(MoveChart());
        }

        /// <summary>
        /// Moves the charts towards the players position.
        /// </summary>
        /// <returns></returns>
        private IEnumerator MoveChart()
        {
            Vector3 startPosition = transform.position;
            for (float time = 0f; time < 1f; time += Time.deltaTime * ChartSpeed)
            {
                transform.position = Vector3.Lerp(startPosition, CameraTransform.position, time);
                yield return new WaitForEndOfFrame();
            }

            gameObject.SetActive(true);
            _movingChart = null;
        }
    }
}