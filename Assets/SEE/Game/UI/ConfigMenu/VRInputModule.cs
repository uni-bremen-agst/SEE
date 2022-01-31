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
#if !UNITY_ANDROID
using Valve.VR;
#endif

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// A basic input module for SteamVR.
    ///
    /// Based on: https://www.youtube.com/watch?v=3mRI1hu9Y3w
    /// </summary>
    public class VRInputModule : BaseInputModule
    {
#if !UNITY_ANDROID
        private readonly SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.LeftHand;
        private readonly SteamVR_Action_Boolean clickAction = SteamVR_Actions._default.InteractUI;
#endif


        private GameObject currentObject;
        private PointerEventData data;

        public Camera PointerCamera { get; set; }

        protected override void Awake()
        {
            base.Awake();
            data = new PointerEventData(eventSystem);
        }

        public override void Process()
        {
            data.Reset();
            data.position =
                new Vector3(PointerCamera.pixelWidth / 2, PointerCamera.pixelHeight / 2);

            eventSystem.RaycastAll(data, m_RaycastResultCache);
            data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            currentObject = data.pointerCurrentRaycast.gameObject;

            m_RaycastResultCache.Clear();

            HandlePointerExitAndEnter(data, currentObject);
            ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.dragHandler);
#if !UNITY_ANDROID
            if (clickAction.GetStateDown(inputSource))
            {
                ProcessPress(data);
            }
            if (clickAction.GetStateUp(inputSource))
            {
                ProcessRelease(data);
            }
#endif
        }
        public PointerEventData GetData() => data;
        private void ProcessPress(PointerEventData data)
        {
            data.pointerPressRaycast = data.pointerCurrentRaycast;

            data.pressPosition = data.position;
            data.pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentObject);
            data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentObject);
            data.rawPointerPress = currentObject;

            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.beginDragHandler);
        }

        private void ProcessRelease(PointerEventData data)
        {
            ExecuteEvents.Execute(currentObject, data,
                                  ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(data.pointerDrag, data,
                                  ExecuteEvents.endDragHandler);

            GameObject pointerUpHandler =
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentObject);

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