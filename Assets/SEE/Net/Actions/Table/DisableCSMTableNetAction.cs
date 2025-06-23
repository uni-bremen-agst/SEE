using SEE.Game.Table;
using SEE.GameObjects;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Disables the <see cref="CitySelectionManager"/> on the corresponding table
    /// for all clients.
    /// </summary>
    public class DisableCSMTableNetAction : TableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
		public DisableCSMTableNetAction(string tableID) : base(tableID) { }

        /// <summary>
        /// Disables the <see cref="CitySelectionManager"/> of the table on the client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.DisableCSM(Table);
        }
    }
}
