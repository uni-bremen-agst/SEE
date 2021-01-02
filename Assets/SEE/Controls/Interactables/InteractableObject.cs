using System.Collections.Generic;
using System.Linq;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public enum HoverFlag
    {
        None                     = 0x0,
        World                    = 0x1,
        ChartMarker              = 0x2,
        ChartMultiSelect         = 0x4,
        ChartScrollViewToggle    = 0x8
    }

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
        private static readonly Dictionary<uint, InteractableObject> idToInteractableObjectDict = new Dictionary<uint, InteractableObject>(); // TODO(torben): is a simple list sufficient?

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

        public uint HoverFlags { get; private set; } = 0;

        /// <summary>
        /// Whether the object is currently hovered by e.g. the mouse or the VR-
        /// controller.
        /// </summary>
        public bool IsHovered => HoverFlags != 0;

        /// <summary>
        /// Whether the given hover flag is set.
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns><code>true</code> if the given flag is set, <code>false</code>
        /// otherwise.</returns>
        public bool IsHoverFlagSet(HoverFlag flag) => (HoverFlags & (uint)flag) != 0;

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

        /// <summary>
        /// The local player to be informed about his/her own hovered, selected,
        /// or grabbed objects.
        /// </summary>
        private PlayerActions localPlayerActions;

        private void Awake()
        {
            ID = nextID++;
            idToInteractableObjectDict.Add(ID, this);

            interactable = GetComponent<Interactable>();
            if (interactable == null)
            {
                Debug.LogErrorFormat("Game object {0} has no component Interactable attached to it.\n", gameObject.name);
            }
        }

        private void Start()
        {
            localPlayerActions = PlayerSettings.LocalPlayer?.GetComponent<PlayerActions>();
        }

        /// <summary>
        /// Returns the interactable object of given id or <code>null</code>, if it does
        /// not exist.
        /// </summary>
        /// <param name="id">The id of the interactable object.</param>
        /// <returns></returns>
        public static InteractableObject Get(uint id)
        {
            if (!idToInteractableObjectDict.TryGetValue(id, out InteractableObject result))
            {
                result = null;
            }
            return result;
        }

        #region Interaction

        public void SetHoverFlags(uint hoverFlags, bool isOwner)
        {
            HoverFlags = hoverFlags;

            if (IsHovered)
            {
                HoverIn?.Invoke(this, isOwner);
                HoveredObjects.Add(this);
                if (isOwner)
                {
                    localPlayerActions?.HoverOn(gameObject);
                }
            }
            else
            {
                HoverOut?.Invoke(this, isOwner);
                HoveredObjects.Remove(this);
                if (isOwner)
                {
                    localPlayerActions?.HoverOff(gameObject);
                }
            }

            if (!Net.Network.UseInOfflineMode && isOwner)
            {
                new Net.SetHoverAction(this, hoverFlags).Execute();
            }
        }

        /// <summary>
        /// Visually emphasizes this object for hovering. 
        /// 
        /// Note: This method may be called locally when a local user interacts with the
        /// object or remotely when a remote user has interacted with the object. In the
        /// former case, <paramref name="isOwner"/> will be true. In the
        /// latter case, it will be called via <see cref="SEE.Net.SetHoverAction.ExecuteOnClient()"/>
        /// where <paramref name="isOwner"/> is false.
        /// </summary>
        /// <param name="hoverFlag">The flag to be set or unset.</param>
        /// <param name="setFlag">Whether this object should be hovered.</param>
        /// <param name="isOwner">Whether this client is initiating the hovering action.
        /// </param>
        public void SetHoverFlag(HoverFlag hoverFlag, bool setFlag, bool isOwner)
        {
            uint hoverFlags;
            if (setFlag)
            {
                hoverFlags = HoverFlags | (uint)hoverFlag;
            }
            else
            {
                hoverFlags = HoverFlags & ~(uint)hoverFlag;
            }
            SetHoverFlags(hoverFlags, isOwner);
        }

        public static void UnhoverAll(bool isOwner)
        {
            while (HoveredObjects.Count != 0)
            {
                HoveredObjects.ElementAt(HoveredObjects.Count - 1).SetHoverFlags(0, isOwner);
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
                SetHoverFlag(HoverFlag.None, true, isOwner); // TODO(torben): is this really necessary? a hover event is invoked, even though nothing changes. these events also create unnecessary performance overhead. also: @DoubleHoverEventPerformance
            }

            if (select)
            {
                SelectIn?.Invoke(this, isOwner);
                SelectedObjects.Add(this);
                if (isOwner)
                {
                    localPlayerActions?.SelectOn(gameObject);
                }
            }
            else
            {
                SelectOut?.Invoke(this, isOwner);
                SelectedObjects.Remove(this);
                if (isOwner)
                {
                    localPlayerActions?.SelectOff(gameObject);
                }
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
            while (SelectedObjects.Count != 0)
            {
                SelectedObjects.ElementAt(SelectedObjects.Count - 1).SetSelect(false, isOwner);
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
                GrabIn?.Invoke(this, isOwner);
                GrabbedObjects.Add(this);
                if (isOwner)
                {
                    localPlayerActions?.GrabOn(gameObject);
                }
            }
            else
            {
                GrabOut?.Invoke(this, isOwner);
                if (isOwner)
                {
                    localPlayerActions?.GrabOff(gameObject);
                }
                // Hovering and selection are continuous operations, that is why we call them here
                // when the object is in the focus but not grabbed any longer.
                if (IsSelected)
                {
                    SetSelect(true, isOwner); // see: @DoubleHoverEventPerformance
                }
                else if (IsHovered)
                {
                    SetHoverFlag(HoverFlag.None, true, isOwner); // see: @DoubleHoverEventPerformance
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

        public static void UngrabAll(bool isOwner)
        {
            while (GrabbedObjects.Count != 0)
            {
                GrabbedObjects.ElementAt(GrabbedObjects.Count - 1).SetGrab(false, isOwner);
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
        public delegate void HoverAction(InteractableObject interactableObject, bool isOwner);
        /// <summary>
        /// Event to be triggered when this game object is being hovered over.
        /// </summary>
        public event HoverAction HoverIn;
        /// <summary>
        /// Event to be triggered when this game object is no longer hovered over.
        /// </summary>
        public event HoverAction HoverOut;

        /// ----------------------------
        /// Selection event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a selection event has happened (selecting
        /// or deselecting the game object).
        /// </summary>
        public delegate void SelectAction(InteractableObject interactableObject, bool isOwner);
        /// <summary>
        /// Event to be triggered when this game object is being selected.
        /// </summary>
        public event SelectAction SelectIn;
        /// <summary>
        /// Event to be triggered when this game object is no longer selected.
        /// </summary>
        public event SelectAction SelectOut;

        /// ----------------------------
        /// Grabbing event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a grab event has happened (grabbing
        /// or releasing the game object).
        /// </summary>
        public delegate void GrabAction(InteractableObject interactableObject, bool isOwner);
        /// <summary>
        /// Event to be triggered when this game object is being grabbed.
        /// </summary>
        public event GrabAction GrabIn;
        /// <summary>
        /// Event to be triggered when this game object is no longer grabbed.
        /// </summary>
        public event GrabAction GrabOut;

        //----------------------------------------------------------------
        // Mouse actions
        //----------------------------------------------------------------

        private void OnMouseEnter()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                SetHoverFlag(HoverFlag.World, true, true);
            }
        }

        private void OnMouseOver()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                bool isWorldBitSet = (HoverFlags & (uint)HoverFlag.World) != 0;
                if (isWorldBitSet && Raycasting.IsMouseOverGUI())
                {
                    SetHoverFlag(HoverFlag.World, false, true);
                }
                else if (!isWorldBitSet && !Raycasting.IsMouseOverGUI())
                {
                    SetHoverFlag(HoverFlag.World, true, true);
                }
            }
        }

        private void OnMouseExit()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                SetHoverFlag(HoverFlag.World, false, true);
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

        private void OnHandHoverBegin(Hand hand) => SetHoverFlag(HoverFlag.World, true, true);
        private void OnHandHoverEnd(Hand hand) => SetHoverFlag(HoverFlag.World, false, true);

        #endregion
    }
}