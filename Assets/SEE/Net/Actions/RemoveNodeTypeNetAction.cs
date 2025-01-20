using SEE.Game;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Removes a node type of a city on all clients.
    /// </summary>
    public class RemoveNodeTypeNetAction : CityNetAction
    {
        /// <summary>
        /// The name of the to removed node type.
        /// </summary>
        public string NodeType;

        /// <summary>
        /// Creates a new AddNodeTypeNetAction.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which city the node type should be removed.</param>
        /// <param name="nodeType">The name of the to removed node type.</param>
        public RemoveNodeTypeNetAction(string tableID, string nodeType) : base(tableID)
        {
            TableID = tableID;
            NodeType = nodeType;
        }

        /// <summary>
        /// Removes the node type of the given table city on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            City.GetComponent<AbstractSEECity>().NodeTypes.Remove(NodeType);
            /// Notify <see cref="RuntimeConfigMenu"/> about changes.
            if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
            {
                runtimeConfigMenu.PerformRebuildOnNextOpening();
            }
        }
    }
}
