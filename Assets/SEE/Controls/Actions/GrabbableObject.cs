using SEE.Game;
using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a grabbed game object.
    /// </summary>
    public sealed class GrabbableObject : HoverableObject
    {
        [Tooltip("The color to be used when the object is grabbed by client.")]
        public Color LocalGrabbingColor = new Color(0.0f, 0.0f, 1.0f);

        [Tooltip("The color to be used when the object is grabbed by some other client.")]
        public Color RemoteGrabbingColor = new Color(0.8f, 0.2f, 1.0f);

        public bool IsGrabbed { get; private set; } = false;

        private MaterialChanger grabbingMaterialChanger;

        protected override void Awake()
        {
            base.Awake();
            grabbingMaterialChanger = new MaterialChanger(
                gameObject, 
                Materials.NewMaterial(Materials.ShaderType.Transparent, LocalGrabbingColor), 
                Materials.NewMaterial(Materials.ShaderType.Transparent, RemoteGrabbingColor)
            );
        }
        
        public void Grab(bool isOwner)
        {
            if (IsHovered)
            {
                HighlightMaterialChanger.ResetMaterial();
            }
            IsGrabbed = true;
            grabbingMaterialChanger.UseSpecialMaterial(isOwner);
        }
        
        public void Release(bool isOwner)
        {
            IsGrabbed = false;
            grabbingMaterialChanger.ResetMaterial();
            if (IsHovered)
            {
                HighlightMaterialChanger.UseSpecialMaterial(isOwner);
            }
        }

        //----------------------------------------------------------------
        // Private actions called by the hand when the object is hovered.
        // These methods are called by SteamVR by way of the interactable.
        //----------------------------------------------------------------

        private const Hand.AttachmentFlags AttachmentFlags
            = Hand.defaultAttachmentFlags
            & (~Hand.AttachmentFlags.SnapOnAttach)
            & (~Hand.AttachmentFlags.DetachOthers)
            & (~Hand.AttachmentFlags.VelocityMovement);

        /// <summary>
        /// Called by <see cref="Hand.Update"/>.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void HandHoverUpdate(Hand hand)
        {
            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
                Grab(true); // TODO(torben): net action
                hand.HoverLock(interactable);
                hand.AttachObject(gameObject, startingGrabType, AttachmentFlags);
            }
            else if (isGrabEnding)
            {
                gameObject.transform.rotation = Quaternion.identity;
                hand.DetachObject(gameObject);
                hand.HoverUnlock(interactable);
                Release(true); // TODO(torben): net action
            }
        }
    }
}