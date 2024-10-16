using SEE.Game.HolisticMetrics;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for deleting a metrics board on all clients.
    /// </summary>
    public class DeleteBoardNetAction : HolisticMetricsNetAction
    {
        /// <summary>
        /// The name of the board to delete. This is unique and can identify a board.
        /// </summary>
        public string BoardName;

        /// <summary>
        /// The constructor of this class. It only assigns the parameter value to a field.
        /// </summary>
        /// <param name="boardName">The name of the board to delete.</param>
        public DeleteBoardNetAction(string boardName)
        {
            BoardName = boardName;
        }

        /// <summary>
        /// This method does nothing.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// This method executes the action on all clients except the requester, i.e., it deletes the board on all
        /// clients.
        /// </summary>
        public override void ExecuteOnClient()
        {
            BoardsManager.Delete(BoardName);
        }
    }
}
