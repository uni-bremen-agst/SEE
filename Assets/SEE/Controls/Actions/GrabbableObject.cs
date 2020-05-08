using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a grabbed game object.
    /// </summary>
    public class GrabbableObject : HoverableObject
    {
        [Tooltip("The color to be used when the object is grabbed.")]
        public Color GrabbingColor = Color.blue;

        /// <summary>
        /// True if the object is currently grabbed.
        /// </summary>
        private bool isGrabbed = false;

        /// <summary>
        /// For highlighting the gameObject while it is being hovered over.
        /// </summary>
        private MaterialChanger grabbingMaterial;

        protected override void Start()
        {
            base.Start();
            grabbingMaterial = new MaterialChanger(gameObject, Materials.NewMaterial(GrabbingColor));
        }

        /// <summary>
        /// Action to be run when given <paramref name="grabber"/> grabs the gameObject.
        /// 
        /// Saves the position of gameObject and moves it toward <paramref name="grabber"/>
        /// using some animation.
        /// </summary>
        /// <param name="grabber">the object grabbing gameObject</param>
        public void Grab(GameObject grabber)
        {
            if (isHovered)
            {
                base.Unhovered();
            }
            //Debug.LogFormat("OnGrabbed({0})\n", graphNode.ID);
            isGrabbed = true;
            grabbingMaterial.UseSpecialMaterial();
            SaveCurrentPosition();
        }

        /// <summary>
        /// Action to be run when the grabbing object released this 
        /// grabbed object. 
        /// 
        /// The grabbed object is reset to its original position.
        /// </summary>
        public void Release()
        {
            //Debug.LogFormat("OnReleased({0})\n", graphNode.ID);
            isGrabbed = false;
            ResetToSavedPosition();
            grabbingMaterial.ResetMaterial();
            if (isHovered)
            {
                base.Hovered();
            }
        }

        /// <summary>
        /// Action to be run while the object is being grabbed.
        /// 
        /// Moves the gameObject toward given <paramref name="position"/>.
        /// </summary>
        /// <param name="position">the position the gameObject should be moved to</param>
        public void Continue(Vector3 position)
        {
            if (isGrabbed)
            {
                //iTween.MoveTo(gameObject, position, 0.5f);
                gameObject.transform.position = position;
                Debug.LogFormat("{0} is being held.\n", gameObject.name);
            }
            else
            {
                Debug.LogErrorFormat("Continue called for object {0} not grabbed.\n", gameObject.name);
            }
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
                Grab(hand.gameObject);

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
                Unhovered();
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