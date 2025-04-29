using SEE.Game.Table;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Disables the city on the corresponding table for all clients.
    /// </summary>
    public class DisableCityTableNetAction : TableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
		public DisableCityTableNetAction(string tableID) : base(tableID) { }

        /// <summary>
        /// Disables the city of the table on the client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.DisableCity(Table);
        }
    }
}