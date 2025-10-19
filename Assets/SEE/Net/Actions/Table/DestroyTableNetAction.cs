using SEE.Game.Table;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Destroys a table on all clients.
    /// </summary>
    public class DestroyTableNetAction : TableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        public DestroyTableNetAction(string tableID) : base(tableID) {}

        /// <summary>
        /// Destroys the table on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.Destroy(Table);
        }
    }
}
