using SEE.Game.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This action manages the creation of a specific metrics board.
    /// </summary>
    internal class CreateBoardAction : Action
    {
        /// <summary>
        /// The configuration of the board to create/that has been created.
        /// </summary>
        private readonly BoardConfig boardConfig;

        /// <summary>
        /// Creates this action. That does not execute it, it only prepares it.
        /// </summary>
        /// <param name="boardConfig">The configuration of the board to create.</param>
        internal CreateBoardAction(BoardConfig boardConfig)
        {
            this.boardConfig = boardConfig;
        }

        /// <summary>
        /// This method (re-)executes the action, i.e. creates the board from the given configuration.
        /// </summary>
        internal override void Do()
        {
            BoardsManager.Create(boardConfig);
            new CreateBoardNetAction(boardConfig).Execute();
        }

        /// <summary>
        /// Deletes the board that was created.
        /// </summary>
        internal override void Undo()
        {
            BoardsManager.Delete(boardConfig.Title);
            new DeleteBoardNetAction(boardConfig.Title).Execute();
        }
    }
}