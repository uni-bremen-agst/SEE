using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.UI.RuntimeConfigMenu;
using System;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Adds a City to all clients.
    /// </summary>
    public class AddNodeTypeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the table to which city the node type should be added.
        /// </summary>
        public string TableID;

        /// <summary>
        /// The visual node attribute for the node type that should be added.
        /// </summary>
        public VisualNodeAttributes VisualNodeAttribute;

        /// <summary>
        /// The name for the added node type.
        /// </summary>
        public string NodeType;

        /// <summary>
        /// Creates a new AddNodeTypeNetAction.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which city the node type should be added.</param>
        /// <param name="nodeType">The name for the added node type.</param>
        /// <param name="visualNodeAttribute">The visual node attribute for the node type that should be added.</param>
        public AddNodeTypeNetAction(string tableID, string nodeType, VisualNodeAttributes visualNodeAttribute) : base()
        {
            TableID = tableID;
            NodeType = nodeType;
            VisualNodeAttribute = visualNodeAttribute;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Adds the node type with his visual node attribute into the given table city on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                GameObject city = citiesHolder.Find(TableID);
                if (city == null)
                {
                    throw new Exception($"The city can't be found");
                }
                city.GetComponent<AbstractSEECity>().NodeTypes[NodeType] = VisualNodeAttribute;
                /// Notify <see cref="RuntimeConfigMenu"/> about changes.
                if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
                {
                    runtimeConfigMenu.PerformRebuildOnNextOpening();
                }
            }
            else
            {
                throw new Exception($"The node type can't be added because there is no CitieHolder component.");
            }
        }
    }
}