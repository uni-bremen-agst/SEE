using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;
using MoveNetAction = SEE.Net.Actions.Drawable.MoveNetAction;

namespace SEE.Controls.Actions.Drawable.MoveRotate
{
    /// <summary>
    /// Moves or rotate a drawable type object.
    /// </summary>
    public class MoveRotateAction : DrawableAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="MoveRotateAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The selected drawable type object.
            /// </summary>
            public GameObject SelectedObject;
            /// <summary>
            /// The drawable surface where the selected object is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The ID of the selected object.
            /// </summary>
            public readonly string ID;
            /// <summary>
            /// The old position of the selected object.
            /// </summary>
            public readonly Vector3 OldObjectPosition;
            /// <summary>
            /// The new position of the selected object.
            /// </summary>
            public readonly Vector3 NewObjectPosition;
            /// <summary>
            /// The old local euler angles of the selected object.
            /// </summary>
            public readonly Vector3 OldObjectLocalEulerAngles;
            /// <summary>
            /// The degree by which the object was rotated (the z value of the local euler angles).
            /// </summary>
            public readonly float Degree;
            /// <summary>
            /// Whether it was moved or rotated.
            /// </summary>
            public readonly ProgressState MoveOrRotate;
            /// <summary>
            /// Whether children were included in case the selected object was a mind map node.
            /// </summary>
            public readonly bool IncludeChildren;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="selectedObject">Is the selected drawable type object.</param>
            /// <param name="surface">Is the drawable surface where the selected object is displayed.</param>
            /// <param name="id">Is the ID of the selected object.</param>
            /// <param name="oldObjectPosition">The old position of the selected object.</param>
            /// <param name="newObjectPosition">The new position of the selected object.</param>
            /// <param name="oldObjectLocalEulerAngles">The old local euler angles of the selected object.</param>
            /// <param name="degree">The degree on that the object was rotated. (Is the z value of the local euler angeles).</param>
            /// <param name="moveOrRotate">The state if was moved or rotated.</param>
            public Memento(GameObject selectedObject, GameObject surface, string id,
                Vector3 oldObjectPosition, Vector3 newObjectPosition, Vector3 oldObjectLocalEulerAngles,
                float degree, ProgressState moveOrRotate, bool includeChildren)
            {
                SelectedObject = selectedObject;
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                ID = id;
                OldObjectPosition = oldObjectPosition;
                NewObjectPosition = newObjectPosition;
                OldObjectLocalEulerAngles = oldObjectLocalEulerAngles;
                Degree = degree;
                MoveOrRotate = moveOrRotate;
                IncludeChildren = includeChildren;
            }
        }

        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="MoveRotateAction"/>
        /// </summary>
        private enum ProgressState
        {
            SelectObject,
            Move,
            Rotate,
            Finish
        }

        /// <summary>
        /// The executed progress state (Move or Rotate)
        /// </summary>
        private ProgressState executedOperation;

        /// <summary>
        /// The selected drawable type object for moving or rotating.
        /// </summary>
        private GameObject selectedObject;

        /// <summary>
        /// The old position of the selected object.
        /// </summary>
        private Vector3 oldObjectPosition;

        /// <summary>
        /// The old local euler angles of the selected object.
        /// </summary>
        private Vector3 oldObjectLocalEulerAngles;

        /// <summary>
        /// The new object position.
        /// </summary>
        private Vector3 newObjectPosition;

        /// <summary>
        /// The new local euler angles.
        /// </summary>
        private Vector3 newObjectLocalEulerAngles;

        /// <summary>
        /// Handles object selection and operation choice.
        /// </summary>
        private readonly MoveRotateSelectionController selectionController = new();

        /// <summary>
        /// Handles moving the selected object.
        /// </summary>
        private readonly MoveRotateMoveOperation moveOperation = new();

        /// <summary>
        /// Handles rotating the selected object.
        /// </summary>
        private readonly MoveRotateRotationOperation rotationOperation = new();

        /// <summary>
        /// Deactivates the blink effect if it is still active
        /// and destroys the rigidbody and collision controller if there are still active.
        /// If the action was not completed in full (finish), the changes are reset.
        /// Destroys the rotation and move menu if it is still active.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (selectionController.IsActive)
            {
                selectionController.Cancel();
            }
            BlinkEffect.Deactivate(selectedObject);
            CollisionDetectionManager.Disable(selectedObject);

            if (progressState != ProgressState.Finish && selectedObject != null)
            {
                if (progressState == ProgressState.Move)
                {
                    moveOperation.Restore(selectedObject, oldObjectPosition);
                }

                if (progressState == ProgressState.Rotate)
                {
                    rotationOperation.Restore(selectedObject, oldObjectLocalEulerAngles.z);
                }
            }

            RotationMenu.Instance.Destroy();
            MoveMenu.Instance.Destroy();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MoveRotator"/>.
        /// It moves or rotates a chosen drawable type object.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            Cancel();

            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// With this block, the user can select a Drawable Type object.
                    case ProgressState.SelectObject:
                        Selection();
                        break;

                    /// With this block, the user can move the object.
                    case ProgressState.Move:
                        Move();
                        break;

                    /// With this block the user can rotate the selected object.
                    case ProgressState.Rotate:
                        Rotate();
                        break;

                    /// With this block, it is checked whether changes have been made.
                    /// If so, the action is completed; otherwise, it is reset.
                    case ProgressState.Finish:
                        return Finish();
                }
            }

            return false;
        }

        /// <summary>
        /// Provides the option to cancel the action.
        /// </summary>
        private void Cancel()
        {
            if ((selectedObject != null || selectionController.IsActive) && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                if (selectionController.IsActive)
                {
                    selectionController.Cancel();
                    progressState = ProgressState.SelectObject;
                    return;
                }
                BlinkEffect.Deactivate(selectedObject);
                CollisionDetectionManager.Disable(selectedObject);
                if (progressState != ProgressState.Finish && selectedObject != null)
                {
                    if (progressState == ProgressState.Move)
                    {
                        moveOperation.Restore(selectedObject, oldObjectPosition);
                    }

                    if (progressState == ProgressState.Rotate)
                    {
                        rotationOperation.Restore(selectedObject, oldObjectLocalEulerAngles.z);
                    }
                }
                RotationMenu.Instance.Destroy();
                MoveMenu.Instance.Destroy();

                progressState = ProgressState.SelectObject;
                selectedObject = null;
            }
        }

        /// <summary>
        /// Enables the user to select a <see cref="DrawableType"/> object.
        /// After the selection, a menu is opened, providing the user with options to move and rotate.
        /// The respective choices are initiated by using the corresponding buttons.
        /// Furthermore, the user is allowed to switch the <see cref="DrawableType"/> object before
        /// making the move or rotate selection.
        /// </summary>
        private void Selection()
        {
            if (!selectionController.TrySelect(out MoveRotateSelectionController.Result result))
            {
                return;
            }

            selectedObject = result.SelectedObject;
            oldObjectPosition = result.OldPosition;
            oldObjectLocalEulerAngles = result.OldLocalEulerAngles;

            switch (result.SelectedOperation)
            {
                case MoveRotateSelectionController.Operation.Move:
                    progressState = ProgressState.Move;
                    executedOperation = ProgressState.Move;
                    break;

                case MoveRotateSelectionController.Operation.Rotate:
                    progressState = ProgressState.Rotate;
                    executedOperation = ProgressState.Rotate;
                    break;
            }
        }

        /// <summary>
        /// Moves the selected object.
        /// </summary>
        private void Move()
        {
            if (moveOperation.TryExecute(selectedObject, out Vector3 position))
            {
                newObjectPosition = position;
                progressState = ProgressState.Finish;
            }
        }

        /// <summary>
        /// Rotates the selected object.
        /// </summary>
        private void Rotate()
        {
            if (rotationOperation.TryExecute(selectedObject, ref newObjectPosition, ref newObjectLocalEulerAngles))
            {
                progressState = ProgressState.Finish;
            }
        }

        /// <summary>
        /// Completes the action, if there are any changes in the position or in the euler angles.
        /// Otherwise it will reset the action.
        /// The action is only completed properly if the object is no longer involved in any collision.
        /// The same applies to children when using mind map nodes with the option to include children.
        /// </summary>
        /// <returns>State of success.</returns>
        private bool Finish()
        {
            newObjectPosition = selectedObject.transform.localPosition;
            newObjectLocalEulerAngles = selectedObject.transform.localEulerAngles;

            if (oldObjectPosition != newObjectPosition
                || oldObjectLocalEulerAngles != newObjectLocalEulerAngles)
            {
                /// Checks if a child is in collision.
                bool childInCollision = false;
                if (selectedObject.CompareTag(Tags.MindMapNode))
                {
                    CollisionController[] ccs = GameFinder.GetAttachedObjectsObject(selectedObject)
                        .GetComponentsInChildren<CollisionController>();
                    foreach (CollisionController cc in ccs)
                    {
                        childInCollision = childInCollision || cc.IsInCollision();
                    }
                }

                /// If there is no collision, the action is completed.
                /// To do this, necessary data is queried, and then a Memento is created.
                /// Subsequently, the rigidbodies and collision controllers are destroyed,
                /// the menus are closed, and the progress state of the action is set to
                /// be completed.
                if (selectedObject.GetComponent<CollisionController>() != null
                    && !selectedObject.GetComponent<CollisionController>().IsInCollision() && !childInCollision)
                {
                    float degree = newObjectLocalEulerAngles.z;
                    bool includeChildren = RotationMenu.Instance.IncludeChildren && executedOperation == ProgressState.Rotate
                        || MoveMenu.Instance.IncludeChildren && executedOperation == ProgressState.Move;
                    memento = new Memento(selectedObject, GameFinder.GetDrawableSurface(selectedObject), selectedObject.name,
                        oldObjectPosition, newObjectPosition, oldObjectLocalEulerAngles, degree, executedOperation,
                        includeChildren);
                    Destroyer.Destroy(selectedObject.GetComponent<Rigidbody>());
                    Destroyer.Destroy(selectedObject.GetComponent<CollisionController>());
                    GameMindMapTransform.DestroyRigidBodiesAndCollisionControllersOfChildren(selectedObject);
                    new RbAndCCDestroyerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                        memento.SelectedObject.name).Execute();
                    RotationMenu.Instance.Destroy();
                    MoveMenu.Instance.Destroy();
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                } else
                {
                    if (executedOperation == ProgressState.Rotate)
                    {
                        /// This code is needed because occasionally a trigger exit is not registered during rotation.
                        /// This block attempts to set the isInCollision value of the Collision Controller to false.
                        /// However, if the object is still in a collision, it will be set back to true by its OnStayCollision method.
                        selectedObject.GetComponent<CollisionController>().SetCollisionToFalse();
                    }
                }
            }
            else
            {
                /// Block for reset.
                CollisionDetectionManager.Disable(selectedObject);
                GameMindMapTransform.DestroyRigidBodiesAndCollisionControllersOfChildren(selectedObject);
                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                new RbAndCCDestroyerNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                    selectedObject.name).Execute();
                selectedObject = null;
                RotationMenu.Instance.Destroy();
                MoveMenu.Instance.Destroy();
                progressState = ProgressState.SelectObject;
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., moves / rotates back to the old position / euler angles.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.SelectedObject == null && memento.ID != null)
            {
                memento.SelectedObject = GameFinder.FindAttachedOrLocalDescendant(memento.Surface.GetDrawableSurface(),
                    memento.ID);
            }

            if (memento.SelectedObject != null)
            {
                if (memento.MoveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(memento.SelectedObject, memento.OldObjectPosition,
                                            memento.IncludeChildren);
                    new MoveNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.ID,
                        memento.OldObjectPosition, memento.IncludeChildren).Execute();
                }
                else if (memento.MoveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.SelectedObject, memento.OldObjectLocalEulerAngles.z,
                        memento.IncludeChildren);
                    new RotatorNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.ID,
                        memento.OldObjectLocalEulerAngles.z, memento.IncludeChildren).Execute();

                    if (memento.OldObjectPosition != memento.NewObjectPosition)
                    {
                        GameMoveRotator.SetPosition(memento.SelectedObject, memento.OldObjectPosition,
                            memento.IncludeChildren);
                        new MoveNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.ID,
                            memento.OldObjectPosition, memento.IncludeChildren).Execute();
                    }
                }

                GameMindMapTransform.DestroyRigidBodiesAndCollisionControllersOfChildren(
                    GameFinder.GetAttachedObjectsObject(memento.SelectedObject));
                new RbAndCCDestroyerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                    memento.SelectedObject.name).Execute();
            }
        }

        /// <summary>
        /// Repeats this action, i.e., moves / rotates again to the new position / euler angles.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.SelectedObject == null && memento.ID != null)
            {
                memento.SelectedObject = GameFinder.FindAttachedOrLocalDescendant(memento.Surface.GetDrawableSurface(),
                    memento.ID);
            }
            if (memento.SelectedObject != null)
            {
                if (memento.MoveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(memento.SelectedObject, memento.NewObjectPosition,
                        memento.IncludeChildren);
                    new MoveNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.ID,
                        memento.NewObjectPosition, memento.IncludeChildren).Execute();
                }
                else if (memento.MoveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.SelectedObject, memento.Degree, memento.IncludeChildren);
                    new RotatorNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.ID,
                        memento.Degree, memento.IncludeChildren).Execute();

                    if (memento.OldObjectPosition != memento.NewObjectPosition)
                    {
                        GameMoveRotator.SetPosition(memento.SelectedObject, memento.NewObjectPosition,
                            memento.IncludeChildren);
                        new MoveNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.ID,
                            memento.NewObjectPosition, memento.IncludeChildren).Execute();
                    }
                }

                GameMindMapTransform.DestroyRigidBodiesAndCollisionControllersOfChildren(
                    GameFinder.GetAttachedObjectsObject(memento.SelectedObject));
                new RbAndCCDestroyerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                    memento.SelectedObject.name).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="MoveRotateAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="MoveRotateAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MoveRotateAction();
        }

        /// <summary>
        /// A new instance of <see cref="MoveRotateAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="MoveRotateAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.MoveRotator"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MoveRotator;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The ID of the moved or rotated object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.SelectedObject == null)
            {
                return new();
            }
            else
            {
                return new()
                {
                    memento.SelectedObject.name
                };
            }
        }
    }
}
