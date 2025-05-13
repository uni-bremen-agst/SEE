using SEE.Game;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Sets an infinite portal for the table for all clients.
    /// </summary>
    public class SetInfinitePortalTableNetAction : TableNetAction
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
		public SetInfinitePortalTableNetAction(string tableID) : base(tableID) { }

        /// <summary>
        /// Sets an infinite portal for the table on the client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            Portal.SetInfinitePortal(GameFinder.FindChildWithTag(Table, Tags.CodeCity));
        }
    }
}