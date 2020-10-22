using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using TMPro;
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
        /// The text label that's displayed above the object when the user hovers over it.
        /// Will be <code>null</code> when the label is not currently being displayed.
        /// </summary>
        private GameObject nodeLabel;

        /// <summary>
        /// Settings for the visualization of the node.
        /// </summary>
        private AbstractSEECity settings;

        /// <summary>
        /// True if this node is a leaf. This value is cached to avoid frequent retrievals.
        /// </summary>
        private bool isLeaf;

        /// <summary>
        /// The synchronizer is attached to <code>this.gameObject</code>, iff it is
        /// grabbed.
        /// </summary>
        public Net.Synchronizer InteractableSynchronizer { get; private set; }

        /// <summary>
        /// The local hovering color of the outline.
        /// </summary>
        private readonly Color LocalHoverColor = Utils.ColorPalette.Viridis(0.0f);

        /// <summary>
        /// The remote hovering color of the outline.
        /// </summary>
        private readonly Color RemoteHoverColor = Utils.ColorPalette.Viridis(0.2f);

        /// <summary>
        /// The local selection color of the outline.
        /// </summary>
        private readonly Color LocalSelectColor = Utils.ColorPalette.Viridis(0.4f);

        /// <summary>
        /// The remote selection color of the outline.
        /// </summary>
        private readonly Color RemoteSelectColor = Utils.ColorPalette.Viridis(0.6f);

        /// <summary>
        /// The local grabbing color of the outline.
        /// </summary>
        private readonly Color LocalGrabColor = Utils.ColorPalette.Viridis(0.8f);

        /// <summary>
        /// The remote grabbing color of the outline.
        /// </summary>
        private readonly Color RemoteGrabColor = Utils.ColorPalette.Viridis(0.0f);

        private void Awake()
        {
            ID = nextID++;
            interactableObjects.Add(ID, this);

            interactable = GetComponent<Interactable>();
            if (interactable == null)
            {
                Debug.LogErrorFormat("Game object {0} has no component Interactable attached to it.\n", gameObject.name);
            }
            
            // Traverse parents until we reach the gameObject with tag "Code City", so that we can access its settings.
            // We also set a maximum of 1000 traversals in case something goes horribly wrong, to avoid an infinite loop.
            GameObject rootCity = gameObject;
            for (uint i = 0; i < 1000 && !rootCity.CompareTag("Code City"); i++)
            { 
                // According to Unity documentation, none of these will ever be null
                rootCity = rootCity.transform.root.gameObject;
            }

            UnityEngine.Assertions.Assert.IsTrue(rootCity.TryGetComponent(out settings));

            isLeaf = gameObject.GetComponent<NodeRef>()?.node?.IsLeaf() ?? false;
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

        #region Interaction

        /// <summary>
        /// Visually emphasizes this object for hovering.
        /// </summary>
        /// <param name="hover">Whether this object should be hovered.</param>
        /// <param name="isOwner">Whether this client is initiating the hovering action.
        /// </param>
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
            }

            if (hover)
            {
                CreateObjectLabel();
                HoveredObjects.Add(this);
            }
            else
            {
                DestroyObjectLabel();
                HoveredObjects.Remove(this);
            }

            if (!Net.Network.UseInOfflineMode && isOwner)
            {
                new Net.SetHoverAction(this, hover).Execute();
            }
        }

        /// <summary>
        /// Returns true iff labels are enabled for this node type.
        /// </summary>
        /// <returns>true iff labels are enabled for this node type</returns>
        private bool LabelsEnabled()
        {
            return isLeaf && settings.ShowLabel || !isLeaf && settings.InnerNodeShowLabel;
        }

        /// <summary>
        /// Creates a text label above the object with its node's SourceName if the label doesn't exist yet.
        /// </summary>
        private void CreateObjectLabel()
        {
            if (!LabelsEnabled()) return;  // If labels are disabled, we don't need to do anything
            
            // If label already exists, nothing needs to be done
            if (nodeLabel != null || !gameObject.TryGetComponent(out NodeRef nodeRef)) return;
            
            Node node = nodeRef.node;
            if (node == null) return;
            
            // Add text
            Vector3 position = gameObject.transform.position;
            position.y += isLeaf ? settings.LabelDistance : settings.InnerNodeLabelDistance;
            nodeLabel = TextFactory.GetText(node.SourceName, position, 
                isLeaf ? settings.LabelSize : settings.InnerNodeLabelSize, textColor: Color.black);
            nodeLabel.transform.SetParent(gameObject.transform);
            
            // Add connecting line between "roof" of object and text
            Vector3 labelPosition = nodeLabel.transform.position;
            Vector3 nodeTopPosition = gameObject.transform.position;
            nodeTopPosition.y = BoundingBox.GetRoof(new List<GameObject> {gameObject});
            labelPosition.y -= nodeLabel.GetComponent<TextMeshPro>().textBounds.extents.y;
            LineFactory.Draw(nodeLabel, new []{nodeTopPosition, labelPosition}, 0.01f, 
                Materials.New(Materials.ShaderType.TransparentLine, Color.black.ColorWithAlpha(0.9f)));

            Portal.SetInfinitePortal(nodeLabel);
        }

        /// <summary>
        /// Destroys the text label above the object if it exists.
        /// </summary>
        /// <seealso cref="CreateObjectLabel"/>
        private void DestroyObjectLabel()
        {
            if (!LabelsEnabled()) return;  // If labels are disabled, we don't need to do anything
            if (nodeLabel != null) Destroyer.DestroyGameObject(nodeLabel);
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
            }

            if (select)
            {
                SelectedObjects.Add(this);
            }
            else
            {
                SelectedObjects.Remove(this);
            }

            if (!Net.Network.UseInOfflineMode && isOwner)
            {
                new Net.SetSelectAction(this, select).Execute();
            }
        }

        /// <summary>
        /// Visually emphasizes this object for grabbing.
        /// </summary>
        /// <param name="hover">Whether this object should be grabbed.</param>
        /// <param name="isOwner">Whether this client is initiating the grabbing action.
        /// </param>
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

                GrabbedObjects.Add(this);
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

        #endregion
    }
}