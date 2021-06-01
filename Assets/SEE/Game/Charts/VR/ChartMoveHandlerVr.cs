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

using SEE.Controls.Actions;
using SEE.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts.VR
{
    /// <summary>
    /// Handles the dragging and minimization of charts in VR.
    /// </summary>
    public class ChartMoveHandlerVr : ChartMoveHandler
    {
        /// <summary>
        /// Contains settings used in this script.
        /// </summary>
        private ChartContentVr _chartContent;

        /// <summary>
        /// The transform of the ChartCanvasVRContainer (Contains world space <see cref="Canvas" /> and 3D
        /// objects for the canvas to sit on).
        /// </summary>
        private Transform _parent;

        /// <summary>
        /// Contains information about scrolling input.
        /// </summary>
        private ChartAction _chartAction;

        /// <summary>
        /// The minimum distance from the controller to the chart.
        /// </summary>
        private float _minimumDistance;

        /// <summary>
        /// The maximum distance from the controller to the chart.
        /// </summary>
        private float _maximumDistance;

        /// <summary>
        /// The speed at which charts will be moved in or out when the player scrolls.
        /// </summary>
        private float _chartScrollSpeed;

        /// <summary>
        /// The <see cref="Camera" /> attached to the pointer.
        /// </summary>
        private Camera _pointerCamera;

        /// <summary>
        /// 3D representation of the chart when not minimized.
        /// </summary>
        private GameObject _physicalOpen;

        /// <summary>
        /// 3D representation of the chart when minimized.
        /// </summary>
        private GameObject _physicalClosed;

        /// <summary>
        /// Contains position Data of the object this script is attached to
        /// </summary>
        private RectTransform _rectTransform;

        /// <summary>
        /// The offset of the <see cref="Canvas" /> to <see cref="_physicalOpen" /> so the two don't clip.
        /// </summary>
        private readonly Vector3 _chartOffset = new Vector3(0, 0, -0.03f);

        /// <summary>
        /// Initializes some attributes.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            Transform parent = transform.parent;
            // FIXME the line below was here before refactoring charts and needs to be reintroduced for VR
            //_parent = parent.GetComponent<ChartContent>().parent.transform;
            _pointerCamera = GameObject.FindGameObjectWithTag("Pointer").GetComponent<Camera>();
            _chartContent = parent.GetComponent<ChartContentVr>();
            _physicalOpen = _chartContent.physicalOpen;
            _physicalClosed = _chartContent.physicalClosed;
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Links the <see cref="ChartManager" /> and gets its setting data.
        /// </summary>
        protected override void GetSettingData()
        {
            base.GetSettingData();
            _chartScrollSpeed = ChartManager.Instance.ChartScrollSpeed;
            _minimumDistance = ChartManager.Instance.DistanceThreshold;
            _maximumDistance = ChartManager.Instance.PointerLength;
            _chartAction = GameObject.Find("VRPlayer").GetComponent<ChartAction>();
        }

        /// <summary>
        /// Turns the chart to always face the player.
        /// </summary>
        protected override void Update()
        {
            base.Update();
            Vector3 parentPosition = _parent.position;
            _parent.LookAt(parentPosition - (MainCamera.Camera.transform.position - parentPosition));
            ScrollInOut();
        }

        /// <summary>
        /// Checks if the player scrolled while moving a chart and if so, moves it towards or away from the
        /// player.
        /// </summary>
        private void ScrollInOut()
        {
            if (!PointerDown || _chartAction.move.Equals(0))
            {
                return;
            }

            Vector3 direction = _pointerCamera.transform.position - _rectTransform.position;
            float moveBy = _chartAction.move * _chartScrollSpeed * Time.deltaTime;
            if (!(_chartAction.move < 0 &&
                  direction.magnitude < _minimumDistance + moveBy ||
                  _chartAction.move > 0 &&
                  direction.magnitude > _maximumDistance - moveBy))
            {
                _parent.position -= direction * moveBy;
            }
        }

        /// <summary>
        /// Moves the chart to the new position.
        /// </summary>
        /// <param name="eventData">Contains position data of the pointer.</param>
        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.worldPosition != Vector3.zero)
            {
                _parent.position = eventData.pointerCurrentRaycast.worldPosition -
                                   (transform.position - (_parent.position + _chartOffset)) -
                                   _chartOffset;
            }
        }

        /// <summary>
        /// Toggles minimization of the chart.
        /// </summary>
        protected override void ToggleMinimize()
        {
            _physicalOpen.SetActive(Minimized);
            _physicalClosed.SetActive(!Minimized);
            base.ToggleMinimize();
        }
    }
}