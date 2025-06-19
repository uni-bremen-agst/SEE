using SEE.Game.Table;
using UnityEngine;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Scales a table on all clients.
    /// </summary>
    public class ScaleTableNetAction : TableNetAction
    {
        /// <summary>
        /// The scale to which the table should be scaled.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        /// <param name="scale">The scale to which the table should be scaled.</param>
        public ScaleTableNetAction(string tableID, Vector3 scale) : base(tableID)
        {
            Scale = scale;
        }

        /// <summary>
        /// Scales the table to the given scale on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.Scale(Table, Scale);
        }
    }
}
