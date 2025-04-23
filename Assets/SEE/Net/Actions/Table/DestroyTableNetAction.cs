using Cysharp.Threading.Tasks;
using SEE.Game.Table;
using UnityEngine;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Destroyes a table on all clients.
    /// </summary>
    public class DestroyTableNetAction : TableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        public DestroyTableNetAction(string tableID) : base(tableID) {}

        /// <summary>
        /// Destroyes the table on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.Destroy(Table);
        }
    }
}