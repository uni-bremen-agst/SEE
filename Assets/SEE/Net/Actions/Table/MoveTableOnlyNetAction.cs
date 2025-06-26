using SEE.Game.Table;
using UnityEngine;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Moves a table (but not the portal) on all clients.
    /// </summary>
    /// <remarks>If you want to also move the portal, use
    /// <see cref="MoveTableAndPortalNetAction"/>.</remarks>
    public class MoveTableOnlyNetAction : MoveTableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        /// <param name="position">The position to which the table should be moved</param>
        public MoveTableOnlyNetAction(string tableID, Vector3 position)
            : base(tableID, position)
        {
        }

        /// <summary>
        /// Moves the table to the given position on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.Move(Table, Position);
        }
    }
}
