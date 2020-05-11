using SEE.Controls.Devices;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Abstract super class of all selection actions. A selection action
    /// is one that is selected by the player.
    /// </summary>
    public abstract class SelectionAction : MonoBehaviour
    {
        /// <summary>
        /// The device yielding the selection information.
        /// </summary>
        protected Selection selectionDevice;
        /// <summary>
        /// The device yielding the selection information.
        /// </summary>
        public Selection SelectionDevice
        {
            get => selectionDevice;
            set => selectionDevice = value;
        }

        /// <summary>
        /// The camera from which to search for objects.
        /// </summary>
        private Camera mainCamera;
        /// <summary>
        /// The camera from which to search for objects.
        /// </summary>
        internal Camera MainCamera
        {
            get => mainCamera;
            set => mainCamera = value;
        }

        /// <summary>
        /// Event invoked when an object was grabbed or released. In the former case,
        /// the grabbed object is passed as a parameter; in the latter case, null
        /// is passed.
        /// </summary>
        public GameObjectEvent OnObjectGrabbed = new GameObjectEvent();

        /// <summary>
        /// The object currently grabbed. May be null.
        /// </summary>
        private GameObject grabbedObject;
        /// <summary>
        /// The object currently hovered over. May be null.
        /// </summary>
        private GameObject hoveredObject;


        /// <summary>
        /// This method is declared here because it will be overridden by 
        /// subclasses.
        /// </summary>
        protected virtual void Start()
        {
        }

        /// <summary>
        /// If the search was activated, a ray is cast from the position of
        /// the MainCamera or selectionDevice towards the selectionDevice's
        /// pointing direction. During this search, visual feedback may be
        /// given (depends upon the subclasses). If an object is hit and
        /// the user intends to grab it, it will be grabbed.
        /// </summary>
        private void Update()
        {
            GameObject hitObject = null;
            
            bool isGrabbing = selectionDevice.IsGrabbing;
            bool isSelecting = isGrabbing ? false : selectionDevice.IsSelecting;

            if (isGrabbing && grabbedObject != null)
            {
                // The user triggered grabbing while an object was already grabbed.
                // That means we need to release the grabbed object.
                // selectionDevice.IsGrabbing works as a toggle.
                ReleaseObject(grabbedObject);
                grabbedObject = null;
            }
            else if (isSelecting || (isGrabbing && grabbedObject == null))
            {
                // While the user wants to select or grab and has not yet grabbed anything
                // we will show the ray and try to hit an object.               
                hitObject = Select(out RaycastHit hitInfo);
                // give visual feedback on where we search
                ShowSearchFeedback(hitObject, hitInfo);
            }
            else
            {
                // not searching/grabbing => no visual feedback for search
                HideSearchFeedback();
            }

            if (hitObject != null)
            {
                // Something was hit
                if (isGrabbing && hitObject != grabbedObject)
                {
                    if (grabbedObject != null)
                    {
                        // first release grabbed object
                        ReleaseObject(grabbedObject);
                        //if (grabbedObject == hoveredObject)
                        //{
                        //    UnhoverObject(hoveredObject);
                        //    hoveredObject = null;
                        //}
                    }
                    grabbedObject = hitObject;
                    GrabObject(grabbedObject);
                } 
                else if (isSelecting && hitObject != hoveredObject)
                {
                    if (hoveredObject != null)
                    {
                        UnhoverObject(hoveredObject);
                    }
                    hoveredObject = hitObject;
                    HoverObject(hoveredObject);
                }
            }
            if (grabbedObject != null)
            {
                HoldObject(grabbedObject);
            }
        }

        /// <summary>
        /// Tries to select an object. If one was selected, it is returned,
        /// and <paramref name="hitInfo"/> contains additional information 
        /// about the hit.
        /// If none is selected, null is returned and <paramref name="hitInfo"/>
        /// is undefined.
        /// </summary>
        /// <param name="hitInfo">information about the hit if an object was hit,
        /// otherwise undefined</param>
        /// <returns>the selected object or null if none was selected</returns>
        private GameObject Select(out RaycastHit hitInfo)
        {
            if (Detect(out hitInfo))
            {
                return hitInfo.collider.gameObject;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Casts a ray to search for objects using the selectionDevice's direction.
        /// Returns true if an object was hit.
        /// </summary>
        /// <param name="hitInfo">additional information on the hit; defined only if this
        /// method returns true</param>
        /// <returns>true if an object was hit</returns>
        protected abstract bool Detect(out RaycastHit hitInfo);

        /// <summary>
        /// Gives visual feedback on the current search for an object.
        /// </summary>
        /// <param name="selectedObject">the object selected or null if none was selected</param>
        /// <param name="hitInfo">information about the hit (used only if <paramref name="selectedObject"/>
        /// is not null)</param>
        protected virtual void ShowSearchFeedback(GameObject selectedObject, RaycastHit hitInfo)
        {
        }

        /// <summary>
        /// Terminates the visual feedback on the current search for an object.
        /// </summary>
        protected virtual void HideSearchFeedback()
        {
        }

        /// <summary>
        /// Called when an object is being hovered over (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void HoverObject(GameObject selectedObject)
        {
            HoverableObject hoverComponent = selectedObject.GetComponent<HoverableObject>();
            if (hoverComponent != null)
            {
                hoverComponent.Hovered();
            }
        }

        /// <summary>
        /// Called when an object is no longer being hovered over (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void UnhoverObject(GameObject selectedObject)
        {
            HoverableObject hoverComponent = selectedObject.GetComponent<HoverableObject>();
            if (hoverComponent != null)
            {
                hoverComponent.Unhovered();
            }
        }

        /// <summary>
        /// Called when an object is grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void GrabObject(GameObject selectedObject)
        {
            GrabbableObject grabbingComponent = selectedObject.GetComponent<GrabbableObject>();
            if (grabbingComponent != null)
            {
                grabbingComponent.Grab(gameObject);
            }
            OnObjectGrabbed.Invoke(selectedObject);
        }

        public float Speed = 10.0f;

        /// <summary>
        /// Called while an object is being grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// This method is called on very Update().
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void HoldObject(GameObject selectedObject)
        {
            GrabbableObject grabbingComponent = selectedObject.GetComponent<GrabbableObject>();
            if (grabbingComponent != null)
            {
                float strength = selectionDevice.Pull;
                // A positive strength is interpreted as drawing the object toward the MainCamera.
                // A negative strength means that the object is moved farther away.
                float step = strength * Speed * Time.deltaTime;
                Vector3 targetPosition = Vector3.MoveTowards(selectedObject.transform.position, selectionDevice.Position, step);

                //Debug.LogFormat("Pulling grabbed object {0} at {1} towards {2} by strength {3} by step {4}\n",
                //                selectedObject.name, selectedObject.transform.position, targetPosition, strength, step);
                grabbingComponent.Continue(targetPosition);
            }
        }

        /// <summary>
        /// Called when an object is released, i.e., no longer grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void ReleaseObject(GameObject selectedObject)
        {
            GrabbableObject grabbingComponent = selectedObject.GetComponent<GrabbableObject>();
            if (grabbingComponent != null)
            {
                grabbingComponent.Release();
            }
            OnObjectGrabbed.Invoke(null);
        }
    }
}
