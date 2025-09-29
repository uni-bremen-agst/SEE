using SEE.Game.Table;
using UnityEngine;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Moves a table including its portal on all clients.
    /// </summary>
    /// <remarks>If you do not want to move the portal, use
    /// <see cref="MoveTableOnlyNetAction"/>.</remarks>
    public class MoveTableAndPortalNetAction : MoveTableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        /// <param name="position">The position to which the table should be moved</param>
        public MoveTableAndPortalNetAction(string tableID, Vector3 position)
            : base(tableID, position)
        {
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
