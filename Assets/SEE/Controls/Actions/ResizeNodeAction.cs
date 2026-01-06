using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using SEE.Utils.History;
using Plane = UnityEngine.Plane;
using SEE.GO.Factories;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to resize a node.
    /// </summary>
    internal class ResizeNodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// The memento holding the information for <see cref="Undo"/> and <see cref="Redo"/>.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Reference to the resize gizmo that is attached to the game object during resize.
        /// </summary>
        private ResizeGizmo gizmo;

        /// <summary>
        /// Whether the action is finished and can be completed.
        /// </summary>
        private bool finished = false;

        #region ReversibleAction

        /// <summary>
        /// Returns a new instance of <see cref="ResizeNodeAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        internal static IReversibleAction CreateReversibleAction() => new ResizeNodeAction();

        /// <summary>
        /// Returns a new instance of <see cref="ResizeNodeAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance() => new ResizeNodeAction();

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ResizeNode"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ResizeNode;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of gameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return String.IsNullOrEmpty(memento.ID) || CurrentState == IReversibleAction.Progress.NoEffect
                ? new HashSet<string>()
                : new HashSet<string>() { memento.ID };
        }

        /// <summary
        /// See <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (finished)
            {
                CurrentState = IReversibleAction.Progress.Completed;
            }
            return CurrentState == IReversibleAction.Progress.Completed;
        }

        /// <summary>
        /// Starts this <see cref="ResizeNodeAction"/>.
        /// </summary>
        public override void Start()
        {
            base.Start();

            InteractableObject.MultiSelectionAllowed = false;
            InteractableObject.LocalAnySelectIn += OnSelectionChanged;
            InteractableObject.LocalAnySelectOut += OnSelectionChanged;
        }

        /// <summary>
        /// Stops this <see cref="ResizeNodeAction"/> without completing it.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            InteractableObject.LocalAnySelectIn -= OnSelectionChanged;
            InteractableObject.LocalAnySelectOut -= OnSelectionChanged;
            InteractableObject.MultiSelectionAllowed = true;

            if (gizmo != null)
            {
                gizmo.OnSizeChanged -= OnResizeStep;
                Destroyer.Destroy(gizmo);
            }

            if (CurrentState == IReversibleAction.Progress.NoEffect)
            {
                memento = new Memento();
            }

        }

        /// <summary>
        /// Undoes this ResizeNodeAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            GameObject resizedObj = memento.GameObject != null ?
                memento.GameObject : GraphElementIDMap.Find(memento.ID);

            if (resizedObj == null)
            {
                return;
            }

            resizedObj.NodeOperator().ResizeTo(memento.OriginalLocalScale, memento.OriginalPosition);
            new ResizeNodeNetAction(memento.ID, memento.OriginalLocalScale, memento.OriginalPosition).Execute();
        }

        /// <summary>
        /// Redoes this ResizeNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            GameObject resizedObj = memento.GameObject != null ?
                memento.GameObject : GraphElementIDMap.Find(memento.ID);

            if (resizedObj == null)
            {
                return;
            }

            resizedObj.NodeOperator().ResizeTo(memento.NewLocalScale, memento.NewPosition);
            new ResizeNodeNetAction(memento.ID, memento.NewLocalScale, memento.NewPosition).Execute();
        }

        #endregion ReversibleAction

        /// <summary>
        /// Used to execute the <see cref="ResizeNodeAction"/> from the context menu.
        /// </summary>
        /// <param name="go">The object to be resize.</param>
        /// <remarks>
        /// This method does not check if the object's type has
        /// <see cref="VisualNodeAttributes.AllowManualResize"/> flag set.
        /// </remarks>
        public void ContextMenuExecution(GameObject go)
        {
            ExecuteViaContextMenu = true;
            InteractableObject.UnselectAll(true);
            InteractableObject interactable = go.GetComponent<InteractableObject>();
            interactable.SetSelect(true, true);

            InteractableObject.MultiSelectionAllowed = false;
            InteractableObject.LocalAnySelectIn += OnSelectionChanged;
            InteractableObject.LocalAnySelectOut += OnSelectionChanged;
        }

        /// <summary>
        /// Event handler that is called every time the node selection in <see cref="InteractableObject"/> changes.
        /// </summary>
        private void OnSelectionChanged(InteractableObject interactableObject)
        {
            // Interactable object is deselected when a handle is clicked, so we cannot stop here…
            if (!interactableObject.IsSelected || InteractableObject.SelectedObjects.Count != 1)
            {
                return;
            }

            // New selection
            if (memento.GameObject != null && interactableObject.gameObject != memento.GameObject)
            {
                Stop();

                if (CurrentState == IReversibleAction.Progress.InProgress)
                {
                    finished = true;
                }
                return;
            }

            // Incompatible type
            GameObject selectedGameObject = interactableObject.gameObject;
            if (!selectedGameObject.TryGetNodeRef(out NodeRef selectedNodeRef))
            {
                return;
            }

            VisualNodeAttributes attrs = selectedGameObject.ContainingCity().NodeTypes[selectedNodeRef.Value.Type];
            if (!attrs.AllowManualResize)
            {
                return;
            }

            // Start resizing
            memento = new Memento(selectedGameObject);
            gizmo = memento.GameObject.AddOrGetComponent<ResizeGizmo>();
            gizmo.HeightResizeEnabled = attrs.AllowManualHeightResize;
            gizmo.OnSizeChanged += OnResizeStep;
        }

        /// <summary>
        /// Event handler that is called by <see cref="ResizeGizmo"/> each time a resize step is finished.
        /// <para>
        /// One resize step is finished each time a handle is released. This, however, does not finish the
        /// <see cref="ResizeNodeAction"/>.
        /// </para>
        /// </summary>
        /// <param name="newLocalScale">The new local scale after the resize step.</param>
        /// <param name="newPosition">The new world-space position after the resize step.</param>
        private void OnResizeStep(Vector3 newLocalScale, Vector3 newPosition)
        {
            CurrentState = IReversibleAction.Progress.InProgress;

            memento.NewLocalScale = newLocalScale;
            memento.NewPosition = newPosition;

            // Apply new position and scale to update edges and propagate changes to other players
            memento.GameObject.NodeOperator().ResizeTo(newLocalScale, newPosition, 0, reparentChildren: false, updateLayers: false);
            new ResizeNodeNetAction(memento.GameObject.name, newLocalScale, newPosition).Execute();
        }

        /// <summary>
        /// The metadata of a resize action that affects the scale and position of a node.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The <c>GameObject</c> of the game object.
            /// </summary>
            public readonly GameObject GameObject;

            /// <summary>
            /// The ID of the game object.
            /// </summary>
            public readonly string ID;

            /// <summary>
            /// The original world-space position of the game object.
            /// </summary>
            public readonly Vector3 OriginalPosition;

            /// <summary>
            /// The original scale of the game object.
            /// </summary>
            public readonly Vector3 OriginalLocalScale;

            /// <summary>
            /// The new world-space position of the game object.
            /// </summary>
            public Vector3 NewPosition;

            /// <summary>
            /// The new scale of the game object.
            /// </summary>
            public Vector3 NewLocalScale;

            /// <summary>
            /// Constructs a <see cref="Memento"/>.
            /// </summary>
            public Memento(GameObject go)
            {
                GameObject = go;
                ID = go.name;
                OriginalPosition = go.transform.position;
                OriginalLocalScale = go.transform.localScale;
                NewPosition = OriginalPosition;
                NewLocalScale = OriginalLocalScale;
            }
        }

        /// <summary>
        /// Added as a component to a game object, this will add handles for manual resize.
        /// <para>
        /// The <see cref="OnSizeChanged"/> event is emitted each time the user finishes a resize
        /// step. To conclude the resize process, the component should be destroyed.
        /// </para>
        /// </summary>
        private class ResizeGizmo : MonoBehaviour
        {
            /// <summary>
            /// Path to the texture for the BOTTOM-RIGHT resize handle.
            /// </summary>
            private static readonly string resizeArrowBottomRightTexture = "Textures/ResizeArrowBottomRight";

            /// <summary>
            /// Path to the texture for the RIGHT resize handle.
            /// </summary>
            private static readonly string resizeArrowRightTexture = "Textures/ResizeArrowRight";

            /// <summary>
            /// Path to the texture for the UP/DOWN resize handle.
            /// </summary>
            private static readonly string resizeArrowUpDownTexture = "Textures/ResizeArrowUpDown";

            /// <summary>
            /// The data of the resize step that is in action.
            /// </summary>
            private ResizeStepData currentResizeStep;

            /// <summary>
            /// The node reference.
            /// </summary>
            private NodeRef nodeRef;

            /// <summary>
            /// Reference to the main camera transform.
            /// </summary>
            private Transform mainCameraTransform;

            /// <summary>
            /// Stores the directional vectors that belong to the individual handles.
            /// </summary>
            private Dictionary<GameObject, Vector3> handles;

            /// <summary>
            /// Transform of the up/down handle.
            /// </summary>
            private Transform upDownHandleTransform;

            /// <summary>
            /// Used to remember if a click was detected in an earlier frame.
            /// </summary>
            private bool clicked = false;

            /// <summary>
            /// Is height resize active?
            /// </summary>
            public bool HeightResizeEnabled = false;

            #region Configurations

            /// <summary>
            /// The size of the handles.
            /// </summary>
            private static readonly Vector3 handleScale = new(0.005f, 0.005f, 0.005f);

            #endregion Configurations

            #region Change Event

            /// <summary>
            /// The event is emitted each time a resize step has finished.
            /// </summary>
            public event Action<Vector3, Vector3> OnSizeChanged;

            #endregion Change Event

            /// <summary>
            /// Initializes the <see cref="ResizeGizmo"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Thrown when the object script is attached to is not a node, i.e., has no <c>NodeRef</c>
            /// component.
            /// </exception>
            private void Start()
            {
                if (!gameObject.TryGetNodeRef(out NodeRef nodeRef))
                {
                    throw new InvalidOperationException("Can only be attached to GameObjects with NodeRef!");
                }

                this.nodeRef = nodeRef;
                mainCameraTransform = MainCamera.Camera.transform;
                InitHandles();
            }

            /// <summary>
            /// Destroys the handles.
            /// </summary>
            private void OnDestroy()
            {
                if (handles == null)
                {
                    return;
                }
                foreach (GameObject handle in handles.Keys)
                {
                    Destroyer.Destroy(handle);
                }
            }

            /// <summary>
            /// Manages the resize steps.
            /// <para>
            /// A resize step starts when a handle is clicked and it ends when the handle is released.
            /// </para><para>
            /// Calls <see cref="StartResizing"/> when a new step is detected.
            /// </para><para>
            /// During the resize step progress, each frame <see cref="UpdateSize"/> is called.
            /// </para>
            /// </summary>
            private void Update()
            {
                bool newClick = false;
                if (Input.GetMouseButton(0))
                {
                    newClick = !clicked;
                    clicked = true;
                }
                else if (clicked)
                {
                    clicked = false;
                    currentResizeStep = new();
                    OnSizeChanged?.Invoke(transform.localScale, transform.position);
                }

                if (newClick)
                {
                    StartResizing();
                }

                if (currentResizeStep.IsSet)
                {
                    UpdateSize();
                }
                UpdateUpDownHandlePosition();
            }

            /// <summary>
            /// Initializes the resize handles and stores them in <see cref="handles"/>
            /// together with the direction vector.
            /// </summary>
            private void InitHandles()
            {
                Vector3[] directions = new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
                                               Vector3.right + Vector3.forward, Vector3.right + Vector3.back,
                                               Vector3.left + Vector3.forward, Vector3.left + Vector3.back };
                Vector3 parentPosition = transform.position;
                Vector3 parentSize = gameObject.WorldSpaceSize();
                float yPos = parentPosition.y + 0.5f * parentSize.y + SpatialMetrics.PlacementOffset;
                handles = directions.ToDictionary(CreateHandle);

                if (HeightResizeEnabled)
                {
                    GameObject upDownHandle = CreateHandle(Vector3.up);
                    upDownHandleTransform = upDownHandle.transform;
                    handles[upDownHandle] = Vector3.up;
                }

                /// <summary>
                /// Creates a resize handle game object at the appropriate position.
                /// </summary>
                /// <param name="direction">The direction for which the handle is created.</param>
                /// <returns>The handle game object.</returns>
                GameObject CreateHandle(Vector3 direction)
                {
                    GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    handle.transform.localScale = handleScale;

                    Material material;
                    if (direction == Vector3.up)
                    {
                        handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        Texture texture = Resources.Load<Texture2D>(resizeArrowUpDownTexture);
                        material = MaterialsFactory.New(MaterialsFactory.ShaderType.Sprite, Color.white, texture);
                    }
                    else if (direction.x != 0f && direction.z != 0f)
                    {
                        if (direction.x > 0f)
                        {
                            handle.transform.localRotation = Quaternion.Euler(0f, direction.z > 0f ? 90f : 180f, 0f);
                        }
                        else
                        {
                            handle.transform.localRotation = Quaternion.Euler(0f, direction.z > 0f ? 0f : 270f, 0f);
                        }
                        Texture texture = Resources.Load<Texture2D>(resizeArrowBottomRightTexture);
                        material = MaterialsFactory.New(MaterialsFactory.ShaderType.Sprite, Color.white, texture);
                    }
                    else
                    {
                        handle.transform.localRotation = Quaternion.Euler(0f, direction.x > 0f ? 180f : 0f + direction.z * 90f, 0f);
                        Texture texture = Resources.Load<Texture2D>(resizeArrowRightTexture);
                        material = MaterialsFactory.New(MaterialsFactory.ShaderType.Sprite, Color.white, texture);
                    }
                    handle.GetComponent<Renderer>().material = material;
                    handle.transform.localPosition = new(
                            parentPosition.x + 0.5f * parentSize.x * direction.x,
                            yPos + (direction == Vector3.up ? 0.5f * handle.WorldSpaceSize().y : 0f),
                            parentPosition.z + 0.5f * parentSize.z * direction.z);

                    handle.name = $"handle__{direction.x}_{direction.y}_{direction.z}";
                    handle.transform.SetParent(transform);

                    // Contain interaction within the portal.
                    Portal.InheritPortal(from: transform.gameObject, to: handle);
                    InteractableAuxiliaryObject io = handle.AddOrGetComponent<InteractableAuxiliaryObject>();
                    io.UpdateLayer();
                    io.PartiallyInteractable = true;

                    return handle;
                }
            }

            /// <summary>
            /// Update the up/down handle's position and rotation so that it faces the camera.
            /// </summary>
            private void UpdateUpDownHandlePosition()
            {
                if (mainCameraTransform == null || upDownHandleTransform == null)
                {
                    return;
                }

                Vector3 directionToCamera = mainCameraTransform.position - transform.position;
                if (Mathf.Abs(directionToCamera.x) > Mathf.Abs(directionToCamera.z))
                {
                    directionToCamera = Vector3.Normalize(new(directionToCamera.x, 0f, 0f));
                    if (upDownHandleTransform.localRotation.eulerAngles.y == 0f)
                    {
                        upDownHandleTransform.localRotation = Quaternion.Euler(90f, 90f, 0f);
                        flipScales();
                    }
                }
                else
                {
                    directionToCamera = Vector3.Normalize(new(0f, 0f, directionToCamera.z));
                    if (upDownHandleTransform.localRotation.eulerAngles.y != 0f)
                    {
                        upDownHandleTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        flipScales();
                    }
                }

                // The height is updated during the resize process
                upDownHandleTransform.localPosition = new(
                        (0.5f + SpatialMetrics.PlacementOffset) * directionToCamera.x,
                        upDownHandleTransform.localPosition.y,
                        (0.5f + SpatialMetrics.PlacementOffset) * directionToCamera.z);


                // Swap Z and Y axes because the plane is rotated 90° on the X-axis to get it upright.
                void flipScales()
                {
                    upDownHandleTransform.localScale = new(
                            upDownHandleTransform.localScale.y,
                            upDownHandleTransform.localScale.x,
                            upDownHandleTransform.localScale.z);
                }
            }

            /// <summary>
            /// Starts a resize step if a handle is selected.
            /// <para>
            /// If the user points to a handle, the <see cref="currentResizeStep"/> attribute is set
            /// to the respective direction vector and the current transform values.
            /// </para>
            /// </summary>
            void StartResizing()
            {
                if (!Raycasting.RaycastInteractableAuxiliaryObject(out RaycastHit hit, out InteractableAuxiliaryObject io, false)
                        || !io.IsInteractable(hit.point))
                {
                    return;
                }
                GameObject hitObject = io.gameObject;
                Vector3? resizeDirection = handles.TryGetValue(hitObject, out Vector3 value) ? value : null;
                if (resizeDirection == null)
                {
                    return;
                }

                currentResizeStep = new(hit.point, resizeDirection.Value, transform);
            }

            /// <summary>
            /// Does the actual resizing (scaling and positioning).
            /// </summary>
            void UpdateSize()
            {
                // Calculate new scale and position
                Vector3 cursorMovement;
                Vector3 newCursorPosition;

                if (currentResizeStep.Up)
                {
                    Vector3 normalToCamera = upDownHandleTransform.up;
                    Plane plane = new(normalToCamera, Vector3.Scale(normalToCamera, currentResizeStep.InitialHitPoint));
                    Raycasting.RaycastPlane(plane, out Vector3 hit);
                    newCursorPosition = hit;
                }
                else
                {
                    Plane plane = new(Vector3.up, new Vector3(0, currentResizeStep.InitialHitPoint.y, 0));
                    Raycasting.RaycastPlane(plane, out Vector3 hit);
                    newCursorPosition = hit;
                }
                cursorMovement = Vector3.Scale(currentResizeStep.Direction, currentResizeStep.InitialHitPoint - newCursorPosition);

                Vector3 newLocalSize = currentResizeStep.InitialLocalSize - Vector3.Scale(currentResizeStep.LocalScaleFactor, cursorMovement);
                Vector3 newLocalPosition = currentResizeStep.InitialLocalPosition
                    - 0.5f * Vector3.Scale(currentResizeStep.LocalScaleFactor, Vector3.Scale(cursorMovement, currentResizeStep.Direction));

                // Collect children
                List<Transform> children = transform.Cast<Transform>().ToList();

                Transform parent = transform.parent;

                // The following checks only apply for 2D resize, not for changing the height.
                if (!currentResizeStep.Up)
                {
                    // Contain in parent
                    Bounds2D bounds = new(
                            newLocalPosition.x - newLocalSize.x / 2,
                            newLocalPosition.x + newLocalSize.x / 2,
                            newLocalPosition.z - newLocalSize.z / 2,
                            newLocalPosition.z + newLocalSize.z / 2
                    );
                    // Parent scale in its own context is always 1
                    Bounds2D otherBounds = new(
                            -0.5f + currentResizeStep.LocalPadding.x,
                            0.5f - currentResizeStep.LocalPadding.x,
                            -0.5f + currentResizeStep.LocalPadding.z,
                            0.5f - currentResizeStep.LocalPadding.z
                    );
                    if (currentResizeStep.Left && bounds.Left < otherBounds.Left)
                    {
                        bounds.Left = otherBounds.Left;
                    }
                    if (currentResizeStep.Right && bounds.Right > otherBounds.Right)
                    {
                        bounds.Right = otherBounds.Right;
                    }
                    if (currentResizeStep.Back && bounds.Back < otherBounds.Back)
                    {
                        bounds.Back = otherBounds.Back;
                    }
                    if (currentResizeStep.Forward && bounds.Front > otherBounds.Front)
                    {
                        bounds.Front = otherBounds.Front;
                    }

                    // Collect siblings
                    // Note: We use an initial size of the list so that the memory does not need to get
                    //       reallocated each time an item is added.
                    List<Transform> siblings = new(parent.childCount);
                    foreach (Transform sibling in parent)
                    {
                        if (sibling != transform && sibling.gameObject.IsNodeAndActiveSelf())
                        {
                            siblings.Add(sibling);
                        }
                    }

                    // Correct sibling overlap
                    foreach (Transform sibling in siblings)
                    {
                        Vector3 siblingSize = sibling.gameObject.LocalSize();
                        Vector3 siblingPos = sibling.localPosition;
                        otherBounds.Left = siblingPos.x - siblingSize.x / 2 - currentResizeStep.LocalPadding.x;
                        otherBounds.Right = siblingPos.x + siblingSize.x / 2 + currentResizeStep.LocalPadding.x;
                        otherBounds.Back = siblingPos.z - siblingSize.z / 2 - currentResizeStep.LocalPadding.z;
                        otherBounds.Front = siblingPos.z + siblingSize.z / 2 + currentResizeStep.LocalPadding.z;

                        if (bounds.Back > otherBounds.Front || bounds.Front < otherBounds.Back
                                || bounds.Left > otherBounds.Right || bounds.Right < otherBounds.Left)
                        {
                            // No overlap detected
                            continue;
                        }

                        // Calculate overlap
                        float[] overlap = { float.MaxValue, float.MaxValue };
                        if (currentResizeStep.Right)
                        {
                            overlap[0] = bounds.Right - otherBounds.Left;
                        }
                        if (currentResizeStep.Left)
                        {
                            overlap[0] = otherBounds.Right - bounds.Left;
                        }
                        if (currentResizeStep.Forward)
                        {
                            overlap[1] = bounds.Front - otherBounds.Back;
                        }
                        if (currentResizeStep.Back)
                        {
                            overlap[1] = otherBounds.Front - bounds.Back;
                        }

                        // Pick correction direction
                        if (overlap[0] < overlap[1] && newLocalSize.x - overlap[0] > currentResizeStep.MinLocalSize.x)
                        {
                            if (currentResizeStep.Right)
                            {
                                bounds.Right = SEEMath.BitDecrement(otherBounds.Left);
                            }
                            else
                            {
                                bounds.Left = SEEMath.BitIncrement(otherBounds.Right);
                            }
                        }
                        else if (newLocalSize.z - overlap[1] > currentResizeStep.MinLocalSize.z)
                        {
                            if (currentResizeStep.Forward)
                            {
                                bounds.Front = SEEMath.BitDecrement(otherBounds.Back);
                            }
                            else
                            {
                                bounds.Back = SEEMath.BitIncrement(otherBounds.Front);
                            }
                        }
                    }

                    // Contain all children
                    foreach (Transform child in children)
                    {
                        if (!child.gameObject.IsNodeAndActiveSelf())
                        {
                            continue;
                        }

                        // Child position and scale on common parent
                        Vector3 childPos = Vector3.Scale(child.localPosition, transform.localScale) + transform.localPosition;
                        Vector3 childSize = Vector3.Scale(child.gameObject.LocalSize(), transform.localScale);
                        otherBounds.Left = childPos.x - childSize.x / 2 - currentResizeStep.LocalPadding.x;
                        otherBounds.Right = childPos.x + childSize.x / 2 + currentResizeStep.LocalPadding.x;
                        otherBounds.Back = childPos.z - childSize.z / 2 - currentResizeStep.LocalPadding.z;
                        otherBounds.Front = childPos.z + childSize.z / 2 + currentResizeStep.LocalPadding.z;

                        if (currentResizeStep.Right && bounds.Right < otherBounds.Right)
                        {
                            bounds.Right = SEEMath.BitIncrement(otherBounds.Right);
                        }

                        if (currentResizeStep.Left && bounds.Left > otherBounds.Left)
                        {
                            bounds.Left = SEEMath.BitDecrement(otherBounds.Left);
                        }


                        if (currentResizeStep.Forward && bounds.Front < otherBounds.Front)
                        {
                            bounds.Front = SEEMath.BitIncrement(otherBounds.Front);
                        }

                        if (currentResizeStep.Back && bounds.Back > otherBounds.Back)
                        {
                            bounds.Back = SEEMath.BitDecrement(otherBounds.Back);
                        }
                    }

                    newLocalSize.x = bounds.Right - bounds.Left;
                    newLocalSize.z = bounds.Front - bounds.Back;
                    newLocalPosition.x = bounds.Left + newLocalSize.x / 2;
                    newLocalPosition.z = bounds.Back + newLocalSize.z / 2;
                }

                // Ensure minimal size
                if (newLocalSize.x < currentResizeStep.MinLocalSize.x)
                {
                    newLocalPosition.x += 0.5f * (currentResizeStep.MinLocalSize.x - newLocalSize.x) * currentResizeStep.Direction.x;
                    newLocalSize.x = currentResizeStep.MinLocalSize.x;
                }
                if (newLocalSize.y < currentResizeStep.MinLocalSize.y)
                {
                    // We only resize in positive direction on this axis
                    newLocalPosition.y += 0.5f * (currentResizeStep.MinLocalSize.y - newLocalSize.y);
                    newLocalSize.y = currentResizeStep.MinLocalSize.y;
                }
                if (newLocalSize.z < currentResizeStep.MinLocalSize.z)
                {
                    newLocalPosition.z += 0.5f * (currentResizeStep.MinLocalSize.z - newLocalSize.z) * currentResizeStep.Direction.z;
                    newLocalSize.z = currentResizeStep.MinLocalSize.z;
                }

                // Prevent child nodes from getting scaled
                foreach (Transform child in children)
                {
                    child.SetParent(parent);
                }
                Vector3 posDiff = 2 * (newLocalPosition - transform.localPosition);

                // Apply new scale and position
                transform.localScale = Vector3.Scale(newLocalSize, currentResizeStep.ScaleSizeFactor);
                transform.localPosition = newLocalPosition;

                // Reparent children
                foreach (Transform child in children)
                {
                    // Adapt children's position based on changed position and size
                    bool shift2D = !child.gameObject.IsNodeAndActiveSelf();
                    child.localPosition = new(
                            child.localPosition.x + (shift2D ? 0.5f * posDiff.x : 0f),
                            child.localPosition.y + posDiff.y, // we always resize height in positive direction
                            child.localPosition.z + (shift2D ? 0.5f * posDiff.z : 0f));

                    // Fix base handle position
                    if (child != upDownHandleTransform && handles.TryGetValue(child.gameObject, out Vector3 direction))
                    {
                        child.localPosition = new(
                                transform.localPosition.x + 0.5f * transform.localScale.x * direction.x,
                                child.localPosition.y,
                                transform.localPosition.z + 0.5f * transform.localScale.z * direction.z);
                    }

                    child.SetParent(transform);
                }
            }

            /// <summary>
            /// Data structure for the individual resize steps.
            /// </summary>
            private readonly struct ResizeStepData
            {
                /// <summary>
                /// Whether the struct has been explicitly initialized with values.
                /// </summary>
                public readonly bool IsSet;

                /// <summary>
                /// The initial raycast hit from which the resize step is started.
                /// </summary>
                public readonly Vector3 InitialHitPoint;

                /// <summary>
                /// The resize direction.
                /// </summary>
                public readonly Vector3 Direction;

                /// <summary>
                /// The position right before the resize step is started.
                /// </summary>
                public readonly Vector3 InitialLocalPosition;

                /// <summary>
                /// The local size right before the resize step is started.
                /// </summary>
                public readonly Vector3 InitialLocalSize;

                /// <summary>
                /// The factor to convert from local size to local scale.
                /// </summary>
                public readonly Vector3 ScaleSizeFactor;

                /// <summary>
                /// The factor to convert world-space to local coordinates in the parent's
                /// coordinate system for the resize step.
                /// </summary>
                public readonly Vector3 LocalScaleFactor;

                /// <summary>
                /// The <see cref="minSize"/> scaled by <see cref="LocalScaleFactor"/>.
                /// </summary>
                public readonly Vector3 MinLocalSize;

                /// <summary>
                /// The <see cref="padding"/> scaled by <see cref="LocalScaleFactor"/>.
                /// This is effectively the local-space padding in parent, e.g., between
                /// the resized object and its siblings.
                /// </summary>
                public readonly Vector3 LocalPadding;

                /// <summary>
                /// Does <see cref="Direction"/> point to the left?
                /// </summary>
                public readonly bool Left;

                /// <summary>
                /// Does <see cref="Direction"/> point to the right?
                /// </summary>
                public readonly bool Right;

                /// <summary>
                /// Does <see cref="Direction"/> point forward?
                /// </summary>
                public readonly bool Forward;

                /// <summary>
                /// Does <see cref="Direction"/> point backward?
                /// </summary>
                public readonly bool Back;

                /// <summary>
                /// Does <see cref="Direction"/> point upward?
                /// </summary>
                public readonly bool Up;

                /// <summary>
                /// The inverse of the screen height. Cached for performance optimization in calculations.
                /// </summary>
                public readonly float InvScreenHeight;

                /// <summary>
                /// Initializes the struct.
                /// </summary>
                public ResizeStepData(Vector3 initialHitPoint, Vector3 direction, Transform transform)
                {
                    InitialHitPoint = initialHitPoint;
                    Direction = direction;
                    InitialLocalPosition = transform.localPosition;
                    InitialLocalSize = transform.gameObject.LocalSize();
                    Vector3 localScale = transform.localScale;
                    ScaleSizeFactor = new(
                        InitialLocalSize.x / localScale.x,
                        InitialLocalSize.y / localScale.y,
                        InitialLocalSize.z / localScale.z
                    );
                    Vector3 lossyScale = transform.lossyScale;
                    LocalScaleFactor = new(
                        localScale.x / lossyScale.x,
                        localScale.y / lossyScale.y,
                        localScale.z / lossyScale.z
                    );
                    MinLocalSize = Vector3.Scale(SpatialMetrics.MinNodeSize, LocalScaleFactor);
                    LocalPadding = SpatialMetrics.Padding.x * LocalScaleFactor;
                    Left = Direction.x < 0;
                    Right = Direction.x > 0;
                    Back = Direction.z < 0;
                    Forward = Direction.z > 0;
                    Up = Direction.y > 0;
                    InvScreenHeight = 1f / Screen.height;
                    IsSet = true;
                }
            }
        }
    }
}
