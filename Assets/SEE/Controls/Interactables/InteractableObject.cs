using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Input;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Super class of the behaviours of game objects the player interacts with.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(NodeRef))]
    public sealed class InteractableObject : MonoBehaviour, IMixedRealityFocusHandler
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
        /// The hovered objects.
        /// </summary>
        public static readonly HashSet<InteractableObject> HoveredObjects = new HashSet<InteractableObject>();

        /// <summary>
        /// The selected objects.
        /// </summary>
        public static readonly HashSet<InteractableObject> SelectedObjects = new HashSet<InteractableObject>();

        /// <summary>
        /// The grabbed objects.
        /// </summary>
        public static readonly HashSet<InteractableObject> GrabbedObjects = new HashSet<InteractableObject>();

        /// <summary>
        /// The unique id of the interactable object.
        /// </summary>
        public uint ID { get; private set; }

        /// <summary>
        /// Whether the object is currently hovered by e.g. the mouse or the VR-
        /// controller.
        /// </summary>
        public bool IsHovered { get; private set; }

        /// <summary>
        /// Whether the object is currently selected by e.g. the mouse or the VR-
        /// controller.
        /// </summary>
        public bool IsSelected { get; private set; }

        /// <summary>
        /// Whether the object is currently grabbed by e.g. the mouse or the VR-
        /// controller.
        /// </summary>
        public bool IsGrabbed { get; private set; }

        /// <summary>
        /// The interactable component, that is used by SteamVR. The interactable
        /// component is attached to <code>this.gameObject</code>.
        /// </summary>
        private Interactable interactable;

        /// <summary>
        /// The synchronizer is attached to <code>this.gameObject</code>, iff it is
        /// grabbed.
        /// </summary>
        public Net.Synchronizer InteractableSynchronizer { get; private set; }

        private void Awake()
        {
            ID = nextID++;
            interactableObjects.Add(ID, this);

            gameObject.TryGetComponentOrLog(out interactable);
        }

        /// <summary>
        /// Returns the interactable object of given id or <code>null</code>, if it does
        /// not exist.
        /// </summary>
        /// <param name="id">The id of the interactable object.</param>
        /// <returns></returns>
        public static InteractableObject Get(uint id)
        {
            interactableObjects.TryGetValue(id, out InteractableObject result);
            return result;
        }

        #region Interaction

        /// <summary>
        /// Visually emphasizes this object for hovering. 
        /// 
        /// Note: This method may be called locally when a local user interacts with the
        /// object or remotely when a remote user has interacted with the object. In the
        /// former case, <paramref name="isOwner"/> will be true. In the
        /// latter case, it will be called via <see cref="SEE.Net.SetHoverAction.ExecuteOnClient()"/>
        /// where <paramref name="isOwner"/> is false.
        /// </summary>
        /// <param name="hover">Whether this object should be hovered.</param>
        /// <param name="isOwner">Whether this client is initiating the hovering action.
        /// </param>
        public void SetHover(bool hover, bool isOwner)
        {
            IsHovered = hover;

            if (hover)
            {
                HoverIn?.Invoke(isOwner);
                AnyHoverIn?.Invoke(this, isOwner);
                HoveredObjects.Add(this);
            }
            else
            {
                HoverOut?.Invoke(isOwner);
                AnyHoverOut?.Invoke(this, isOwner);
                HoveredObjects.Remove(this);
            }

            if (!Net.Network.UseInOfflineMode && isOwner)
            {
                new Net.SetHoverAction(this, hover).Execute();
            }
        }        

        /// <summary>
        /// Visually emphasizes this object for selection.
        /// </summary>
        /// <param name="hover">Whether this object should be selected.</param>
        /// <param name="isOwner">Whether this client is initiating the selection action.
        /// </param>
        public void SetSelect(bool select, bool isOwner)
        {
            IsSelected = select;

            if (!IsGrabbed && !IsSelected && IsHovered)
            {
                // Hovering is a continuous operation, that is why we call it here
                // when the object is in the focus but neither grabbed nor selected.
                SetHover(true, isOwner);
            }

            if (select)
            {
                SelectIn?.Invoke(isOwner);
                AnySelectIn?.Invoke(this, isOwner);
                SelectedObjects.Add(this);
            }
            else
            {
                SelectOut?.Invoke(isOwner);
                AnySelectOut?.Invoke(this, isOwner);
                SelectedObjects.Remove(this);
            }

            if (!Net.Network.UseInOfflineMode && isOwner)
            {
                new Net.SetSelectAction(this, select).Execute();
            }
        }

        /// <summary>
        /// Deselects all currently selected interactable objects.
        /// </summary>
        /// <param name="isOwner">Whether this client is initiating the selection action.
        public static void UnselectAll(bool isOwner)
        {
            // We cannot iterate on SelectedObjects directly because SetSelect will
            // remove the iterated interactable from SelectedObjects. That is why
            // we convert SelectedObjects to an array first.
            foreach (InteractableObject interactable in SelectedObjects.ToArray())
            {
                interactable.SetSelect(false, isOwner);
            }
        }

        /// <summary>
        /// Visually emphasizes this object for grabbing.
        /// </summary>
        /// <param name="grab">Whether this object should be grabbed.</param>
        /// <param name="isOwner">Whether this client is initiating the grabbing action.
        /// </param>
        public void SetGrab(bool grab, bool isOwner)
        {
            IsGrabbed = grab;

            if (grab)
            {
                GrabIn?.Invoke(isOwner);
                AnyGrabIn?.Invoke(this, isOwner);
                GrabbedObjects.Add(this);
            }
            else
            {
                GrabOut?.Invoke(isOwner);
                AnyGrabOut?.Invoke(this, isOwner);
                // Hovering and selection are continuous operations, that is why we call them here
                // when the object is in the focus but not grabbed any longer.
                if (IsSelected)
                {
                    SetSelect(true, isOwner);
                }
                else if (IsHovered)
                {
                    SetHover(true, isOwner);
                }
                GrabbedObjects.Remove(this);
            }

            if (!Net.Network.UseInOfflineMode && isOwner)
            {
                new Net.SetGrabAction(this, grab).Execute();
                if (grab)
                {
                    InteractableSynchronizer = interactable.gameObject.AddComponent<Net.Synchronizer>();
                }
                else
                {
                    Destroy(InteractableSynchronizer);
                    InteractableSynchronizer = null;
                }
            }
        }

        #endregion

        #region Events

        ///------------------------------------------------------------------
        /// Actions can register on selection, hovering, and grabbing events.
        /// Then they will be invoked if those events occur.
        ///------------------------------------------------------------------
        ///
        /// ----------------------------
        /// Hovering event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a hovering event has happened (hover over
        /// or hover off the game object).
        /// </summary>
        public delegate void HoverAction(bool isOwner);
        /// <summary>
        /// Event to be triggered when this game object is being hovered over.
        /// </summary>
        public event HoverAction HoverIn;
        /// <summary>
        /// Event to be triggered when this game object is no longer hovered over.
        /// </summary>
        public event HoverAction HoverOut;

        public delegate void AnyHoverAction(InteractableObject interactableObject, bool isOwner);
        public static event AnyHoverAction AnyHoverIn;
        public static event AnyHoverAction AnyHoverOut;

        /// ----------------------------
        /// Selection event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a selection event has happened (selecting
        /// or deselecting the game object).
        /// </summary>
        public delegate void SelectAction(bool isOwner);
        /// <summary>
        /// Event to be triggered when this game object is being selected.
        /// </summary>
        public event SelectAction SelectIn;
        /// <summary>
        /// Event to be triggered when this game object is no longer selected.
        /// </summary>
        public event SelectAction SelectOut;

        public delegate void AnySelectAction(InteractableObject interactableObject, bool isOwner);
        public static event AnySelectAction AnySelectIn;
        public static event AnySelectAction AnySelectOut;

        /// ----------------------------
        /// Grabbing event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a grab event has happened (grabbing
        /// or releasing the game object).
        /// </summary>
        public delegate void GrabAction(bool isOwner);
        /// <summary>
        /// Event to be triggered when this game object is being grabbed.
        /// </summary>
        public event GrabAction GrabIn;
        /// <summary>
        /// Event to be triggered when this game object is no longer grabbed.
        /// </summary>
        public event GrabAction GrabOut;

        public delegate void AnyGrabAction(InteractableObject interactableObject, bool isOwner);
        public static event AnyGrabAction AnyGrabIn;
        public static event AnyGrabAction AnyGrabOut;

#if false // TODO(torben): will we ever need this?
        public delegate void CollisionAction(InteractableObject interactableObject, Collision collision);
        public event CollisionAction CollisionIn;
        public event CollisionAction CollisionOut;

        private void OnCollisionEnter(Collision collision) => CollisionIn?.Invoke(this, collision);
        private void OnCollisionExit(Collision collision) => CollisionIn?.Invoke(this, collision);
#endif

        //----------------------------------------------------------------
        // Mouse actions
        //----------------------------------------------------------------

        private void OnMouseEnter()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                SetHover(true, true);
            }
        }

        private void OnMouseOver()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                if (IsHovered && Raycasting.IsMouseOverGUI())
                {
                    SetHover(false, true);
                }
                else if (!IsHovered && !Raycasting.IsMouseOverGUI())
                {
                    SetHover(true, true);
                }
            }
        }

        private void OnMouseExit()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                SetHover(false, true);
            }
        }
        
        //----------------------------------------
        // Actions called by the MRTK on HoloLens.
        //----------------------------------------
        
        public void OnFocusEnter(FocusEventData eventData)
        {
            // In case of eye gaze, we discard the input.
            // We handle eye gaze using the BaseEyeFocusHandler in order to only activate hovering mechanisms
            // when the user dwells on the object, otherwise the sudden changes would be too jarring.
            if (eventData.Pointer.InputSourceParent.SourceType != InputSourceType.Eyes)
            {
                SetHover(true, true);
            }
        }

        public void OnFocusExit(FocusEventData eventData)
        {
            // Similarly to OnFocusEnter(), we discard the input in case of eye gaze to avoid jarring changes.
            if (eventData.Pointer.InputSourceParent.SourceType != InputSourceType.Eyes 
                && !eventData.Pointer.PointerName.StartsWith("None"))
            {
                // Unfortunately, there seems to be a bug in the MRTK:
                // The SourceType is falsely reported by the MRTK as "Hands" here
                // (in contrast to OnFocusEnter(), where Eyes are correctly reported.)
                // The only recognizable difference seems to be that the pointer isn't attached to any hand
                // so it's just called "None Hand" instead of "Right Hand", we use this to detect it.
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
            & ~Hand.AttachmentFlags.SnapOnAttach
            & ~Hand.AttachmentFlags.DetachOthers
            & ~Hand.AttachmentFlags.VelocityMovement;

        private void OnHandHoverBegin(Hand hand) => SetHover(true, true);
        private void OnHandHoverEnd(Hand hand) => SetHover(false, true);

        #endregion

    }
}