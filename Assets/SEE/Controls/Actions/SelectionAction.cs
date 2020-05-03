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
        /// The object currently grabbed. May be null.
        /// </summary>
        private GameObject grabbedObject;
        /// <summary>
        /// The object currently hovered over. May be null.
        /// </summary>
        private GameObject hoveredObject;

        /// <summary>
        /// If the search was activated, a ray is cast from the position of
        /// the MainCamera or selectionDevice towards the selectionDevice's
        /// pointing direction. During this search, visual feedback may be
        /// given (depends upon the subclasses). If an object is hit and
        /// the user intends to grab it, it will be grabbed.
        /// </summary>
        private void Update()
        {
            if (grabbedObject == null)
            {
                // nothing grabbed; we allow searching
                if (selectionDevice.Activated)
                {
                    // we are searching for objects
                    GameObject hitObject = Select(out RaycastHit hitInfo);
                    // give visual feedback on where we search
                    ShowSearchFeedback(hitObject, hitInfo);
                    // Have we hit anything?
                    if (hitObject == null)
                    {
                        // nothing hit; in case something was previously hovered over,
                        // it must be reset
                        if (hoveredObject != null)
                        {
                            UnhoverObject(hoveredObject);
                            hoveredObject = null;
                        }
                    } 
                    else
                    {
                        // something was hit
                        // in case something was previously hovered over, it must be reset
                        if (hoveredObject != null)
                        {
                            UnhoverObject(hoveredObject);
                        }
                        // the hit object is the new object that is currently being hovered over
                        hoveredObject = hitObject;
                        HoverObject(hoveredObject);
                        if (selectionDevice.IsGrabbing)
                        {
                            // If the user wants us to grab the hovered object, we grab it.
                            // Note: hovering is possible only while we have not already grabbed
                            // an object, hence, at this point we do not release any grabbed object
                            grabbedObject = hoveredObject;
                            GrabObject(grabbedObject);
                        }
                    }
                }
                else
                {
                    // not searching => no visual feedback for search
                    HideSearchFeedback();
                }
            }
            else
            {
                // An object is already grabbed; we do not allow searching. Similarly,
                // an object can only be grabbed if it is being hovered over.
                if (selectionDevice.IsReleasing)
                {
                    ReleaseObject(grabbedObject);
                    grabbedObject = null;
                }
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
            GrabObject grabbingComponent = selectedObject.GetComponent<GrabObject>();
            if (grabbingComponent != null)
            {
                grabbingComponent.OnHoverBegin();
            }
        }

        /// <summary>
        /// Called when an object is no longer being hovered over (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void UnhoverObject(GameObject selectedObject)
        {
            GrabObject grabbingComponent = selectedObject.GetComponent<GrabObject>();
            if (grabbingComponent != null)
            {
                grabbingComponent.OnHoverEnd();
            }
        }

        /// <summary>
        /// Called when an object is grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void GrabObject(GameObject selectedObject)
        {
            GrabObject grabbingComponent = selectedObject.GetComponent<GrabObject>();
            if (grabbingComponent != null)
            {
                grabbingComponent.OnGrabbed(gameObject);
            }
        }

        /// <summary>
        /// Called when an object is released, i.e., no longer grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void ReleaseObject(GameObject selectedObject)
        {
            GrabObject grabbingComponent = selectedObject.GetComponent<GrabObject>();
            if (grabbingComponent != null)
            {
                grabbingComponent.OnReleased();
            }
        }
    }
}
