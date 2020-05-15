using SEE.Controls.Devices;
using SEE.GO;
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
            bool isGrabbing = selectionDevice.IsGrabbing;
            // The user could be both selecting and grabbing. However, grabbing overrules 
            // selecting.
            bool isSelecting = isGrabbing ? false : selectionDevice.IsSelecting;

            if (isGrabbing)
            {
                if (grabbedObject != null)
                {
                    // The user triggered grabbing while an object was already grabbed.
                    // That means we need to release the grabbed object.
                    // selectionDevice.IsGrabbing works as a toggle.
                    ReleaseObject(grabbedObject);
                    grabbedObject = null;
                }
                else if (hoveredObject != null)
                {                    
                    // The hovered object becomes the grabbed object. If no object is
                    // currently hovered over, nothing will be grabbed.
                    grabbedObject = hoveredObject;
                    GrabObject(grabbedObject);
                }
            }
            // Note: isGrabbing => !isSelecting
            // No selection while an object is already grabbed.
            if (isSelecting && grabbedObject == null)
            {
                // While the user wants to select we will show the ray and try to 
                // hit an object.               
                GameObject hitObject = Select(out RaycastHit hitInfo);
                // Give visual feedback on where we search.
                ShowSearchFeedback(hitObject, hitInfo);
                if (hitObject != hoveredObject)
                {
                    // Note: hitObject and hoveredObject may both be null here.
                    if (hoveredObject != null)
                    {
                        UnhoverObject(hoveredObject);
                    }
                    hoveredObject = hitObject;
                    if (hoveredObject != null)
                    {
                        HoverObject(hoveredObject);
                    }
                }
            }
            else
            {
                // not selecting (independent from isGrabbing) => no visual feedback for search
                HideSearchFeedback();
            }
            if (grabbedObject != null)
            {
                HoldObject(grabbedObject);
            }

            //if (isGrabbing && grabbedObject != null)
            //{

            //}
            //else if (isGrabbing && grabbedObject == null && hoveredObject != null)
            //{
            //    // While the user wants to select or grab and has not yet grabbed anything
            //    // we will show the ray and try to hit an object.               
            //    hitObject = Select(out RaycastHit hitInfo);
            //    // give visual feedback on where we search
            //    ShowSearchFeedback(hitObject, hitInfo);
            //}
            //else
            //{

            //}

            //if (hitObject != null)
            //{
            //    // Something was hit
            //    if (isGrabbing && hitObject != grabbedObject)
            //    {
            //        if (grabbedObject != null)
            //        {
            //            // first release grabbed object
            //            ReleaseObject(grabbedObject);
            //            //if (grabbedObject == hoveredObject)
            //            //{
            //            //    UnhoverObject(hoveredObject);
            //            //    hoveredObject = null;
            //            //}
            //        }
            //        grabbedObject = hitObject;
            //        GrabObject(grabbedObject);
            //    } 
            //    else if (isSelecting && hitObject != hoveredObject)
            //    {
            //        if (hoveredObject != null)
            //        {
            //            UnhoverObject(hoveredObject);
            //        }
            //        hoveredObject = hitObject;
            //        HoverObject(hoveredObject);
            //    }
            //}

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
            hoverComponent?.Hovered();
        }

        /// <summary>
        /// Called when an object is no longer being hovered over (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void UnhoverObject(GameObject selectedObject)
        {
            HoverableObject hoverComponent = selectedObject.GetComponent<HoverableObject>();
            hoverComponent?.Unhovered();
        }

        /// <summary>
        /// Called when an object is grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void GrabObject(GameObject selectedObject)
        {
            GrabbableObject grabbingComponent = selectedObject.GetComponent<GrabbableObject>();
            grabbingComponent?.Grab(gameObject);
            OnObjectGrabbed.Invoke(selectedObject);
        }

        /// <summary>
        /// Called while an object is being grabbed (passed as parameter <paramref name="grabbedObject"/>).
        /// This method is called on every Update().
        /// </summary>
        /// <param name="grabbedObject">the grabbed object</param>
        protected virtual void HoldObject(GameObject grabbedObject)
        {
            GrabbableObject grabbingComponent = grabbedObject.GetComponent<GrabbableObject>();
            if (grabbingComponent != null)
            {
                grabbingComponent.Continue(EndOfRay(grabbedObject));
            }
        }

        /// <summary>
        /// To draw a line from the player to the grabbed object when an object
        /// is grabbed.
        /// </summary>
        private class GrabLine
        {
            private readonly LineRenderer renderer;
            private readonly Vector3[] linePoints = new Vector3[2];

            public GrabLine(GameObject gameObject)
            {
                renderer = gameObject.AddComponent<LineRenderer>();
                LineFactory.SetDefaults(renderer);
                LineFactory.SetWidth(renderer, 0.01f);
                renderer.positionCount = linePoints.Length;
                renderer.useWorldSpace = true;
            }

            public void Draw(Vector3 from, Vector3 to)
            {
                renderer.enabled = true;
                linePoints[0] = from;
                linePoints[1] = to;
                renderer.SetPositions(linePoints);
            }

            public void Off()
            {
                linePoints[1] = linePoints[0];
                renderer.SetPositions(linePoints);
                renderer.enabled = false;
            }
        }

        /// <summary>
        /// The line drawn from the player to the grabbed object when an object
        /// is grabbed.
        /// </summary>
        private GrabLine grabLine;

        protected virtual Vector3 EndOfRay(GameObject heldObject)
        {
            if (grabLine == null)
            {
                grabLine = new GrabLine(gameObject);
            }
            // Draw a ray from the player to the held object.
            // The origin of the ray must be slightly off the camera. Otherwise it cannot be seen.
            grabLine.Draw(MainCamera.transform.position + Vector3.down * 0.1f, heldObject.transform.position);

            // Distance between player and held object.
            float rayLength = Vector3.Distance(MainCamera.transform.position, heldObject.transform.position);

            // Now move the held object following the pointing direction of the player.
            Vector3 direction = selectionDevice.Direction;
            Ray ray = MainCamera.ScreenPointToRay(direction);
            // A positive pull is interpreted as drawing the object towards the MainCamera.
            // A negative pull means that the object is moved farther away.
            float pull = selectionDevice.Pull; // pull direction
            float pullDirection = pull > 0.01f ? 1.0f : (pull < -0.01f ? -1.0f : 0.0f);

            float speed = 10.0f; // base speed; true speed is speed * pull
            float step = speed * Time.deltaTime * pullDirection;  // distance to move
            Vector3 target = ray.origin + ray.direction.normalized * rayLength; // head of the ray
            target = Vector3.MoveTowards(target, ray.origin, step);

            //if (pullDirection != 0)
            //{
            //    Debug.LogFormat("mouse direction {0} ray.origin {1} ray.direction {2} raylength {3} pull {4} step={5} from {6} to {7} by {8}\n",
            //                    direction, ray.origin, ray.direction, rayLength, pullDirection, step, heldObject.transform.position, target, Vector3.Distance(heldObject.transform.position, target));
            //}
            // Due to imprecisions of floating point arithmetics there may be tiny differences
            // between the positions which, however, accumulate to larger differences
            // because this method is called at the frame rate. That is why we will
            // return the original position of the held object if the difference is minor.
            if (Vector3.Distance(heldObject.transform.position, target) > 0.05f)
            {
                return target;
            }
            else
            {
                return heldObject.transform.position;
            }
        }

        /// <summary>
        /// Called when an object is released, i.e., no longer grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void ReleaseObject(GameObject selectedObject)
        {
            GrabbableObject grabbingComponent = selectedObject.GetComponent<GrabbableObject>();
            grabbingComponent?.Release();
            grabLine?.Off();
            OnObjectGrabbed.Invoke(null);
        }
    }
}
