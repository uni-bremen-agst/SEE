using SEE.Game.Table;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Enables and redraws the city on the corresponding table for all clients.
    /// </summary>
    public class EnableCityTableNetAction : TableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
		public EnableCityTableNetAction(string tableID) : base(tableID) { }

        /// <summary>
        /// Enables and redraws the city of the table on the client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.EnableCity(Table);
        }
    }
}
