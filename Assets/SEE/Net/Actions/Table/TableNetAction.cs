using SEE.Game;
using SEE.GameObjects;
using System;
using UnityEngine;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Superclass for all table net actions.
    /// </summary>
    public abstract class TableNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the table to be manipulated.
        /// </summary>
        public string TableID;

        /// <summary>
        /// The table object that should be manipulated by this action.
        /// Will be set in the <see cref="ExecuteOnClient"/> method.
        /// </summary>
        protected GameObject Table { get; private set; }

        /// <summary>
        /// The constructor of this action. It sets the <see cref="TableID"/> to identify the table
        /// to be manipulated.
        /// </summary>
        /// <param name="tableID">The unique name of the table to be manipulated.</param>
        public TableNetAction(string tableID) : base()
        {
            TableID = tableID;
        }

        /// <summary>
        /// Returns the table object with the id <see cref="TableID"/>.
        /// </summary>
        /// <returns>the found table object (never null)</returns>
        /// <exception cref="Exception">thrown if a table object cannot be found</exception>
        private GameObject Find()
        {
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                GameObject table = citiesHolder.FindTable(TableID);
                if (table == null)
                {
                    throw new Exception($"The table {TableID} can't be found.");
                }
                return table;
            }
            else
            {
                throw new Exception($"The table can't be found because there is no {nameof(CitiesHolder)} component.");
            }
        }

        /// <summary>
        /// Unifies the search for the city object for the subclasses.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Table = Find();
        }
    }
}
