using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Net.Actions.HolisticMetrics;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class manages the delete action deleting one metrics board. When deleting a board, you should use this
    /// class.
    /// </summary>
    internal class DeleteBoardAction : Action
    {
        /// <summary>
        /// The name of the board to delete.
        /// </summary>
        private readonly string boardName;

        /// <summary>
        /// The entire configuration of the board for creating it again when the player wants to undo the action.
        /// </summary>
        private readonly BoardConfig boardConfig;
        
        /// <summary>
        /// Creates this action, that means we save the name and the entire configuration of the board in fields of this
        /// class.
        /// </summary>
        /// <param name="boardName">The name of the board to delete</param>
        internal DeleteBoardAction(string boardName)
        {
            this.boardName = boardName;
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);
            boardConfig = ConfigManager.GetBoardConfig(widgetsManager);
        }
        
        /// <summary>
        /// Deletes the board (again).
        /// </summary>
        internal override void Do()
        {
            BoardsManager.Delete(boardName);
            new DeleteBoardNetAction(boardName).Execute();
        }

        /// <summary>
        /// Creates the deleted board again from the saved board config.
        /// </summary>
        internal override void Undo()
        {
            BoardsManager.Create(boardConfig);
            new CreateBoardNetAction(boardConfig).Execute();
        }
    }
}