using System;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using SEE.Utils.History;

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
        /// <returns>new instance</returns>
        internal static IReversibleAction CreateReversibleAction() => new ResizeNodeAction();

        /// <summary>
        /// Returns a new instance of <see cref="ResizeNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance() => new ResizeNodeAction();

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ResizeNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ResizeNode;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return memento.GameObject == null || CurrentState == IReversibleAction.Progress.NoEffect
                ? new HashSet<string>()
                : new HashSet<string>() { memento.GameObject.name };
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

            if (memento.GameObject == null)
            {
                return;
            }

            memento.GameObject.NodeOperator().ResizeTo(memento.OriginalLocalScale, memento.OriginalPosition);
            new ResizeNodeNetAction(memento.GameObject.name, memento.OriginalLocalScale, memento.OriginalPosition).Execute();
        }

        /// <summary>
        /// Redoes this ResizeNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            if (memento.GameObject == null)
            {
                return;
            }

            memento.GameObject.NodeOperator().ResizeTo(memento.NewLocalScale, memento.NewPosition);
            new ResizeNodeNetAction(memento.GameObject.name, memento.NewLocalScale, memento.NewPosition).Execute();
        }

        #endregion ReversibleAction

        /// <summary>
        /// Used to execute the <see cref="ResizeNodeAction"/> from the context menu.
        /// </summary>
        /// <param name="go">The object to be resize</param>
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
            if (!selectedGameObject.TryGetNodeRef(out NodeRef selectedNodeRef)
                || !selectedGameObject.ContainingCity().NodeTypes[selectedNodeRef.Value.Type].AllowManualResize)
            {
                return;
            }

            // Start resizing
            memento = new Memento(selectedGameObject);
            gizmo = memento.GameObject.AddOrGetComponent<ResizeGizmo>();
            gizmo.OnSizeChanged += OnResizeStep;
        }

        /// <summary>
        /// Event handler that is called by <see cref="ResizeGizmo"/> each time a resize step is finished.
        /// <para>
        /// One resize step is finished each time a handle is released. This, however, does not finish the
        /// <see cref="ResizeNodeAction"/>.
        /// </para>
        /// </summary>
        /// <param name="newLocalScale">The new local scale after the resize step</param>
        /// <param name="newPosition">The new world-space position after the resize step</param>
        private void OnResizeStep(Vector3 newLocalScale, Vector3 newPosition)
        {
            CurrentState = IReversibleAction.Progress.InProgress;

            memento.NewLocalScale = newLocalScale;
            memento.NewPosition = newPosition;

            // Apply new position and scale to update edges and propagate changes to other players
            memento.GameObject.NodeOperator().ResizeTo(newLocalScale, newPosition, 0, reparentChildren: false);
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
            /// The data of the resize step that is in action.
            /// </summary>
            private ResizeStepData currentResizeStep;

            /// <summary>
            /// The node reference.
            /// </summary>
            private NodeRef nodeRef;

            /// <summary>
            /// Stores the directional vectors that belong to the individual handles.
            /// </summary>
            private Dictionary<GameObject, Vector3> handles;

            /// <summary>
            /// Used to remember if a click was detected in an earlier frame.
            /// </summary>
            private bool clicked = false;

            #region Configurations

            /// <summary>
            /// The minimal size of a node in world space.
            /// </summary>
            private static readonly Vector3 minSize = new (0.04f, 0.001f, 0.04f);

            /// <summary>
            /// The minimal world-space distance between nodes while resizing.
            /// </summary>
            private const float padding = 0.004f;

            /// <summary>
            /// A small offset is used as a difference between the detection and the set value
            /// to prevent instant re-detection.
            /// </summary>
            private const float detectionOffset = 0.0001f;

            /// <summary>
            /// The size of the handles.
            /// </summary>
            private static readonly Vector3 handleScale = new(0.02f, 0.02f, 0.02f);

            /// <summary>
            /// The color of the handles.
            /// </summary>
            private static Color handleColor = Color.cyan;

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
            }

            /// <summary>
            /// Initializes the resize handles and stores them in <see cref="handles"/>
            /// together with the direction vector.
            /// </summary>
            private void InitHandles()
            {
                handles = new Dictionary<GameObject, Vector3>();

                Vector3[] directions = new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
                                               Vector3.right + Vector3.forward, Vector3.right + Vector3.back,
                                               Vector3.left + Vector3.forward, Vector3.left + Vector3.back,
                                               Vector3.up };
                Vector3 position = transform.position;
                Vector3 size = gameObject.WorldSpaceSize();
                foreach (Vector3 direction in directions)
                {
                    handles[CreateHandle(direction, position, size)] = direction;
                }
            }

            /// <summary>
            /// Creates a resize handle game object at the appropriate position.
            /// </summary>
            /// <param name="direction">The direction for which the handle is created.</param>
            /// <param name="parentWorldPosition">The cached parent world-space position.</param>
            /// <param name="parentWorldScale">The cached parent world-space scale.</param>
            private GameObject CreateHandle(Vector3 direction, Vector3 parentWorldPosition, Vector3 parentWorldScale)
            {
                // Move base handles down
                if (direction.y == 0f)
                {
                    direction.y = -1f;
                }

                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.GetComponent<Renderer>().material.color = handleColor;
                handle.transform.localScale = handleScale;
                handle.transform.localPosition = parentWorldPosition + 0.5f * Vector3.Scale(parentWorldScale, direction);
                handle.name = $"handle__{direction.x}_{direction.y}_{direction.z}";
                handle.transform.SetParent(transform);
                return handle;
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
                if (!Raycasting.RaycastAnything(out RaycastHit hit))
                {
                    return;
                }
                Vector3? resizeDirection = handles.TryGetValue(hit.collider.gameObject, out Vector3 value) ? value : null;
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
                Raycasting.RaycastLowestNode(out RaycastHit? targetObjectHit, out _, nodeRef);
                if (!targetObjectHit.HasValue)
                {
                    return;
                }

                // Collect siblings
                Transform parent = transform.parent;
                // We use an initial size of the list so that the memory does not need to get
                // reallocated each time an item is added.
                List<Transform> siblings = new(parent.childCount);
                foreach (Transform sibling in parent)
                {
                    if (sibling != transform && sibling.gameObject.IsNodeAndActiveSelf())
                    {
                        siblings.Add(sibling);
                    }
                }

                // Collect children
                List<Transform> children = new(transform.childCount);
                foreach (Transform child in transform)
                {
                    if (child.gameObject.IsNodeAndActiveSelf())
                    {
                        children.Add(child);
                    }
                }

                // Calculate new scale and position
                Vector3 hitPoint = targetObjectHit.Value.point;
                Vector3 cursorMovement = currentResizeStep.Up
                        ? new(0f, (currentResizeStep.InitialMousePosition.y - Input.mousePosition.y) / Screen.height, 0f)
                        : Vector3.Scale(currentResizeStep.InitialHitPoint - hitPoint, currentResizeStep.Direction);
                Vector3 newLocalSize = currentResizeStep.InitialLocalSize - Vector3.Scale(currentResizeStep.LocalScaleFactor, cursorMovement);
                Vector3 newLocalPosition = currentResizeStep.InitialLocalPosition
                    - 0.5f * Vector3.Scale(currentResizeStep.LocalScaleFactor, Vector3.Scale(cursorMovement, currentResizeStep.Direction));

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

                // Correct sibling overlap
                foreach (Transform sibling in siblings)
                {
                    Vector3 siblingSize = sibling.gameObject.LocalSize();
                    Vector3 siblingPos = sibling.localPosition;
                    otherBounds.Left  = siblingPos.x - siblingSize.x / 2 - currentResizeStep.LocalPadding.x + detectionOffset;
                    otherBounds.Right = siblingPos.x + siblingSize.x / 2 + currentResizeStep.LocalPadding.x - detectionOffset;
                    otherBounds.Back  = siblingPos.z - siblingSize.z / 2 - currentResizeStep.LocalPadding.z + detectionOffset;
                    otherBounds.Front = siblingPos.z + siblingSize.z / 2 + currentResizeStep.LocalPadding.z - detectionOffset;

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
                            bounds.Right = otherBounds.Left - detectionOffset;
                        }
                        else
                        {
                            bounds.Left = otherBounds.Right + detectionOffset;
                        }
                    }
                    else if (newLocalSize.z - overlap[1] > currentResizeStep.MinLocalSize.z)
                    {
                        if (currentResizeStep.Forward)
                        {
                            bounds.Front = otherBounds.Back - detectionOffset;
                        }
                        else
                        {
                            bounds.Back = otherBounds.Front + detectionOffset;
                        }
                    }
                }

                // Contain all children
                foreach (Transform child in children)
                {
                    // Child position and scale on common parent
                    Vector3 childPos = Vector3.Scale(child.localPosition, transform.localScale) + transform.localPosition;
                    Vector3 childSize = Vector3.Scale(child.gameObject.LocalSize(), transform.localScale);
                    otherBounds.Left  = childPos.x - childSize.x / 2 - currentResizeStep.LocalPadding.x + detectionOffset;
                    otherBounds.Right = childPos.x + childSize.x / 2 + currentResizeStep.LocalPadding.x - detectionOffset;
                    otherBounds.Back  = childPos.z - childSize.z / 2 - currentResizeStep.LocalPadding.z + detectionOffset;
                    otherBounds.Front = childPos.z + childSize.z / 2 + currentResizeStep.LocalPadding.z - detectionOffset;

                    if (currentResizeStep.Right && bounds.Right < otherBounds.Right)
                    {
                        bounds.Right = otherBounds.Right + detectionOffset;
                    }

                    if (currentResizeStep.Left && bounds.Left > otherBounds.Left)
                    {
                        bounds.Left = otherBounds.Left - detectionOffset;
                    }


                    if (currentResizeStep.Forward && bounds.Front < otherBounds.Front)
                    {
                        bounds.Front = otherBounds.Front + detectionOffset;
                    }

                    if (currentResizeStep.Back && bounds.Back > otherBounds.Back)
                    {
                        bounds.Back = otherBounds.Back - detectionOffset;
                    }
                }

                newLocalSize.x = bounds.Right - bounds.Left;
                newLocalSize.z = bounds.Front - bounds.Back;
                newLocalPosition.x = bounds.Left + newLocalSize.x / 2;
                newLocalPosition.z = bounds.Back + newLocalSize.z / 2;

                // Ensure minimal size
                if (newLocalSize.x < currentResizeStep.MinLocalSize.x
                        || newLocalSize.y < currentResizeStep.MinLocalSize.y
                        || newLocalSize.z < currentResizeStep.MinLocalSize.z)
                {
                    return;
                }

                // Prevent child nodes from getting scaled
                Transform tempParent = transform.parent;
                foreach (Transform childNode in children)
                {
                    childNode.SetParent(tempParent);
                }

                // Apply new scale and position
                transform.localScale = Vector3.Scale(newLocalSize, currentResizeStep.ScaleSizeFactor);
                transform.localPosition = newLocalPosition;

                // Reparent children
                foreach (Transform child in children)
                {
                    child.SetParent(transform);
                }

                // Restore handle scale
                foreach (GameObject handle in handles.Keys)
                {
                    handle.transform.SetParent(null);
                    handle.transform.localScale = handleScale;
                    handle.transform.SetParent(transform);
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
                /// The initial mouse position.
                /// </summary>
                public readonly Vector2 InitialMousePosition;

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
                /// Initializes the struct.
                /// </summary>
                public ResizeStepData (Vector3 initialHitPoint, Vector3 direction, Transform transform)
                {
                    InitialHitPoint = initialHitPoint;
                    InitialMousePosition = Input.mousePosition;
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
                    IsSet = true;
                    MinLocalSize = Vector3.Scale(minSize, LocalScaleFactor);
                    LocalPadding = padding * LocalScaleFactor;
                    Left    = Direction.x < 0;
                    Right   = Direction.x > 0;
                    Back    = Direction.z < 0;
                    Forward = Direction.z > 0;
                    Up      = Direction.y > 0;
                }
            }

            /// <summary>
            /// Data structure for 2-dimensional bounds.
            /// </summary>
            private struct Bounds2D
            {
                /// <summary>
                /// The left side of the area.
                /// </summary>
                public float Left;
                /// <summary>
                /// The right side of the area.
                /// </summary>
                public float Right;
                /// <summary>
                /// The back side of the area.
                /// </summary>
                public float Back;
                /// <summary>
                /// The front side of the area.
                /// </summary>
                public float Front;

                /// <summary>
                /// Initializes the struct.
                /// </summary>
                public Bounds2D (float left, float right, float back, float front)
                {
                    Left = left;
                    Right = right;
                    Back = back;
                    Front = front;
                }

                /// <summary>
                /// Implicit conversion to string.
                /// <summary>
                public static implicit operator string(Bounds2D bounds)
                {
                    return bounds.ToString();
                }

                /// <summary>
                /// Returns a printable string with the struct's values.
                /// <summary>
                public override readonly string ToString()
                {
                    return $"{nameof(Bounds2D)}(Left: {Left}, Right: {Right}, Bottom: {Back}, Top: {Front})";
                }
            }
        }
    }
}
