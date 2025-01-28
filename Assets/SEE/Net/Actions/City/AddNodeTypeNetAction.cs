using SEE.Game;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.City
{
    /// <summary>
    /// Adds a node type to a city on all clients.
    /// </summary>
    public class AddNodeTypeNetAction : CityNetAction
    {
        /// <summary>
        /// The visual node attribute for the node type that should be added.
        /// </summary>
        public VisualNodeAttributes VisualNodeAttribute;

        /// <summary>
        /// The name for the added node type.
        /// </summary>
        public string NodeType;

        /// <summary>
        /// The constructor of this action. Sets the <see cref="VisualNodeAttributes"/> and the name
        /// of the <see cref="NodeType"/> to be added.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which city the node type should be added.</param>
        /// <param name="nodeType">The name for the added node type.</param>
        /// <param name="visualNodeAttribute">The visual node attribute for the node type that should be added.</param>
        public AddNodeTypeNetAction(string tableID, string nodeType, VisualNodeAttributes visualNodeAttribute) : base(tableID)
        {
            NodeType = nodeType;
            VisualNodeAttribute = visualNodeAttribute;
        }

        /// <summary>
        /// Adds the node type with its visual node attributes to the given table city on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            City.GetComponent<AbstractSEECity>().NodeTypes[NodeType] = VisualNodeAttribute;
            /// Notify <see cref="RuntimeConfigMenu"/> about changes.
            if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
            {
                runtimeConfigMenu.PerformRebuildOnNextOpening();
            }
        }
    }
}
