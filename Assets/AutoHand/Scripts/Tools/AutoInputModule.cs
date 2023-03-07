using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

namespace Autohand
{
    public class AutoInputModule : BaseInputModule
    {
        private List<HandCanvasPointer> pointers = new List<HandCanvasPointer>();
        private PointerEventData[] eventDatas;

        AutoInputModule _instance;
        private bool _isDestroyed = false;

        public AutoInputModule Instance
        {
            get
            {
                if (_isDestroyed)
                    return null;

                if (_instance == null)
                {
                    if (!(_instance = FindObjectOfType<AutoInputModule>()))
                    {
                        _instance = new GameObject().AddComponent<AutoInputModule>();
                        _instance.transform.parent = AutoHandExtensions.transformParent;
                    }



                    EventSystem[] system = null;
                    BaseInputModule[] inputModule;

                    inputModule = FindObjectsOfType<BaseInputModule>();
                    if (inputModule.Length > 1)
                    {
                        for (int i = inputModule.Length - 1; i >= 0; i--)
                        {
                            if (!inputModule[i].gameObject.GetComponent<AutoInputModule>())
                                Destroy(inputModule[i]);
                            Debug.LogWarning("AUTO HAND:  REMOVING ADDITIONAL EVENT SYSTEMS FROM THE SCENE");
                        }
                    }

                    system = FindObjectsOfType<EventSystem>();
                    if (system.Length > 1)
                    {
                        for (int i = system.Length - 1; i >= 0; i--)
                        {
                            if (!system[i].gameObject.GetComponent<AutoInputModule>())
                                Destroy(system[i]);
                            Debug.LogWarning("AUTO HAND:  REMOVING ADDITIONAL EVENT SYSTEMS FROM THE SCENE");
                        }
                    }

                }

                return _instance;
            }
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            _isDestroyed = true;
        }

        public int AddPointer(HandCanvasPointer pointer)
        {
            if (!pointers.Contains(pointer))
            {
                pointers.Add(pointer);
                eventDatas = new PointerEventData[pointers.Count];

                for (int i = 0; i < eventDatas.Length; i++)
                {
                    eventDatas[i] = new PointerEventData(eventSystem);
                    eventDatas[i].delta = Vector2.zero;
                    eventDatas[i].position = new Vector2(Screen.width / 2, Screen.height / 2);
                }
            }

            return pointers.IndexOf(pointer);
        }

        public void RemovePointer(HandCanvasPointer pointer)
        {
            if (pointers.Contains(pointer))
                pointers.Remove(pointer);
            foreach (var point in pointers)
            {
                point.SetIndex(pointers.IndexOf(point));
            }
            eventDatas = new PointerEventData[pointers.Count];
            for (int i = 0; i < eventDatas.Length; i++)
            {
                eventDatas[i] = new PointerEventData(eventSystem);
                eventDatas[i].delta = Vector2.zero;
                eventDatas[i].position = new Vector2(Screen.width / 2, Screen.height / 2);
            }
        }

        public override void Process()
        {
#pragma warning disable
            for (int index = 0; index < pointers.Count; index++)
            {
                try
                {
                    if (pointers[index] != null && pointers[index].enabled)
                    {
                        pointers[index].Preprocess();
                        // Hooks in to Unity's event system to handle hovering
                        eventSystem.RaycastAll(eventDatas[index], m_RaycastResultCache);
                        eventDatas[index].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

                        HandlePointerExitAndEnter(eventDatas[index], eventDatas[index].pointerCurrentRaycast.gameObject);

                        ExecuteEvents.Execute(eventDatas[index].pointerDrag, eventDatas[index], ExecuteEvents.dragHandler);
                    }

                }
                catch { }
            }
#pragma warning restore
        }

        public void ProcessPress(int index)
        {
            pointers[index].Preprocess();
            // Hooks in to Unity's event system to process a release
            eventDatas[index].pointerPressRaycast = eventDatas[index].pointerCurrentRaycast;

            eventDatas[index].pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventDatas[index].pointerPressRaycast.gameObject);
            eventDatas[index].pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(eventDatas[index].pointerPressRaycast.gameObject);

            ExecuteEvents.Execute(eventDatas[index].pointerPress, eventDatas[index], ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(eventDatas[index].pointerDrag, eventDatas[index], ExecuteEvents.beginDragHandler);
        }

        public void ProcessRelease(int index)
        {
            pointers[index].Preprocess();
            // Hooks in to Unity's event system to process a press
            GameObject pointerRelease = ExecuteEvents.GetEventHandler<IPointerClickHandler>(eventDatas[index].pointerCurrentRaycast.gameObject);

            if (eventDatas[index].pointerPress == pointerRelease)
                ExecuteEvents.Execute(eventDatas[index].pointerPress, eventDatas[index], ExecuteEvents.pointerClickHandler);

            ExecuteEvents.Execute(eventDatas[index].pointerPress, eventDatas[index], ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(eventDatas[index].pointerDrag, eventDatas[index], ExecuteEvents.endDragHandler);

            eventDatas[index].pointerPress = null;
            eventDatas[index].pointerDrag = null;

            eventDatas[index].pointerCurrentRaycast.Clear();
        }

        public PointerEventData GetData(int index) { return eventDatas[index]; }
    }
}