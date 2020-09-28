using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a grabbed game object.
    /// </summary>
    [RequireComponent(typeof(NodeRef))]
    public sealed class GrabbableObject : HoverableObject
    {
        [Tooltip("The color to be used when the object is grabbed by client.")]
        public Color LocalGrabbingColor = new Color(0.0f, 0.0f, 1.0f);

        [Tooltip("The color to be used when the object is grabbed by some other client.")]
        public Color RemoteGrabbingColor = new Color(0.8f, 0.2f, 1.0f);

        public bool IsGrabbed { get; private set; } = false;

        private XRNavigationAction navAction;
        private MaterialChanger grabbingMaterialChanger;

        protected override void Awake()
        {
            base.Awake();

            // TODO(torben): i don't like this!
            navAction = SEECity.GetByGraph(GetComponent<NodeRef>().node.ItsGraph).GetComponent<XRNavigationAction>();
            // TODO(torben): this creates two unique materials for every grabbableObject! This can be cached better! same for HighlightableObject
            grabbingMaterialChanger = new MaterialChanger(
                gameObject, 
                Materials.New(Materials.ShaderType.Transparent, LocalGrabbingColor), 
                Materials.New(Materials.ShaderType.Transparent, RemoteGrabbingColor)
            );
            // TODO(torben): there needs to be a better way to find the parents! this is slow! same for HighlightableObject
            Transform parent = transform;
            while (parent.parent != null)
            {
                parent = parent.parent;
            }
            Portal.GetDimensions(parent.gameObject, out Vector2 min, out Vector2 max);
            Portal.SetPortal(min, max, grabbingMaterialChanger.LocalSpecialMaterial);
            Portal.SetPortal(min, max, grabbingMaterialChanger.RemoteSpecialMaterial);
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
            bool isGrabEnding = hand.IsGrabEnding(this.gameObject);
            GrabTypes grabType = isGrabEnding ? hand.GetGrabEnding() : hand.GetGrabStarting();

            if (!isGrabEnding)
            {
                if (grabType == GrabTypes.Pinch && interactable.attachedToHand == null)
                {
                    Grab(true); // TODO(torben): net action
                    hand.HoverLock(interactable);
                    hand.AttachObject(gameObject, grabType, AttachmentFlags);
                }
                else if (grabType == GrabTypes.Grip)
                {
                    navAction.OnStartGrab(hand);
                }
            }
            else // isGrabEnding == true
            {
                if (grabType == GrabTypes.Pinch)
                {
                    gameObject.transform.rotation = Quaternion.identity;
                    hand.DetachObject(gameObject);
                    hand.HoverUnlock(interactable);
                    Release(true); // TODO(torben): net action
                }
            }
        }
    }
}