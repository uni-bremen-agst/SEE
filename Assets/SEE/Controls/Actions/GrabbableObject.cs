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
    public sealed class GrabbableObject : HoverableObject
    {
        [Tooltip("The color to be used when the object is grabbed by client.")]
        public Color LocalGrabbingColor = new Color(0.0f, 0.0f, 1.0f);

        [Tooltip("The color to be used when the object is grabbed by some other client.")]
        public Color RemoteGrabbingColor = new Color(0.8f, 0.2f, 1.0f);

        public bool IsGrabbed { get; private set; } = false;

        private static MaterialChanger grabbingMaterialChanger;

        protected override void Awake()
        {
            base.Awake();

            // TODO(torben): this creates two unique materials for every grabbableObject! This can be cached better! same for HighlightableObject
            grabbingMaterialChanger = new MaterialChanger(
                gameObject, 
                Materials.New(Materials.ShaderType.Transparent, LocalGrabbingColor), 
                Materials.New(Materials.ShaderType.Transparent, RemoteGrabbingColor)
            );
            Transform parent = transform; // TODO(torben): there needs to be a better way to find the parents! this is slow! same for HighlightableObject
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
            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
                switch (startingGrabType)
                {
                    case GrabTypes.None:
                        break;
                    case GrabTypes.Trigger:
                        break;
                    case GrabTypes.Pinch: // grab and move part of city
                        {
                            Grab(true); // TODO(torben): net action
                            hand.HoverLock(interactable);
                            hand.AttachObject(gameObject, startingGrabType, AttachmentFlags);
                        }
                        break;
                    case GrabTypes.Grip: // move city as a whole
                        {
                            Interactable rootInteractable = interactable;
                            while (true)
                            {
                                Transform parent = rootInteractable.transform.parent;
                                if (parent == null)
                                {
                                    break;
                                }

                                Interactable parentInteractable = parent.GetComponent<Interactable>();
                                if (parentInteractable == null)
                                {
                                    break;
                                }

                                rootInteractable = parentInteractable;
                            }
                            hand.HoverLock(rootInteractable);
                            hand.AttachObject(rootInteractable.gameObject, startingGrabType, AttachmentFlags);
                        }
                        break;
                    case GrabTypes.Scripted:
                        break;
                    default:
                        break;
                }
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