using System.Collections.Generic;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using SEE.XR;
using SEE.Game;
using SEE.UI;
using SEE.Controls.Interactables;

namespace SEE.Utils
{
    /// <summary>
    /// What precisely was hit by a ray cast.
    /// </summary>
    public enum HitGraphElement
    {
        None, // Neither a node nor an edge was hit.
        Node, // A node was hit.
        Edge, // An edge was hit.
        Author, // An author of a file for code cities representing repository data was hit.
        Auxiliary, // An auxiliary object was hit, such as a resize handle or a rotation handle
    }

    /// <summary>
    /// Utilities related to ray casting.
    /// </summary>
    public static class Raycasting
    {
        /// <summary>
        /// Maximal interaction distance.
        /// </summary>
        [Tooltip("Maximal interaction distance (World Units).")]
        public const float InteractionRadius = 3.0f;

        /// <summary>
        /// Number of raycast hits we can store in the buffer for <see cref="RaycastLowestNode"/>.
        /// </summary>
        private const uint raycastBufferSize = 500;

        /// <summary>
        /// Layer number for game objects representing UI components.
        /// </summary>
        private const uint uiLayer = 5;

        /// <summary>
        /// Names of game objects which are on the <see cref="uiLayer"/>,
        /// but won't "count" as UI components, e.g., when checking whether
        /// the user is currently hovering over any UI components.
        /// </summary>
        private static readonly ISet<string> ignoredUINames =
            new HashSet<string>
            {
                "ChatBox" // ignored because otherwise it'd obscure half the screen.
            };

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing.
        /// The hit will be set, if no GUI element is hit.
        /// <para>
        /// Note: <paramref name="elementRef"/> is null if the result is <see cref="HitGraphElement.None"/>.
        /// Yet, <paramref name="raycastHit"/> will always be the hit object if any was hit,
        /// no matter whether it was a graph element or not.
        /// </para>
        /// </summary>
        /// <param name="raycastHit">The hit object.</param>
        /// <param name="nodeRef">The hit graph element if None is not returned.</param>
        /// <param name="requireInteractable">If true, only raycasts against <see cref="InteractableObject"/>s
        /// on the interactable layer. Passing false usually excludes objects outside their portal.</param>
        /// <param name="maxDistance">The maximum distance to raycast, defaults to <see cref="InteractionRadius"/>.</param>
        /// <returns>If no GUI element is hit, but a GameObject with either
        /// an attached <see cref="NodeRef"/> or <see cref="EdgeRef"/> is hit, then
        /// <see cref="HitGraphElement.Node"/> or <see cref="HitGraphElement.Edge"/>,
        /// respectively, is returned. Otherwise if a GUI element is hit or if the
        /// hit game object has neither a <see cref="NodeRef"/> nor an <see cref="EdgeRef"/>
        /// attached, <see cref="HitGraphElement.None"/> is returned.
        /// </returns>
        public static HitGraphElement RaycastGraphElement(
                out RaycastHit raycastHit,
                out GraphElementRef elementRef,
                bool requireInteractable = true,
                float maxDistance = InteractionRadius)
        {
            RaycastHit actualHit;
            int layerMask = requireInteractable ? Layers.InteractableGraphObjectsLayerMask : Layers.GraphObjectsLayerMask;
            if (!IsMouseOverGUI() && Physics.Raycast(UserPointsTo(), out actualHit, maxDistance, layerMask))
            {
                raycastHit = actualHit;
                if (raycastHit.transform.TryGetComponent(out NodeRef nodeRef))
                {
                    elementRef = nodeRef;
                    return HitGraphElement.Node;
                }
                else if (raycastHit.transform.TryGetComponent(out EdgeRef edgeRef))
                {
                    elementRef = edgeRef;
                    return HitGraphElement.Edge;
                }
                else
                {
                    elementRef = null;
                    return HitGraphElement.None;
                }
            }
            else
            {
                raycastHit = new RaycastHit();
                elementRef = null;
                return HitGraphElement.None;
            }
        }

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing.
        /// Returns true if the mouse is not over any GUI element and if anything was hit.
        /// <paramref name="raycastHit"/> will be set, if true is returned.
        /// </summary>
        /// <param name="raycastHit">Hit object if true is returned, undefined otherwise.</param>
        /// <param name="maxDistance">The maximum distance to raycast, defaults to <see cref="InteractionRadius"/>.</param>
        /// <returns>True if the mouse is not over any GUI element and if anything was hit.</returns>
        public static bool RaycastAnything(out RaycastHit raycastHit, float maxDistance = InteractionRadius)
        {
            raycastHit = new RaycastHit();
            Physics.queriesHitBackfaces = true;
            return !IsMouseOverGUI() && Physics.Raycast(UserPointsTo(), out raycastHit, maxDistance);
        }

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing and chooses the
        /// node that is the lowest one in the node hierarchy, if considered relevant for a hit.
        /// More precisely, the lowest one is that one with the greatest value of the node attribute
        /// Level, where Level counting starts at the root and increases downward into the tree.
        ///
        /// <para>
        /// If <paramref name="referenceNode"/> equals null, all nodes are considered relevant.
        /// Otherwise a node is considered relevant if it is in the same graph as
        /// <paramref name="referenceNode"/> and neither <paramref name="referenceNode"/> itself nor
        /// any of its descendants in the node hierarchy in the underlying graph.
        /// </para>
        /// </summary>
        /// <param name="raycastHit">Hit object of lowest node if true is returned, null otherwise.</param>
        /// <param name="hitNode">Lowest node if true is returned, null otherwise.</param>
        /// <param name="referenceNode">If given, all nodes which are not in the same graph as
        /// <paramref name="referenceNode"/> as well as itself will not be considered when sorting raycast results.</param>
        /// <param name="requireInteractable">If true, only raycasts against <see cref="InteractableObject"/>s
        /// on the interactable layer. Passing false usually excludes objects outside their portal.</param>
        /// /// <param name="maxDistance">The maximum distance to raycast, defaults to <see cref="InteractionRadius"/>.</param>
        /// <returns>True if the mouse was over at least one node fulfilling the criteria; only then
        /// <paramref name="hitNode"/> and <paramref name="raycastHit"/> are defined.</returns>
        public static bool RaycastLowestNode(
                out RaycastHit? raycastHit,
                out Node hitNode,
                NodeRef referenceNode = null,
                bool requireInteractable = true,
                float maxDistance = InteractionRadius)
        {
            RaycastHit[] hits = new RaycastHit[raycastBufferSize];
            int numberOfHits;
            if (requireInteractable)
            {
                numberOfHits = Physics.RaycastNonAlloc(UserPointsTo(), hits, maxDistance, Layers.InteractableGraphObjectsLayerMask);
            }
            else
            {
                numberOfHits = Physics.RaycastNonAlloc(UserPointsTo(), hits, maxDistance);
            }
            if (numberOfHits == raycastBufferSize)
            {
                Debug.LogWarning("We possibly got more hits than buffer space is available.\n");
            }

            raycastHit = null;
            hitNode = null;
            // We are using a loop iteration bounded by numberOfHits. A foreach loop would traverse
            // all elements in hits, where most of them would be null.
            for (int i = 0; i < numberOfHits; i++)
            {
                RaycastHit hit = hits[i];
                // referenceNode will be ignored if set
                if (hit.collider.gameObject == referenceNode.gameObject)
                {
                    continue;
                }

                // Is it a node at all?
                NodeRef hitNodeRef = hit.transform.GetComponent<NodeRef>();
                if (hitNodeRef == null || hitNodeRef.Value == null)
                {
                    continue;
                }

                // Is it in the same graph as the reference node, if present?
                if (referenceNode == null || referenceNode.Value == null || hitNodeRef.Value.ItsGraph != referenceNode.Value.ItsGraph)
                {
                    continue;
                }

                // Have we found a node deeper into the tree than the current hitNode?
                if (hitNode != null && hitNode.Level >= hitNodeRef.Value.Level)
                {
                    continue;
                }

                // Check whether descendants are to be ignored and if so, whether the hit node is a descendant
                if (referenceNode != null && hitNodeRef.Value.IsDescendantOf(referenceNode.Value))
                {
                    continue;
                }

                hitNode = hitNodeRef.Value;
                raycastHit = hit;
            }
            return hitNode != null;
        }

        /// <summary>
        /// Raycasts against <see cref="InteractableObject"/>s and outputs either the closest hit
        /// or null, if no such hit exists.
        /// </summary>
        /// <param name="raycastHit">The raycast hit for the hit interactable object or the default value.</param>
        /// <param name="io">The hit object or null.</param>
        /// <param name="requireInteractable">If true, raycasts using <see cref="Layers.InteractableGraphObjectsLayerMask"/>,
        /// else <see cref="Layers.GraphObjectsLayerMask"/>.
        /// Passing false usually excludes objects outside their portal.</param>
        /// <param name="maxDistance">The maximum distance to raycast, defaults to <see cref="InteractionRadius"/>.</param>
        /// <returns>The corresponding enum value for the hit.</returns>
        public static HitGraphElement RaycastInteractableObject(
                out RaycastHit raycastHit,
                out InteractableObject io,
                bool requireInteractable = true,
                float maxDistance = InteractionRadius)
        {
            int layer = requireInteractable ? Layers.InteractableGraphObjectsLayerMask : Layers.GraphObjectsLayerMask;
            if (RaycastInteractableObjectBase(out RaycastHit hit, out InteractableObjectBase obj, layer, maxDistance)
                && obj is InteractableObject interactableObject)
            {
                raycastHit = hit;
                io = interactableObject;
                if (obj is InteractableGraphElement graphElement)
                {
                    return graphElement.GraphElemRef.Elem switch
                    {
                        null => HitGraphElement.None,
                        Node => HitGraphElement.Node,
                        Edge => HitGraphElement.Edge,
                        _ => throw new System.ArgumentOutOfRangeException()
                    };
                }
                else if (obj is InteractableAuxiliaryObject)
                {
                    return HitGraphElement.Auxiliary;
                }
                else if (obj is InteractableAuthor)
                {
                    return HitGraphElement.Author;
                }
            }
            raycastHit = hit;
            io = null;
            return HitGraphElement.None;
        }

        /// <summary>
        /// Raycasts against <see cref="InteractableAuxiliaryObject"/>s and outputs either the closest hit
        /// or null, if no such hit exists.
        /// </summary>
        /// <param name="raycastHit">The raycast hit for the hit interactable object or the default value.</param>
        /// <param name="io">The hit object or null.</param>
        /// <param name="requireInteractable">If true, raycasts using <see cref="Layers.InteractableAuxiliaryObjectsLayerMask"/>,
        /// else <see cref="Layers.AuxiliaryObjectsLayerMask"/>.
        /// Passing false usually excludes objects outside their portal.</param>
        /// <param name="maxDistance">The maximum distance to raycast, defaults to <see cref="InteractionRadius"/>.</param>
        /// <returns>True, if a hit exists, false otherwise.</returns>
        public static bool RaycastInteractableAuxiliaryObject(
                out RaycastHit raycastHit,
                out InteractableAuxiliaryObject io,
                bool requireInteractable = true,
                float maxDistance = InteractionRadius)
        {
            int layer = requireInteractable ? Layers.InteractableAuxiliaryObjectsLayerMask : Layers.AuxiliaryObjectsLayerMask;
            if (RaycastInteractableObjectBase(out RaycastHit hit, out InteractableObjectBase obj, layer, maxDistance)
                    && obj is InteractableAuxiliaryObject)
            {
                raycastHit = hit;
                io = (InteractableAuxiliaryObject)obj;
                return true;
            }
            raycastHit = hit;
            io = null;
            return false;
        }

        /// <summary>
        /// Raycasts against <see cref="InteractableObjectBase"/>s and outputs either the closest hit
        /// or null, if no such hit exists.
        /// </summary>
        /// <param name="raycastHit">The raycast hit for the hit interactable object or the default value.</param>
        /// <param name="io">The hit object or null.</param>
        /// <param name="requireInteractable">If true, raycasts using <see cref="Layers.InteractableObjectsLayerMask"/>,
        /// else <see cref="Layers.NonInteractableObjectsLayerMask"/>.
        /// Passing false usually excludes objects outside their portal.</param>
        /// <param name="maxDistance">The maximum distance to raycast, defaults to <see cref="InteractionRadius"/>.</param>
        /// <returns>True, if a hit exists, false otherwise.</returns>
        public static bool RaycastInteractableObjectBase(
                out RaycastHit raycastHit,
                out InteractableObjectBase io,
                bool requireInteractable = true,
                float maxDistance = InteractionRadius)
        {
            int layerMask = requireInteractable ? Layers.InteractableObjectsLayerMask : Layers.AnyInteractableObjectsLayerMask;
            return RaycastInteractableObjectBase(out raycastHit, out io, layerMask, maxDistance);
        }

        /// <summary>
        /// Raycasts against <see cref="InteractableObjectBase"/>s and outputs either the closest hit
        /// or null, if no such hit exists.
        /// </summary>
        /// <param name="raycastHit">The raycast hit for the hit interactable object or the default value.</param>
        /// <param name="io">The hit object or null.</param>
        /// <param name="layerMask">
        /// The layer mask to raycast against, defaults to <see cref="Layers.InteractableObjectsLayerMask"/>.</param>
        /// <param name="maxDistance">The maximum distance to raycast, defaults to <see cref="InteractionRadius"/>.</param>
        /// <returns>True, if a hit exists, false otherwise.</returns>
        public static bool RaycastInteractableObjectBase(
                out RaycastHit raycastHit,
                out InteractableObjectBase io,
                int? layerMask,
                float maxDistance = InteractionRadius)
        {
            layerMask ??= Layers.InteractableObjectsLayerMask;

            raycastHit = new RaycastHit();
            io = null;

            if (IsMouseOverGUI())
            {
                return false;
            }

            if (Physics.Raycast(UserPointsTo(), out RaycastHit hit, maxDistance, layerMask.Value))
            {
                raycastHit = hit;
                if (hit.transform.TryGetComponent(out InteractableObjectBase obj))
                {
                    io = obj;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Whether the mouse currently hovers over a GUI element.
        /// <para>
        /// Note: If no <see cref="EventSystem"/> exists in the scene, internal calls will fail
        /// and false will be returned.
        /// </para><para>
        /// Note: If <see cref="SceneSettings.InputType"/> is not <see cref="PlayerInputType.DesktopPlayer"/>, this method will always return false.
        /// </para>
        /// </summary>
        /// <returns>Whether the mouse currently hovers over a GUI element.</returns>
        public static bool IsMouseOverGUI()
        {
            if (User.UserSettings.Instance.InputType != PlayerInputType.DesktopPlayer)
            {
                return false;
            }
            InputSystemUIInputModule inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (inputModule == null)
            {
                Debug.LogError("Could not find input system UI module! Falling back to old detection for now.\n");
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
            else
            {
                Assert.IsNotNull(Mouse.current);
                GameObject lastGameObject = inputModule.GetLastRaycastResult(Mouse.current.deviceId).gameObject;
                /// Prevent wrong IsMouseOverGUI() state when a newly created child object (e.g.,
                /// a ripple from ButtonManagerBasicIcon with useRipple enabled) does not have
                /// the UI layer. In this case, also check parent objects for the UI layer or UI canvas name.
                return lastGameObject != null && !ignoredUINames.Contains(lastGameObject.name)
                    && (lastGameObject.layer == uiLayer
                        || lastGameObject.HasParentWithLayer(uiLayer)
                        || lastGameObject.FindParentWithName(UICanvas.Canvas.name) != null);
            }
        }

        /// <summary>
        /// Raycasts against the clipping plane.
        /// </summary>
        /// <param name="clippingPlane">The plane, which defines the clipping area.</param>
        /// <param name="hit">Whether the clipping plane was hit.</param>
        /// <param name="hitInsideClippingArea">Whether the clipping plane was hit inside of its clipping area.</param>
        /// <param name="hitPointOnPlane">The hit position on the plane or <see cref="Vector3.zero"/>, if the plane was not hit.</param>
        public static void RaycastClippingPlane(
                GO.Plane clippingPlane,
                out bool hit,
                out bool hitInsideClippingArea,
                out Vector3 hitPointOnPlane)
        {
            Ray ray = UserPointsTo();
            UnityEngine.Plane raycastPlane = new(Vector3.up, clippingPlane.transform.position);
            hit = raycastPlane.Raycast(ray, out float enter);
            if (hit)
            {
                hitPointOnPlane = ray.GetPoint(enter);
                MathExtensions.TestPointAABB(
                    hitPointOnPlane.XZ(),
                    clippingPlane.LeftFrontCorner,
                    clippingPlane.RightBackCorner,
                    out float distanceFromPoint,
                    out _
                );
                hitInsideClippingArea = distanceFromPoint < 0.0f;
            }
            else
            {
                hitInsideClippingArea = false;
                hitPointOnPlane = Vector3.zero;
            }
        }

        /// <summary>
        /// Raycasts against the given <paramref name="plane"/>. If the plane is hit, <paramref name="hit"/>
        /// contains the co-ordinates of the location where the raycast hit the <paramref name="plane"/>
        /// and true is returned. If the plane is not hit, false is returned and <paramref name="hit"/>
        /// will be <see cref="Vector3.positiveInfinity"/>.
        /// </summary>
        /// <param name="plane">The plane to raycast against.</param>
        /// <param name="hit">The hit point on the plane or <see cref="Vector3.positiveInfinity"/>,
        /// if ray and plane are parallel.</param>
        /// <returns>Whether the plane was hit.</returns>
        public static bool RaycastPlane(UnityEngine.Plane plane, out Vector3 hit)
        {
            Ray ray = UserPointsTo();
            bool result = plane.Raycast(ray, out float enter);
            hit = result ? ray.GetPoint(enter) : Vector3.positiveInfinity;
            return result;
        }

        /// <summary>
        /// A ray from the user's pointing device (mouse in a desktop environment,
        /// controller in VR).
        /// </summary>
        /// <returns>Ray from the user's mouse.</returns>
        public static Ray UserPointsTo()
        {
            Camera mainCamera = MainCamera.Camera;
            Vector3 screenPoint;
            if (User.UserSettings.IsVR)
            {
                XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit);
                screenPoint = mainCamera.WorldToScreenPoint(hit.point);
            }
            else
            {
                screenPoint = Input.mousePosition;
            }

            return mainCamera != null
                ? mainCamera.ScreenPointToRay(screenPoint)
                : new Ray(origin: Vector3.zero, direction: Vector3.zero);
        }
    }
}
