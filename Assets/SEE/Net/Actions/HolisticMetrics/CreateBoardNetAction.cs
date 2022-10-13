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
        public BoardConfiguration BoardConfiguration;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="boardConfiguration">The configuration of the board to add.</param>
        public CreateBoardNetAction(BoardConfiguration boardConfiguration)
        {
            BoardConfiguration = boardConfiguration;
        }
        
        /// <summary>
        /// This method does not do anything.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Executes the action on each client, i.e., creates the board on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            BoardsManager.Create(BoardConfiguration);
        }
    }
}
