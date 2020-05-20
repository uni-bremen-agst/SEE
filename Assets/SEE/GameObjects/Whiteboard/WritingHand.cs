/*
MIT License 
Copyright(c) 2017 MarekMarchlewicz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using UnityEngine;

namespace SEE.GO.Whiteboard
{
    [Obsolete("No longer required in the new SteamVR Input System. Remove.")]
    //[RequireComponent(typeof(SteamVR_TrackedController))]
    public class WritingHand : MonoBehaviour
    {
        [SerializeField]
        private float minDistanceToGrab = 0.2f;

        private DraggableObject[] draggables;

        private DraggableObject draggedObject = null;

        //private SteamVR_RenderModel renderModel;

        private void Awake()
        {
            //SteamVR_TrackedController controller = GetComponent<SteamVR_TrackedController>();

            //controller.TriggerClicked += OnTriggerCliked;
            //controller.TriggerUnclicked += OnTriggerUncliked;

            //renderModel = GetComponentInChildren<SteamVR_RenderModel>();

            draggables = FindObjectsOfType<DraggableObject>();
        }

        private void OnTriggerCliked(object sender) // RK FIXME, ClickedEventArgs e)
        {
            float closestDistance = float.MaxValue;
            DraggableObject closestObject = null;

            foreach (DraggableObject draggable in draggables)
            {
                if (!draggable.IsDragged)
                {
                    float distance = Vector3.Distance(draggable.transform.position, transform.position);

                    if (distance < minDistanceToGrab && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestObject = draggable;
                    }
                }
            }

            if (closestObject != null)
            {
                draggedObject = closestObject;

                draggedObject.StartDragging(transform);

                // RK FIXME
                //renderModel.gameObject.SetActive(false);
            }
        }

        //private void OnTriggerUncliked(object sender, ClickedEventArgs e)
        //{
        //    if(draggedObject != null)
        //    {
        //        draggedObject.StopDragging();

        //        draggedObject = null;

        //        renderModel.gameObject.SetActive(true);
        //    }
        //}
    }
}