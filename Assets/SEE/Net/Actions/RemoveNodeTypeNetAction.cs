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
    public class RemoveNodeTypeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the table to which city the node type should be removed.
        /// </summary>
        public string TableID;

        /// <summary>
        /// The name of the ro removed node type.
        /// </summary>
        public string NodeType;

        /// <summary>
        /// Creates a new AddNodeTypeNetAction.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which city the node type should be removed.</param>
        /// <param name="nodeType">The name of the to removed node type.</param>
        public RemoveNodeTypeNetAction(string tableID, string nodeType) : base()
        {
            TableID = tableID;
            NodeType = nodeType;
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
        /// Removes the node type of the given table city on each client.
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
                city.GetComponent<AbstractSEECity>().NodeTypes.Remove(NodeType);
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
