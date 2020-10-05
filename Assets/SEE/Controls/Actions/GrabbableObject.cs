using SEE.Game;
using SEE.GO;
using SEE.Utils;
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
        public bool IsGrabbed { get; private set; } = false;

        private XRNavigationAction navAction;
        private MaterialChanger grabbingMaterialChanger;
        private Transform originalParent;

        protected override void Awake()
        {
            base.Awake();

            // TODO(torben): i don't like this!
            navAction = SEECity.GetByGraph(GetComponent<NodeRef>().node.ItsGraph).GetComponent<XRNavigationAction>();
            // TODO(torben): this creates two unique materials for every grabbableObject! This can be cached better! same for HighlightableObject

            Color color = GetComponent<MeshRenderer>().sharedMaterial.color;
            Color.RGBToHSV(color, out float h, out float s, out float v);

            Color localColor = Color.HSVToRGB((h - 0.1f) % 1.0f, s, v);
            Color remoteColor = Color.HSVToRGB((h + 0.1f) % 1.0f, s, v);

            grabbingMaterialChanger = new MaterialChanger(
                gameObject,
                Materials.New(Materials.ShaderType.Transparent, localColor),
                Materials.New(Materials.ShaderType.Transparent, remoteColor)
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
            grabbingMaterialChanger.UseSpecialMaterial(isOwner);

            if (!isOwner)
            {
                originalParent = transform.parent;
                transform.parent = null;
            }

            IsGrabbed = true;
        }
        
        public void Release(bool isOwner)
        {
            IsGrabbed = false;

            if (!isOwner)
            {
                gameObject.transform.rotation = Quaternion.identity;
                transform.parent = originalParent;
                originalParent = null;
            }

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
                    new Net.GrabBuildingAction(this).Execute();
                    interactable.gameObject.AddComponent<Net.Synchronizer>();

                    Grab(true);
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
                    Release(true);

                    Destroy(interactable.gameObject.GetComponent<Net.Synchronizer>());
                    new Net.ReleaseBuildingAction(this).Execute();
                }
            }
        }
    }
}