using SEE.Game.Table;
using UnityEngine;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Moves a table including the portal on all clients.
    /// </summary>
    public class MoveIncPortalTableNetAction : TableNetAction
    {
        /// <summary>
        /// The position to which the table should be moved.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        /// <param name="position">The position to which the table should be moved</param>
        public MoveIncPortalTableNetAction(string tableID, Vector3 position) : base(tableID)
        {
            Position = position;
        }

        /// <summary>
        /// Moves the table including the portal to the given position on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.MoveIncPortal(Table, Position);
        }
    }
}
