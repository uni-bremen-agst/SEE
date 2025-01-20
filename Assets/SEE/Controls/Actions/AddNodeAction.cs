using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.Audio;
using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using SEE.Utils.History;
using SEE.XR;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new node for a selected city.
    /// </summary>
    internal class AddNodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// The life cycle of this add node action.
        /// </summary>
        private enum ProgressState
        {
            /// <summary>
            /// Initial state when no parent node is selected.
            /// </summary>
            NoNodeSelected,
            /// <summary>
            /// A new node is created and selected, the dialog is opened,
            /// and we wait for input.
            /// </summary>
            WaitingForInput,
            /// <summary>
            /// When the action is finished.
            /// </summary>
            Finish
        }

        /// <summary>
        /// The current state of the add node process.
        /// </summary>
        private ProgressState progress = ProgressState.NoNodeSelected;

        /// <summary>
        /// The chosen parent for the new node when executed via context menu.
        /// </summary>
        private GameObject contextMenuTargetParent;

        /// <summary>
        /// The chosen local position for the new node on its parent when executed via context menu.
        /// </summary>
        private Vector3 contextMenuTargetLocalPosition;

        /// <summary>
        /// Tolerance value for comparing localScale to minimal size threshold.
        /// <para>
        /// This is necessary to compensate for precision fluctuations in float values.
        /// </para>
        /// </summary>
        private const float tolerance = 0.0001f;

        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// this graph node is a parent to which a new node is created and added as a child.
        /// <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            switch (progress)
            {
                case ProgressState.NoNodeSelected:
                    if (SceneSettings.InputType == PlayerInputType.DesktopPlayer && Input.GetMouseButtonDown(0)
                        && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
                    {
                        AddNode(raycastHit.collider.gameObject, raycastHit.transform.InverseTransformPoint(raycastHit.point));
                    }
                    else if (SceneSettings.InputType == PlayerInputType.VRPlayer && XRSEEActions.Selected && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef() &&
                        XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
                    {
                        XRSEEActions.Selected = false;
                        AddNode(raycastHit.collider.gameObject, raycastHit.transform.InverseTransformPoint(raycastHit.point));
                    }
                    else if (ExecuteViaContextMenu)
                    {
                        ExecuteViaContextMenu = false;
                        AddNode(contextMenuTargetParent, contextMenuTargetLocalPosition);
                    }
                    break;
                case ProgressState.WaitingForInput:
                    // Waiting until the dialog is closed and all input is present.
                    break;
                case ProgressState.Finish:
                    result = true;
                    CurrentState = IReversibleAction.Progress.Completed;
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NewNodeSound, addedGameNode);
                    break;
                default:
                    throw new NotImplementedException($"Unhandled case {nameof(progress)}.");
            }
            return result;
        }

        /// <summary>
        /// Adds a node on the chosen <paramref name="parent"/> at the
        /// chosen <paramref name="targetPosition"/>.
        /// </summary>
        /// <param name="parent">The parent on which to place the node.</param>
        /// <param name="targetPosition">The local position where the node should be placed.</param>
        private void AddNode(GameObject parent, Vector3 targetPosition)
        {
            Vector3 minLocalScale = SpatialMetrics.MinNodeSizeLocalScale(parent.transform);
            Vector3 localPadding = parent.transform.InverseTransformVector(SpatialMetrics.Padding);
            Bounds parentBounds3D = parent.LocalBounds();
            Bounds2D parentBounds = new(
                    parentBounds3D.min.x + localPadding.x,
                    parentBounds3D.max.x - localPadding.x,
                    parentBounds3D.min.z + localPadding.z,
                    parentBounds3D.max.z - localPadding.z);

            // Initial intended/default size
            Bounds2D bounds = new(
                    targetPosition.x - Mathf.Max(SpatialMetrics.HalfDefaultNodeLocalScale, 0.5f * minLocalScale.x),
                    targetPosition.x + Mathf.Max(SpatialMetrics.HalfDefaultNodeLocalScale, 0.5f * minLocalScale.x),
                    targetPosition.z - Mathf.Max(SpatialMetrics.HalfDefaultNodeLocalScale, 0.5f * minLocalScale.z),
                    targetPosition.z + Mathf.Max(SpatialMetrics.HalfDefaultNodeLocalScale, 0.5f * minLocalScale.z));

            List<Bounds2D> siblingBoundsList = new();
            Bounds2D potentialGrow = new(0f, 0f, 0f, 0f);

            moveInsideParentArea();
            shrink2D();
            preventOverlap();
            fillAvailableSpace();

            float localHeight = SpatialMetrics.DefaultNodeHeight * parent.transform.InverseTransformVector(Vector3.up).y;
            Vector3 scale = new(
                    bounds.Right - bounds.Left,
                    localHeight,
                    bounds.Front - bounds.Back);
            Vector3 position = new(
                bounds.Left + 0.5f * scale.x,
                parentBounds3D.max.y + localPadding.y + 0.5f * localHeight,
                bounds.Back + 0.5f * scale.z);

            squarify();

                        // Enforce minimal size
            if (scale.x + tolerance < minLocalScale.x || scale.z + tolerance < minLocalScale.z)
            {
                ShowNotification.Warn(
                        "Node Not Created",
                        "There is not enough space to create a new node at the given position.");
                return;
            }

            addedGameNode = GameNodeAdder.AddChild(parent);
            addedGameNode.transform.localScale = scale;
            addedGameNode.transform.localPosition = position;

            memento = new(child: addedGameNode, parent: parent)
            {
                NodeID = addedGameNode.name
            };
            new AddNodeNetAction(parentID: memento.Parent.name, newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();

            progress = ProgressState.WaitingForInput;
            OpenDialog(addedGameNode);

            void moveInsideParentArea()
            {
                    if (bounds.Left < parentBounds.Left)
                {
                    float diff = parentBounds.Left - bounds.Left;
                    bounds.Left = parentBounds.Left;
                    bounds.Right = Mathf.Min(bounds.Right + diff, parentBounds.Right);
                }
                if (bounds.Right > parentBounds.Right)
                {
                    float diff = bounds.Right - parentBounds.Right;
                    bounds.Right = parentBounds.Right;
                    bounds.Left = Mathf.Max(bounds.Left - diff, parentBounds.Left);
                }
                if (bounds.Back < parentBounds.Back)
                {
                    float diff = parentBounds.Back - bounds.Back;
                    bounds.Back = parentBounds.Back;
                    bounds.Front = Mathf.Min(bounds.Front + diff, parentBounds.Front);
                }
                if (bounds.Front > parentBounds.Front)
                {
                    float diff = bounds.Front - parentBounds.Front;
                    bounds.Front = parentBounds.Front;
                    bounds.Back = Mathf.Max(bounds.Back - diff, parentBounds.Back);
                }
            }

            /// <summary>
            /// Shrink by raycasting on X/Z axes.
            /// </summary>
            void shrink2D()
            {
                //
                foreach (Transform sibling in parent.transform)
                {
                    if (!sibling.gameObject.IsNodeAndActiveSelf())
                    {
                        continue;
                    }

                    Vector3 siblingSize = sibling.gameObject.LocalSize();
                    Vector3 siblingPos = sibling.localPosition;
                    Bounds2D siblingBounds = new(
                            siblingPos.x - siblingSize.x / 2 - localPadding.x,
                            siblingPos.x + siblingSize.x / 2 + localPadding.x,
                            siblingPos.z - siblingSize.z / 2 - localPadding.z,
                            siblingPos.z + siblingSize.z / 2 + localPadding.z);
                    siblingBoundsList.Add(siblingBounds);

                    if (siblingBounds.LineIntersect(new(targetPosition.x, targetPosition.z), Direction2D.Left) && bounds.Left <= siblingBounds.Right)
                    {
                        float newVal = SEEMath.BitIncrement(siblingBounds.Right);
                        potentialGrow.Right = newVal - bounds.Left;
                        potentialGrow.Left = 0f;
                        bounds.Left = newVal;
                    }
                    if (siblingBounds.LineIntersect(new(targetPosition.x, targetPosition.z), Direction2D.Right) && bounds.Right >= siblingBounds.Left)
                    {
                        float newVal =  SEEMath.BitDecrement(siblingBounds.Left);
                        potentialGrow.Left = bounds.Right - newVal;
                        potentialGrow.Right = 0f;
                        bounds.Right = newVal;
                    }
                    if (siblingBounds.LineIntersect(new(targetPosition.x, targetPosition.z), Direction2D.Back) && bounds.Back <= siblingBounds.Front)
                    {
                        float newVal = SEEMath.BitIncrement(siblingBounds.Front);
                        potentialGrow.Front = newVal - bounds.Back;
                        potentialGrow.Back = 0f;
                        bounds.Back = newVal;
                    }
                    if (siblingBounds.LineIntersect(new(targetPosition.x, targetPosition.z), Direction2D.Front) && bounds.Front >= siblingBounds.Back)
                    {
                        float newVal =  SEEMath.BitDecrement(siblingBounds.Back);
                        potentialGrow.Back = bounds.Front - newVal;
                        potentialGrow.Front = 0f;
                        bounds.Front = newVal;
                    }
                }
            }

            /// <summary>
            /// Shrink to prevent sibling overlap with siblings.
            /// </summary>
            void preventOverlap()
            {
                foreach (Bounds2D siblingBounds in siblingBoundsList.Where(bounds.HasOverlap))
                {
                    // Determine shrink direction: weight by area size
                    float area = 0f;
                    float potentialArea;
                    Direction2D direction = Direction2D.None;
                    if (bounds.Left < siblingBounds.Right)
                    {
                        float overlapLen = siblingBounds.Right - bounds.Left;
                        potentialArea = (bounds.Right - bounds.Left - overlapLen) * (bounds.Front - bounds.Back);
                        if (potentialArea > area)
                        {
                            area = potentialArea;
                            direction = Direction2D.Left;
                        }
                    }
                    if (siblingBounds.Left < bounds.Right)
                    {
                        float overlapLen = bounds.Right - siblingBounds.Left;
                        potentialArea = (bounds.Right - bounds.Left - overlapLen) * (bounds.Front - bounds.Back);
                        if (potentialArea > area)
                        {
                            area = potentialArea;
                            direction = Direction2D.Right;
                        }
                    }
                    if (bounds.Back < siblingBounds.Front)
                    {
                        float overlapLen = siblingBounds.Front - bounds.Back;
                        potentialArea = (bounds.Right - bounds.Left) * (bounds.Front - bounds.Back - overlapLen);
                        if (potentialArea > area)
                        {
                            area = potentialArea;
                            direction = Direction2D.Back;
                        }
                    }
                    if (siblingBounds.Back < bounds.Front)
                    {
                        float overlapLen = bounds.Front - siblingBounds.Back;
                        potentialArea = (bounds.Right - bounds.Left) * (bounds.Front - bounds.Back - overlapLen);
                        if (potentialArea > area)
                        {
                            direction = Direction2D.Front;
                        }
                    }

                    // Adapt bounds to prevent overlap with siblings
                    switch (direction)
                    {
                        case Direction2D.Left: {
                            float newVal = SEEMath.BitIncrement(siblingBounds.Right);
                            potentialGrow.Right += newVal - bounds.Left;
                            potentialGrow.Left = 0f;
                            bounds.Left = newVal;
                            break;
                        }
                        case Direction2D.Right: {
                            float newVal = SEEMath.BitDecrement(siblingBounds.Left);
                            potentialGrow.Left += bounds.Right - newVal;
                            potentialGrow.Right = 0f;
                            bounds.Right = newVal;
                            break;
                        }
                        case Direction2D.Back: {
                            float newVal = SEEMath.BitIncrement(siblingBounds.Front);
                            potentialGrow.Front += newVal - bounds.Back;
                            potentialGrow.Back = 0f;
                            bounds.Back = newVal;
                            break;
                        }
                        case Direction2D.Front: {
                            float newVal = SEEMath.BitDecrement(siblingBounds.Back);
                            potentialGrow.Back += bounds.Front - newVal;
                            potentialGrow.Front = 0f;
                            bounds.Front = newVal;
                            break;
                        }
                    }
                }
            }

            /// <summary>
            /// Grow to fill the available space.
            /// </summary>
            void fillAvailableSpace()
            {
                foreach (Direction2D direction in new[] {Direction2D.Left, Direction2D.Right, Direction2D.Back, Direction2D.Front})
                {
                    float oldValue;
                    switch (direction)
                    {
                        case Direction2D.Left:
                            if (Mathf.Approximately(potentialGrow.Left, 0f))
                            {
                                continue;
                            }
                            oldValue = bounds.Left;
                            bounds.Left = Mathf.Max(bounds.Left - potentialGrow.Left, parentBounds.Left);
                            break;
                        case Direction2D.Right:
                            if (Mathf.Approximately(potentialGrow.Right, 0f))
                            {
                                continue;
                            }
                            oldValue = bounds.Right;
                            bounds.Right = Mathf.Min(bounds.Right + potentialGrow.Right, parentBounds.Right);
                            break;
                        case Direction2D.Back:
                            if (Mathf.Approximately(potentialGrow.Back, 0f))
                            {
                                continue;
                            }
                            oldValue = bounds.Back;
                            bounds.Back = Mathf.Max(bounds.Back - potentialGrow.Back, parentBounds.Back);
                            break;
                        case Direction2D.Front:
                            if (Mathf.Approximately(potentialGrow.Front, 0f))
                            {
                                continue;
                            }
                            oldValue = bounds.Front;
                            bounds.Front = Mathf.Min(bounds.Front + potentialGrow.Front, parentBounds.Front);
                            break;
                        default:
                            continue;
                    }

                    foreach (Bounds2D siblingBounds in siblingBoundsList.Where(bounds.HasOverlap))
                    {
                        float newValue;
                        if (direction == Direction2D.Left && bounds.Left < siblingBounds.Right &&
                                (newValue = SEEMath.BitIncrement(siblingBounds.Right)) <= oldValue)
                        {
                            bounds.Left = newValue;
                        }
                        if (direction == Direction2D.Right && bounds.Right > siblingBounds.Left &&
                                (newValue = SEEMath.BitDecrement(siblingBounds.Left)) >= oldValue)
                        {
                            bounds.Right = newValue;
                        }
                        if (direction == Direction2D.Back && bounds.Back < siblingBounds.Front &&
                                (newValue = SEEMath.BitIncrement(siblingBounds.Front)) <= oldValue)
                        {
                            bounds.Back = newValue;
                        }
                        if (direction == Direction2D.Front && bounds.Front > siblingBounds.Back &&
                                (newValue = SEEMath.BitDecrement(siblingBounds.Back)) >= oldValue)
                        {
                            bounds.Front = newValue;
                        }
                    }
                }
            }

            /// <summary>
            /// Squarify by applying the length of the shorter side to the other axis (X/Z).
            /// The resulting square is moved as close as possible to the target position.
            /// </summary>
            void squarify()
            {
                Vector3 lossyScale = Vector3.Scale(scale, parent.transform.lossyScale);
                if (lossyScale.x < lossyScale.z)
                {
                    scale.z = lossyScale.x / parent.transform.lossyScale.z;

                    // Move close to target position
                    float newBackBound = position.z - 0.5f * scale.z;
                    float maxMovement = newBackBound - bounds.Back;
                    float targetMovement = targetPosition.z - position.z;
                    float actualMovement = Mathf.Min(maxMovement, Mathf.Abs(targetMovement));
                    if (targetMovement >= 0f)
                    {
                        position.z += actualMovement;
                    }
                    else
                    {
                        position.z -= actualMovement;
                    }
                }
                else
                {
                    scale.x = lossyScale.z / parent.transform.lossyScale.x;

                    // Move close to target position
                    float newLeftBound = position.x - 0.5f * scale.x;
                    float maxMovement = newLeftBound - bounds.Left;
                    float targetMovement = targetPosition.x - position.x;
                    float actualMovement = Mathf.Min(maxMovement, Mathf.Abs(targetMovement));
                    if (targetMovement >= 0f)
                    {
                        position.x += actualMovement;
                    }
                    else
                    {
                        position.x -= actualMovement;
                    }
                }
            }
        }

        /// <summary>
        /// Opens a dialog where the user can enter the node name and select its type.
        /// If the user presses the OK button, the SourceName and Type of
        /// <see cref="memento.node"/> will have the new values entered
        /// and <see cref="memento.Name"/> and <see cref="memento.Type"/>
        /// will be set to memorize these and <see cref="progress"/> is
        /// moved forward to <see cref="ProgressState.ValuesAreGiven"/>.
        /// If the user presses the Cancel button, the node will be created as
        /// an unnamed node with the unkown type.
        /// </summary>
        /// <param name="go">New node.</param>
        private void OpenDialog(GameObject go)
        {
            Node node = go.GetNode();
            NodePropertyDialog dialog = new(node);
            dialog.OnConfirm.AddListener(OnConfirm);
            dialog.OnCancel.AddListener(OnCancel);
            dialog.Open(true);
            SEEInput.KeyboardShortcutsEnabled = false;

            void OnConfirm()
            {
                memento.Name = node.SourceName;
                memento.Type = node.Type;
                new EditNodeNetAction(node.ID, node.SourceName, node.Type).Execute();
                InteractableObject.UnselectAll(true);
                progress = ProgressState.Finish;
                SEEInput.KeyboardShortcutsEnabled = true;
            }

            void OnCancel()
            {
                // New node discarded
                Destroyer.Destroy(go);
                new DeleteNetAction(go.name).Execute();
                progress = ProgressState.NoNodeSelected;
                SEEInput.KeyboardShortcutsEnabled = true;
            }
        }

        /// <summary>
        /// Used to execute the <see cref="AddNodeAction"/> from the context menu.
        /// Calls <see cref="AddNode"/> and ensures that the <see cref="Update"/> method
        /// performs the execution via context menu.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="position">The world-space position where the node should be placed on parent.</param>
        public void ContextMenuExecution(GameObject parent, Vector3 position)
        {
            contextMenuTargetParent = parent;
            contextMenuTargetLocalPosition = parent.transform.InverseTransformPoint(position);
            ExecuteViaContextMenu = true;
        }

        /// <summary>
        /// The node that was added when this action was executed. It is saved so
        /// that it can be removed on <see cref="Undo"/>.
        /// </summary>
        private GameObject addedGameNode;

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to re-add a node whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The parent of the new node.
            /// </summary>
            public readonly GameObject Parent;
            /// <summary>
            /// The position of the new node in world space.
            /// </summary>
            public readonly Vector3 Position;
            /// <summary>
            /// The scale of the new node in world space.
            /// </summary>
            public readonly Vector3 Scale;
            /// <summary>
            /// The node ID for the added node. It must be kept to re-use the
            /// original name of the node in Redo().
            /// </summary>
            public string NodeID;
            /// <summary>
            /// The chosen name for the added node.
            /// </summary>
            public string Name;
            /// <summary>
            /// The chosen type for the added node.
            /// </summary>
            public string Type;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="child">child that was added</param>
            /// <param name="parent">parent of <paramref name="child"/></param>
            public Memento(GameObject child, GameObject parent)
            {
                Parent = parent;
                Position = child.transform.position;
                Scale = child.transform.lossyScale;
                NodeID = null;
                Name = string.Empty;
                Type = string.Empty;
            }
        }

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (addedGameNode != null)
            {
                GameElementDeleter.RemoveNodeFromGraph(addedGameNode);
                new DeleteNetAction(addedGameNode.name).Execute();
                Destroyer.Destroy(addedGameNode);
                addedGameNode = null;
            }
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedGameNode = GameNodeAdder.AddChild(memento.Parent, worldSpacePosition: memento.Position,
                                                   worldSpaceScale: memento.Scale, nodeID: memento.NodeID);
            if (addedGameNode != null)
            {
                new AddNodeNetAction(parentID: memento.Parent.name,
                    newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();

                if (!string.IsNullOrEmpty(memento.Type))
                {
                    Node node = addedGameNode.GetNode();
                    GameNodeEditor.ChangeName(node, memento.Name);
                    GameNodeEditor.ChangeType(node, memento.Type);
                    new EditNodeNetAction(node.ID, node.SourceName, node.Type).Execute();
                }
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new AddNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.NewNode;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.Parent.name,
                memento.NodeID
            };
        }
    }
}
