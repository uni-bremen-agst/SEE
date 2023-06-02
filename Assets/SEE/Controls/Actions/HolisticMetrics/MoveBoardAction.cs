using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// Each instance of this class manages one concrete move action of a metrics board from an old position and
    /// rotation to a new position and rotation. This should be used when moving a board.
    /// </summary>
    internal class MoveBoardAction : AbstractPlayerAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="MoveBoardAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The name of the board that will be moved.
            /// </summary>
            internal readonly string boardName;

            /// <summary>
            /// The old position of the board so we can revert this action.
            /// </summary>
            internal readonly Vector3 oldPosition;

            /// <summary>
            /// The position to which the board is to be moved by this action.
            /// </summary>
            internal readonly Vector3 newPosition;

            /// <summary>
            /// The old rotation that the board had, so we can revert this action.
            /// </summary>
            internal readonly Quaternion oldRotation;

            /// <summary>
            /// The new rotation to which the board will be set.
            /// </summary>
            internal readonly Quaternion newRotation;

            /// <summary>
            /// Initializes the fields of this instance.
            /// </summary>
            /// <param name="boardName">The name of the board; will be used to identify the board.</param>
            /// <param name="oldPosition">The position that the board is/was at before the moving.</param>
            /// <param name="newPosition">The position to move the board to.</param>
            /// <param name="oldRotation">The rotation the board is/was at before the moving.</param>
            /// <param name="newRotation">The rotation to rotate the board to.</param>
            internal Memento(string boardName, Vector3 oldPosition, Vector3 newPosition, Quaternion oldRotation,
                Quaternion newRotation)
            {
                this.boardName = boardName;
                this.oldPosition = oldPosition;
                this.newPosition = newPosition;
                this.oldRotation = oldRotation;
                this.newRotation = newRotation;
            }
        }

        /// <summary>
        /// Makes the metrics boards movable by activating a little button underneath each board that can be dragged
        /// around.
        /// </summary>
        public override void Start()
        {
            BoardsManager.ToggleMoving(true);
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MoveBoard"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (BoardsManager.TryGetMovement(out string boardName, out Vector3 oldPosition, out Vector3 newPosition,
                    out Quaternion oldRotation, out Quaternion newRotation))
            {
                memento = new Memento(boardName, oldPosition, newPosition, oldRotation, newRotation);
                BoardsManager.Move(memento.boardName, memento.newPosition, memento.newRotation);
                new MoveBoardNetAction(memento.boardName, memento.newPosition, memento.newRotation).Execute();
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deactivates the little buttons underneath the metrics boards that let the player move the boards around.
        /// </summary>
        public override void Stop()
        {
            BoardsManager.ToggleMoving(false);
        }

        /// <summary>
        /// Here we revert the moving action by moving the board to the old position on this client and all other
        /// clients. This method should only be called from the history.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            BoardsManager.Move(memento.boardName, memento.oldPosition, memento.oldRotation);
            new MoveBoardNetAction(memento.boardName, memento.oldPosition, memento.oldRotation).Execute();
        }

        /// <summary>
        /// Here we execute the moving action by moving the board to the new position on this client and all other
        /// clients. This method should only be called from the history and is also called from the base class of this
        /// class.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            BoardsManager.Move(memento.boardName, memento.newPosition, memento.newRotation);
            new MoveBoardNetAction(memento.boardName, memento.newPosition, memento.newRotation).Execute();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MoveBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MoveBoardAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MoveBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns a HashSet with one item which is the name of the board that was moved in this action.
        /// </summary>
        /// <returns>A HashSet with one item which is the name of the board that was moved in this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.boardName };
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this class.
        /// </summary>
        /// <returns><see cref="ActionStateType.MoveBoard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MoveBoard;
        }
    }
}
