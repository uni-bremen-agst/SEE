using SEE.GO;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Super class of the behaviours of game objects the player interacts with.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(NodeRef))]
    public sealed class InteractableObject : MonoBehaviour
    {
        // Tutorial on grabbing objects:
        // https://www.youtube.com/watch?v=MKOc8J877tI&t=15s

        // These are the messages the hand sends to objects that it is interacting with:
        //
        // OnHandHoverBegin:       Sent when the hand first starts hovering over the object
        // HandHoverUpdate:        Sent every frame that the hand is hovering over the object
        // OnHandHoverEnd:         Sent when the hand stops hovering over the object
        // OnAttachedToHand:       Sent when the object gets attached to the hand
        // HandAttachedUpdate:     Sent every frame while the object is attached to the hand
        // OnDetachedFromHand:     Sent when the object gets detached from the hand
        // OnHandFocusLost:        Sent when an attached object loses focus because something else has been attached to the hand
        // OnHandFocusAcquired:    Sent when an attached object gains focus because the previous focus object has been detached from the hand
        //
        // See https://valvesoftware.github.io/steamvr_unity_plugin/articles/Interaction-System.html

        /// <summary>
        /// The next available ID to be assigned.
        /// </summary>
        private static uint nextID = 0;

        /// <summary>
        /// The interactable objects.
        /// </summary>
        private static readonly Dictionary<uint, InteractableObject> interactableObjects = new Dictionary<uint, InteractableObject>();

        /// <summary>
        /// The unique id of the interactable object.
        /// </summary>
        public uint ID { get; private set; }

        public bool IsHovered { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsGrabbed { get; private set; }

        public Interactable InteractableObj { get; private set; }
        public Net.Synchronizer SynchronizerObj { get; private set; }

        private readonly Color LocalHoverColor = Utils.ColorPalette.Viridis(0.0f);
        private readonly Color RemoteHoverColor = Utils.ColorPalette.Viridis(0.2f);
        private readonly Color LocalSelectColor = Utils.ColorPalette.Viridis(0.4f);
        private readonly Color RemoteSelectColor = Utils.ColorPalette.Viridis(0.6f);
        private readonly Color LocalGrabColor = Utils.ColorPalette.Viridis(0.8f);
        private readonly Color RemoteGrabColor = Utils.ColorPalette.Viridis(0.0f);

        private void Awake()
        {
            ID = nextID++;
            interactableObjects.Add(ID, this);
            
            InteractableObj = GetComponent<Interactable>();
            if (InteractableObj == null)
            {
                Debug.LogErrorFormat("Game object {0} has no component Interactable attached to it.\n", gameObject.name);
            }
        }

        /// <summary>
        /// Returns the interactable object of given id or <code>null</code>, if it does
        /// not exist.
        /// </summary>
        /// <param name="id">The id of the interactable object.</param>
        /// <returns></returns>
        public static InteractableObject Get(uint id)
        {
            if (!interactableObjects.TryGetValue(id, out InteractableObject result))
            {
                result = null;
            }
            return result;
        }

        public void SetHover(bool hover, bool isOwner)
        {
            IsHovered = hover;

            if (!IsSelected && !IsGrabbed)
            {
                bool hasOutline = TryGetComponent(out Outline outline);

                if (hover)
                {
                    if (hasOutline)
                    {
                        outline.SetColor(isOwner ? LocalHoverColor : RemoteHoverColor);
                    }
                    else
                    {
                        Outline.Create(gameObject, isOwner ? LocalHoverColor : RemoteHoverColor);
                    }
                }
                else
                {
                    if (hasOutline)
                    {
                        DestroyImmediate(outline);
                    }
                }

                if (!Net.Network.UseInOfflineMode && isOwner)
                {
                    new Net.SetHoverAction(this, hover).Execute();
                }
            }
        }

        public void SetSelect(bool select, bool isOwner)
        {
            IsSelected = select;

            if (!IsGrabbed)
            {
                bool hasOutline = TryGetComponent(out Outline outline);

                if (select)
                {
                    if (hasOutline)
                    {
                        outline.SetColor(isOwner ? LocalSelectColor : RemoteSelectColor);
                    }
                    else
                    {
                        Outline.Create(gameObject, isOwner ? LocalSelectColor : RemoteSelectColor);
                    }
                }
                else
                {
                    if (IsHovered)
                    {
                        SetHover(true, isOwner);
                    }
                    else if (hasOutline)
                    {
                        DestroyImmediate(outline);
                    }
                }

                if (!Net.Network.UseInOfflineMode && isOwner)
                {
                    new Net.SetSelectAction(this, select).Execute();
                }
            }
        }

        public void SetGrab(bool grab, bool isOwner)
        {
            IsGrabbed = grab;

            bool hasOutline = TryGetComponent(out Outline outline);

            if (grab)
            {
                if (hasOutline)
                {
                    outline.SetColor(isOwner ? LocalGrabColor : RemoteGrabColor);
                }
                else
                {
                    Outline.Create(gameObject, isOwner ? LocalGrabColor : RemoteGrabColor);
                }
            }
            else
            {
                if (IsSelected)
                {
                    SetSelect(true, isOwner);
                }
                else if (IsHovered)
                {
                    SetHover(true, isOwner);
                }
                else if (hasOutline)
                {
                    DestroyImmediate(outline);
                }
            }

            if (!Net.Network.UseInOfflineMode && isOwner)
            {
                new Net.SetGrabAction(this, grab).Execute();
                if (grab)
                {
                    SynchronizerObj = InteractableObj.gameObject.AddComponent<Net.Synchronizer>();
                }
                else
                {
                    Destroy(SynchronizerObj);
                    SynchronizerObj = null;
                }
            }
        }

        //----------------------------------------------------------------
        // Mouse actions
        //----------------------------------------------------------------

        private void OnMouseEnter()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                SetHover(true, true);
            }
        }

        private void OnMouseExit()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                SetHover(false, true);
            }
        }

        //----------------------------------------------------------------
        // Private actions called by the hand when the object is hovered.
        // These methods are called by SteamVR by way of the interactable.
        // <see cref="Hand.Update"/>
        //----------------------------------------------------------------

        private const Hand.AttachmentFlags AttachmentFlags
            = Hand.defaultAttachmentFlags
            & (~Hand.AttachmentFlags.SnapOnAttach)
            & (~Hand.AttachmentFlags.DetachOthers)
            & (~Hand.AttachmentFlags.VelocityMovement);

        private void OnHandHoverBegin(Hand hand) => SetHover(true, true);
        private void OnHandHoverEnd(Hand hand) => SetHover(false, true);
    }
}