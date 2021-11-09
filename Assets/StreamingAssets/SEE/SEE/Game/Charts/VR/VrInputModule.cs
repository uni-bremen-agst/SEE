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
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts.VR
{
    /// <summary>
    /// Extends the standard <see cref="BaseInputModule" /> for usage in VR.
    /// </summary>
    public class VrInputModule : BaseInputModule
    {
        /// <summary>
        /// Manages the VR pointer used to interact with canvases.
        /// </summary>
        private VrPointer _pointer;

        /// <summary>
        /// Contains the users clicking information.
        /// </summary>
        private ChartAction _chartAction;

        public PointerEventData EventData { get; private set; }

        /// <summary>
        /// Calls methods for initialization and links the <see cref="VrPointer" /> script.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            GetSettingData();
            _pointer = GameObject.FindGameObjectWithTag("Pointer").GetComponent<VrPointer>();
        }

        /// <summary>
        /// Links the <see cref="ChartManager" /> and gets its setting data.
        /// </summary>
        private void GetSettingData()
        {
            _chartAction = GameObject.Find("VRPlayer").GetComponent<ChartAction>();
        }

        /// <summary>
        /// Initializes <see cref="EventData" />.
        /// </summary>
        protected override void Start()
        {
            EventData = new PointerEventData(eventSystem)
            {
                position = new Vector2(_pointer.Camera.pixelWidth / 2,
                    _pointer.Camera.pixelHeight / 2)
            };
        }

        /// <summary>
        /// Sends a raycast and triggers actions depending on the users input.
        /// </summary>
        public override void Process()
        {
            eventSystem.RaycastAll(EventData, m_RaycastResultCache);
            EventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            Ray ray = new Ray(_pointer.transform.position, _pointer.transform.forward);
            Physics.Raycast(ray, out RaycastHit hitData, ChartManager.Instance.PointerLength);
            float colliderDistance = hitData.distance.Equals(0f)
                ? ChartManager.Instance.PointerLength
                : hitData.distance;
            float canvasDistance = EventData.pointerCurrentRaycast.distance.Equals(0f)
                ? ChartManager.Instance.PointerLength
                : EventData.pointerCurrentRaycast.distance;

            if (colliderDistance.CompareTo(canvasDistance) < 0)
            {
                ExecutePhysical(hitData);
            }
            else
            {
                ExecuteCanvas();
            }
        }

        private void ExecutePhysical(RaycastHit hitData)
        {
            HandlePointerExitAndEnter(EventData, hitData.transform.gameObject);
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);
            if (_chartAction.clickDown)
            {
                Press(hitData.transform.gameObject);
            }

            if (_chartAction.clickUp)
            {
                Release(hitData.transform.gameObject);
            }
        }

        private void ExecuteCanvas()
        {
            HandlePointerExitAndEnter(EventData, EventData.pointerCurrentRaycast.gameObject);
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);
            if (_chartAction.clickDown)
            {
                Press(EventData.pointerCurrentRaycast.gameObject);
            }

            if (_chartAction.clickUp)
            {
                Release(EventData.pointerCurrentRaycast.gameObject);
            }
        }

        /// <summary>
        /// Finds handlers on the object the user pressed on, that should react to the press and triggers them.
        /// </summary>
        private void Press(GameObject hitObject)
        {
            EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;

            EventData.pointerPress =
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
            EventData.pointerDrag =
                ExecuteEvents.GetEventHandler<IDragHandler>(hitObject);

            ExecuteEvents.Execute(EventData.pointerPress, EventData,
                ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.beginDragHandler);
        }

        /// <summary>
        /// Finds handlers on the object the user released on, that should react to a release and triggers
        /// them.
        /// </summary>
        private void Release(GameObject hitObject)
        {
            GameObject pointerRelease = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);

            if (EventData.pointerPress == pointerRelease)
            {
                ExecuteEvents.Execute(EventData.pointerPress, EventData,
                    ExecuteEvents.pointerClickHandler);
            }

            ExecuteEvents.Execute(EventData.pointerPress, EventData,
                ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.endDragHandler);

            EventData.pointerPress = null;
            EventData.pointerDrag = null;
            EventData.pointerCurrentRaycast.Clear();
        }
    }
}
