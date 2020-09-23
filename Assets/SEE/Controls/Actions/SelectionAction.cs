using SEE.Controls.Devices;
using SEE.Game;
using SEE.GO;
using System.ComponentModel;
using System.Net.NetworkInformation;
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
        public GameObject handledObject;

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

        private enum ObjectState
        {
            None,
            IsSelected,
            IsGrabbed,
            IsAnnotating
        }

        private ObjectState objectState = ObjectState.None;

        /// <summary>
        /// If the search was activated, a ray is cast from the position of
        /// the MainCamera or selectionDevice towards the selectionDevice's
        /// pointing direction. During this search, visual feedback may be
        /// given (depends upon the subclasses). If an object is hit and
        /// the user intends to grab it, it will be grabbed.
        /// </summary>
        private void Update()
        {
            if (Animation.IsOn())
            {
                return;
            }
            // The user could be doing everything together selecting, canceling,
            // grabbing, and zooming. 
            // We are using the following priorities:
            // Canceling is possible only when an object is selected or grabbed.
            // Zooming is possible only when an object is selected but not grabbed.
            // In those cases, canceling overrules all other actions.
            // Grabbing overrules selecting and selecting overrules zooming.
            bool isCanceling = selectionDevice.IsCanceling;
            bool isGrabbing = selectionDevice.IsGrabbing;
            bool isSelecting = selectionDevice.IsSelecting;
            bool startAnnotating = selectionDevice.IsAnnotating;


            if (isCanceling)
            {
                if (handledObject != null)
                {
                    if (objectState == ObjectState.IsGrabbed)
                    {
                        ReleaseObject(handledObject, false);
                    }
                    else if (objectState == ObjectState.IsSelected)
                    {
                        new Net.SelectionAction(handledObject.GetComponent<HoverableObject>(), null).Execute();
                        handledObject = null;
                        objectState = ObjectState.None;
                    }
                     else if (objectState == ObjectState.IsAnnotating)
                     {
                         objectState = ObjectState.IsSelected;
                         handledObject.GetComponent<AnnotatableObject>().CloseAnnotationEditor();
                     }
                }
            }
            else if (objectState == ObjectState.IsAnnotating)
            {
                if (handledObject.GetComponent<AnnotatableObject>().GetEditorState() == false)
                {
                    objectState = ObjectState.IsSelected;
                }
                else
                {
                    return;
                }
            }
            else if (startAnnotating)
            {
                handledObject.GetComponent<AnnotatableObject>().SetEditorState(true);
                handledObject.GetComponent<AnnotatableObject>().OpenAnnotationEditor();
                objectState = ObjectState.IsAnnotating;
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
                        // handledObject != null may be true because an object was selected
                        // but is not yet grabbed.
                        if (objectState == ObjectState.IsSelected)
                        {
                            if (handledObject == hitObject) // TODO: this if-statement can be removed if above TODO is fixed!
                            {
                                GrabObject(handledObject);
                            }
                        }
                        else
                        {
                            // The user continues grabbing while an object was already grabbed.
                            new Net.MoveAction(handledObject.GetComponent<GrabbableObject>(), TipOfGrabbingRay(handledObject)).Execute();
                        }
                    }
                }
                else if (hitObject != null && hitObject.GetComponent<HoverableObject>() != null && hitObject != handledObject)
                {
                    // assert: !isGrabbing && isSelecting && hitObject != null && hitObject != handledObject
                    // The user is selecting, not grabbing, and hit a new object.
                    HoverableObject newHoverableObject = hitObject.GetComponent<HoverableObject>();
                    if (newHoverableObject != null && !newHoverableObject.IsHovered)
                    {
                        if (selectionDevice is MouseSelection)
                        {
                            // TODO: This could possibly also be interesting for other devices
                            ((MouseSelection)selectionDevice).ResetSelectionTimer();
                        }
                        HoverableObject oldHoverableObject = handledObject ? handledObject.GetComponent<HoverableObject>() : null;
                        new Net.SelectionAction(oldHoverableObject, newHoverableObject).Execute();
                        objectState = ObjectState.IsSelected;
                        handledObject = hitObject;
                    }
                }
                else if ((hitObject == null || hitObject.GetComponent<HoverableObject>() == null) && handledObject != null && objectState == ObjectState.IsSelected)
                {
                    new Net.SelectionAction(handledObject.GetComponent<HoverableObject>(), null).Execute();
                    handledObject = null;
                    objectState = ObjectState.None;
                }
            }
            else if (objectState == ObjectState.IsGrabbed)
            {
                // assert: !isCanceling && !isGrabbing && !isSelecting && objectState == ObjectState.IsGrabbed
                // Grabbed object is released and the action was not canceled.
                ReleaseObject(handledObject, true);
            }

            if (objectState == ObjectState.IsSelected)
            {
                AllowZooming();
            }


            if (!isGrabbing && !isSelecting)
            {
                HideHoveringFeedback();
            }
        }

        private void AllowZooming()
        {
            // assert: !isCanceling && !isGrabbing && !isSelecting && objectState == ObjectState.IsSelected
            // Zooming uses animation. When the animation is complete, we will
            // notified via a call to OnZoomingComplete().
            if (selectionDevice.IsZoomingIn && Transformer.CanZoomInto(handledObject))
            {
                new Net.ZoomIntoAction(handledObject.GetComponent<HoverableObject>()).Execute();
            }
            else if (selectionDevice.IsZoomingOut && Transformer.CanZoomOutOf(handledObject))
            {
                new Net.ZoomOutOfAction(handledObject.GetComponent<HoverableObject>()).Execute();
            }
            else if (selectionDevice.IsZoomingHome && Transformer.CanZoomRoot(handledObject))
            {
                new Net.ZoomRootAction(handledObject.GetComponent<HoverableObject>()).Execute();
            }
        }

        /// <summary>
        /// This method will be called by Transformer when the animation of the zooming
        /// is completed.
        /// </summary>
        private void OnZoomingComplete()
        {
            Animation.End();
        }

        /// <summary>
        /// This method will be called by Transformer when the animation of the zooming
        /// is completed.
        /// </summary>
        private void OnZoomingOutComplete(Transformer transformer)
        {
            Animation.End();
            transformer.FinalizeZoomOut();
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
        /// Gives visual feedback when an object was grabbed.
        /// </summary>
        /// <param name="heldObject">the object selected or null if none was selected</param>
        public virtual void ShowGrabbingFeedback(GameObject heldObject)
        {
        }

        /// <summary>
        /// Terminates the visual feedback on the currently ongoing grabbing.
        /// </summary>
        protected virtual void HideGrabbingFeedback()
        {
        }

        //-----------------------------------------------------------------------
        // Grabbing actions
        //-----------------------------------------------------------------------

        /// <summary>
        /// Called when an object is grabbed (passed as parameter <paramref name="grabbedObject"/>).
        /// </summary>
        /// <param name="grabbedObject">the grabbed object</param>
        protected virtual void GrabObject(GameObject grabbedObject)
        {
            handledObjectMemento = new ObjectMemento(grabbedObject);
            handledObject = grabbedObject;
            objectState = ObjectState.IsGrabbed;
            OnObjectGrabbed.Invoke(grabbedObject);
            new Net.GrabAction(grabbedObject.GetComponent<GrabbableObject>(), handledObjectMemento.LocalPosition, true).Execute();
        }

        /// <summary>
        /// Called when an object is released, i.e., no longer grabbed (passed as parameter <paramref name="grabbedObject"/>).
        /// </summary>
        /// <param name="grabbedObject">the grabbed object</param>
        /// <param name="actionFinalized">if true, the object should be released at its final destination; otherwise
        /// the movement should be canceled and its original position be restored</param>
        protected virtual void ReleaseObject(GameObject grabbedObject, bool actionFinalized)
        {
            objectState = ObjectState.IsSelected;
            OnObjectGrabbed.Invoke(null);
            new Net.GrabAction(grabbedObject.GetComponent<GrabbableObject>(), handledObjectMemento.LocalPosition, false, actionFinalized).Execute();
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
        /// Called by iTween when grabbing was cancelled and the grabbed object
        /// has reached its original position (the one where it was located before
        /// being grabbed). After that, the animation is over and animationIsRunning
        /// can be reset to false.
        /// 
        /// Note: Its call is requested in ReleaseObject(). Watch out when you rename 
        /// this method. Then you need to adjust the parameter 
        /// </summary>
        protected void ResetCompleted()
        {
            Animation.End();
        }

        /// <summary>
        /// Animation mode with a time out.
        /// </summary>
        public static class Animation
        {
            /// <summary>
            /// Whether any animation is currently running.
            /// </summary>
            private static bool animationIsRunning = false;

            /// <summary>
            /// Remaining waiting time for the time out in seconds.
            /// </summary>
            private static float animationTimeOut = 0.0f;

            /// <summary>
            /// Indicates the start of a new animation.
            /// </summary>
            public static void Start()
            {
                animationIsRunning = true;
                animationTimeOut = 3.0f;
            }

            /// <summary>
            /// Indicates the end of a running animation.
            /// </summary>
            public static void End()
            {
                animationIsRunning = false;
            }

            /// <summary>
            /// Returns if there is no animation is currently running or if the 
            /// waiting time for an animation to be finished is up. Must be called
            /// exactly once per frame.
            /// </summary>
            /// <returns>true if animation is finished or time is up</returns>
            public static bool IsOn()
            {
                animationTimeOut -= Time.deltaTime;
                if (animationIsRunning && animationTimeOut <= 0)
                {
                    Debug.LogWarning("Animation time out.\n");
                    animationIsRunning = false;
                }
                return animationIsRunning;
            }
        }
    }
}
