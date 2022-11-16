using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for changing the position and orientation of a metrics board on all clients.
    /// </summary>
    public class MoveBoardNetAction : HolisticMetricsNetAction
    {
        /// <summary>
        /// The name of the board to move (this is unique and can identify a board).
        /// </summary>
        public string BoardName;

        /// <summary>
        /// The new position of the board.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The new rotation of the board.
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// The constructor of this class. It only assigns the parameter values to fields.
        /// </summary>
        /// <param name="boardName">The name of the board to move</param>
        /// <param name="position">The new position of the board</param>
        /// <param name="rotation">The new rotation of the board</param>
        public MoveBoardNetAction(string boardName, Vector3 position, Quaternion rotation)
        {
            BoardName = boardName;
            Position = position;
            Rotation = rotation;
        }
        
        /// <summary>
        /// This method does nothing.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// This method executes the action on all clients except the requester, i.e., changes the position/rotation of
        /// the board.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                BoardsManager.Move(BoardName, Position, Rotation);    
            }
        }
    }
}
