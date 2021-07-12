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
using Valve.VR;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// A basic input module for SteamVR.
    ///
    /// Based on: https://www.youtube.com/watch?v=3mRI1hu9Y3w
    /// </summary>
    public class VRInputModule : BaseInputModule
    {
        private SteamVR_Input_Sources _inputSource = SteamVR_Input_Sources.LeftHand;
        private SteamVR_Action_Boolean _clickAction = SteamVR_Actions._default.InteractUI;

        private GameObject _currentObject;
        private PointerEventData _data;

        public Camera PointerCamera { get; set; }

        protected override void Awake()
        {
            base.Awake();
            _data = new PointerEventData(eventSystem);
        }

        public override void Process()
        {
            _data.Reset();
            _data.position =
                new Vector3(PointerCamera.pixelWidth / 2, PointerCamera.pixelHeight / 2);

            eventSystem.RaycastAll(_data, m_RaycastResultCache);
            _data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            _currentObject = _data.pointerCurrentRaycast.gameObject;

            m_RaycastResultCache.Clear();

            HandlePointerExitAndEnter(_data, _currentObject);
            ExecuteEvents.Execute(_data.pointerDrag, _data, ExecuteEvents.dragHandler);

            if (_clickAction.GetStateDown(_inputSource))
            {
                ProcessPress(_data);
            }
            if (_clickAction.GetStateUp(_inputSource))
            {
                ProcessRelease(_data);
            }
        }

        public PointerEventData GetData()
        {
            return _data;
        }

        private void ProcessPress(PointerEventData data)
        {
            data.pointerPressRaycast = data.pointerCurrentRaycast;

            data.pressPosition = data.position;
            data.pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_currentObject);
            data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(_currentObject);
            data.rawPointerPress = _currentObject;

            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.beginDragHandler);
        }

        private void ProcessRelease(PointerEventData data)
        {
            ExecuteEvents.Execute(_currentObject, data,
                                  ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(data.pointerDrag, data,
                                  ExecuteEvents.endDragHandler);

            GameObject pointerUpHandler =
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(_currentObject);

            if (data.pointerPress == pointerUpHandler)
            {
                ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
            }

            eventSystem.SetSelectedGameObject(null);

            data.pressPosition = Vector2.zero;
            data.pointerPress = null;
            data.pointerDrag = null;
            data.rawPointerPress = null;
        }
    }
}
