using SEE.Game.City;
using SEE.Game;
using SEE.GameObjects;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using System;
using SEE.Utils.Paths;
using SEE.GO;
using Cysharp.Threading.Tasks;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Loads a part of a ReflexionCity to all clients.
    /// </summary>
    public class LoadPartOfReflexionCityNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the table on which city the part should be loaded.
        /// </summary>
        public string TableID;

        /// <summary>
        /// Distinction between wheter an architecture or implementation graph should be loaded.
        /// </summary>
        public bool LoadArchitecture;

        /// <summary>
        /// The GXL file to be loaded.
        /// </summary>
        public DataPath GXL;

        /// <summary>
        /// The project folder; can be null if a architecture graph should be loaded.
        /// </summary>
        public DataPath ProjectFolder;

        /// <summary>
        /// Creates a new LoadPartOfReflexionCityNetAction.
        /// </summary>
        /// <param name="tableID">The unique name of the table on which city the part should be loaded.</param>
        /// <param name="loadArchitecture">Distinction between whether an architecture or implementation graph should be loaded.</param>
        /// <param name="gxl">The GXL file to be loaded.</param>
        /// <param name="projectFolder">The project folder; can be null if a architecture graph should be loaded.</param>
        public LoadPartOfReflexionCityNetAction(string tableID, bool loadArchitecture, DataPath gxl, DataPath projectFolder = null) : base()
        {
            TableID = tableID;
            LoadArchitecture = loadArchitecture;
            GXL = gxl;
            ProjectFolder = projectFolder;
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
        /// Adds the city of type <see cref="CityType"/> identified by <see cref="TableID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                GameObject city = citiesHolder.Find(TableID);
                if (city.GetComponent<SEEReflexionCity>() != null)
                {
                    city.GetComponent<SEEReflexionCity>().LoadAndDrawSubgraphAsync(GXL, LoadArchitecture? null : ProjectFolder).Forget();
                } else
                {
                    throw new Exception($"GXL can't be loaded because ther is no ReflexionCity component.");
                }
            }
            else
            {
                throw new Exception($"The city can't be added because there is no CitieHolder component.");
            }
        }
    }
}