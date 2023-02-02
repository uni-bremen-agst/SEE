using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class manages the delete action deleting one metrics board. When deleting a board, you should use this
    /// class.
    /// </summary>
    internal class DeleteBoardAction : AbstractPlayerAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;
        
        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="DeleteBoardAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The entire configuration of the board for creating it again when the player wants to undo the action.
            /// </summary>
            public readonly BoardConfig boardConfig;

            /// <summary>
            /// The constructor of this struct which simply assigns the parameters to the fields of this struct.
            /// </summary>
            /// <param name="boardConfig">The configuration of the board, from which the board can be completely
            /// restored.</param>
            public Memento(BoardConfig boardConfig)
            {
                this.boardConfig = boardConfig;
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.DeleteBoard"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (Input.GetMouseButtonDown(0) && Raycasting.RaycastAnything(out RaycastHit raycastHit))
            {
                WidgetsManager widgetsManager = raycastHit.transform.GetComponent<WidgetsManager>();
                if (widgetsManager == null)  
                {
                    // If the clicked object isn't a metrics board, is it maybe a widget? Then we could get its
                    // parent's WidgetsManager
                    widgetsManager = raycastHit.transform.parent.GetComponent<WidgetsManager>();
                }
                if (widgetsManager != null)
                {
                    memento = new Memento(ConfigManager.GetBoardConfig(widgetsManager));
                    BoardsManager.Delete(memento.boardConfig.Title);
                    new DeleteBoardNetAction(memento.boardConfig.Title).Execute();
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates the deleted board again from the saved board config.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            BoardsManager.Create(memento.boardConfig);
            new CreateBoardNetAction(memento.boardConfig).Execute();
        }
        
        /// <summary>
        /// Deletes the board (again).
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            BoardsManager.Delete(memento.boardConfig.Title);
            new DeleteBoardNetAction(memento.boardConfig.Title).Execute();
        }
        
        /// <summary>
        /// Returns a new instance of <see cref="DeleteBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DeleteBoardAction();
        }
        
        /// <summary>
        /// Returns a new instance of <see cref="DeleteBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        
        /// <summary>
        /// Returns the ID (name) of the metrics board that has been deleted by this action.
        /// </summary>
        /// <returns></returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.boardConfig.Title };
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this class.
        /// </summary>
        /// <returns><see cref="ActionStateType.DeleteBoard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.DeleteBoard;
        }
    }
}
