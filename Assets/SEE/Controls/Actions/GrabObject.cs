using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a grabbed game object.
    /// </summary>
    public class GrabObject : InteractableObject
    {
        /// <summary>
        /// Sets up interactable and graphNode
        /// 
        /// The following assumptions are made:
        /// 1) gameObject has a component Interactable attached to it
        /// 2) gameObject has a NodeRef component attached to it
        /// 3) this NodeRef refers to a valid graph node with a valid information that can
        ///    be retrieved and shown when the user hovers over the object
        /// </summary>
        protected override void Start()
        {
            base.Start();
        }

        /// <summary>
        /// Action to be run when the grabbing object released this 
        /// grabbed object. 
        /// 
        /// The grabbed object is reset to its original position.
        /// </summary>
        public void OnReleased()
        {
            //Debug.LogFormat("OnReleased({0})\n", graphNode.ID);
            ResetToSavedPosition();
        }

        /// <summary>
        /// Action to be run when given <paramref name="grabber"/> grabs the gameObject.
        /// 
        /// Saves the position of gameObject and moves it toward <paramref name="grabber"/>
        /// using some animation.
        /// </summary>
        /// <param name="grabber">the object grabbing gameObject</param>
        public void OnGrabbed(GameObject grabber)
        {
            //Debug.LogFormat("OnGrabbed({0})\n", graphNode.ID);
            SaveCurrentPosition();
            iTween.MoveTo(gameObject, grabber.transform.position, 2.0f);
        }

        //----------------------------------------------------------------
        // Private actions called by the hand when the object is hovered.
        // These methods are called by SteamVR by way of the interactable.
        //----------------------------------------------------------------

        private Hand.AttachmentFlags attachmentFlags
       = Hand.defaultAttachmentFlags
         & (~Hand.AttachmentFlags.SnapOnAttach)
         & (~Hand.AttachmentFlags.DetachOthers)
         & (~Hand.AttachmentFlags.VelocityMovement);

        /// <summary>
        /// Called every Update() by a Hand while that Hand is hovering over this object.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void HandHoverUpdate(Hand hand)
        {
            //Debug.Log("HandHoverUpdate");
            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
                // The hand is grabbing the object
                OnGrabbed(hand.gameObject);

                // Call this to continue receiving HandHoverUpdate messages,
                // and prevent the hand from hovering over anything else
                hand.HoverLock(interactable);

                // Attach this object to the hand
                hand.AttachObject(gameObject, startingGrabType, attachmentFlags);

                //hand.HideGrabHint();
            }
            else if (isGrabEnding)
            {
                // The hand is no longer grabbing the object.

                // Detach this object from the hand
                hand.DetachObject(gameObject);

                // Call this to undo HoverLock
                hand.HoverUnlock(interactable);
                OnReleased();
            }
        }

        //-----------------------------------------------
        // Restoring the original position and rotation
        //-----------------------------------------------

        /// <summary>
        /// The position before the object was grabbed so that it can be restored
        /// when the object is no longer grabbed.
        /// </summary>
        private Vector3 oldPosition;
        /// <summary>
        /// The rotation before the object was grabbed so that it can be restored
        /// when the object is no longer grabbed.
        /// </summary>
        private Quaternion oldRotation;

        /// <summary>
        /// The local scale before the object was grabbed so that it can be restored
        /// when the object is no longer grabbed.
        /// </summary>
        private Vector3 oldLocalScale;

        /// <summary>
        /// How long the animation to restore the grabbed object to is original position
        /// and rotation should last in seconds.
        /// </summary>
        private const float ResetAnimationTime = 1.0f;

        /// <summary>
        /// Save our position/rotation/scale so that we can restore it when we detach.
        /// </summary>
        private void SaveCurrentPosition()
        {
            oldPosition = transform.position;
            oldRotation = transform.rotation;
            oldLocalScale = transform.localScale;
        }

        /// <summary>
        /// Restores the grabbed object to is original scale, position and rotation by animation.
        /// </summary>
        private void ResetToSavedPosition()
        {
            //HideInformation();
            gameObject.transform.rotation = oldRotation;
            iTween.ScaleTo(gameObject, oldLocalScale, ResetAnimationTime);
            iTween.MoveTo(gameObject, iTween.Hash(
                                          "position", oldPosition,
                                          //"islocal", true,
                                          //"rotation", oldRotation,
                                          "time", ResetAnimationTime
                ));
            
        }
    }
}