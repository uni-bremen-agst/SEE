using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for creating a new metrics board on all clients.
    /// </summary>
    public class CreateBoardNetAction : AbstractNetAction
    {
        /// <summary>
        /// The board configuration of the new board.
        /// </summary>
        public BoardConfig BoardConfig;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="boardConfig">The configuration of the board to add.</param>
        public CreateBoardNetAction(BoardConfig boardConfig)
        {
            BoardConfig = boardConfig;
        }
        
        /// <summary>
        /// This method does not do anything.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Executes the action on each client except the requester, i.e., creates the board on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                BoardsManager.Create(BoardConfig);   
            }
        }
    }
}
