﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.UI.ModernUIPack
{
    public class WindowDragger : UIBehaviour, IBeginDragHandler, IDragHandler
    {
        [Header("RESOURCES")]
        public RectTransform dragArea;
        public RectTransform dragObject;

        [Header("SETTINGS")]
        public bool topOnClick = true;

        private Vector2 originalLocalPointerPosition;
        private Vector3 originalPanelLocalPosition;

        public new void Start()
        {
            if(dragArea == null)
            {
                try
                {
                    var canvas = (Canvas)GameObject.FindObjectsOfType(typeof(Canvas))[0];
                    dragArea = canvas.GetComponent<RectTransform>();
                }

                catch
                {
                    Debug.LogError("Movable Window - Drag Area has not been assigned.");
                }
            }
        }

        private RectTransform DragObjectInternal
        {
            get
            {
                if (dragObject == null)
                    return (transform as RectTransform);
                else
                    return dragObject;
            }
        }

        private RectTransform DragAreaInternal
        {
            get
            {
                if (dragArea == null)
                {
                    RectTransform canvas = transform as RectTransform;
                    while (canvas.parent != null && canvas.parent is RectTransform)
                    {
                        canvas = canvas.parent as RectTransform;
                    }
                    return canvas;
                }
                else
                    return dragArea;
            }
        }

        public void OnBeginDrag(PointerEventData data)
        {
            originalPanelLocalPosition = DragObjectInternal.localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(DragAreaInternal, data.position, data.pressEventCamera, out originalLocalPointerPosition);
            gameObject.transform.SetAsLastSibling();

            if (topOnClick == true)
                dragObject.transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData data)
        {
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(DragAreaInternal, data.position, data.pressEventCamera, out localPointerPosition))
            {
                Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
                DragObjectInternal.localPosition = originalPanelLocalPosition + offsetToOriginal;
            }

            ClampToArea();
        }

        private void ClampToArea()
        {
            Vector3 pos = DragObjectInternal.localPosition;

            Vector3 minPosition = DragAreaInternal.rect.min - DragObjectInternal.rect.min;
            Vector3 maxPosition = DragAreaInternal.rect.max - DragObjectInternal.rect.max;

            pos.x = Mathf.Clamp(DragObjectInternal.localPosition.x, minPosition.x, maxPosition.x);
            pos.y = Mathf.Clamp(DragObjectInternal.localPosition.y, minPosition.y, maxPosition.y);

            DragObjectInternal.localPosition = pos;
        }
    }
}