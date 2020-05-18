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
        /// The object currently grabbed or hovered over. May be null.
        /// </summary>
        private GameObject handledObject;

        /// <summary>
        /// The memento of the state of handledObject just before it was grabbed.
        /// Invariant: handledObject != null => handledObjectMemento != null.
        /// </summary>
        private ObjectMemento handledObjectMemento;

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
            // The user could be doing everything together selecting, canceling, and grabbing. 
            // We are using the following priorities:
            // Canceling is possible only when an object is selected or grabbed.
            // In those cases, canceling overrules selecting and grabbing.
            // Grabbing overrules canceling.
            bool isCanceling = selectionDevice.IsCanceling;
            bool isGrabbing = selectionDevice.IsGrabbing;
            bool isSelecting = selectionDevice.IsSelecting;

            if (isCanceling)
            {
                if (handledObject != null)
                {
                    if (isGrabbing)
                    {
                        ReleaseObject(handledObject);
                        handledObjectMemento.Reset();
                    }
                    else if (isSelecting)
                    {
                        HideHoveringFeedback();
                    }                    
                    handledObject = null;
                }
            }
            else if (isGrabbing || isSelecting)
            {
                GameObject hitObject = Select(out RaycastHit hitInfo);
                // Give visual feedback on where we search.
                ShowHoveringFeedback(hitObject, hitInfo);
                if (isGrabbing)
                {
                    if (handledObject != null)
                    {
                        // The user continues grabbing while an object was already grabbed.
                        HoldObject(handledObject);
                    }
                    else if (hitObject != null)
                    {
                        // The user is currently not holding an object and hit a new object.
                        handledObject = hitObject;
                        GrabObject(handledObject);
                        handledObjectMemento = new ObjectMemento(handledObject);
                    }
                }
                else if (hitObject != null && hitObject != handledObject)
                {
                    // The user is selecting, not grabbing, and hit a new object.
                    if (handledObject != null)
                    {
                        UnhoverObject(handledObject);
                    }
                    handledObject = hitObject;
                    HoverObject(handledObject);
                }
            }

            //else if (handledObject != null)
            //{
            //    ReleaseObject(handledObject);
            //    handledObject = null;
            //}
            //// Note: isGrabbing => !isSelecting
            //// No selection while an object is already grabbed.
            //if (isSelecting && handledObject == null)
            //{
            //    // While the user wants to select we will show the ray and try to 
            //    // hit an object.               
            //    GameObject hitObject = Select(out RaycastHit hitInfo);
            //    // Give visual feedback on where we search.
            //    ShowHoveringFeedback(hitObject, hitInfo);
            //    if (hitObject != hoveredObject)
            //    {
            //        // Note: hitObject and hoveredObject may both be null here.
            //        if (hoveredObject != null)
            //        {
            //            UnhoverObject(hoveredObject);
            //        }
            //        hoveredObject = hitObject;
            //        if (hoveredObject != null)
            //        {
            //            HoverObject(hoveredObject);
            //        }
            //    }
            //}
            //else
            //{
            //    // not selecting (independent from isGrabbing) => no visual feedback for search
            //    HideHoveringFeedback();
            //}
            //if (handledObject != null)
            //{
            //    HoldObject(handledObject);
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

        //-----------------------------------------------------------------------
        // Visual hovering feedback
        //-----------------------------------------------------------------------

        /// <summary>
        /// Gives visual feedback on the current search for an object.
        /// </summary>
        /// <param name="selectedObject">the object selected or null if none was selected</param>
        /// <param name="hitInfo">information about the hit (used only if <paramref name="selectedObject"/>
        /// is not null)</param>
        protected virtual void ShowHoveringFeedback(GameObject selectedObject, RaycastHit hitInfo)
        {
        }

        /// <summary>
        /// Terminates the visual feedback on the current search for an object.
        /// </summary>
        protected virtual void HideHoveringFeedback()
        {
        }

        //-----------------------------------------------------------------------
        // Visual grabbing feedback
        //-----------------------------------------------------------------------

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

        /// <summary>
        /// Gives visual feedback when an object was grabbed.
        /// </summary>
        /// <param name="heldObject">the object selected or null if none was selected</param>
        protected void ShowGrabbingFeedback(GameObject heldObject)
        {
            if (grabLine == null)
            {
                grabLine = new GrabLine(gameObject);
            }
            // Draw a ray from the player to the held object.
            grabLine.Draw(GrabbingRayStart(), heldObject.transform.position);
        }

        protected virtual Vector3 GrabbingRayStart()
        {
            // The origin of the ray must be slightly off the camera. Otherwise it cannot be seen.
            return MainCamera.transform.position + Vector3.down * 0.1f;
        }

        /// <summary>
        /// Terminates the visual feedback on the currently ongoing grabbing.
        /// </summary>
        protected void HideGrabbingFeedback()
        {
            grabLine?.Off();
        }

        //-----------------------------------------------------------------------
        // Hovering actions
        //-----------------------------------------------------------------------

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

        //-----------------------------------------------------------------------
        // Grabbing actions
        //-----------------------------------------------------------------------

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
                ShowGrabbingFeedback(grabbedObject);
                grabbingComponent.Continue(TipOfGrabbingRay(grabbedObject));
            }
        }

        /// <summary>
        ///  Pull speed of the grabbing when a remote object was grabbed.
        /// </summary>
        [Tooltip("Pull speed of the grabbing when a remote object was grabbed."), Range(0.1f, 100.0f)]
        public float PullSpeed = 10.0f;

        [Tooltip("A grabbed object must be moved at least that far to become actually moved."), Range(0.01f, 0.3f)]
        public float MoveSensitivity = 0.01f;

        /// <summary>
        /// Yields the tip of the grabbing ray. The grabbing ray initially starts at the origin of the 
        /// selection device and ends at the grabbed object. It follows the pointing direction of
        /// the selection device. In addition, the user can pull the grabbed object. Depending on
        /// the direction of the pulling, the grabbed object can be drawn towards the player 
        /// or pushed farther away along the direction of the grabbing ray. The position of the tip
        /// of the ray (the position where the grabbed object should be moved according to the
        /// user) is returned.
        /// 
        /// </summary>
        /// <param name="heldObject">the object currently held</param>
        /// <returns>new position where the held object should be moved</returns>
        protected virtual Vector3 TipOfGrabbingRay(GameObject heldObject)
        {
            Ray ray = GetRay();
            // Distance between player and held object.
            float rayLength = Vector3.Distance(ray.origin, heldObject.transform.position);

            // Now move the held object following the pointing direction of the player.

            // A positive pull is interpreted as drawing the object towards the player.
            // A negative pull means that the object is moved farther away.
            float pull = selectionDevice.Pull; // pull direction
            float pullDirection = pull > 0.01f ? 1.0f : (pull < -0.01f ? -1.0f : 0.0f);
            //Debug.LogFormat("pull {0} pullDirection {1}\n", pull, pullDirection);

            float step = PullSpeed * Time.deltaTime * pullDirection;  // distance to move
            Vector3 target = ray.origin + ray.direction.normalized * rayLength; // head of the ray
            target = Vector3.MoveTowards(target, ray.origin, step);

            // Due to imprecisions of floating point arithmetics there may be tiny differences
            // between the positions which, however, accumulate to larger differences
            // because this method is called at the frame rate. That is why we will
            // return the original position of the held object if the difference is minor.
            if (Vector3.Distance(heldObject.transform.position, target) >= MoveSensitivity)
            {
                return target;
            }
            else
            {
                return heldObject.transform.position;
            }
        }

        /// <summary>
        /// Returns a ray cast from the player along the pointing direction of the selection device.
        /// </summary>
        /// <returns></returns>
        protected abstract Ray GetRay();

        /// <summary>
        /// Called when an object is released, i.e., no longer grabbed (passed as parameter <paramref name="selectedObject"/>).
        /// </summary>
        /// <param name="selectedObject">the selected object</param>
        protected virtual void ReleaseObject(GameObject selectedObject)
        {
            GrabbableObject grabbingComponent = selectedObject.GetComponent<GrabbableObject>();
            grabbingComponent?.Release();
            HideGrabbingFeedback();
            OnObjectGrabbed.Invoke(null);
        }
    }
}
