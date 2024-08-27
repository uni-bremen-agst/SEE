using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
            return CurrentState == IReversibleAction.Progress.Completed;
        }

        /// <summary>
        /// Starts this <see cref="ResizeNodeAction"/>.
        /// </summary>
        public override void Start()
        {
            base.Start();

            InteractableObject.MultiSelectionAllowed = false;
            if (InteractableObject.SelectedObjects.Count > 1)
            {
                InteractableObject.UnselectAll(true);
            }
            InteractableObject.LocalAnySelectIn += OnSelectionChanged;
            InteractableObject.LocalAnySelectOut += OnSelectionChanged;
        }

        /// <summary>
        /// Stops this <see cref="ResizeNodeAction"/> without completing it.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if (memento.GameObject == null)
            {
                return;
            }

            if (gizmo != null)
            {
                gizmo.OnSizeChanged -= OnResizeStep;
                GameObject.Destroy(gizmo);
            }

            if (CurrentState == IReversibleAction.Progress.NoEffect)
            {
                memento = new Memento();
            }

            InteractableObject.LocalAnySelectIn -= OnSelectionChanged;
            InteractableObject.LocalAnySelectOut -= OnSelectionChanged;
            InteractableObject.MultiSelectionAllowed = true;
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

            memento.GameObject.transform.localScale = memento.OriginalLocalScale;
            memento.GameObject.transform.position = memento.OriginalPosition;
            // FIXME update edges
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

            memento.GameObject.transform.localScale = memento.NewLocalScale;
            memento.GameObject.transform.position = memento.NewPosition;
            // FIXME update edges
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
            memento = new Memento(go);
        }

        /// <summary>
        /// Event handler that is called every time the node selection in <see cref="InteractableObject"/> changes.
        /// </summary>
        private void OnSelectionChanged(InteractableObject interactableObject)
        {
            // FIXME Interactable object is deselected when a handle is clicked, so we cannot stop here…
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
                    CurrentState = IReversibleAction.Progress.Completed;
                }
                return;
            }

            // Incompatible type
            GameObject selectedGameObject = interactableObject.gameObject;
            if (!selectedGameObject.TryGetNodeRef(out NodeRef selectedNodeRef) || !selectedGameObject.ContainingCity().NodeTypes[selectedNodeRef.Value.Type].AllowManualResize)
            {
                return;
            }

            // Start resizing
            memento = new Memento(selectedGameObject);
            gizmo = memento.GameObject.AddComponent<ResizeGizmo>();
            gizmo.OnSizeChanged += OnResizeStep;
        }

        /// <summary>
        /// Event handler that is called by <see cref="ResizeGizmo"/> each time a resize step is finished.
        /// <para>
        /// One resize step is finished each time a handle is released. This, however, does not finish the
        /// <see cref="ResizeNodeAction"/>.
        /// </para>
        /// </summary>
        private void OnResizeStep(Vector3 newLocalScale, Vector3 newPosition)
        {
            CurrentState = IReversibleAction.Progress.InProgress;

            memento.NewLocalScale = newLocalScale;
            memento.NewPosition = newPosition;
            // FIXME update edges
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
            /// The original world position of the game object.
            /// </summary>
            public readonly Vector3 OriginalPosition;

            /// <summary>
            /// The original scale of the game object.
            /// </summary>
            public readonly Vector3 OriginalLocalScale;

            /// <summary>
            /// The new world position of the game object.
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


        private class ResizeGizmo : MonoBehaviour
        {
            /// <summary>
            /// The node reference.
            /// </summary>
            private NodeRef nodeRef;
            /// <summary>
            /// The parent node.
            /// </summary>
            private Node parent;
            /// <summary>
            /// Stores the directional vectors that belong to the individual handles.
            /// </summary>
            private Dictionary<GameObject, Vector3> handles;

            /// <summary>
            /// The size of the handles.
            /// </summary>
            private static readonly Vector3 handleScale = new (0.02f, 0.02f, 0.02f);
            /// <summary>
            /// The color of the handles.
            /// </summary>
            private Color handleColor = Color.cyan;

            /// <summary>
            /// Used to remember if a click was detected in an earlier frame.
            /// </summary>
            private bool clicked = false;

            /// <summary>
            /// The data of the resize step that is in action.
            /// </summary>
            private ResizeStepData? currentResizeStep;

            #region Change Event

            /// <summary>
            /// Delegate for signalling change.
            /// </summary>
            public delegate void ResizeNodeEvent(Vector3 newLocalScale, Vector3 newPosition);

            /// <summary>
            /// The event is emitted each time a resize step has finished.
            /// </summary>
            public event ResizeNodeEvent OnSizeChanged;

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
                parent = nodeRef.Value.Parent;
                Assert.IsNotNull(parent);
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
                    Destroy(handle);
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
                    currentResizeStep = null;
                    OnSizeChanged(transform.localScale, transform.position);
                }

                if (newClick)
                {
                    StartResizing();
                }

                if (currentResizeStep != null)
                {
                    UpdateSize();
                }
            }

            /// <summary>
            /// Initialized the resize handles and stores them in <see cref="handles"/>
            /// together with the direction vector.
            /// </summary>
            private void InitHandles()
            {
                handles = new Dictionary<GameObject, Vector3>();

                Vector3[] directions = new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
                                               Vector3.right + Vector3.forward, Vector3.right + Vector3.back,
                                               Vector3.left + Vector3.forward, Vector3.left + Vector3.back };
                foreach (Vector3 direction in directions)
                {
                    handles[CreateHandle(direction)] = direction;
                }
            }

            /// <summary>
            /// Creates a resize handle game object at the appropriate position.
            /// </summary>
            /// <param name="direction">The direction for which the handle is created.</param>
            private GameObject CreateHandle(Vector3 direction)
            {
                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.GetComponent<Renderer>().material.color = handleColor;
                handle.transform.localScale = handleScale;
                handle.transform.localPosition = transform.position + 0.5f * Vector3.Scale(transform.lossyScale, direction);
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

                currentResizeStep = new (hit.point, resizeDirection.Value, transform.position, transform.localScale, transform.lossyScale);
            }

            /// <summary>
            /// Does the actual resizing (scaling and positioning).
            /// </summary>
            void UpdateSize()
            {
                Raycasting.RaycastLowestNode(out RaycastHit? targetObjectHit, out Node hitNode, nodeRef);
                if (!targetObjectHit.HasValue || !hitNode.IsDescendantOf(parent))
                {
                    return;
                }
                Vector3 hitPoint = targetObjectHit.Value.point;

                Vector3 lastScale = transform.localScale;
                Vector3 lastPosition = transform.position;

                Vector3 cursorMovement = Vector3.Scale(currentResizeStep.Value.InitialHitPoint - hitPoint, currentResizeStep.Value.Direction);
                transform.localScale = currentResizeStep.Value.InitialLocalScale - Vector3.Scale(currentResizeStep.Value.ScaleFactor, cursorMovement);
                transform.position = currentResizeStep.Value.InitialPosition - 0.5f * Vector3.Scale(cursorMovement, currentResizeStep.Value.Direction);

                if (gameObject.OverlapsWithSiblings())
                {
                    transform.localScale = lastScale;
                    transform.position = lastPosition;
                    return;
                }

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
                public readonly Vector3 InitialPosition;
                /// <summary>
                /// The local scale right before the resize step is started.
                /// </summary>
                public readonly Vector3 InitialLocalScale;
                /// <summary>
                /// The factor to convert world-space to local coordinates for the resize step.
                /// </summary>
                public readonly Vector3 ScaleFactor;

                /// <summary>
                /// Initialized the struct.
                /// </summary>
                public ResizeStepData (Vector3 initialHitPoint, Vector3 direction, Vector3 initialPosition, Vector3 initialLocalScale, Vector3 initialLossyScale)
                {
                    InitialHitPoint = initialHitPoint;
                    Direction = direction;
                    InitialPosition = initialPosition;
                    InitialLocalScale = initialLocalScale;
                    ScaleFactor = new (
                        initialLocalScale.x / initialLossyScale.x,
                        initialLocalScale.y / initialLossyScale.y,
                        initialLocalScale.z / initialLossyScale.z
                    );
                }
            }
        }
    }
}
